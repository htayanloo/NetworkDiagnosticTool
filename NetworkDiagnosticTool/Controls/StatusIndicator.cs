using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using NetworkDiagnosticTool.Models;

namespace NetworkDiagnosticTool.Controls
{
    public class StatusIndicator : Control
    {
        private CheckStatus _status = CheckStatus.Unknown;
        private bool _isAnimating = false;
        private int _animationAngle = 0;
        private Timer _animationTimer;

        public StatusIndicator()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint, true);

            BackColor = Color.Transparent;
            Size = new Size(16, 16);

            _animationTimer = new Timer();
            _animationTimer.Interval = 50;
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        [Category("Appearance")]
        [Description("The status to display")]
        public CheckStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    UpdateAnimation();
                    Invalidate();
                }
            }
        }

        public void SetSuccess()
        {
            Status = CheckStatus.Success;
        }

        public void SetFailure()
        {
            Status = CheckStatus.Failure;
        }

        public void SetWarning()
        {
            Status = CheckStatus.Warning;
        }

        public void SetChecking()
        {
            Status = CheckStatus.Checking;
        }

        public void SetUnknown()
        {
            Status = CheckStatus.Unknown;
        }

        private void UpdateAnimation()
        {
            if (_status == CheckStatus.Checking)
            {
                if (!_isAnimating)
                {
                    _isAnimating = true;
                    _animationTimer.Start();
                }
            }
            else
            {
                _isAnimating = false;
                _animationTimer.Stop();
                _animationAngle = 0;
            }
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            _animationAngle = (_animationAngle + 30) % 360;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(2, 2, Width - 4, Height - 4);
            Color fillColor;
            Color borderColor;

            switch (_status)
            {
                case CheckStatus.Success:
                    fillColor = Color.FromArgb(40, 167, 69);       // Green
                    borderColor = Color.FromArgb(30, 130, 55);
                    break;
                case CheckStatus.Warning:
                    fillColor = Color.FromArgb(255, 193, 7);       // Yellow
                    borderColor = Color.FromArgb(200, 150, 0);
                    break;
                case CheckStatus.Failure:
                    fillColor = Color.FromArgb(220, 53, 69);       // Red
                    borderColor = Color.FromArgb(170, 40, 55);
                    break;
                case CheckStatus.Checking:
                    fillColor = Color.FromArgb(0, 123, 255);       // Blue
                    borderColor = Color.FromArgb(0, 95, 200);
                    DrawSpinner(g, rect, fillColor);
                    return;
                default:
                    fillColor = Color.FromArgb(108, 117, 125);     // Gray
                    borderColor = Color.FromArgb(80, 90, 100);
                    break;
            }

            using (var brush = new SolidBrush(fillColor))
            using (var pen = new Pen(borderColor, 1.5f))
            {
                g.FillEllipse(brush, rect);
                g.DrawEllipse(pen, rect);
            }

            // Add a slight highlight for 3D effect
            var highlightRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width / 3, rect.Height / 3);
            using (var highlightBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
            {
                g.FillEllipse(highlightBrush, highlightRect);
            }
        }

        private void DrawSpinner(Graphics g, Rectangle rect, Color color)
        {
            using (var pen = new Pen(Color.FromArgb(60, color), 2))
            {
                g.DrawEllipse(pen, rect);
            }

            using (var pen = new Pen(color, 2))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                var startAngle = _animationAngle;
                var sweepAngle = 90;
                g.DrawArc(pen, rect, startAngle, sweepAngle);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
