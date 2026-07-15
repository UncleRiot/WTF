using System;
using System.Drawing;
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
        private Panel panelPageHost;
        private Panel panelGeneral;
        private Panel panelExport;
        private Panel panelColors;
        private Panel panelLayout;
        private LucidCheckBox checkBoxShowFilesInTree;
        private LucidCheckBox checkBoxSkipReparsePoints;
        private LucidCheckBox checkBoxShowPartitionPanel;
        private LucidCheckBox checkBoxStartElevatedOnStartup;
        private LucidCheckBox checkBoxShowElevationPromptOnStartup;
        private LucidCheckBox checkBoxShellContextMenuEnabled;
        private LucidLabel labelLanguage;
        private ComboBox comboBoxLanguage;
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
        private LucidButton buttonOk;
        private LucidButton buttonCancel;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;

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
                Size = new Size(116, 32),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonGeneralTab.Click += buttonGeneralTab_Click;

            buttonExportTab = new LucidButton
            {
                Name = "buttonExportTab",
                Text = LocalizationService.GetText("Settings.Export"),
                Location = new Point(140, 16),
                Size = new Size(116, 32),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonExportTab.Click += buttonExportTab_Click;

            buttonColorsTab = new LucidButton
            {
                Name = "buttonColorsTab",
                Text = LocalizationService.GetText("Settings.Colors"),
                Location = new Point(262, 16),
                Size = new Size(116, 32),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonColorsTab.Click += buttonColorsTab_Click;

            buttonLayoutTab = new LucidButton
            {
                Name = "buttonLayoutTab",
                Text = LocalizationService.GetText("Settings.LayoutTab"),
                Location = new Point(384, 16),
                Size = new Size(116, 32),
                ButtonStyle = LucidButtonStyle.Normal
            };
            buttonLayoutTab.Click += buttonLayoutTab_Click;

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

            labelLanguage = CreateLabel(
                "labelLanguage",
                LocalizationService.GetText("Settings.Language"),
                250);

            comboBoxLanguage = new ComboBox
            {
                Name = "comboBoxLanguage",
                Location = new Point(174, 250),
                Size = new Size(268, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Standard,
                BackColor = backgroundSecondary,
                ForeColor = ThemeProvider.Theme.Colors.TextPrimary,
                Font = SystemFonts.MessageBoxFont
            };

            comboBoxLanguage.Items.Add(new LanguageItem(
                LocalizationService.GetText("Settings.LanguageGerman"),
                LocalizationService.GermanLanguageCode));
            comboBoxLanguage.Items.Add(new LanguageItem(
                LocalizationService.GetText("Settings.LanguageEnglish"),
                LocalizationService.EnglishLanguageCode));
            labelLayout = CreateLabel(
                "labelLayout",
                LocalizationService.GetText("Settings.Layout"),
                290);

            comboBoxLayout = new ComboBox
            {
                Name = "comboBoxLayout",
                Location = new Point(174, 290),
                Size = new Size(268, 28),
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
                24);
            labelBarChartBarHeight.Size = new Size(220, 28);

            textBoxBarChartBarHeight = new LucidTextBox
            {
                Name = "textBoxBarChartBarHeight",
                Location = new Point(253, 24),
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
            labelBarChartBarHeightDefault.Location = new Point(308, 24);
            labelBarChartBarHeightDefault.Size = new Size(160, 28);

            panelGeneral.Controls.Add(checkBoxShowFilesInTree);
            panelGeneral.Controls.Add(checkBoxSkipReparsePoints);
            panelGeneral.Controls.Add(checkBoxShowPartitionPanel);
            panelGeneral.Controls.Add(checkBoxStartElevatedOnStartup);
            panelGeneral.Controls.Add(checkBoxShowElevationPromptOnStartup);
            panelGeneral.Controls.Add(checkBoxShellContextMenuEnabled);
            panelGeneral.Controls.Add(labelLanguage);
            panelGeneral.Controls.Add(comboBoxLanguage);
            panelGeneral.Controls.Add(labelLayout);
            panelGeneral.Controls.Add(comboBoxLayout);

            panelExport.Controls.Add(checkBoxExportPath);
            panelExport.Controls.Add(checkBoxExportSizeGb);
            panelExport.Controls.Add(checkBoxExportSizeMb);
            panelExport.Controls.Add(labelExportMaxDepth);
            panelExport.Controls.Add(textBoxExportMaxDepth);

            panelColors.Controls.Add(labelPartitionFillLight);
            panelColors.Controls.Add(buttonPartitionFillLightColor);
            panelColors.Controls.Add(panelPartitionFillLightPreview);
            panelColors.Controls.Add(labelPartitionFillLightBrightness);
            panelColors.Controls.Add(trackBarPartitionFillLightBrightness);
            panelColors.Controls.Add(labelPartitionFillLightBrightnessValue);
            panelColors.Controls.Add(labelPartitionFillDark);
            panelColors.Controls.Add(buttonPartitionFillDarkColor);
            panelColors.Controls.Add(panelPartitionFillDarkPreview);
            panelColors.Controls.Add(labelPartitionFillDarkBrightness);
            panelColors.Controls.Add(trackBarPartitionFillDarkBrightness);
            panelColors.Controls.Add(labelPartitionFillDarkBrightnessValue);

            panelLayout.Controls.Add(labelBarChartBarHeight);
            panelLayout.Controls.Add(textBoxBarChartBarHeight);
            panelLayout.Controls.Add(labelBarChartBarHeightDefault);

            panelPageHost.Controls.Add(panelGeneral);
            panelPageHost.Controls.Add(panelExport);
            panelPageHost.Controls.Add(panelColors);
            panelPageHost.Controls.Add(panelLayout);

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
            Controls.Add(buttonColorsTab);
            Controls.Add(buttonLayoutTab);
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
            buttonGeneralTab.Enabled = page != panelGeneral;
            buttonExportTab.Enabled = page != panelExport;
            buttonColorsTab.Enabled = page != panelColors;
            buttonLayoutTab.Enabled = page != panelLayout;
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
            _settings.BarChartBarHeight = barChartBarHeight;

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
