using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WTF
{
    public static class ModernFormStyler
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 0x1;
        private const int HTLEFT = 0xA;
        private const int HTRIGHT = 0xB;
        private const int HTTOP = 0xC;
        private const int HTTOPLEFT = 0xD;
        private const int HTTOPRIGHT = 0xE;
        private const int HTBOTTOM = 0xF;
        private const int HTBOTTOMLEFT = 0x10;
        private const int HTBOTTOMRIGHT = 0x11;
        private const int ResizeGripSize = 8;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private static readonly Dictionary<Form, BorderlessResizeHandler> ResizeHandlers = new Dictionary<Form, BorderlessResizeHandler>();

        public static void Apply(Form form, AppLayout layout)
        {
            form.Icon = AppResources.ApplicationIcon;

            if (layout == AppLayout.Modern)
            {
                ApplyModern(form);
                return;
            }

            ApplyWindows(form, layout);
        }

        private static void ApplyModern(Form form)
        {
            bool allowMinimize = form.MinimizeBox;

            form.SuspendLayout();

            SetImmersiveDarkMode(form, true);

            form.FormBorderStyle = FormBorderStyle.None;
            form.BackColor = ModernTheme.WindowBackColor;
            form.ForeColor = ModernTheme.TextColor;
            form.Font = ModernTheme.DefaultFont;
            form.SizeGripStyle = IsResizable(form) ? SizeGripStyle.Auto : SizeGripStyle.Hide;

            MoveExistingControlsBelowTitleBar(form);
            AddModernTitleBar(form, allowMinimize);

            ModernWindowFrame.Apply(form);
            ApplyBorderlessResize(form);

            foreach (Control control in form.Controls)
            {
                ApplyControl(control);
            }

            form.ResumeLayout(false);
            form.PerformLayout();
        }

        private static void ApplyWindows(Form form, AppLayout layout)
        {
            form.SuspendLayout();

            RemoveModernTitleBar(form);
            ModernWindowFrame.Remove(form);

            form.FormBorderStyle = FormBorderStyle.Sizable;
            form.SizeGripStyle = IsResizable(form) ? SizeGripStyle.Auto : SizeGripStyle.Hide;
            form.Font = SystemFonts.MessageBoxFont;

            bool useDarkMode = layout == AppLayout.WindowsDarkMode;
            SetImmersiveDarkMode(form, useDarkMode);

            if (layout == AppLayout.WindowsDarkMode)
            {
                form.BackColor = Color.FromArgb(32, 32, 32);
                form.ForeColor = Color.White;
            }
            else
            {
                form.BackColor = SystemColors.Control;
                form.ForeColor = SystemColors.ControlText;
            }

            foreach (Control control in form.Controls)
            {
                ApplyWindowsControl(control, layout);
            }

            form.ResumeLayout(false);
            form.PerformLayout();
        }

        private static bool IsResizable(Form form)
        {
            return form.MinimumSize != form.MaximumSize;
        }

        private static void ApplyBorderlessResize(Form form)
        {
            if (!IsResizable(form))
                return;

            if (ResizeHandlers.ContainsKey(form))
                return;

            BorderlessResizeHandler resizeHandler = new BorderlessResizeHandler(form);
            ResizeHandlers.Add(form, resizeHandler);

            form.FormClosed += (sender, e) =>
            {
                resizeHandler.ReleaseHandle();
                ResizeHandlers.Remove(form);
            };
        }

        private static void RemoveModernTitleBar(Form form)
        {
            if (!form.Controls.ContainsKey("panelModernTitleBar"))
                return;

            Control panelModernTitleBar = form.Controls["panelModernTitleBar"];
            form.Controls.Remove(panelModernTitleBar);
            panelModernTitleBar.Dispose();

            foreach (Control control in form.Controls)
            {
                if (control.Dock == DockStyle.None)
                {
                    control.Top -= ModernTheme.TitleBarHeight;
                }
            }

            form.Padding = new Padding(
                form.Padding.Left,
                Math.Max(0, form.Padding.Top - ModernTheme.TitleBarHeight),
                form.Padding.Right,
                form.Padding.Bottom);

            form.Height -= ModernTheme.TitleBarHeight;
        }

        private sealed class BorderlessResizeHandler : NativeWindow
        {
            private readonly Form _form;

            public BorderlessResizeHandler(Form form)
            {
                _form = form;

                if (form.IsHandleCreated)
                {
                    AssignHandle(form.Handle);
                }

                form.HandleCreated += form_HandleCreated;
                form.HandleDestroyed += form_HandleDestroyed;
            }

            private void form_HandleCreated(object sender, System.EventArgs e)
            {
                AssignHandle(_form.Handle);
            }

            private void form_HandleDestroyed(object sender, System.EventArgs e)
            {
                ReleaseHandle();
            }

            protected override void WndProc(ref Message message)
            {
                base.WndProc(ref message);

                if (message.Msg != WM_NCHITTEST)
                    return;

                if ((int)message.Result != HTCLIENT)
                    return;

                if (_form.WindowState != FormWindowState.Normal)
                    return;

                Point cursorPosition = _form.PointToClient(Cursor.Position);
                bool left = cursorPosition.X <= ResizeGripSize;
                bool right = cursorPosition.X >= _form.ClientSize.Width - ResizeGripSize;
                bool top = cursorPosition.Y <= ResizeGripSize;
                bool bottom = cursorPosition.Y >= _form.ClientSize.Height - ResizeGripSize;

                if (left && top)
                {
                    message.Result = (IntPtr)HTTOPLEFT;
                }
                else if (right && top)
                {
                    message.Result = (IntPtr)HTTOPRIGHT;
                }
                else if (left && bottom)
                {
                    message.Result = (IntPtr)HTBOTTOMLEFT;
                }
                else if (right && bottom)
                {
                    message.Result = (IntPtr)HTBOTTOMRIGHT;
                }
                else if (left)
                {
                    message.Result = (IntPtr)HTLEFT;
                }
                else if (right)
                {
                    message.Result = (IntPtr)HTRIGHT;
                }
                else if (top)
                {
                    message.Result = (IntPtr)HTTOP;
                }
                else if (bottom)
                {
                    message.Result = (IntPtr)HTBOTTOM;
                }
            }
        }

        private static void MoveExistingControlsBelowTitleBar(Form form)
        {
            if (form.Controls.ContainsKey("panelModernTitleBar"))
                return;

            foreach (Control control in form.Controls)
            {
                if (control.Dock == DockStyle.None)
                {
                    control.Top += ModernTheme.TitleBarHeight;
                }
            }

            form.Padding = new Padding(
                form.Padding.Left,
                form.Padding.Top + ModernTheme.TitleBarHeight,
                form.Padding.Right,
                form.Padding.Bottom);

            form.Height += ModernTheme.TitleBarHeight;
        }

        private static void AddModernTitleBar(Form form, bool allowMinimize)
        {
            if (form.Controls.ContainsKey("panelModernTitleBar"))
                return;

            Panel panelModernTitleBar = new Panel
            {
                Name = "panelModernTitleBar",
                Location = new Point(0, 0),
                Size = new Size(form.ClientSize.Width, ModernTheme.TitleBarHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = ModernTheme.TitleBarBackColor
            };

            PictureBox pictureBoxModernTitleIcon = new PictureBox
            {
                Name = "pictureBoxModernTitleIcon",
                Location = new Point(10, 8),
                Size = new Size(16, 16),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = AppResources.ApplicationImage,
                BackColor = Color.Transparent
            };

            Label labelModernTitle = new Label
            {
                Name = "labelModernTitle",
                Text = " " + form.Text,
                AutoSize = false,
                Location = new Point(34, 0),
                Size = new Size(form.ClientSize.Width - 174, ModernTheme.TitleBarHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ModernTheme.TextColor,
                BackColor = Color.Transparent,
                Font = new Font(ModernTheme.FontFamilyName, ModernTheme.TitleFontSize, FontStyle.Regular)
            };

            Button buttonModernClose = CreateModernTitleBarButton(
                "buttonModernClose",
                "×",
                new Point(form.ClientSize.Width - ModernTheme.TitleBarButtonSize.Width, 0));

            buttonModernClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonModernClose.MouseEnter += (sender, e) => buttonModernClose.BackColor = ModernTheme.CloseButtonHoverColor;
            buttonModernClose.MouseLeave += (sender, e) => buttonModernClose.BackColor = ModernTheme.TitleBarBackColor;
            buttonModernClose.Click += (sender, e) => form.Close();

            Button buttonModernMinimize = CreateModernTitleBarButton(
                "buttonModernMinimize",
                "−",
                new Point(form.ClientSize.Width - ModernTheme.TitleBarButtonSize.Width * 2, 0));

            buttonModernMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonModernMinimize.Visible = allowMinimize;
            buttonModernMinimize.Click += (sender, e) => form.WindowState = FormWindowState.Minimized;

            panelModernTitleBar.MouseDown += (sender, e) => BeginWindowDrag(form, e);
            pictureBoxModernTitleIcon.MouseDown += (sender, e) => BeginWindowDrag(form, e);
            labelModernTitle.MouseDown += (sender, e) => BeginWindowDrag(form, e);

            panelModernTitleBar.Controls.Add(pictureBoxModernTitleIcon);
            panelModernTitleBar.Controls.Add(labelModernTitle);
            panelModernTitleBar.Controls.Add(buttonModernMinimize);
            panelModernTitleBar.Controls.Add(buttonModernClose);

            form.Controls.Add(panelModernTitleBar);
            panelModernTitleBar.BringToFront();
        }

        private static Button CreateModernTitleBarButton(string name, string text, Point location)
        {
            Button button = new Button
            {
                Name = name,
                Text = text,
                Location = location,
                Size = ModernTheme.TitleBarButtonSize,
                FlatStyle = FlatStyle.Flat,
                BackColor = ModernTheme.TitleBarBackColor,
                ForeColor = ModernTheme.TextColor,
                Font = new Font(ModernTheme.FontFamilyName, 10F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TabStop = false,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = ModernTheme.ControlHoverBackColor;
            button.FlatAppearance.MouseDownBackColor = ModernTheme.ControlBackColor;

            return button;
        }

        private static void BeginWindowDrag(Form form, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            ReleaseCapture();
            SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        private static void ApplyControl(Control control)
        {
            if (control is Button button)
            {
                if (!button.Name.StartsWith("buttonModern"))
                {
                    ModernTheme.ApplyButtonStyle(button);
                }
            }
            else if (control is Label label)
            {
                ModernTheme.ApplyLabelStyle(label);
            }
            else if (control is TextBox textBox)
            {
                ModernTheme.ApplyTextBoxStyle(textBox);
            }
            else if (control is ComboBox comboBox)
            {
                ModernTheme.ApplyComboBoxStyle(comboBox);
            }
            else if (control is CheckBox checkBox)
            {
                ModernTheme.ApplyCheckBoxStyle(checkBox);
            }
            else if (control is MenuStrip menuStrip)
            {
                ModernTheme.ApplyMenuStyle(menuStrip);
            }
            else if (control is ToolStrip toolStrip)
            {
                ModernTheme.ApplyToolStripStyle(toolStrip);
            }
            else if (control is DataGridView dataGridView)
            {
                ModernTheme.ApplyDataGridViewStyle(dataGridView);
            }
            else if (control is TreeView treeView)
            {
                ModernTheme.ApplyTreeViewStyle(treeView);
            }
            else if (control is ListView listView)
            {
                ModernTheme.ApplyListViewStyle(listView);
            }
            else if (control is PieChartView || control is BarChartView)
            {
                control.BackColor = ModernTheme.ControlBackColor;
                control.ForeColor = ModernTheme.TextColor;
                control.Font = ModernTheme.DefaultFont;
                control.Invalidate();
            }
            else if (control is Panel panel)
            {
                if (panel.Name != "panelModernTitleBar")
                {
                    panel.BackColor = ModernTheme.ControlBackColor;
                    panel.ForeColor = ModernTheme.TextColor;
                }
            }

            foreach (Control child in control.Controls)
            {
                ApplyControl(child);
            }
        }

        private static void ApplyWindowsControl(Control control, AppLayout layout)
        {
            Color windowBackColor;
            Color controlBackColor;
            Color headerBackColor;
            Color textColor;
            Color selectedBackColor;
            Color selectedTextColor;

            if (layout == AppLayout.WindowsDarkMode)
            {
                windowBackColor = Color.FromArgb(32, 32, 32);
                controlBackColor = Color.FromArgb(45, 45, 45);
                headerBackColor = Color.FromArgb(24, 24, 24);
                textColor = Color.White;
                selectedBackColor = SystemColors.Highlight;
                selectedTextColor = SystemColors.HighlightText;
            }
            else
            {
                windowBackColor = SystemColors.Window;
                controlBackColor = SystemColors.Control;
                headerBackColor = SystemColors.Control;
                textColor = SystemColors.ControlText;
                selectedBackColor = SystemColors.Highlight;
                selectedTextColor = SystemColors.HighlightText;
            }

            control.Font = SystemFonts.MessageBoxFont;
            control.ForeColor = textColor;

            if (control is TextBox textBox)
            {
                textBox.BorderStyle = BorderStyle.Fixed3D;
                textBox.BackColor = windowBackColor;
                textBox.ForeColor = textColor;
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.FlatStyle = FlatStyle.Standard;
                comboBox.BackColor = windowBackColor;
                comboBox.ForeColor = textColor;
            }
            else if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Standard;
                button.UseVisualStyleBackColor = layout != AppLayout.WindowsDarkMode;
                button.BackColor = layout == AppLayout.WindowsDarkMode ? controlBackColor : SystemColors.Control;
                button.ForeColor = textColor;
                button.Cursor = Cursors.Default;
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.UseVisualStyleBackColor = layout != AppLayout.WindowsDarkMode;
                checkBox.BackColor = layout == AppLayout.WindowsDarkMode ? windowBackColor : SystemColors.Control;
                checkBox.ForeColor = textColor;
            }
            else if (control is Label label)
            {
                label.BackColor = Color.Transparent;
                label.ForeColor = textColor;
            }
            else if (control is MenuStrip menuStrip)
            {
                menuStrip.BackColor = controlBackColor;
                menuStrip.ForeColor = textColor;
                menuStrip.RenderMode = ToolStripRenderMode.System;
                menuStrip.Renderer = null;

                foreach (ToolStripMenuItem item in menuStrip.Items)
                {
                    ApplyWindowsMenuItem(item, layout);
                }
            }
            else if (control is ToolStrip toolStrip)
            {
                toolStrip.BackColor = controlBackColor;
                toolStrip.ForeColor = textColor;
                toolStrip.RenderMode = ToolStripRenderMode.System;
                toolStrip.Renderer = null;
                toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            }
            else if (control is StatusStrip statusStrip)
            {
                statusStrip.BackColor = controlBackColor;
                statusStrip.ForeColor = textColor;
                statusStrip.RenderMode = ToolStripRenderMode.System;
                statusStrip.Renderer = null;
            }
            else if (control is DataGridView dataGridView)
            {
                dataGridView.BackgroundColor = windowBackColor;
                dataGridView.GridColor = layout == AppLayout.WindowsDarkMode ? Color.FromArgb(80, 80, 80) : SystemColors.ControlDark;
                dataGridView.BorderStyle = BorderStyle.Fixed3D;
                dataGridView.EnableHeadersVisualStyles = layout != AppLayout.WindowsDarkMode;
                dataGridView.DefaultCellStyle.BackColor = windowBackColor;
                dataGridView.DefaultCellStyle.ForeColor = textColor;
                dataGridView.DefaultCellStyle.SelectionBackColor = selectedBackColor;
                dataGridView.DefaultCellStyle.SelectionForeColor = selectedTextColor;
                dataGridView.ColumnHeadersDefaultCellStyle.BackColor = headerBackColor;
                dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = textColor;
                dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = headerBackColor;
                dataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor = textColor;
                dataGridView.RowHeadersDefaultCellStyle.BackColor = headerBackColor;
                dataGridView.RowHeadersDefaultCellStyle.ForeColor = textColor;
                dataGridView.RowHeadersDefaultCellStyle.SelectionBackColor = headerBackColor;
                dataGridView.RowHeadersDefaultCellStyle.SelectionForeColor = textColor;
                dataGridView.AlternatingRowsDefaultCellStyle.BackColor = windowBackColor;
            }
            else if (control is TreeView treeView)
            {
                treeView.BackColor = windowBackColor;
                treeView.ForeColor = textColor;
                treeView.BorderStyle = BorderStyle.Fixed3D;
            }
            else if (control is ListView listView)
            {
                if (layout == AppLayout.WindowsDarkMode)
                {
                    ModernTheme.ApplyListViewStyle(
                        listView,
                        headerBackColor,
                        windowBackColor,
                        textColor,
                        selectedBackColor,
                        selectedTextColor,
                        BorderStyle.Fixed3D,
                        SystemFonts.MessageBoxFont);
                }
                else
                {
                    ModernTheme.ApplyWindowsDefaultListViewStyle(
                        listView,
                        windowBackColor,
                        textColor,
                        SystemFonts.MessageBoxFont);
                }
            }
            else if (control is PieChartView || control is BarChartView)
            {
                control.BackColor = windowBackColor;
                control.ForeColor = textColor;
                control.Font = SystemFonts.MessageBoxFont;
                control.Invalidate();
            }
            else if (control is SplitContainer splitContainer)
            {
                splitContainer.BackColor = controlBackColor;
                splitContainer.ForeColor = textColor;
            }
            else if (control is SplitterPanel splitterPanel)
            {
                splitterPanel.BackColor = windowBackColor;
                splitterPanel.ForeColor = textColor;
            }
            else if (control is TableLayoutPanel tableLayoutPanel)
            {
                tableLayoutPanel.BackColor = windowBackColor;
                tableLayoutPanel.ForeColor = textColor;
            }
            else if (control is ToolStripPanel toolStripPanel)
            {
                toolStripPanel.BackColor = controlBackColor;
                toolStripPanel.ForeColor = textColor;
            }
            else if (control is Panel panel)
            {
                panel.BackColor = windowBackColor;
                panel.ForeColor = textColor;
            }
            else
            {
                control.BackColor = windowBackColor;
            }

            foreach (Control child in control.Controls)
            {
                ApplyWindowsControl(child, layout);
            }
        }

        private static void ApplyWindowsMenuItem(ToolStripMenuItem item, AppLayout layout)
        {
            Color backColor = layout == AppLayout.WindowsDarkMode
                ? Color.FromArgb(45, 45, 45)
                : SystemColors.Control;

            Color foreColor = layout == AppLayout.WindowsDarkMode
                ? Color.White
                : SystemColors.ControlText;

            item.BackColor = backColor;
            item.ForeColor = foreColor;
            item.Padding = new Padding(4, 0, 4, 0);

            foreach (ToolStripItem child in item.DropDownItems)
            {
                child.BackColor = backColor;
                child.ForeColor = foreColor;
                child.Padding = new Padding(4, 0, 4, 0);

                if (child is ToolStripMenuItem childMenuItem)
                {
                    ApplyWindowsMenuItem(childMenuItem, layout);
                }
            }
        }

        private static void SetImmersiveDarkMode(Form form, bool enabled)
        {
            if (!form.IsHandleCreated)
            {
                form.HandleCreated += (sender, e) => SetImmersiveDarkMode(form, enabled);
                return;
            }

            int useDarkMode = enabled ? 1 : 0;
            DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
        }
    }
}