using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WTF
{
    public static class ModernFormStyler
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void Apply(Form form, AppLayout layout)
        {
            form.Icon = AppResources.ApplicationIcon;
            ApplyWindowsTheme(form, layout);
        }

        private static void ApplyWindowsTheme(Form form, AppLayout layout)
        {
            form.SuspendLayout();

            bool useDarkMode = ShouldUseDarkMode(layout);

            form.Font = SystemFonts.MessageBoxFont;
            form.SizeGripStyle = IsResizable(form) ? SizeGripStyle.Auto : SizeGripStyle.Hide;

            SetImmersiveDarkMode(form, useDarkMode);

            if (useDarkMode)
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
                ApplyWindowsControl(control, useDarkMode);
            }

            form.ResumeLayout(false);
            form.PerformLayout();
        }

        private static bool IsResizable(Form form)
        {
            return form.MinimumSize != form.MaximumSize;
        }

        private static bool ShouldUseDarkMode(AppLayout layout)
        {
            if (layout == AppLayout.WindowsDarkMode)
                return true;

            if (layout == AppLayout.WindowsLightMode)
                return false;

            return IsWindowsAppDarkModeEnabled();
        }

        private static bool IsWindowsAppDarkModeEnabled()
        {
            try
            {
                using Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

                object value = key?.GetValue("AppsUseLightTheme");

                if (value is int appsUseLightTheme)
                {
                    return appsUseLightTheme == 0;
                }
            }
            catch
            {
            }

            return false;
        }

        private static void ApplyWindowsControl(Control control, bool useDarkMode)
        {
            Color windowBackColor;
            Color controlBackColor;
            Color headerBackColor;
            Color textColor;
            Color selectedBackColor;
            Color selectedTextColor;
            Color gridColor;

            if (useDarkMode)
            {
                windowBackColor = Color.FromArgb(32, 32, 32);
                controlBackColor = Color.FromArgb(45, 45, 45);
                headerBackColor = Color.FromArgb(24, 24, 24);
                textColor = Color.White;
                selectedBackColor = SystemColors.Highlight;
                selectedTextColor = SystemColors.HighlightText;
                gridColor = Color.FromArgb(80, 80, 80);
            }
            else
            {
                windowBackColor = SystemColors.Window;
                controlBackColor = SystemColors.Control;
                headerBackColor = SystemColors.Control;
                textColor = SystemColors.ControlText;
                selectedBackColor = SystemColors.Highlight;
                selectedTextColor = SystemColors.HighlightText;
                gridColor = SystemColors.ControlDark;
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
                button.UseVisualStyleBackColor = !useDarkMode;
                button.BackColor = useDarkMode ? controlBackColor : SystemColors.Control;
                button.ForeColor = textColor;
                button.Cursor = Cursors.Default;
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.UseVisualStyleBackColor = !useDarkMode;
                checkBox.BackColor = useDarkMode ? windowBackColor : SystemColors.Control;
                checkBox.ForeColor = textColor;
            }
            else if (control is Label label)
            {
                label.BackColor = Color.Transparent;
                label.ForeColor = textColor;
            }
            else if (control is LinkLabel linkLabel)
            {
                linkLabel.BackColor = Color.Transparent;
                linkLabel.ForeColor = textColor;
                linkLabel.LinkColor = useDarkMode ? Color.LightSkyBlue : SystemColors.HotTrack;
                linkLabel.ActiveLinkColor = useDarkMode ? Color.DeepSkyBlue : SystemColors.Highlight;
                linkLabel.VisitedLinkColor = useDarkMode ? Color.Plum : SystemColors.HotTrack;
            }
            else if (control is MenuStrip menuStrip)
            {
                menuStrip.BackColor = controlBackColor;
                menuStrip.ForeColor = textColor;
                menuStrip.RenderMode = ToolStripRenderMode.System;
                menuStrip.Renderer = null;

                foreach (ToolStripMenuItem item in menuStrip.Items)
                {
                    ApplyWindowsMenuItem(item, useDarkMode);
                }
            }
            else if (control is ToolStrip toolStrip)
            {
                toolStrip.BackColor = controlBackColor;
                toolStrip.ForeColor = textColor;
                toolStrip.RenderMode = ToolStripRenderMode.System;
                toolStrip.Renderer = null;
                toolStrip.GripStyle = ToolStripGripStyle.Hidden;

                foreach (ToolStripItem item in toolStrip.Items)
                {
                    ApplyWindowsToolStripItem(item, useDarkMode);
                }
            }
            else if (control is StatusStrip statusStrip)
            {
                statusStrip.BackColor = controlBackColor;
                statusStrip.ForeColor = textColor;
                statusStrip.RenderMode = ToolStripRenderMode.System;
                statusStrip.Renderer = null;

                foreach (ToolStripItem item in statusStrip.Items)
                {
                    ApplyWindowsToolStripItem(item, useDarkMode);
                }
            }
            else if (control is DataGridView dataGridView)
            {
                dataGridView.BackgroundColor = windowBackColor;
                dataGridView.GridColor = gridColor;
                dataGridView.BorderStyle = BorderStyle.Fixed3D;
                dataGridView.EnableHeadersVisualStyles = !useDarkMode;

                dataGridView.DefaultCellStyle.BackColor = windowBackColor;
                dataGridView.DefaultCellStyle.ForeColor = textColor;
                dataGridView.DefaultCellStyle.SelectionBackColor = selectedBackColor;
                dataGridView.DefaultCellStyle.SelectionForeColor = selectedTextColor;

                dataGridView.AlternatingRowsDefaultCellStyle.BackColor = windowBackColor;
                dataGridView.AlternatingRowsDefaultCellStyle.ForeColor = textColor;
                dataGridView.AlternatingRowsDefaultCellStyle.SelectionBackColor = selectedBackColor;
                dataGridView.AlternatingRowsDefaultCellStyle.SelectionForeColor = selectedTextColor;

                dataGridView.ColumnHeadersDefaultCellStyle.BackColor = headerBackColor;
                dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = textColor;
                dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = headerBackColor;
                dataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor = textColor;

                dataGridView.RowHeadersDefaultCellStyle.BackColor = headerBackColor;
                dataGridView.RowHeadersDefaultCellStyle.ForeColor = textColor;
                dataGridView.RowHeadersDefaultCellStyle.SelectionBackColor = headerBackColor;
                dataGridView.RowHeadersDefaultCellStyle.SelectionForeColor = textColor;
            }
            else if (control is TreeView treeView)
            {
                treeView.BackColor = windowBackColor;
                treeView.ForeColor = textColor;
                treeView.BorderStyle = BorderStyle.Fixed3D;
            }
            else if (control is ListView listView)
            {
                listView.BackColor = windowBackColor;
                listView.ForeColor = textColor;
                listView.BorderStyle = BorderStyle.Fixed3D;
                listView.Font = SystemFonts.MessageBoxFont;
            }
            else if (control is Chart_PieChart || control is Chart_BarChart)
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
                ApplyWindowsControl(child, useDarkMode);
            }
        }

        private static void ApplyWindowsMenuItem(ToolStripMenuItem item, bool useDarkMode)
        {
            Color backColor = useDarkMode
                ? Color.FromArgb(45, 45, 45)
                : SystemColors.Control;

            Color foreColor = useDarkMode
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
                    ApplyWindowsMenuItem(childMenuItem, useDarkMode);
                }
            }
        }

        private static void ApplyWindowsToolStripItem(ToolStripItem item, bool useDarkMode)
        {
            item.BackColor = useDarkMode
                ? Color.FromArgb(45, 45, 45)
                : SystemColors.Control;

            item.ForeColor = useDarkMode
                ? Color.White
                : SystemColors.ControlText;
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