using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WTF
{
    public sealed class Chart_TableGridChart : Chart_ResponsiveTableGrid
    {
        private List<EntryChartItem> _rows = new List<EntryChartItem>();
        private string _sortProperty = nameof(EntryChartItem.SizeBytes);
        private bool _sortAscending;
        private FileSystemEntry _entry;
        private bool _showFiles;

        public Chart_TableGridChart()
        {
            ConfigureTableGridChart();

            ParentChanged += Chart_TableGridChart_ParentChanged;
            CellPainting += Chart_TableGridChart_CellPainting;
            CellToolTipTextNeeded += Chart_TableGridChart_CellToolTipTextNeeded;
            ColumnHeaderMouseClick += Chart_TableGridChart_ColumnHeaderMouseClick;
        }

        public void SetEntry(FileSystemEntry entry)
        {
            _entry = entry;
            BindEntryRows();
        }

        public void SetShowFiles(bool showFiles)
        {
            if (_showFiles == showFiles)
                return;

            _showFiles = showFiles;
            BindEntryRows();
        }

        private void BindEntryRows()
        {
            if (_entry == null)
            {
                _rows = new List<EntryChartItem>();
                DataSource = null;
                FitToCurrentBounds();
                Invalidate();
                return;
            }

            List<FileSystemEntry> visibleEntries = _showFiles
                ? GetLargestFilesRecursive(_entry, 100)
                : _entry.Children
                    .Where(child => child.IsDirectory)
                    .ToList();

            long totalSize = visibleEntries.Sum(child => child.SizeBytes);

            _rows = visibleEntries
                .Select(child => new EntryChartItem
                {
                    Name = child.Name,
                    FullPath = child.FullPath,
                    SizeBytes = child.SizeBytes,
                    FormattedSize = SizeFormatter.Format(child.SizeBytes),
                    Percent = totalSize <= 0 ? 0 : (double)child.SizeBytes * 100D / totalSize
                })
                .ToList();

            AutoGenerateColumns = false;
            BindSortedRows();
            RemoveUnexpectedColumns();
            FitToCurrentBounds();
            Invalidate();
        }

        private static List<FileSystemEntry> GetLargestFilesRecursive(
            FileSystemEntry rootEntry,
            int maximumFileCount)
        {
            PriorityQueue<FileSystemEntry, long> largestFiles =
                new PriorityQueue<FileSystemEntry, long>();
            Stack<FileSystemEntry> pendingEntries =
                new Stack<FileSystemEntry>();

            pendingEntries.Push(rootEntry);

            while (pendingEntries.Count > 0)
            {
                FileSystemEntry currentEntry = pendingEntries.Pop();

                foreach (FileSystemEntry child in currentEntry.Children)
                {
                    if (child.IsDirectory)
                    {
                        pendingEntries.Push(child);
                        continue;
                    }

                    largestFiles.Enqueue(child, child.SizeBytes);

                    if (largestFiles.Count > maximumFileCount)
                    {
                        largestFiles.Dequeue();
                    }
                }
            }

            List<FileSystemEntry> result =
                new List<FileSystemEntry>(largestFiles.Count);

            while (largestFiles.Count > 0)
            {
                result.Add(largestFiles.Dequeue());
            }

            return result;
        }

        private void Chart_TableGridChart_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            string propertyName = Columns[e.ColumnIndex].DataPropertyName;
            _sortAscending = _sortProperty == propertyName && !_sortAscending;
            _sortProperty = propertyName;
            BindSortedRows();
            FitToCurrentBounds();
            Invalidate();
        }

        private void BindSortedRows()
        {
            IEnumerable<EntryChartItem> sortedRows = _sortProperty switch
            {
                nameof(EntryChartItem.Name) => _sortAscending
                    ? _rows.OrderBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase)
                    : _rows.OrderByDescending(row => row.Name, StringComparer.CurrentCultureIgnoreCase),
                nameof(EntryChartItem.FormattedSize) or nameof(EntryChartItem.SizeBytes) => _sortAscending
                    ? _rows.OrderBy(row => row.SizeBytes)
                    : _rows.OrderByDescending(row => row.SizeBytes),
                nameof(EntryChartItem.Percent) => _sortAscending
                    ? _rows.OrderBy(row => row.Percent)
                    : _rows.OrderByDescending(row => row.Percent),
                nameof(EntryChartItem.FullPath) => _sortAscending
                    ? _rows.OrderBy(row => row.FullPath, StringComparer.CurrentCultureIgnoreCase)
                    : _rows.OrderByDescending(row => row.FullPath, StringComparer.CurrentCultureIgnoreCase),
                _ => _rows.OrderByDescending(row => row.SizeBytes)
            };

            DataSource = sortedRows.ToList();
            UpdateSortGlyphs();
        }

        private void UpdateSortGlyphs()
        {
            SortOrder sortOrder = _sortAscending
                ? SortOrder.Ascending
                : SortOrder.Descending;

            foreach (DataGridViewColumn column in Columns)
            {
                column.HeaderCell.SortGlyphDirection =
                    IsSortedColumn(column)
                        ? sortOrder
                        : SortOrder.None;
            }

            Invalidate();
        }

        private bool IsSortedColumn(DataGridViewColumn column)
        {
            if (column == null)
                return false;

            if (_sortProperty == nameof(EntryChartItem.SizeBytes))
            {
                return column.Name == "ColumnSize";
            }

            return column.DataPropertyName == _sortProperty;
        }

        public void ApplyEntryGridColumnWidths()
        {
            FitToCurrentBounds();
            Invalidate();
        }

        private void ConfigureTableGridChart()
        {
            Columns.Clear();

            Dock = DockStyle.Fill;
            AutoSize = false;
            MinimumSize = System.Drawing.Size.Empty;
            MaximumSize = System.Drawing.Size.Empty;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            AllowUserToResizeColumns = true;
            AutoGenerateColumns = false;
            ReadOnly = true;
            RowHeadersVisible = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            ScrollBars = ScrollBars.Vertical;
            BackgroundColor = System.Drawing.SystemColors.Window;
            BorderStyle = BorderStyle.FixedSingle;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            ColumnHeadersHeight = 24;
            EnableHeadersVisualStyles = true;

            Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnName",
                HeaderText = LocalizationService.GetText("Common.Name"),
                DataPropertyName = "Name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 2,
                MinimumWidth = 2,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                Resizable = DataGridViewTriState.True
            });

            Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnSize",
                HeaderText = LocalizationService.GetText("Common.Size"),
                DataPropertyName = "FormattedSize",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 2,
                MinimumWidth = 2,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                Resizable = DataGridViewTriState.True
            });

            Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnPercent",
                HeaderText = LocalizationService.GetText("Chart.TableUsage"),
                DataPropertyName = "Percent",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 2,
                MinimumWidth = 2,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                Resizable = DataGridViewTriState.True
            });

            Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnPath",
                HeaderText = LocalizationService.GetText("Common.Path"),
                DataPropertyName = "FullPath",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                Width = 2,
                MinimumWidth = 2,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                Resizable = DataGridViewTriState.True
            });

            ConfigureEntryGridColumns();
            RemoveUnexpectedColumns();
            ApplyLucidTableStyle();
        }

        private void ApplyLucidTableStyle()
        {
            ApplyLucidStyle();

            DataGridView grid = Controls
                .OfType<DataGridView>()
                .FirstOrDefault();

            if (grid == null)
                return;

            grid.GotFocus -= TableGrid_FocusChanged;
            grid.GotFocus += TableGrid_FocusChanged;
            grid.LostFocus -= TableGrid_FocusChanged;
            grid.LostFocus += TableGrid_FocusChanged;
        }

        private static void TableGrid_FocusChanged(object sender, EventArgs e)
        {
            if (sender is DataGridView grid)
            {
                DialogTableStyle.Apply(grid);
            }
        }

        public void ApplyLocalizedTexts()
        {
            if (Columns.Contains("ColumnName"))
            {
                Columns["ColumnName"].HeaderText = LocalizationService.GetText("Common.Name");
            }

            if (Columns.Contains("ColumnSize"))
            {
                Columns["ColumnSize"].HeaderText = LocalizationService.GetText("Common.Size");
            }

            if (Columns.Contains("ColumnPercent"))
            {
                Columns["ColumnPercent"].HeaderText = LocalizationService.GetText("Chart.TableUsage");
            }

            if (Columns.Contains("ColumnPath"))
            {
                Columns["ColumnPath"].HeaderText = LocalizationService.GetText("Common.Path");
            }

            FitToCurrentBounds();
        }

        private void ConfigureEntryGridColumns()
        {
            AutoSize = false;
            MinimumSize = System.Drawing.Size.Empty;
            MaximumSize = System.Drawing.Size.Empty;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            ScrollBars = ScrollBars.Vertical;

            if (Columns.Contains("ColumnName"))
            {
                Columns["ColumnName"].Visible = true;
                Columns["ColumnName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                Columns["ColumnName"].MinimumWidth = 2;
            }

            if (Columns.Contains("ColumnSize"))
            {
                Columns["ColumnSize"].Visible = true;
                Columns["ColumnSize"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                Columns["ColumnSize"].MinimumWidth = 2;
            }

            if (Columns.Contains("ColumnPercent"))
            {
                Columns["ColumnPercent"].Visible = true;
                Columns["ColumnPercent"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                Columns["ColumnPercent"].MinimumWidth = 2;
            }

            if (Columns.Contains("ColumnPath"))
            {
                Columns["ColumnPath"].Visible = true;
                Columns["ColumnPath"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                Columns["ColumnPath"].MinimumWidth = 2;
            }

            FitToCurrentBounds();
        }


        private void RemoveUnexpectedColumns()
        {
            for (int index = Columns.Count - 1; index >= 0; index--)
            {
                string columnName = Columns[index].Name;

                if (columnName == "ColumnName" ||
                    columnName == "ColumnSize" ||
                    columnName == "ColumnPercent" ||
                    columnName == "ColumnPath")
                {
                    continue;
                }

                Columns.RemoveAt(index);
            }

            if (Columns.Contains("ColumnName"))
            {
                Columns["ColumnName"].DisplayIndex = 0;
            }

            if (Columns.Contains("ColumnSize"))
            {
                Columns["ColumnSize"].DisplayIndex = 1;
            }

            if (Columns.Contains("ColumnPercent"))
            {
                Columns["ColumnPercent"].DisplayIndex = 2;
            }

            if (Columns.Contains("ColumnPath"))
            {
                Columns["ColumnPath"].DisplayIndex = 3;
            }
        }

        private void FitToCurrentBounds()
        {
            SetResponsiveColumns(
                ("ColumnName", 22),
                ("ColumnSize", 14),
                ("ColumnPercent", 14),
                ("ColumnPath", 50));
        }

        private void Chart_TableGridChart_ParentChanged(object sender, EventArgs e)
        {
            FitToCurrentBounds();
        }

        private void Chart_TableGridChart_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex == -1 &&
                e.ColumnIndex >= 0 &&
                e.ColumnIndex < Columns.Count &&
                IsSortedColumn(Columns[e.ColumnIndex]))
            {
                e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);

                int centerX = e.CellBounds.Right - 10;
                int centerY = e.CellBounds.Top + e.CellBounds.Height / 2;

                System.Drawing.Point[] points = _sortAscending
                    ? new[]
                    {
                        new System.Drawing.Point(centerX, centerY - 4),
                        new System.Drawing.Point(centerX - 4, centerY + 3),
                        new System.Drawing.Point(centerX + 4, centerY + 3)
                    }
                    : new[]
                    {
                        new System.Drawing.Point(centerX - 4, centerY - 3),
                        new System.Drawing.Point(centerX + 4, centerY - 3),
                        new System.Drawing.Point(centerX, centerY + 4)
                    };

                using System.Drawing.SolidBrush glyphBrush =
                    new System.Drawing.SolidBrush(e.CellStyle.ForeColor);

                e.Graphics.FillPolygon(glyphBrush, points);
                e.Handled = true;
                return;
            }

            if (e.RowIndex < 0)
                return;

            if (!Columns.Contains("ColumnPercent"))
                return;

            if (e.ColumnIndex != Columns["ColumnPercent"].Index)
                return;

            double percent = 0D;

            if (e.Value is double doubleValue)
            {
                percent = doubleValue;
            }
            else if (e.Value != null)
            {
                double.TryParse(e.Value.ToString(), out percent);
            }

            percent = Math.Max(0D, Math.Min(100D, percent));

            e.PaintBackground(e.CellBounds, true);

            System.Drawing.Rectangle barBounds = new System.Drawing.Rectangle(
                e.CellBounds.Left + 4,
                e.CellBounds.Top + 4,
                Math.Max(0, e.CellBounds.Width - 8),
                Math.Max(0, e.CellBounds.Height - 8));

            int fillWidth = (int)Math.Round(barBounds.Width * percent / 100D);

            System.Drawing.Rectangle fillBounds = new System.Drawing.Rectangle(
                barBounds.Left,
                barBounds.Top,
                fillWidth,
                barBounds.Height);

            using (System.Drawing.SolidBrush emptyBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(230, 230, 230)))
            using (System.Drawing.SolidBrush fillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(90, 140, 210)))
            {
                e.Graphics.FillRectangle(emptyBrush, barBounds);

                if (fillBounds.Width > 0)
                {
                    e.Graphics.FillRectangle(fillBrush, fillBounds);
                }
            }

            string text = percent.ToString("0.0") + " %";
            int textCenterX = e.CellBounds.Left + e.CellBounds.Width / 2;
            bool textCenterIsInsideFill = fillBounds.Width > 0 &&
                textCenterX <= fillBounds.Right;
            System.Drawing.Color textColor = textCenterIsInsideFill
                ? System.Drawing.Color.White
                : System.Drawing.Color.Black;

            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.CellStyle.Font,
                e.CellBounds,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
            e.Handled = true;
        }

        private void Chart_TableGridChart_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.RowIndex >= Rows.Count)
                return;

            if (Rows[e.RowIndex].DataBoundItem is not EntryChartItem entryChartItem)
                return;

            e.ToolTipText = FormatFileSystemDateToolTip(entryChartItem.FullPath);
        }

        private string FormatFileSystemDateToolTip(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return string.Empty;

            try
            {
                DateTime creationTime;
                DateTime lastWriteTime;
                DateTime lastAccessTime;

                if (Directory.Exists(fullPath))
                {
                    creationTime = Directory.GetCreationTime(fullPath);
                    lastWriteTime = Directory.GetLastWriteTime(fullPath);
                    lastAccessTime = Directory.GetLastAccessTime(fullPath);
                }
                else if (File.Exists(fullPath))
                {
                    creationTime = File.GetCreationTime(fullPath);
                    lastWriteTime = File.GetLastWriteTime(fullPath);
                    lastAccessTime = File.GetLastAccessTime(fullPath);
                }
                else
                {
                    return string.Empty;
                }

                return string.Format(
                    LocalizationService.GetText("Chart.TooltipDates"),
                    creationTime,
                    Environment.NewLine,
                    lastWriteTime,
                    lastAccessTime);
            }
            catch
            {
                return string.Empty;
            }
        }

        private sealed class EntryChartItem
        {
            public string Name { get; set; }
            public string FullPath { get; set; }
            public long SizeBytes { get; set; }
            public string FormattedSize { get; set; }
            public double Percent { get; set; }
        }

    }
}