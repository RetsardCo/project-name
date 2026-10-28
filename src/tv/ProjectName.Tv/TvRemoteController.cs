using System;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Tv;

namespace ProjectName.Tv
{
    /// <summary>
    /// High-level Google TV / Android TV remote controller.
    /// Manages connections to verified target devices (e.g. Xiaomi Mi TV at 192.168.254.104)
    /// and provides intuitive directional D-Pad, selection, navigation, and volume controls.
    /// </summary>
    public class TvRemoteController : IAsyncDisposable
    {
        private readonly GoogleTvRemoteClient _client;

        public bool IsConnected => _client.IsConnected;
        public string? TargetHost => _client.ConnectedHost;
        public int TargetPort => _client.ConnectedPort;

        public event EventHandler<string>? Disconnected
        {
            add => _client.Disconnected += value;
            remove => _client.Disconnected -= value;
        }

        public TvRemoteController(GoogleTvRemoteClient? client = null)
        {
            _client = client ?? new GoogleTvRemoteClient();
        }

        /// <summary>
        /// Connects to a target TV endpoint.
        /// </summary>
        public Task ConnectAsync(string host, int port = GoogleTvRemoteClient.DefaultControlPort, CancellationToken cancellationToken = default)
        {
            return _client.ConnectAsync(host, port, cancellationToken);
        }

        /// <summary>
        /// Navigates D-Pad Up.
        /// </summary>
        public Task DpadUpAsync(CancellationToken cancellationToken = default) =>
            _client.SendKeyAsync(TvKeyCode.DpadUp, TvKeyAction.Click, cancellationToken);

        /// <summary>
        /// Navigates D-Pad Down.
        /// </summary>
        public Task DpadDownAsync(CancellationToken cancellationToken = default) =>
            _client.SendKeyAsync(TvKeyCode.DpadDown, TvKeyAction.Click, cancellationToken);

        /// <summary>
        /// Navigates D-Pad Left.
        /// </summary>
        public Task DpadLeftAsync(CancellationToken cancellationToken = default) =>
            _client.SendKeyAsync(TvKeyCode.DpadLeft, TvKeyAction.Click, cancellationToken);

        /// <summary>
        /// Navigates D-Pad Right.
        /// </summary>
        public Task DpadRightAsync(CancellationToken cancellationToken = default) =>
            _client.SendKeyAsync(TvKeyCode.DpadRight, TvKeyAction.Click, cancellationToken);

        /// <summary>
        /// Confirms / Selects (D-Pad Center / OK).
        /// </summary>
        public Task SelectAsync(CancellationToken cancellationToken = default) =>
            _client.SendKeyAsync(TvKeyCode.DpadCenter, TvKeyAction.Click, cancellationToken);

        /// <summary>
        /// Returns to previous screen (Back).
        /// </summary>
        public Task BackAsync(CancellationToken cancellationToken = default) =>
            _client.SendKeyAsync(TvKeyCode.Back, TvKeyAction.Click, cancellationToken);

        /// <summary>
        /// Returns to Android TV Home screen.
        /// </summary>
        public Task HomeAsync(CancellationToken cancellationToken = default) =>
            _client.SendKeyAsync(TvKeyCode.Home, TvKeyAction.Click, cancellationToken);

        /// <summary>
        /// Adjusts TV volume up.
        /// </summary>
        public Task VolumeUpAsync(CancellationToken cancellationToken = default) =>
            _client.SendKeyAsync(TvKeyCode.VolumeUp, TvKeyAction.Click, cancellationToken);

        /// <summary>
        /// Adjusts TV volume down.
        /// </summary>
        public Task VolumeDownAsync(CancellationToken cancellationToken = default) =>
            _client.SendKeyAsync(TvKeyCode.VolumeDown, TvKeyAction.Click, cancellationToken);

        /// <summary>
        /// Toggles Play/Pause media playback.
        /// </summary>
        public Task PlayPauseAsync(CancellationToken cancellationToken = default) =>
            _client.SendKeyAsync(TvKeyCode.MediaPlayPause, TvKeyAction.Click, cancellationToken);

        /// <summary>
        /// Sends an arbitrary mapped protocol command (e.g. 'dpad_up', 'volume_up', 'back').
        /// </summary>
        public Task SendCommandAsync(string command, CancellationToken cancellationToken = default) =>
            _client.SendCommandAsync(command, cancellationToken);

        /// <summary>
        /// Checks network reachability of a TV endpoint.
        /// </summary>
        public static Task<bool> PingTvEndpointAsync(string host, int port = GoogleTvRemoteClient.DefaultControlPort) =>
            GoogleTvRemoteClient.CheckConnectivityAsync(host, port);

        public Task DisconnectAsync(string reason = "Controller requested disconnect.") =>
            _client.DisconnectAsync(reason);

        public ValueTask DisposeAsync() =>
            _client.DisposeAsync();
    }
}
