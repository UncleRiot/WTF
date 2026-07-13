using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WTF
{
    public sealed class StorageHistoryForm : Form
    {
        private readonly AppSettings _settings;
        private readonly ComboBox comboBoxPaths;
        private readonly ComboBox comboBoxDisplayMode;
        private readonly DataGridView dataGridViewRecords;
        private readonly StorageHistoryChart storageHistoryChart;
        private readonly Button buttonDelete;
        private readonly Button buttonClose;
        private IReadOnlyList<StorageHistoryRecord> _currentRecords = Array.Empty<StorageHistoryRecord>();

        public StorageHistoryForm(AppSettings settings)
        {
            _settings = settings;

            Text = LocalizationService.GetText("StorageHistory.Title");
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(800, 500);
            Size = new Size(1050, 650);

            Label labelPath = new Label
            {
                AutoSize = true,
                Text = LocalizationService.GetText("StorageHistory.Path"),
                Anchor = AnchorStyles.Left
            };

            comboBoxPaths = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            comboBoxPaths.SelectedIndexChanged += comboBoxPaths_SelectedIndexChanged;

            Label labelDisplayMode = new Label
            {
                AutoSize = true,
                Text = LocalizationService.GetText("StorageHistory.Display"),
                Anchor = AnchorStyles.Left
            };

            comboBoxDisplayMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130
            };
            comboBoxDisplayMode.Items.Add(new StorageHistoryDisplayModeItem(
                StorageHistoryDisplayMode.UsedSpace,
                LocalizationService.GetText("StorageHistory.Used")));
            comboBoxDisplayMode.Items.Add(new StorageHistoryDisplayModeItem(
                StorageHistoryDisplayMode.FreeSpace,
                LocalizationService.GetText("StorageHistory.Free")));
            comboBoxDisplayMode.SelectedIndexChanged += comboBoxDisplayMode_SelectedIndexChanged;

            buttonDelete = new Button
            {
                AutoSize = true,
                Text = LocalizationService.GetText("StorageHistory.Delete")
            };
            buttonDelete.Click += buttonDelete_Click;

            buttonClose = new Button
            {
                AutoSize = true,
                Text = LocalizationService.GetText("Common.Close"),
                DialogResult = DialogResult.OK
            };

            TableLayoutPanel pathLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 5,
                Padding = new Padding(8)
            };
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLayout.Controls.Add(labelPath, 0, 0);
            pathLayout.Controls.Add(comboBoxPaths, 1, 0);
            pathLayout.Controls.Add(labelDisplayMode, 2, 0);
            pathLayout.Controls.Add(comboBoxDisplayMode, 3, 0);
            pathLayout.Controls.Add(buttonDelete, 4, 0);

            dataGridViewRecords = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            dataGridViewRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnDate",
                HeaderText = LocalizationService.GetText("StorageHistory.Date"),
                DataPropertyName = "Date",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 45F
            });
            dataGridViewRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnSize",
                HeaderText = LocalizationService.GetText("StorageHistory.Used"),
                DataPropertyName = "Size",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 30F
            });
            dataGridViewRecords.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnChange",
                HeaderText = LocalizationService.GetText("StorageHistory.Change"),
                DataPropertyName = "Change",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 25F
            });

            storageHistoryChart = new StorageHistoryChart
            {
                Dock = DockStyle.Fill
            };

            SplitContainer splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            splitContainer.Panel1.Controls.Add(dataGridViewRecords);
            splitContainer.Panel2.Controls.Add(storageHistoryChart);

            FlowLayoutPanel bottomLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            bottomLayout.Controls.Add(buttonClose);

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.Controls.Add(pathLayout, 0, 0);
            mainLayout.Controls.Add(splitContainer, 0, 1);
            mainLayout.Controls.Add(bottomLayout, 0, 2);

            Controls.Add(mainLayout);
            AcceptButton = buttonClose;
            CancelButton = buttonClose;

            Shown += (sender, e) =>
            {
                splitContainer.Panel1MinSize = 280;
                splitContainer.Panel2MinSize = 320;
                splitContainer.SplitterDistance = 400;
            };

            WindowsFormStyler.Apply(this, _settings.Layout);
            comboBoxDisplayMode.SelectedIndex = 0;
            LoadPaths();
        }

        private void LoadPaths()
        {
            string selectedPath = comboBoxPaths.SelectedItem as string;
            IReadOnlyList<string> paths = StorageHistoryService.GetPaths();

            comboBoxPaths.BeginUpdate();
            comboBoxPaths.Items.Clear();

            foreach (string path in paths)
            {
                comboBoxPaths.Items.Add(path);
            }

            comboBoxPaths.EndUpdate();

            if (comboBoxPaths.Items.Count == 0)
            {
                BindRecords(Array.Empty<StorageHistoryRecord>());
                buttonDelete.Enabled = false;
                return;
            }

            int selectedIndex = selectedPath == null
                ? 0
                : comboBoxPaths.FindStringExact(selectedPath);

            comboBoxPaths.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            buttonDelete.Enabled = true;
        }

        private void comboBoxPaths_SelectedIndexChanged(object sender, EventArgs e)
        {
            string path = comboBoxPaths.SelectedItem as string;
            BindRecords(StorageHistoryService.GetRecords(path));
        }

        private void comboBoxDisplayMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindRecords(_currentRecords);
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
                    Date = record.RecordedAtUtc.ToLocalTime().ToString("g"),
                    Size = SizeFormatter.Format(currentSize),
                    Change = change.HasValue
                        ? (change.Value >= 0L ? "+" : "-") + SizeFormatter.Format(Math.Abs(change.Value))
                        : string.Empty
                });

                previousSize = currentSize;
            }

            rows.Reverse();
            dataGridViewRecords.DataSource = rows;
            dataGridViewRecords.Columns["ColumnSize"].HeaderText = LocalizationService.GetText(
                displayMode == StorageHistoryDisplayMode.FreeSpace
                    ? "StorageHistory.Free"
                    : "StorageHistory.Used");
            storageHistoryChart.SetRecords(orderedRecords, displayMode);
        }

        private StorageHistoryDisplayMode GetDisplayMode()
        {
            if (comboBoxDisplayMode.SelectedItem is StorageHistoryDisplayModeItem item)
                return item.DisplayMode;

            return StorageHistoryDisplayMode.UsedSpace;
        }

        private static long GetDisplayValue(
            StorageHistoryRecord record,
            StorageHistoryDisplayMode displayMode)
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
            string path = comboBoxPaths.SelectedItem as string;

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

        private sealed class StorageHistoryDisplayModeItem
        {
            public StorageHistoryDisplayModeItem(
                StorageHistoryDisplayMode displayMode,
                string text)
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
            public string Date { get; set; }
            public string Size { get; set; }
            public string Change { get; set; }
        }
    }
}
