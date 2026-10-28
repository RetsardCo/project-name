using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Protocol;

namespace ProjectName.Shared.Transport
{
    /// <summary>
    /// Event arguments for incoming protocol messages.
    /// </summary>
    public class ProtocolMessageEventArgs : EventArgs
    {
        public ProtocolEnvelope Envelope { get; }
        public string RawJson { get; }

        public ProtocolMessageEventArgs(ProtocolEnvelope envelope, string rawJson)
        {
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
            RawJson = rawJson ?? throw new ArgumentNullException(nameof(rawJson));
        }
    }

    /// <summary>
    /// Event arguments for peer connection lifecycle events.
    /// </summary>
    public class PeerConnectionEventArgs : EventArgs
    {
        public EndPoint? RemoteEndPoint { get; }
        public string? Reason { get; }

        public PeerConnectionEventArgs(EndPoint? remoteEndPoint, string? reason = null)
        {
            RemoteEndPoint = remoteEndPoint;
            Reason = reason;
        }
    }

    /// <summary>
    /// Contract for the TLS-encrypted control channel server.
    /// </summary>
    public interface ITlsControlServer : IAsyncDisposable
    {
        int Port { get; }
        bool IsRunning { get; }
        bool HasActiveClient { get; }
        SequenceTracker InboundSequenceTracker { get; }

        event EventHandler<ProtocolMessageEventArgs>? MessageReceived;
        event EventHandler<PeerConnectionEventArgs>? ClientConnected;
        event EventHandler<PeerConnectionEventArgs>? ClientDisconnected;

        Task StartAsync(CancellationToken cancellationToken = default);
        Task StopAsync();
        Task SendEnvelopeAsync(ProtocolEnvelope envelope, CancellationToken cancellationToken = default);
        Task DisconnectCurrentClientAsync(string reason);
    }
}
