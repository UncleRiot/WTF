using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Lucid.Controls;
using Lucid.Controls.GridView;
using Lucid.Theming;

namespace WTF
{
    public sealed class StorageHistoryForm : Form
    {
        private readonly AppSettings _settings;
        private readonly ShellIconService _shellIconService;
        private readonly ComboBox comboBoxPaths;
        private readonly LucidComboBox comboBoxDisplayMode;
        private readonly DataGridView dataGridViewRecords;
        private readonly StorageHistoryChart storageHistoryChart;
        private readonly TrackBar trackBarGradientIntensity;
        private readonly LucidLabel labelGradientIntensityValue;
        private readonly LucidButton buttonDelete;
        private readonly LucidButton buttonClose;
        private IReadOnlyList<StorageHistoryRecord> _currentRecords = Array.Empty<StorageHistoryRecord>();
        private List<StorageHistoryRow> _currentRows = new List<StorageHistoryRow>();
        private string _sortColumnName = "ColumnDate";
        private SortOrder _sortOrder = SortOrder.Descending;

        public StorageHistoryForm(AppSettings settings)
        {
            _settings = settings;
            _shellIconService = new ShellIconService();
            LucidThemeService.Apply(_settings.Layout);

            bool useDarkMode = IsDarkMode();
            Color windowBackColor = useDarkMode
                ? Color.FromArgb(32, 32, 32)
                : Color.White;
            Color controlBackColor = useDarkMode
                ? Color.FromArgb(45, 45, 45)
                : Color.White;
            Color textColor = useDarkMode
                ? Color.White
                : Color.Black;

            Text = LocalizationService.GetText("StorageHistory.Title");
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(900, 500);
            Size = new Size(1120, 650);

            if (_settings.HasStorageHistoryWindowBounds &&
                _settings.StorageHistoryWindowWidth >= MinimumSize.Width &&
                _settings.StorageHistoryWindowHeight >= MinimumSize.Height)
            {
                Rectangle savedBounds = new Rectangle(
                    _settings.StorageHistoryWindowLeft,
                    _settings.StorageHistoryWindowTop,
                    _settings.StorageHistoryWindowWidth,
                    _settings.StorageHistoryWindowHeight);

                if (Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(savedBounds)))
                {
                    StartPosition = FormStartPosition.Manual;
                    Bounds = savedBounds;
                }
            }

            LucidLabel labelPath = new LucidLabel
            {
                AutoSize = true,
                Text = LocalizationService.GetText("StorageHistory.Path"),
                Anchor = AnchorStyles.Left
            };

            comboBoxPaths = new StorageHistoryPathComboBox(_shellIconService)
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                FlatStyle = FlatStyle.Flat,
                IntegralHeight = false,
                ItemHeight = 20,
                Size = new Size(260, 28),
                DropDownWidth = 260,
                Anchor = AnchorStyles.Left
            };
            comboBoxPaths.SelectedIndexChanged += comboBoxPaths_SelectedIndexChanged;

            LucidLabel labelDisplayMode = new LucidLabel
            {
                AutoSize = true,
                Text = LocalizationService.GetText("StorageHistory.Display"),
                Anchor = AnchorStyles.Left
            };

            comboBoxDisplayMode = new LucidComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Anchor = AnchorStyles.Left
            };
            comboBoxDisplayMode.Items.Add(new StorageHistoryDisplayModeItem(
                StorageHistoryDisplayMode.UsedSpace,
                LocalizationService.GetText("StorageHistory.Used")));
            comboBoxDisplayMode.Items.Add(new StorageHistoryDisplayModeItem(
                StorageHistoryDisplayMode.FreeSpace,
                LocalizationService.GetText("StorageHistory.Free")));
            comboBoxDisplayMode.SelectedIndexChanged += comboBoxDisplayMode_SelectedIndexChanged;

            LucidLabel labelGradientIntensity = new LucidLabel
            {
                AutoSize = true,
                Text = "Intensity:",
                Anchor = AnchorStyles.Left
            };

            trackBarGradientIntensity = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 10,
                SmallChange = 5,
                LargeChange = 10,
                AutoSize = false,
                Height = 28,
                Width = 140,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BackColor = controlBackColor,
                ForeColor = textColor,
                Value = Clamp(_settings.StorageHistoryGradientIntensityPercent, 0, 100)
            };
            trackBarGradientIntensity.ValueChanged += trackBarGradientIntensity_ValueChanged;

            labelGradientIntensityValue = new LucidLabel
            {
                AutoSize = false,
                Width = 44,
                Height = 28,
                Text = trackBarGradientIntensity.Value.ToString() + "%",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };

            TableLayoutPanel gradientIntensityPanel = new TableLayoutPanel
            {
                AutoSize = false,
                BackColor = windowBackColor,
                ForeColor = textColor,
                Width = 188,
                Height = 28,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Anchor = AnchorStyles.Left
            };
            gradientIntensityPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            gradientIntensityPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));
            gradientIntensityPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            gradientIntensityPanel.Controls.Add(trackBarGradientIntensity, 0, 0);
            gradientIntensityPanel.Controls.Add(labelGradientIntensityValue, 1, 0);

            buttonDelete = new LucidButton
            {
                AutoSize = true,
                Text = LocalizationService.GetText("StorageHistory.Delete"),
                ButtonStyle = LucidButtonStyle.Normal,
                Anchor = AnchorStyles.Left
            };
            buttonDelete.Click += buttonDelete_Click;

            buttonClose = new LucidButton
            {
                AutoSize = true,
                Text = LocalizationService.GetText("Common.Close"),
                DialogResult = DialogResult.OK,
                ButtonStyle = LucidButtonStyle.Normal
            };

            TableLayoutPanel pathLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = windowBackColor,
                ForeColor = textColor,
                AutoSize = true,
                ColumnCount = 7,
                RowCount = 1,
                Padding = new Padding(8)
            };
            pathLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 188F));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.Controls.Add(labelPath, 0, 0);
            pathLayout.Controls.Add(comboBoxPaths, 1, 0);
            pathLayout.Controls.Add(labelDisplayMode, 2, 0);
            pathLayout.Controls.Add(comboBoxDisplayMode, 3, 0);
            pathLayout.Controls.Add(labelGradientIntensity, 4, 0);
            pathLayout.Controls.Add(gradientIntensityPanel, 5, 0);
            pathLayout.Controls.Add(buttonDelete, 6, 0);

            dataGridViewRecords = new StorageHistoryDataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                AutoGenerateColumns = false,
                ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable,
                EditMode = DataGridViewEditMode.EditProgrammatically,
                BackgroundColor = windowBackColor,
                BackColor = windowBackColor,
                ForeColor = textColor,
                BorderStyle = BorderStyle.FixedSingle,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dataGridViewRecords.CellFormatting += dataGridViewRecords_CellFormatting;
            dataGridViewRecords.ColumnHeaderMouseClick += dataGridViewRecords_ColumnHeaderMouseClick;
            dataGridViewRecords.DataBindingComplete += dataGridViewRecords_DataBindingComplete;

            dataGridViewRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnDate",
                HeaderText = LocalizationService.GetText("StorageHistory.Date"),
                DataPropertyName = "Date",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 45F,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });
            dataGridViewRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnSize",
                HeaderText = LocalizationService.GetText("StorageHistory.Used"),
                DataPropertyName = "Size",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 30F,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });
            dataGridViewRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnChange",
                HeaderText = LocalizationService.GetText("StorageHistory.Change"),
                DataPropertyName = "Change",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 25F,
                SortMode = DataGridViewColumnSortMode.Programmatic
            });

            storageHistoryChart = new StorageHistoryChart
            {
                Dock = DockStyle.Fill
            };
            storageHistoryChart.ApplyTheme(useDarkMode);
            storageHistoryChart.SetGradientIntensity(trackBarGradientIntensity.Value);

            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                BackColor = windowBackColor,
                ForeColor = textColor,
                Orientation = Orientation.Vertical
            };
            splitContainer.Panel1.Padding = new Padding(16, 0, 0, 8);
            splitContainer.Panel2.Padding = new Padding(12, 0, 8, 0);
            splitContainer.Panel1.Controls.Add(dataGridViewRecords);

            FlowLayoutPanel bottomLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = windowBackColor,
                ForeColor = textColor,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            bottomLayout.Controls.Add(buttonClose);

            TableLayoutPanel chartLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = windowBackColor,
                ForeColor = textColor,
                RowCount = 2,
                ColumnCount = 1,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            chartLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            chartLayout.Controls.Add(storageHistoryChart, 0, 0);
            chartLayout.Controls.Add(bottomLayout, 0, 1);
            splitContainer.Panel2.Controls.Add(chartLayout);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = windowBackColor,
                ForeColor = textColor,
                RowCount = 2,
                ColumnCount = 1
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(pathLayout, 0, 0);
            mainLayout.Controls.Add(splitContainer, 0, 1);

            Controls.Add(mainLayout);
            AcceptButton = buttonClose;
            CancelButton = buttonClose;

            Shown += (sender, e) =>
            {
                splitContainer.Panel1MinSize = 280;
                splitContainer.Panel2MinSize = 320;
                splitContainer.SplitterDistance = 416;
                ApplyHistoryGridScrollBarTheme();
            };

            BackColor = windowBackColor;
            ForeColor = textColor;
            WindowsFormStyler.Apply(this, _settings.Layout);

            comboBoxPaths.BackColor = controlBackColor;
            comboBoxPaths.ForeColor = textColor;
            comboBoxDisplayMode.BackColor = controlBackColor;
            comboBoxDisplayMode.ForeColor = textColor;
            dataGridViewRecords.BackgroundColor = windowBackColor;
            dataGridViewRecords.BackColor = windowBackColor;
            dataGridViewRecords.ForeColor = textColor;
            ApplyHistoryGridScrollBarTheme();

            comboBoxDisplayMode.SelectedIndex = 1;
            LoadPaths();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            Rectangle windowBounds = WindowState == FormWindowState.Normal
                ? Bounds
                : RestoreBounds;

            _settings.StorageHistoryGradientIntensityPercent = trackBarGradientIntensity.Value;

            if (windowBounds.Width >= MinimumSize.Width &&
                windowBounds.Height >= MinimumSize.Height)
            {
                _settings.HasStorageHistoryWindowBounds = true;
                _settings.StorageHistoryWindowLeft = windowBounds.Left;
                _settings.StorageHistoryWindowTop = windowBounds.Top;
                _settings.StorageHistoryWindowWidth = windowBounds.Width;
                _settings.StorageHistoryWindowHeight = windowBounds.Height;
            }

            try
            {
                _settings.Save();
            }
            catch
            {
            }
        }

        private void LoadPaths()
        {
            string selectedPath = GetSelectedHistoryPath();
            IReadOnlyList<string> paths = StorageHistoryService.GetPaths();

            comboBoxPaths.BeginUpdate();
            comboBoxPaths.Items.Clear();

            foreach (string path in paths)
            {
                comboBoxPaths.Items.Add(new StorageHistoryPathItem(
                    path,
                    GetHistoryPathDisplayName(path)));
            }

            comboBoxPaths.EndUpdate();

            if (comboBoxPaths.Items.Count == 0)
            {
                BindRecords(Array.Empty<StorageHistoryRecord>());
                buttonDelete.Enabled = false;
                return;
            }

            int selectedIndex = 0;

            if (selectedPath != null)
            {
                for (int index = 0; index < comboBoxPaths.Items.Count; index++)
                {
                    if (comboBoxPaths.Items[index] is StorageHistoryPathItem item &&
                        string.Equals(item.Path, selectedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedIndex = index;
                        break;
                    }
                }
            }

            comboBoxPaths.SelectedIndex = selectedIndex;
            buttonDelete.Enabled = true;
        }

        private void comboBoxPaths_SelectedIndexChanged(object sender, EventArgs e)
        {
            string path = GetSelectedHistoryPath();
            BindRecords(StorageHistoryService.GetRecords(path));
        }

        private void comboBoxDisplayMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindRecords(_currentRecords);
        }

        private void trackBarGradientIntensity_ValueChanged(object sender, EventArgs e)
        {
            labelGradientIntensityValue.Text = trackBarGradientIntensity.Value.ToString() + "%";
            _settings.StorageHistoryGradientIntensityPercent = trackBarGradientIntensity.Value;
            storageHistoryChart.SetGradientIntensity(trackBarGradientIntensity.Value);
        }

        private void comboBoxPaths_DrawItem(object sender, DrawItemEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            object item = e.Index >= 0
                ? comboBox.Items[e.Index]
                : comboBox.SelectedItem;

            if (item == null)
                return;

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            Color backgroundColor = isSelected
                ? SystemColors.Highlight
                : comboBox.BackColor;

            Color textColor = isSelected
                ? SystemColors.HighlightText
                : comboBox.ForeColor;

            using (SolidBrush backgroundBrush = new SolidBrush(backgroundColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            }

            string text = item.ToString();

            string iconPath = item is StorageHistoryPathItem pathItem
                ? pathItem.Path
                : string.Empty;

            int iconLeft = e.Bounds.Left + 3;
            int iconTop = e.Bounds.Top + Math.Max(0, (e.Bounds.Height - 16) / 2);

            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                using Bitmap icon = _shellIconService.GetSmallSystemIcon(iconPath);
                e.Graphics.DrawImage(icon, iconLeft, iconTop, 16, 16);
            }

            Rectangle textBounds = new Rectangle(
                e.Bounds.Left + 24,
                e.Bounds.Top,
                Math.Max(0, e.Bounds.Width - 26),
                e.Bounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                comboBox.Font,
                textBounds,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.DrawFocusRectangle();
        }

        private void dataGridViewRecords_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            ApplyHistoryGridScrollBarTheme();
        }

        private void dataGridViewRecords_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            bool isSelected = dataGridViewRecords.Rows[e.RowIndex].Selected;
            bool isOddRow = e.RowIndex % 2 != 0;

            if (isSelected)
            {
                e.CellStyle.BackColor = SystemColors.Highlight;
                e.CellStyle.ForeColor = SystemColors.HighlightText;
                e.CellStyle.SelectionBackColor = SystemColors.Highlight;
                e.CellStyle.SelectionForeColor = SystemColors.HighlightText;
                return;
            }

            bool useDarkMode = IsDarkMode();

            e.CellStyle.BackColor = isOddRow
                ? (useDarkMode ? Color.FromArgb(24, 24, 24) : Color.FromArgb(245, 245, 245))
                : (useDarkMode ? Color.FromArgb(61, 66, 68) : Color.White);
            e.CellStyle.ForeColor = useDarkMode ? Color.White : Color.Black;
            e.CellStyle.SelectionBackColor = SystemColors.Highlight;
            e.CellStyle.SelectionForeColor = SystemColors.HighlightText;
        }

        private void dataGridViewRecords_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0)
                return;

            string columnName = dataGridViewRecords.Columns[e.ColumnIndex].Name;

            if (string.Equals(_sortColumnName, columnName, StringComparison.Ordinal))
            {
                _sortOrder = _sortOrder == SortOrder.Ascending
                    ? SortOrder.Descending
                    : SortOrder.Ascending;
            }
            else
            {
                _sortColumnName = columnName;
                _sortOrder = columnName == "ColumnDate"
                    ? SortOrder.Descending
                    : SortOrder.Ascending;
            }

            ApplyRecordSort();
        }

        private void BindRecords(IReadOnlyList<StorageHistoryRecord> records)
        {
            _currentRecords = records ?? Array.Empty<StorageHistoryRecord>();

            List<StorageHistoryRecord> orderedRecords = _currentRecords
                .OrderBy(record => record.RecordedAtUtc)
                .ToList();
            List<StorageHistoryRow> rows = new List<StorageHistoryRow>();
            long? previousSize = null;
            StorageHistoryDisplayMode displayMode = GetDisplayMode();

            foreach (StorageHistoryRecord record in orderedRecords)
            {
                long currentSize = GetDisplayValue(record, displayMode);
                long? change = previousSize.HasValue ? currentSize - previousSize.Value : null;

                rows.Add(new StorageHistoryRow
                {
                    DateValue = record.RecordedAtUtc.ToLocalTime(),
                    SizeValue = currentSize,
                    ChangeValue = change,
                    Date = record.RecordedAtUtc.ToLocalTime().ToString("g"),
                    Size = SizeFormatter.Format(currentSize),
                    Change = change.HasValue
                        ? (change.Value >= 0L ? "+" : "-") + SizeFormatter.Format(Math.Abs(change.Value))
                        : string.Empty
                });

                previousSize = currentSize;
            }

            _currentRows = rows;
            dataGridViewRecords.Columns["ColumnSize"].HeaderText = LocalizationService.GetText(
                displayMode == StorageHistoryDisplayMode.FreeSpace
                    ? "StorageHistory.Free"
                    : "StorageHistory.Used");

            ApplyRecordSort();
            storageHistoryChart.SetGradientIntensity(trackBarGradientIntensity.Value);
            storageHistoryChart.SetRecords(orderedRecords, displayMode);
        }

        private void ApplyRecordSort()
        {
            IEnumerable<StorageHistoryRow> sortedRows;

            switch (_sortColumnName)
            {
                case "ColumnSize":
                    sortedRows = _sortOrder == SortOrder.Ascending
                        ? _currentRows.OrderBy(row => row.SizeValue)
                        : _currentRows.OrderByDescending(row => row.SizeValue);
                    break;

                case "ColumnChange":
                    sortedRows = _sortOrder == SortOrder.Ascending
                        ? _currentRows
                            .OrderBy(row => row.ChangeValue.HasValue ? 0 : 1)
                            .ThenBy(row => row.ChangeValue.GetValueOrDefault())
                        : _currentRows
                            .OrderBy(row => row.ChangeValue.HasValue ? 0 : 1)
                            .ThenByDescending(row => row.ChangeValue.GetValueOrDefault());
                    break;

                default:
                    sortedRows = _sortOrder == SortOrder.Ascending
                        ? _currentRows.OrderBy(row => row.DateValue)
                        : _currentRows.OrderByDescending(row => row.DateValue);
                    break;
            }

            dataGridViewRecords.DataSource = sortedRows.ToList();

            ApplyRecordSortHeaderState();

            if (IsHandleCreated)
                BeginInvoke(new MethodInvoker(ApplyHistoryGridScrollBarTheme));
        }

        private void ApplyRecordSortHeaderState()
        {
            string sizeHeaderText = LocalizationService.GetText(
                GetDisplayMode() == StorageHistoryDisplayMode.FreeSpace
                    ? "StorageHistory.Free"
                    : "StorageHistory.Used");

            SetRecordColumnHeader("ColumnDate", LocalizationService.GetText("StorageHistory.Date"));
            SetRecordColumnHeader("ColumnSize", sizeHeaderText);
            SetRecordColumnHeader("ColumnChange", LocalizationService.GetText("StorageHistory.Change"));

            foreach (DataGridViewColumn column in dataGridViewRecords.Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            if (dataGridViewRecords.Columns.Contains(_sortColumnName))
            {
                DataGridViewColumn sortedColumn = dataGridViewRecords.Columns[_sortColumnName];
                sortedColumn.HeaderCell.SortGlyphDirection = _sortOrder;
            }
        }

        private void SetRecordColumnHeader(string columnName, string headerText)
        {
            if (dataGridViewRecords.Columns.Contains(columnName))
                dataGridViewRecords.Columns[columnName].HeaderText = headerText;
        }

        private StorageHistoryDisplayMode GetDisplayMode()
        {
            if (comboBoxDisplayMode.SelectedItem is StorageHistoryDisplayModeItem item)
                return item.DisplayMode;

            return StorageHistoryDisplayMode.FreeSpace;
        }

        private static long GetDisplayValue(StorageHistoryRecord record, StorageHistoryDisplayMode displayMode)
        {
            if (displayMode == StorageHistoryDisplayMode.FreeSpace)
            {
                if (record.TotalCapacityBytes > 0L)
                {
                    return Math.Max(
                        0L,
                        Math.Min(record.TotalCapacityBytes, record.FreeSpaceBytes));
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

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            string path = GetSelectedHistoryPath();

            if (string.IsNullOrWhiteSpace(path))
                return;

            DialogResult result = MessageBox.Show(
                this,
                LocalizationService.GetText("StorageHistory.DeleteConfirm"),
                LocalizationService.GetText("StorageHistory.Title"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            StorageHistoryService.DeleteRecords(path);
            LoadPaths();
        }

        private string GetSelectedHistoryPath()
        {
            if (comboBoxPaths.SelectedItem is StorageHistoryPathItem item)
                return item.Path;

            return comboBoxPaths.Text == null
                ? string.Empty
                : comboBoxPaths.Text.Trim();
        }

        private static string GetHistoryPathDisplayName(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            try
            {
                string fullPath = Path.GetFullPath(path);
                string rootPath = Path.GetPathRoot(fullPath);

                if (!string.IsNullOrWhiteSpace(rootPath))
                {
                    DriveInfo driveInfo = new DriveInfo(rootPath);

                    string label = string.IsNullOrWhiteSpace(driveInfo.VolumeLabel)
                        ? LocalizationService.GetText("Drive.LocalDisk")
                        : driveInfo.VolumeLabel;

                    return LocalizationService.Format("Drive.Display", label, rootPath);
                }
            }
            catch
            {
            }

            return path;
        }

        private bool IsDarkMode()
        {
            if (_settings.Layout == AppLayout.WindowsDarkMode)
                return true;

            if (_settings.Layout == AppLayout.WindowsLightMode)
                return false;

            try
            {
                using Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

                object value = key?.GetValue("AppsUseLightTheme");

                if (value is int appsUseLightTheme)
                {
                    return appsUseLightTheme == 0;
                }
            }
            catch
            {
            }

            return false;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            if (value < minimum)
                return minimum;

            if (value > maximum)
                return maximum;

            return value;
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        private void ApplyHistoryGridScrollBarTheme()
        {
            if (!dataGridViewRecords.IsHandleCreated)
                return;

            try
            {
                string themeName = IsDarkMode() ? "DarkMode_Explorer" : "Explorer";

                SetWindowTheme(dataGridViewRecords.Handle, themeName, null);

                foreach (Control child in dataGridViewRecords.Controls)
                {
                    if (child.IsHandleCreated)
                        SetWindowTheme(child.Handle, themeName, null);
                }

                dataGridViewRecords.Invalidate();
            }
            catch
            {
            }
        }

        private sealed class StorageHistoryPathComboBox : ComboBox
        {
            private readonly ShellIconService _shellIconService;

            public StorageHistoryPathComboBox(ShellIconService shellIconService)
            {
                _shellIconService = shellIconService;
            }

            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                if (e.Index < 0)
                    return;

                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

                Color backgroundColor = isSelected
                    ? SystemColors.Highlight
                    : BackColor;

                Color textColor = isSelected
                    ? SystemColors.HighlightText
                    : ForeColor;

                using (SolidBrush backgroundBrush = new SolidBrush(backgroundColor))
                {
                    e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
                }

                object item = Items[e.Index];

                string text = item == null
                    ? string.Empty
                    : item.ToString();

                string iconPath = GetItemIconPath(item);

                int iconLeft = e.Bounds.Left + 3;
                int iconTop = e.Bounds.Top + Math.Max(0, (e.Bounds.Height - 16) / 2);

                if (!string.IsNullOrWhiteSpace(iconPath))
                {
                    using Bitmap icon = _shellIconService.GetSmallSystemIcon(iconPath);
                    e.Graphics.DrawImage(icon, iconLeft, iconTop, 16, 16);
                }

                Rectangle textBounds = new Rectangle(
                    e.Bounds.Left + 24,
                    e.Bounds.Top,
                    Math.Max(0, e.Bounds.Width - 26),
                    e.Bounds.Height);

                TextRenderer.DrawText(
                    e.Graphics,
                    text,
                    Font,
                    textBounds,
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                e.DrawFocusRectangle();
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                const int WM_PAINT = 0x000F;

                if (m.Msg != WM_PAINT ||
                    DrawMode != DrawMode.OwnerDrawFixed ||
                    DropDownStyle != ComboBoxStyle.DropDownList ||
                    SelectedItem == null)
                {
                    return;
                }

                using Graphics graphics = CreateGraphics();

                Rectangle bounds = new Rectangle(
                    1,
                    1,
                    Math.Max(0, Width - SystemInformation.VerticalScrollBarWidth - 3),
                    Math.Max(0, Height - 2));

                using (SolidBrush backgroundBrush = new SolidBrush(BackColor))
                {
                    graphics.FillRectangle(backgroundBrush, bounds);
                }

                string iconPath = GetItemIconPath(SelectedItem);
                int iconLeft = bounds.Left + 3;
                int iconTop = bounds.Top + Math.Max(0, (bounds.Height - 16) / 2);

                if (!string.IsNullOrWhiteSpace(iconPath))
                {
                    using Bitmap icon = _shellIconService.GetSmallSystemIcon(iconPath);
                    graphics.DrawImage(icon, iconLeft, iconTop, 16, 16);
                }

                Rectangle textBounds = new Rectangle(
                    bounds.Left + 24,
                    bounds.Top,
                    Math.Max(0, bounds.Width - 26),
                    bounds.Height);

                TextRenderer.DrawText(
                    graphics,
                    SelectedItem.ToString(),
                    Font,
                    textBounds,
                    ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            private static string GetItemIconPath(object item)
            {
                if (item is StorageHistoryPathItem pathItem)
                    return pathItem.Path;

                if (item is string path)
                {
                    if (Directory.Exists(path))
                        return path;

                    if (File.Exists(path))
                        return path;
                }

                return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            }
        }

        private sealed class StorageHistoryDataGridView : DataGridView
        {
            private const int WM_MOUSEMOVE = 0x0200;
            private const int MK_LBUTTON = 0x0001;

            public StorageHistoryDataGridView()
            {
                AllowDrop = false;
                AllowUserToOrderColumns = false;
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_MOUSEMOVE && (((int)m.WParam) & MK_LBUTTON) == MK_LBUTTON)
                    return;

                base.WndProc(ref m);
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                    return;

                base.OnMouseMove(e);
            }

            protected override void OnCellMouseMove(DataGridViewCellMouseEventArgs e)
            {
                if ((MouseButtons & MouseButtons.Left) == MouseButtons.Left)
                    return;

                base.OnCellMouseMove(e);
            }

            protected override void OnDragEnter(DragEventArgs drgevent)
            {
                drgevent.Effect = DragDropEffects.None;
            }

            protected override void OnDragOver(DragEventArgs drgevent)
            {
                drgevent.Effect = DragDropEffects.None;
            }

            protected override void OnDragDrop(DragEventArgs drgevent)
            {
                drgevent.Effect = DragDropEffects.None;
            }
        }


        private sealed class StorageHistoryPathItem
        {
            public StorageHistoryPathItem(string path, string displayName)
            {
                Path = path;
                DisplayName = displayName;
            }

            public string Path { get; }
            public string DisplayName { get; }

            public override string ToString()
            {
                return DisplayName;
            }
        }

        private sealed class StorageHistoryDisplayModeItem
        {
            public StorageHistoryDisplayModeItem(StorageHistoryDisplayMode displayMode, string text)
            {
                DisplayMode = displayMode;
                Text = text;
            }

            public StorageHistoryDisplayMode DisplayMode { get; }
            public string Text { get; }

            public override string ToString()
            {
                return Text;
            }
        }

        private sealed class StorageHistoryRow
        {
            public DateTime DateValue { get; set; }
            public long SizeValue { get; set; }
            public long? ChangeValue { get; set; }
            public string Date { get; set; }
            public string Size { get; set; }
            public string Change { get; set; }
        }
    }
}
