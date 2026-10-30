using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ProjectName.Windows.UI.Controls
{
    /// <summary>
    /// Circular directional pad (D-Pad) for Google TV / Android TV remote control.
    /// Provides low-latency Up, Down, Left, Right, and Center (OK) triggers with smooth hover feedback.
    /// </summary>
    public class PeerlightTvDpad : Control
    {
        public event EventHandler? UpClicked;
        public event EventHandler? DownClicked;
        public event EventHandler? LeftClicked;
        public event EventHandler? RightClicked;
        public event EventHandler? CenterClicked;

        private int _hoveredSegment = 0; // 0: None, 1: Up, 2: Right, 3: Down, 4: Left, 5: Center
        private int _pressedSegment = 0;

        public PeerlightTvDpad()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(220, 220);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int segment = HitTest(e.Location);
            if (segment != _hoveredSegment)
            {
                _hoveredSegment = segment;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoveredSegment = 0;
            _pressedSegment = 0;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _pressedSegment = HitTest(e.Location);
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            int segment = HitTest(e.Location);
            if (segment == _pressedSegment && segment > 0)
            {
                switch (segment)
                {
                    case 1: UpClicked?.Invoke(this, EventArgs.Empty); break;
                    case 2: RightClicked?.Invoke(this, EventArgs.Empty); break;
                    case 3: DownClicked?.Invoke(this, EventArgs.Empty); break;
                    case 4: LeftClicked?.Invoke(this, EventArgs.Empty); break;
                    case 5: CenterClicked?.Invoke(this, EventArgs.Empty); break;
                }
            }
            _pressedSegment = 0;
            Invalidate();
            base.OnMouseUp(e);
        }

        private int HitTest(Point pt)
        {
            float cx = Width / 2f;
            float cy = Height / 2f;
            float dx = pt.X - cx;
            float dy = pt.Y - cy;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            float outerRadius = Math.Min(Width, Height) / 2f - 10f;
            float innerRadius = outerRadius * 0.44f;

            if (dist > outerRadius) return 0;
            if (dist <= innerRadius) return 5; // Center

            // Directional angles
            double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            if (angle < 0) angle += 360.0;

            if (angle >= 45 && angle < 135) return 3; // Down
            if (angle >= 135 && angle < 225) return 4; // Left
            if (angle >= 225 && angle < 315) return 1; // Up
            return 2; // Right
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = Width / 2f;
            float cy = Height / 2f;
            float outerR = Math.Min(Width, Height) / 2f - 10f;
            float innerR = outerR * 0.44f;

            // Outer Ring Background
            var outerBounds = new RectangleF(cx - outerR, cy - outerR, outerR * 2, outerR * 2);
            using (var ringBrush = new SolidBrush(PeerlightTheme.SurfaceElevated))
            using (var ringPen = new Pen(PeerlightTheme.BorderSubtle, 1.5f))
            {
                g.FillEllipse(ringBrush, outerBounds);
                g.DrawEllipse(ringPen, outerBounds);
            }

            // Highlight Active Segment if any
            if (_hoveredSegment >= 1 && _hoveredSegment <= 4)
            {
                float startAngle = _hoveredSegment switch
                {
                    1 => 225f, // Up
                    2 => 315f, // Right
                    3 => 45f,  // Down
                    4 => 135f, // Left
                    _ => 0f
                };

                using var piePath = new GraphicsPath();
                piePath.AddPie(outerBounds.X, outerBounds.Y, outerBounds.Width, outerBounds.Height, startAngle, 90f);
                Color segColor = (_pressedSegment == _hoveredSegment) ? PeerlightTheme.BrandBlueHover : PeerlightTheme.SurfaceHover;
                using var segBrush = new SolidBrush(segColor);
                g.FillPath(segBrush, piePath);
            }

            // Directional Arrow Symbols
            DrawArrow(g, cx, cy - (outerR + innerR) / 2f, 0);   // Up
            DrawArrow(g, cx + (outerR + innerR) / 2f, cy, 90);  // Right
            DrawArrow(g, cx, cy + (outerR + innerR) / 2f, 180); // Down
            DrawArrow(g, cx - (outerR + innerR) / 2f, cy, 270); // Left

            // Center Button (OK / Select)
            var innerBounds = new RectangleF(cx - innerR, cy - innerR, innerR * 2, innerR * 2);
            Color centerColor = (_pressedSegment == 5) ? PeerlightTheme.BrandBlueHover :
                                (_hoveredSegment == 5 ? PeerlightTheme.BrandBlueHover : PeerlightTheme.BrandBlue);

            using (var centerBrush = new SolidBrush(centerColor))
            using (var centerPen = new Pen(PeerlightTheme.BorderSubtle, 2f))
            {
                g.FillEllipse(centerBrush, innerBounds);
                g.DrawEllipse(centerPen, innerBounds);
            }

            // Draw "OK" in center
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var okBrush = new SolidBrush(PeerlightTheme.TextPrimary);
            g.DrawString("OK", PeerlightTheme.FontHeading, okBrush, innerBounds, sf);
        }

        private static void DrawArrow(Graphics g, float x, float y, float angleDegrees)
        {
            var state = g.Save();
            g.TranslateTransform(x, y);
            g.RotateTransform(angleDegrees);

            using var brush = new SolidBrush(PeerlightTheme.TextSecondary);
            PointF[] arrow =
            {
                new PointF(0, -7),
                new PointF(6, 4),
                new PointF(-6, 4)
            };
            g.FillPolygon(brush, arrow);
            g.Restore(state);
        }
    }
}
