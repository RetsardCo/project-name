namespace ProjectName.Shared.Tv
{
    /// <summary>
    /// Standard Android key codes used by the Android TV Remote Service protocol.
    /// References official Android KeyEvent definitions for Android TV 11+ (API 30+).
    /// </summary>
    public enum TvKeyCode
    {
        Unknown = 0,
        Home = 3,
        Back = 4,
        DpadUp = 19,
        DpadDown = 20,
        DpadLeft = 21,
        DpadRight = 22,
        DpadCenter = 23,
        VolumeUp = 24,
        VolumeDown = 25,
        Power = 26,
        MediaPlayPause = 85,
        MediaStop = 86,
        MediaNext = 87,
        MediaPrevious = 88,
        Mute = 164
    }

    /// <summary>
    /// Key action type for discrete remote button presses.
    /// </summary>
    public enum TvKeyAction
    {
        Down = 0,
        Up = 1,
        Click = 2 // Down followed immediately by Up
    }
}
