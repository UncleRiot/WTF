using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WTF
{
    public sealed class BarChartView : Control
    {
        private FileSystemEntry _entry;

        public BarChartView()
        {
            DoubleBuffered = true;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            UpdateStyles();
        }

        public void SetEntry(FileSystemEntry entry)
        {
            _entry = entry;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle visibleBounds = GetVisibleClientRectangle();

            if (visibleBounds.IsEmpty)
                return;

            int visibleWidth = visibleBounds.Width;
            int visibleHeight = visibleBounds.Height;

            if (visibleWidth <= 0 || visibleHeight <= 0)
                return;

            e.Graphics.SetClip(visibleBounds);
            e.Graphics.Clear(BackColor);

            if (_entry == null || _entry.Children.Count == 0)
            {
                DrawEmptyText(e.Graphics);
                return;
            }

            List<FileSystemEntry> items = _entry.Children
                .Where(child => child.SizeBytes > 0)
                .OrderByDescending(child => child.SizeBytes)
                .Take(18)
                .ToList();

            if (items.Count == 0)
            {
                DrawEmptyText(e.Graphics);
                return;
            }

            long maxSize = items.Max(item => item.SizeBytes);

            if (maxSize <= 0)
            {
                DrawEmptyText(e.Graphics);
                return;
            }

            int leftMargin = 20;
            int rightMargin = 20;
            int topMargin = 18;
            int labelToBarGap = 20;
            int rowHeight = 21;
            int barHeight = 14;
            int textPaddingLeft = 10;
            int textGapRightOfBar = 8;

            int contentLeft = visibleBounds.Left + leftMargin;
            int contentRight = visibleBounds.Right - rightMargin;

            if (contentRight <= contentLeft)
                return;

            int longestLabelWidth = 0;

            foreach (FileSystemEntry item in items)
            {
                Size labelSize = TextRenderer.MeasureText(e.Graphics, item.Name, Font);
                longestLabelWidth = Math.Max(longestLabelWidth, labelSize.Width);
            }

            int maximumLabelWidth = Math.Max(80, Math.Min(220, visibleWidth / 3));
            int labelWidth = Math.Min(longestLabelWidth, maximumLabelWidth);

            int barLeft = contentLeft + labelWidth + labelToBarGap;
            int maximumBarWidth = contentRight - barLeft;

            if (maximumBarWidth <= 0)
                return;

            for (int index = 0; index < items.Count; index++)
            {
                FileSystemEntry item = items[index];
                int y = visibleBounds.Top + topMargin + index * rowHeight;

                if (y + rowHeight > visibleBounds.Bottom)
                    break;

                string sizeText = SizeFormatter.Format(item.SizeBytes);

                TextRenderer.DrawText(
                    e.Graphics,
                    item.Name,
                    Font,
                    new Rectangle(contentLeft, y, labelWidth, rowHeight),
                    ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                int barWidth = (int)Math.Round(maximumBarWidth * ((double)item.SizeBytes / maxSize));
                barWidth = Math.Max(1, Math.Min(maximumBarWidth, barWidth));

                Rectangle barBounds = new Rectangle(
                    barLeft,
                    y + (rowHeight - barHeight) / 2,
                    barWidth,
                    barHeight);

                using SolidBrush barBrush = new SolidBrush(ModernTheme.ChartColors[index % ModernTheme.ChartColors.Length]);
                e.Graphics.FillRectangle(barBrush, barBounds);

                Size sizeTextSize = TextRenderer.MeasureText(e.Graphics, sizeText, Font);
                bool drawTextInsideBar = barBounds.Width >= sizeTextSize.Width + textPaddingLeft;

                Rectangle textBounds;

                if (drawTextInsideBar)
                {
                    textBounds = new Rectangle(
                        barBounds.Left + textPaddingLeft,
                        y,
                        Math.Max(0, barBounds.Width - textPaddingLeft),
                        rowHeight);
                }
                else
                {
                    textBounds = new Rectangle(
                        barBounds.Right + textGapRightOfBar,
                        y,
                        Math.Max(0, contentRight - barBounds.Right - textGapRightOfBar),
                        rowHeight);
                }

                TextRenderer.DrawText(
                    e.Graphics,
                    sizeText,
                    Font,
                    textBounds,
                    ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            Invalidate();

            if (Parent != null)
            {
                Parent.Invalidate(true);
            }
        }
        private void Parent_SizeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }
        private Rectangle GetVisibleClientRectangle()
        {
            Rectangle visibleScreenRectangle = RectangleToScreen(ClientRectangle);

            Control parent = Parent;

            while (parent != null)
            {
                visibleScreenRectangle = Rectangle.Intersect(
                    visibleScreenRectangle,
                    parent.RectangleToScreen(parent.ClientRectangle));

                parent = parent.Parent;
            }

            if (visibleScreenRectangle.Width <= 0 || visibleScreenRectangle.Height <= 0)
            {
                return Rectangle.Empty;
            }

            Point localLocation = PointToClient(visibleScreenRectangle.Location);

            return new Rectangle(
                localLocation.X,
                localLocation.Y,
                visibleScreenRectangle.Width,
                visibleScreenRectangle.Height);
        }
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            if (Parent != null)
            {
                Parent.SizeChanged -= Parent_SizeChanged;
                Parent.SizeChanged += Parent_SizeChanged;
            }

            Invalidate();
        }
        private void DrawEmptyText(Graphics graphics)
        {
            TextRenderer.DrawText(
                graphics,
                "Keine Daten vorhanden.",
                Font,
                ClientRectangle,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}