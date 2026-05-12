using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WTF
{
    public sealed class PartitionGridController
    {
        private readonly AppSettings _settings;
        private readonly SplitContainer _splitContainerLeft;
        private readonly DataGridView _listViewPartitions;
        private readonly ImageList _imageListPartitions;
        private readonly ShellIconService _shellIconService;

        // Increase this value for more vertical row spacing, decrease it for a more compact partition grid.
        private const int PartitionGridRowVerticalSpacing = 2;

        public PartitionGridController(
            AppSettings settings,
            SplitContainer splitContainerLeft,
            DataGridView listViewPartitions,
            ImageList imageListPartitions,
            ShellIconService shellIconService)
        {
            _settings = settings;
            _splitContainerLeft = splitContainerLeft;
            _listViewPartitions = listViewPartitions;
            _imageListPartitions = imageListPartitions;
            _shellIconService = shellIconService;
        }

        public void Configure()
        {
            _listViewPartitions.CellPainting += listViewPartitions_CellPainting;
            _listViewPartitions.SizeChanged += listViewPartitions_SizeChanged;
        }

        public void LoadPartitionList()
        {
            _listViewPartitions.SuspendLayout();

            try
            {
                ApplyCompactPartitionGridLayout();
                _listViewPartitions.Rows.Clear();
                _imageListPartitions.Images.Clear();

                foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
                {
                    if (!driveInfo.IsReady)
                        continue;

                    string rootPath = driveInfo.RootDirectory.FullName;
                    _imageListPartitions.Images.Add(rootPath, _shellIconService.GetSmallSystemIcon(rootPath));

                    long totalSize = driveInfo.TotalSize;
                    long freeSpace = driveInfo.AvailableFreeSpace;
                    int freePercent = totalSize <= 0 ? 0 : (int)Math.Round((double)freeSpace * 100D / totalSize);

                    int rowIndex = _listViewPartitions.Rows.Add(
                        rootPath,
                        SizeFormatter.Format(totalSize),
                        SizeFormatter.Format(freeSpace),
                        freePercent + " %");

                    DataGridViewRow row = _listViewPartitions.Rows[rowIndex];
                    row.Height = _listViewPartitions.RowTemplate.Height;
                    row.Tag = freePercent;
                    row.Cells[0].Tag = rootPath;
                }

                AdjustColumns();
            }
            finally
            {
                _listViewPartitions.ResumeLayout();
                _listViewPartitions.Invalidate();
            }
        }

        public void SaveColumnLayout()
        {
            _settings.HasColumnLayout = true;

            if (_listViewPartitions.Columns.Count == 4)
            {
                _settings.PartitionColumnNameWidth = _listViewPartitions.Columns[0].Width;
                _settings.PartitionColumnSizeWidth = _listViewPartitions.Columns[1].Width;
                _settings.PartitionColumnFreeWidth = _listViewPartitions.Columns[2].Width;
                _settings.PartitionColumnFreePercentWidth = _listViewPartitions.Columns[3].Width;
            }
        }

        public void UpdatePartitionPanelVisibility()
        {
            _splitContainerLeft.Panel2Collapsed = !_settings.ShowPartitionPanel;
        }

        public void AdjustColumns()
        {
            if (_listViewPartitions.Columns.Count != 4)
                return;

            int clientWidth = _listViewPartitions.ClientSize.Width;

            if (clientWidth <= 0)
                return;

            int verticalScrollBarWidth = _listViewPartitions.Rows.Count > _listViewPartitions.DisplayedRowCount(false)
                ? SystemInformation.VerticalScrollBarWidth
                : 0;

            int availableWidth = Math.Max(0, clientWidth - verticalScrollBarWidth - 2);

            int sizeColumnWidth = Math.Max(64, Math.Min(78, availableWidth / 5));
            int freeColumnWidth = Math.Max(64, Math.Min(78, availableWidth / 5));
            int freePercentColumnWidth = Math.Max(68, Math.Min(82, availableWidth / 5));
            int nameColumnWidth = Math.Max(70, availableWidth - sizeColumnWidth - freeColumnWidth - freePercentColumnWidth);

            _listViewPartitions.Columns[0].Width = nameColumnWidth;
            _listViewPartitions.Columns[1].Width = sizeColumnWidth;
            _listViewPartitions.Columns[2].Width = freeColumnWidth;
            _listViewPartitions.Columns[3].Width = freePercentColumnWidth;
        }

        public void HandleCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex < 0)
                return;

            if (e.ColumnIndex != 0 && e.ColumnIndex != 3)
                return;

            e.Handled = true;

            bool selected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;

            Color backColor = selected
                ? SystemColors.Highlight
                : _listViewPartitions.BackgroundColor;

            Color textColor = selected
                ? SystemColors.HighlightText
                : _listViewPartitions.ForeColor;

            using (SolidBrush backBrush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.CellBounds);
            }

            if (e.ColumnIndex == 0)
            {
                string text = Convert.ToString(e.FormattedValue);
                string rootPath = Convert.ToString(_listViewPartitions.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag);

                int iconLeft = e.CellBounds.Left + 4;
                int iconTop = e.CellBounds.Top + Math.Max(0, (e.CellBounds.Height - 16) / 2);

                if (!string.IsNullOrWhiteSpace(rootPath) && _imageListPartitions.Images.ContainsKey(rootPath))
                {
                    e.Graphics.DrawImage(_imageListPartitions.Images[rootPath], iconLeft, iconTop, 16, 16);
                }

                Rectangle textBounds = new Rectangle(
                    e.CellBounds.Left + 24,
                    e.CellBounds.Top,
                    Math.Max(0, e.CellBounds.Width - 28),
                    e.CellBounds.Height);

                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    e.CellStyle.Font,
                    textBounds,
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                return;
            }

            int freePercent = _listViewPartitions.Rows[e.RowIndex].Tag is int value ? value : 0;
            freePercent = Math.Max(0, Math.Min(100, freePercent));

            Rectangle barBounds = new Rectangle(
                e.CellBounds.Left + 4,
                e.CellBounds.Top + 2,
                Math.Max(0, e.CellBounds.Width - 8),
                Math.Max(0, e.CellBounds.Height - 4));

            int barWidth = (int)Math.Round(barBounds.Width * freePercent / 100D);

            using (SolidBrush emptyBrush = new SolidBrush(Color.FromArgb(230, 230, 230)))
            using (SolidBrush fillBrush = new SolidBrush(Color.Lime))
            using (Pen borderPen = new Pen(Color.Silver))
            {
                e.Graphics.FillRectangle(emptyBrush, barBounds);

                if (barWidth > 0)
                {
                    e.Graphics.FillRectangle(
                        fillBrush,
                        new Rectangle(barBounds.Left, barBounds.Top, barWidth, barBounds.Height));
                }

                e.Graphics.DrawRectangle(borderPen, barBounds);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Convert.ToString(e.FormattedValue),
                e.CellStyle.Font,
                barBounds,
                Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void listViewPartitions_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            HandleCellPainting(e);
        }

        private void listViewPartitions_SizeChanged(object sender, EventArgs e)
        {
            AdjustColumns();
        }

        private void ApplyCompactPartitionGridLayout()
        {
            int rowHeight = Math.Max(_listViewPartitions.Font.Height + PartitionGridRowVerticalSpacing, 18);
            int headerHeight = Math.Max(_listViewPartitions.Font.Height + 6, 20);

            _listViewPartitions.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            _listViewPartitions.RowTemplate.Height = rowHeight;
            _listViewPartitions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _listViewPartitions.ColumnHeadersHeight = headerHeight;
            _listViewPartitions.RowTemplate.MinimumHeight = rowHeight;
            _listViewPartitions.RowsDefaultCellStyle.Padding = Padding.Empty;
        }
    }
}
