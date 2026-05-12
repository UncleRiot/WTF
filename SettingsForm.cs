using System;
using System.Windows.Forms;

namespace WTF
{
    public sealed class SettingsForm : Form
    {
        private readonly AppSettings _settings;

        private TabControl tabControlSettings;
        private TabPage tabPageGeneral;
        private TabPage tabPageExport;
        private CheckBox checkBoxShowFilesInTree;
        private CheckBox checkBoxSkipReparsePoints;
        private CheckBox checkBoxShowPartitionPanel;
        private CheckBox checkBoxStartElevatedOnStartup;
        private CheckBox checkBoxShowElevationPromptOnStartup;
        private CheckBox checkBoxShellContextMenuEnabled;
        private Label labelLanguage;
        private ComboBox comboBoxLanguage;
        private Label labelLayout;
        private ComboBox comboBoxLayout;
        private CheckBox checkBoxExportPath;
        private CheckBox checkBoxExportSizeGb;
        private CheckBox checkBoxExportSizeMb;
        private Label labelExportMaxDepth;
        private TextBox textBoxExportMaxDepth;
        private Button buttonOk;
        private Button buttonCancel;

        public SettingsForm(AppSettings settings)
        {
            _settings = settings;

            InitializeComponent();
            WindowsFormStyler.Apply(this, _settings.Layout);
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = LocalizationService.GetText("Settings.Title");
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(460, 414);
            MinimumSize = Size;
            MaximumSize = Size;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            KeyDown += SettingsForm_KeyDown;

            tabControlSettings = new TabControl
            {
                Name = "tabControlSettings",
                Location = new System.Drawing.Point(12, 12),
                Size = new System.Drawing.Size(436, 342)
            };

            tabPageGeneral = new TabPage
            {
                Name = "tabPageGeneral",
                Text = LocalizationService.GetText("Settings.General")
            };

            tabPageExport = new TabPage
            {
                Name = "tabPageExport",
                Text = LocalizationService.GetText("Settings.Export")
            };

            checkBoxShowFilesInTree = new CheckBox
            {
                Name = "checkBoxShowFilesInTree",
                Text = LocalizationService.GetText("Settings.ShowFilesInTree"),
                Location = new System.Drawing.Point(12, 18),
                AutoSize = true
            };

            checkBoxSkipReparsePoints = new CheckBox
            {
                Name = "checkBoxSkipReparsePoints",
                Text = LocalizationService.GetText("Settings.SkipReparsePoints"),
                Location = new System.Drawing.Point(12, 50),
                AutoSize = true
            };

            checkBoxShowPartitionPanel = new CheckBox
            {
                Name = "checkBoxShowPartitionPanel",
                Text = LocalizationService.GetText("Settings.ShowPartitionPanel"),
                Location = new System.Drawing.Point(12, 82),
                AutoSize = true
            };

            checkBoxStartElevatedOnStartup = new CheckBox
            {
                Name = "checkBoxStartElevatedOnStartup",
                Text = LocalizationService.GetText("Settings.StartElevated"),
                Location = new System.Drawing.Point(12, 114),
                AutoSize = true
            };

            checkBoxShowElevationPromptOnStartup = new CheckBox
            {
                Name = "checkBoxShowElevationPromptOnStartup",
                Text = LocalizationService.GetText("Settings.ShowElevationPrompt"),
                Location = new System.Drawing.Point(12, 146),
                AutoSize = true
            };

            checkBoxShellContextMenuEnabled = new CheckBox
            {
                Name = "checkBoxShellContextMenuEnabled",
                Text = LocalizationService.GetText("Settings.ShellContextMenu"),
                Location = new System.Drawing.Point(12, 178),
                AutoSize = true
            };

            labelLanguage = new Label
            {
                Name = "labelLanguage",
                Text = LocalizationService.GetText("Settings.Language"),
                Location = new System.Drawing.Point(12, 218),
                Size = new System.Drawing.Size(120, 23),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            comboBoxLanguage = new ComboBox
            {
                Name = "comboBoxLanguage",
                Location = new System.Drawing.Point(138, 218),
                Size = new System.Drawing.Size(205, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            comboBoxLanguage.Items.Add(new LanguageItem(LocalizationService.GetText("Settings.LanguageGerman"), LocalizationService.GermanLanguageCode));
            comboBoxLanguage.Items.Add(new LanguageItem(LocalizationService.GetText("Settings.LanguageEnglish"), LocalizationService.EnglishLanguageCode));

            labelLayout = new Label
            {
                Name = "labelLayout",
                Text = LocalizationService.GetText("Settings.Layout"),
                Location = new System.Drawing.Point(12, 252),
                Size = new System.Drawing.Size(120, 23),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            comboBoxLayout = new ComboBox
            {
                Name = "comboBoxLayout",
                Location = new System.Drawing.Point(138, 252),
                Size = new System.Drawing.Size(205, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            comboBoxLayout.Items.Add(new LayoutItem(LocalizationService.GetText("Settings.LayoutWindowsDefault"), AppLayout.WindowsDefault));
            comboBoxLayout.Items.Add(new LayoutItem(LocalizationService.GetText("Settings.LayoutWindowsLight"), AppLayout.WindowsLightMode));
            comboBoxLayout.Items.Add(new LayoutItem(LocalizationService.GetText("Settings.LayoutWindowsDark"), AppLayout.WindowsDarkMode));

            checkBoxExportPath = new CheckBox
            {
                Name = "checkBoxExportPath",
                Text = LocalizationService.GetText("Settings.ExportPath"),
                Location = new System.Drawing.Point(12, 18),
                AutoSize = true
            };

            checkBoxExportSizeGb = new CheckBox
            {
                Name = "checkBoxExportSizeGb",
                Text = LocalizationService.GetText("Settings.ExportSizeGb"),
                Location = new System.Drawing.Point(12, 50),
                AutoSize = true
            };

            checkBoxExportSizeMb = new CheckBox
            {
                Name = "checkBoxExportSizeMb",
                Text = LocalizationService.GetText("Settings.ExportSizeMb"),
                Location = new System.Drawing.Point(12, 82),
                AutoSize = true
            };

            labelExportMaxDepth = new Label
            {
                Name = "labelExportMaxDepth",
                Text = LocalizationService.GetText("Settings.ExportMaxDepth"),
                Location = new System.Drawing.Point(12, 122),
                Size = new System.Drawing.Size(150, 23),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            textBoxExportMaxDepth = new TextBox
            {
                Name = "textBoxExportMaxDepth",
                Location = new System.Drawing.Point(168, 122),
                Size = new System.Drawing.Size(80, 23)
            };

            tabPageGeneral.Controls.Add(checkBoxShowFilesInTree);
            tabPageGeneral.Controls.Add(checkBoxSkipReparsePoints);
            tabPageGeneral.Controls.Add(checkBoxShowPartitionPanel);
            tabPageGeneral.Controls.Add(checkBoxStartElevatedOnStartup);
            tabPageGeneral.Controls.Add(checkBoxShowElevationPromptOnStartup);
            tabPageGeneral.Controls.Add(checkBoxShellContextMenuEnabled);
            tabPageGeneral.Controls.Add(labelLanguage);
            tabPageGeneral.Controls.Add(comboBoxLanguage);
            tabPageGeneral.Controls.Add(labelLayout);
            tabPageGeneral.Controls.Add(comboBoxLayout);

            tabPageExport.Controls.Add(checkBoxExportPath);
            tabPageExport.Controls.Add(checkBoxExportSizeGb);
            tabPageExport.Controls.Add(checkBoxExportSizeMb);
            tabPageExport.Controls.Add(labelExportMaxDepth);
            tabPageExport.Controls.Add(textBoxExportMaxDepth);

            tabControlSettings.TabPages.Add(tabPageGeneral);
            tabControlSettings.TabPages.Add(tabPageExport);

            buttonOk = new Button
            {
                Name = "buttonOk",
                Text = LocalizationService.GetText("Common.OK"),
                Location = new System.Drawing.Point(280, 370),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.OK
            };

            buttonCancel = new Button
            {
                Name = "buttonCancel",
                Text = LocalizationService.GetText("Common.Cancel"),
                Location = new System.Drawing.Point(365, 370),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            buttonOk.Click += buttonOk_Click;

            Controls.Add(tabControlSettings);
            Controls.Add(buttonOk);
            Controls.Add(buttonCancel);

            AcceptButton = buttonOk;
            CancelButton = buttonCancel;
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
            textBoxExportMaxDepth.Text = _settings.ExportMaxDepth.HasValue ? _settings.ExportMaxDepth.Value.ToString() : string.Empty;

            for (int index = 0; index < comboBoxLanguage.Items.Count; index++)
            {
                if (comboBoxLanguage.Items[index] is LanguageItem languageItem &&
                    string.Equals(languageItem.LanguageCode, LocalizationService.NormalizeLanguageCode(_settings.LanguageCode), StringComparison.OrdinalIgnoreCase))
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
                if (comboBoxLayout.Items[index] is LayoutItem layoutItem && layoutItem.Layout == _settings.Layout)
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
                if (!int.TryParse(textBoxExportMaxDepth.Text.Trim(), out int parsedExportMaxDepth) || parsedExportMaxDepth < 0)
                {
                    MessageBox.Show(this, LocalizationService.GetText("Settings.ExportMaxDepthInvalid"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tabControlSettings.SelectedTab = tabPageExport;
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
                _settings.LanguageCode = LocalizationService.NormalizeLanguageCode(selectedLanguageItem.LanguageCode);
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
                MessageBox.Show(this, LocalizationService.GetText("Settings.ShellContextMenuFailed"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
