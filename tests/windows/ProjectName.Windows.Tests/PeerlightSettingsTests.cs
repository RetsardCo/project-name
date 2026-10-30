using System;
using System.Drawing;
using System.Text.Json;
using ProjectName.Windows.Configuration;
using ProjectName.Windows.UI;
using Xunit;

namespace ProjectName.Windows.Tests
{
    public class PeerlightSettingsTests
    {
        [Fact]
        public void PeerlightSettings_Defaults_ConformToSecurityAndDesignStandards()
        {
            var settings = new PeerlightSettings();

            Assert.Equal("Dark", settings.ThemeMode);
            Assert.True(settings.EnableMdns);
            Assert.Equal(8443, settings.ControlPort);
            Assert.Equal(6466, settings.TvPort);
            Assert.Equal("192.168.254.104", settings.DefaultTvHost);
            Assert.Equal(60, settings.PinExpirySeconds);
            Assert.True(settings.RequireMutualPairing);
            Assert.True(settings.ZeroRecordingEnforced);
            Assert.Equal(30, settings.TargetFps);
            Assert.True(settings.DropStaleFrames);
            Assert.Equal(2, settings.MaxFrameBufferCapacity);
        }

        [Fact]
        public void PeerlightSettings_SerializationRoundtrip_PreservesAllConfiguredValues()
        {
            var original = new PeerlightSettings
            {
                ThemeMode = "Light",
                EnableMdns = false,
                ControlPort = 9443,
                TvPort = 7466,
                DefaultTvHost = "192.168.1.200",
                PinExpirySeconds = 120,
                RequireMutualPairing = true,
                PointerSensitivity = "Fast",
                InvertScrollWheel = true,
                TargetFps = 60,
                DropStaleFrames = false,
                ShowDesktopNotifications = false,
                StartMinimized = true
            };

            string json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<PeerlightSettings>(json);

            Assert.NotNull(deserialized);
            Assert.Equal("Light", deserialized.ThemeMode);
            Assert.False(deserialized.EnableMdns);
            Assert.Equal(9443, deserialized.ControlPort);
            Assert.Equal(7466, deserialized.TvPort);
            Assert.Equal("192.168.1.200", deserialized.DefaultTvHost);
            Assert.Equal(120, deserialized.PinExpirySeconds);
            Assert.Equal("Fast", deserialized.PointerSensitivity);
            Assert.True(deserialized.InvertScrollWheel);
            Assert.Equal(60, deserialized.TargetFps);
            Assert.False(deserialized.DropStaleFrames);
            Assert.False(deserialized.ShowDesktopNotifications);
            Assert.True(deserialized.StartMinimized);
        }

        [Fact]
        public void PeerlightTheme_IsDarkModeToggle_SwitchesPaletteTokensCorrectly()
        {
            // Dark Mode
            PeerlightTheme.IsDarkMode = true;
            Color darkBg = PeerlightTheme.NavyBg;
            Color darkText = PeerlightTheme.TextPrimary;

            Assert.Equal(Color.FromArgb(7, 20, 38), darkBg);
            Assert.Equal(Color.FromArgb(244, 247, 251), darkText);

            // Light Mode
            PeerlightTheme.IsDarkMode = false;
            Color lightBg = PeerlightTheme.NavyBg;
            Color lightText = PeerlightTheme.TextPrimary;

            Assert.Equal(Color.FromArgb(245, 247, 250), lightBg);
            Assert.Equal(Color.FromArgb(7, 20, 38), lightText);

            // Restore dark default
            PeerlightTheme.IsDarkMode = true;
        }
    }
}
