using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Protocol;

namespace ProjectName.Shared.Transport
{
    /// <summary>
    /// Contract for the TLS control channel client.
    /// </summary>
    public interface ITlsControlClient : IAsyncDisposable
    {
        bool IsConnected { get; }
        SequenceTracker InboundSequenceTracker { get; }

        event EventHandler<ProtocolMessageEventArgs>? MessageReceived;
        event EventHandler<PeerConnectionEventArgs>? Disconnected;

        Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default);
        Task SendEnvelopeAsync(ProtocolEnvelope envelope, CancellationToken cancellationToken = default);
        Task<ProtocolEnvelope> SendMessageAsync(string type, object? payload, CancellationToken cancellationToken = default);
        Task DisconnectAsync(string reason = "Client closed session.");
    }
}
