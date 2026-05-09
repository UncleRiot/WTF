using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WTF
{
    public sealed class MainForm : Form
    {
        private readonly AppSettings _settings;
        private readonly DriveService _driveService;
        private readonly CsvExportService _csvExportService;

        private CancellationTokenSource _scanCancellationTokenSource;
        private FileSystemEntry _currentRootEntry;

        private MenuStrip menuStripMain;
        private ToolStripMenuItem menuItemFile;
        private ToolStripMenuItem menuItemExportCsv;
        private ToolStripMenuItem menuItemSettings;
        private ToolStripMenuItem menuItemExit;
        private ToolStripMenuItem menuItemHelp;
        private ToolStripMenuItem menuItemAbout;
        private ToolStripPanel toolStripPanelMain;
        private ToolStrip toolStripMain;
        private ToolStripLabel toolStripLabelDrive;
        private ToolStripComboBox toolStripComboBoxDrives;
        private ToolStripButton toolStripButtonScan;
        private SplitContainer splitContainerMain;
        private SplitContainer splitContainerLeft;
        private TreeView treeViewEntries;
        private ImageList imageListEntries;
        private ListView listViewPartitions;
        private ImageList imageListPartitions;
        private DataGridView dataGridViewEntries;
        private StatusStrip statusStripMain;
        private ToolStripStatusLabel toolStripStatusLabel;

        private const int SHGFI_ICON = 0x100;
        private const int SHGFI_SMALLICON = 0x1;

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool GetDiskFreeSpace(
            string lpRootPathName,
            out uint lpSectorsPerCluster,
            out uint lpBytesPerSector,
            out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public MainForm()
        {
            _settings = AppSettings.Load();
            _driveService = new DriveService();
            _csvExportService = new CsvExportService();

            InitializeComponent();

            if (listViewPartitions != null)
            {
                listViewPartitions.SizeChanged += listViewPartitions_SizeChanged;
            }

            ModernFormStyler.Apply(this, _settings.Layout);
            toolStripMain.GripStyle = ToolStripGripStyle.Visible;
            LoadDrives();
            LoadPartitionList();
            UpdatePartitionPanelVisibility();
        }

        private void InitializeComponent()
        {
            Text = "WTF - Where’s The Filespace";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new System.Drawing.Size(780, 490);
            Size = new System.Drawing.Size(1180, 760);
            MaximizeBox = true;
            SizeGripStyle = SizeGripStyle.Show;

            menuStripMain = new MenuStrip();
            menuStripMain.Padding = new Padding(0, 2, 0, 2);

            menuItemFile = new ToolStripMenuItem("Datei");
            menuItemExportCsv = new ToolStripMenuItem("Export CSV");
            menuItemSettings = new ToolStripMenuItem("Einstellungen");
            menuItemExit = new ToolStripMenuItem("Beenden");
            menuItemHelp = new ToolStripMenuItem("Hilfe");
            menuItemAbout = new ToolStripMenuItem("Über");

            menuItemFile.DropDownItems.Add(menuItemExportCsv);
            menuItemFile.DropDownItems.Add(new ToolStripSeparator());
            menuItemFile.DropDownItems.Add(menuItemSettings);
            menuItemFile.DropDownItems.Add(new ToolStripSeparator());
            menuItemFile.DropDownItems.Add(menuItemExit);
            menuItemHelp.DropDownItems.Add(menuItemAbout);
            menuStripMain.Items.Add(menuItemFile);
            menuStripMain.Items.Add(menuItemHelp);

            menuItemExportCsv.Click += menuItemExportCsv_Click;
            menuItemSettings.Click += menuItemSettings_Click;
            menuItemExit.Click += menuItemExit_Click;
            menuItemAbout.Click += menuItemAbout_Click;

            toolStripPanelMain = new ToolStripPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            toolStripMain = new ToolStrip();
            toolStripMain.Dock = DockStyle.None;
            toolStripMain.GripStyle = ToolStripGripStyle.Visible;
            toolStripMain.AllowItemReorder = true;
            toolStripMain.Padding = new Padding(0);
            toolStripMain.Margin = new Padding(0);

            toolStripLabelDrive = new ToolStripLabel("Laufwerk:");
            toolStripComboBoxDrives = new ToolStripComboBox();
            toolStripButtonScan = new ToolStripButton("▶");

            toolStripLabelDrive.Margin = new Padding(0, 1, 0, 2);
            toolStripComboBoxDrives.DropDownStyle = ComboBoxStyle.DropDownList;
            toolStripComboBoxDrives.AutoSize = false;
            toolStripComboBoxDrives.Width = 260;
            toolStripButtonScan.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonScan.ToolTipText = "Scan starten";
            toolStripButtonScan.Click += toolStripButtonScan_Click;

            toolStripMain.Items.Add(toolStripLabelDrive);
            toolStripMain.Items.Add(toolStripComboBoxDrives);
            toolStripMain.Items.Add(toolStripButtonScan);
            toolStripPanelMain.Join(toolStripMain, 0, 0);

            splitContainerMain = new SplitContainer();
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.Size = new System.Drawing.Size(1180, 650);
            splitContainerMain.FixedPanel = FixedPanel.Panel1;
            splitContainerMain.Panel1MinSize = 220;
            splitContainerMain.Panel2MinSize = 320;
            splitContainerMain.SplitterDistance = 360;

            splitContainerLeft = new SplitContainer();
            splitContainerLeft.Dock = DockStyle.Fill;
            splitContainerLeft.Size = new System.Drawing.Size(360, 650);
            splitContainerLeft.Orientation = Orientation.Horizontal;
            splitContainerLeft.FixedPanel = FixedPanel.Panel2;
            splitContainerLeft.Panel1MinSize = 180;
            splitContainerLeft.Panel2MinSize = 90;
            splitContainerLeft.SplitterDistance = 470;

            imageListEntries = new ImageList();
            imageListEntries.ColorDepth = ColorDepth.Depth32Bit;
            imageListEntries.ImageSize = new System.Drawing.Size(16, 16);
            imageListEntries.Images.Add("Drive", GetSmallSystemIcon(Environment.SystemDirectory));
            imageListEntries.Images.Add("Folder", GetSmallSystemIcon(Environment.GetFolderPath(Environment.SpecialFolder.Windows)));
            imageListEntries.Images.Add("File", System.Drawing.SystemIcons.Application.ToBitmap());

            treeViewEntries = new TreeView();
            treeViewEntries.Dock = DockStyle.Fill;
            treeViewEntries.HideSelection = false;
            treeViewEntries.ShowLines = true;
            treeViewEntries.ShowPlusMinus = true;
            treeViewEntries.ShowRootLines = true;
            treeViewEntries.DrawMode = TreeViewDrawMode.OwnerDrawText;
            treeViewEntries.ItemHeight = 22;
            treeViewEntries.FullRowSelect = true;
            treeViewEntries.ImageList = imageListEntries;
            treeViewEntries.AfterSelect += treeViewEntries_AfterSelect;
            treeViewEntries.DrawNode += treeViewEntries_DrawNode;

            imageListPartitions = new ImageList();
            imageListPartitions.ColorDepth = ColorDepth.Depth32Bit;
            imageListPartitions.ImageSize = new System.Drawing.Size(16, 16);

            listViewPartitions = new ListView();
            listViewPartitions.Dock = DockStyle.Fill;
            listViewPartitions.View = View.Details;
            listViewPartitions.FullRowSelect = true;
            listViewPartitions.GridLines = false;
            listViewPartitions.HeaderStyle = ColumnHeaderStyle.Clickable;
            listViewPartitions.SmallImageList = imageListPartitions;
            listViewPartitions.Columns.Add("Name", 120);
            listViewPartitions.Columns.Add("Größe", 80, HorizontalAlignment.Right);
            listViewPartitions.Columns.Add("Frei", 80, HorizontalAlignment.Right);
            listViewPartitions.Columns.Add("% Frei", 70, HorizontalAlignment.Right);

            dataGridViewEntries = new DataGridView();
            dataGridViewEntries.Dock = DockStyle.Fill;
            dataGridViewEntries.AllowUserToAddRows = false;
            dataGridViewEntries.AllowUserToDeleteRows = false;
            dataGridViewEntries.AllowUserToResizeRows = false;
            dataGridViewEntries.AutoGenerateColumns = false;
            dataGridViewEntries.ReadOnly = true;
            dataGridViewEntries.RowHeadersVisible = false;
            dataGridViewEntries.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewEntries.MultiSelect = false;

            dataGridViewEntries.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnName",
                HeaderText = "Name",
                DataPropertyName = "Name",
                Width = 220
            });

            dataGridViewEntries.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnSize",
                HeaderText = "Größe",
                DataPropertyName = "FormattedSize",
                Width = 110
            });

            dataGridViewEntries.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnSizeBytes",
                HeaderText = "Bytes",
                DataPropertyName = "SizeBytes",
                Width = 120
            });

            dataGridViewEntries.Columns.Add(new SizeBarColumn
            {
                Name = "ColumnPercent",
                HeaderText = "Anteil",
                DataPropertyName = "Percent",
                Width = 160
            });

            dataGridViewEntries.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnPath",
                HeaderText = "Pfad",
                DataPropertyName = "FullPath",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            splitContainerLeft.Panel1.Controls.Add(treeViewEntries);
            splitContainerLeft.Panel2.Controls.Add(listViewPartitions);

            splitContainerMain.Panel1.Controls.Add(splitContainerLeft);
            splitContainerMain.Panel2.Controls.Add(dataGridViewEntries);

            statusStripMain = new StatusStrip();
            statusStripMain.SizingGrip = true;
            toolStripStatusLabel = new ToolStripStatusLabel("Bereit");
            statusStripMain.Items.Add(toolStripStatusLabel);

            TableLayoutPanel tableLayoutPanelMain = new TableLayoutPanel();
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.ColumnCount = 1;
            tableLayoutPanelMain.RowCount = 4;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            menuStripMain.Dock = DockStyle.Fill;
            toolStripPanelMain.Dock = DockStyle.Fill;
            splitContainerMain.Dock = DockStyle.Fill;
            statusStripMain.Dock = DockStyle.Fill;

            tableLayoutPanelMain.Controls.Add(menuStripMain, 0, 0);
            tableLayoutPanelMain.Controls.Add(toolStripPanelMain, 0, 1);
            tableLayoutPanelMain.Controls.Add(splitContainerMain, 0, 2);
            tableLayoutPanelMain.Controls.Add(statusStripMain, 0, 3);

            Controls.Add(tableLayoutPanelMain);

            MainMenuStrip = menuStripMain;
        }

        private System.Drawing.Bitmap GetSmallSystemIcon(string path)
        {
            SHFILEINFO shellFileInfo = new SHFILEINFO();

            IntPtr result = SHGetFileInfo(
                path,
                0,
                ref shellFileInfo,
                (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(SHFILEINFO)),
                SHGFI_ICON | SHGFI_SMALLICON);

            if (result == IntPtr.Zero || shellFileInfo.hIcon == IntPtr.Zero)
            {
                return System.Drawing.SystemIcons.Application.ToBitmap();
            }

            try
            {
                using System.Drawing.Icon icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(shellFileInfo.hIcon).Clone();
                return icon.ToBitmap();
            }
            finally
            {
                DestroyIcon(shellFileInfo.hIcon);
            }
        }

        private void LoadDrives()
        {
            List<DriveItem> drives = _driveService.GetReadyDrives();

            toolStripComboBoxDrives.Items.Clear();

            foreach (DriveItem driveItem in drives)
            {
                toolStripComboBoxDrives.Items.Add(driveItem);
            }

            if (toolStripComboBoxDrives.Items.Count > 0)
            {
                toolStripComboBoxDrives.SelectedIndex = 0;
                UpdateStatusStripForDrive(((DriveItem)toolStripComboBoxDrives.SelectedItem).RootPath);
            }
        }

        private void LoadPartitionList()
        {
            listViewPartitions.BeginUpdate();
            listViewPartitions.Items.Clear();
            imageListPartitions.Images.Clear();

            foreach (System.IO.DriveInfo driveInfo in System.IO.DriveInfo.GetDrives())
            {
                if (!driveInfo.IsReady)
                    continue;

                string rootPath = driveInfo.RootDirectory.FullName;
                imageListPartitions.Images.Add(rootPath, GetSmallSystemIcon(rootPath));

                long totalSize = driveInfo.TotalSize;
                long freeSpace = driveInfo.AvailableFreeSpace;
                int freePercent = totalSize <= 0 ? 0 : (int)Math.Round((double)freeSpace * 100D / totalSize);

                ListViewItem item = new ListViewItem(rootPath, rootPath);
                item.SubItems.Add(SizeFormatter.Format(totalSize));
                item.SubItems.Add(SizeFormatter.Format(freeSpace));
                item.SubItems.Add(freePercent + " %");

                listViewPartitions.Items.Add(item);
            }

            AdjustPartitionColumns();
            listViewPartitions.EndUpdate();
        }
        private void listViewPartitions_SizeChanged(object sender, EventArgs e)
        {
            AdjustPartitionColumns();
        }
        private void AdjustPartitionColumns()
        {
            if (listViewPartitions.Columns.Count != 4)
                return;

            int fixedWidth =
                listViewPartitions.Columns[0].Width +
                listViewPartitions.Columns[1].Width +
                listViewPartitions.Columns[2].Width;

            int remainingWidth = listViewPartitions.ClientSize.Width - fixedWidth - 1;

            listViewPartitions.Columns[3].Width = Math.Max(70, remainingWidth);
        }

        private void UpdatePartitionPanelVisibility()
        {
            splitContainerLeft.Panel2Collapsed = !_settings.ShowPartitionPanel;
        }

        private void UpdateStatusStripForDrive(string rootPath)
        {
            try
            {
                System.IO.DriveInfo driveInfo = new System.IO.DriveInfo(rootPath);
                long clusterSize = GetClusterSize(rootPath);
                string driveName = driveInfo.Name.TrimEnd('\\');

                toolStripStatusLabel.Text = string.Format(
                    "Freier Speicherplatz {0}: {1} (von {2}), Clustersize: {3}",
                    driveName,
                    SizeFormatter.Format(driveInfo.AvailableFreeSpace),
                    SizeFormatter.Format(driveInfo.TotalSize),
                    clusterSize);
            }
            catch
            {
                toolStripStatusLabel.Text = "Bereit";
            }
        }
        private long GetClusterSize(string rootPath)
        {
            bool success = GetDiskFreeSpace(
                rootPath,
                out uint sectorsPerCluster,
                out uint bytesPerSector,
                out uint numberOfFreeClusters,
                out uint totalNumberOfClusters);

            if (!success)
            {
                return 0;
            }

            return (long)sectorsPerCluster * bytesPerSector;
        }

        private void treeViewEntries_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null)
                return;

            FileSystemEntry entry = e.Node.Tag as FileSystemEntry;

            if (entry == null)
            {
                e.DrawDefault = true;
                return;
            }

            bool selected = (e.State & TreeNodeStates.Selected) == TreeNodeStates.Selected;

            System.Drawing.Rectangle rowBounds = new System.Drawing.Rectangle(
                e.Bounds.Left,
                e.Bounds.Top,
                treeViewEntries.ClientSize.Width - e.Bounds.Left,
                e.Bounds.Height);

            using (System.Drawing.SolidBrush backgroundBrush = new System.Drawing.SolidBrush(selected ? System.Drawing.SystemColors.Highlight : treeViewEntries.BackColor))
            {
                e.Graphics.FillRectangle(backgroundBrush, rowBounds);
            }

            long totalSizeBytes = _currentRootEntry == null ? entry.SizeBytes : _currentRootEntry.SizeBytes;
            double percent = totalSizeBytes <= 0 ? 0D : (double)entry.SizeBytes * 100D / totalSizeBytes;

            int maxBarWidth = Math.Max(0, treeViewEntries.ClientSize.Width - e.Bounds.Left - 6);
            int barWidth = (int)(maxBarWidth * Math.Max(0D, Math.Min(100D, percent)) / 100D);

            if (barWidth > 0)
            {
                System.Drawing.Rectangle barBounds = new System.Drawing.Rectangle(
                    e.Bounds.Left,
                    e.Bounds.Top + 2,
                    barWidth,
                    e.Bounds.Height - 4);

                using (System.Drawing.SolidBrush barBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(110, 130, 120, 255)))
                {
                    e.Graphics.FillRectangle(barBrush, barBounds);
                }
            }

            string text = SizeFormatter.Format(entry.SizeBytes) + "  " + entry.Name;

            TextRenderer.DrawText(
                e.Graphics,
                text,
                treeViewEntries.Font,
                rowBounds,
                selected ? System.Drawing.SystemColors.HighlightText : treeViewEntries.ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private async void toolStripButtonScan_Click(object sender, EventArgs e)
        {
            if (_scanCancellationTokenSource != null)
            {
                _scanCancellationTokenSource.Cancel();
                return;
            }

            if (toolStripComboBoxDrives.SelectedItem is not DriveItem driveItem)
            {
                MessageBox.Show(this, "Kein Laufwerk ausgewählt.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await StartScanAsync(driveItem.RootPath);
        }

        private async Task StartScanAsync(string rootPath)
        {
            SetScanningState(true);
            treeViewEntries.Nodes.Clear();
            dataGridViewEntries.DataSource = null;
            _currentRootEntry = null;

            _scanCancellationTokenSource = new CancellationTokenSource();

            Progress<ScanProgress> progress = new Progress<ScanProgress>(scanProgress =>
            {
                toolStripStatusLabel.Text = string.Format(
                    "Scan: {0} | {1} | Ordner: {2} | Dateien: {3}",
                    scanProgress.CurrentPath,
                    SizeFormatter.Format(scanProgress.ScannedBytes),
                    scanProgress.ScannedDirectories,
                    scanProgress.ScannedFiles);
            });

            try
            {
                DirectoryScanner directoryScanner = new DirectoryScanner(_settings);
                _currentRootEntry = await directoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                RenderScanResult(_currentRootEntry);
                LoadPartitionList();
                UpdateStatusStripForDrive(rootPath);
            }
            catch (OperationCanceledException)
            {
                toolStripStatusLabel.Text = "Scan abgebrochen";
            }
            finally
            {
                _scanCancellationTokenSource.Dispose();
                _scanCancellationTokenSource = null;
                SetScanningState(false);
            }
        }

        private void SetScanningState(bool scanning)
        {
            toolStripButtonScan.Text = scanning ? "■" : "▶";
            toolStripButtonScan.ToolTipText = scanning ? "Scan abbrechen" : "Scan starten";
            toolStripComboBoxDrives.Enabled = !scanning;
            menuItemExportCsv.Enabled = !scanning && _currentRootEntry != null;
            splitContainerMain.IsSplitterFixed = scanning;
            splitContainerLeft.IsSplitterFixed = scanning;
        }

        private void RenderScanResult(FileSystemEntry rootEntry)
        {
            TreeNode rootNode = CreateTreeNode(rootEntry);
            treeViewEntries.BeginUpdate();
            treeViewEntries.Nodes.Clear();
            treeViewEntries.Nodes.Add(rootNode);
            rootNode.Expand();
            treeViewEntries.SelectedNode = rootNode;
            treeViewEntries.TopNode = rootNode;
            rootNode.EnsureVisible();
            treeViewEntries.EndUpdate();

            BindGrid(rootEntry);
        }

        private TreeNode CreateTreeNode(FileSystemEntry entry)
        {
            TreeNode node = new TreeNode(string.Format("{0} ({1})", entry.Name, SizeFormatter.Format(entry.SizeBytes)))
            {
                Tag = entry,
                ImageKey = entry.IsDirectory ? "Folder" : "File",
                SelectedImageKey = entry.IsDirectory ? "Folder" : "File"
            };

            if (entry.FullPath != null && entry.FullPath.EndsWith(":\\", StringComparison.OrdinalIgnoreCase))
            {
                node.ImageKey = "Drive";
                node.SelectedImageKey = "Drive";
            }

            foreach (FileSystemEntry child in entry.Children.Where(child => child.IsDirectory || _settings.ShowFilesInTree))
            {
                node.Nodes.Add(CreateTreeNode(child));
            }

            return node;
        }

        private void BindGrid(FileSystemEntry entry)
        {
            long totalSize = entry.Children.Sum(child => child.SizeBytes);

            var rows = entry.Children
                .OrderByDescending(child => child.SizeBytes)
                .Select(child => new
                {
                    child.Name,
                    child.FullPath,
                    child.SizeBytes,
                    FormattedSize = SizeFormatter.Format(child.SizeBytes),
                    Percent = totalSize <= 0 ? 0 : (double)child.SizeBytes * 100D / totalSize
                })
                .ToList();

            dataGridViewEntries.DataSource = rows;
        }

        private void treeViewEntries_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null && e.Node.Tag is FileSystemEntry entry)
            {
                BindGrid(entry);
            }
        }

        private void menuItemExportCsv_Click(object sender, EventArgs e)
        {
            if (_currentRootEntry == null)
                return;

            using SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = _csvExportService.FileFilter,
                FileName = "wtf-scan.csv"
            };

            if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            _csvExportService.Export(saveFileDialog.FileName, new[] { _currentRootEntry });
            toolStripStatusLabel.Text = "Export gespeichert: " + saveFileDialog.FileName;
        }

        private void menuItemSettings_Click(object sender, EventArgs e)
        {
            using SettingsForm settingsForm = new SettingsForm(_settings);

            if (settingsForm.ShowDialog(this) != DialogResult.OK)
                return;

            _settings.Save();
            ModernFormStyler.Apply(this, _settings.Layout);
            toolStripMain.GripStyle = ToolStripGripStyle.Visible;
            UpdatePartitionPanelVisibility();

            if (_currentRootEntry != null)
            {
                RenderScanResult(_currentRootEntry);
            }
        }

        private void menuItemExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void menuItemAbout_Click(object sender, EventArgs e)
        {
            using AboutForm aboutForm = new AboutForm(_settings);
            aboutForm.ShowDialog(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_scanCancellationTokenSource != null)
            {
                _scanCancellationTokenSource.Cancel();
            }

            base.OnFormClosing(e);
        }
    }
}