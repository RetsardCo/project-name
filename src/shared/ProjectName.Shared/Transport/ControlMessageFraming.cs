using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProjectName.Shared.Protocol;

namespace ProjectName.Shared.Transport
{
    /// <summary>
    /// Implements deterministic stream framing for the TLS control channel.
    /// Each message is preceded by a 4-byte big-endian payload length header.
    /// Strictly enforces the 64 KB maximum envelope size (65,536 bytes) per PROTOCOL_SPEC section 2.
    /// </summary>
    public static class ControlMessageFraming
    {
        public const int MaxEnvelopeSizeBytes = ProtocolEnvelope.MaxMessageSizeBytes; // 64 KB
        public const int HeaderSizeBytes = 4;

        /// <summary>
        /// Asynchronously writes a framed message to the provided stream.
        /// </summary>
        /// <param name="stream">The output stream (e.g. SslStream or NetworkStream).</param>
        /// <param name="rawJson">The raw JSON string to transmit.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static async Task WriteFrameAsync(Stream stream, string rawJson, CancellationToken cancellationToken = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (rawJson == null) throw new ArgumentNullException(nameof(rawJson));

            byte[] payloadBytes = Encoding.UTF8.GetBytes(rawJson);
            if (payloadBytes.Length > MaxEnvelopeSizeBytes)
            {
                throw new InvalidOperationException(
                    $"Payload size {payloadBytes.Length} exceeds maximum allowed boundary of {MaxEnvelopeSizeBytes} bytes.");
            }

            // Write 4-byte big-endian length prefix
            byte[] header = new byte[HeaderSizeBytes];
            BinaryPrimitives.WriteInt32BigEndian(header, payloadBytes.Length);

            await stream.WriteAsync(header.AsMemory(0, HeaderSizeBytes), cancellationToken).ConfigureAwait(false);
            if (payloadBytes.Length > 0)
            {
                await stream.WriteAsync(payloadBytes.AsMemory(0, payloadBytes.Length), cancellationToken).ConfigureAwait(false);
            }
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously reads a framed message from the provided stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The raw UTF-8 JSON string payload, or null if stream reaches EOF cleanly before a frame.</returns>
        public static async Task<string?> ReadFrameAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            byte[] header = new byte[HeaderSizeBytes];
            int headerBytesRead = await ReadExactAsync(stream, header, 0, HeaderSizeBytes, cancellationToken).ConfigureAwait(false);

            if (headerBytesRead == 0)
            {
                // Clean end of stream
                return null;
            }

            if (headerBytesRead < HeaderSizeBytes)
            {
                throw new EndOfStreamException("Incomplete frame header received before connection termination.");
            }

            int payloadLength = BinaryPrimitives.ReadInt32BigEndian(header);
            if (payloadLength < 0 || payloadLength > MaxEnvelopeSizeBytes)
            {
                throw new InvalidDataException(
                    $"Violated protocol framing: payload length {payloadLength} outside valid range [0, {MaxEnvelopeSizeBytes}].");
            }

            if (payloadLength == 0)
            {
                return string.Empty;
            }

            byte[] payloadBuffer = new byte[payloadLength];
            int payloadBytesRead = await ReadExactAsync(stream, payloadBuffer, 0, payloadLength, cancellationToken).ConfigureAwait(false);

            if (payloadBytesRead < payloadLength)
            {
                throw new EndOfStreamException(
                    $"Incomplete frame payload: expected {payloadLength} bytes, but received only {payloadBytesRead} bytes.");
            }

            return Encoding.UTF8.GetString(payloadBuffer, 0, payloadLength);
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> bytes into the buffer, handling partial reads across TCP packets.
        /// </summary>
        private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                int read = await stream.ReadAsync(
                    buffer.AsMemory(offset + totalBytesRead, count - totalBytesRead),
                    cancellationToken).ConfigureAwait(false);

                if (read == 0)
                {
                    break;
                }
                totalBytesRead += read;
            }
            return totalBytesRead;
        }
    }
}
