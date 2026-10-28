using System;
using ProjectName.Shared.Tv;
using ProjectName.Tv;
using Xunit;

namespace ProjectName.Tv.Tests
{
    public class TvRemoteControllerTests
    {
        [Fact]
        public void TvCommandMapper_MapsStandardDpadAndMediaCommandsCorrectly()
        {
            Assert.Equal(TvKeyCode.DpadUp, TvCommandMapper.MapCommand("dpad_up"));
            Assert.Equal(TvKeyCode.DpadDown, TvCommandMapper.MapCommand("dpad_down"));
            Assert.Equal(TvKeyCode.DpadLeft, TvCommandMapper.MapCommand("dpad_left"));
            Assert.Equal(TvKeyCode.DpadRight, TvCommandMapper.MapCommand("dpad_right"));
            Assert.Equal(TvKeyCode.DpadCenter, TvCommandMapper.MapCommand("dpad_center"));
            Assert.Equal(TvKeyCode.DpadCenter, TvCommandMapper.MapCommand("select"));
            Assert.Equal(TvKeyCode.Back, TvCommandMapper.MapCommand("back"));
            Assert.Equal(TvKeyCode.Home, TvCommandMapper.MapCommand("home"));
            Assert.Equal(TvKeyCode.VolumeUp, TvCommandMapper.MapCommand("volume_up"));
            Assert.Equal(TvKeyCode.VolumeDown, TvCommandMapper.MapCommand("volume_down"));
            Assert.Equal(TvKeyCode.MediaPlayPause, TvCommandMapper.MapCommand("play_pause"));
        }

        [Fact]
        public void TvCommandMapper_ThrowsArgumentException_OnUnknownCommand()
        {
            Assert.Throws<ArgumentException>(() => TvCommandMapper.MapCommand("invalid_nonexistent_command"));
        }

        [Fact]
        public void GoogleTvRemoteClient_EncodeRemoteKeyPacket_ProducesExpectedWireFormat()
        {
            byte[] packet = GoogleTvRemoteClient.EncodeRemoteKeyPacket(TvKeyCode.DpadUp, TvKeyAction.Down);
            Assert.NotNull(packet);
            Assert.True(packet.Length > 0);

            // Verify outer tag 0x52 is present in the wire packet
            Assert.Contains((byte)0x52, packet);
            // Verify keycode 19 (0x13) for DpadUp is present
            Assert.Contains((byte)19, packet);
        }
    }
}
