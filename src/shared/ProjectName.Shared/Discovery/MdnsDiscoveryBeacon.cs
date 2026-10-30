using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Protocol.Payloads;

namespace ProjectName.Shared.Discovery
{
    /// <summary>
    /// Implements mDNS / DNS-SD discovery beacon for service type '_projectname._tcp'.
    /// Operates over UDP port 5353 using standard multicast (224.0.0.251).
    /// Discovers active endpoints on the LAN without external servers or cloud dependencies.
    /// </summary>
    public class MdnsDiscoveryBeacon : IAsyncDisposable
    {
        public const string ServiceType = "_peerlight._tcp.local.";
        public const int MdnsPort = 5353;
        private static readonly IPAddress MulticastAddressV4 = IPAddress.Parse("224.0.0.251");

        private readonly DiscoveryPayload _localPayload;
        private readonly ConcurrentDictionary<string, DiscoveredDevice> _discoveredDevices = new(StringComparer.OrdinalIgnoreCase);
        private UdpClient? _udpClient;
        private CancellationTokenSource? _cts;
        private Task? _listenerTask;
        private readonly object _lock = new();

        public IReadOnlyCollection<DiscoveredDevice> DiscoveredDevices =>
            _discoveredDevices.Values as IReadOnlyCollection<DiscoveredDevice> ?? new List<DiscoveredDevice>(_discoveredDevices.Values);

        public event EventHandler<DiscoveredDevice>? DeviceDiscovered;

        public MdnsDiscoveryBeacon(DiscoveryPayload localPayload)
        {
            _localPayload = localPayload ?? throw new ArgumentNullException(nameof(localPayload));
        }

        /// <summary>
        /// Starts the mDNS listener and announces this endpoint to the local network.
        /// </summary>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_udpClient != null) return Task.CompletedTask;

                _cts = new CancellationTokenSource();

                try
                {
                    // Configure UDP client with multicast reuse
                    var client = new UdpClient();
                    client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    
                    // Attempt to bind to standard mDNS port 5353 with socket reuse, falling back to ephemeral port
                    try
                    {
                        var mDnsEndpoint = new IPEndPoint(IPAddress.Any, MdnsPort);
                        client.Client.Bind(mDnsEndpoint);
                    }
                    catch
                    {
                        var fallbackEndpoint = new IPEndPoint(IPAddress.Any, 0);
                        client.Client.Bind(fallbackEndpoint);
                    }

                    try
                    {
                        client.JoinMulticastGroup(MulticastAddressV4);
                    }
                    catch
                    {
                        // Some network interfaces or VPNs may not support multicast joining; client will still operate in unicast query mode
                    }

                    _udpClient = client;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to initialize discovery socket: {ex.Message}", ex);
                }
            }

            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token), CancellationToken.None);

            // Announce presence immediately
            return AnnouncePresenceAsync(cancellationToken);
        }

        /// <summary>
        /// Multicasts an announcement packet to advertise this device to other peers on the LAN.
        /// </summary>
        public async Task AnnouncePresenceAsync(CancellationToken cancellationToken = default)
        {
            UdpClient? client;
            lock (_lock)
            {
                client = _udpClient;
            }

            if (client == null) return;

            var envelope = ProtocolEnvelope.Create(MessageTypes.DiscoveryAnnounce, _localPayload, 0);
            string json = JsonSerializer.Serialize(envelope);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            var multicastTarget = new IPEndPoint(MulticastAddressV4, MdnsPort);
            try
            {
                await client.SendAsync(bytes, bytes.Length, multicastTarget).ConfigureAwait(false);
            }
            catch
            {
                // Ignore transient transmission failures (e.g. interface temporarily down)
            }
        }

        /// <summary>
        /// Broadcasts a discovery query packet requesting all active endpoints on the LAN to announce themselves.
        /// </summary>
        public async Task QueryPeersAsync(CancellationToken cancellationToken = default)
        {
            UdpClient? client;
            lock (_lock)
            {
                client = _udpClient;
            }

            if (client == null) return;

            var envelope = ProtocolEnvelope.Create(MessageTypes.DiscoveryQuery, new { service = ServiceType }, 0);
            string json = JsonSerializer.Serialize(envelope);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            var multicastTarget = new IPEndPoint(MulticastAddressV4, MdnsPort);
            try
            {
                await client.SendAsync(bytes, bytes.Length, multicastTarget).ConfigureAwait(false);
            }
            catch
            {
                // Transient network send error
            }
        }

        private async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            UdpClient? client;
            lock (_lock)
            {
                client = _udpClient;
            }

            if (client == null) return;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                    if (result.Buffer.Length == 0 || result.Buffer.Length > ProtocolEnvelope.MaxMessageSizeBytes)
                    {
                        continue;
                    }

                    string json = Encoding.UTF8.GetString(result.Buffer);
                    var validation = ProtocolEnvelope.Validate(json, 0);
                    if (!validation.IsValid || validation.Envelope == null)
                    {
                        continue;
                    }

                    var env = validation.Envelope;
                    if (env.Type == MessageTypes.DiscoveryQuery)
                    {
                        // Respond to query by multicasting our announcement
                        _ = AnnouncePresenceAsync(CancellationToken.None);
                    }
                    else if (env.Type == MessageTypes.DiscoveryAnnounce)
                    {
                        ProcessAnnouncement(env, result.RemoteEndPoint.Address.ToString());
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    if (cancellationToken.IsCancellationRequested) break;
                }
            }
        }

        private void ProcessAnnouncement(ProtocolEnvelope env, string senderIp)
        {
            try
            {
                var payload = env.Payload.Deserialize<DiscoveryPayload>();
                if (payload == null) return;

                // Ignore announcements originating from self
                if (string.Equals(payload.DeviceId, _localPayload.DeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var device = new DiscoveredDevice(
                    DeviceId: payload.DeviceId,
                    DisplayName: payload.DisplayName,
                    Platform: payload.Platform,
                    HostOrIp: senderIp,
                    ControlPort: payload.ControlPort,
                    Capabilities: payload.Capabilities ?? new List<string>(),
                    LastSeenUtc: DateTime.UtcNow
                );

                _discoveredDevices[device.DeviceId] = device;
                DeviceDiscovered?.Invoke(this, device);
            }
            catch
            {
                // Silently ignore corrupted discovery payloads
            }
        }

        public Task StopAsync()
        {
            lock (_lock)
            {
                _cts?.Cancel();
                _udpClient?.Dispose();
                _udpClient = null;
            }

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _cts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
