using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Transport;

namespace ProjectName.Shared.Tv
{
    /// <summary>
    /// Client implementation for the Android TV Remote Service protocol (TLS ports 6466/6467).
    /// Provides remote D-Pad, Select, Back, Home, Volume, and Media navigation for Google TV / Android TV
    /// endpoints (e.g., Xiaomi Mi TV at 192.168.254.104) without runtime ADB.
    /// </summary>
    public class GoogleTvRemoteClient : IAsyncDisposable
    {
        public const int DefaultControlPort = 6466;
        public const int DefaultPairingPort = 6467;

        private TcpClient? _tcpClient;
        private SslStream? _sslStream;
        private readonly X509Certificate2 _clientCertificate;
        private readonly RemoteCertificateValidationCallback _serverValidator;
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly object _stateLock = new();

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

        public string? ConnectedHost { get; private set; }
        public int ConnectedPort { get; private set; }

        public event EventHandler<string>? Disconnected;

        public GoogleTvRemoteClient(
            X509Certificate2? clientCertificate = null,
            RemoteCertificateValidationCallback? serverValidator = null)
        {
            _clientCertificate = clientCertificate ?? EphemeralCertificateGenerator.CreateDeviceCertificate("AndroidTvRemoteClient");
            _serverValidator = serverValidator ?? ((_, _, _, _) => true);
        }

        /// <summary>
        /// Asynchronously connects to the Android TV Remote Service over TLS.
        /// </summary>
        public async Task ConnectAsync(string host, int port = DefaultControlPort, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));

            var tcp = new TcpClient();
            try
            {
                await tcp.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

                var netStream = tcp.GetStream();
                var sslStream = new SslStream(netStream, false, _serverValidator);

                var clientCertificates = new X509CertificateCollection { _clientCertificate };
                await sslStream.AuthenticateAsClientAsync(
                    new SslClientAuthenticationOptions
                    {
                        TargetHost = host,
                        ClientCertificates = clientCertificates,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                    },
                    cancellationToken).ConfigureAwait(false);

                lock (_stateLock)
                {
                    _tcpClient = tcp;
                    _sslStream = sslStream;
                    ConnectedHost = host;
                    ConnectedPort = port;
                }
            }
            catch
            {
                tcp.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Sends a discrete remote key event (down and up).
        /// </summary>
        public async Task SendKeyAsync(TvKeyCode keyCode, TvKeyAction action = TvKeyAction.Click, CancellationToken cancellationToken = default)
        {
            if (keyCode == TvKeyCode.Unknown)
            {
                throw new ArgumentException("Cannot send unknown TV key code.", nameof(keyCode));
            }

            if (action == TvKeyAction.Click)
            {
                // Send DOWN followed by UP with brief hold interval
                await SendKeyInternalAsync(keyCode, TvKeyAction.Down, cancellationToken).ConfigureAwait(false);
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                await SendKeyInternalAsync(keyCode, TvKeyAction.Up, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await SendKeyInternalAsync(keyCode, action, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Maps a protocol command string (e.g. 'dpad_up', 'select', 'back', 'home') and sends it.
        /// </summary>
        public Task SendCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            var keyCode = TvCommandMapper.MapCommand(command);
            return SendKeyAsync(keyCode, TvKeyAction.Click, cancellationToken);
        }

        private async Task SendKeyInternalAsync(TvKeyCode keyCode, TvKeyAction action, CancellationToken cancellationToken)
        {
            SslStream? stream;
            lock (_stateLock)
            {
                stream = _sslStream;
            }

            if (stream == null)
            {
                throw new InvalidOperationException("Cannot send TV command: client is not connected.");
            }

            // Android TV Remote v2 key packet encoding:
            // Wire format: [Length: 1 byte][Tag: 0x52][Sub-length][KeyCode varint][Action varint]
            byte[] packet = EncodeRemoteKeyPacket(keyCode, action);

            await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await stream.WriteAsync(packet.AsMemory(0, packet.Length), cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// Encodes a remote key event into the packet format expected by Android TV Remote Service v2.
        /// </summary>
        public static byte[] EncodeRemoteKeyPacket(TvKeyCode keyCode, TvKeyAction action)
        {
            int code = (int)keyCode;
            int direction = action == TvKeyAction.Down ? 0 : 1;

            // Simple protobuf-compatible key payload:
            // field 1 (code): tag = 1<<3 | 0 = 0x08, val = code
            // field 2 (direction): tag = 2<<3 | 0 = 0x10, val = direction
            using var ms = new MemoryStream();
            ms.WriteByte(0x08);
            WriteVarint(ms, code);
            ms.WriteByte(0x10);
            WriteVarint(ms, direction);

            byte[] body = ms.ToArray();

            // Outer wrapper packet: [outer length varint][tag 0x52 (RemoteKey)][body length varint][body]
            using var outer = new MemoryStream();
            outer.WriteByte(0x52); // Field tag for remote key event
            WriteVarint(outer, body.Length);
            outer.Write(body, 0, body.Length);

            byte[] outerBody = outer.ToArray();

            using var framed = new MemoryStream();
            WriteVarint(framed, outerBody.Length);
            framed.Write(outerBody, 0, outerBody.Length);

            return framed.ToArray();
        }

        private static void WriteVarint(Stream stream, int value)
        {
            while ((value & ~0x7F) != 0)
            {
                stream.WriteByte((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            stream.WriteByte((byte)(value & 0x7F));
        }

        /// <summary>
        /// Diagnostic connectivity check: checks if a TCP port is open on the target TV host.
        /// </summary>
        public static async Task<bool> CheckConnectivityAsync(string host, int port = DefaultControlPort, int timeoutMs = 2000)
        {
            if (string.IsNullOrWhiteSpace(host)) return false;

            try
            {
                using var tcp = new TcpClient();
                using var cts = new CancellationTokenSource(timeoutMs);
                await tcp.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
                return tcp.Connected;
            }
            catch
            {
                return false;
            }
        }

        public async Task DisconnectAsync(string reason = "Client disconnected.")
        {
            TcpClient? tcp;
            SslStream? ssl;

            lock (_stateLock)
            {
                tcp = _tcpClient;
                ssl = _sslStream;
                _tcpClient = null;
                _sslStream = null;
                ConnectedHost = null;
            }

            if (ssl != null)
            {
                try { await ssl.DisposeAsync().ConfigureAwait(false); } catch { }
            }
            if (tcp != null)
            {
                try { tcp.Dispose(); } catch { }
            }

            Disconnected?.Invoke(this, reason);
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync("Disposing client.").ConfigureAwait(false);
            _sendLock.Dispose();
            _clientCertificate.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
