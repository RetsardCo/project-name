using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ProjectName.Windows.UI.Controls
{
    public enum ButtonVariant
    {
        Primary,
        Secondary,
        Danger,
        Ghost
    }

    /// <summary>
    /// Modern macOS-inspired rounded button supporting Peerlight theme variants.
    /// </summary>
    public class PeerlightButton : Button
    {
        private bool _isHovered;
        private bool _isPressed;
        private ButtonVariant _variant = ButtonVariant.Primary;
        private int _borderRadius = 8;

        public ButtonVariant Variant
        {
            get => _variant;
            set { _variant = value; Invalidate(); }
        }

        public int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = value; Invalidate(); }
        }

        public PeerlightButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = PeerlightTheme.FontSubheading;
            Cursor = Cursors.Hand;
            Size = new Size(130, 36);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _isPressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            Color bgColor;
            Color fgColor = PeerlightTheme.TextPrimary;
            Color borderColor = Color.Transparent;

            switch (_variant)
            {
                case ButtonVariant.Primary:
                    bgColor = _isPressed ? PeerlightTheme.BrandBlueHover : (_isHovered ? PeerlightTheme.BrandBlueHover : PeerlightTheme.BrandBlue);
                    break;
                case ButtonVariant.Danger:
                    bgColor = _isPressed ? PeerlightTheme.StatusDangerHover : (_isHovered ? PeerlightTheme.StatusDangerHover : PeerlightTheme.StatusDanger);
                    break;
                case ButtonVariant.Secondary:
                    bgColor = _isPressed ? PeerlightTheme.SurfaceHover : (_isHovered ? PeerlightTheme.SurfaceHover : PeerlightTheme.SurfaceElevated);
                    borderColor = PeerlightTheme.BorderSubtle;
                    break;
                case ButtonVariant.Ghost:
                default:
                    bgColor = _isPressed ? PeerlightTheme.SurfaceHover : (_isHovered ? PeerlightTheme.SurfaceElevated : Color.Transparent);
                    fgColor = _isHovered ? PeerlightTheme.TextPrimary : PeerlightTheme.TextSecondary;
                    break;
            }

            if (!Enabled)
            {
                bgColor = PeerlightTheme.SurfaceDark;
                fgColor = PeerlightTheme.TextMuted;
                borderColor = PeerlightTheme.BorderSubtle;
            }

            using var path = PeerlightTheme.CreateRoundedRectangle(bounds, _borderRadius);
            using var brush = new SolidBrush(bgColor);
            g.FillPath(brush, path);

            if (borderColor != Color.Transparent)
            {
                using var pen = new Pen(borderColor, 1f);
                g.DrawPath(pen, path);
            }

            // Draw Button Text
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var textBrush = new SolidBrush(fgColor);
            g.DrawString(Text, Font, textBrush, bounds, sf);
        }
    }
}
