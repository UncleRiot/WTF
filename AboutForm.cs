using System.Windows.Forms;

namespace WTF
{
    public sealed class AboutForm : Form
    {
        private readonly AppSettings _settings;

        private Label labelTitle;
        private Label labelInfo;
        private Button buttonOk;

        public AboutForm(AppSettings settings)
        {
            _settings = settings;

            InitializeComponent();
            ModernFormStyler.Apply(this, _settings.Layout);
        }

        private void InitializeComponent()
        {
            Text = "Über WTF";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(460, 220);
            MinimumSize = Size;
            MaximumSize = Size;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            labelTitle = new Label
            {
                Name = "labelTitle",
                Text = "WTF - Where’s The Filespace",
                Location = new System.Drawing.Point(24, 28),
                Size = new System.Drawing.Size(400, 28),
                Font = new System.Drawing.Font(ModernTheme.FontFamilyName, 12F, System.Drawing.FontStyle.Bold)
            };

            labelInfo = new Label
            {
                Name = "labelInfo",
                Text = "Speicherplatzanalyse für Windows.",
                Location = new System.Drawing.Point(24, 70),
                Size = new System.Drawing.Size(400, 60)
            };

            buttonOk = new Button
            {
                Name = "buttonOk",
                Text = "OK",
                Location = new System.Drawing.Point(360, 160),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.OK
            };

            Controls.Add(labelTitle);
            Controls.Add(labelInfo);
            Controls.Add(buttonOk);

            AcceptButton = buttonOk;
        }
    }
}