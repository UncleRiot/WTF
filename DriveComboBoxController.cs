using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WTF
{
    public sealed class DriveComboBoxController
    {
        private readonly ToolStripComboBox _toolStripComboBoxDrives;
        private readonly ShellIconService _shellIconService;
        private readonly Action<string> _updateStatusStripForDrive;

        public DriveComboBoxController(
            ToolStripComboBox toolStripComboBoxDrives,
            ShellIconService shellIconService,
            Action<string> updateStatusStripForDrive)
        {
            _toolStripComboBoxDrives = toolStripComboBoxDrives;
            _shellIconService = shellIconService;
            _updateStatusStripForDrive = updateStatusStripForDrive;
        }

        public void Configure()
        {
            if (_toolStripComboBoxDrives == null)
                return;

            _toolStripComboBoxDrives.DropDownStyle = ComboBoxStyle.DropDownList;
            _toolStripComboBoxDrives.ComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            _toolStripComboBoxDrives.ComboBox.ItemHeight = Math.Max(20, _toolStripComboBoxDrives.ComboBox.ItemHeight);
            _toolStripComboBoxDrives.ComboBox.DrawItem -= toolStripComboBoxDrives_DrawItem;
            _toolStripComboBoxDrives.ComboBox.DrawItem += toolStripComboBoxDrives_DrawItem;
        }

        public void LoadDrives()
        {
            if (_toolStripComboBoxDrives == null)
                return;

            List<DriveItem> drives = GetReadyDrives();

            _toolStripComboBoxDrives.DropDownStyle = ComboBoxStyle.DropDownList;
            _toolStripComboBoxDrives.Items.Clear();

            foreach (DriveItem driveItem in drives)
            {
                _toolStripComboBoxDrives.Items.Add(driveItem);
            }

            if (_toolStripComboBoxDrives.Items.Count > 0)
            {
                _toolStripComboBoxDrives.SelectedIndex = 0;
                _updateStatusStripForDrive?.Invoke(((DriveItem)_toolStripComboBoxDrives.SelectedItem).RootPath);
            }
        }

        public string GetSelectedScanPath()
        {
            if (_toolStripComboBoxDrives == null)
                return string.Empty;

            if (_toolStripComboBoxDrives.SelectedItem is DriveItem driveItem)
                return driveItem.RootPath;

            return _toolStripComboBoxDrives.Text == null
                ? string.Empty
                : _toolStripComboBoxDrives.Text.Trim();
        }

        public void AddOrSelectPath(string path)
        {
            if (_toolStripComboBoxDrives == null)
                return;

            if (string.IsNullOrWhiteSpace(path))
                return;

            string fullPath = Path.GetFullPath(path);

            foreach (object item in _toolStripComboBoxDrives.Items)
            {
                if (item is DriveItem driveItem &&
                    string.Equals(driveItem.RootPath, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    _toolStripComboBoxDrives.SelectedItem = item;
                    return;
                }

                if (item is string itemPath &&
                    string.Equals(itemPath, fullPath, StringComparison.OrdinalIgnoreCase))
                {
                    _toolStripComboBoxDrives.SelectedItem = item;
                    return;
                }
            }

            _toolStripComboBoxDrives.Items.Add(fullPath);
            _toolStripComboBoxDrives.SelectedItem = fullPath;
        }

        public void SetEnabled(bool enabled)
        {
            if (_toolStripComboBoxDrives == null)
                return;

            _toolStripComboBoxDrives.Enabled = enabled;
        }

        private List<DriveItem> GetReadyDrives()
        {
            List<DriveItem> drives = new List<DriveItem>();

            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (!driveInfo.IsReady)
                    continue;

                string label = string.IsNullOrWhiteSpace(driveInfo.VolumeLabel)
                    ? LocalizationService.GetText("Drive.LocalDisk")
                    : driveInfo.VolumeLabel;

                drives.Add(new DriveItem
                {
                    RootPath = driveInfo.RootDirectory.FullName,
                    DisplayName = LocalizationService.Format("Drive.Display", label, driveInfo.RootDirectory.FullName)
                });
            }

            return drives;
        }

        private void toolStripComboBoxDrives_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index < 0)
                return;

            ComboBox comboBox = (ComboBox)sender;
            object item = comboBox.Items[e.Index];

            string text = item == null
                ? string.Empty
                : item.ToString();

            string iconPath = GetDriveComboBoxItemIconPath(item);

            Color textColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
                ? SystemColors.HighlightText
                : comboBox.ForeColor;

            int iconLeft = e.Bounds.Left + 3;
            int iconTop = e.Bounds.Top + Math.Max(0, (e.Bounds.Height - 16) / 2);

            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                using Bitmap icon = _shellIconService.GetSmallSystemIcon(iconPath);
                e.Graphics.DrawImage(icon, iconLeft, iconTop, 16, 16);
            }

            Rectangle textBounds = new Rectangle(
                e.Bounds.Left + 24,
                e.Bounds.Top,
                Math.Max(0, e.Bounds.Width - 26),
                e.Bounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                comboBox.Font,
                textBounds,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.DrawFocusRectangle();
        }

        private string GetDriveComboBoxItemIconPath(object item)
        {
            if (item is DriveItem driveItem)
                return driveItem.RootPath;

            if (item is string path)
            {
                if (Directory.Exists(path))
                    return path;

                if (File.Exists(path))
                    return path;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        }
    }
}
