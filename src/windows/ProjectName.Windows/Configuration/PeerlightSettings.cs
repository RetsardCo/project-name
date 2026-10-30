using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectName.Windows.Configuration
{
    /// <summary>
    /// User and runtime configuration settings for Peerlight Windows Desktop.
    /// Manages persistent preferences across sessions, including appearance,
    /// discovery beacons, network ports, input behavior, and video streaming parameters.
    /// </summary>
    public class PeerlightSettings
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // 1. Appearance
        [JsonPropertyName("theme_mode")]
        public string ThemeMode { get; set; } = "Dark"; // "Dark" or "Light"

        // 2. Device Discovery
        [JsonPropertyName("enable_mdns")]
        public bool EnableMdns { get; set; } = true;

        [JsonPropertyName("discovery_service_type")]
        public string DiscoveryServiceType { get; set; } = "_peerlight._tcp.local.";

        // 3. Network Configuration
        [JsonPropertyName("control_port")]
        public int ControlPort { get; set; } = 8443;

        [JsonPropertyName("tv_port")]
        public int TvPort { get; set; } = 6466;

        [JsonPropertyName("default_tv_host")]
        public string DefaultTvHost { get; set; } = "192.168.254.104";

        // 4. Pairing & Security
        [JsonPropertyName("pin_expiry_seconds")]
        public int PinExpirySeconds { get; set; } = 60;

        [JsonPropertyName("require_mutual_pairing")]
        public bool RequireMutualPairing { get; set; } = true;

        [JsonPropertyName("zero_recording_enforced")]
        public bool ZeroRecordingEnforced { get; set; } = true;

        // 5. Input Behavior
        [JsonPropertyName("pointer_sensitivity")]
        public string PointerSensitivity { get; set; } = "Normal"; // "Slow", "Normal", "Fast"

        [JsonPropertyName("invert_scroll_wheel")]
        public bool InvertScrollWheel { get; set; } = false;

        [JsonPropertyName("release_inputs_on_focus_loss")]
        public bool ReleaseInputsOnFocusLoss { get; set; } = true;

        // 6. Display & Streaming Quality
        [JsonPropertyName("target_fps")]
        public int TargetFps { get; set; } = 30;

        [JsonPropertyName("drop_stale_frames")]
        public bool DropStaleFrames { get; set; } = true;

        [JsonPropertyName("max_frame_buffer_capacity")]
        public int MaxFrameBufferCapacity { get; set; } = 2;

        // 7. Notifications
        [JsonPropertyName("show_desktop_notifications")]
        public bool ShowDesktopNotifications { get; set; } = true;

        // 8. Startup Behavior
        [JsonPropertyName("start_minimized")]
        public bool StartMinimized { get; set; } = false;

        // 9. Diagnostics
        [JsonPropertyName("diagnostics_logging")]
        public bool DiagnosticsLogging { get; set; } = true;

        public static string GetSettingsFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "Peerlight");
            if (!Directory.Exists(folder))
            {
                try { Directory.CreateDirectory(folder); } catch { }
            }
            return Path.Combine(folder, "peerlight-settings.json");
        }

        public static PeerlightSettings Load()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<PeerlightSettings>(json, JsonOptions);
                    if (settings != null) return settings;
                }
            }
            catch { }

            return new PeerlightSettings();
        }

        public void Save()
        {
            try
            {
                string path = GetSettingsFilePath();
                string json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }
}
