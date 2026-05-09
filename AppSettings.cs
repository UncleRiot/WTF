using System;
using System.IO;
using System.Text.Json;

namespace WTF
{
    public enum AppLayout
    {
        Modern,
        WindowsDefault,
        WindowsLightMode,
        WindowsDarkMode
    }

    public sealed class AppSettings
    {
        private static readonly string SettingsDirectoryPath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "WTF");

        private static readonly string SettingsFilePath = System.IO.Path.Combine(
            SettingsDirectoryPath,
            "settings.json");

        public bool ShowFilesInTree { get; set; }
        public bool SkipReparsePoints { get; set; } = true;
        public bool ShowPartitionPanel { get; set; } = true;
        public AppLayout Layout { get; set; } = AppLayout.Modern;
        public ViewMode SelectedViewMode { get; set; } = ViewMode.Table;

        public bool HasMainWindowBounds { get; set; }
        public int MainWindowLeft { get; set; }
        public int MainWindowTop { get; set; }
        public int MainWindowWidth { get; set; }
        public int MainWindowHeight { get; set; }
        public bool MainWindowMaximized { get; set; }

        public bool HasToolStripLayout { get; set; }
        public int ToolStripMainLeft { get; set; }
        public int ToolStripMainTop { get; set; }
        public int ToolStripViewModeLeft { get; set; }
        public int ToolStripViewModeTop { get; set; }

        public bool HasSplitterLayout { get; set; }
        public int SplitContainerMainDistance { get; set; }
        public int SplitContainerLeftDistance { get; set; }

        public static AppSettings Load()
        {
            if (!System.IO.File.Exists(SettingsFilePath))
            {
                return new AppSettings();
            }

            try
            {
                string json = System.IO.File.ReadAllText(SettingsFilePath);
                AppSettings settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);

                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save()
        {
            System.IO.Directory.CreateDirectory(SettingsDirectoryPath);

            System.Text.Json.JsonSerializerOptions options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = System.Text.Json.JsonSerializer.Serialize(this, options);
            System.IO.File.WriteAllText(SettingsFilePath, json);
        }
    }
}