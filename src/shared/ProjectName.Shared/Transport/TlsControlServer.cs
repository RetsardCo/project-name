using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Protocol;

namespace ProjectName.Shared.Transport
{
    /// <summary>
    /// Thread-safe TLS TCP server implementation for the control and signaling channel.
    /// Strictly adheres to Milestone 2 requirements:
    /// 1. TLS-encrypted transport.
    /// 2. Bounded message envelopes (max 64 KB).
    /// 3. Inbound monotonic sequence verification to prevent replay attacks.
    /// 4. One-controller-at-a-time enforcement (rejects concurrent sessions).
    /// </summary>
    public class TlsControlServer : ITlsControlServer
    {
        private readonly TcpListener _listener;
        private readonly X509Certificate2 _serverCertificate;
        private readonly RemoteCertificateValidationCallback? _clientCertValidator;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly object _stateLock = new();

        private CancellationTokenSource? _serverCts;
        private Task? _listenerLoopTask;
        private TcpClient? _currentClient;
        private SslStream? _currentSslStream;
        private long _outboundSequence;

        public int Port { get; private set; }
        public bool IsRunning { get; private set; }
        public bool HasActiveClient
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentClient != null && _currentClient.Connected;
                }
            }
        }

        public SequenceTracker InboundSequenceTracker { get; } = new();

        public event EventHandler<ProtocolMessageEventArgs>? MessageReceived;
        public event EventHandler<PeerConnectionEventArgs>? ClientConnected;
        public event EventHandler<PeerConnectionEventArgs>? ClientDisconnected;

        public TlsControlServer(
            int port = 0,
            X509Certificate2? serverCertificate = null,
            RemoteCertificateValidationCallback? clientCertValidator = null)
        {
            Port = port;
            _serverCertificate = serverCertificate ?? EphemeralCertificateGenerator.CreateDeviceCertificate("ProjectNameServer");
            _clientCertValidator = clientCertValidator ?? ((_, _, _, _) => true); // In LAN pairing mode, peer trust is verified via PIN/token exchange
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_stateLock)
            {
                if (IsRunning) return Task.CompletedTask;

                _listener.Start();
                Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
                IsRunning = true;
                _serverCts = new CancellationTokenSource();
            }

            _listenerLoopTask = Task.Run(() => AcceptClientsLoopAsync(_serverCts.Token), CancellationToken.None);
            return Task.CompletedTask;
        }

        private async Task AcceptClientsLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient incomingClient;
                try
                {
                    incomingClient = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    continue;
                }

                // Check single-controller policy
                bool shouldReject = false;
                lock (_stateLock)
                {
                    if (_currentClient != null && _currentClient.Connected)
                    {
                        shouldReject = true;
                    }
                }

                if (shouldReject)
                {
                    _ = HandleRejectedClientAsync(incomingClient);
                    continue;
                }

                // Handle the authenticated primary client connection
                _ = HandlePrimaryClientAsync(incomingClient, cancellationToken);
            }
        }

        private async Task HandleRejectedClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (var netStream = client.GetStream())
                {
                    // Attempt quick TLS handshake to send an orderly ERROR envelope
                    using var ssl = new SslStream(netStream, false, _clientCertValidator);
                    await ssl.AuthenticateAsServerAsync(
                        new SslServerAuthenticationOptions
                        {
                            ServerCertificate = _serverCertificate,
                            ClientCertificateRequired = false,
                            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                        },
                        CancellationToken.None).ConfigureAwait(false);

                    var busyEnvelope = ProtocolEnvelope.Create(
                        MessageTypes.Error,
                        new { error = "BUSY", message = "Another controller session is already actively connected." },
                        0);
                    string rawJson = JsonSerializer.Serialize(busyEnvelope);
                    await ControlMessageFraming.WriteFrameAsync(ssl, rawJson, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch
            {
                // Ignore transient rejection errors
            }
        }

        private async Task HandlePrimaryClientAsync(TcpClient client, CancellationToken serverShutdownToken)
        {
            EndPoint? remoteEndPoint = null;
            SslStream? sslStream = null;

            try
            {
                remoteEndPoint = client.Client.RemoteEndPoint;
                var netStream = client.GetStream();
                sslStream = new SslStream(netStream, false, _clientCertValidator);

                await sslStream.AuthenticateAsServerAsync(
                    new SslServerAuthenticationOptions
                    {
                        ServerCertificate = _serverCertificate,
                        ClientCertificateRequired = false,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                    },
                    serverShutdownToken).ConfigureAwait(false);

                lock (_stateLock)
                {
                    _currentClient = client;
                    _currentSslStream = sslStream;
                    InboundSequenceTracker.Reset();
                    Interlocked.Exchange(ref _outboundSequence, 0);
                }

                ClientConnected?.Invoke(this, new PeerConnectionEventArgs(remoteEndPoint));

                // Message processing loop
                while (!serverShutdownToken.IsCancellationRequested && client.Connected)
                {
                    string? rawJson = await ControlMessageFraming.ReadFrameAsync(sslStream, serverShutdownToken).ConfigureAwait(false);
                    if (rawJson == null)
                    {
                        // Clean peer disconnect
                        break;
                    }

                    // Validate schema, bounds, and sequence order
                    var validation = ProtocolEnvelope.Validate(rawJson, InboundSequenceTracker.NextExpectedSequence);
                    if (!validation.IsValid)
                    {
                        // Send validation failure envelope back and terminate session to prevent corrupted states
                        var errorEnvelope = ProtocolEnvelope.Create(
                            MessageTypes.Error,
                            new { error = "VALIDATION_FAILED", detail = validation.Error },
                            Interlocked.Increment(ref _outboundSequence));
                        await SendEnvelopeInternalAsync(sslStream, errorEnvelope, CancellationToken.None).ConfigureAwait(false);
                        break;
                    }

                    // Accept sequence number
                    if (!InboundSequenceTracker.TryAccept(validation.Envelope!.SequenceNumber, out var seqError))
                    {
                        var errorEnvelope = ProtocolEnvelope.Create(
                            MessageTypes.Error,
                            new { error = "REPLAY_REJECTED", detail = seqError },
                            Interlocked.Increment(ref _outboundSequence));
                        await SendEnvelopeInternalAsync(sslStream, errorEnvelope, CancellationToken.None).ConfigureAwait(false);
                        break;
                    }

                    // Dispatch validated message to handlers
                    MessageReceived?.Invoke(this, new ProtocolMessageEventArgs(validation.Envelope, rawJson));
                }
            }
            catch (Exception ex)
            {
                // Disconnect triggered by error or network drop
                ClientDisconnected?.Invoke(this, new PeerConnectionEventArgs(remoteEndPoint, ex.Message));
                return;
            }
            finally
            {
                await CleanupCurrentClientAsync(remoteEndPoint, "Client session terminated.").ConfigureAwait(false);
            }
        }

        private async Task CleanupCurrentClientAsync(EndPoint? remoteEndPoint, string reason)
        {
            TcpClient? clientToClose;
            SslStream? streamToClose;

            lock (_stateLock)
            {
                clientToClose = _currentClient;
                streamToClose = _currentSslStream;
                _currentClient = null;
                _currentSslStream = null;
            }

            if (streamToClose != null)
            {
                try { await streamToClose.DisposeAsync().ConfigureAwait(false); } catch { }
            }
            if (clientToClose != null)
            {
                try { clientToClose.Dispose(); } catch { }
            }

            ClientDisconnected?.Invoke(this, new PeerConnectionEventArgs(remoteEndPoint, reason));
        }

        public async Task SendEnvelopeAsync(ProtocolEnvelope envelope, CancellationToken cancellationToken = default)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));

            SslStream? stream;
            lock (_stateLock)
            {
                stream = _currentSslStream;
            }

            if (stream == null)
            {
                throw new InvalidOperationException("Cannot send message: no active client connection.");
            }

            await SendEnvelopeInternalAsync(stream, envelope, cancellationToken).ConfigureAwait(false);
        }

        private async Task SendEnvelopeInternalAsync(SslStream stream, ProtocolEnvelope envelope, CancellationToken cancellationToken)
        {
            string rawJson = JsonSerializer.Serialize(envelope);
            await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await ControlMessageFraming.WriteFrameAsync(stream, rawJson, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task DisconnectCurrentClientAsync(string reason)
        {
            TcpClient? client;
            lock (_stateLock)
            {
                client = _currentClient;
            }

            if (client != null)
            {
                await CleanupCurrentClientAsync(client.Client.RemoteEndPoint, reason).ConfigureAwait(false);
            }
        }

        public async Task StopAsync()
        {
            lock (_stateLock)
            {
                if (!IsRunning) return;
                IsRunning = false;
                _serverCts?.Cancel();
            }

            try { _listener.Stop(); } catch { }

            await CleanupCurrentClientAsync(null, "Server stopped.").ConfigureAwait(false);

            if (_listenerLoopTask != null)
            {
                try { await _listenerLoopTask.ConfigureAwait(false); } catch { }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _sendLock.Dispose();
            _serverCts?.Dispose();
            _serverCertificate.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
