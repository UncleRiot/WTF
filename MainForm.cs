using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
        private readonly string _startupScanPath;

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
        private ToolStripButton toolStripButtonOpenFolder;
        private ToolStrip toolStripViewMode;
        private ToolStrip toolStripExport;
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
        private DataGridView listViewPartitions;
        private ImageList imageListPartitions;
        private DataGridView dataGridViewEntries;
        private Panel panelRightViewHost;
        private PieChartView pieChartView;
        private BarChartView barChartView;
        private ViewMode _viewMode;
        private FileSystemEntry _selectedEntry;
        private StatusStrip statusStripAlerts;
        private ToolStripStatusLabel toolStripAlertInformationLabel;
        private ToolStripStatusLabel toolStripAlertWarningLabel;
        private ToolStripStatusLabel toolStripAlertErrorLabel;
        private StatusStrip statusStripMain;
        private ToolStripStatusLabel toolStripStatusLabel;
        
        private System.Windows.Forms.Timer liveTreeUpdateTimer;
        private ScanProgress _pendingLiveTreeScanProgress;
        private bool _liveTreeUpdateInProgress;
        private bool _suspendPersistentSettingsSave;

        private const int SHGFI_ICON = 0x100;
        private const int SHGFI_SMALLICON = 0x1;
        private const int WM_SETREDRAW = 0x000B;
        private const int TVM_SETEXTENDEDSTYLE = 0x112C;
        private const int TVS_EX_DOUBLEBUFFER = 0x0004;

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref SHFILEINFO psfi,
            uint cbFileInfo,
            uint uFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

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
    : this(null)
        {
        }
        public MainForm(string startupScanPath)
        {
            _suspendPersistentSettingsSave = true;

            _settings = AppSettings.Load();
            _driveService = new DriveService();
            _csvExportService = new CsvExportService();
            _viewMode = _settings.SelectedViewMode;
            _startupScanPath = startupScanPath;

            InitializeComponent();
            AppAlertLog.Changed += AppAlertLog_Changed;
            UpdateAlertStatusStrip();
            ConfigureTreeViewFlickerReduction();
            ConfigureLiveTreeUpdateTimer();
            ConfigureDriveComboBoxDrawing();
            ConfigureOpenFolderButtonImage();
            ApplyMainWindowSettings();
            ApplyDefaultToolStripLayout();
            ApplyToolStripLayout();
            ApplySplitterLayout();

            SizeChanged += MainForm_SizeChanged;
            Shown += MainForm_Shown;
            listViewPartitions.SizeChanged += listViewPartitions_SizeChanged;
            treeViewEntries.BeforeExpand += treeViewEntries_BeforeExpand;
            panelRightViewHost.SizeChanged += panelRightViewHost_SizeChanged;
            dataGridViewEntries.SizeChanged += dataGridViewEntries_SizeChanged;
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
            toolStripExport.GripStyle = ToolStripGripStyle.Visible;
            LoadDrives();
            LoadPartitionList();
            ApplyColumnLayout();
            ConfigureEntryGridColumns();
            UpdatePartitionPanelVisibility();
            SetViewMode(_settings.SelectedViewMode);
            UpdateRightViewBounds();

            _suspendPersistentSettingsSave = false;
        }
        private void MainForm_Shown(object sender, EventArgs e)
        {
            Shown -= MainForm_Shown;
            StartStartupScanIfRequested();
        }
        private void StartStartupScanIfRequested()
        {
            if (string.IsNullOrWhiteSpace(_startupScanPath))
                return;

            BeginInvoke(new Action(async () =>
            {
                if (!Directory.Exists(_startupScanPath))
                    return;

                if (_scanCancellationTokenSource != null)
                    return;

                AddOrSelectPathInDriveComboBox(_startupScanPath);
                await StartScanAsync(_startupScanPath);
            }));
        }
        private void SavePersistentSettings()
        {
            if (_suspendPersistentSettingsSave)
                return;

            SaveViewSettings();
            TrySaveAppSettings(false);
        }

        private bool TrySaveAppSettings(bool showMessage)
        {
            try
            {
                _settings.Save();
                return true;
            }
            catch (Exception exception)
            {
                AppAlertLog.AddError("Einstellungen", "Einstellungen konnten nicht gespeichert werden: " + exception.Message);

                if (showMessage)
                {
                    MessageBox.Show(
                        this,
                        "Die Einstellungen konnten nicht gespeichert werden." + Environment.NewLine + Environment.NewLine + exception.Message,
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                return false;
            }
        }
        private void toolStripLayout_LocationChanged(object sender, EventArgs e)
        {
            SavePersistentSettings();
        }
        private void AppAlertLog_Changed(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateAlertStatusStrip));
                return;
            }

            UpdateAlertStatusStrip();
        }

        private void UpdateAlertStatusStrip()
        {
            if (toolStripAlertInformationLabel == null || toolStripAlertWarningLabel == null || toolStripAlertErrorLabel == null)
                return;

            toolStripAlertInformationLabel.Text = AppAlertLog.GetUnconfirmedCount(AppAlertSeverity.Information).ToString();
            toolStripAlertWarningLabel.Text = AppAlertLog.GetUnconfirmedCount(AppAlertSeverity.Warning).ToString();
            toolStripAlertErrorLabel.Text = AppAlertLog.GetUnconfirmedCount(AppAlertSeverity.Error).ToString();
        }

        private void toolStripAlertLabel_Click(object sender, EventArgs e)
        {
            using AlertHistoryForm alertHistoryForm = new AlertHistoryForm(_settings);
            alertHistoryForm.ShowDialog(this);
        }

        private void ConfigureTreeViewFlickerReduction()
        {
            treeViewEntries.HandleCreated -= treeViewEntries_HandleCreated;
            treeViewEntries.HandleCreated += treeViewEntries_HandleCreated;

            if (treeViewEntries.IsHandleCreated)
            {
                ApplyTreeViewNativeDoubleBuffer();
            }
        }
        private void FlushPendingLiveTreeUpdate()
        {
            if (_liveTreeUpdateInProgress)
                return;

            ScanProgress scanProgress = _pendingLiveTreeScanProgress;

            if (scanProgress == null)
                return;

            _pendingLiveTreeScanProgress = null;
            _liveTreeUpdateInProgress = true;

            try
            {
                ApplyScanProgressToLiveTree(scanProgress);
            }
            finally
            {
                _liveTreeUpdateInProgress = false;
            }
        }
        private void QueueLiveTreeUpdate(ScanProgress scanProgress)
        {
            if (scanProgress == null)
                return;

            if (scanProgress.LiveRootEntry == null)
                return;

            _pendingLiveTreeScanProgress = scanProgress;

            if (liveTreeUpdateTimer != null && !liveTreeUpdateTimer.Enabled)
            {
                liveTreeUpdateTimer.Start();
            }
        }
        private void ConfigureLiveTreeUpdateTimer()
        {
            liveTreeUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 250
            };

            liveTreeUpdateTimer.Tick += liveTreeUpdateTimer_Tick;
        }
        private void liveTreeUpdateTimer_Tick(object sender, EventArgs e)
        {
            FlushPendingLiveTreeUpdate();
        }

        private void treeViewEntries_HandleCreated(object sender, EventArgs e)
        {
            ApplyTreeViewNativeDoubleBuffer();
        }

        private void ApplyTreeViewNativeDoubleBuffer()
        {
            SendMessage(
                treeViewEntries.Handle,
                TVM_SETEXTENDEDSTYLE,
                new IntPtr(TVS_EX_DOUBLEBUFFER),
                new IntPtr(TVS_EX_DOUBLEBUFFER));
        }        private void SetTreeViewRedraw(bool enabled)
        {
            if (treeViewEntries == null || !treeViewEntries.IsHandleCreated)
                return;

            SendMessage(
                treeViewEntries.Handle,
                WM_SETREDRAW,
                enabled ? new IntPtr(1) : IntPtr.Zero,
                IntPtr.Zero);
        }




        private void ConfigureDriveComboBoxDrawing()
        {
            toolStripComboBoxDrives.DropDownStyle = ComboBoxStyle.DropDownList;
            toolStripComboBoxDrives.ComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            toolStripComboBoxDrives.ComboBox.ItemHeight = Math.Max(20, toolStripComboBoxDrives.ComboBox.ItemHeight);
            toolStripComboBoxDrives.ComboBox.DrawItem -= toolStripComboBoxDrives_DrawItem;
            toolStripComboBoxDrives.ComboBox.DrawItem += toolStripComboBoxDrives_DrawItem;
        }
        private void ConfigureOpenFolderButtonImage()
        {
            toolStripButtonOpenFolder.Image = GetSmallStockIcon(SHSTOCKICONID.SIID_FOLDEROPEN);
            toolStripButtonOpenFolder.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonOpenFolder.Text = string.Empty;

            toolStripButtonScan.Image = CreateScanButtonImage();
            toolStripButtonScan.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonScan.Text = string.Empty;
        }
        private System.Drawing.Bitmap CreateScanButtonImage()
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(System.Drawing.Color.Transparent);

                System.Drawing.Point[] points =
                {
            new System.Drawing.Point(4, 2),
            new System.Drawing.Point(13, 8),
            new System.Drawing.Point(4, 14)
        };

                using System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 215));
                graphics.FillPolygon(brush, points);

                using System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 84, 153));
                graphics.DrawPolygon(pen, points);
            }

            return bitmap;
        }
        private void toolStripComboBoxDrives_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0)
                return;

            ComboBox comboBox = (ComboBox)sender;
            object item = comboBox.Items[e.Index];

            string text = item == null
                ? string.Empty
                : item.ToString();

            string iconPath = GetDriveComboBoxItemIconPath(item);

            System.Drawing.Color textColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? System.Drawing.SystemColors.HighlightText
                : comboBox.ForeColor;

            int iconLeft = e.Bounds.Left + 3;
            int iconTop = e.Bounds.Top + Math.Max(0, (e.Bounds.Height - 16) / 2);

            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                using System.Drawing.Bitmap icon = GetSmallSystemIcon(iconPath);
                e.Graphics.DrawImage(icon, iconLeft, iconTop, 16, 16);
            }

            System.Drawing.Rectangle textBounds = new System.Drawing.Rectangle(
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
        private string GetDriveComboBoxItemIconPath(object item)
        {
            if (item is DriveItem driveItem)
                return driveItem.RootPath;

            if (item is string path)
            {
                if (Directory.Exists(path))
                    return path;

                if (File.Exists(path))
                    return path;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        }
        private void panelRightViewHost_SizeChanged(object sender, EventArgs e)
        {
            UpdateRightViewBounds();
        }
        private void ConfigureEntryGridColumns()
        {
            dataGridViewEntries.Dock = DockStyle.Fill;
            dataGridViewEntries.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dataGridViewEntries.ScrollBars = ScrollBars.Vertical;

            if (dataGridViewEntries.Columns.Contains("ColumnSizeBytes"))
            {
                dataGridViewEntries.Columns["ColumnSizeBytes"].Visible = false;
                dataGridViewEntries.Columns["ColumnSizeBytes"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridViewEntries.Columns["ColumnSizeBytes"].MinimumWidth = 2;
            }

            if (dataGridViewEntries.Columns.Contains("ColumnName"))
            {
                dataGridViewEntries.Columns["ColumnName"].Visible = true;
                dataGridViewEntries.Columns["ColumnName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridViewEntries.Columns["ColumnName"].MinimumWidth = 20;
            }

            if (dataGridViewEntries.Columns.Contains("ColumnSize"))
            {
                dataGridViewEntries.Columns["ColumnSize"].Visible = true;
                dataGridViewEntries.Columns["ColumnSize"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridViewEntries.Columns["ColumnSize"].MinimumWidth = 20;
            }

            if (dataGridViewEntries.Columns.Contains("ColumnPercent"))
            {
                dataGridViewEntries.Columns["ColumnPercent"].Visible = true;
                dataGridViewEntries.Columns["ColumnPercent"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridViewEntries.Columns["ColumnPercent"].MinimumWidth = 20;
            }

            if (dataGridViewEntries.Columns.Contains("ColumnPath"))
            {
                dataGridViewEntries.Columns["ColumnPath"].Visible = true;
                dataGridViewEntries.Columns["ColumnPath"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridViewEntries.Columns["ColumnPath"].MinimumWidth = 20;
            }

            ApplyEntryGridColumnWidths();
        }
        private void ApplyEntryGridColumnWidths()
        {
            if (dataGridViewEntries == null)
                return;

            if (!dataGridViewEntries.Columns.Contains("ColumnName"))
                return;

            if (!dataGridViewEntries.Columns.Contains("ColumnSize"))
                return;

            if (!dataGridViewEntries.Columns.Contains("ColumnPercent"))
                return;

            if (!dataGridViewEntries.Columns.Contains("ColumnPath"))
                return;

            int availableWidth = dataGridViewEntries.ClientSize.Width - 2;

            if (availableWidth <= 0)
                return;

            if (dataGridViewEntries.RowCount > 0 &&
                dataGridViewEntries.DisplayedRowCount(false) < dataGridViewEntries.RowCount)
            {
                availableWidth -= SystemInformation.VerticalScrollBarWidth;
            }

            availableWidth = Math.Max(availableWidth, 80);

            int nameWidth = Math.Max(20, (int)Math.Round(availableWidth * 0.34D));
            int sizeWidth = Math.Max(20, (int)Math.Round(availableWidth * 0.14D));
            int percentWidth = Math.Max(20, (int)Math.Round(availableWidth * 0.18D));
            int pathWidth = Math.Max(20, availableWidth - nameWidth - sizeWidth - percentWidth);

            int usedWidth = nameWidth + sizeWidth + percentWidth + pathWidth;

            if (usedWidth > availableWidth)
            {
                int overflow = usedWidth - availableWidth;
                pathWidth = Math.Max(20, pathWidth - overflow);
            }

            dataGridViewEntries.SuspendLayout();

            try
            {
                dataGridViewEntries.Columns["ColumnName"].Width = nameWidth;
                dataGridViewEntries.Columns["ColumnSize"].Width = sizeWidth;
                dataGridViewEntries.Columns["ColumnPercent"].Width = percentWidth;
                dataGridViewEntries.Columns["ColumnPath"].Width = pathWidth;
            }
            finally
            {
                dataGridViewEntries.ResumeLayout();
            }
        }
        private void splitContainerMain_SplitterMoved(object sender, SplitterEventArgs e)
        {
            UpdateRightViewBounds();
        }
        private void UpdateRightViewBounds()
        {
            if (panelRightViewHost == null)
                return;

            panelRightViewHost.SuspendLayout();

            try
            {
                if (dataGridViewEntries != null)
                {
                    dataGridViewEntries.Dock = DockStyle.Fill;
                    ApplyEntryGridColumnWidths();
                }

                if (pieChartView != null)
                {
                    pieChartView.Dock = DockStyle.Fill;
                    pieChartView.Invalidate();
                }

                if (barChartView != null)
                {
                    barChartView.Dock = DockStyle.Fill;
                    barChartView.Invalidate();
                }
            }
            finally
            {
                panelRightViewHost.ResumeLayout(true);
            }

            panelRightViewHost.Invalidate(true);
        }
        private void dataGridViewEntries_SizeChanged(object sender, EventArgs e)
        {
            ApplyEntryGridColumnWidths();
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
            _settings.SplitContainerLeftDistance = splitContainerLeft.Height - splitContainerLeft.SplitterDistance - splitContainerLeft.SplitterWidth;
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

            int splitContainerLeftDistance = splitContainerLeft.Height - _settings.SplitContainerLeftDistance - splitContainerLeft.SplitterWidth;

            if (splitContainerLeftDistance >= splitContainerLeft.Panel1MinSize &&
                splitContainerLeftDistance <= splitContainerLeft.Height - splitContainerLeft.Panel2MinSize)
            {
                splitContainerLeft.SplitterDistance = splitContainerLeftDistance;
            }
        }
        private void ApplyDefaultToolStripLayout()
        {
            toolStripPanelMain.Join(toolStripMain, 0, 0);
            toolStripPanelMain.Join(toolStripViewMode, 390, 0);
            toolStripPanelMain.Join(toolStripExport, 610, 0);
        }
        private void ApplyToolStripLayout()
        {
            if (!_settings.HasToolStripLayout)
                return;

            if (_settings.ToolStripLayoutVersion != 1)
                return;

            toolStripPanelMain.Join(
                toolStripMain,
                Math.Max(0, _settings.ToolStripMainLeft),
                Math.Max(0, _settings.ToolStripMainTop));

            toolStripPanelMain.Join(
                toolStripViewMode,
                Math.Max(0, _settings.ToolStripViewModeLeft),
                Math.Max(0, _settings.ToolStripViewModeTop));

            toolStripPanelMain.Join(
                toolStripExport,
                Math.Max(0, _settings.ToolStripExportLeft),
                Math.Max(0, _settings.ToolStripExportTop));
        }
        private void SaveToolStripLayout()
        {
            _settings.HasToolStripLayout = true;
            _settings.ToolStripLayoutVersion = 1;

            _settings.ToolStripMainLeft = toolStripMain.Left;
            _settings.ToolStripMainTop = toolStripMain.Top;

            _settings.ToolStripViewModeLeft = toolStripViewMode.Left;
            _settings.ToolStripViewModeTop = toolStripViewMode.Top;

            _settings.ToolStripExportLeft = toolStripExport.Left;
            _settings.ToolStripExportTop = toolStripExport.Top;
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
            toolStripButtonOpenFolder = new ToolStripButton("Öffnen");

            toolStripLabelDrive.Margin = new Padding(0, 1, 0, 2);
            toolStripComboBoxDrives.DropDownStyle = ComboBoxStyle.DropDownList;
            toolStripComboBoxDrives.AutoSize = false;
            toolStripComboBoxDrives.Width = 260;
            toolStripButtonScan.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonScan.ToolTipText = "Scan starten";
            toolStripButtonScan.Click += toolStripButtonScan_Click;
            toolStripButtonOpenFolder.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonOpenFolder.ToolTipText = "Ordner auswählen und scannen";
            toolStripButtonOpenFolder.Click += toolStripButtonOpenFolder_Click;

            toolStripMain.Items.Add(toolStripLabelDrive);
            toolStripMain.Items.Add(toolStripComboBoxDrives);
            toolStripMain.Items.Add(toolStripButtonScan);
            toolStripMain.Items.Add(toolStripButtonOpenFolder);

            toolStripViewMode = new ToolStrip();
            toolStripViewMode.Dock = DockStyle.None;
            toolStripViewMode.GripStyle = ToolStripGripStyle.Visible;
            toolStripViewMode.AllowItemReorder = true;
            toolStripViewMode.Padding = new Padding(0);
            toolStripViewMode.Margin = new Padding(0);

            toolStripButtonTable = new ToolStripButton("▦ Tabelle");
            toolStripButtonPieChart = new ToolStripButton("◔ Pie-Chart");
            toolStripButtonBarChart = new ToolStripButton("▥ Balkenchart");

            toolStripButtonTable.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonPieChart.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonBarChart.DisplayStyle = ToolStripItemDisplayStyle.Text;

            toolStripButtonTable.Click += toolStripButtonTable_Click;
            toolStripButtonPieChart.Click += toolStripButtonPieChart_Click;
            toolStripButtonBarChart.Click += toolStripButtonBarChart_Click;

            toolStripViewMode.Items.Add(toolStripButtonTable);
            toolStripViewMode.Items.Add(toolStripButtonPieChart);
            toolStripViewMode.Items.Add(toolStripButtonBarChart);

            toolStripExport = new ToolStrip();
            toolStripExport.Dock = DockStyle.None;
            toolStripExport.GripStyle = ToolStripGripStyle.Visible;
            toolStripExport.AllowItemReorder = true;
            toolStripExport.Padding = new Padding(0);
            toolStripExport.Margin = new Padding(0);

            toolStripButtonExportCsv = new ToolStripButton("Export");
            toolStripButtonExportCsv.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            toolStripButtonExportCsv.Image = CreateExportButtonImage();
            toolStripButtonExportCsv.ToolTipText = "CSV exportieren";
            toolStripButtonExportCsv.Enabled = false;
            menuItemExportCsv.Enabled = false;
            toolStripButtonExportCsv.Click += toolStripButtonExportCsv_Click;

            toolStripExport.Items.Add(toolStripButtonExportCsv);

            toolStripPanelMain.Join(toolStripMain, 0, 0);
            toolStripPanelMain.Join(toolStripViewMode, 340, 0);
            toolStripPanelMain.Join(toolStripExport, 610, 0);

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

            listViewPartitions = new DataGridView();
            listViewPartitions.Dock = DockStyle.Fill;
            listViewPartitions.AllowUserToAddRows = false;
            listViewPartitions.AllowUserToDeleteRows = false;
            listViewPartitions.AllowUserToResizeRows = false;
            listViewPartitions.AutoGenerateColumns = false;
            listViewPartitions.BackgroundColor = System.Drawing.SystemColors.Window;
            listViewPartitions.BorderStyle = BorderStyle.FixedSingle;
            listViewPartitions.CellBorderStyle = DataGridViewCellBorderStyle.None;
            listViewPartitions.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            listViewPartitions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            listViewPartitions.ColumnHeadersHeight = 24;
            listViewPartitions.EnableHeadersVisualStyles = true;
            listViewPartitions.MultiSelect = false;
            listViewPartitions.ReadOnly = true;
            listViewPartitions.RowHeadersVisible = false;
            listViewPartitions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            listViewPartitions.CellPainting += listViewPartitions_CellPainting;

            listViewPartitions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PartitionColumnName",
                HeaderText = "Name",
                Width = 120,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            listViewPartitions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PartitionColumnSize",
                HeaderText = "Größe",
                Width = 80,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            listViewPartitions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PartitionColumnFree",
                HeaderText = "Frei",
                Width = 80,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            listViewPartitions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PartitionColumnFreePercent",
                HeaderText = "% Frei",
                Width = 70,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

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

            statusStripAlerts = new StatusStrip
            {
                Name = "statusStripAlerts",
                SizingGrip = false
            };

            toolStripAlertInformationLabel = new ToolStripStatusLabel
            {
                Name = "toolStripAlertInformationLabel",
                Image = SystemIcons.Information.ToBitmap(),
                Text = "0",
                ToolTipText = "Informationen anzeigen"
            };

            toolStripAlertWarningLabel = new ToolStripStatusLabel
            {
                Name = "toolStripAlertWarningLabel",
                Image = SystemIcons.Warning.ToBitmap(),
                Text = "0",
                ToolTipText = "Warnungen anzeigen"
            };

            toolStripAlertErrorLabel = new ToolStripStatusLabel
            {
                Name = "toolStripAlertErrorLabel",
                Image = SystemIcons.Error.ToBitmap(),
                Text = "0",
                ToolTipText = "Fehler anzeigen"
            };

            toolStripAlertInformationLabel.Click += toolStripAlertLabel_Click;
            toolStripAlertWarningLabel.Click += toolStripAlertLabel_Click;
            toolStripAlertErrorLabel.Click += toolStripAlertLabel_Click;

            statusStripAlerts.Items.Add(toolStripAlertInformationLabel);
            statusStripAlerts.Items.Add(toolStripAlertWarningLabel);
            statusStripAlerts.Items.Add(toolStripAlertErrorLabel);

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
            tableLayoutPanelMain.RowCount = 5;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            menuStripMain.Dock = DockStyle.Fill;
            toolStripPanelMain.Dock = DockStyle.Fill;
            splitContainerMain.Dock = DockStyle.Fill;
            statusStripAlerts.Dock = DockStyle.Fill;
            statusStripMain.Dock = DockStyle.Fill;

            tableLayoutPanelMain.Controls.Add(menuStripMain, 0, 0);
            tableLayoutPanelMain.Controls.Add(toolStripPanelMain, 0, 1);
            tableLayoutPanelMain.Controls.Add(splitContainerMain, 0, 2);
            tableLayoutPanelMain.Controls.Add(statusStripAlerts, 0, 3);
            tableLayoutPanelMain.Controls.Add(statusStripMain, 0, 4);

            Controls.Add(tableLayoutPanelMain);

            MainMenuStrip = menuStripMain;
        }
        private System.Drawing.Bitmap CreateExportButtonImage()
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(System.Drawing.Color.Transparent);

                using System.Drawing.SolidBrush documentBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(245, 245, 245));
                using System.Drawing.Pen documentPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(90, 90, 90));
                using System.Drawing.SolidBrush arrowBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 215));
                using System.Drawing.Pen arrowPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 84, 153));

                graphics.FillRectangle(documentBrush, new Rectangle(2, 2, 8, 12));
                graphics.DrawRectangle(documentPen, new Rectangle(2, 2, 8, 12));

                System.Drawing.Point[] arrowPoints =
                {
            new System.Drawing.Point(8, 5),
            new System.Drawing.Point(14, 8),
            new System.Drawing.Point(8, 11)
        };

                graphics.FillPolygon(arrowBrush, arrowPoints);
                graphics.DrawPolygon(arrowPen, arrowPoints);
                graphics.DrawLine(arrowPen, 5, 8, 12, 8);
            }

            return bitmap;
        }
        private async void toolStripButtonOpenFolder_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Ordner zum Scannen auswählen",
                ShowNewFolderButton = false
            };

            if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                return;

            AddOrSelectPathInDriveComboBox(folderBrowserDialog.SelectedPath);

            await StartScanAsync(folderBrowserDialog.SelectedPath);
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

            toolStripComboBoxDrives.DropDownStyle = ComboBoxStyle.DropDownList;
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
        private const uint SHGSI_ICON = 0x000000100;
        private const uint SHGSI_SMALLICON = 0x000000001;

        [System.Runtime.InteropServices.DllImport("shell32.dll", SetLastError = false)]
        private static extern int SHGetStockIconInfo(
            SHSTOCKICONID siid,
            uint uFlags,
            ref SHSTOCKICONINFO psii);

        private enum SHSTOCKICONID : uint
        {
            SIID_FOLDEROPEN = 4,
            SIID_FIND = 22
        }
        private System.Drawing.Bitmap GetSmallStockIcon(SHSTOCKICONID stockIconId)
        {
            SHSTOCKICONINFO stockIconInfo = new SHSTOCKICONINFO();
            stockIconInfo.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(SHSTOCKICONINFO));

            int result = SHGetStockIconInfo(
                stockIconId,
                SHGSI_ICON | SHGSI_SMALLICON,
                ref stockIconInfo);

            if (result != 0 || stockIconInfo.hIcon == IntPtr.Zero)
            {
                return System.Drawing.SystemIcons.Application.ToBitmap();
            }

            try
            {
                using System.Drawing.Icon icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(stockIconInfo.hIcon).Clone();
                return icon.ToBitmap();
            }
            finally
            {
                DestroyIcon(stockIconInfo.hIcon);
            }
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private struct SHSTOCKICONINFO
        {
            public uint cbSize;
            public IntPtr hIcon;
            public int iSysImageIndex;
            public int iIcon;

            [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szPath;
        }
        private void listViewPartitions_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }
        private void listViewPartitions_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
        }
        private void listViewPartitions_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
        }
        private void listViewPartitions_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.ColumnIndex < 0)
                return;

            if (e.ColumnIndex != 0 && e.ColumnIndex != 3)
                return;

            e.Handled = true;

            bool selected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;

            System.Drawing.Color backColor = selected
                ? System.Drawing.SystemColors.Highlight
                : listViewPartitions.BackgroundColor;

            System.Drawing.Color textColor = selected
                ? System.Drawing.SystemColors.HighlightText
                : listViewPartitions.ForeColor;

            using (System.Drawing.SolidBrush backBrush = new System.Drawing.SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(backBrush, e.CellBounds);
            }

            if (e.ColumnIndex == 0)
            {
                string text = Convert.ToString(e.FormattedValue);
                string rootPath = Convert.ToString(listViewPartitions.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag);

                int iconLeft = e.CellBounds.Left + 4;
                int iconTop = e.CellBounds.Top + Math.Max(0, (e.CellBounds.Height - 16) / 2);

                if (!string.IsNullOrWhiteSpace(rootPath) && imageListPartitions.Images.ContainsKey(rootPath))
                {
                    e.Graphics.DrawImage(imageListPartitions.Images[rootPath], iconLeft, iconTop, 16, 16);
                }

                System.Drawing.Rectangle textBounds = new System.Drawing.Rectangle(
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

            int freePercent = listViewPartitions.Rows[e.RowIndex].Tag is int value ? value : 0;
            freePercent = Math.Max(0, Math.Min(100, freePercent));

            System.Drawing.Rectangle barBounds = new System.Drawing.Rectangle(
                e.CellBounds.Left + 4,
                e.CellBounds.Top + 3,
                Math.Max(0, e.CellBounds.Width - 8),
                Math.Max(0, e.CellBounds.Height - 6));

            int barWidth = (int)Math.Round(barBounds.Width * freePercent / 100D);

            using (System.Drawing.SolidBrush emptyBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(230, 230, 230)))
            using (System.Drawing.SolidBrush fillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Lime))
            using (System.Drawing.Pen borderPen = new System.Drawing.Pen(System.Drawing.Color.Silver))
            {
                e.Graphics.FillRectangle(emptyBrush, barBounds);

                if (barWidth > 0)
                {
                    e.Graphics.FillRectangle(
                        fillBrush,
                        new System.Drawing.Rectangle(barBounds.Left, barBounds.Top, barWidth, barBounds.Height));
                }

                e.Graphics.DrawRectangle(borderPen, barBounds);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Convert.ToString(e.FormattedValue),
                e.CellStyle.Font,
                barBounds,
                System.Drawing.Color.Black,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        private void LoadPartitionList()
        {
            listViewPartitions.SuspendLayout();
            listViewPartitions.Rows.Clear();
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

                int rowIndex = listViewPartitions.Rows.Add(
                    rootPath,
                    SizeFormatter.Format(totalSize),
                    SizeFormatter.Format(freeSpace),
                    freePercent + " %");

                DataGridViewRow row = listViewPartitions.Rows[rowIndex];
                row.Tag = freePercent;
                row.Cells[0].Tag = rootPath;
            }

            AdjustPartitionColumns();
            listViewPartitions.ResumeLayout();
            listViewPartitions.Invalidate();
        }

        private void listViewPartitions_SizeChanged(object sender, EventArgs e)
        {
            AdjustPartitionColumns();
        }
        private void SaveColumnLayout()
        {
            _settings.HasColumnLayout = true;

            if (listViewPartitions.Columns.Count == 4)
            {
                _settings.PartitionColumnNameWidth = listViewPartitions.Columns[0].Width;
                _settings.PartitionColumnSizeWidth = listViewPartitions.Columns[1].Width;
                _settings.PartitionColumnFreeWidth = listViewPartitions.Columns[2].Width;
                _settings.PartitionColumnFreePercentWidth = listViewPartitions.Columns[3].Width;
            }
        }
        private void ApplyColumnLayout()
        {
            if (!_settings.HasColumnLayout)
                return;

            if (listViewPartitions.Columns.Count == 4)
            {
                if (_settings.PartitionColumnNameWidth > 0)
                    listViewPartitions.Columns[0].Width = _settings.PartitionColumnNameWidth;

                if (_settings.PartitionColumnSizeWidth > 0)
                    listViewPartitions.Columns[1].Width = _settings.PartitionColumnSizeWidth;

                if (_settings.PartitionColumnFreeWidth > 0)
                    listViewPartitions.Columns[2].Width = _settings.PartitionColumnFreeWidth;

                if (_settings.PartitionColumnFreePercentWidth > 0)
                    listViewPartitions.Columns[3].Width = _settings.PartitionColumnFreePercentWidth;
            }
        }
        private void AdjustPartitionColumns()
        {
            if (_settings.HasColumnLayout)
                return;

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
                string driveRootPath = Path.GetPathRoot(rootPath);

                if (string.IsNullOrWhiteSpace(driveRootPath))
                {
                    toolStripStatusLabel.Text = "Bereit";
                    SetStatusProgressText(null);
                    return;
                }

                System.IO.DriveInfo driveInfo = new System.IO.DriveInfo(driveRootPath);
                long clusterSize = GetClusterSize(driveRootPath);
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
        private string GetSelectedScanPath()
        {
            if (toolStripComboBoxDrives.SelectedItem is DriveItem driveItem)
                return driveItem.RootPath;

            return toolStripComboBoxDrives.Text == null
                ? string.Empty
                : toolStripComboBoxDrives.Text.Trim();
        }
        private void AddOrSelectPathInDriveComboBox(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            string fullPath = Path.GetFullPath(path);

            foreach (object item in toolStripComboBoxDrives.Items)
            {
                if (item is DriveItem driveItem &&
                    string.Equals(driveItem.RootPath, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    toolStripComboBoxDrives.SelectedItem = item;
                    return;
                }

                if (item is string itemPath &&
                    string.Equals(itemPath, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    toolStripComboBoxDrives.SelectedItem = item;
                    return;
                }
            }

            toolStripComboBoxDrives.Items.Add(fullPath);
            toolStripComboBoxDrives.SelectedItem = fullPath;
        }
        private async void toolStripButtonScan_Click(object sender, EventArgs e)
        {
            if (_scanCancellationTokenSource != null)
            {
                _scanCancellationTokenSource.Cancel();
                return;
            }

            string rootPath = GetSelectedScanPath();

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                MessageBox.Show(this, "Kein Pfad ausgewählt.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(rootPath))
            {
                MessageBox.Show(this, "Pfad nicht gefunden: " + rootPath, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AddOrSelectPathInDriveComboBox(rootPath);

            await StartScanAsync(rootPath);
        }

        private async Task StartScanAsync(string rootPath)
        {
            SetScanningState(true);
            dataGridViewEntries.DataSource = null;
            _currentRootEntry = null;
            _pendingLiveTreeScanProgress = null;
            treeViewEntries.Nodes.Clear();

            long scanTargetBytes = GetUsedSpaceBytes(rootPath);
            SetStatusProgressText(0D);

            _scanCancellationTokenSource = new CancellationTokenSource();
            int skippedDirectories = 0;
            HashSet<string> skippedDirectoryDetailSet = new HashSet<string>();
            List<string> skippedDirectoryDetails = new List<string>();

            Progress<ScanProgress> progress = new Progress<ScanProgress>(scanProgress =>
            {
                double percent = scanTargetBytes <= 0 ? 0D : (double)scanProgress.ScannedBytes * 100D / scanTargetBytes;
                skippedDirectories = Math.Max(skippedDirectories, scanProgress.SkippedDirectories);

                if (scanProgress.SkippedDirectoryDetails != null)
                {
                    foreach (string skippedDirectoryDetail in scanProgress.SkippedDirectoryDetails)
                    {
                        if (skippedDirectoryDetailSet.Add(skippedDirectoryDetail))
                        {
                            skippedDirectoryDetails.Add(skippedDirectoryDetail);
                        }
                    }
                }

                QueueLiveTreeUpdate(scanProgress);

                if (scanProgress.IsCacheSavePhase)
                {
                    toolStripStatusLabel.Text = string.Format(
                        "{0} | {1} | Ordner: {2} | Dateien: {3}",
                        scanProgress.CurrentPath,
                        SizeFormatter.Format(scanProgress.ScannedBytes),
                        scanProgress.ScannedDirectories,
                        scanProgress.ScannedFiles);
                }
                else if (scanProgress.IsCacheVerification)
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
                        "Schnellscan: {0} | {1} | Ordner: {2} | Dateien: {3}",
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
                NtQueryDirectoryScanner ntQueryDirectoryScanner = new NtQueryDirectoryScanner(_settings);

                string pathRoot = Path.GetPathRoot(rootPath);
                bool scanRootDrive = !string.IsNullOrWhiteSpace(pathRoot) &&
                    string.Equals(
                        Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        pathRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase);

                if (scanRootDrive && NtfsMftScanner.IsSupported(rootPath))
                {
                    try
                    {
                        toolStripStatusLabel.Text = "NTFS-MFT-Schnellscan läuft...";
                        NtfsMftScanner ntfsMftScanner = new NtfsMftScanner(_settings);
                        _currentRootEntry = await ntfsMftScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception mftException)
                    {
                        AppAlertLog.AddWarning("Scan", "MFT-Schnellscan nicht verfügbar: " + mftException.Message);

                        try
                        {
                            toolStripStatusLabel.Text = "MFT-Schnellscan nicht verfügbar - NT-API-Schnellscan läuft...";
                            _currentRootEntry = await ntQueryDirectoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ntQueryException)
                        {
                            AppAlertLog.AddWarning("Scan", "NT-API-Schnellscan nicht verfügbar: " + ntQueryException.Message);
                            toolStripStatusLabel.Text = "NT-API-Schnellscan nicht verfügbar - normaler Scan läuft...";
                            _currentRootEntry = await directoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                        }
                    }
                }
                else
                {
                    try
                    {
                        toolStripStatusLabel.Text = "NT-API-Schnellscan läuft...";
                        _currentRootEntry = await ntQueryDirectoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ntQueryException)
                    {
                        AppAlertLog.AddWarning("Scan", "NT-API-Schnellscan nicht verfügbar: " + ntQueryException.Message);
                        toolStripStatusLabel.Text = "NT-API-Schnellscan nicht verfügbar - normaler Scan läuft...";
                        _currentRootEntry = await directoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                    }
                }

                FlushPendingLiveTreeUpdate();
                RenderScanResult(_currentRootEntry);
                LoadPartitionList();
                UpdateStatusStripForDrive(rootPath);
                SetStatusProgressText(100D);

                if (skippedDirectories > 0)
                {
                    List<string> expectedSkippedDirectoryDetails = new List<string>();
                    List<string> warningSkippedDirectoryDetails = new List<string>();

                    foreach (string skippedDirectoryDetail in skippedDirectoryDetails)
                    {
                        string[] lines = skippedDirectoryDetail
                            .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                        string skippedDirectoryPath = lines.Length > 0 ? lines[0] : string.Empty;
                        string skippedDirectoryReason = skippedDirectoryDetail;

                        string normalizedSkippedDirectoryPath = skippedDirectoryPath
                            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                        bool isExpectedSystemDirectory =
                            normalizedSkippedDirectoryPath.EndsWith(
                                Path.DirectorySeparatorChar + "System Volume Information",
                                StringComparison.OrdinalIgnoreCase) &&
                            (skippedDirectoryReason.IndexOf("0xC0000022", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             skippedDirectoryReason.IndexOf("Zugriff verweigert", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             skippedDirectoryReason.IndexOf("Access is denied", StringComparison.OrdinalIgnoreCase) >= 0);

                        if (isExpectedSystemDirectory)
                        {
                            expectedSkippedDirectoryDetails.Add(skippedDirectoryDetail);
                        }
                        else
                        {
                            warningSkippedDirectoryDetails.Add(skippedDirectoryDetail);
                        }
                    }

                    int unknownSkippedDirectories = Math.Max(0, skippedDirectories - skippedDirectoryDetails.Count);

                    if (unknownSkippedDirectories > 0)
                    {
                        warningSkippedDirectoryDetails.Add(unknownSkippedDirectories + " weitere Ordner konnten nicht gelesen werden. Details wurden nicht erfasst.");
                    }

                    if (expectedSkippedDirectoryDetails.Count > 0)
                    {
                        string expectedSkippedDirectoryMessage = expectedSkippedDirectoryDetails.Count == 1
                            ? "1 Systemordner wurde erwartungsgemäß übersprungen."
                            : expectedSkippedDirectoryDetails.Count + " Systemordner wurden erwartungsgemäß übersprungen.";

                        string expectedSkippedDirectoryDetailsText = string.Join(Environment.NewLine + Environment.NewLine, expectedSkippedDirectoryDetails);

                        AppAlertLog.AddInformation("Scan", expectedSkippedDirectoryMessage, expectedSkippedDirectoryDetailsText);
                    }

                    if (warningSkippedDirectoryDetails.Count > 0)
                    {
                        string skippedDirectoryMessage = warningSkippedDirectoryDetails.Count == 1
                            ? "1 Ordner konnte nicht gelesen werden."
                            : warningSkippedDirectoryDetails.Count + " Ordner konnten nicht gelesen werden.";

                        string skippedDirectoryDetailsText = string.Join(Environment.NewLine + Environment.NewLine, warningSkippedDirectoryDetails);

                        AppAlertLog.AddWarning("Scan", skippedDirectoryMessage, skippedDirectoryDetailsText);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                toolStripStatusLabel.Text = "Scan abgebrochen";
                SetStatusProgressText(null);
            }
            finally
            {
                if (liveTreeUpdateTimer != null)
                {
                    liveTreeUpdateTimer.Stop();
                }

                _pendingLiveTreeScanProgress = null;
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
            if (scanProgress == null)
                return;

            if (scanProgress.LiveRootEntry == null)
                return;

            if (treeViewEntries.Nodes.Count == 0)
            {
                TreeNode rootNode = CreateLiveTreeNode(scanProgress.LiveRootEntry);
                treeViewEntries.Nodes.Add(rootNode);
                treeViewEntries.SelectedNode = rootNode;
                rootNode.Expand();
                SyncLiveTreeChildren(rootNode, scanProgress.LiveRootEntry, true);
                return;
            }

            TreeNode existingRootNode = treeViewEntries.Nodes[0];
            UpdateLiveTreeNode(existingRootNode, scanProgress.LiveRootEntry);
            SyncLiveTreeChildren(existingRootNode, scanProgress.LiveRootEntry, true);

            if (!existingRootNode.IsExpanded)
            {
                existingRootNode.Expand();
            }
        }

        private void SyncLiveTreeChildren(TreeNode parentNode, FileSystemEntry parentEntry, bool liveUpdate)
        {
            List<FileSystemEntry> childEntries = GetChildEntriesSnapshot(parentEntry);

            if (parentNode.Nodes.Count == 1 && IsLazyPlaceholderNode(parentNode.Nodes[0]) && childEntries.Count > 0)
            {
                parentNode.Nodes.Clear();
            }

            Dictionary<string, TreeNode> existingNodesByPath = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);

            foreach (TreeNode childNode in parentNode.Nodes)
            {
                if (childNode.Tag is FileSystemEntry childEntry && !string.IsNullOrWhiteSpace(childEntry.FullPath))
                {
                    existingNodesByPath[childEntry.FullPath] = childNode;
                }
            }

            foreach (FileSystemEntry childEntry in childEntries)
            {
                if (string.IsNullOrWhiteSpace(childEntry.FullPath))
                    continue;

                if (!existingNodesByPath.TryGetValue(childEntry.FullPath, out TreeNode childNode))
                {
                    childNode = CreateLiveTreeNode(childEntry);
                    parentNode.Nodes.Add(childNode);
                }
                else
                {
                    UpdateLiveTreeNode(childNode, childEntry);
                }

                if (childEntry.IsDirectory && childNode.IsExpanded)
                {
                    SyncLiveTreeChildren(childNode, childEntry, liveUpdate);
                }
                else if (childEntry.IsDirectory && HasChildEntries(childEntry) && childNode.Nodes.Count == 0)
                {
                    childNode.Nodes.Add(CreateLazyPlaceholderNode());
                }
            }

            if (!liveUpdate)
            {
                HashSet<string> expectedChildPaths = new HashSet<string>(
                    childEntries
                        .Where(child => !string.IsNullOrWhiteSpace(child.FullPath))
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
                string driveImageKey = EnsureTreeDriveIcon(entry.FullPath);
                node.ImageKey = driveImageKey;
                node.SelectedImageKey = driveImageKey;
            }

            if (entry.IsDirectory && HasChildEntries(entry))
            {
                node.Nodes.Add(CreateLazyPlaceholderNode());
            }

            return node;
        }
        private List<FileSystemEntry> GetChildEntriesSnapshot(FileSystemEntry entry)
        {
            if (entry == null)
            {
                return new List<FileSystemEntry>();
            }

            lock (entry.Children)
            {
                return entry.Children.ToList();
            }
        }
        private bool HasChildEntries(FileSystemEntry entry)
        {
            if (entry == null)
                return false;

            lock (entry.Children)
            {
                return entry.Children.Count > 0;
            }
        }

        private void UpdateLiveTreeNode(TreeNode node, FileSystemEntry entry)
        {
            node.Tag = entry;

            string text = SizeFormatter.Format(entry.SizeBytes) + "  " + entry.Name;

            if (node.Text != text)
            {
                node.Text = text;
            }

            string imageKey = entry.IsDirectory ? "Folder" : "File";
            string selectedImageKey = imageKey;

            if (entry.FullPath != null && entry.FullPath.EndsWith(":\\", StringComparison.OrdinalIgnoreCase))
            {
                imageKey = EnsureTreeDriveIcon(entry.FullPath);
                selectedImageKey = imageKey;
            }

            if (node.ImageKey != imageKey)
            {
                node.ImageKey = imageKey;
            }

            if (node.SelectedImageKey != selectedImageKey)
            {
                node.SelectedImageKey = selectedImageKey;
            }

            if (entry.IsDirectory && HasChildEntries(entry) && node.Nodes.Count == 0)
            {
                node.Nodes.Add(CreateLazyPlaceholderNode());
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
            toolStripButtonOpenFolder.Enabled = !scanning;
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
        private string EnsureTreeDriveIcon(string rootPath)
        {
            string imageKey = "Drive:" + rootPath;

            if (!imageListEntries.Images.ContainsKey(imageKey))
            {
                imageListEntries.Images.Add(imageKey, GetSmallSystemIcon(rootPath));
            }

            return imageKey;
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
                string driveImageKey = EnsureTreeDriveIcon(entry.FullPath);
                node.ImageKey = driveImageKey;
                node.SelectedImageKey = driveImageKey;
            }

            if (entry.IsDirectory && entry.Children.Any())
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
                         .OrderByDescending(child => child.IsDirectory)
                         .ThenByDescending(child => child.SizeBytes)
                         .ThenBy(child => child.Name))
            {
                parentNode.Nodes.Add(CreateTreeNode(child));
            }
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
                    "Erstellt: {0}{1}Geändert: {2}{1}Letzter Zugriff: {3}",
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
        private void dataGridViewEntries_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.RowIndex >= dataGridViewEntries.Rows.Count)
                return;

            if (dataGridViewEntries.Rows[e.RowIndex].DataBoundItem is not EntryChartItem entryChartItem)
                return;

            e.ToolTipText = FormatFileSystemDateToolTip(entryChartItem.FullPath);
        }
        private void BindGrid(FileSystemEntry entry)
        {
            _selectedEntry = entry;

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

            dataGridViewEntries.DataSource = rows;
            ApplyEntryGridColumnWidths();
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
            SavePersistentSettings();
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
            SaveColumnLayout();
            SaveViewSettings();
            TrySaveAppSettings(false);

            AppAlertLog.Changed -= AppAlertLog_Changed;

            base.OnFormClosing(e);
        }
    }
}