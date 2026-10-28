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
    /// Thread-safe TLS client implementation for connecting to an endpoint's control channel.
    /// Handles TLS negotiation, message framing, sequence incrementing, and background reading.
    /// </summary>
    public class TlsControlClient : ITlsControlClient
    {
        private TcpClient? _tcpClient;
        private SslStream? _sslStream;
        private readonly RemoteCertificateValidationCallback _serverCertValidator;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly object _stateLock = new();
        private CancellationTokenSource? _readCts;
        private Task? _readLoopTask;
        private long _outboundSequence;

        public bool IsConnected
        {
            get
            {
                lock (_stateLock)
                {
                    return _tcpClient != null && _tcpClient.Connected && _sslStream != null;
                }
            }
        }

        public SequenceTracker InboundSequenceTracker { get; } = new();

        public event EventHandler<ProtocolMessageEventArgs>? MessageReceived;
        public event EventHandler<PeerConnectionEventArgs>? Disconnected;

        public TlsControlClient(RemoteCertificateValidationCallback? serverCertValidator = null)
        {
            // By default accept self-signed certificates on trusted LAN; pairing PIN verification authenticates the session
            _serverCertValidator = serverCertValidator ?? ((_, _, _, _) => true);
        }

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            var tcp = new TcpClient();
            try
            {
                await tcp.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

                var netStream = tcp.GetStream();
                var sslStream = new SslStream(netStream, false, _serverCertValidator);

                await sslStream.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions
                    {
                        TargetHost = host,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                    },
                    cancellationToken).ConfigureAwait(false);

                lock (_stateLock)
                {
                    _tcpClient = tcp;
                    _sslStream = sslStream;
                    _readCts = new CancellationTokenSource();
                    InboundSequenceTracker.Reset();
                    Interlocked.Exchange(ref _outboundSequence, 0);
                }

                _readLoopTask = Task.Run(() => ReadLoopAsync(_readCts.Token), CancellationToken.None);
            }
            catch
            {
                tcp.Dispose();
                throw;
            }
        }

        private async Task ReadLoopAsync(CancellationToken cancellationToken)
        {
            SslStream? stream;
            lock (_stateLock)
            {
                stream = _sslStream;
            }

            if (stream == null) return;

            string? disconnectReason = null;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    string? rawJson = await ControlMessageFraming.ReadFrameAsync(stream, cancellationToken).ConfigureAwait(false);
                    if (rawJson == null)
                    {
                        disconnectReason = "Server closed connection cleanly.";
                        break;
                    }

                    var validation = ProtocolEnvelope.Validate(rawJson, InboundSequenceTracker.NextExpectedSequence);
                    if (!validation.IsValid)
                    {
                        disconnectReason = $"Received invalid message envelope: {validation.Error}";
                        break;
                    }

                    if (!InboundSequenceTracker.TryAccept(validation.Envelope!.SequenceNumber, out var seqError))
                    {
                        disconnectReason = $"Sequence validation failed: {seqError}";
                        break;
                    }

                    MessageReceived?.Invoke(this, new ProtocolMessageEventArgs(validation.Envelope, rawJson));
                }
            }
            catch (OperationCanceledException)
            {
                disconnectReason = "Client canceled read operation.";
            }
            catch (Exception ex)
            {
                disconnectReason = $"Read loop terminated with error: {ex.Message}";
            }
            finally
            {
                await DisconnectAsync(disconnectReason ?? "Connection terminated.").ConfigureAwait(false);
            }
        }

        public async Task SendEnvelopeAsync(ProtocolEnvelope envelope, CancellationToken cancellationToken = default)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));

            SslStream? stream;
            lock (_stateLock)
            {
                stream = _sslStream;
            }

            if (stream == null)
            {
                throw new InvalidOperationException("Cannot send envelope: client is not connected.");
            }

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

        public async Task<ProtocolEnvelope> SendMessageAsync(string type, object? payload, CancellationToken cancellationToken = default)
        {
            long seq = Interlocked.Increment(ref _outboundSequence);
            var envelope = ProtocolEnvelope.Create(type, payload, seq);
            await SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
            return envelope;
        }

        public async Task DisconnectAsync(string reason = "Client closed session.")
        {
            TcpClient? tcp;
            SslStream? ssl;
            CancellationTokenSource? cts;
            EndPoint? remoteEndPoint = null;

            lock (_stateLock)
            {
                tcp = _tcpClient;
                ssl = _sslStream;
                cts = _readCts;

                _tcpClient = null;
                _sslStream = null;
                _readCts = null;
            }

            if (tcp == null && ssl == null && cts == null)
            {
                // Already disconnected
                return;
            }

            try
            {
                remoteEndPoint = tcp?.Client?.RemoteEndPoint;
            }
            catch
            {
                // Ignore if socket already closed or disposed
            }

            if (cts != null)
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }

            if (ssl != null)
            {
                try { await ssl.DisposeAsync().ConfigureAwait(false); } catch { }
            }

            if (tcp != null)
            {
                try { tcp.Dispose(); } catch { }
            }

            Disconnected?.Invoke(this, new PeerConnectionEventArgs(remoteEndPoint, reason));
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync("Disposing client.").ConfigureAwait(false);
            _sendLock.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
