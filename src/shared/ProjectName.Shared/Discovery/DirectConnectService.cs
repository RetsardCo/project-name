using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Protocol;
using ProjectName.Shared.Protocol.Payloads;
using ProjectName.Shared.Transport;

namespace ProjectName.Shared.Discovery
{
    /// <summary>
    /// Provides direct IP/hostname fallback connection for network environments where mDNS or multicast
    /// is blocked by router AP client isolation. Probes endpoint and securely retrieves device identity.
    /// </summary>
    public static class DirectConnectService
    {
        /// <summary>
        /// Probes an endpoint at the specified host and port using the TLS control channel.
        /// </summary>
        public static async Task<DiscoveredDevice> ProbeEndpointAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(host)) throw new ArgumentNullException(nameof(host));
            if (port <= 0 || port > 65535) throw new ArgumentOutOfRangeException(nameof(port));

            var client = new TlsControlClient();
            var tcs = new TaskCompletionSource<DiscoveryPayload>();

            client.MessageReceived += (_, e) =>
            {
                if (e.Envelope.Type == MessageTypes.DiscoveryAnnounce)
                {
                    try
                    {
                        var payload = e.Envelope.Payload.Deserialize<DiscoveryPayload>();
                        if (payload != null)
                        {
                            tcs.TrySetResult(payload);
                        }
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }
            };

            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

            try
            {
                // Send discovery query to trigger announcement
                await client.SendMessageAsync(MessageTypes.DiscoveryQuery, new { service = MdnsDiscoveryBeacon.ServiceType }, cancellationToken).ConfigureAwait(false);

                // Wait for announcement response with timeout
                using var ctsTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ctsTimeout.Token);

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.Infinite, linkedCts.Token)).ConfigureAwait(false);
                if (completedTask == tcs.Task)
                {
                    var payload = await tcs.Task.ConfigureAwait(false);
                    return new DiscoveredDevice(
                        DeviceId: payload.DeviceId,
                        DisplayName: payload.DisplayName,
                        Platform: payload.Platform,
                        HostOrIp: host,
                        ControlPort: port,
                        Capabilities: payload.Capabilities ?? new List<string>(),
                        LastSeenUtc: DateTime.UtcNow
                    );
                }
                else
                {
                    throw new TimeoutException($"Endpoint at {host}:{port} did not respond to discovery query within timeout.");
                }
            }
            finally
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
