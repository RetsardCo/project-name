using System;
using System.Collections.Generic;
using ProjectName.Shared.Protocol.Payloads;

namespace ProjectName.Shared.Media
{
    public record VideoStreamProfile(
        string Codec,
        int Width,
        int Height,
        int Fps,
        int TargetBitrateKbps
    );

    /// <summary>
    /// Negotiates video streaming profiles and codecs between endpoints.
    /// Selects optimal resolution and bitrate targets to achieve sub-100 ms interaction latency.
    /// </summary>
    public static class MediaNegotiator
    {
        public static readonly VideoStreamProfile Baseline720p = new(
            Codec: "H264",
            Width: 1280,
            Height: 720,
            Fps: 30,
            TargetBitrateKbps: 3000
        );

        public static readonly VideoStreamProfile High1080p = new(
            Codec: "H264",
            Width: 1920,
            Height: 1080,
            Fps: 30,
            TargetBitrateKbps: 6000
        );

        public static VideoStreamProfile Negotiate(ResolutionSpec requested, IReadOnlyList<string>? supportedCodecs = null)
        {
            string chosenCodec = "H264";
            if (supportedCodecs != null)
            {
                foreach (var codec in supportedCodecs)
                {
                    if (string.Equals(codec, "H264", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(codec, "VP8", StringComparison.OrdinalIgnoreCase))
                    {
                        chosenCodec = codec.ToUpperInvariant();
                        break;
                    }
                }
            }

            int width = requested.Width > 0 ? Math.Clamp(requested.Width, 640, 3840) : 1280;
            int height = requested.Height > 0 ? Math.Clamp(requested.Height, 360, 2160) : 720;
            int fps = requested.Fps > 0 ? Math.Clamp(requested.Fps, 15, 60) : 30;

            int bitrate = (width * height >= 1920 * 1080) ? 6000 : 3000;
            return new VideoStreamProfile(chosenCodec, width, height, fps, bitrate);
        }
    }
}
