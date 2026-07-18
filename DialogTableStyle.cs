using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Lucid.Theming;

namespace WTF
{
    public static class DialogTableStyle
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(
            IntPtr hwnd,
            string pszSubAppName,
            string pszSubIdList);

        public const int HorizontalMargin = 15;

        public static Padding CreateHorizontalPadding(int top, int bottom)
        {
            return new Padding(HorizontalMargin, top, HorizontalMargin, bottom);
        }

        public static Panel CreateTableHost(
            Control content,
            Color backColor,
            int top,
            int bottom)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = CreateHorizontalPadding(top, bottom),
                BackColor = backColor
            };

            content.Dock = DockStyle.Fill;
            panel.Controls.Add(content);

            return panel;
        }

        public static void ConfigureTablePage(TabPage tabPage, Color backColor)
        {
            if (tabPage == null)
                return;

            tabPage.Padding = new Padding(8);
            tabPage.BackColor = backColor;
        }

        public static void Apply(DataGridView grid)
        {
            if (grid == null)
                return;

            Color backgroundPrimary = ThemeProvider.Theme.Colors.BackgroundPrimary;
            Color backgroundSecondary = ThemeProvider.Theme.Colors.BackgroundSecondary;
            Color textPrimary = ThemeProvider.Theme.Colors.TextPrimary;
            Color selectionBackColor = ThemeProvider.Theme.Colors.Accent;
            Color headerBackColor = ControlPaint.Dark(backgroundSecondary, 0.08f);
            Color gridColor = ControlPaint.Dark(backgroundSecondary, 0.2f);

            grid.BackgroundColor = backgroundSecondary;
            grid.GridColor = gridColor;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.EnableHeadersVisualStyles = false;

            grid.DefaultCellStyle.BackColor = backgroundSecondary;
            grid.DefaultCellStyle.ForeColor = textPrimary;
            grid.DefaultCellStyle.SelectionBackColor = selectionBackColor;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;

            grid.AlternatingRowsDefaultCellStyle.BackColor = backgroundPrimary;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = textPrimary;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = selectionBackColor;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            grid.ColumnHeadersDefaultCellStyle.BackColor = headerBackColor;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = textPrimary;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = headerBackColor;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = textPrimary;

            foreach (DataGridViewColumn column in grid.Columns)
            {
                if (column.SortMode != DataGridViewColumnSortMode.NotSortable)
                {
                    column.SortMode = DataGridViewColumnSortMode.Programmatic;
                }
            }

            grid.HandleCreated -= Grid_HandleCreated;
            grid.HandleCreated += Grid_HandleCreated;
            grid.ControlAdded -= Grid_ControlAdded;
            grid.ControlAdded += Grid_ControlAdded;

            ApplyScrollBarTheme(grid);
        }

        public static void ApplyDetails(DataGridView grid)
        {
            Apply(grid);

            if (grid == null)
                return;

            Color backgroundSecondary = ThemeProvider.Theme.Colors.BackgroundSecondary;
            Color textPrimary = ThemeProvider.Theme.Colors.TextPrimary;

            grid.DefaultCellStyle.SelectionBackColor = backgroundSecondary;
            grid.DefaultCellStyle.SelectionForeColor = textPrimary;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = backgroundSecondary;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = textPrimary;
        }

        private static void Grid_HandleCreated(object sender, EventArgs e)
        {
            if (sender is DataGridView grid)
            {
                ApplyScrollBarTheme(grid);
            }
        }

        private static void Grid_ControlAdded(object sender, ControlEventArgs e)
        {
            if (sender is DataGridView grid)
            {
                ApplyScrollBarTheme(grid);
            }
        }

        private static void ApplyScrollBarTheme(DataGridView grid)
        {
            if (grid == null || !grid.IsHandleCreated)
                return;

            bool useDarkMode = IsDarkTheme();
            ApplyNativeTheme(grid, useDarkMode);

            foreach (Control child in grid.Controls)
            {
                if (child is ScrollBar)
                {
                    ApplyNativeTheme(child, useDarkMode);
                    child.BackColor = ThemeProvider.Theme.Colors.BackgroundSecondary;
                    child.ForeColor = ThemeProvider.Theme.Colors.TextPrimary;
                }
            }
        }

        private static void ApplyNativeTheme(Control control, bool useDarkMode)
        {
            try
            {
                SetWindowTheme(
                    control.Handle,
                    useDarkMode ? "DarkMode_Explorer" : "Explorer",
                    null);
            }
            catch
            {
            }
        }

        private static bool IsDarkTheme()
        {
            Color color = ThemeProvider.Theme.Colors.BackgroundPrimary;
            double brightness =
                (color.R * 299D + color.G * 587D + color.B * 114D) / 1000D;

            return brightness < 128D;
        }
    }
}
