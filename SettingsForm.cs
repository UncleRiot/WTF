using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Lucid.Controls;
using Lucid.Theming;

namespace WTF
{
    public sealed class SettingsForm : Form
    {
        private readonly AppSettings _settings;

        private LucidButton buttonGeneralTab;
        private LucidButton buttonExportTab;
        private LucidButton buttonColorsTab;
        private LucidButton buttonLayoutTab;
        private LucidButton buttonStatisticsTab;
        private LucidButton buttonLoggingTab;
        private Panel panelPageHost;
        private Panel panelGeneral;
        private Panel panelExport;
        private Panel panelColors;
        private Panel panelLayout;
        private Panel panelStatistics;
        private Panel panelLogging;
        private LucidCheckBox checkBoxShowFilesInTree;
        private LucidCheckBox checkBoxSkipReparsePoints;
        private LucidCheckBox checkBoxShowPartitionPanel;
        private LucidCheckBox checkBoxStartElevatedOnStartup;
        private LucidCheckBox checkBoxShowElevationPromptOnStartup;
        private LucidCheckBox checkBoxShellContextMenuEnabled;
        private LucidLabel labelLanguage;
        private ComboBox comboBoxLanguage;
        private LucidButton buttonAddLanguage;
        private LucidButton buttonDeleteLanguage;
        private ToolTip toolTip;
        private LucidLabel labelLayout;
        private ComboBox comboBoxLayout;
        private LucidCheckBox checkBoxExportPath;
        private LucidCheckBox checkBoxExportSizeGb;
        private LucidCheckBox checkBoxExportSizeMb;
        private LucidLabel labelExportMaxDepth;
        private LucidTextBox textBoxExportMaxDepth;
        private LucidLabel labelPartitionFillLight;
        private LucidButton buttonPartitionFillLightColor;
        private Panel panelPartitionFillLightPreview;
        private LucidLabel labelPartitionFillLightBrightness;
        private TrackBar trackBarPartitionFillLightBrightness;
        private LucidLabel labelPartitionFillLightBrightnessValue;
        private LucidLabel labelPartitionFillDark;
        private LucidButton buttonPartitionFillDarkColor;
        private Panel panelPartitionFillDarkPreview;
        private LucidLabel labelPartitionFillDarkBrightness;
        private TrackBar trackBarPartitionFillDarkBrightness;
        private LucidLabel labelPartitionFillDarkBrightnessValue;
        private Color partitionFillLightColor;
        private Color partitionFillDarkColor;
        private LucidLabel labelBarChartBarHeight;
        private LucidTextBox textBoxBarChartBarHeight;
        private LucidLabel labelBarChartBarHeightDefault;
        private LucidCheckBox checkBoxSaveScanHistory;
        private LucidLabel labelScanHistoryDatabasePath;
        private LucidTextBox textBoxScanHistoryDatabasePath;
        private LucidButton buttonBrowseScanHistoryDatabasePath;
        private LucidLabel labelScanHistoryDatabaseMoveHint;
        private LucidLabel labelScanHistoryDatabaseSize;
        private LucidLabel labelScanHistoryMaximumScansPerPath;
        private LucidTextBox textBoxScanHistoryMaximumScansPerPath;
        private LucidLabel labelLogLevel;
        private ComboBox comboBoxLogLevel;
        private LucidCheckBox checkBoxAutoSaveLog;
        private LucidLabel labelMaximumLogFileSizeMb;
        private LucidTextBox textBoxMaximumLogFileSizeMb;
        private LucidLabel labelMaximumLogFileSizeUnit;
        private LucidButton buttonOk;
        private LucidButton buttonCancel;
        private DatabasePathSelectionMode selectedDatabasePathSelectionMode;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;
            _settings.ScanHistoryDatabasePath = ScanHistoryService.NormalizeDatabasePath(
                _settings.ScanHistoryDatabasePath);
            ScanHistoryService.ConfigureDatabasePath(_settings.ScanHistoryDatabasePath);

            LucidThemeService.Apply(_settings.Layout);
            WindowsFormStyler.Apply(this, _settings.Layout);
            InitializeComponent();
            LoadSettings();
            ShowPage(panelGeneral);
        }

        private void InitializeComponent()
        {
            Color backgroundPrimary = ThemeProvider.Theme.Colors.BackgroundPrimary;
            Color backgroundSecondary = ThemeProvider.Theme.Colors.BackgroundSecondary;
            Color borderColor = ThemeProvider.Theme.Colors.SurfaceHighlight;

            Text = LocalizationService.GetText("Settings.Title");
            Icon = AppResources.ApplicationIcon;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 454);
            MinimumSize = Size;
            MaximumSize = Size;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            BackColor = backgroundPrimary;
            ForeColor = ThemeProvider.Theme.Colors.TextPrimary;
            KeyDown += SettingsForm_KeyDown;

            buttonGeneralTab = new LucidButton
            {
                Name = "buttonGeneralTab",
                Text = LocalizationService.GetText("Settings.General"),
                Location = new Point(18, 16),
                Size = new Size(80, 32),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonGeneralTab.Click += buttonGeneralTab_Click;

            buttonExportTab = new LucidButton
            {
                Name = "buttonExportTab",
                Text = LocalizationService.GetText("Settings.Export"),
                Location = new Point(102, 16),
                Size = new Size(80, 32),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonExportTab.Click += buttonExportTab_Click;

            buttonColorsTab = new LucidButton
            {
                Name = "buttonColorsTab",
                Text = LocalizationService.GetText("Settings.Colors"),
                Location = new Point(210, 16),
                Size = new Size(92, 32),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonColorsTab.Click += buttonColorsTab_Click;

            buttonLayoutTab = new LucidButton
            {
                Name = "buttonLayoutTab",
                Text = LocalizationService.GetText("Settings.LayoutTab"),
                Location = new Point(186, 16),
                Size = new Size(80, 32),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonLayoutTab.Click += buttonLayoutTab_Click;

            buttonStatisticsTab = new LucidButton
            {
                Name = "buttonStatisticsTab",
                Text = LocalizationService.GetText("Settings.Statistics"),
                Location = new Point(270, 16),
                Size = new Size(100, 32),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonStatisticsTab.Click += buttonStatisticsTab_Click;

            buttonLoggingTab = new LucidButton
            {
                Name = "buttonLoggingTab",
                Text = LocalizationService.GetText("Settings.Logging"),
                Location = new Point(374, 16),
                Size = new Size(100, 32),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonLoggingTab.Click += buttonLoggingTab_Click;

            panelPageHost = new Panel
            {
                Name = "panelPageHost",
                Location = new Point(18, 54),
                Size = new Size(484, 338),
                BackColor = backgroundSecondary,
                BorderStyle = BorderStyle.FixedSingle
            };

            panelGeneral = new Panel
            {
                Name = "panelGeneral",
                Dock = DockStyle.Fill,
                BackColor = backgroundSecondary
            };

            panelExport = new Panel
            {
                Name = "panelExport",
                Dock = DockStyle.Fill,
                BackColor = backgroundSecondary,
                Visible = false
            };

            panelColors = new Panel
            {
                Name = "panelColors",
                Dock = DockStyle.Fill,
                BackColor = backgroundSecondary,
                Visible = false
            };

            panelLayout = new Panel
            {
                Name = "panelLayout",
                Dock = DockStyle.Fill,
                BackColor = backgroundSecondary,
                Visible = false
            };

            panelStatistics = new Panel
            {
                Name = "panelStatistics",
                Dock = DockStyle.Fill,
                BackColor = backgroundSecondary,
                Visible = false
            };

            panelLogging = new Panel
            {
                Name = "panelLogging",
                Dock = DockStyle.Fill,
                BackColor = backgroundSecondary,
                Visible = false
            };

            checkBoxShowFilesInTree = CreateCheckBox(
                "checkBoxShowFilesInTree",
                LocalizationService.GetText("Settings.ShowFilesInTree"),
                24,
                backgroundSecondary);

            checkBoxSkipReparsePoints = CreateCheckBox(
                "checkBoxSkipReparsePoints",
                LocalizationService.GetText("Settings.SkipReparsePoints"),
                60,
                backgroundSecondary);

            checkBoxShowPartitionPanel = CreateCheckBox(
                "checkBoxShowPartitionPanel",
                LocalizationService.GetText("Settings.ShowPartitionPanel"),
                96,
                backgroundSecondary);

            checkBoxStartElevatedOnStartup = CreateCheckBox(
                "checkBoxStartElevatedOnStartup",
                LocalizationService.GetText("Settings.StartElevated"),
                132,
                backgroundSecondary);

            checkBoxShowElevationPromptOnStartup = CreateCheckBox(
                "checkBoxShowElevationPromptOnStartup",
                LocalizationService.GetText("Settings.ShowElevationPrompt"),
                168,
                backgroundSecondary);

            checkBoxShellContextMenuEnabled = CreateCheckBox(
                "checkBoxShellContextMenuEnabled",
                LocalizationService.GetText("Settings.ShellContextMenu"),
                204,
                backgroundSecondary);

            // Language/Layout label start
            labelLanguage = CreateLabel(
                "labelLanguage",
                LocalizationService.GetText("Settings.Language"),
                250);
            labelLanguage.Location = new Point(22, 250);
            // Language/Layout label width
            labelLanguage.Size = new Size(80, 28);

            // Language/Layout dropdown start
            comboBoxLanguage = new ComboBox
            {
                Name = "comboBoxLanguage",
                // Dropdown text moves with control
                Location = new Point(100, 250),
                Size = new Size(216, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Standard,
                BackColor = backgroundSecondary,
                ForeColor = ThemeProvider.Theme.Colors.TextPrimary,
                Font = SystemFonts.MessageBoxFont
            };
            comboBoxLanguage.SelectedIndexChanged += comboBoxLanguage_SelectedIndexChanged;

            // Language buttons start
            buttonAddLanguage = new LucidButton
            {
                Name = "buttonAddLanguage",
                Text = "+",
                Location = new Point(326, 250),
                Size = new Size(28, 28),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonAddLanguage.Click += buttonAddLanguage_Click;

            buttonDeleteLanguage = new LucidButton
            {
                Name = "buttonDeleteLanguage",
                Text = "−",
                Location = new Point(358, 250),
                Size = new Size(28, 28),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonDeleteLanguage.Click += buttonDeleteLanguage_Click;

            toolTip = new ToolTip();
            toolTip.SetToolTip(
                buttonAddLanguage,
                LocalizationService.GetText("Settings.AddLanguage"));
            toolTip.SetToolTip(
                buttonDeleteLanguage,
                LocalizationService.GetText("Settings.DeleteLanguage"));

            ReloadLanguageItems(_settings.LanguageCode);

            labelLayout = CreateLabel(
                "labelLayout",
                LocalizationService.GetText("Settings.Layout"),
                290);
            labelLayout.Location = new Point(22, 290);
            labelLayout.Size = new Size(68, 28);

            comboBoxLayout = new ComboBox
            {
                Name = "comboBoxLayout",
                // Dropdown text moves with control
                Location = new Point(100, 290),
                Size = new Size(216, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Standard,
                BackColor = backgroundSecondary,
                ForeColor = ThemeProvider.Theme.Colors.TextPrimary,
                Font = SystemFonts.MessageBoxFont
            };

            comboBoxLayout.Items.Add(new LayoutItem(
                LocalizationService.GetText("Settings.LayoutWindowsDefault"),
                AppLayout.WindowsDefault));
            comboBoxLayout.Items.Add(new LayoutItem(
                LocalizationService.GetText("Settings.LayoutWindowsLight"),
                AppLayout.WindowsLightMode));
            comboBoxLayout.Items.Add(new LayoutItem(
                LocalizationService.GetText("Settings.LayoutWindowsDark"),
                AppLayout.WindowsDarkMode));
            comboBoxLayout.SelectedIndexChanged += comboBoxLayout_SelectedIndexChanged;

            checkBoxExportPath = CreateCheckBox(
                "checkBoxExportPath",
                LocalizationService.GetText("Settings.ExportPath"),
                24,
                backgroundSecondary);

            checkBoxExportSizeGb = CreateCheckBox(
                "checkBoxExportSizeGb",
                LocalizationService.GetText("Settings.ExportSizeGb"),
                60,
                backgroundSecondary);

            checkBoxExportSizeMb = CreateCheckBox(
                "checkBoxExportSizeMb",
                LocalizationService.GetText("Settings.ExportSizeMb"),
                96,
                backgroundSecondary);

            labelExportMaxDepth = new LucidLabel
            {
                Name = "labelExportMaxDepth",
                Text = LocalizationService.GetText("Settings.ExportMaxDepth"),
                Location = new Point(24, 146),
                Size = new Size(200, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };

            textBoxExportMaxDepth = new LucidTextBox
            {
                Name = "textBoxExportMaxDepth",
                Location = new Point(230, 148),
                Size = new Size(100, 25),
                TextAlign = HorizontalAlignment.Right
            };

            labelPartitionFillLight = CreateLabel(
                "labelPartitionFillLight",
                LocalizationService.GetText("Settings.PartitionFillLight"),
                24);

            labelPartitionFillLight.Size = new Size(220, 28);

            buttonPartitionFillLightColor = new LucidButton
            {
                Name = "buttonPartitionFillLightColor",
                Text = LocalizationService.GetText("Settings.SelectColor"),
                Location = new Point(250, 24),
                Size = new Size(140, 28),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonPartitionFillLightColor.Click += buttonPartitionFillLightColor_Click;

            panelPartitionFillLightPreview = new Panel
            {
                Name = "panelPartitionFillLightPreview",
                Location = new Point(400, 24),
                Size = new Size(42, 28),
                BorderStyle = BorderStyle.FixedSingle
            };

            labelPartitionFillLightBrightness = CreateLabel(
                "labelPartitionFillLightBrightness",
                LocalizationService.GetText("Settings.Brightness"),
                64);

            trackBarPartitionFillLightBrightness = CreateBrightnessTrackBar(
                "trackBarPartitionFillLightBrightness",
                250,
                64);
            trackBarPartitionFillLightBrightness.ValueChanged +=
                trackBarPartitionFillLightBrightness_ValueChanged;

            labelPartitionFillLightBrightnessValue = CreateBrightnessValueLabel(
                "labelPartitionFillLightBrightnessValue",
                404,
                64);

            labelPartitionFillDark = CreateLabel(
                "labelPartitionFillDark",
                LocalizationService.GetText("Settings.PartitionFillDark"),
                24);

            labelPartitionFillDark.Size = new Size(220, 28);

            buttonPartitionFillDarkColor = new LucidButton
            {
                Name = "buttonPartitionFillDarkColor",
                Text = LocalizationService.GetText("Settings.SelectColor"),
                Location = new Point(250, 24),
                Size = new Size(140, 28),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonPartitionFillDarkColor.Click += buttonPartitionFillDarkColor_Click;

            panelPartitionFillDarkPreview = new Panel
            {
                Name = "panelPartitionFillDarkPreview",
                Location = new Point(400, 24),
                Size = new Size(42, 28),
                BorderStyle = BorderStyle.FixedSingle
            };

            labelPartitionFillDarkBrightness = CreateLabel(
                "labelPartitionFillDarkBrightness",
                LocalizationService.GetText("Settings.Brightness"),
                64);

            trackBarPartitionFillDarkBrightness = CreateBrightnessTrackBar(
                "trackBarPartitionFillDarkBrightness",
                250,
                64);
            trackBarPartitionFillDarkBrightness.ValueChanged +=
                trackBarPartitionFillDarkBrightness_ValueChanged;

            labelPartitionFillDarkBrightnessValue = CreateBrightnessValueLabel(
                "labelPartitionFillDarkBrightnessValue",
                404,
                64);

            labelBarChartBarHeight = CreateLabel(
                "labelBarChartBarHeight",
                LocalizationService.GetText("Settings.BarChartBarHeight"),
                120);
            labelBarChartBarHeight.Size = new Size(220, 28);

            textBoxBarChartBarHeight = new LucidTextBox
            {
                Name = "textBoxBarChartBarHeight",
                Location = new Point(253, 120),
                Size = new Size(45, 25),
                TextAlign = HorizontalAlignment.Right,
                MaxLength = 3
            };

            labelBarChartBarHeightDefault = CreateLabel(
                "labelBarChartBarHeightDefault",
                string.Format(
                    LocalizationService.GetText("Settings.BarChartBarHeightDefault"),
                    14),
                24);
            labelBarChartBarHeightDefault.Location = new Point(308, 120);
            labelBarChartBarHeightDefault.Size = new Size(160, 28);

            checkBoxSaveScanHistory = CreateCheckBox(
                "checkBoxSaveScanHistory",
                LocalizationService.GetText("Settings.SaveScanHistory"),
                24,
                backgroundSecondary);
            checkBoxSaveScanHistory.CheckedChanged += checkBoxSaveScanHistory_CheckedChanged;
            checkBoxSaveScanHistory.Location = new Point(24, 24);

            labelScanHistoryDatabasePath = new LucidLabel
            {
                Name = "labelScanHistoryDatabasePath",
                Text = LocalizationService.GetText("Settings.ScanHistoryDatabasePath"),
                Location = new Point(24, 60),
                Size = new Size(420, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };

            textBoxScanHistoryDatabasePath = new LucidTextBox
            {
                Name = "textBoxScanHistoryDatabasePath",
                Location = new Point(24, 88),
                Size = new Size(330, 25),
                Text = _settings.ScanHistoryDatabasePath,
                ReadOnly = true,
                Visible = false
            };

            buttonBrowseScanHistoryDatabasePath = new LucidButton
            {
                Name = "buttonBrowseScanHistoryDatabasePath",
                Text = LocalizationService.GetText("Settings.MoveDatabase"),
                Location = new Point(364, 86),
                Size = new Size(90, 28),
                ButtonStyle = LucidButtonStyle.Normal,
                Visible = false
            };
            buttonBrowseScanHistoryDatabasePath.Click += buttonBrowseScanHistoryDatabasePath_Click;

            labelScanHistoryDatabaseMoveHint = new LucidLabel
            {
                Name = "labelScanHistoryDatabaseMoveHint",
                Text = LocalizationService.GetText("Settings.ScanHistoryDatabaseMoveHint"),
                Location = new Point(24, 122),
                Size = new Size(430, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };

            labelScanHistoryDatabaseSize = new LucidLabel
            {
                Name = "labelScanHistoryDatabaseSize",
                Location = new Point(24, 150),
                Size = new Size(430, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };

            labelScanHistoryMaximumScansPerPath = new LucidLabel
            {
                Name = "labelScanHistoryMaximumScansPerPath",
                Text = LocalizationService.GetText("Settings.ScanHistoryMaximumScansPerPath"),
                Location = new Point(24, 182),
                // Labelsize
                Size = new Size(184, 25),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };

            textBoxScanHistoryMaximumScansPerPath = new LucidTextBox
            {
                Name = "textBoxScanHistoryMaximumScansPerPath",
                Location = new Point(208, 182),
                // Textbox
                Size = new Size(30, 25),
                TextAlign = HorizontalAlignment.Right,
                MaxLength = 5,
                Visible = false
            };

            labelLogLevel = new LucidLabel
            {
                Name = "labelLogLevel",
                Text = LocalizationService.GetText("Settings.LogLevel"),
                Location = new Point(21, 24),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Pos: Log Level
            comboBoxLogLevel = new ComboBox
            {
                Name = "comboBoxLogLevel",
                Location = new Point(100, 22),
                Size = new Size(150, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Standard,
                BackColor = backgroundSecondary,
                ForeColor = ThemeProvider.Theme.Colors.TextPrimary,
                Font = SystemFonts.MessageBoxFont
            };
            comboBoxLogLevel.Items.Add(AppLogLevel.Normal);
            comboBoxLogLevel.Items.Add(AppLogLevel.Verbose);

            checkBoxAutoSaveLog = CreateCheckBox(
                "checkBoxAutoSaveLog",
                LocalizationService.GetText("Settings.AutoSaveLog"),
                60,
                backgroundSecondary);
            checkBoxAutoSaveLog.CheckedChanged += checkBoxAutoSaveLog_CheckedChanged;

            labelMaximumLogFileSizeMb = new LucidLabel
            {
                Name = "labelMaximumLogFileSizeMb",
                Text = LocalizationService.GetText("Settings.MaximumLogFileSizeMb"),
                Location = new Point(21, 96),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Pos: MaxLogSize
            textBoxMaximumLogFileSizeMb = new LucidTextBox
            {
                Name = "textBoxMaximumLogFileSizeMb",
                Location = new Point(126, 94),
                Size = new Size(40, 25),
                TextAlign = HorizontalAlignment.Right,
                MaxLength = 5
            };

            // Pos: MaxLogSize MB
            labelMaximumLogFileSizeUnit = new LucidLabel
            {
                Name = "labelMaximumLogFileSizeUnit",
                Text = "(MB)",
                Location = new Point(170, 96),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            panelGeneral.Controls.Add(checkBoxShowFilesInTree);
            panelGeneral.Controls.Add(checkBoxSkipReparsePoints);
            panelGeneral.Controls.Add(checkBoxShowPartitionPanel);
            panelGeneral.Controls.Add(checkBoxStartElevatedOnStartup);
            panelGeneral.Controls.Add(checkBoxShowElevationPromptOnStartup);
            panelGeneral.Controls.Add(checkBoxShellContextMenuEnabled);
            panelGeneral.Controls.Add(labelLanguage);
            panelGeneral.Controls.Add(comboBoxLanguage);
            panelGeneral.Controls.Add(buttonAddLanguage);
            panelGeneral.Controls.Add(buttonDeleteLanguage);
            panelGeneral.Controls.Add(labelLayout);
            panelGeneral.Controls.Add(comboBoxLayout);

            panelExport.Controls.Add(checkBoxExportPath);
            panelExport.Controls.Add(checkBoxExportSizeGb);
            panelExport.Controls.Add(checkBoxExportSizeMb);
            panelExport.Controls.Add(labelExportMaxDepth);
            panelExport.Controls.Add(textBoxExportMaxDepth);

            panelLayout.Controls.Add(labelPartitionFillLight);
            panelLayout.Controls.Add(buttonPartitionFillLightColor);
            panelLayout.Controls.Add(panelPartitionFillLightPreview);
            panelLayout.Controls.Add(labelPartitionFillLightBrightness);
            panelLayout.Controls.Add(trackBarPartitionFillLightBrightness);
            panelLayout.Controls.Add(labelPartitionFillLightBrightnessValue);
            panelLayout.Controls.Add(labelPartitionFillDark);
            panelLayout.Controls.Add(buttonPartitionFillDarkColor);
            panelLayout.Controls.Add(panelPartitionFillDarkPreview);
            panelLayout.Controls.Add(labelPartitionFillDarkBrightness);
            panelLayout.Controls.Add(trackBarPartitionFillDarkBrightness);
            panelLayout.Controls.Add(labelPartitionFillDarkBrightnessValue);

            panelLayout.Controls.Add(labelBarChartBarHeight);
            panelLayout.Controls.Add(textBoxBarChartBarHeight);
            panelLayout.Controls.Add(labelBarChartBarHeightDefault);

            panelStatistics.Controls.Add(checkBoxSaveScanHistory);
            panelStatistics.Controls.Add(labelScanHistoryDatabasePath);
            panelStatistics.Controls.Add(textBoxScanHistoryDatabasePath);
            panelStatistics.Controls.Add(buttonBrowseScanHistoryDatabasePath);
            panelStatistics.Controls.Add(labelScanHistoryDatabaseMoveHint);
            panelStatistics.Controls.Add(labelScanHistoryDatabaseSize);
            panelStatistics.Controls.Add(labelScanHistoryMaximumScansPerPath);
            panelStatistics.Controls.Add(textBoxScanHistoryMaximumScansPerPath);

            panelLogging.Controls.Add(labelLogLevel);
            panelLogging.Controls.Add(comboBoxLogLevel);
            panelLogging.Controls.Add(checkBoxAutoSaveLog);
            panelLogging.Controls.Add(labelMaximumLogFileSizeMb);
            panelLogging.Controls.Add(textBoxMaximumLogFileSizeMb);
            panelLogging.Controls.Add(labelMaximumLogFileSizeUnit);

            panelPageHost.Controls.Add(panelGeneral);
            panelPageHost.Controls.Add(panelExport);
            panelPageHost.Controls.Add(panelLayout);
            panelPageHost.Controls.Add(panelStatistics);
            panelPageHost.Controls.Add(panelLogging);

            buttonOk = new LucidButton
            {
                Name = "buttonOk",
                Text = LocalizationService.GetText("Common.OK"),
                Location = new Point(312, 406),
                Size = new Size(90, 32),
                DialogResult = DialogResult.OK,
                ButtonStyle = LucidButtonStyle.Normal
            };

            buttonCancel = new LucidButton
            {
                Name = "buttonCancel",
                Text = LocalizationService.GetText("Common.Cancel"),
                Location = new Point(412, 406),
                Size = new Size(90, 32),
                DialogResult = DialogResult.Cancel,
                ButtonStyle = LucidButtonStyle.Normal
            };

            buttonOk.Click += buttonOk_Click;

            Controls.Add(buttonGeneralTab);
            Controls.Add(buttonExportTab);
            Controls.Add(buttonLayoutTab);
            Controls.Add(buttonStatisticsTab);
            Controls.Add(buttonLoggingTab);
            Controls.Add(panelPageHost);
            Controls.Add(buttonOk);
            Controls.Add(buttonCancel);

            AcceptButton = buttonOk;
            CancelButton = buttonCancel;
        }

        private static LucidCheckBox CreateCheckBox(
            string name,
            string text,
            int top,
            Color backColor)
        {
            return new LucidCheckBox
            {
                Name = name,
                Text = text,
                Location = new Point(24, top),
                Size = new Size(420, 24),
                UseBackColorProperty = true,
                BackColor = backColor
            };
        }

        private static TrackBar CreateBrightnessTrackBar(
            string name,
            int left,
            int top)
        {
            return new TrackBar
            {
                Name = name,
                Location = new Point(left, top),
                Size = new Size(150, 45),
                Minimum = 0,
                Maximum = 200,
                TickFrequency = 25,
                SmallChange = 1,
                LargeChange = 10
            };
        }

        private static LucidLabel CreateBrightnessValueLabel(
            string name,
            int left,
            int top)
        {
            return new LucidLabel
            {
                Name = name,
                Location = new Point(left, top),
                Size = new Size(54, 28),
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        private static LucidLabel CreateLabel(string name, string text, int top)
        {
            return new LucidLabel
            {
                Name = name,
                Text = text,
                Location = new Point(24, top),
                Size = new Size(138, 28),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private void buttonGeneralTab_Click(object sender, EventArgs e)
        {
            ShowPage(panelGeneral);
        }

        private void buttonExportTab_Click(object sender, EventArgs e)
        {
            ShowPage(panelExport);
        }

        private void buttonColorsTab_Click(object sender, EventArgs e)
        {
            UpdatePartitionFillControlsVisibility();
            ShowPage(panelColors);
        }

        private void buttonLayoutTab_Click(object sender, EventArgs e)
        {
            ShowPage(panelLayout);
        }

        private void buttonStatisticsTab_Click(object sender, EventArgs e)
        {
            ShowPage(panelStatistics);
        }

        private void buttonLoggingTab_Click(object sender, EventArgs e)
        {
            ShowPage(panelLogging);
        }

        private void checkBoxAutoSaveLog_CheckedChanged(object sender, EventArgs e)
        {
            UpdateLoggingControls();
        }

        private void UpdateLoggingControls()
        {
            bool autoSaveLog = checkBoxAutoSaveLog.Checked;
            labelMaximumLogFileSizeMb.Enabled = autoSaveLog;
            textBoxMaximumLogFileSizeMb.Enabled = autoSaveLog;
            labelMaximumLogFileSizeUnit.Enabled = autoSaveLog;
        }

        private void checkBoxSaveScanHistory_CheckedChanged(object sender, EventArgs e)
        {
            UpdateScanHistoryDatabasePathVisibility();
        }

        private void UpdateScanHistoryDatabasePathVisibility()
        {
            bool showDatabasePath = checkBoxSaveScanHistory.Checked;
            labelScanHistoryDatabasePath.Visible = showDatabasePath;
            textBoxScanHistoryDatabasePath.Visible = showDatabasePath;
            buttonBrowseScanHistoryDatabasePath.Visible = showDatabasePath;
            labelScanHistoryDatabaseMoveHint.Visible = showDatabasePath;
            labelScanHistoryDatabaseSize.Visible = showDatabasePath;
            labelScanHistoryMaximumScansPerPath.Visible = showDatabasePath;
            textBoxScanHistoryMaximumScansPerPath.Visible = showDatabasePath;

            if (showDatabasePath && string.IsNullOrWhiteSpace(textBoxScanHistoryDatabasePath.Text))
            {
                textBoxScanHistoryDatabasePath.Text = ScanHistoryService.DatabasePath;
            }

            UpdateScanHistoryDatabaseSize();
        }

        private void UpdateScanHistoryDatabaseSize()
        {
            string selectedDatabasePath = string.IsNullOrWhiteSpace(
                    textBoxScanHistoryDatabasePath.Text)
                ? ScanHistoryService.DatabasePath
                : textBoxScanHistoryDatabasePath.Text;
            string databasePath = ScanHistoryService.NormalizeDatabasePath(
                selectedDatabasePath);
            string databaseSize = LocalizationService.GetText("Settings.DatabaseSizeUnavailable");

            try
            {
                if (System.IO.File.Exists(databasePath))
                {
                    databaseSize = SizeFormatter.Format(
                        new System.IO.FileInfo(databasePath).Length);
                }
            }
            catch
            {
            }

            labelScanHistoryDatabaseSize.Text = string.Format(
                LocalizationService.GetText("Settings.DatabaseSize"),
                databaseSize);
        }

        private void buttonBrowseScanHistoryDatabasePath_Click(object sender, EventArgs e)
        {
            string currentDatabasePath = ScanHistoryService.NormalizeDatabasePath(
                ScanHistoryService.DatabasePath);

            using DatabaseMoveForm databaseMoveForm = new DatabaseMoveForm(
                _settings.Layout,
                currentDatabasePath);

            if (databaseMoveForm.ShowDialog(this) == DialogResult.OK)
            {
                textBoxScanHistoryDatabasePath.Text = ScanHistoryService.NormalizeDatabasePath(
                    databaseMoveForm.SelectedDatabasePath);
                selectedDatabasePathSelectionMode = databaseMoveForm.SelectionMode;
                UpdateScanHistoryDatabaseSize();
            }
        }

        private static string GetExistingDirectoryPath(string filePath)
        {
            try
            {
                string directoryPath = System.IO.Path.GetDirectoryName(filePath);

                if (!string.IsNullOrWhiteSpace(directoryPath) &&
                    System.IO.Directory.Exists(directoryPath))
                {
                    return directoryPath;
                }
            }
            catch
            {
            }

            return AppContext.BaseDirectory;
        }

        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonDeleteLanguage.Enabled =
                comboBoxLanguage.SelectedItem is LanguageItem selectedLanguageItem &&
                !LocalizationService.IsBuiltInLanguage(selectedLanguageItem.LanguageCode);
        }

        private void buttonAddLanguage_Click(object sender, EventArgs e)
        {
            DialogResult warningResult = MessageBox.Show(
                this,
                LocalizationService.GetText("Settings.AddLanguageWarning"),
                LocalizationService.GetText("Common.Warning"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (warningResult != DialogResult.Yes)
                return;

            using OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = LocalizationService.GetText("Settings.AddLanguage"),
                Filter = LocalizationService.GetText("Settings.LanguageFileFilter"),
                CheckFileExists = true,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog(this) != DialogResult.OK)
                return;

            string fileName = Path.GetFileName(openFileDialog.FileName);
            string languageCode = GetLanguageCodeFromFileName(fileName);

            if (languageCode == null || !IsValidLanguageFile(openFileDialog.FileName))
            {
                MessageBox.Show(
                    this,
                    LocalizationService.GetText("Settings.InvalidLanguageFile"),
                    LocalizationService.GetText("Common.Warning"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(LocalizationService.GetSettingsDirectoryPath());

                string sourceFilePath = Path.GetFullPath(openFileDialog.FileName);
                string targetFilePath = Path.GetFullPath(
                    LocalizationService.GetLanguageFilePath(languageCode));

                if (!string.Equals(
                        sourceFilePath,
                        targetFilePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(sourceFilePath, targetFilePath, true);
                }

                ReloadLanguageItems(languageCode);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    LocalizationService.GetText("Settings.LanguageImportFailed") +
                    Environment.NewLine +
                    Environment.NewLine +
                    exception.Message,
                    LocalizationService.GetText("Common.Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void buttonDeleteLanguage_Click(object sender, EventArgs e)
        {
            if (!(comboBoxLanguage.SelectedItem is LanguageItem selectedLanguageItem))
                return;

            if (LocalizationService.IsBuiltInLanguage(selectedLanguageItem.LanguageCode))
                return;

            DialogResult warningResult = MessageBox.Show(
                this,
                LocalizationService.Format(
                    "Settings.DeleteLanguageConfirm",
                    selectedLanguageItem.Text),
                LocalizationService.GetText("Common.Warning"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (warningResult != DialogResult.Yes)
                return;

            try
            {
                string languageFilePath = LocalizationService.GetLanguageFilePath(
                    selectedLanguageItem.LanguageCode);

                if (File.Exists(languageFilePath))
                {
                    File.Delete(languageFilePath);
                }

                ReloadLanguageItems(LocalizationService.EnglishLanguageCode);
            }
            catch
            {
                MessageBox.Show(
                    this,
                    LocalizationService.GetText("Settings.LanguageDeleteFailed"),
                    LocalizationService.GetText("Common.Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ReloadLanguageItems(string selectedLanguageCode)
        {
            string normalizedSelectedLanguageCode =
                LocalizationService.NormalizeLanguageCode(selectedLanguageCode);

            comboBoxLanguage.BeginUpdate();

            try
            {
                comboBoxLanguage.Items.Clear();

                foreach (string languageCode in LocalizationService.GetAvailableLanguageCodes())
                {
                    comboBoxLanguage.Items.Add(new LanguageItem(
                        LocalizationService.GetLanguageDisplayName(languageCode),
                        languageCode));
                }
            }
            finally
            {
                comboBoxLanguage.EndUpdate();
            }

            for (int index = 0; index < comboBoxLanguage.Items.Count; index++)
            {
                if (comboBoxLanguage.Items[index] is LanguageItem languageItem &&
                    string.Equals(
                        languageItem.LanguageCode,
                        normalizedSelectedLanguageCode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    comboBoxLanguage.SelectedIndex = index;
                    return;
                }
            }

            comboBoxLanguage.SelectedIndex = comboBoxLanguage.Items.Count > 0 ? 0 : -1;
        }

        private static string GetLanguageCodeFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                !fileName.StartsWith("lang_", StringComparison.OrdinalIgnoreCase) ||
                !fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string languageCode = fileName.Substring(5, fileName.Length - 10);

            if (string.IsNullOrWhiteSpace(languageCode))
                return null;

            string normalizedLanguageCode = LocalizationService.NormalizeLanguageCode(languageCode);

            return string.Equals(
                normalizedLanguageCode,
                languageCode,
                StringComparison.OrdinalIgnoreCase)
                ? normalizedLanguageCode
                : null;
        }

        private static bool IsValidLanguageFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                Dictionary<string, string> texts =
                    JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                return texts != null && texts.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void comboBoxLayout_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdatePartitionFillControlsVisibility();
        }

        private void buttonPartitionFillLightColor_Click(object sender, EventArgs e)
        {
            using ColorDialog colorDialog = new ColorDialog
            {
                Color = partitionFillLightColor,
                FullOpen = true
            };

            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                partitionFillLightColor = colorDialog.Color;
                UpdateColorPreviews();
            }
        }

        private void buttonPartitionFillDarkColor_Click(object sender, EventArgs e)
        {
            using ColorDialog colorDialog = new ColorDialog
            {
                Color = partitionFillDarkColor,
                FullOpen = true
            };

            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                partitionFillDarkColor = colorDialog.Color;
                UpdateColorPreviews();
            }
        }

        private void trackBarPartitionFillLightBrightness_ValueChanged(object sender, EventArgs e)
        {
            UpdateColorPreviews();
        }

        private void trackBarPartitionFillDarkBrightness_ValueChanged(object sender, EventArgs e)
        {
            UpdateColorPreviews();
        }

        private void ShowPage(Panel page)
        {
            panelGeneral.Visible = page == panelGeneral;
            panelExport.Visible = page == panelExport;
            panelColors.Visible = page == panelColors;
            panelLayout.Visible = page == panelLayout;
            panelStatistics.Visible = page == panelStatistics;
            panelLogging.Visible = page == panelLogging;
            buttonGeneralTab.Enabled = page != panelGeneral;
            buttonExportTab.Enabled = page != panelExport;
            buttonColorsTab.Enabled = page != panelColors;
            buttonLayoutTab.Enabled = page != panelLayout;
            buttonStatisticsTab.Enabled = page != panelStatistics;
            buttonLoggingTab.Enabled = page != panelLogging;
            page.BringToFront();
        }

        private void SettingsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.Alt && e.KeyCode == Keys.D)
            {
                e.SuppressKeyPress = true;

                using (DebugClassForm debugClassForm = new DebugClassForm(_settings.Layout))
                {
                    debugClassForm.ShowDialog(this);
                }
            }
        }

        private void LoadSettings()
        {
            checkBoxShowFilesInTree.Checked = _settings.ShowFilesInTree;
            checkBoxSkipReparsePoints.Checked = _settings.SkipReparsePoints;
            checkBoxShowPartitionPanel.Checked = _settings.ShowPartitionPanel;
            checkBoxStartElevatedOnStartup.Checked = _settings.StartElevatedOnStartup;
            checkBoxShowElevationPromptOnStartup.Checked = _settings.ShowElevationPromptOnStartup;
            checkBoxShellContextMenuEnabled.Checked = _settings.ShellContextMenuEnabled;
            checkBoxExportPath.Checked = _settings.ExportPath;
            checkBoxExportSizeGb.Checked = _settings.ExportSizeGb;
            checkBoxExportSizeMb.Checked = _settings.ExportSizeMb;
            textBoxExportMaxDepth.Text = _settings.ExportMaxDepth.HasValue
                ? _settings.ExportMaxDepth.Value.ToString()
                : string.Empty;
            textBoxBarChartBarHeight.Text = _settings.BarChartBarHeight.ToString();
            textBoxScanHistoryDatabasePath.Text = ScanHistoryService.NormalizeDatabasePath(
                _settings.ScanHistoryDatabasePath);
            textBoxScanHistoryMaximumScansPerPath.Text =
                _settings.ScanHistoryMaximumScansPerPath.ToString();
            checkBoxSaveScanHistory.Checked = _settings.SaveScanHistory;
            comboBoxLogLevel.SelectedItem = _settings.LogLevel;
            if (comboBoxLogLevel.SelectedIndex < 0)
            {
                comboBoxLogLevel.SelectedItem = AppLogLevel.Normal;
            }
            checkBoxAutoSaveLog.Checked = _settings.AutoSaveLog;
            textBoxMaximumLogFileSizeMb.Text =
                _settings.MaximumLogFileSizeMb.ToString();
            UpdateScanHistoryDatabasePathVisibility();
            UpdateLoggingControls();

            partitionFillLightColor = Color.FromArgb(_settings.PartitionFillColorLightArgb);
            partitionFillDarkColor = Color.FromArgb(_settings.PartitionFillColorDarkArgb);
            trackBarPartitionFillLightBrightness.Value = Math.Max(
                trackBarPartitionFillLightBrightness.Minimum,
                Math.Min(
                    trackBarPartitionFillLightBrightness.Maximum,
                    _settings.PartitionFillBrightnessLightPercent));
            trackBarPartitionFillDarkBrightness.Value = Math.Max(
                trackBarPartitionFillDarkBrightness.Minimum,
                Math.Min(
                    trackBarPartitionFillDarkBrightness.Maximum,
                    _settings.PartitionFillBrightnessDarkPercent));
            UpdateColorPreviews();

            for (int index = 0; index < comboBoxLanguage.Items.Count; index++)
            {
                if (comboBoxLanguage.Items[index] is LanguageItem languageItem &&
                    string.Equals(
                        languageItem.LanguageCode,
                        LocalizationService.NormalizeLanguageCode(_settings.LanguageCode),
                        StringComparison.OrdinalIgnoreCase))
                {
                    comboBoxLanguage.SelectedIndex = index;
                    break;
                }
            }

            if (comboBoxLanguage.SelectedIndex < 0)
            {
                comboBoxLanguage.SelectedIndex = 0;
            }

            for (int index = 0; index < comboBoxLayout.Items.Count; index++)
            {
                if (comboBoxLayout.Items[index] is LayoutItem layoutItem &&
                    layoutItem.Layout == _settings.Layout)
                {
                    comboBoxLayout.SelectedIndex = index;
                    return;
                }
            }

            comboBoxLayout.SelectedIndex = 0;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            if (!TrySaveSettings())
            {
                DialogResult = DialogResult.None;
            }
        }

        private bool TrySaveSettings()
        {
            int? exportMaxDepth = null;

            if (!int.TryParse(
                    textBoxMaximumLogFileSizeMb.Text.Trim(),
                    out int maximumLogFileSizeMb) ||
                maximumLogFileSizeMb < 1)
            {
                MessageBox.Show(
                    this,
                    LocalizationService.GetText("Settings.MaximumLogFileSizeMbInvalid"),
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ShowPage(panelLogging);
                textBoxMaximumLogFileSizeMb.Focus();
                textBoxMaximumLogFileSizeMb.SelectAll();
                return false;
            }

            if (!int.TryParse(
                    textBoxScanHistoryMaximumScansPerPath.Text.Trim(),
                    out int scanHistoryMaximumScansPerPath) ||
                scanHistoryMaximumScansPerPath < 1)
            {
                MessageBox.Show(
                    this,
                    LocalizationService.GetText("Settings.ScanHistoryMaximumScansPerPathInvalid"),
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ShowPage(panelStatistics);
                textBoxScanHistoryMaximumScansPerPath.Focus();
                textBoxScanHistoryMaximumScansPerPath.SelectAll();
                return false;
            }

            if (!int.TryParse(
                    textBoxBarChartBarHeight.Text.Trim(),
                    out int barChartBarHeight) ||
                barChartBarHeight < 5 ||
                barChartBarHeight > 30)
            {
                MessageBox.Show(
                    this,
                    LocalizationService.GetText("Settings.BarChartBarHeightInvalid"),
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                ShowPage(panelLayout);
                textBoxBarChartBarHeight.Focus();
                textBoxBarChartBarHeight.SelectAll();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(textBoxExportMaxDepth.Text))
            {
                if (!int.TryParse(
                        textBoxExportMaxDepth.Text.Trim(),
                        out int parsedExportMaxDepth) ||
                    parsedExportMaxDepth < 0)
                {
                    MessageBox.Show(
                        this,
                        LocalizationService.GetText("Settings.ExportMaxDepthInvalid"),
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    ShowPage(panelExport);
                    textBoxExportMaxDepth.Focus();
                    return false;
                }

                exportMaxDepth = parsedExportMaxDepth;
            }

            _settings.ShowFilesInTree = checkBoxShowFilesInTree.Checked;
            _settings.SkipReparsePoints = checkBoxSkipReparsePoints.Checked;
            _settings.ShowPartitionPanel = checkBoxShowPartitionPanel.Checked;
            _settings.StartElevatedOnStartup = checkBoxStartElevatedOnStartup.Checked;
            _settings.ShowElevationPromptOnStartup = checkBoxShowElevationPromptOnStartup.Checked;
            _settings.ShellContextMenuEnabled = checkBoxShellContextMenuEnabled.Checked;
            _settings.ExportPath = checkBoxExportPath.Checked;
            _settings.ExportSizeGb = checkBoxExportSizeGb.Checked;
            _settings.ExportSizeMb = checkBoxExportSizeMb.Checked;
            _settings.ExportMaxDepth = exportMaxDepth;
            _settings.PartitionFillColorLightArgb = partitionFillLightColor.ToArgb();
            _settings.PartitionFillBrightnessLightPercent =
                trackBarPartitionFillLightBrightness.Value;
            _settings.PartitionFillColorDarkArgb = partitionFillDarkColor.ToArgb();
            _settings.PartitionFillBrightnessDarkPercent =
                trackBarPartitionFillDarkBrightness.Value;
            string selectedScanHistoryDatabasePath = ScanHistoryService.NormalizeDatabasePath(
                textBoxScanHistoryDatabasePath.Text);

            if (!TryApplyScanHistoryDatabasePath(selectedScanHistoryDatabasePath))
            {
                ShowPage(panelStatistics);
                return false;
            }

            _settings.BarChartBarHeight = barChartBarHeight;
            _settings.SaveScanHistory = checkBoxSaveScanHistory.Checked;
            _settings.ScanHistoryDatabasePath = selectedScanHistoryDatabasePath;
            _settings.ScanHistoryMaximumScansPerPath = scanHistoryMaximumScansPerPath;
            ScanHistoryService.ConfigureRetention(scanHistoryMaximumScansPerPath);

            _settings.LogLevel = comboBoxLogLevel.SelectedItem is AppLogLevel selectedLogLevel
                ? selectedLogLevel
                : AppLogLevel.Normal;
            _settings.AutoSaveLog = checkBoxAutoSaveLog.Checked;
            _settings.MaximumLogFileSizeMb = maximumLogFileSizeMb;
            AppAlertLog.Configure(
                _settings.LogLevel,
                _settings.AutoSaveLog,
                _settings.MaximumLogFileSizeMb);

            if (comboBoxLanguage.SelectedItem is LanguageItem selectedLanguageItem)
            {
                _settings.LanguageCode = LocalizationService.NormalizeLanguageCode(
                    selectedLanguageItem.LanguageCode);
                LocalizationService.Load(_settings.LanguageCode);
            }

            if (comboBoxLayout.SelectedItem is LayoutItem layoutItem)
            {
                _settings.Layout = layoutItem.Layout;
            }

            try
            {
                ShellContextMenuService.Apply(_settings.ShellContextMenuEnabled);
            }
            catch
            {
                MessageBox.Show(
                    this,
                    LocalizationService.GetText("Settings.ShellContextMenuFailed"),
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private bool TryApplyScanHistoryDatabasePath(string selectedScanHistoryDatabasePath)
        {
            try
            {
                string currentDatabasePath = ScanHistoryService.NormalizeDatabasePath(
                    ScanHistoryService.DatabasePath);

                if (string.Equals(
                        currentDatabasePath,
                        selectedScanHistoryDatabasePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ScanHistoryService.ConfigureDatabasePath(selectedScanHistoryDatabasePath);
                }
                else
                {
                    switch (selectedDatabasePathSelectionMode)
                    {
                        case DatabasePathSelectionMode.MoveCurrentDatabase:
                            if (System.IO.File.Exists(selectedScanHistoryDatabasePath))
                            {
                                throw new System.IO.IOException(
                                    LocalizationService.GetText("DatabaseBrowse.TargetExists"));
                            }

                            ScanHistoryService.MoveDatabase(selectedScanHistoryDatabasePath);
                            break;

                        case DatabasePathSelectionMode.UseExistingDatabase:
                            if (!System.IO.File.Exists(selectedScanHistoryDatabasePath))
                            {
                                throw new System.IO.FileNotFoundException(
                                    LocalizationService.GetText("DatabaseBrowse.SourceMissing"),
                                    selectedScanHistoryDatabasePath);
                            }

                            ScanHistoryService.ConfigureDatabasePath(selectedScanHistoryDatabasePath);
                            break;

                        case DatabasePathSelectionMode.CreateNewDatabase:
                            if (System.IO.File.Exists(selectedScanHistoryDatabasePath))
                            {
                                throw new System.IO.IOException(
                                    LocalizationService.GetText("DatabaseBrowse.TargetExists"));
                            }

                            ScanHistoryService.ConfigureDatabasePath(selectedScanHistoryDatabasePath);
                            break;

                        default:
                            throw new InvalidOperationException(
                                LocalizationService.GetText("DatabaseBrowse.SelectionRequired"));
                    }
                }

                selectedDatabasePathSelectionMode = DatabasePathSelectionMode.None;
                textBoxScanHistoryDatabasePath.Text = ScanHistoryService.DatabasePath;
                UpdateScanHistoryDatabaseSize();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    LocalizationService.GetText("DatabaseBrowse.ApplyFailed") +
                    Environment.NewLine +
                    Environment.NewLine +
                    ex.Message,
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                textBoxScanHistoryDatabasePath.Focus();
                return false;
            }
        }

        private void UpdatePartitionFillControlsVisibility()
        {
            bool useDarkMode;

            if (comboBoxLayout.SelectedItem is LayoutItem selectedLayoutItem)
            {
                useDarkMode = selectedLayoutItem.Layout switch
                {
                    AppLayout.WindowsDarkMode => true,
                    AppLayout.WindowsLightMode => false,
                    _ => ThemeProvider.Theme.Colors.BackgroundPrimary.GetBrightness() < 0.5f
                };
            }
            else
            {
                useDarkMode = ThemeProvider.Theme.Colors.BackgroundPrimary.GetBrightness() < 0.5f;
            }

            labelPartitionFillLight.Visible = !useDarkMode;
            buttonPartitionFillLightColor.Visible = !useDarkMode;
            panelPartitionFillLightPreview.Visible = !useDarkMode;
            labelPartitionFillLightBrightness.Visible = !useDarkMode;
            trackBarPartitionFillLightBrightness.Visible = !useDarkMode;
            labelPartitionFillLightBrightnessValue.Visible = !useDarkMode;

            labelPartitionFillDark.Visible = useDarkMode;
            buttonPartitionFillDarkColor.Visible = useDarkMode;
            panelPartitionFillDarkPreview.Visible = useDarkMode;
            labelPartitionFillDarkBrightness.Visible = useDarkMode;
            trackBarPartitionFillDarkBrightness.Visible = useDarkMode;
            labelPartitionFillDarkBrightnessValue.Visible = useDarkMode;
        }

        private void UpdateColorPreviews()
        {
            Color lightPreviewColor = ApplyBrightness(
                partitionFillLightColor,
                trackBarPartitionFillLightBrightness.Value);
            Color darkPreviewColor = ApplyBrightness(
                partitionFillDarkColor,
                trackBarPartitionFillDarkBrightness.Value);

            panelPartitionFillLightPreview.BackColor = lightPreviewColor;
            panelPartitionFillDarkPreview.BackColor = darkPreviewColor;
            labelPartitionFillLightBrightnessValue.Text =
                trackBarPartitionFillLightBrightness.Value + " %";
            labelPartitionFillDarkBrightnessValue.Text =
                trackBarPartitionFillDarkBrightness.Value + " %";

            UpdatePartitionFillControlsVisibility();
        }

        private static Color ApplyBrightness(Color color, int brightnessPercent)
        {
            double factor = Math.Max(0, Math.Min(200, brightnessPercent)) / 100D;

            return Color.FromArgb(
                color.A,
                Math.Max(0, Math.Min(255, (int)Math.Round(color.R * factor))),
                Math.Max(0, Math.Min(255, (int)Math.Round(color.G * factor))),
                Math.Max(0, Math.Min(255, (int)Math.Round(color.B * factor))));
        }

        private sealed class LanguageItem
        {
            public LanguageItem(string text, string languageCode)
            {
                Text = text;
                LanguageCode = languageCode;
            }

            public string Text { get; }
            public string LanguageCode { get; }

            public override string ToString()
            {
                return Text;
            }
        }

        private sealed class LayoutItem
        {
            public LayoutItem(string text, AppLayout layout)
            {
                Text = text;
                Layout = layout;
            }

            public string Text { get; }
            public AppLayout Layout { get; }

            public override string ToString()
            {
                return Text;
            }
        }
    }
}
