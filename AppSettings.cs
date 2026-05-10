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
            System.AppContext.BaseDirectory,
            "Settings");

        private static readonly string SettingsFilePath = System.IO.Path.Combine(
            SettingsDirectoryPath,
            "settings.json");

        public bool ShowFilesInTree { get; set; }
        public bool SkipReparsePoints { get; set; } = true;
        public bool ShowPartitionPanel { get; set; } = true;
        public bool ShowElevationPromptOnStartup { get; set; } = true;
        public bool StartElevatedOnStartup { get; set; }
        public AppLayout Layout { get; set; } = AppLayout.Modern;
        public ViewMode SelectedViewMode { get; set; } = ViewMode.Table;

        public bool ExportPath { get; set; } = true;
        public bool ExportSizeGb { get; set; } = true;
        public bool ExportSizeMb { get; set; } = true;
        public int? ExportMaxDepth { get; set; }

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
        public int ToolStripExportLeft { get; set; }
        public int ToolStripExportTop { get; set; }

        public bool HasSplitterLayout { get; set; }
        public int SplitContainerMainDistance { get; set; }
        public int SplitContainerLeftDistance { get; set; }

        public bool HasColumnLayout { get; set; }
        public int PartitionColumnNameWidth { get; set; }
        public int PartitionColumnSizeWidth { get; set; }
        public int PartitionColumnFreeWidth { get; set; }
        public int PartitionColumnFreePercentWidth { get; set; }
        public int EntryColumnNameWidth { get; set; }
        public int EntryColumnSizeWidth { get; set; }
        public int EntryColumnSizeBytesWidth { get; set; }
        public int EntryColumnPercentWidth { get; set; }
        public int EntryColumnPathWidth { get; set; }

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