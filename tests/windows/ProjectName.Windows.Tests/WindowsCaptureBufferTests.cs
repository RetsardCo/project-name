using System;
using ProjectName.Windows.Capture;
using ProjectName.Windows.Input;
using Xunit;

namespace ProjectName.Windows.Tests
{
    public class WindowsCaptureBufferTests
    {
        [Fact]
        public void WindowsCaptureBuffer_EnforcesMaxCapacity_AndDiscardsOldestFrames()
        {
            var buffer = new WindowsCaptureBuffer(maxCapacity: 2);
            byte[] frame1 = new byte[] { 1, 2, 3 };
            byte[] frame2 = new byte[] { 4, 5, 6 };
            byte[] frame3 = new byte[] { 7, 8, 9 };

            buffer.PushFrame(frame1, 1920, 1080, 100);
            buffer.PushFrame(frame2, 1920, 1080, 133);
            Assert.Equal(2, buffer.QueuedFramesCount);
            Assert.Equal(0, buffer.DroppedFramesCount);

            // Adding third frame beyond capacity of 2 must drop frame1
            buffer.PushFrame(frame3, 1920, 1080, 166);
            Assert.Equal(2, buffer.QueuedFramesCount);
            Assert.Equal(1, buffer.DroppedFramesCount);

            // Dequeue should yield frame2 first, then frame3
            bool dequeued1 = buffer.TryGetNextFrame(out var next1);
            Assert.True(dequeued1);
            Assert.NotNull(next1);
            Assert.Equal(frame2, next1!.Data);

            bool dequeued2 = buffer.TryGetNextFrame(out var next2);
            Assert.True(dequeued2);
            Assert.NotNull(next2);
            Assert.Equal(frame3, next2!.Data);

            // Queue should now be empty
            Assert.False(buffer.TryGetNextFrame(out _));
            Assert.Equal(0, buffer.QueuedFramesCount);
        }

        [Fact]
        public void WindowsCaptureBuffer_Clear_EmptiesAllFramesImmediately()
        {
            var buffer = new WindowsCaptureBuffer(maxCapacity: 5);
            buffer.PushFrame(new byte[] { 1 }, 100, 100, 1);
            buffer.PushFrame(new byte[] { 2 }, 100, 100, 2);
            Assert.Equal(2, buffer.QueuedFramesCount);

            buffer.Clear();
            Assert.Equal(0, buffer.QueuedFramesCount);
            Assert.False(buffer.TryGetNextFrame(out _));
        }

        [Fact]
        public void WindowsInputInjector_HeldInputsTracking_AndSafetyRelease()
        {
            var injector = new WindowsInputInjector();

            injector.InjectPointerButton("left", "down");
            injector.InjectKey("Ctrl", "down");
            injector.InjectKey("Shift", "down");

            Assert.Contains("left", injector.HeldButtons);
            Assert.Contains("Ctrl", injector.HeldKeys);
            Assert.Contains("Shift", injector.HeldKeys);

            // When session ends or disconnects, all held inputs must be cleared safely
            injector.ReleaseAllHeldInputs();

            Assert.Empty(injector.HeldButtons);
            Assert.Empty(injector.HeldKeys);
        }
    }
}
