using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace WTF
{
    public sealed class PieChartView : Control
    {
        private FileSystemEntry _entry;

        public PieChartView()
        {
            DoubleBuffered = true;
        }

        public void SetEntry(FileSystemEntry entry)
        {
            _entry = entry;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);

            if (_entry == null || _entry.Children.Count == 0)
            {
                DrawEmptyText(e.Graphics);
                return;
            }

            List<ChartItem> chartItems = CreateChartItems(_entry);

            if (chartItems.Count == 0)
            {
                DrawEmptyText(e.Graphics);
                return;
            }

            Rectangle chartBounds = new Rectangle(24, 24, Math.Min(260, Width / 2), Math.Min(260, Height - 48));
            chartBounds.Width = Math.Min(chartBounds.Width, chartBounds.Height);
            chartBounds.Height = chartBounds.Width;

            long totalSize = chartItems.Sum(item => item.SizeBytes);

            if (totalSize <= 0)
            {
                DrawEmptyText(e.Graphics);
                return;
            }

            float startAngle = -90F;

            for (int index = 0; index < chartItems.Count; index++)
            {
                ChartItem item = chartItems[index];
                float sweepAngle = (float)((double)item.SizeBytes * 360D / totalSize);

                using SolidBrush brush = new SolidBrush(ModernTheme.ChartColors[index % ModernTheme.ChartColors.Length]);
                e.Graphics.FillPie(brush, chartBounds, startAngle, sweepAngle);

                startAngle += sweepAngle;
            }

            using Pen borderPen = new Pen(ForeColor, 1);
            e.Graphics.DrawEllipse(borderPen, chartBounds);

            DrawLegend(e.Graphics, chartItems, totalSize, chartBounds.Right + 24, 24);
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

        private List<ChartItem> CreateChartItems(FileSystemEntry entry)
        {
            List<ChartItem> items = entry.Children
                .Where(child => child.SizeBytes > 0)
                .OrderByDescending(child => child.SizeBytes)
                .Take(10)
                .Select(child => new ChartItem(child.Name, child.SizeBytes))
                .ToList();

            long topSize = items.Sum(item => item.SizeBytes);
            long totalSize = entry.Children.Sum(child => child.SizeBytes);
            long otherSize = totalSize - topSize;

            if (otherSize > 0)
            {
                items.Add(new ChartItem("Sonstige", otherSize));
            }

            return items;
        }

        private void DrawLegend(Graphics graphics, List<ChartItem> chartItems, long totalSize, int left, int top)
        {
            int y = top;

            for (int index = 0; index < chartItems.Count; index++)
            {
                ChartItem item = chartItems[index];

                using SolidBrush colorBrush = new SolidBrush(ModernTheme.ChartColors[index % ModernTheme.ChartColors.Length]);
                graphics.FillRectangle(colorBrush, left, y + 3, 14, 14);

                string text = string.Format(
                    "{0} - {1} ({2:0.0} %)",
                    item.Name,
                    SizeFormatter.Format(item.SizeBytes),
                    (double)item.SizeBytes * 100D / totalSize);

                TextRenderer.DrawText(
                    graphics,
                    text,
                    Font,
                    new Rectangle(left + 22, y, Math.Max(0, Width - left - 30), 22),
                    ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                y += 24;
            }
        }

        private sealed class ChartItem
        {
            public ChartItem(string name, long sizeBytes)
            {
                Name = name;
                SizeBytes = sizeBytes;
            }

            public string Name { get; }
            public long SizeBytes { get; }
        }
    }
}