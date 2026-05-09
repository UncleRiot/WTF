using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace WTF
{
    public static class ModernTheme
    {
        public static readonly Color WindowBackColor = Color.FromArgb(24, 38, 52);
        public static readonly Color TitleBarBackColor = Color.FromArgb(18, 30, 43);
        public static readonly Color ControlBackColor = Color.FromArgb(32, 49, 66);
        public static readonly Color ControlHoverBackColor = Color.FromArgb(43, 65, 86);
        public static readonly Color AccentColor = Color.FromArgb(102, 192, 244);
        public static readonly Color TextColor = Color.FromArgb(235, 235, 235);
        public static readonly Color MutedTextColor = Color.FromArgb(170, 184, 196);
        public static readonly Color DarkTextColor = Color.FromArgb(18, 30, 43);
        public static readonly Color WindowBorderColor = Color.FromArgb(102, 192, 244);
        public static readonly Color CloseButtonHoverColor = Color.FromArgb(196, 43, 28);

        public const string FontFamilyName = "Segoe UI";
        public const float DefaultFontSize = 9F;
        public const float TitleFontSize = 9.5F;
        public const int TitleBarHeight = 32;
        public const int WindowBorderWidth = 1;

        public static readonly Font DefaultFont = new Font(FontFamilyName, DefaultFontSize, FontStyle.Regular);
        public static readonly Size TitleBarButtonSize = new Size(44, TitleBarHeight);
        public static readonly int TitleBarTextLeft = 12;

        private static readonly ConditionalWeakTable<ListView, ListViewStyleColors> ListViewStyleColorsByControl = new ConditionalWeakTable<ListView, ListViewStyleColors>();

        public static void ApplyButtonStyle(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = AccentColor;
            button.BackColor = ControlBackColor;
            button.ForeColor = TextColor;
            button.Font = DefaultFont;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;

            button.MouseEnter -= button_MouseEnter;
            button.MouseLeave -= button_MouseLeave;
            button.MouseEnter += button_MouseEnter;
            button.MouseLeave += button_MouseLeave;
        }
        public static readonly Color[] ChartColors =
{
    Color.FromArgb(102, 192, 244),
    Color.FromArgb(244, 159, 67),
    Color.FromArgb(120, 220, 140),
    Color.FromArgb(190, 140, 255),
    Color.FromArgb(255, 120, 120),
    Color.FromArgb(120, 210, 210),
    Color.FromArgb(255, 210, 90),
    Color.FromArgb(170, 190, 255),
    Color.FromArgb(210, 160, 120),
    Color.FromArgb(150, 220, 180),
    Color.FromArgb(220, 150, 210)
};

        public static void ApplyTextBoxStyle(TextBox textBox)
        {
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = ControlBackColor;
            textBox.ForeColor = TextColor;
            textBox.Font = DefaultFont;
        }

        public static void ApplyComboBoxStyle(ComboBox comboBox)
        {
            comboBox.BackColor = ControlBackColor;
            comboBox.ForeColor = TextColor;
            comboBox.Font = DefaultFont;
            comboBox.FlatStyle = FlatStyle.Flat;
        }

        public static void ApplyLabelStyle(Label label)
        {
            label.BackColor = Color.Transparent;
            label.ForeColor = TextColor;
            label.Font = DefaultFont;
        }

        public static void ApplyCheckBoxStyle(CheckBox checkBox)
        {
            checkBox.BackColor = WindowBackColor;
            checkBox.ForeColor = TextColor;
            checkBox.Font = DefaultFont;
            checkBox.UseVisualStyleBackColor = false;
        }

        public static void ApplyMenuStyle(MenuStrip menuStrip)
        {
            menuStrip.BackColor = TitleBarBackColor;
            menuStrip.ForeColor = TextColor;
            menuStrip.Font = DefaultFont;
            menuStrip.RenderMode = ToolStripRenderMode.Professional;
            menuStrip.Renderer = new ToolStripProfessionalRenderer(new ModernColorTable());

            foreach (ToolStripMenuItem item in menuStrip.Items)
            {
                ApplyMenuItemStyle(item);
            }
        }

        public static void ApplyToolStripStyle(ToolStrip toolStrip)
        {
            toolStrip.BackColor = ControlBackColor;
            toolStrip.ForeColor = TextColor;
            toolStrip.Font = DefaultFont;
            toolStrip.RenderMode = ToolStripRenderMode.Professional;
            toolStrip.Renderer = new ToolStripProfessionalRenderer(new ModernColorTable());
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        }

        public static void ApplyDataGridViewStyle(DataGridView dataGridView)
        {
            dataGridView.BackgroundColor = WindowBackColor;
            dataGridView.GridColor = ControlHoverBackColor;
            dataGridView.BorderStyle = BorderStyle.FixedSingle;
            dataGridView.EnableHeadersVisualStyles = false;
            dataGridView.DefaultCellStyle.BackColor = ControlBackColor;
            dataGridView.DefaultCellStyle.ForeColor = TextColor;
            dataGridView.DefaultCellStyle.SelectionBackColor = AccentColor;
            dataGridView.DefaultCellStyle.SelectionForeColor = DarkTextColor;
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = TitleBarBackColor;
            dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = TextColor;
            dataGridView.RowHeadersDefaultCellStyle.BackColor = TitleBarBackColor;
            dataGridView.RowHeadersDefaultCellStyle.ForeColor = TextColor;
            dataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(35, 55, 75);
            dataGridView.Font = DefaultFont;
        }

        public static void ApplyTreeViewStyle(TreeView treeView)
        {
            treeView.BackColor = ControlBackColor;
            treeView.ForeColor = TextColor;
            treeView.BorderStyle = BorderStyle.FixedSingle;
            treeView.Font = DefaultFont;
        }

        public static void ApplyListViewStyle(ListView listView)
        {
            ApplyListViewStyle(
                listView,
                TitleBarBackColor,
                ControlBackColor,
                TextColor,
                AccentColor,
                DarkTextColor,
                BorderStyle.FixedSingle,
                DefaultFont);
        }

        public static void ApplyListViewStyle(
            ListView listView,
            Color headerBackColor,
            Color rowBackColor,
            Color foreColor,
            Color selectedBackColor,
            Color selectedForeColor,
            BorderStyle borderStyle,
            Font font)
        {
            listView.BackColor = rowBackColor;
            listView.ForeColor = foreColor;
            listView.BorderStyle = borderStyle;
            listView.Font = font;
            listView.OwnerDraw = true;

            ListViewStyleColorsByControl.Remove(listView);
            ListViewStyleColorsByControl.Add(listView, new ListViewStyleColors(
                headerBackColor,
                rowBackColor,
                foreColor,
                selectedBackColor,
                selectedForeColor,
                font));

            listView.DrawColumnHeader -= listView_DrawColumnHeader;
            listView.DrawItem -= listView_DrawItem;
            listView.DrawSubItem -= listView_DrawSubItem;

            listView.DrawColumnHeader += listView_DrawColumnHeader;
            listView.DrawItem += listView_DrawItem;
            listView.DrawSubItem += listView_DrawSubItem;

            listView.Invalidate();
        }

        private static void RemoveListViewOwnerDraw(ListView listView)
        {
            listView.DrawColumnHeader -= listView_DrawColumnHeader;
            listView.DrawItem -= listView_DrawItem;
            listView.DrawSubItem -= listView_DrawSubItem;
            ListViewStyleColorsByControl.Remove(listView);
            listView.OwnerDraw = false;
        }

        public static void ApplyWindowsDefaultListViewStyle(ListView listView, Color backColor, Color foreColor, Font font)
        {
            RemoveListViewOwnerDraw(listView);
            listView.BackColor = backColor;
            listView.ForeColor = foreColor;
            listView.BorderStyle = BorderStyle.Fixed3D;
            listView.Font = font;
            listView.Invalidate();
        }

        private static void button_MouseEnter(object sender, System.EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackColor = ControlHoverBackColor;
            }
        }

        private static void button_MouseLeave(object sender, System.EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackColor = ControlBackColor;
            }
        }

        private static void listView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (sender is not ListView listView)
                return;

            if (!ListViewStyleColorsByControl.TryGetValue(listView, out ListViewStyleColors colors))
                return;

            using SolidBrush backgroundBrush = new SolidBrush(colors.HeaderBackColor);
            using Pen borderPen = new Pen(ControlHoverBackColor);

            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
            e.Graphics.DrawRectangle(borderPen, e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 1, e.Bounds.Height - 1);

            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

            if (e.Header.TextAlign == HorizontalAlignment.Right)
            {
                flags |= TextFormatFlags.Right;
            }
            else if (e.Header.TextAlign == HorizontalAlignment.Center)
            {
                flags |= TextFormatFlags.HorizontalCenter;
            }
            else
            {
                flags |= TextFormatFlags.Left;
            }

            Rectangle textBounds = new Rectangle(
                e.Bounds.Left + 6,
                e.Bounds.Top,
                e.Bounds.Width - 12,
                e.Bounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                e.Header.Text,
                colors.Font,
                textBounds,
                colors.ForeColor,
                flags);
        }

        private static void listView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
        }

        private static void listView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (sender is not ListView listView)
                return;

            if (!ListViewStyleColorsByControl.TryGetValue(listView, out ListViewStyleColors colors))
                return;

            bool selected = e.Item.Selected;

            using SolidBrush backgroundBrush = new SolidBrush(selected ? colors.SelectedBackColor : colors.RowBackColor);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

            Color textColor = selected ? colors.SelectedForeColor : colors.ForeColor;

            Rectangle textBounds = new Rectangle(
                e.Bounds.Left + 6,
                e.Bounds.Top,
                e.Bounds.Width - 12,
                e.Bounds.Height);

            if (e.ColumnIndex == 0 && listView.SmallImageList != null && e.Item.ImageIndex >= 0 && e.Item.ImageIndex < listView.SmallImageList.Images.Count)
            {
                Image image = listView.SmallImageList.Images[e.Item.ImageIndex];

                e.Graphics.DrawImage(
                    image,
                    e.Bounds.Left + 4,
                    e.Bounds.Top + (e.Bounds.Height - image.Height) / 2,
                    image.Width,
                    image.Height);

                textBounds = new Rectangle(
                    e.Bounds.Left + 24,
                    e.Bounds.Top,
                    e.Bounds.Width - 30,
                    e.Bounds.Height);
            }

            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

            if (e.Header.TextAlign == HorizontalAlignment.Right)
            {
                flags |= TextFormatFlags.Right;
            }
            else if (e.Header.TextAlign == HorizontalAlignment.Center)
            {
                flags |= TextFormatFlags.HorizontalCenter;
            }
            else
            {
                flags |= TextFormatFlags.Left;
            }

            TextRenderer.DrawText(
                e.Graphics,
                e.SubItem.Text,
                colors.Font,
                textBounds,
                textColor,
                flags);
        }

        private static void ApplyMenuItemStyle(ToolStripMenuItem item)
        {
            item.BackColor = TitleBarBackColor;
            item.ForeColor = TextColor;
            item.Padding = new Padding(4, 0, 4, 0);

            if (item.DropDown is ToolStripDropDownMenu dropDownMenu)
            {
                dropDownMenu.ShowImageMargin = false;
                dropDownMenu.ShowCheckMargin = false;
            }

            foreach (ToolStripItem child in item.DropDownItems)
            {
                child.BackColor = TitleBarBackColor;
                child.ForeColor = TextColor;
                child.Padding = new Padding(4, 0, 4, 0);

                if (child is ToolStripMenuItem childMenuItem)
                {
                    ApplyMenuItemStyle(childMenuItem);
                }
            }
        }

        private sealed class ListViewStyleColors
        {
            public ListViewStyleColors(
                Color headerBackColor,
                Color rowBackColor,
                Color foreColor,
                Color selectedBackColor,
                Color selectedForeColor,
                Font font)
            {
                HeaderBackColor = headerBackColor;
                RowBackColor = rowBackColor;
                ForeColor = foreColor;
                SelectedBackColor = selectedBackColor;
                SelectedForeColor = selectedForeColor;
                Font = font;
            }

            public Color HeaderBackColor { get; }
            public Color RowBackColor { get; }
            public Color ForeColor { get; }
            public Color SelectedBackColor { get; }
            public Color SelectedForeColor { get; }
            public Font Font { get; }
        }

        private sealed class ModernColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => ControlHoverBackColor;
            public override Color MenuItemBorder => AccentColor;
            public override Color MenuItemSelectedGradientBegin => ControlHoverBackColor;
            public override Color MenuItemSelectedGradientEnd => ControlHoverBackColor;
            public override Color MenuItemPressedGradientBegin => ControlBackColor;
            public override Color MenuItemPressedGradientEnd => ControlBackColor;
            public override Color ToolStripDropDownBackground => TitleBarBackColor;
            public override Color ImageMarginGradientBegin => TitleBarBackColor;
            public override Color ImageMarginGradientMiddle => TitleBarBackColor;
            public override Color ImageMarginGradientEnd => TitleBarBackColor;
            public override Color ToolStripBorder => TitleBarBackColor;
            public override Color ToolStripGradientBegin => ControlBackColor;
            public override Color ToolStripGradientMiddle => ControlBackColor;
            public override Color ToolStripGradientEnd => ControlBackColor;
            public override Color ButtonSelectedHighlight => AccentColor;
            public override Color ButtonSelectedGradientBegin => ControlHoverBackColor;
            public override Color ButtonSelectedGradientEnd => ControlHoverBackColor;
            public override Color ButtonPressedGradientBegin => AccentColor;
            public override Color ButtonPressedGradientEnd => AccentColor;
        }
    }
}