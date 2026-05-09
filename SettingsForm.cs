using System;
using System.Windows.Forms;

namespace WTF
{
    public sealed class SettingsForm : Form
    {
        private readonly AppSettings _settings;

        private CheckBox checkBoxShowFilesInTree;
        private CheckBox checkBoxSkipReparsePoints;
        private CheckBox checkBoxShowPartitionPanel;
        private Label labelLayout;
        private ComboBox comboBoxLayout;
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
            ClientSize = new System.Drawing.Size(460, 260);
            MinimumSize = Size;
            MaximumSize = Size;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            checkBoxShowFilesInTree = new CheckBox
            {
                Name = "checkBoxShowFilesInTree",
                Text = "Dateien im Baum anzeigen",
                Location = new System.Drawing.Point(24, 24),
                AutoSize = true
            };

            checkBoxSkipReparsePoints = new CheckBox
            {
                Name = "checkBoxSkipReparsePoints",
                Text = "Reparse Points / Junctions überspringen",
                Location = new System.Drawing.Point(24, 56),
                AutoSize = true
            };

            checkBoxShowPartitionPanel = new CheckBox
            {
                Name = "checkBoxShowPartitionPanel",
                Text = "Partitionsfenster anzeigen",
                Location = new System.Drawing.Point(24, 88),
                AutoSize = true
            };

            labelLayout = new Label
            {
                Name = "labelLayout",
                Text = "Layout:",
                Location = new System.Drawing.Point(24, 128),
                Size = new System.Drawing.Size(120, 23),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            comboBoxLayout = new ComboBox
            {
                Name = "comboBoxLayout",
                Location = new System.Drawing.Point(150, 128),
                Size = new System.Drawing.Size(205, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            comboBoxLayout.Items.Add(new LayoutItem("Modern", AppLayout.Modern));
            comboBoxLayout.Items.Add(new LayoutItem("Windows default", AppLayout.WindowsDefault));
            comboBoxLayout.Items.Add(new LayoutItem("Windows light mode", AppLayout.WindowsLightMode));
            comboBoxLayout.Items.Add(new LayoutItem("Windows dark mode", AppLayout.WindowsDarkMode));

            buttonOk = new Button
            {
                Name = "buttonOk",
                Text = "OK",
                Location = new System.Drawing.Point(280, 200),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.OK
            };

            buttonCancel = new Button
            {
                Name = "buttonCancel",
                Text = "Abbrechen",
                Location = new System.Drawing.Point(365, 200),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            buttonOk.Click += buttonOk_Click;

            Controls.Add(checkBoxShowFilesInTree);
            Controls.Add(checkBoxSkipReparsePoints);
            Controls.Add(checkBoxShowPartitionPanel);
            Controls.Add(labelLayout);
            Controls.Add(comboBoxLayout);
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
            _settings.ShowFilesInTree = checkBoxShowFilesInTree.Checked;
            _settings.SkipReparsePoints = checkBoxSkipReparsePoints.Checked;
            _settings.ShowPartitionPanel = checkBoxShowPartitionPanel.Checked;

            if (comboBoxLayout.SelectedItem is LayoutItem layoutItem)
            {
                _settings.Layout = layoutItem.Layout;
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