using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WTF
{
    public sealed class Chart_TableGridChart : DataGridView
    {
        private bool _applyingColumnWidths;

        public Chart_TableGridChart()
        {
            ConfigureTableGridChart();

            ParentChanged += Chart_TableGridChart_ParentChanged;
            SizeChanged += Chart_TableGridChart_SizeChanged;
            CellPainting += Chart_TableGridChart_CellPainting;
            CellToolTipTextNeeded += Chart_TableGridChart_CellToolTipTextNeeded;
            ColumnWidthChanged += Chart_TableGridChart_ColumnWidthChanged;
        }

        public void SetEntry(FileSystemEntry entry)
        {
            if (entry == null)
            {
                DataSource = null;
                FitToCurrentBounds();
                Invalidate();
                return;
            }

            long totalSize = entry.Children.Sum(child => child.SizeBytes);

            List<EntryChartItem> rows = entry.Children
                .OrderByDescending(child => child.SizeBytes)
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
            DataSource = rows;
            RemoveUnexpectedColumns();
            FitToCurrentBounds();
            Invalidate();
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
                SortMode = DataGridViewColumnSortMode.NotSortable,
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
                SortMode = DataGridViewColumnSortMode.NotSortable,
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
                SortMode = DataGridViewColumnSortMode.NotSortable,
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
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Resizable = DataGridViewTriState.True
            });

            ConfigureEntryGridColumns();
            RemoveUnexpectedColumns();
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
            Dock = DockStyle.Fill;
            AutoSize = false;
            MinimumSize = System.Drawing.Size.Empty;
            MaximumSize = System.Drawing.Size.Empty;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            ScrollBars = ScrollBars.Vertical;

            RemoveUnexpectedColumns();

            if (!Columns.Contains("ColumnName"))
                return;

            if (!Columns.Contains("ColumnSize"))
                return;

            if (!Columns.Contains("ColumnPercent"))
                return;

            if (!Columns.Contains("ColumnPath"))
                return;

            if (TryApplySavedColumnWidths())
                return;

            int availableWidth = ClientSize.Width - 2;

            if (RowCount > 0 && DisplayedRowCount(false) < RowCount)
            {
                availableWidth -= SystemInformation.VerticalScrollBarWidth;
            }

            availableWidth = Math.Max(availableWidth, 8);

            int nameWidth = CalculateTextColumnWidth("ColumnName", 40);
            int sizeWidth = CalculateTextColumnWidth("ColumnSize", 40);
            int percentWidth = CalculatePercentColumnDefaultWidth();

            int pathWidth = availableWidth - nameWidth - sizeWidth - percentWidth;

            if (pathWidth < 2)
            {
                pathWidth = 2;
            }

            ApplyColumnWidths(nameWidth, sizeWidth, percentWidth, pathWidth);
        }

        private bool TryApplySavedColumnWidths()
        {
            AppSettings settings = AppSettings.Load();

            if (!settings.HasEntryColumnLayout)
                return false;

            if (settings.EntryColumnNameWidth <= 0)
                return false;

            if (settings.EntryColumnSizeWidth <= 0)
                return false;

            if (settings.EntryColumnPercentWidth <= 0)
                return false;

            if (settings.EntryColumnPathWidth <= 0)
                return false;

            ApplyColumnWidths(
                settings.EntryColumnNameWidth,
                settings.EntryColumnSizeWidth,
                settings.EntryColumnPercentWidth,
                settings.EntryColumnPathWidth);

            return true;
        }

        private void ApplyColumnWidths(int nameWidth, int sizeWidth, int percentWidth, int pathWidth)
        {
            SuspendLayout();
            _applyingColumnWidths = true;

            try
            {
                HorizontalScrollingOffset = 0;

                Columns["ColumnName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                Columns["ColumnSize"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                Columns["ColumnPercent"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                Columns["ColumnPath"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

                int minimumNameWidth = CalculateTextColumnWidth("ColumnName", 2);
                int minimumSizeWidth = CalculateTextColumnWidth("ColumnSize", 2);
                int minimumPercentWidth = CalculatePercentColumnDefaultWidth();

                Columns["ColumnName"].MinimumWidth = minimumNameWidth;
                Columns["ColumnSize"].MinimumWidth = minimumSizeWidth;
                Columns["ColumnPercent"].MinimumWidth = minimumPercentWidth;
                Columns["ColumnPath"].MinimumWidth = 2;

                Columns["ColumnName"].Width = Math.Max(minimumNameWidth, nameWidth);
                Columns["ColumnSize"].Width = Math.Max(minimumSizeWidth, sizeWidth);
                Columns["ColumnPercent"].Width = Math.Max(minimumPercentWidth, percentWidth);
                Columns["ColumnPath"].Width = Math.Max(2, pathWidth);
            }
            finally
            {
                _applyingColumnWidths = false;
                ResumeLayout();
            }
        }

        private int CalculatePercentColumnDefaultWidth()
        {
            int headerWidth = TextRenderer.MeasureText(
                LocalizationService.GetText("Chart.TableUsage"),
                Font).Width + 15;

            int textWidth = TextRenderer.MeasureText("100.0 %", Font).Width + 15;

            return Math.Max(48, Math.Max(headerWidth, textWidth));
        }

        private int CalculateTextColumnWidth(string columnName, int fallbackWidth)
        {
            if (!Columns.Contains(columnName))
                return fallbackWidth;

            DataGridViewColumn column = Columns[columnName];

            int width = TextRenderer.MeasureText(column.HeaderText ?? string.Empty, Font).Width + 15;

            foreach (DataGridViewRow row in Rows)
            {
                if (row.IsNewRow)
                    continue;

                object value = row.Cells[column.Index].Value;
                string text = value == null ? string.Empty : value.ToString();

                int textWidth = TextRenderer.MeasureText(text, Font).Width + 15;

                if (textWidth > width)
                {
                    width = textWidth;
                }
            }

            return Math.Max(fallbackWidth, width);
        }

        private void Chart_TableGridChart_ParentChanged(object sender, EventArgs e)
        {
            FitToCurrentBounds();
        }

        private void Chart_TableGridChart_SizeChanged(object sender, EventArgs e)
        {
            FitToCurrentBounds();
            Invalidate();
        }

        private void Chart_TableGridChart_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            if (_applyingColumnWidths)
                return;

            SaveEntryGridColumnWidths();
        }

        private void SaveEntryGridColumnWidths()
        {
            RemoveUnexpectedColumns();

            if (!Columns.Contains("ColumnName"))
                return;

            if (!Columns.Contains("ColumnSize"))
                return;

            if (!Columns.Contains("ColumnPercent"))
                return;

            if (!Columns.Contains("ColumnPath"))
                return;

            AppSettings settings = AppSettings.Load();

            settings.HasEntryColumnLayout = true;
            settings.EntryColumnNameWidth = Columns["ColumnName"].Width;
            settings.EntryColumnSizeWidth = Columns["ColumnSize"].Width;
            settings.EntryColumnPercentWidth = Columns["ColumnPercent"].Width;
            settings.EntryColumnPathWidth = Columns["ColumnPath"].Width;

            settings.Save();
        }

        private void Chart_TableGridChart_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
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

            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.CellStyle.Font,
                e.CellBounds,
                e.CellStyle.ForeColor,
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
    }
}