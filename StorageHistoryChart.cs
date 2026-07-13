using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace WTF
{
    public enum StorageHistoryDisplayMode
    {
        UsedSpace,
        FreeSpace
    }

    public sealed class StorageHistoryChart : Control
    {
        private readonly ToolTip toolTip;
        private IReadOnlyList<StorageHistoryRecord> _records = Array.Empty<StorageHistoryRecord>();
        private PointF[] _points = Array.Empty<PointF>();
        private StorageHistoryDisplayMode _displayMode;
        private int _hoveredPointIndex = -1;

        public StorageHistoryChart()
        {
            DoubleBuffered = true;
            BackColor = SystemColors.Window;
            ForeColor = SystemColors.WindowText;
            toolTip = new ToolTip
            {
                InitialDelay = 150,
                ReshowDelay = 50,
                AutoPopDelay = 10000,
                ShowAlways = true
            };
        }

        public void SetRecords(
            IReadOnlyList<StorageHistoryRecord> records,
            StorageHistoryDisplayMode displayMode)
        {
            _records = records ?? Array.Empty<StorageHistoryRecord>();
            _displayMode = displayMode;
            _hoveredPointIndex = -1;
            toolTip.Hide(this);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredPointIndex = -1;
            toolTip.Hide(this);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_points.Length == 0)
                return;

            int nearestIndex = -1;
            double nearestDistance = double.MaxValue;

            for (int index = 0; index < _points.Length; index++)
            {
                double deltaX = _points[index].X - e.X;
                double deltaY = _points[index].Y - e.Y;
                double distance = deltaX * deltaX + deltaY * deltaY;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = index;
                }
            }

            if (nearestIndex < 0 || nearestDistance > 144D)
            {
                if (_hoveredPointIndex >= 0)
                {
                    _hoveredPointIndex = -1;
                    toolTip.Hide(this);
                }

                return;
            }

            if (_hoveredPointIndex == nearestIndex)
                return;

            _hoveredPointIndex = nearestIndex;
            StorageHistoryRecord record = _records[nearestIndex];
            long value = GetDisplayValue(record);
            string hint = string.Format(
                CultureInfo.CurrentCulture,
                "{0}\r\n{1}: {2}",
                record.RecordedAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
                LocalizationService.GetText(
                    _displayMode == StorageHistoryDisplayMode.FreeSpace
                        ? "StorageHistory.Free"
                        : "StorageHistory.Used"),
                SizeFormatter.Format(value));

            toolTip.Show(hint, this, e.X + 14, e.Y + 14, 10000);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle plotArea = new Rectangle(
                80,
                45,
                Math.Max(1, ClientSize.Width - 105),
                Math.Max(1, ClientSize.Height - 105));

            using Font titleFont = new Font(Font, FontStyle.Bold);
            using Brush titleBrush = new SolidBrush(ForeColor);
            e.Graphics.DrawString(LocalizationService.GetText("StorageHistory.Graph"), titleFont, titleBrush, 12, 12);

            if (_records.Count == 0)
            {
                _points = Array.Empty<PointF>();
                TextRenderer.DrawText(
                    e.Graphics,
                    LocalizationService.GetText("StorageHistory.NoData"),
                    Font,
                    ClientRectangle,
                    ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            long maximumCapacity = _records.Max(record => record.TotalCapacityBytes);
            long maximumValue = _records.Max(GetDisplayValue);
            long axisMaximum = Math.Max(1L, Math.Max(maximumCapacity, maximumValue));

            DateTime minimumTime = _records.Min(record => record.RecordedAtUtc);
            DateTime maximumTime = _records.Max(record => record.RecordedAtUtc);
            double timeRangeTicks = Math.Max(1D, (maximumTime - minimumTime).Ticks);

            using Pen gridPen = new Pen(SystemColors.ControlLight);
            using Pen axisPen = new Pen(SystemColors.ControlDark);

            const int horizontalGridLineCount = 5;

            for (int index = 0; index <= horizontalGridLineCount; index++)
            {
                float ratio = index / (float)horizontalGridLineCount;
                float y = plotArea.Bottom - ratio * plotArea.Height;
                e.Graphics.DrawLine(gridPen, plotArea.Left, y, plotArea.Right, y);

                long labelValue = (long)Math.Round(axisMaximum * ratio);
                string labelText = SizeFormatter.Format(labelValue);
                Size labelSize = TextRenderer.MeasureText(labelText, Font);
                TextRenderer.DrawText(
                    e.Graphics,
                    labelText,
                    Font,
                    new Point(plotArea.Left - labelSize.Width - 6, (int)y - labelSize.Height / 2),
                    ForeColor);
            }

            int verticalGridLineCount = Math.Max(2, Math.Min(6, plotArea.Width / 140));

            for (int index = 0; index <= verticalGridLineCount; index++)
            {
                float ratio = index / (float)verticalGridLineCount;
                float x = plotArea.Left + ratio * plotArea.Width;
                e.Graphics.DrawLine(gridPen, x, plotArea.Top, x, plotArea.Bottom);

                DateTime labelTime = minimumTime.AddTicks(
                    (long)Math.Round((maximumTime - minimumTime).Ticks * ratio));
                string labelText = FormatAxisTime(labelTime, minimumTime, maximumTime);
                Size labelSize = TextRenderer.MeasureText(labelText, Font);
                int labelX = (int)x - labelSize.Width / 2;
                labelX = Math.Max(plotArea.Left, Math.Min(plotArea.Right - labelSize.Width, labelX));

                TextRenderer.DrawText(
                    e.Graphics,
                    labelText,
                    Font,
                    new Point(labelX, plotArea.Bottom + 8),
                    ForeColor);
            }

            e.Graphics.DrawLine(axisPen, plotArea.Left, plotArea.Top, plotArea.Left, plotArea.Bottom);
            e.Graphics.DrawLine(axisPen, plotArea.Left, plotArea.Bottom, plotArea.Right, plotArea.Bottom);

            _points = new PointF[_records.Count];

            for (int index = 0; index < _records.Count; index++)
            {
                double elapsedTicks = (_records[index].RecordedAtUtc - minimumTime).Ticks;
                float normalizedTime = _records.Count == 1
                    ? 0.5F
                    : (float)(elapsedTicks / timeRangeTicks);
                float x = plotArea.Left + normalizedTime * plotArea.Width;

                long value = GetDisplayValue(_records[index]);
                float normalizedValue = value / (float)axisMaximum;
                float y = plotArea.Bottom - normalizedValue * plotArea.Height;
                _points[index] = new PointF(x, y);
            }

            using Pen graphPen = new Pen(SystemColors.Highlight, 2F);
            if (_points.Length > 1)
            {
                e.Graphics.DrawLines(graphPen, _points);
            }

            using Brush pointBrush = new SolidBrush(SystemColors.Highlight);
            foreach (PointF point in _points)
            {
                e.Graphics.FillEllipse(pointBrush, point.X - 3F, point.Y - 3F, 6F, 6F);
            }
        }

        private long GetDisplayValue(StorageHistoryRecord record)
        {
            if (_displayMode == StorageHistoryDisplayMode.FreeSpace)
            {
                if (record.TotalCapacityBytes > 0L)
                {
                    return Math.Max(0L, Math.Min(record.TotalCapacityBytes, record.FreeSpaceBytes));
                }

                return 0L;
            }

            if (record.TotalCapacityBytes > 0L)
            {
                return Math.Max(
                    0L,
                    Math.Min(record.TotalCapacityBytes, record.TotalCapacityBytes - record.FreeSpaceBytes));
            }

            return Math.Max(0L, record.SizeBytes);
        }

        private static string FormatAxisTime(
            DateTime valueUtc,
            DateTime minimumUtc,
            DateTime maximumUtc)
        {
            TimeSpan range = maximumUtc - minimumUtc;
            DateTime localValue = valueUtc.ToLocalTime();

            if (range.TotalDays < 1D)
                return localValue.ToString("t", CultureInfo.CurrentCulture);

            if (range.TotalDays < 7D)
                return localValue.ToString("g", CultureInfo.CurrentCulture);

            return localValue.ToString("d", CultureInfo.CurrentCulture);
        }
    }
}
