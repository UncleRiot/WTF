using System;
using System.Drawing;
using System.Windows.Forms;


namespace WTF
{
    public sealed class SizeBarCell : DataGridViewTextBoxCell
    {
        protected override void Paint(
            Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates cellState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            base.Paint(
                graphics,
                clipBounds,
                cellBounds,
                rowIndex,
                cellState,
                value,
                formattedValue,
                errorText,
                cellStyle,
                advancedBorderStyle,
                paintParts & ~DataGridViewPaintParts.ContentForeground);

            double percent = 0;

            if (value != null)
            {
                double.TryParse(value.ToString(), out percent);
            }

            percent = Math.Max(0, Math.Min(100, percent));

            Rectangle barBounds = new Rectangle(
                cellBounds.Left + 4,
                cellBounds.Top + 6,
                (int)((cellBounds.Width - 8) * percent / 100D),
                cellBounds.Height - 12);

            using Brush barBrush = new SolidBrush(ModernTheme.AccentColor);
            graphics.FillRectangle(barBrush, barBounds);

            string text = percent.ToString("0.##") + " %";

            TextRenderer.DrawText(
                graphics,
                text,
                cellStyle.Font,
                cellBounds,
                cellStyle.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}