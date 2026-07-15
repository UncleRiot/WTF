using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lucid.Controls.GridView;

namespace WTF
{
    public sealed class MainForm : Form
    {
        private readonly AppSettings _settings;
        private readonly CsvExportService _csvExportService;
        private readonly ExportEntryController _exportEntryController;
        private readonly LayoutMainFormController _layoutMainFormController;
        private readonly StatusMainFormController _statusMainFormController;
        private readonly ScanExecutionController _scanExecutionController;
        private readonly ShellIconService _shellIconService;
        private readonly DriveComboBoxController _driveComboBoxController;
        private PartitionGridController _partitionGridController;
        private TreeEntryController _treeEntryController;

        private readonly Dictionary<string, ScanSession> _scanSessions = new Dictionary<string, ScanSession>(StringComparer.OrdinalIgnoreCase);
        private FileSystemEntry _currentRootEntry;
        private readonly string _startupScanPath;

        private MenuStrip menuStripMain;
        private ToolStripMenuItem menuItemFile;
        private ToolStripMenuItem menuItemExportCsv;
        private ToolStripMenuItem menuItemSettings;
        private ToolStripMenuItem menuItemSaveScanResult;
        private ToolStripMenuItem menuItemLoadScanResult;
        private ToolStripMenuItem menuItemAdvancedFeatures;
        private ToolStripMenuItem menuItemStorageHistory;
        private ToolStripMenuItem menuItemExit;
        private ToolStripMenuItem menuItemHelp;
        private ToolStripMenuItem menuItemAbout;
        private ToolStripPanel toolStripPanelMain;
        private ToolStrip toolStripMain;
        private ToolStripLabel toolStripLabelDrive;
        private ComboBox toolStripComboBoxDrives;
        private ToolStripControlHost toolStripComboBoxDrivesHost;
        private ToolStripButton toolStripButtonScan;
        private ToolStripButton toolStripButtonPause;
        private ToolStripButton toolStripButtonOpenFolder;
        private ToolStrip toolStripViewMode;
        private ToolStrip toolStripExport;
        private ToolStrip toolStripFeatures;
        private ToolStripButton toolStripButtonTable;
        private ToolStripButton toolStripButtonPieChart;
        private ToolStripButton toolStripButtonBarChart;
        private ToolStripButton toolStripButtonExportCsv;
        private ToolStripButton toolStripButtonAnalysis;
        private ToolStripButton toolStripButtonStorageHistory;
        private SplitContainer splitContainerMain;
        private SplitContainer splitContainerLeft;
        private TreeEntrySizeBarView treeViewEntries;
        private ContextMenuStrip contextMenuStripTreeEntries;
        private ToolStripMenuItem contextMenuItemOpenInExplorer;
        private ToolStripMenuItem contextMenuItemExport;
        private ToolStripMenuItem contextMenuItemCopyToClipboard;
        private ToolStripMenuItem contextMenuItemCopyPath;
        private ImageList imageListEntries;
        private LucidDataGridView listViewPartitions;
        private ImageList imageListPartitions;
        private Chart_TableGridChart dataGridViewEntries;
        private Panel panelRightViewHost;
        private Chart_PieChart pieChartView;
        private Chart_BarChart barChartView;
        private StatusStrip statusStripAlerts;
        private ToolStripStatusLabel toolStripAlertInformationLabel;
        private ToolStripStatusLabel toolStripAlertWarningLabel;
        private ToolStripStatusLabel toolStripAlertErrorLabel;
        private StatusStrip statusStripMain;
        private ToolStripStatusLabel toolStripStatusLabel;
        
        private bool _suspendPersistentSettingsSave;

        private void ApplyDriveComboBoxTheme()
        {
            bool useDarkMode = _settings.Layout == AppLayout.WindowsDarkMode;

            if (_settings.Layout == AppLayout.WindowsDefault)
            {
                try
                {
                    using Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

                    object value = key?.GetValue("AppsUseLightTheme");

                    if (value is int appsUseLightTheme)
                    {
                        useDarkMode = appsUseLightTheme == 0;
                    }
                }
                catch
                {
                    useDarkMode = false;
                }
            }

            Color backColor = useDarkMode
                ? Color.FromArgb(32, 32, 32)
                : Color.White;
            Color foreColor = useDarkMode
                ? Color.White
                : Color.Black;

            _driveComboBoxController.ApplyTheme(backColor, foreColor);
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            _layoutMainFormController.UpdateRightViewBounds();
        }
        private void splitContainerMainPanel2_SizeChanged(object sender, EventArgs e)
        {
            _layoutMainFormController.UpdateRightViewBounds();
        }
        public MainForm()
    : this(null)
        {
        }
        public MainForm(string startupScanPath)
        {
            _suspendPersistentSettingsSave = true;

            _settings = AppSettings.Load();
            LocalizationService.Load(_settings.LanguageCode);
            _csvExportService = new CsvExportService();
            _shellIconService = new ShellIconService();
            _startupScanPath = startupScanPath;

            LucidThemeService.Apply(_settings.Layout);
            InitializeComponent();
            _statusMainFormController = new StatusMainFormController(
                _settings,
                this,
                toolStripStatusLabel,
                statusStripMain,
                toolStripAlertInformationLabel,
                toolStripAlertWarningLabel,
                toolStripAlertErrorLabel);
            _scanExecutionController = new ScanExecutionController(_settings, _statusMainFormController);
            _exportEntryController = new ExportEntryController(
                _csvExportService,
                _settings,
                this,
                _statusMainFormController.SetStatusText);
            _layoutMainFormController = new LayoutMainFormController(
                _settings,
                this,
                splitContainerMain,
                splitContainerLeft,
                toolStripPanelMain,
                toolStripMain,
                toolStripViewMode,
                toolStripExport,
                toolStripFeatures,
                panelRightViewHost,
                dataGridViewEntries,
                pieChartView,
                barChartView,
                toolStripButtonTable,
                toolStripButtonPieChart,
                toolStripButtonBarChart,
                _settings.SelectedViewMode);
            _driveComboBoxController = new DriveComboBoxController(
                toolStripComboBoxDrives,
                _shellIconService,
                _statusMainFormController.UpdateStatusStripForDrive,
                DriveComboBoxScanPathSelectionCommitted);
            _partitionGridController = new PartitionGridController(
                _settings,
                splitContainerLeft,
                listViewPartitions,
                imageListPartitions,
                _shellIconService);
            _treeEntryController = new TreeEntryController(
                treeViewEntries,
                imageListEntries,
                _shellIconService,
                contextMenuStripTreeEntries,
                contextMenuItemOpenInExplorer,
                contextMenuItemExport,
                contextMenuItemCopyToClipboard,
                entry => _layoutMainFormController.BindGrid(entry));
            AppAlertLog.Changed += _statusMainFormController.AppAlertLogChanged;
            _statusMainFormController.ConfigureAlertStatusStrip();
            _driveComboBoxController.Configure();
            _partitionGridController.Configure();
            ConfigureOpenFolderButtonImage();
            _layoutMainFormController.ApplyMainWindowSettings();
            _layoutMainFormController.ApplyDefaultToolStripLayout();
            _layoutMainFormController.ApplyToolStripLayout();
            _layoutMainFormController.ApplySplitterLayout();

            SizeChanged += MainForm_SizeChanged;
            Shown += MainForm_Shown;
            panelRightViewHost.SizeChanged += panelRightViewHost_SizeChanged;
            splitContainerMain.SplitterMoved += splitContainerMain_SplitterMoved;
            splitContainerMain.Panel2.SizeChanged += splitContainerMainPanel2_SizeChanged;

            SetDoubleBuffered(treeViewEntries, true);
            SetDoubleBuffered(dataGridViewEntries, true);
            SetDoubleBuffered(listViewPartitions, true);
            SetDoubleBuffered(pieChartView, true);
            SetDoubleBuffered(barChartView, true);

            WindowsFormStyler.Apply(this, _settings.Layout);
            ApplyDriveComboBoxTheme();
            toolStripMain.GripStyle = ToolStripGripStyle.Visible;
            toolStripViewMode.GripStyle = ToolStripGripStyle.Visible;
            toolStripExport.GripStyle = ToolStripGripStyle.Visible;
            toolStripFeatures.GripStyle = ToolStripGripStyle.Visible;
            _driveComboBoxController.LoadDrives();
            _partitionGridController.LoadPartitionList();
            _partitionGridController.AdjustColumns();
            _partitionGridController.UpdatePartitionPanelVisibility();
            _layoutMainFormController.SetViewMode(_settings.SelectedViewMode, _suspendPersistentSettingsSave);
            _layoutMainFormController.UpdateRightViewBounds();
            SetScanningState(false);

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

                _driveComboBoxController.AddOrSelectPath(_startupScanPath);
                await StartScanAsync(_startupScanPath);
            }));
        }
        private void SavePersistentSettings()
        {
            _layoutMainFormController.SavePersistentSettings(_suspendPersistentSettingsSave);
        }
        private void toolStripLayout_LocationChanged(object sender, EventArgs e)
        {
            SavePersistentSettings();
        }



        private void ConfigureOpenFolderButtonImage()
        {
            toolStripButtonOpenFolder.Image = _shellIconService.GetSmallStockIcon(ShellStockIconId.FolderOpen);
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

        private System.Drawing.Bitmap CreateStopButtonImage()
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(System.Drawing.Color.Transparent);

                System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(4, 4, 8, 8);

                using System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(196, 43, 28));
                graphics.FillRectangle(brush, rectangle);

                using System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(135, 24, 15));
                graphics.DrawRectangle(pen, rectangle);
            }

            return bitmap;
        }
                        private void panelRightViewHost_SizeChanged(object sender, EventArgs e)
        {
            _layoutMainFormController.UpdateRightViewBounds();
        }
        private void splitContainerMain_SplitterMoved(object sender, SplitterEventArgs e)
        {
            _layoutMainFormController.UpdateRightViewBounds();
        }
        private void InitializeComponent()
        {
            Text = LocalizationService.GetText("App.Title");
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new System.Drawing.Size(780, 490);
            Size = new System.Drawing.Size(1180, 760);
            MaximizeBox = true;
            SizeGripStyle = SizeGripStyle.Show;

            menuStripMain = new MenuStrip();
            menuStripMain.Padding = new Padding(0, 2, 0, 2);

            menuItemFile = new ToolStripMenuItem(LocalizationService.GetText("Menu.File"));
            menuItemExportCsv = new ToolStripMenuItem(LocalizationService.GetText("Menu.ExportCsv"));
            menuItemSettings = new ToolStripMenuItem(LocalizationService.GetText("Menu.Settings"));
            menuItemSaveScanResult = new ToolStripMenuItem(LocalizationService.GetText("Menu.SaveScanResult"));
            menuItemLoadScanResult = new ToolStripMenuItem(LocalizationService.GetText("Menu.LoadScanResult"));
            menuItemAdvancedFeatures = new ToolStripMenuItem(LocalizationService.GetText("Menu.Analysis"));
            menuItemStorageHistory = new ToolStripMenuItem(LocalizationService.GetText("Menu.StorageHistory"));
            menuItemExit = new ToolStripMenuItem(LocalizationService.GetText("Menu.Exit"));
            menuItemHelp = new ToolStripMenuItem(LocalizationService.GetText("Menu.Help"));
            menuItemAbout = new ToolStripMenuItem(LocalizationService.GetText("Menu.About"));

            menuItemFile.DropDownItems.Add(menuItemExportCsv);
            menuItemFile.DropDownItems.Add(menuItemSaveScanResult);
            menuItemFile.DropDownItems.Add(menuItemLoadScanResult);
            menuItemFile.DropDownItems.Add(menuItemAdvancedFeatures);
            menuItemFile.DropDownItems.Add(menuItemStorageHistory);
            menuItemFile.DropDownItems.Add(new ToolStripSeparator());
            menuItemFile.DropDownItems.Add(menuItemSettings);
            menuItemFile.DropDownItems.Add(new ToolStripSeparator());
            menuItemFile.DropDownItems.Add(menuItemExit);
            menuItemHelp.DropDownItems.Add(menuItemAbout);
            menuStripMain.Items.Add(menuItemFile);
            menuStripMain.Items.Add(menuItemHelp);

            menuItemExportCsv.Click += menuItemExportCsv_Click;
            menuItemSettings.Click += menuItemSettings_Click;
            menuItemSaveScanResult.Click += menuItemSaveScanResult_Click;
            menuItemLoadScanResult.Click += menuItemLoadScanResult_Click;
            menuItemAdvancedFeatures.Click += menuItemAdvancedFeatures_Click;
            menuItemStorageHistory.Click += menuItemStorageHistory_Click;
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

            toolStripLabelDrive = new ToolStripLabel(LocalizationService.GetText("Toolbar.Drive"));
            toolStripComboBoxDrives = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                FlatStyle = FlatStyle.Flat,
                IntegralHeight = false,
                ItemHeight = 20,
                Size = new Size(260, 28)
            };
            toolStripComboBoxDrivesHost = new ToolStripControlHost(toolStripComboBoxDrives)
            {
                AutoSize = false,
                Size = new Size(260, 28),
                Margin = new Padding(0)
            };
            toolStripButtonScan = new ToolStripButton("▶");
            toolStripButtonPause = new ToolStripButton("⏸");
            toolStripButtonOpenFolder = new ToolStripButton(LocalizationService.GetText("Toolbar.Open"));

            toolStripLabelDrive.Margin = new Padding(0, 1, 0, 2);
            toolStripButtonScan.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonScan.ToolTipText = LocalizationService.GetText("Toolbar.ScanStart");
            toolStripButtonScan.Click += toolStripButtonScan_Click;
            toolStripButtonPause.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonPause.ToolTipText = LocalizationService.GetText("Toolbar.PauseResume");
            toolStripButtonPause.Enabled = false;
            toolStripButtonPause.Click += toolStripButtonPause_Click;
            toolStripButtonOpenFolder.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonOpenFolder.ToolTipText = LocalizationService.GetText("Toolbar.SelectFolderAndScan");
            toolStripButtonOpenFolder.Click += toolStripButtonOpenFolder_Click;

            toolStripMain.Items.Add(toolStripLabelDrive);
            toolStripMain.Items.Add(toolStripComboBoxDrivesHost);
            toolStripMain.Items.Add(toolStripButtonScan);
            toolStripMain.Items.Add(toolStripButtonPause);
            toolStripMain.Items.Add(toolStripButtonOpenFolder);

            toolStripViewMode = new ToolStrip();
            toolStripViewMode.Dock = DockStyle.None;
            toolStripViewMode.GripStyle = ToolStripGripStyle.Visible;
            toolStripViewMode.AllowItemReorder = true;
            toolStripViewMode.Padding = new Padding(0);
            toolStripViewMode.Margin = new Padding(0);

            toolStripButtonTable = new ToolStripButton(LocalizationService.GetText("Toolbar.Table"));
            toolStripButtonPieChart = new ToolStripButton(LocalizationService.GetText("Toolbar.PieChart"));
            toolStripButtonBarChart = new ToolStripButton(LocalizationService.GetText("Toolbar.BarChart"));

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

            toolStripButtonExportCsv = new ToolStripButton(LocalizationService.GetText("Toolbar.Export"));
            toolStripButtonExportCsv.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            toolStripButtonExportCsv.Image = CreateExportButtonImage();
            toolStripButtonExportCsv.ToolTipText = LocalizationService.GetText("Toolbar.ExportCsv");
            toolStripButtonExportCsv.Enabled = false;
            menuItemExportCsv.Enabled = false;
            toolStripButtonExportCsv.Click += toolStripButtonExportCsv_Click;

            toolStripExport.Items.Add(toolStripButtonExportCsv);

            toolStripFeatures = new ToolStrip();
            toolStripFeatures.Dock = DockStyle.None;
            toolStripFeatures.GripStyle = ToolStripGripStyle.Visible;
            toolStripFeatures.AllowItemReorder = true;
            toolStripFeatures.Padding = new Padding(0);
            toolStripFeatures.Margin = new Padding(0);

            toolStripButtonAnalysis = new ToolStripButton(LocalizationService.GetText("Menu.Analysis"));
            toolStripButtonAnalysis.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            toolStripButtonAnalysis.Image = CreateAnalysisButtonImage();
            toolStripButtonAnalysis.Click += menuItemAdvancedFeatures_Click;

            toolStripButtonStorageHistory = new ToolStripButton(LocalizationService.GetText("Menu.StorageHistory"));
            toolStripButtonStorageHistory.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            toolStripButtonStorageHistory.Image = CreateStorageHistoryButtonImage();
            toolStripButtonStorageHistory.Click += menuItemStorageHistory_Click;

            toolStripFeatures.Items.Add(toolStripButtonAnalysis);
            toolStripFeatures.Items.Add(toolStripButtonStorageHistory);

            toolStripPanelMain.Join(toolStripMain, 0, 0);
            toolStripPanelMain.Join(toolStripViewMode, 340, 0);
            toolStripPanelMain.Join(toolStripExport, 610, 0);
            toolStripPanelMain.Join(toolStripFeatures, 720, 0);

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
            imageListEntries.Images.Add("Drive", _shellIconService.GetSmallSystemIcon(Environment.SystemDirectory));
            imageListEntries.Images.Add("Folder", _shellIconService.GetSmallSystemIcon(Environment.GetFolderPath(Environment.SpecialFolder.Windows)));
            imageListEntries.Images.Add("File", System.Drawing.SystemIcons.Application.ToBitmap());

            treeViewEntries = new TreeEntrySizeBarView();
            treeViewEntries.Dock = DockStyle.Fill;
            treeViewEntries.EntryImageList = imageListEntries;
            treeViewEntries.ShellIconService = _shellIconService;
            treeViewEntries.RowHeight = 22;

            contextMenuStripTreeEntries = new ContextMenuStrip();
            contextMenuItemOpenInExplorer = new ToolStripMenuItem(LocalizationService.GetText("Context.OpenInExplorer"));
            contextMenuItemExport = new ToolStripMenuItem(LocalizationService.GetText("Context.Export"));
            contextMenuItemCopyToClipboard = new ToolStripMenuItem(LocalizationService.GetText("Context.CopyToClipboard"));
            contextMenuItemCopyPath = new ToolStripMenuItem(LocalizationService.GetText("Context.CopyPath"));
            contextMenuItemOpenInExplorer.Click += contextMenuItemOpenInExplorer_Click;
            contextMenuItemExport.Click += contextMenuItemExport_Click;
            contextMenuItemCopyToClipboard.Click += contextMenuItemCopyToClipboard_Click;
            contextMenuItemCopyPath.Click += contextMenuItemCopyPath_Click;
            contextMenuStripTreeEntries.Items.Add(contextMenuItemExport);
            contextMenuStripTreeEntries.Items.Add(contextMenuItemCopyToClipboard);
            contextMenuStripTreeEntries.Items.Add(contextMenuItemCopyPath);
            contextMenuStripTreeEntries.Items.Add(contextMenuItemOpenInExplorer);

            imageListPartitions = new ImageList();
            imageListPartitions.ColorDepth = ColorDepth.Depth32Bit;
            imageListPartitions.ImageSize = new System.Drawing.Size(16, 16);

            listViewPartitions = new LucidDataGridView();
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


            dataGridViewEntries = new Chart_TableGridChart();
            dataGridViewEntries.Dock = DockStyle.Fill;
            dataGridViewEntries.AllowUserToAddRows = false;
            dataGridViewEntries.AllowUserToDeleteRows = false;
            dataGridViewEntries.AllowUserToResizeRows = false;
            dataGridViewEntries.AutoGenerateColumns = false;
            dataGridViewEntries.ReadOnly = true;
            dataGridViewEntries.RowHeadersVisible = false;
            dataGridViewEntries.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewEntries.MultiSelect = false;

            pieChartView = new Chart_PieChart
            {
                Name = "pieChartView",
                Dock = DockStyle.Fill,
                Visible = false
            };

            barChartView = new Chart_BarChart
            {
                Name = "barChartView",
                Dock = DockStyle.Fill,
                Visible = false,
                BarHeight = _settings.BarChartBarHeight
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
                Text = "0"
            };

            toolStripAlertWarningLabel = new ToolStripStatusLabel
            {
                Name = "toolStripAlertWarningLabel",
                Text = "0"
            };

            toolStripAlertErrorLabel = new ToolStripStatusLabel
            {
                Name = "toolStripAlertErrorLabel",
                Text = "0"
            };


            statusStripAlerts.Items.Add(toolStripAlertInformationLabel);
            statusStripAlerts.Items.Add(toolStripAlertWarningLabel);
            statusStripAlerts.Items.Add(toolStripAlertErrorLabel);

            statusStripMain = new StatusStrip();
            statusStripMain.SizingGrip = true;
            statusStripMain.Dock = DockStyle.Bottom;

            toolStripStatusLabel = new ToolStripStatusLabel(LocalizationService.GetText("Common.Ready"))
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
            statusStripAlerts.Dock = DockStyle.Fill;

            tableLayoutPanelMain.Controls.Add(menuStripMain, 0, 0);
            tableLayoutPanelMain.Controls.Add(toolStripPanelMain, 0, 1);
            tableLayoutPanelMain.Controls.Add(splitContainerMain, 0, 2);
            tableLayoutPanelMain.Controls.Add(statusStripAlerts, 0, 3);

            Controls.Add(tableLayoutPanelMain);
            Controls.Add(statusStripMain);

            MainMenuStrip = menuStripMain;
        }

        private void ApplyLocalizedTexts()
        {
            Text = LocalizationService.GetText("App.Title");

            menuItemFile.Text = LocalizationService.GetText("Menu.File");
            menuItemExportCsv.Text = LocalizationService.GetText("Menu.ExportCsv");
            menuItemSaveScanResult.Text = LocalizationService.GetText("Menu.SaveScanResult");
            menuItemLoadScanResult.Text = LocalizationService.GetText("Menu.LoadScanResult");
            menuItemAdvancedFeatures.Text = LocalizationService.GetText("Menu.Analysis");
            menuItemStorageHistory.Text = LocalizationService.GetText("Menu.StorageHistory");
            menuItemSettings.Text = LocalizationService.GetText("Menu.Settings");
            menuItemExit.Text = LocalizationService.GetText("Menu.Exit");
            menuItemHelp.Text = LocalizationService.GetText("Menu.Help");
            menuItemAbout.Text = LocalizationService.GetText("Menu.About");

            toolStripLabelDrive.Text = LocalizationService.GetText("Toolbar.Drive");
            toolStripButtonOpenFolder.Text = LocalizationService.GetText("Toolbar.Open");
            string selectedScanPath = NormalizeScanPath(_driveComboBoxController.GetSelectedScanPath());
            bool selectedScanIsRunning = _scanSessions.TryGetValue(selectedScanPath, out ScanSession selectedScanSession) &&
                selectedScanSession.IsRunning;
            toolStripButtonScan.ToolTipText = selectedScanIsRunning
                ? LocalizationService.GetText("Toolbar.ScanCancel")
                : LocalizationService.GetText("Toolbar.ScanStart");
            toolStripButtonOpenFolder.ToolTipText = LocalizationService.GetText("Toolbar.SelectFolderAndScan");
            toolStripButtonPause.ToolTipText = LocalizationService.GetText("Toolbar.PauseResume");
            toolStripButtonTable.Text = LocalizationService.GetText("Toolbar.Table");
            toolStripButtonPieChart.Text = LocalizationService.GetText("Toolbar.PieChart");
            toolStripButtonBarChart.Text = LocalizationService.GetText("Toolbar.BarChart");
            toolStripButtonExportCsv.Text = LocalizationService.GetText("Toolbar.Export");
            toolStripButtonExportCsv.ToolTipText = LocalizationService.GetText("Toolbar.ExportCsv");
            toolStripButtonAnalysis.Text = LocalizationService.GetText("Menu.Analysis");
            toolStripButtonStorageHistory.Text = LocalizationService.GetText("Menu.StorageHistory");

            contextMenuItemOpenInExplorer.Text = LocalizationService.GetText("Context.OpenInExplorer");
            contextMenuItemExport.Text = LocalizationService.GetText("Context.Export");
            contextMenuItemCopyToClipboard.Text = LocalizationService.GetText("Context.CopyToClipboard");
            contextMenuItemCopyPath.Text = LocalizationService.GetText("Context.CopyPath");

            _statusMainFormController?.ApplyLocalizedTexts();

            _partitionGridController?.ApplyLocalizedTexts();

            dataGridViewEntries.ApplyLocalizedTexts();
        }

        private System.Drawing.Bitmap CreateAnalysisButtonImage()
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(System.Drawing.Color.Transparent);

                using System.Drawing.Pen axisPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(110, 110, 110));
                using System.Drawing.Pen graphPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 120, 215), 2f);

                graphics.DrawLine(axisPen, 2, 2, 2, 13);
                graphics.DrawLine(axisPen, 2, 13, 14, 13);

                System.Drawing.Point[] points =
                {
                    new System.Drawing.Point(3, 11),
                    new System.Drawing.Point(6, 8),
                    new System.Drawing.Point(9, 10),
                    new System.Drawing.Point(13, 4)
                };

                graphics.DrawLines(graphPen, points);
            }

            return bitmap;
        }

        private System.Drawing.Bitmap CreateStorageHistoryButtonImage()
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(System.Drawing.Color.Transparent);

                using System.Drawing.Pen clockPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 120, 215), 2f);
                using System.Drawing.Pen handPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(90, 90, 90), 2f);

                graphics.DrawEllipse(clockPen, 2, 2, 12, 12);
                graphics.DrawLine(handPen, 8, 8, 8, 4);
                graphics.DrawLine(handPen, 8, 8, 11, 10);
            }

            return bitmap;
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
                Description = LocalizationService.GetText("Dialog.SelectFolder"),
                ShowNewFolderButton = false
            };

            if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                return;

            _driveComboBoxController.AddOrSelectPath(folderBrowserDialog.SelectedPath);

            await StartScanAsync(folderBrowserDialog.SelectedPath);
        }

        private async void DriveComboBoxScanPathSelectionCommitted(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                return;

            if (!Directory.Exists(rootPath))
            {
                MessageBox.Show(this, LocalizationService.GetText("Message.PathNotFoundPrefix") + rootPath, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await StartScanAsync(rootPath);
        }

        private async void toolStripButtonScan_Click(object sender, EventArgs e)
        {
            string rootPath = _driveComboBoxController.GetSelectedScanPath();

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                MessageBox.Show(this, LocalizationService.GetText("Message.NoPathSelected"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string normalizedRootPath = NormalizeScanPath(rootPath);

            if (_scanSessions.TryGetValue(normalizedRootPath, out ScanSession existingSession) && existingSession.IsRunning)
            {
                existingSession.CancellationTokenSource.Cancel();
                return;
            }

            if (!Directory.Exists(rootPath))
            {
                MessageBox.Show(this, LocalizationService.GetText("Message.PathNotFoundPrefix") + rootPath, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _driveComboBoxController.AddOrSelectPath(rootPath);
            await StartScanAsync(rootPath);
        }

        private async Task StartScanAsync(string rootPath)
        {
            string normalizedRootPath = NormalizeScanPath(rootPath);

            if (_scanSessions.TryGetValue(normalizedRootPath, out ScanSession existingSession) && existingSession.IsRunning)
            {
                existingSession.CancellationTokenSource.Cancel();
            }

            ScanSession session = new ScanSession
            {
                RootPath = normalizedRootPath,
                CancellationTokenSource = new CancellationTokenSource(),
                PauseTokenSource = new PauseTokenSource(),
                IsRunning = true,
                ScanTargetBytes = GetUsedSpaceBytes(rootPath)
            };

            _scanSessions[normalizedRootPath] = session;
            _treeEntryController.ClearPendingLiveTreeUpdate(normalizedRootPath);

            FileSystemEntry initialRootEntry = new FileSystemEntry
            {
                Name = normalizedRootPath,
                FullPath = normalizedRootPath,
                IsDirectory = true
            };

            session.RootEntry = initialRootEntry;
            _currentRootEntry = initialRootEntry;
            RenderScanResult(initialRootEntry);
            SetScanningState(true);

            Progress<ScanProgress> progress = new Progress<ScanProgress>(scanProgress =>
            {
                if (!IsCurrentScanSession(session))
                    return;

                session.LatestProgress = scanProgress;
                session.SkippedDirectories = Math.Max(session.SkippedDirectories, scanProgress.SkippedDirectories);

                if (scanProgress.SkippedDirectoryDetails != null)
                {
                    foreach (string skippedDirectoryDetail in scanProgress.SkippedDirectoryDetails)
                    {
                        if (session.SkippedDirectoryDetailSet.Add(skippedDirectoryDetail))
                        {
                            session.SkippedDirectoryDetails.Add(skippedDirectoryDetail);
                        }
                    }
                }

                _treeEntryController.QueueLiveTreeUpdate(scanProgress);

                if (IsSelectedScanPath(session.RootPath))
                {
                    UpdateSelectedScanStatus(session, scanProgress);
                }
            });

            try
            {
                FileSystemEntry rootEntry = await _scanExecutionController.ScanAsync(
                    rootPath,
                    progress,
                    session.CancellationTokenSource.Token,
                    session.PauseTokenSource.Token,
                    statusKey =>
                    {
                        if (IsCurrentScanSession(session) && IsSelectedScanPath(session.RootPath))
                        {
                            _statusMainFormController.SetStatusTextByKey(statusKey);
                        }
                    });

                if (!IsCurrentScanSession(session))
                    return;

                session.RootEntry = rootEntry;
                session.LatestProgress = null;
                StorageHistoryService.AddRecord(rootEntry.FullPath, rootEntry.SizeBytes);

                _treeEntryController.FlushPendingLiveTreeUpdate();
                _treeEntryController.UpdateScanResult(rootEntry);
                _partitionGridController.LoadPartitionList();

                if (IsSelectedScanPath(session.RootPath))
                {
                    _currentRootEntry = rootEntry;
                    _layoutMainFormController.BindGrid(rootEntry);
                    ApplyEntryColumnVisibility();
                    _statusMainFormController.UpdateStatusStripForDrive(rootPath);
                    _statusMainFormController.SetStatusProgressText(100D);
                    _statusMainFormController.ReportSkippedDirectories(session.SkippedDirectories, session.SkippedDirectoryDetails);
                }
            }
            catch (OperationCanceledException)
            {
                session.WasCanceled = true;

                if (IsCurrentScanSession(session) && IsSelectedScanPath(session.RootPath))
                {
                    _statusMainFormController.SetStatusTextByKey("Status.ScanCanceled");
                    _statusMainFormController.SetStatusProgressText(null);
                }
            }
            finally
            {
                session.IsRunning = false;
                session.PauseTokenSource.Dispose();
                session.CancellationTokenSource.Dispose();

                if (IsCurrentScanSession(session) && IsSelectedScanPath(session.RootPath))
                {
                    SetScanningState(false);
                }
            }
        }

        private bool IsCurrentScanSession(ScanSession session)
        {
            return session != null &&
                _scanSessions.TryGetValue(session.RootPath, out ScanSession currentSession) &&
                ReferenceEquals(currentSession, session);
        }



        private void ShowScanSession(string rootPath)
        {
            string normalizedRootPath = NormalizeScanPath(rootPath);
            _treeEntryController.StopLiveTreeUpdateTimer();
            _treeEntryController.ClearPendingLiveTreeUpdate();

            if (!_scanSessions.TryGetValue(normalizedRootPath, out ScanSession session))
            {
                _currentRootEntry = null;
                _treeEntryController.ClearEntries();
                _layoutMainFormController.BindGrid(null);
                SetScanningState(false);
                _statusMainFormController.UpdateStatusStripForDrive(rootPath);
                return;
            }

            _currentRootEntry = session.RootEntry;

            if (session.RootEntry != null)
            {
                RenderScanResult(session.RootEntry);
            }
            else if (session.LatestProgress?.LiveRootEntry != null)
            {
                _treeEntryController.RenderScanResult(session.LatestProgress.LiveRootEntry);
                _layoutMainFormController.BindGrid(session.LatestProgress.LiveRootEntry);
                ApplyEntryColumnVisibility();
            }
            else
            {
                _treeEntryController.ClearEntries();
                _layoutMainFormController.BindGrid(null);
            }

            SetScanningState(session.IsRunning);

            if (session.IsRunning && session.LatestProgress != null)
            {
                UpdateSelectedScanStatus(session, session.LatestProgress);
                _treeEntryController.QueueLiveTreeUpdate(session.LatestProgress);
            }
            else
            {
                _statusMainFormController.UpdateStatusStripForDrive(rootPath);
            }
        }

        private void UpdateSelectedScanStatus(ScanSession session, ScanProgress scanProgress)
        {
            double percent = session.ScanTargetBytes <= 0
                ? 0D
                : (double)scanProgress.ScannedBytes * 100D / session.ScanTargetBytes;

            if (scanProgress.IsCacheSavePhase)
            {
                _statusMainFormController.SetFormattedStatusText(
                    "Status.ScanCacheSave",
                    scanProgress.CurrentPath,
                    SizeFormatter.Format(scanProgress.ScannedBytes),
                    scanProgress.ScannedDirectories,
                    scanProgress.ScannedFiles);
            }
            else if (scanProgress.IsCacheVerification)
            {
                _statusMainFormController.SetFormattedStatusText(
                    "Status.CacheVerification",
                    scanProgress.CurrentPath,
                    SizeFormatter.Format(scanProgress.ScannedBytes),
                    scanProgress.ScannedDirectories,
                    scanProgress.ScannedFiles);
            }
            else
            {
                _statusMainFormController.SetFormattedStatusText(
                    "Status.FastScan",
                    scanProgress.CurrentPath,
                    SizeFormatter.Format(scanProgress.ScannedBytes),
                    scanProgress.ScannedDirectories,
                    scanProgress.ScannedFiles);
            }

            _statusMainFormController.SetStatusProgressText(percent);
        }

        private bool IsSelectedScanPath(string rootPath)
        {
            return string.Equals(
                NormalizeScanPath(_driveComboBoxController.GetSelectedScanPath()),
                NormalizeScanPath(rootPath),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeScanPath(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                return string.Empty;

            try
            {
                string fullPath = Path.GetFullPath(rootPath);
                string pathRoot = Path.GetPathRoot(fullPath);

                if (!string.IsNullOrWhiteSpace(pathRoot) &&
                    string.Equals(
                        fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        pathRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return pathRoot;
                }

                return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return rootPath.Trim();
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
            Image oldImage = toolStripButtonScan.Image;
            toolStripButtonScan.Image = scanning ? CreateStopButtonImage() : CreateScanButtonImage();
            oldImage?.Dispose();

            toolStripButtonScan.Text = string.Empty;
            toolStripButtonScan.DisplayStyle = ToolStripItemDisplayStyle.Image;
            toolStripButtonScan.ToolTipText = scanning ? LocalizationService.GetText("Toolbar.ScanCancel") : LocalizationService.GetText("Toolbar.ScanStart");
            _driveComboBoxController.SetEnabled(true);
            toolStripButtonOpenFolder.Enabled = !scanning;
            toolStripButtonPause.Enabled = scanning;

            if (!scanning)
            {
                toolStripButtonPause.Text = "⏸";
            }

            menuItemExportCsv.Enabled = !scanning && _currentRootEntry != null;
            menuItemSaveScanResult.Enabled = !scanning && _currentRootEntry != null;
            menuItemAdvancedFeatures.Enabled = !scanning && _currentRootEntry != null;
            toolStripButtonAnalysis.Enabled = !scanning && _currentRootEntry != null;
            toolStripButtonExportCsv.Enabled = !scanning && _currentRootEntry != null;
            splitContainerMain.IsSplitterFixed = false;
            splitContainerLeft.IsSplitterFixed = false;
        }

        private void RenderScanResult(FileSystemEntry rootEntry)
        {
            TreeSortService.Sort(rootEntry, _settings.TreeSortMode);
            _treeEntryController.RenderScanResult(rootEntry);
            _layoutMainFormController.BindGrid(rootEntry);
            ApplyEntryColumnVisibility();
        }

        private void toolStripButtonTable_Click(object sender, EventArgs e)
        {
            _layoutMainFormController.SetViewMode(ViewMode.Table, _suspendPersistentSettingsSave);
        }
        private void toolStripButtonPieChart_Click(object sender, EventArgs e)
        {
            _layoutMainFormController.SetViewMode(ViewMode.PieChart, _suspendPersistentSettingsSave);
        }
        private void toolStripButtonBarChart_Click(object sender, EventArgs e)
        {
            _layoutMainFormController.SetViewMode(ViewMode.BarChart, _suspendPersistentSettingsSave);
        }

        private void toolStripButtonExportCsv_Click(object sender, EventArgs e)
        {
            _exportEntryController.ExportEntry(_currentRootEntry);
        }

        private void menuItemExportCsv_Click(object sender, EventArgs e)
        {
            _exportEntryController.ExportEntry(_currentRootEntry);
        }


        private void contextMenuItemOpenInExplorer_Click(object sender, EventArgs e)
        {
            FileSystemEntry contextMenuEntry = _treeEntryController.ContextMenuEntry;

            if (contextMenuEntry == null || string.IsNullOrWhiteSpace(contextMenuEntry.FullPath))
                return;

            string arguments = File.Exists(contextMenuEntry.FullPath)
                ? "/select,\"" + contextMenuEntry.FullPath + "\""
                : "\"" + contextMenuEntry.FullPath + "\"";

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = arguments,
                UseShellExecute = true
            });
        }

        private void contextMenuItemExport_Click(object sender, EventArgs e)
        {
            _exportEntryController.ExportEntry(_treeEntryController.ContextMenuEntry);
        }

        private void contextMenuItemCopyToClipboard_Click(object sender, EventArgs e)
        {
            _exportEntryController.CopyEntryExportToClipboard(_treeEntryController.ContextMenuEntry);
        }

        private void toolStripButtonPause_Click(object sender, EventArgs e)
        {
            string rootPath = NormalizeScanPath(_driveComboBoxController.GetSelectedScanPath());

            if (!_scanSessions.TryGetValue(rootPath, out ScanSession session) || !session.IsRunning)
                return;

            if (session.PauseTokenSource.IsPaused)
            {
                session.PauseTokenSource.Resume();
                toolStripButtonPause.Text = "⏸";
                _statusMainFormController.SetStatusTextByKey("Status.NtQueryRunning");
            }
            else
            {
                session.PauseTokenSource.Pause();
                toolStripButtonPause.Text = "▶";
                _statusMainFormController.SetStatusTextByKey("Status.ScanPaused");
            }
        }

        private void contextMenuItemCopyPath_Click(object sender, EventArgs e)
        {
            FileSystemEntry entry = _treeEntryController.ContextMenuEntry;

            if (entry == null || string.IsNullOrWhiteSpace(entry.FullPath))
                return;

            Clipboard.SetText(entry.FullPath);
        }

        private void menuItemSaveScanResult_Click(object sender, EventArgs e)
        {
            if (_currentRootEntry == null)
                return;

            using SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = "WTF Scan (*.wtfscan)|*.wtfscan|JSON (*.json)|*.json",
                DefaultExt = "wtfscan",
                FileName = "scan-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".wtfscan"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                ScanResultFileService.Save(dialog.FileName, _currentRootEntry);
            }
        }

        private void menuItemLoadScanResult_Click(object sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "WTF Scan (*.wtfscan;*.json)|*.wtfscan;*.json"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            FileSystemEntry loadedEntry = ScanResultFileService.Load(dialog.FileName);

            if (loadedEntry == null)
                return;

            _currentRootEntry = loadedEntry;
            RenderScanResult(_currentRootEntry);
            SetScanningState(false);
        }

        private void menuItemAdvancedFeatures_Click(object sender, EventArgs e)
        {
            if (_currentRootEntry == null)
                return;

            using AdvancedFeaturesForm form = new AdvancedFeaturesForm(_currentRootEntry, _settings, dataGridViewEntries);
            form.ShowDialog(this);
            RenderScanResult(_currentRootEntry);
        }

        private void menuItemStorageHistory_Click(object sender, EventArgs e)
        {
            using StorageHistoryForm storageHistoryForm = new StorageHistoryForm(_settings);
            storageHistoryForm.ShowDialog(this);
        }

        private void ApplyEntryColumnVisibility()
        {
            if (dataGridViewEntries.Columns.Contains("ColumnName"))
                dataGridViewEntries.Columns["ColumnName"].Visible = _settings.EntryColumnNameVisible;

            if (dataGridViewEntries.Columns.Contains("ColumnSize"))
                dataGridViewEntries.Columns["ColumnSize"].Visible = _settings.EntryColumnSizeVisible;

            if (dataGridViewEntries.Columns.Contains("ColumnPercent"))
                dataGridViewEntries.Columns["ColumnPercent"].Visible = _settings.EntryColumnPercentVisible;

            if (dataGridViewEntries.Columns.Contains("ColumnPath"))
                dataGridViewEntries.Columns["ColumnPath"].Visible = _settings.EntryColumnPathVisible;
        }

        private void menuItemSettings_Click(object sender, EventArgs e)
        {
            using SettingsForm settingsForm = new SettingsForm(_settings);

            if (settingsForm.ShowDialog(this) != DialogResult.OK)
                return;

            _settings.Save();
            LocalizationService.Load(_settings.LanguageCode);
            LucidThemeService.Apply(_settings.Layout);
            ApplyLocalizedTexts();
            _driveComboBoxController.LoadDrives();
            WindowsFormStyler.Apply(this, _settings.Layout);
            ApplyDriveComboBoxTheme();
            treeViewEntries.Invalidate();
            listViewPartitions.Invalidate();
            dataGridViewEntries.Invalidate();
            barChartView.BarHeight = _settings.BarChartBarHeight;
            toolStripMain.GripStyle = ToolStripGripStyle.Visible;
            toolStripViewMode.GripStyle = ToolStripGripStyle.Visible;
            _partitionGridController.UpdatePartitionPanelVisibility();
            _layoutMainFormController.UpdateRightViewBounds();

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


        private sealed class ScanSession
        {
            public string RootPath { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
            public PauseTokenSource PauseTokenSource { get; set; }
            public FileSystemEntry RootEntry { get; set; }
            public ScanProgress LatestProgress { get; set; }
            public long ScanTargetBytes { get; set; }
            public bool IsRunning { get; set; }
            public bool WasCanceled { get; set; }
            public int SkippedDirectories { get; set; }
            public HashSet<string> SkippedDirectoryDetailSet { get; } = new HashSet<string>();
            public List<string> SkippedDirectoryDetails { get; } = new List<string>();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            foreach (ScanSession session in _scanSessions.Values)
            {
                if (session.IsRunning)
                {
                    session.CancellationTokenSource.Cancel();
                }
            }

            _layoutMainFormController.SaveMainWindowSettings();
            _layoutMainFormController.SaveToolStripLayout();
            _layoutMainFormController.SaveSplitterLayout();
            _partitionGridController.SaveColumnLayout();
            _layoutMainFormController.SaveViewSettings();
            _settings.Save();

            base.OnFormClosing(e);
        }
    }
}