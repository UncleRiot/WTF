using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private ToolStrip toolStripViewMode;
        private ToolStripButton toolStripButtonTable;
        private ToolStripButton toolStripButtonPieChart;
        private ToolStripButton toolStripButtonBarChart;
        private ToolStripButton toolStripButtonExportCsv;
        private SplitContainer splitContainerMain;
        private SplitContainer splitContainerLeft;
        private TreeView treeViewEntries;
        private ContextMenuStrip contextMenuStripTreeEntries;
        private ToolStripMenuItem contextMenuItemOpenInExplorer;
        private ToolStripMenuItem contextMenuItemExport;
        private ToolStripMenuItem contextMenuItemCopyToClipboard;
        private FileSystemEntry _treeContextMenuEntry;
        private ImageList imageListEntries;
        private ListView listViewPartitions;
        private ImageList imageListPartitions;
        private DataGridView dataGridViewEntries;
        private Panel panelRightViewHost;
        private PieChartView pieChartView;
        private BarChartView barChartView;
        private ViewMode _viewMode;
        private FileSystemEntry _selectedEntry;
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
        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            UpdateRightViewBounds();
        }
        private void splitContainerMainPanel2_SizeChanged(object sender, EventArgs e)
        {
            UpdateRightViewBounds();
        }
        public MainForm()
        {
            _settings = AppSettings.Load();
            _driveService = new DriveService();
            _csvExportService = new CsvExportService();
            _viewMode = _settings.SelectedViewMode;

            InitializeComponent();
            ApplyMainWindowSettings();
            ApplyToolStripLayout();
            ApplySplitterLayout();

            SizeChanged += MainForm_SizeChanged;
            listViewPartitions.SizeChanged += listViewPartitions_SizeChanged;
            treeViewEntries.BeforeExpand += treeViewEntries_BeforeExpand;
            panelRightViewHost.SizeChanged += panelRightViewHost_SizeChanged;
            splitContainerMain.SplitterMoved += splitContainerMain_SplitterMoved;
            splitContainerMain.Panel2.SizeChanged += splitContainerMainPanel2_SizeChanged;

            SetDoubleBuffered(treeViewEntries, true);
            SetDoubleBuffered(dataGridViewEntries, true);
            SetDoubleBuffered(listViewPartitions, true);
            SetDoubleBuffered(pieChartView, true);
            SetDoubleBuffered(barChartView, true);

            ModernFormStyler.Apply(this, _settings.Layout);
            toolStripMain.GripStyle = ToolStripGripStyle.Visible;
            toolStripViewMode.GripStyle = ToolStripGripStyle.Visible;
            LoadDrives();
            LoadPartitionList();
            UpdatePartitionPanelVisibility();
            SetViewMode(_settings.SelectedViewMode);
            UpdateRightViewBounds();
        }
        private void panelRightViewHost_SizeChanged(object sender, EventArgs e)
        {
            UpdateRightViewBounds();
        }
        private void splitContainerMain_SplitterMoved(object sender, SplitterEventArgs e)
        {
            UpdateRightViewBounds();
        }
        private void UpdateRightViewBounds()
        {
            if (panelRightViewHost == null)
                return;

            System.Drawing.Rectangle bounds = panelRightViewHost.ClientRectangle;

            panelRightViewHost.SuspendLayout();

            try
            {
                if (dataGridViewEntries != null)
                {
                    dataGridViewEntries.Dock = DockStyle.None;
                    dataGridViewEntries.Bounds = bounds;
                }

                if (pieChartView != null)
                {
                    pieChartView.Dock = DockStyle.None;
                    pieChartView.Bounds = bounds;
                    pieChartView.Invalidate();
                }

                if (barChartView != null)
                {
                    barChartView.Dock = DockStyle.None;
                    barChartView.Bounds = bounds;
                    barChartView.Invalidate();
                }
            }
            finally
            {
                panelRightViewHost.ResumeLayout(true);
            }

            panelRightViewHost.Invalidate(true);
        }
        private void ApplyMainWindowSettings()
        {
            if (!_settings.HasMainWindowBounds)
                return;

            if (_settings.MainWindowWidth < MinimumSize.Width || _settings.MainWindowHeight < MinimumSize.Height)
                return;

            System.Drawing.Rectangle savedBounds = new System.Drawing.Rectangle(
                _settings.MainWindowLeft,
                _settings.MainWindowTop,
                _settings.MainWindowWidth,
                _settings.MainWindowHeight);

            if (!IsVisibleOnAnyScreen(savedBounds))
                return;

            StartPosition = FormStartPosition.Manual;
            Bounds = savedBounds;

            if (_settings.MainWindowMaximized)
            {
                WindowState = FormWindowState.Maximized;
            }
        }
        private bool IsVisibleOnAnyScreen(System.Drawing.Rectangle bounds)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(bounds))
                {
                    return true;
                }
            }

            return false;
        }
        private void SaveSplitterLayout()
        {
            _settings.HasSplitterLayout = true;
            _settings.SplitContainerMainDistance = splitContainerMain.SplitterDistance;
            _settings.SplitContainerLeftDistance = splitContainerLeft.SplitterDistance;
        }
        private void SaveViewSettings()
        {
            _settings.SelectedViewMode = _viewMode;
        }
        private void ApplySplitterLayout()
        {
            if (!_settings.HasSplitterLayout)
                return;

            if (_settings.SplitContainerMainDistance >= splitContainerMain.Panel1MinSize &&
                _settings.SplitContainerMainDistance <= splitContainerMain.Width - splitContainerMain.Panel2MinSize)
            {
                splitContainerMain.SplitterDistance = _settings.SplitContainerMainDistance;
            }

            if (_settings.SplitContainerLeftDistance >= splitContainerLeft.Panel1MinSize &&
                _settings.SplitContainerLeftDistance <= splitContainerLeft.Height - splitContainerLeft.Panel2MinSize)
            {
                splitContainerLeft.SplitterDistance = _settings.SplitContainerLeftDistance;
            }
        }
        private void ApplyToolStripLayout()
        {
            if (!_settings.HasToolStripLayout)
                return;

            toolStripPanelMain.Join(
                toolStripMain,
                Math.Max(0, _settings.ToolStripMainLeft),
                Math.Max(0, _settings.ToolStripMainTop));

            toolStripPanelMain.Join(
                toolStripViewMode,
                Math.Max(0, _settings.ToolStripViewModeLeft),
                Math.Max(0, _settings.ToolStripViewModeTop));
        }
        private void SaveToolStripLayout()
        {
            _settings.HasToolStripLayout = true;

            _settings.ToolStripMainLeft = toolStripMain.Left;
            _settings.ToolStripMainTop = toolStripMain.Top;

            _settings.ToolStripViewModeLeft = toolStripViewMode.Left;
            _settings.ToolStripViewModeTop = toolStripViewMode.Top;
        }
        private void SaveMainWindowSettings()
        {
            System.Drawing.Rectangle bounds = WindowState == FormWindowState.Normal
                ? Bounds
                : RestoreBounds;

            _settings.HasMainWindowBounds = true;
            _settings.MainWindowLeft = bounds.Left;
            _settings.MainWindowTop = bounds.Top;
            _settings.MainWindowWidth = bounds.Width;
            _settings.MainWindowHeight = bounds.Height;
            _settings.MainWindowMaximized = WindowState == FormWindowState.Maximized;
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

            toolStripViewMode = new ToolStrip();
            toolStripViewMode.Dock = DockStyle.None;
            toolStripViewMode.GripStyle = ToolStripGripStyle.Visible;
            toolStripViewMode.AllowItemReorder = true;
            toolStripViewMode.Padding = new Padding(0);
            toolStripViewMode.Margin = new Padding(0);

            toolStripButtonTable = new ToolStripButton("▦ Tabelle");
            toolStripButtonPieChart = new ToolStripButton("◔ Pie-Chart");
            toolStripButtonBarChart = new ToolStripButton("▥ Balkenchart");
            toolStripButtonExportCsv = new ToolStripButton("Export");

            toolStripButtonTable.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonPieChart.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonBarChart.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonExportCsv.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonExportCsv.ToolTipText = "CSV exportieren";
            toolStripButtonExportCsv.Enabled = false;
            menuItemExportCsv.Enabled = false;

            toolStripButtonTable.Click += toolStripButtonTable_Click;
            toolStripButtonPieChart.Click += toolStripButtonPieChart_Click;
            toolStripButtonBarChart.Click += toolStripButtonBarChart_Click;
            toolStripButtonExportCsv.Click += toolStripButtonExportCsv_Click;

            toolStripViewMode.Items.Add(toolStripButtonTable);
            toolStripViewMode.Items.Add(toolStripButtonPieChart);
            toolStripViewMode.Items.Add(toolStripButtonBarChart);
            toolStripViewMode.Items.Add(new ToolStripSeparator());
            toolStripViewMode.Items.Add(toolStripButtonExportCsv);

            toolStripPanelMain.Join(toolStripMain, 0, 0);
            toolStripPanelMain.Join(toolStripViewMode, 340, 0);

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
            treeViewEntries.NodeMouseClick += treeViewEntries_NodeMouseClick;

            contextMenuStripTreeEntries = new ContextMenuStrip();
            contextMenuItemOpenInExplorer = new ToolStripMenuItem("Im Explorer öffnen");
            contextMenuItemExport = new ToolStripMenuItem("Export");
            contextMenuItemCopyToClipboard = new ToolStripMenuItem("In Zwischenablage kopieren");
            contextMenuItemOpenInExplorer.Click += contextMenuItemOpenInExplorer_Click;
            contextMenuItemExport.Click += contextMenuItemExport_Click;
            contextMenuItemCopyToClipboard.Click += contextMenuItemCopyToClipboard_Click;
            contextMenuStripTreeEntries.Items.Add(contextMenuItemOpenInExplorer);
            contextMenuStripTreeEntries.Items.Add(contextMenuItemExport);
            contextMenuStripTreeEntries.Items.Add(contextMenuItemCopyToClipboard);

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

            pieChartView = new PieChartView
            {
                Name = "pieChartView",
                Dock = DockStyle.Fill,
                Visible = false
            };

            barChartView = new BarChartView
            {
                Name = "barChartView",
                Dock = DockStyle.Fill,
                Visible = false
            };

            panelRightViewHost = new Panel
            {
                Name = "panelRightViewHost",
                Dock = DockStyle.Fill
            };

            panelRightViewHost.Controls.Add(dataGridViewEntries);
            panelRightViewHost.Controls.Add(pieChartView);
            panelRightViewHost.Controls.Add(barChartView);

            splitContainerLeft.Panel1.Controls.Add(treeViewEntries);
            splitContainerLeft.Panel2.Controls.Add(listViewPartitions);

            splitContainerMain.Panel1.Controls.Add(splitContainerLeft);
            splitContainerMain.Panel2.Controls.Add(panelRightViewHost);

            statusStripMain = new StatusStrip();
            statusStripMain.SizingGrip = true;

            toolStripStatusLabel = new ToolStripStatusLabel("Bereit")
            {
                Spring = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            ToolStripStatusLabel toolStripProgressLabel = new ToolStripStatusLabel
            {
                Name = "toolStripProgressLabel",
                Text = string.Empty,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };

            statusStripMain.Items.Add(toolStripStatusLabel);
            statusStripMain.Items.Add(toolStripProgressLabel);

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

                SetStatusProgressText(null);
            }
            catch
            {
                toolStripStatusLabel.Text = "Bereit";
                SetStatusProgressText(null);
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

            long totalSizeBytes = 0;

            if (_currentRootEntry != null)
            {
                totalSizeBytes = _currentRootEntry.SizeBytes;
            }
            else if (treeViewEntries.Nodes.Count > 0 && treeViewEntries.Nodes[0].Tag is FileSystemEntry liveRootEntry)
            {
                totalSizeBytes = liveRootEntry.SizeBytes;
            }

            if (totalSizeBytes <= 0)
            {
                totalSizeBytes = entry.SizeBytes;
            }

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
            dataGridViewEntries.DataSource = null;
            _currentRootEntry = null;

            FileSystemEntry cachedRootEntry = ScanCacheService.TryLoadCachedTree(rootPath);
            bool cachePreviewLoaded = cachedRootEntry != null;

            if (cachePreviewLoaded)
            {
                _currentRootEntry = cachedRootEntry;
                RenderScanResult(cachedRootEntry);
                toolStripStatusLabel.Text = "Cache geladen - überprüfe Änderungen...";
                SetMainWindowTitleForCacheVerification();
            }
            else
            {
                treeViewEntries.Nodes.Clear();
            }

            long scanTargetBytes = GetUsedSpaceBytes(rootPath);
            SetStatusProgressText(0D);

            _scanCancellationTokenSource = new CancellationTokenSource();

            Progress<ScanProgress> progress = new Progress<ScanProgress>(scanProgress =>
            {
                double percent = scanTargetBytes <= 0 ? 0D : (double)scanProgress.ScannedBytes * 100D / scanTargetBytes;

                ApplyScanProgressToLiveTree(scanProgress);

                if (scanProgress.IsCacheSavePhase)
                {
                    toolStripStatusLabel.Text = string.Format(
                        "{0} | {1} | Ordner: {2} | Dateien: {3}",
                        scanProgress.CurrentPath,
                        SizeFormatter.Format(scanProgress.ScannedBytes),
                        scanProgress.ScannedDirectories,
                        scanProgress.ScannedFiles);
                }
                else if (cachePreviewLoaded || scanProgress.IsCacheVerification)
                {
                    toolStripStatusLabel.Text = string.Format(
                        "Cache geladen - überprüfe Änderungen: {0} | {1} | Ordner: {2} | Dateien: {3}",
                        scanProgress.CurrentPath,
                        SizeFormatter.Format(scanProgress.ScannedBytes),
                        scanProgress.ScannedDirectories,
                        scanProgress.ScannedFiles);
                }
                else
                {
                    toolStripStatusLabel.Text = string.Format(
                        "Scan: {0} | {1} | Ordner: {2} | Dateien: {3}",
                        scanProgress.CurrentPath,
                        SizeFormatter.Format(scanProgress.ScannedBytes),
                        scanProgress.ScannedDirectories,
                        scanProgress.ScannedFiles);
                }

                SetStatusProgressText(percent);
            });

            try
            {
                DirectoryScanner directoryScanner = new DirectoryScanner(_settings);

                if (NtfsMftScanner.IsSupported(rootPath))
                {
                    try
                    {
                        toolStripStatusLabel.Text = "NTFS-MFT-Schnellscan läuft...";
                        NtfsMftScanner ntfsMftScanner = new NtfsMftScanner(_settings);
                        _currentRootEntry = await ntfsMftScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                    }
                    catch
                    {
                        toolStripStatusLabel.Text = "MFT-Schnellscan nicht verfügbar - normaler Scan läuft...";
                        _currentRootEntry = await directoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                    }
                }
                else
                {
                    _currentRootEntry = await directoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                }

                RenderScanResult(_currentRootEntry);
                LoadPartitionList();
                UpdateStatusStripForDrive(rootPath);
                SetStatusProgressText(100D);
            }
            catch (OperationCanceledException)
            {
                toolStripStatusLabel.Text = "Scan abgebrochen";
                SetStatusProgressText(null);
            }
            finally
            {
                _scanCancellationTokenSource.Dispose();
                _scanCancellationTokenSource = null;
                SetScanningState(false);
            }
        }
        private void SetMainWindowTitleForCacheVerification()
        {
            string title = "WTF - Where’s The Filespace - Cache geladen / überprüfe Änderungen";

            Text = title;

            Control[] titleLabels = Controls.Find("labelModernTitle", true);

            foreach (Control control in titleLabels)
            {
                control.Text = " " + title;
            }
        }
        private void ApplyScanProgressToLiveTree(ScanProgress scanProgress)
        {
            if (scanProgress.LiveRootEntry == null)
                return;

            treeViewEntries.BeginUpdate();

            try
            {
                if (treeViewEntries.Nodes.Count == 0)
                {
                    TreeNode rootNode = CreateLiveTreeNode(scanProgress.LiveRootEntry);
                    treeViewEntries.Nodes.Add(rootNode);
                    treeViewEntries.SelectedNode = rootNode;
                    rootNode.Expand();
                    SyncLiveTreeChildren(rootNode, scanProgress.LiveRootEntry);
                    return;
                }

                TreeNode existingRootNode = treeViewEntries.Nodes[0];
                UpdateLiveTreeNode(existingRootNode, scanProgress.LiveRootEntry);
                SyncLiveTreeChildren(existingRootNode, scanProgress.LiveRootEntry);
                existingRootNode.Expand();
            }
            finally
            {
                treeViewEntries.EndUpdate();
            }
        }

        private void SyncLiveTreeChildren(TreeNode parentNode, FileSystemEntry parentEntry)
        {
            HashSet<string> expectedChildPaths = new HashSet<string>(
                parentEntry.Children
                    .Where(child => child.IsDirectory || _settings.ShowFilesInTree)
                    .Select(child => child.FullPath),
                StringComparer.OrdinalIgnoreCase);

            for (int index = parentNode.Nodes.Count - 1; index >= 0; index--)
            {
                TreeNode childNode = parentNode.Nodes[index];

                if (childNode.Tag is not FileSystemEntry childEntry || !expectedChildPaths.Contains(childEntry.FullPath))
                {
                    parentNode.Nodes.RemoveAt(index);
                }
            }

            int targetIndex = 0;

            foreach (FileSystemEntry childEntry in parentEntry.Children.Where(child => child.IsDirectory || _settings.ShowFilesInTree))
            {
                TreeNode childNode = FindChildNodeByFullPath(parentNode, childEntry.FullPath);

                if (childNode == null)
                {
                    childNode = CreateLiveTreeNode(childEntry);
                    parentNode.Nodes.Insert(targetIndex, childNode);
                }
                else
                {
                    UpdateLiveTreeNode(childNode, childEntry);

                    if (childNode.Index != targetIndex)
                    {
                        parentNode.Nodes.Remove(childNode);
                        parentNode.Nodes.Insert(targetIndex, childNode);
                    }
                }

                targetIndex++;
            }
        }

        private TreeNode FindChildNodeByFullPath(TreeNode parentNode, string fullPath)
        {
            foreach (TreeNode childNode in parentNode.Nodes)
            {
                if (childNode.Tag is FileSystemEntry childEntry &&
                    string.Equals(childEntry.FullPath, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    return childNode;
                }
            }

            return null;
        }

        private TreeNode CreateLiveTreeNode(FileSystemEntry entry)
        {
            TreeNode node = new TreeNode
            {
                Tag = entry,
                Text = SizeFormatter.Format(entry.SizeBytes) + "  " + entry.Name,
                ImageKey = entry.IsDirectory ? "Folder" : "File",
                SelectedImageKey = entry.IsDirectory ? "Folder" : "File"
            };

            if (entry.FullPath != null && entry.FullPath.EndsWith(":\\", StringComparison.OrdinalIgnoreCase))
            {
                node.ImageKey = "Drive";
                node.SelectedImageKey = "Drive";
            }

            return node;
        }

        private void UpdateLiveTreeNode(TreeNode node, FileSystemEntry entry)
        {
            node.Tag = entry;

            string text = SizeFormatter.Format(entry.SizeBytes) + "  " + entry.Name;

            if (node.Text != text)
            {
                node.Text = text;
            }

            node.ImageKey = entry.IsDirectory ? "Folder" : "File";
            node.SelectedImageKey = entry.IsDirectory ? "Folder" : "File";

            if (entry.FullPath != null && entry.FullPath.EndsWith(":\\", StringComparison.OrdinalIgnoreCase))
            {
                node.ImageKey = "Drive";
                node.SelectedImageKey = "Drive";
            }
        }

        private long GetUsedSpaceBytes(string rootPath)
        {
            try
            {
                System.IO.DriveInfo driveInfo = new System.IO.DriveInfo(rootPath);
                return Math.Max(0, driveInfo.TotalSize - driveInfo.AvailableFreeSpace);
            }
            catch
            {
                return 0;
            }
        }

        private void SetStatusProgressText(double? percent)
        {
            ToolStripItem[] items = statusStripMain.Items.Find("toolStripProgressLabel", false);

            if (items.Length == 0)
                return;

            if (!percent.HasValue)
            {
                items[0].Text = string.Empty;
                SetMainWindowTitle(null);
                return;
            }

            double value = Math.Max(0D, Math.Min(100D, percent.Value));
            items[0].Text = value.ToString("0.0") + " %";
            SetMainWindowTitle(value);
        }

        private void SetMainWindowTitle(double? scanPercent)
        {
            string title = "WTF - Where’s The Filespace";

            if (scanPercent.HasValue)
            {
                double value = Math.Max(0D, Math.Min(100D, scanPercent.Value));

                if (value >= 100D)
                {
                    title += " - Scan: 100% / completed";
                }
                else
                {
                    title += " - Scan: " + value.ToString("0.0") + "%";
                }
            }

            Text = title;

            Control[] titleLabels = Controls.Find("labelModernTitle", true);

            foreach (Control control in titleLabels)
            {
                control.Text = " " + title;
            }
        }

        private void SetDoubleBuffered(Control control, bool enabled)
        {
            if (control == null)
                return;

            System.Reflection.PropertyInfo propertyInfo = typeof(Control).GetProperty(
                "DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (propertyInfo == null)
                return;

            propertyInfo.SetValue(control, enabled, null);
        }

        private void SetScanningState(bool scanning)
        {
            toolStripButtonScan.Text = scanning ? "■" : "▶";
            toolStripButtonScan.ToolTipText = scanning ? "Scan abbrechen" : "Scan starten";
            toolStripComboBoxDrives.Enabled = !scanning;
            menuItemExportCsv.Enabled = !scanning && _currentRootEntry != null;
            toolStripButtonExportCsv.Enabled = !scanning && _currentRootEntry != null;
            splitContainerMain.IsSplitterFixed = scanning;
            splitContainerLeft.IsSplitterFixed = scanning;
        }

        private void RenderScanResult(FileSystemEntry rootEntry)
        {
            TreeNode rootNode = CreateTreeNode(rootEntry);

            treeViewEntries.BeginUpdate();

            try
            {
                treeViewEntries.Nodes.Clear();
                treeViewEntries.Nodes.Add(rootNode);
                PopulateTreeNodeChildren(rootNode);
                rootNode.Expand();
                treeViewEntries.SelectedNode = rootNode;
                treeViewEntries.TopNode = rootNode;
                rootNode.EnsureVisible();
            }
            finally
            {
                treeViewEntries.EndUpdate();
            }

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

            if (entry.IsDirectory && entry.Children.Any(child => child.IsDirectory || _settings.ShowFilesInTree))
            {
                node.Nodes.Add(CreateLazyPlaceholderNode());
            }

            return node;
        }

        private TreeNode CreateLazyPlaceholderNode()
        {
            return new TreeNode
            {
                Name = "__LAZY_PLACEHOLDER__",
                Text = string.Empty
            };
        }

        private bool IsLazyPlaceholderNode(TreeNode node)
        {
            return node != null && node.Name == "__LAZY_PLACEHOLDER__";
        }

        private void treeViewEntries_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node == null)
                return;

            PopulateTreeNodeChildren(e.Node);
        }

        private void PopulateTreeNodeChildren(TreeNode parentNode)
        {
            if (parentNode.Tag is not FileSystemEntry parentEntry)
                return;

            if (parentNode.Nodes.Count == 1 && IsLazyPlaceholderNode(parentNode.Nodes[0]))
            {
                parentNode.Nodes.Clear();
            }
            else if (parentNode.Nodes.Count > 0)
            {
                return;
            }

            foreach (FileSystemEntry child in parentEntry.Children
                         .Where(child => child.IsDirectory || _settings.ShowFilesInTree)
                         .OrderByDescending(child => child.SizeBytes)
                         .ThenBy(child => child.Name))
            {
                parentNode.Nodes.Add(CreateTreeNode(child));
            }
        }

        private void BindGrid(FileSystemEntry entry)
        {
            _selectedEntry = entry;

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
            pieChartView.SetEntry(entry);
            barChartView.SetEntry(entry);
            UpdateRightView();
        }
        private void toolStripButtonTable_Click(object sender, EventArgs e)
        {
            SetViewMode(ViewMode.Table);
        }
        private void toolStripButtonPieChart_Click(object sender, EventArgs e)
        {
            SetViewMode(ViewMode.PieChart);
        }
        private void toolStripButtonBarChart_Click(object sender, EventArgs e)
        {
            SetViewMode(ViewMode.BarChart);
        }
        private void SetViewMode(ViewMode viewMode)
        {
            _viewMode = viewMode;
            _settings.SelectedViewMode = viewMode;
            UpdateViewModeButtons();
            UpdateRightView();
        }
        private void UpdateViewModeButtons()
        {
            toolStripButtonTable.Checked = _viewMode == ViewMode.Table;
            toolStripButtonPieChart.Checked = _viewMode == ViewMode.PieChart;
            toolStripButtonBarChart.Checked = _viewMode == ViewMode.BarChart;
        }
        private void UpdateRightView()
        {
            UpdateRightViewBounds();

            dataGridViewEntries.Visible = _viewMode == ViewMode.Table;
            pieChartView.Visible = _viewMode == ViewMode.PieChart;
            barChartView.Visible = _viewMode == ViewMode.BarChart;

            if (dataGridViewEntries.Visible)
            {
                dataGridViewEntries.BringToFront();
            }
            else if (pieChartView.Visible)
            {
                pieChartView.BringToFront();
                pieChartView.Invalidate();
            }
            else if (barChartView.Visible)
            {
                barChartView.BringToFront();
                barChartView.Invalidate();
            }
        }

        private void treeViewEntries_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null && e.Node.Tag is FileSystemEntry entry)
            {
                BindGrid(entry);
            }
        }

        private void toolStripButtonExportCsv_Click(object sender, EventArgs e)
        {
            ExportEntry(_currentRootEntry);
        }

        private void menuItemExportCsv_Click(object sender, EventArgs e)
        {
            ExportEntry(_currentRootEntry);
        }

        private void treeViewEntries_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            treeViewEntries.SelectedNode = e.Node;

            if (e.Node != null && e.Node.Tag is FileSystemEntry entry && entry.IsDirectory)
            {
                _treeContextMenuEntry = entry;
                contextMenuItemOpenInExplorer.Enabled = true;
                contextMenuItemExport.Enabled = true;
                contextMenuItemCopyToClipboard.Enabled = true;
                contextMenuStripTreeEntries.Show(treeViewEntries, e.Location);
                return;
            }

            _treeContextMenuEntry = null;
        }

        private void contextMenuItemOpenInExplorer_Click(object sender, EventArgs e)
        {
            if (_treeContextMenuEntry == null || string.IsNullOrWhiteSpace(_treeContextMenuEntry.FullPath))
                return;

            if (!Directory.Exists(_treeContextMenuEntry.FullPath))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = _treeContextMenuEntry.FullPath,
                UseShellExecute = true
            });
        }

        private void contextMenuItemExport_Click(object sender, EventArgs e)
        {
            ExportEntry(_treeContextMenuEntry);
        }

        private void contextMenuItemCopyToClipboard_Click(object sender, EventArgs e)
        {
            CopyEntryExportToClipboard(_treeContextMenuEntry);
        }

        private void CopyEntryExportToClipboard(FileSystemEntry rootEntry)
        {
            if (rootEntry == null)
                return;

            string csvText = _csvExportService.ExportToString(new[] { rootEntry }, _settings);

            if (string.IsNullOrEmpty(csvText))
                return;

            Clipboard.SetText(csvText, TextDataFormat.UnicodeText);
            toolStripStatusLabel.Text = "Export in Zwischenablage kopiert: " + rootEntry.FullPath;
        }

        private void ExportEntry(FileSystemEntry rootEntry)
        {
            if (rootEntry == null)
                return;

            using SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = _csvExportService.FileFilter,
                FileName = CreateExportFileName(rootEntry)
            };

            if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            _csvExportService.Export(saveFileDialog.FileName, new[] { rootEntry }, _settings);
            toolStripStatusLabel.Text = "Export gespeichert: " + saveFileDialog.FileName;
        }

        private string CreateExportFileName(FileSystemEntry entry)
        {
            string name = string.IsNullOrWhiteSpace(entry.Name) ? "wtf-scan" : entry.Name;

            foreach (char invalidFileNameChar in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidFileNameChar, '_');
            }

            return name + ".csv";
        }

        private void menuItemSettings_Click(object sender, EventArgs e)
        {
            using SettingsForm settingsForm = new SettingsForm(_settings);

            if (settingsForm.ShowDialog(this) != DialogResult.OK)
                return;

            _settings.Save();
            ModernFormStyler.Apply(this, _settings.Layout);
            toolStripMain.GripStyle = ToolStripGripStyle.Visible;
            toolStripViewMode.GripStyle = ToolStripGripStyle.Visible;
            UpdatePartitionPanelVisibility();
            UpdateRightView();

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

            SaveMainWindowSettings();
            SaveToolStripLayout();
            SaveSplitterLayout();
            SaveViewSettings();
            _settings.Save();

            base.OnFormClosing(e);
        }
    }
}