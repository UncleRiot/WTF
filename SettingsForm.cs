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
        private Panel panelPageHost;
        private Panel panelGeneral;
        private Panel panelExport;
        private LucidCheckBox checkBoxShowFilesInTree;
        private LucidCheckBox checkBoxSkipReparsePoints;
        private LucidCheckBox checkBoxShowPartitionPanel;
        private LucidCheckBox checkBoxStartElevatedOnStartup;
        private LucidCheckBox checkBoxShowElevationPromptOnStartup;
        private LucidCheckBox checkBoxShellContextMenuEnabled;
        private LucidLabel labelLanguage;
        private LucidComboBox comboBoxLanguage;
        private LucidLabel labelLayout;
        private LucidComboBox comboBoxLayout;
        private LucidCheckBox checkBoxExportPath;
        private LucidCheckBox checkBoxExportSizeGb;
        private LucidCheckBox checkBoxExportSizeMb;
        private LucidLabel labelExportMaxDepth;
        private LucidTextBox textBoxExportMaxDepth;
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

            comboBoxLanguage = new LucidComboBox
            {
                Name = "comboBoxLanguage",
                Location = new Point(174, 250),
                Size = new Size(268, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
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

            comboBoxLayout = new LucidComboBox
            {
                Name = "comboBoxLayout",
                Location = new Point(174, 290),
                Size = new Size(268, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
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

            panelPageHost.Controls.Add(panelGeneral);
            panelPageHost.Controls.Add(panelExport);

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

        private void ShowPage(Panel page)
        {
            panelGeneral.Visible = page == panelGeneral;
            panelExport.Visible = page == panelExport;
            buttonGeneralTab.Enabled = page != panelGeneral;
            buttonExportTab.Enabled = page != panelExport;
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
