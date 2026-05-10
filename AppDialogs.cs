using System;
using System.Windows.Forms;

namespace WTF
{
    public static class AppDialogs
    {
        private const int IDI_QUESTION = 32514;
        private const int DI_NORMAL = 0x0003;

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool DrawIconEx(
            IntPtr hdc,
            int xLeft,
            int yTop,
            IntPtr hIcon,
            int cxWidth,
            int cyWidth,
            int istepIfAniCur,
            IntPtr hbrFlickerFreeDraw,
            int diFlags);

        public static ElevationPromptResult ShowElevationPrompt(AppSettings settings)
        {
            using DialogForm dialogForm = new DialogForm(
                settings,
                "WTF",
                "Möchten Sie WTF mit erhöhten Rechten ausführen, um die" + Environment.NewLine +
                "Scangeschwindigkeit und Genauigkeit zu steigern?",
                "Diese Meldung nicht mehr anzeigen",
                "Ja",
                "Nein");

            DialogResult dialogResult = dialogForm.ShowDialog();

            return new ElevationPromptResult(
                dialogResult == DialogResult.Yes,
                dialogForm.IsCheckBoxChecked);
        }

        public readonly struct ElevationPromptResult
        {
            public ElevationPromptResult(bool shouldRestartElevated, bool doNotShowAgain)
            {
                ShouldRestartElevated = shouldRestartElevated;
                DoNotShowAgain = doNotShowAgain;
            }

            public bool ShouldRestartElevated { get; }
            public bool DoNotShowAgain { get; }
        }

        private sealed class DialogForm : Form
        {
            private readonly AppSettings _settings;
            private readonly string _messageText;
            private readonly string _checkBoxText;
            private readonly string _yesButtonText;
            private readonly string _noButtonText;

            private NativeQuestionIconControl nativeQuestionIconControl;
            private Label labelMessage;
            private CheckBox checkBoxOption;
            private Button buttonYes;
            private Button buttonNo;

            public bool IsCheckBoxChecked
            {
                get { return checkBoxOption.Checked; }
            }

            public DialogForm(
                AppSettings settings,
                string title,
                string messageText,
                string checkBoxText,
                string yesButtonText,
                string noButtonText)
            {
                _settings = settings;
                _messageText = messageText;
                _checkBoxText = checkBoxText;
                _yesButtonText = yesButtonText;
                _noButtonText = noButtonText;

                Text = title;

                InitializeComponent();
                ModernFormStyler.Apply(this, _settings.Layout);
            }

            private void InitializeComponent()
            {
                StartPosition = FormStartPosition.CenterScreen;
                ClientSize = new System.Drawing.Size(430, 150);
                MinimumSize = Size;
                MaximumSize = Size;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                FormBorderStyle = FormBorderStyle.FixedDialog;

                nativeQuestionIconControl = new NativeQuestionIconControl
                {
                    Name = "nativeQuestionIconControl",
                    Location = new System.Drawing.Point(24, 24),
                    Size = new System.Drawing.Size(32, 32)
                };

                labelMessage = new Label
                {
                    Name = "labelMessage",
                    AutoSize = false,
                    Location = new System.Drawing.Point(74, 22),
                    Size = new System.Drawing.Size(334, 42),
                    BackColor = System.Drawing.Color.Transparent,
                    Text = _messageText
                };

                checkBoxOption = new CheckBox
                {
                    Name = "checkBoxOption",
                    AutoSize = true,
                    Location = new System.Drawing.Point(24, 80),
                    Text = _checkBoxText,
                    BackColor = System.Drawing.Color.Transparent
                };

                buttonYes = new Button
                {
                    Name = "buttonYes",
                    Text = _yesButtonText,
                    Size = new System.Drawing.Size(84, 30),
                    Location = new System.Drawing.Point(244, 108),
                    DialogResult = DialogResult.Yes
                };

                buttonNo = new Button
                {
                    Name = "buttonNo",
                    Text = _noButtonText,
                    Size = new System.Drawing.Size(84, 30),
                    Location = new System.Drawing.Point(336, 108),
                    DialogResult = DialogResult.No
                };

                Controls.Add(nativeQuestionIconControl);
                Controls.Add(labelMessage);
                Controls.Add(checkBoxOption);
                Controls.Add(buttonYes);
                Controls.Add(buttonNo);

                AcceptButton = buttonYes;
                CancelButton = buttonNo;
            }
        }

        private sealed class NativeQuestionIconControl : Control
        {
            public NativeQuestionIconControl()
            {
                SetStyle(ControlStyles.UserPaint, true);
                SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (Parent != null)
                {
                    using System.Drawing.SolidBrush backgroundBrush = new System.Drawing.SolidBrush(Parent.BackColor);
                    e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);
                }

                IntPtr questionIconHandle = LoadIcon(IntPtr.Zero, new IntPtr(IDI_QUESTION));

                if (questionIconHandle == IntPtr.Zero)
                    return;

                IntPtr hdc = e.Graphics.GetHdc();

                try
                {
                    DrawIconEx(
                        hdc,
                        0,
                        0,
                        questionIconHandle,
                        32,
                        32,
                        0,
                        IntPtr.Zero,
                        DI_NORMAL);
                }
                finally
                {
                    e.Graphics.ReleaseHdc(hdc);
                }
            }
        }
    }
}