using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WTF
{
    public sealed class MainForm : Form
    {
        private readonly AppSettings _settings;
        private readonly CsvExportService _csvExportService;
        private readonly ExportEntryController _exportEntryController;
        private readonly LayoutMainFormController _layoutMainFormController;
        private readonly StatusMainFormController _statusMainFormController;
        private readonly ShellIconService _shellIconService;
        private readonly DriveComboBoxController _driveComboBoxController;
        private PartitionGridController _partitionGridController;
        private TreeEntryController _treeEntryController;

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
        private TreeEntrySizeBarView treeViewEntries;
        private ContextMenuStrip contextMenuStripTreeEntries;
        private ToolStripMenuItem contextMenuItemOpenInExplorer;
        private ToolStripMenuItem contextMenuItemExport;
        private ToolStripMenuItem contextMenuItemCopyToClipboard;
        private ImageList imageListEntries;
        private DataGridView listViewPartitions;
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

            InitializeComponent();
            _exportEntryController = new ExportEntryController(
                _csvExportService,
                _settings,
                this,
                statusText => toolStripStatusLabel.Text = statusText);
            _layoutMainFormController = new LayoutMainFormController(
                _settings,
                this,
                splitContainerMain,
                splitContainerLeft,
                toolStripPanelMain,
                toolStripMain,
                toolStripViewMode,
                toolStripExport,
                panelRightViewHost,
                dataGridViewEntries,
                pieChartView,
                barChartView,
                toolStripButtonTable,
                toolStripButtonPieChart,
                toolStripButtonBarChart,
                _settings.SelectedViewMode);
            _statusMainFormController = new StatusMainFormController(
                _settings,
                this,
                toolStripStatusLabel,
                statusStripMain,
                toolStripAlertInformationLabel,
                toolStripAlertWarningLabel,
                toolStripAlertErrorLabel);
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
                entry => _layoutMainFormController.BindGrid(entry),
                () => _currentRootEntry);
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
            toolStripMain.GripStyle = ToolStripGripStyle.Visible;
            toolStripViewMode.GripStyle = ToolStripGripStyle.Visible;
            toolStripExport.GripStyle = ToolStripGripStyle.Visible;
            _driveComboBoxController.LoadDrives();
            _partitionGridController.LoadPartitionList();
            _partitionGridController.AdjustColumns();
            _partitionGridController.UpdatePartitionPanelVisibility();
            _layoutMainFormController.SetViewMode(_settings.SelectedViewMode, _suspendPersistentSettingsSave);
            _layoutMainFormController.UpdateRightViewBounds();

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
            menuItemExit = new ToolStripMenuItem(LocalizationService.GetText("Menu.Exit"));
            menuItemHelp = new ToolStripMenuItem(LocalizationService.GetText("Menu.Help"));
            menuItemAbout = new ToolStripMenuItem(LocalizationService.GetText("Menu.About"));

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

            toolStripLabelDrive = new ToolStripLabel(LocalizationService.GetText("Toolbar.Drive"));
            toolStripComboBoxDrives = new ToolStripComboBox();
            toolStripButtonScan = new ToolStripButton("▶");
            toolStripButtonOpenFolder = new ToolStripButton(LocalizationService.GetText("Toolbar.Open"));

            toolStripLabelDrive.Margin = new Padding(0, 1, 0, 2);
            toolStripComboBoxDrives.DropDownStyle = ComboBoxStyle.DropDownList;
            toolStripComboBoxDrives.AutoSize = false;
            toolStripComboBoxDrives.Width = 260;
            toolStripButtonScan.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonScan.ToolTipText = LocalizationService.GetText("Toolbar.ScanStart");
            toolStripButtonScan.Click += toolStripButtonScan_Click;
            toolStripButtonOpenFolder.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonOpenFolder.ToolTipText = LocalizationService.GetText("Toolbar.SelectFolderAndScan");
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

            listViewPartitions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PartitionColumnName",
                HeaderText = LocalizationService.GetText("Common.Name"),
                Width = 120,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            listViewPartitions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "PartitionColumnSize",
                HeaderText = LocalizationService.GetText("Common.Size"),
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
                HeaderText = LocalizationService.GetText("Common.Free"),
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
                HeaderText = LocalizationService.GetText("Common.FreePercent"),
                Width = 70,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

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
            menuItemSettings.Text = LocalizationService.GetText("Menu.Settings");
            menuItemExit.Text = LocalizationService.GetText("Menu.Exit");
            menuItemHelp.Text = LocalizationService.GetText("Menu.Help");
            menuItemAbout.Text = LocalizationService.GetText("Menu.About");

            toolStripLabelDrive.Text = LocalizationService.GetText("Toolbar.Drive");
            toolStripButtonOpenFolder.Text = LocalizationService.GetText("Toolbar.Open");
            toolStripButtonScan.ToolTipText = _scanCancellationTokenSource != null
                ? LocalizationService.GetText("Toolbar.ScanCancel")
                : LocalizationService.GetText("Toolbar.ScanStart");
            toolStripButtonOpenFolder.ToolTipText = LocalizationService.GetText("Toolbar.SelectFolderAndScan");
            toolStripButtonTable.Text = LocalizationService.GetText("Toolbar.Table");
            toolStripButtonPieChart.Text = LocalizationService.GetText("Toolbar.PieChart");
            toolStripButtonBarChart.Text = LocalizationService.GetText("Toolbar.BarChart");
            toolStripButtonExportCsv.Text = LocalizationService.GetText("Toolbar.Export");
            toolStripButtonExportCsv.ToolTipText = LocalizationService.GetText("Toolbar.ExportCsv");

            contextMenuItemOpenInExplorer.Text = LocalizationService.GetText("Context.OpenInExplorer");
            contextMenuItemExport.Text = LocalizationService.GetText("Context.Export");
            contextMenuItemCopyToClipboard.Text = LocalizationService.GetText("Context.CopyToClipboard");

            _statusMainFormController?.ApplyLocalizedTexts();

            if (listViewPartitions.Columns.Contains("PartitionColumnName"))
            {
                listViewPartitions.Columns["PartitionColumnName"].HeaderText = LocalizationService.GetText("Common.Name");
            }

            if (listViewPartitions.Columns.Contains("PartitionColumnSize"))
            {
                listViewPartitions.Columns["PartitionColumnSize"].HeaderText = LocalizationService.GetText("Common.Size");
            }

            if (listViewPartitions.Columns.Contains("PartitionColumnFree"))
            {
                listViewPartitions.Columns["PartitionColumnFree"].HeaderText = LocalizationService.GetText("Common.Free");
            }

            if (listViewPartitions.Columns.Contains("PartitionColumnFreePercent"))
            {
                listViewPartitions.Columns["PartitionColumnFreePercent"].HeaderText = LocalizationService.GetText("Common.FreePercent");
            }

            dataGridViewEntries.ApplyLocalizedTexts();
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
            if (_scanCancellationTokenSource != null)
                return;

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
            if (_scanCancellationTokenSource != null)
            {
                _scanCancellationTokenSource.Cancel();
                return;
            }

            string rootPath = _driveComboBoxController.GetSelectedScanPath();

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                MessageBox.Show(this, LocalizationService.GetText("Message.NoPathSelected"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            SetScanningState(true);
            dataGridViewEntries.DataSource = null;
            _currentRootEntry = null;
            _treeEntryController.ClearPendingLiveTreeUpdate();

            long scanTargetBytes = GetUsedSpaceBytes(rootPath);
            _statusMainFormController.SetStatusProgressText(0D);

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

                _treeEntryController.QueueLiveTreeUpdate(scanProgress);

                if (scanProgress.IsCacheSavePhase)
                {
                    toolStripStatusLabel.Text = string.Format(
                        LocalizationService.GetText("Status.ScanCacheSave"),
                        scanProgress.CurrentPath,
                        SizeFormatter.Format(scanProgress.ScannedBytes),
                        scanProgress.ScannedDirectories,
                        scanProgress.ScannedFiles);
                }
                else if (scanProgress.IsCacheVerification)
                {
                    toolStripStatusLabel.Text = string.Format(
                        LocalizationService.GetText("Status.CacheVerification"),
                        scanProgress.CurrentPath,
                        SizeFormatter.Format(scanProgress.ScannedBytes),
                        scanProgress.ScannedDirectories,
                        scanProgress.ScannedFiles);
                }
                else
                {
                    toolStripStatusLabel.Text = string.Format(
                        LocalizationService.GetText("Status.FastScan"),
                        scanProgress.CurrentPath,
                        SizeFormatter.Format(scanProgress.ScannedBytes),
                        scanProgress.ScannedDirectories,
                        scanProgress.ScannedFiles);
                }

                _statusMainFormController.SetStatusProgressText(percent);
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
                        toolStripStatusLabel.Text = LocalizationService.GetText("Status.MftFastScanRunning");
                        NtfsMftScanner ntfsMftScanner = new NtfsMftScanner(_settings);
                        _currentRootEntry = await ntfsMftScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception mftException)
                    {
                        AppAlertLog.AddWarning(
                            LocalizationService.GetText("Alert.Scan"),
                            LocalizationService.Format("Alert.MftUnavailable", mftException.Message));

                        try
                        {
                            toolStripStatusLabel.Text = LocalizationService.GetText("Status.MftUnavailableNtQuery");
                            _currentRootEntry = await ntQueryDirectoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ntQueryException)
                        {
                            AppAlertLog.AddWarning(
                                LocalizationService.GetText("Alert.Scan"),
                                LocalizationService.Format("Alert.NtQueryUnavailable", ntQueryException.Message));

                            toolStripStatusLabel.Text = LocalizationService.GetText("Status.NtQueryUnavailableNormal");
                            _currentRootEntry = await directoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                        }
                    }
                }
                else
                {
                    try
                    {
                        toolStripStatusLabel.Text = LocalizationService.GetText("Status.NtQueryRunning");
                        _currentRootEntry = await ntQueryDirectoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ntQueryException)
                    {
                        AppAlertLog.AddWarning(
                            LocalizationService.GetText("Alert.Scan"),
                            LocalizationService.Format("Alert.NtQueryUnavailable", ntQueryException.Message));

                        toolStripStatusLabel.Text = LocalizationService.GetText("Status.NtQueryUnavailableNormal");
                        _currentRootEntry = await directoryScanner.ScanAsync(rootPath, progress, _scanCancellationTokenSource.Token);
                    }
                }

                _treeEntryController.FlushPendingLiveTreeUpdate();
                RenderScanResult(_currentRootEntry);
                _partitionGridController.LoadPartitionList();
                _statusMainFormController.UpdateStatusStripForDrive(rootPath);
                _statusMainFormController.SetStatusProgressText(100D);

                _statusMainFormController.ReportSkippedDirectories(skippedDirectories, skippedDirectoryDetails);
            }
            catch (OperationCanceledException)
            {
                toolStripStatusLabel.Text = LocalizationService.GetText("Status.ScanCanceled");
                _statusMainFormController.SetStatusProgressText(null);
            }
            finally
            {
                _treeEntryController.StopLiveTreeUpdateTimer();
                _treeEntryController.ClearPendingLiveTreeUpdate();
                _scanCancellationTokenSource.Dispose();
                _scanCancellationTokenSource = null;
                SetScanningState(false);
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
            toolStripButtonScan.Text = scanning ? "■" : "▶";
            toolStripButtonScan.ToolTipText = scanning ? LocalizationService.GetText("Toolbar.ScanCancel") : LocalizationService.GetText("Toolbar.ScanStart");
            _driveComboBoxController.SetEnabled(!scanning);
            toolStripButtonOpenFolder.Enabled = !scanning;
            menuItemExportCsv.Enabled = !scanning && _currentRootEntry != null;
            toolStripButtonExportCsv.Enabled = !scanning && _currentRootEntry != null;
            splitContainerMain.IsSplitterFixed = scanning;
            splitContainerLeft.IsSplitterFixed = scanning;
        }

        private void RenderScanResult(FileSystemEntry rootEntry)
        {
            _treeEntryController.RenderScanResult(rootEntry);
            _layoutMainFormController.BindGrid(rootEntry);
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

            if (!Directory.Exists(contextMenuEntry.FullPath))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = contextMenuEntry.FullPath,
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

        private void menuItemSettings_Click(object sender, EventArgs e)
        {
            using SettingsForm settingsForm = new SettingsForm(_settings);

            if (settingsForm.ShowDialog(this) != DialogResult.OK)
                return;

            _settings.Save();
            LocalizationService.Load(_settings.LanguageCode);
            ApplyLocalizedTexts();
            _driveComboBoxController.LoadDrives();
            WindowsFormStyler.Apply(this, _settings.Layout);
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_scanCancellationTokenSource != null)
            {
                _scanCancellationTokenSource.Cancel();
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