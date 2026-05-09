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
            ModernFormStyler.Apply(this, _settings.Layout);
            LoadSettings();
        }

        private void InitializeComponent()
        {
            Text = "Einstellungen";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(460, 310);
            MinimumSize = Size;
            MaximumSize = Size;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            tabControlSettings = new TabControl
            {
                Name = "tabControlSettings",
                Location = new System.Drawing.Point(12, 12),
                Size = new System.Drawing.Size(436, 238)
            };

            tabPageGeneral = new TabPage
            {
                Name = "tabPageGeneral",
                Text = "Allgemein"
            };

            tabPageExport = new TabPage
            {
                Name = "tabPageExport",
                Text = "Export"
            };

            checkBoxShowFilesInTree = new CheckBox
            {
                Name = "checkBoxShowFilesInTree",
                Text = "Dateien im Baum anzeigen",
                Location = new System.Drawing.Point(12, 18),
                AutoSize = true
            };

            checkBoxSkipReparsePoints = new CheckBox
            {
                Name = "checkBoxSkipReparsePoints",
                Text = "Reparse Points / Junctions überspringen",
                Location = new System.Drawing.Point(12, 50),
                AutoSize = true
            };

            checkBoxShowPartitionPanel = new CheckBox
            {
                Name = "checkBoxShowPartitionPanel",
                Text = "Partitionsfenster anzeigen",
                Location = new System.Drawing.Point(12, 82),
                AutoSize = true
            };

            labelLayout = new Label
            {
                Name = "labelLayout",
                Text = "Layout:",
                Location = new System.Drawing.Point(12, 122),
                Size = new System.Drawing.Size(120, 23),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            comboBoxLayout = new ComboBox
            {
                Name = "comboBoxLayout",
                Location = new System.Drawing.Point(138, 122),
                Size = new System.Drawing.Size(205, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            comboBoxLayout.Items.Add(new LayoutItem("Modern", AppLayout.Modern));
            comboBoxLayout.Items.Add(new LayoutItem("Windows default", AppLayout.WindowsDefault));
            comboBoxLayout.Items.Add(new LayoutItem("Windows light mode", AppLayout.WindowsLightMode));
            comboBoxLayout.Items.Add(new LayoutItem("Windows dark mode", AppLayout.WindowsDarkMode));

            checkBoxExportPath = new CheckBox
            {
                Name = "checkBoxExportPath",
                Text = "Path exportieren",
                Location = new System.Drawing.Point(12, 18),
                AutoSize = true
            };

            checkBoxExportSizeGb = new CheckBox
            {
                Name = "checkBoxExportSizeGb",
                Text = "Size (GB) exportieren",
                Location = new System.Drawing.Point(12, 50),
                AutoSize = true
            };

            checkBoxExportSizeMb = new CheckBox
            {
                Name = "checkBoxExportSizeMb",
                Text = "Size (MB) exportieren",
                Location = new System.Drawing.Point(12, 82),
                AutoSize = true
            };

            labelExportMaxDepth = new Label
            {
                Name = "labelExportMaxDepth",
                Text = "Maximale Ebenen/Tiefe:",
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
                Text = "OK",
                Location = new System.Drawing.Point(280, 266),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.OK
            };

            buttonCancel = new Button
            {
                Name = "buttonCancel",
                Text = "Abbrechen",
                Location = new System.Drawing.Point(365, 266),
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

        private void LoadSettings()
        {
            checkBoxShowFilesInTree.Checked = _settings.ShowFilesInTree;
            checkBoxSkipReparsePoints.Checked = _settings.SkipReparsePoints;
            checkBoxShowPartitionPanel.Checked = _settings.ShowPartitionPanel;
            checkBoxExportPath.Checked = _settings.ExportPath;
            checkBoxExportSizeGb.Checked = _settings.ExportSizeGb;
            checkBoxExportSizeMb.Checked = _settings.ExportSizeMb;
            textBoxExportMaxDepth.Text = _settings.ExportMaxDepth.HasValue ? _settings.ExportMaxDepth.Value.ToString() : string.Empty;

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
                    MessageBox.Show(this, "Die maximale Ebenen/Tiefe muss leer oder eine Zahl ab 0 sein.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tabControlSettings.SelectedTab = tabPageExport;
                    textBoxExportMaxDepth.Focus();
                    return false;
                }

                exportMaxDepth = parsedExportMaxDepth;
            }

            _settings.ShowFilesInTree = checkBoxShowFilesInTree.Checked;
            _settings.SkipReparsePoints = checkBoxSkipReparsePoints.Checked;
            _settings.ShowPartitionPanel = checkBoxShowPartitionPanel.Checked;
            _settings.ExportPath = checkBoxExportPath.Checked;
            _settings.ExportSizeGb = checkBoxExportSizeGb.Checked;
            _settings.ExportSizeMb = checkBoxExportSizeMb.Checked;
            _settings.ExportMaxDepth = exportMaxDepth;

            if (comboBoxLayout.SelectedItem is LayoutItem layoutItem)
            {
                _settings.Layout = layoutItem.Layout;
            }

            return true;
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
