using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ProjectName.Windows.UI
{
    /// <summary>
    /// Peerlight Design System Tokens and Drawing Helpers for Windows Desktop.
    /// Provides macOS-inspired minimalist dark theme palette, typography, and card rendering.
    /// </summary>
    public static class PeerlightTheme
    {
        // Theme Mode State
        public static bool IsDarkMode { get; set; } = true;

        // 1. Brand Palette Tokens
        public static Color NavyBg => IsDarkMode ? Color.FromArgb(7, 20, 38) : Color.FromArgb(245, 247, 250);          // #071426 / #F5F7FA
        public static Color SurfaceDark => IsDarkMode ? Color.FromArgb(13, 27, 45) : Color.FromArgb(255, 255, 255);     // #0D1B2D / #FFFFFF
        public static Color SurfaceElevated => IsDarkMode ? Color.FromArgb(22, 39, 63) : Color.FromArgb(234, 240, 246); // #16273F / #EAF0F6
        public static Color SurfaceHover => IsDarkMode ? Color.FromArgb(30, 52, 84) : Color.FromArgb(221, 230, 240);    // #1E3454 / #DDE6F0
        public static Color BorderSubtle => IsDarkMode ? Color.FromArgb(28, 48, 76) : Color.FromArgb(209, 220, 232);    // #1C304C / #D1DCE8
        public static Color BrandBlue => Color.FromArgb(22, 139, 255);     // #168BFF
        public static Color BrandBlueHover => Color.FromArgb(2, 132, 199); // #0284C7
        public static Color BrandCyan => IsDarkMode ? Color.FromArgb(32, 217, 232) : Color.FromArgb(2, 132, 199);     // #20D9E8 / #0284C7
        public static Color BrandCyanLight => Color.FromArgb(103, 232, 249);// #67E8F9

        // 2. Semantic Status Tokens
        public static Color StatusSuccess => Color.FromArgb(34, 197, 94);  // #22C55E
        public static Color StatusWarning => Color.FromArgb(245, 158, 11); // #F59E0B
        public static Color StatusDanger => Color.FromArgb(239, 68, 68);   // #EF4444
        public static Color StatusDangerHover => Color.FromArgb(220, 38, 38); // #DC2626

        // 3. Text Hierarchy Tokens
        public static Color TextPrimary => IsDarkMode ? Color.FromArgb(244, 247, 251) : Color.FromArgb(7, 20, 38);  // #F4F7FB / #071426
        public static Color TextSecondary => IsDarkMode ? Color.FromArgb(154, 170, 192) : Color.FromArgb(74, 93, 117);// #9AAAC0 / #4A5D75
        public static Color TextMuted => IsDarkMode ? Color.FromArgb(100, 116, 139) : Color.FromArgb(130, 148, 170);    // #64748B / #8294AA

        // 4. Typography Scale
        public static readonly Font FontDisplay = new Font("Segoe UI Variable Display", 18f, FontStyle.Bold);
        public static readonly Font FontHeading = new Font("Segoe UI Variable Text", 13f, FontStyle.Bold);
        public static readonly Font FontSubheading = new Font("Segoe UI Variable Text", 11f, FontStyle.Bold);
        public static readonly Font FontBody = new Font("Segoe UI Variable Text", 9.5f, FontStyle.Regular);
        public static readonly Font FontCaption = new Font("Segoe UI Variable Text", 8.5f, FontStyle.Regular);
        public static readonly Font FontMonospace = new Font("Consolas", 14f, FontStyle.Bold);
        public static readonly Font FontMonospaceSmall = new Font("Consolas", 9f, FontStyle.Regular);

        // 5. Drawing Helpers
        public static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void DrawCard(Graphics g, Rectangle bounds, int radius = 10, Color? fill = null, Color? border = null)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundedRectangle(bounds, radius);
            using var brush = new SolidBrush(fill ?? SurfaceDark);
            using var pen = new Pen(border ?? BorderSubtle, 1f);

            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        public static void DrawPill(Graphics g, Rectangle bounds, string text, Color bg, Color fg, Font? font = null)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int radius = bounds.Height / 2;
            using var path = CreateRoundedRectangle(bounds, radius);
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);

            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var textBrush = new SolidBrush(fg);
            g.DrawString(text, font ?? FontCaption, textBrush, bounds, sf);
        }
    }
}
