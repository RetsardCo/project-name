using System;

namespace ProjectName.Shared.Protocol
{
    public enum ScreenOrientation
    {
        Portrait = 0,
        LandscapeRight = 90,
        PortraitUpsideDown = 180,
        LandscapeLeft = 270
    }

    public record PixelPoint(int X, int Y);

    public static class CoordinateMapper
    {
        public static PixelPoint MapNormalizedToPixel(
            double xNorm,
            double yNorm,
            int screenWidth,
            int screenHeight,
            ScreenOrientation orientation = ScreenOrientation.Portrait)
        {
            if (screenWidth <= 0) throw new ArgumentOutOfRangeException(nameof(screenWidth), "Width must be positive.");
            if (screenHeight <= 0) throw new ArgumentOutOfRangeException(nameof(screenHeight), "Height must be positive.");

            // Clamp normalized inputs strictly to [0.0, 1.0]
            double clampedX = Math.Clamp(xNorm, 0.0, 1.0);
            double clampedY = Math.Clamp(yNorm, 0.0, 1.0);

            double mappedX;
            double mappedY;

            switch (orientation)
            {
                case ScreenOrientation.LandscapeRight:
                    // 90 deg clockwise
                    mappedX = (1.0 - clampedY);
                    mappedY = clampedX;
                    break;

                case ScreenOrientation.PortraitUpsideDown:
                    // 180 deg
                    mappedX = (1.0 - clampedX);
                    mappedY = (1.0 - clampedY);
                    break;

                case ScreenOrientation.LandscapeLeft:
                    // 270 deg clockwise
                    mappedX = clampedY;
                    mappedY = (1.0 - clampedX);
                    break;

                case ScreenOrientation.Portrait:
                default:
                    mappedX = clampedX;
                    mappedY = clampedY;
                    break;
            }

            int pixelX = (int)Math.Round(mappedX * (screenWidth - 1));
            int pixelY = (int)Math.Round(mappedY * (screenHeight - 1));

            return new PixelPoint(
                Math.Clamp(pixelX, 0, screenWidth - 1),
                Math.Clamp(pixelY, 0, screenHeight - 1));
        }
    }
}
