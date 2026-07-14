using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Lucid.Controls.GridView;
using Lucid.Theming;

namespace WTF
{
    public sealed class AdvancedFeaturesForm : Form
    {
        private sealed class FileTypeRow
        {
            public string Extension { get; set; }
            public int FileCount { get; set; }
            public string FormattedSize { get; set; }
            public long SizeBytes { get; set; }
        }

        private sealed class LargestFileRow
        {
            public string Name { get; set; }
            public string FormattedSize { get; set; }
            public long SizeBytes { get; set; }
            public DateTime LastWriteTime { get; set; }
            public string FullPath { get; set; }
        }

        private readonly FileSystemEntry _rootEntry;
        private readonly LucidDataGridView _fileTypeGrid = new LucidDataGridView();
        private readonly LucidDataGridView _largestFilesGrid = new LucidDataGridView();
        private List<FileTypeRow> _fileTypeRows = new List<FileTypeRow>();
        private List<LargestFileRow> _largestFileRows = new List<LargestFileRow>();
        private string _fileTypeSortProperty = nameof(FileTypeRow.SizeBytes);
        private bool _fileTypeSortAscending;
        private string _largestFilesSortProperty = nameof(LargestFileRow.SizeBytes);
        private bool _largestFilesSortAscending;

        public AdvancedFeaturesForm(FileSystemEntry rootEntry, AppSettings settings, Chart_TableGridChart entryGrid)
        {
            _rootEntry = rootEntry ?? throw new ArgumentNullException(nameof(rootEntry));

            LucidThemeService.Apply(settings.Layout);

            Text = LocalizationService.GetText("Advanced.Title");
            Icon = AppResources.ApplicationIcon;
            Width = 1050;
            Height = 700;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ThemeProvider.Theme.Colors.BackgroundPrimary;
            ForeColor = ThemeProvider.Theme.Colors.TextPrimary;

            AnalysisTabControl tabs = new AnalysisTabControl
            {
                Dock = DockStyle.Fill
            };
            tabs.TabPages.Add(CreateFileTypesPage());
            tabs.TabPages.Add(CreateLargestFilesPage());

            Controls.Add(tabs);

            WindowsFormStyler.Apply(this, settings.Layout);
            ApplyTheme();
            RefreshData();
        }

        private TabPage CreateFileTypesPage()
        {
            ConfigureGrid(_fileTypeGrid);
            _fileTypeGrid.AutoGenerateColumns = false;
            _fileTypeGrid.ColumnHeaderMouseClick += FileTypeGrid_ColumnHeaderMouseClick;

            _fileTypeGrid.Columns.Add(CreateTextColumn(
                "ColumnExtension",
                LocalizationService.GetText("Advanced.FileType"),
                nameof(FileTypeRow.Extension)));

            _fileTypeGrid.Columns.Add(CreateTextColumn(
                "ColumnFileCount",
                LocalizationService.GetText("Advanced.Files"),
                nameof(FileTypeRow.FileCount)));

            _fileTypeGrid.Columns.Add(CreateTextColumn(
                "ColumnFormattedSize",
                LocalizationService.GetText("Common.Size"),
                nameof(FileTypeRow.FormattedSize)));

            _fileTypeGrid.Columns.Add(CreateTextColumn(
                "ColumnSizeBytes",
                LocalizationService.GetText("Advanced.Bytes"),
                nameof(FileTypeRow.SizeBytes)));

            return CreatePage(LocalizationService.GetText("Advanced.FileTypes"), _fileTypeGrid);
        }

        private TabPage CreateLargestFilesPage()
        {
            ConfigureGrid(_largestFilesGrid);
            _largestFilesGrid.AutoGenerateColumns = false;
            _largestFilesGrid.ColumnHeaderMouseClick += LargestFilesGrid_ColumnHeaderMouseClick;
            _largestFilesGrid.CellDoubleClick += OpenSelectedFile;

            _largestFilesGrid.Columns.Add(CreateTextColumn(
                "ColumnName",
                LocalizationService.GetText("Common.Name"),
                nameof(LargestFileRow.Name)));

            _largestFilesGrid.Columns.Add(CreateTextColumn(
                "ColumnFormattedSize",
                LocalizationService.GetText("Common.Size"),
                nameof(LargestFileRow.FormattedSize)));

            _largestFilesGrid.Columns.Add(CreateTextColumn(
                "ColumnSizeBytes",
                LocalizationService.GetText("Advanced.Bytes"),
                nameof(LargestFileRow.SizeBytes)));

            _largestFilesGrid.Columns.Add(CreateTextColumn(
                "ColumnLastWriteTime",
                LocalizationService.GetText("Advanced.Modified"),
                nameof(LargestFileRow.LastWriteTime)));

            _largestFilesGrid.Columns.Add(CreateTextColumn(
                "ColumnFullPath",
                LocalizationService.GetText("Common.Path"),
                nameof(LargestFileRow.FullPath)));

            return CreatePage(LocalizationService.GetText("Advanced.LargestFiles"), _largestFilesGrid);
        }

        private void RefreshData()
        {
            List<FileSystemEntry> files = GetFiles();

            _fileTypeRows = files
                .GroupBy(file => string.IsNullOrWhiteSpace(Path.GetExtension(file.Name))
                    ? LocalizationService.GetText("Advanced.NoExtension")
                    : Path.GetExtension(file.Name).ToLowerInvariant())
                .Select(group => new FileTypeRow
                {
                    Extension = group.Key,
                    FileCount = group.Count(),
                    FormattedSize = SizeFormatter.Format(group.Sum(file => file.SizeBytes)),
                    SizeBytes = group.Sum(file => file.SizeBytes)
                })
                .ToList();

            _largestFileRows = files
                .OrderByDescending(file => file.SizeBytes)
                .Take(1000)
                .Select(file => new LargestFileRow
                {
                    Name = file.Name,
                    FormattedSize = SizeFormatter.Format(file.SizeBytes),
                    SizeBytes = file.SizeBytes,
                    LastWriteTime = file.LastWriteTimeUtc == DateTime.MinValue
                        ? DateTime.MinValue
                        : file.LastWriteTimeUtc.ToLocalTime(),
                    FullPath = file.FullPath
                })
                .ToList();

            BindFileTypes();
            BindLargestFiles();
        }

        private void FileTypeGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            string propertyName = _fileTypeGrid.Columns[e.ColumnIndex].DataPropertyName;
            _fileTypeSortAscending = _fileTypeSortProperty == propertyName && !_fileTypeSortAscending;
            _fileTypeSortProperty = propertyName;
            BindFileTypes();
        }

        private void LargestFilesGrid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            string propertyName = _largestFilesGrid.Columns[e.ColumnIndex].DataPropertyName;
            _largestFilesSortAscending = _largestFilesSortProperty == propertyName && !_largestFilesSortAscending;
            _largestFilesSortProperty = propertyName;
            BindLargestFiles();
        }

        private void BindFileTypes()
        {
            IEnumerable<FileTypeRow> sortedRows = _fileTypeSortProperty switch
            {
                nameof(FileTypeRow.Extension) => _fileTypeSortAscending
                    ? _fileTypeRows.OrderBy(row => row.Extension, StringComparer.CurrentCultureIgnoreCase)
                    : _fileTypeRows.OrderByDescending(row => row.Extension, StringComparer.CurrentCultureIgnoreCase),
                nameof(FileTypeRow.FileCount) => _fileTypeSortAscending
                    ? _fileTypeRows.OrderBy(row => row.FileCount)
                    : _fileTypeRows.OrderByDescending(row => row.FileCount),
                nameof(FileTypeRow.FormattedSize) or nameof(FileTypeRow.SizeBytes) => _fileTypeSortAscending
                    ? _fileTypeRows.OrderBy(row => row.SizeBytes)
                    : _fileTypeRows.OrderByDescending(row => row.SizeBytes),
                _ => _fileTypeRows.OrderByDescending(row => row.SizeBytes)
            };

            _fileTypeGrid.DataSource = sortedRows.ToList();
            ApplySortGlyph(_fileTypeGrid, _fileTypeSortProperty, _fileTypeSortAscending);
        }

        private void BindLargestFiles()
        {
            IEnumerable<LargestFileRow> sortedRows = _largestFilesSortProperty switch
            {
                nameof(LargestFileRow.Name) => _largestFilesSortAscending
                    ? _largestFileRows.OrderBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase)
                    : _largestFileRows.OrderByDescending(row => row.Name, StringComparer.CurrentCultureIgnoreCase),
                nameof(LargestFileRow.FormattedSize) or nameof(LargestFileRow.SizeBytes) => _largestFilesSortAscending
                    ? _largestFileRows.OrderBy(row => row.SizeBytes)
                    : _largestFileRows.OrderByDescending(row => row.SizeBytes),
                nameof(LargestFileRow.LastWriteTime) => _largestFilesSortAscending
                    ? _largestFileRows.OrderBy(row => row.LastWriteTime)
                    : _largestFileRows.OrderByDescending(row => row.LastWriteTime),
                nameof(LargestFileRow.FullPath) => _largestFilesSortAscending
                    ? _largestFileRows.OrderBy(row => row.FullPath, StringComparer.CurrentCultureIgnoreCase)
                    : _largestFileRows.OrderByDescending(row => row.FullPath, StringComparer.CurrentCultureIgnoreCase),
                _ => _largestFileRows.OrderByDescending(row => row.SizeBytes)
            };

            _largestFilesGrid.DataSource = sortedRows.ToList();
            ApplySortGlyph(_largestFilesGrid, _largestFilesSortProperty, _largestFilesSortAscending);
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string headerText, string dataPropertyName)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                DataPropertyName = dataPropertyName,
                SortMode = DataGridViewColumnSortMode.Programmatic,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells
            };
        }

        private static void ApplySortGlyph(LucidDataGridView grid, string propertyName, bool ascending)
        {
            foreach (DataGridViewColumn column in grid.Columns)
            {
                column.HeaderCell.SortGlyphDirection = column.DataPropertyName == propertyName
                    ? ascending ? SortOrder.Ascending : SortOrder.Descending
                    : SortOrder.None;
            }
        }

        private List<FileSystemEntry> GetFiles()
        {
            if (_rootEntry.AllFiles != null && _rootEntry.AllFiles.Count > 0)
                return _rootEntry.AllFiles.Where(file => file != null && !file.IsDirectory).ToList();

            List<FileSystemEntry> files = new List<FileSystemEntry>();
            CollectFiles(_rootEntry, files);
            return files;
        }

        private static void CollectFiles(FileSystemEntry entry, List<FileSystemEntry> files)
        {
            if (entry == null)
                return;

            foreach (FileSystemEntry child in entry.Children)
            {
                if (child.IsDirectory)
                    CollectFiles(child, files);
                else
                    files.Add(child);
            }
        }

        private static void ConfigureGrid(LucidDataGridView grid)
        {
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToOrderColumns = true;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.RowHeadersVisible = false;
            grid.BorderStyle = BorderStyle.None;
            grid.BackgroundColor = ThemeProvider.Theme.Colors.BackgroundPrimary;
            grid.BackColor = ThemeProvider.Theme.Colors.BackgroundPrimary;
            grid.ForeColor = ThemeProvider.Theme.Colors.TextPrimary;
        }

        private static TabPage CreatePage(string title, Control control)
        {
            TabPage page = new TabPage(title)
            {
                BackColor = ThemeProvider.Theme.Colors.BackgroundPrimary,
                ForeColor = ThemeProvider.Theme.Colors.TextPrimary,
                Padding = Padding.Empty
            };

            control.Dock = DockStyle.Fill;
            page.Controls.Add(control);
            return page;
        }

        private void ApplyTheme()
        {
            BackColor = ThemeProvider.Theme.Colors.BackgroundPrimary;
            ForeColor = ThemeProvider.Theme.Colors.TextPrimary;

            ApplyGridTheme(_fileTypeGrid);
            ApplyGridTheme(_largestFilesGrid);
        }

        private static void ApplyGridTheme(LucidDataGridView grid)
        {
            Color backgroundColor = ThemeProvider.Theme.Colors.BackgroundPrimary;
            Color headerColor = ThemeProvider.Theme.Colors.BackgroundSecondary;
            Color textColor = ThemeProvider.Theme.Colors.TextPrimary;
            Color borderColor = ThemeProvider.Theme.Colors.SurfaceHighlight;

            grid.BackgroundColor = backgroundColor;
            grid.BackColor = backgroundColor;
            grid.ForeColor = textColor;
            grid.GridColor = borderColor;
            grid.EnableHeadersVisualStyles = false;

        }

        private sealed class AnalysisTabControl : TabControl
        {
            public AnalysisTabControl()
            {
                DrawMode = TabDrawMode.OwnerDrawFixed;
                Appearance = TabAppearance.FlatButtons;
                SizeMode = TabSizeMode.Fixed;
                ItemSize = new Size(120, 30);
                Padding = new Point(12, 4);
            }

            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                Rectangle tabBounds = GetTabRect(e.Index);
                bool selected = SelectedIndex == e.Index;

                Color backColor = selected
                    ? ThemeProvider.Theme.Colors.BackgroundTertiary
                    : ThemeProvider.Theme.Colors.BackgroundSecondary;
                Color textColor = ThemeProvider.Theme.Colors.TextPrimary;
                Color borderColor = ThemeProvider.Theme.Colors.SurfaceHighlight;

                using (SolidBrush backBrush = new SolidBrush(backColor))
                using (Pen borderPen = new Pen(borderColor))
                {
                    e.Graphics.FillRectangle(backBrush, tabBounds);
                    e.Graphics.DrawRectangle(
                        borderPen,
                        tabBounds.Left,
                        tabBounds.Top,
                        tabBounds.Width - 1,
                        tabBounds.Height - 1);

                    TextRenderer.DrawText(
                        e.Graphics,
                        TabPages[e.Index].Text,
                        Font,
                        tabBounds,
                        textColor,
                        TextFormatFlags.HorizontalCenter |
                        TextFormatFlags.VerticalCenter |
                        TextFormatFlags.EndEllipsis);

                    if (selected)
                    {
                        using (Pen accentPen = new Pen(
                            ThemeProvider.Theme.Colors.Accent,
                            2F))
                        {
                            e.Graphics.DrawLine(
                                accentPen,
                                tabBounds.Left + 1,
                                tabBounds.Bottom - 2,
                                tabBounds.Right - 2,
                                tabBounds.Bottom - 2);
                        }
                    }
                }
            }
        }

        private void OpenSelectedFile(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            LargestFileRow selectedRow = _largestFilesGrid.Rows[e.RowIndex].DataBoundItem as LargestFileRow;
            string path = selectedRow?.FullPath;

            if (string.IsNullOrWhiteSpace(path))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = File.Exists(path) ? "/select,\"" + path + "\"" : "\"" + path + "\"",
                UseShellExecute = true
            });
        }
    }
}
