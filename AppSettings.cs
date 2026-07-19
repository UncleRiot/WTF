using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;

namespace WTF
{
    public enum AppLayout
    {
        WindowsDefault,
        WindowsLightMode,
        WindowsDarkMode
    }

    public sealed class AppSettings
    {
        private static readonly string SettingsDirectoryPath = System.AppContext.BaseDirectory;

        private static readonly string SettingsFilePath = System.IO.Path.Combine(
            SettingsDirectoryPath,
            "settings.json");

        public bool ShowFilesInTree { get; set; }
        public bool SkipReparsePoints { get; set; } = true;
        public bool ShowPartitionPanel { get; set; } = true;
        public int PartitionFillColorLightArgb { get; set; } = unchecked((int)0xFF32CD32);
        public int PartitionFillBrightnessLightPercent { get; set; } = 100;
        public int PartitionFillColorDarkArgb { get; set; } = unchecked((int)0xFF32CD32);
        public int PartitionFillBrightnessDarkPercent { get; set; } = 100;
        public int BarChartBarHeight { get; set; } = 14;
        public bool ShowElevationPromptOnStartup { get; set; } = true;
        public bool StartElevatedOnStartup { get; set; }
        public bool ShellContextMenuEnabled { get; set; }
        public List<string> ExcludedPaths { get; set; } = new List<string>();
        public bool EntryColumnNameVisible { get; set; } = true;
        public bool EntryColumnSizeVisible { get; set; } = true;
        public bool EntryColumnPercentVisible { get; set; } = true;
        public bool EntryColumnPathVisible { get; set; } = true;
        public TreeSortMode TreeSortMode { get; set; } = TreeSortMode.SizeDescending;
        public AppLayout Layout { get; set; } = AppLayout.WindowsDefault;
        public ViewMode SelectedViewMode { get; set; } = ViewMode.Table;
        public string LanguageCode { get; set; } = LocalizationService.EnglishLanguageCode;
        public bool SaveScanHistory { get; set; }
        public string ScanHistoryDatabasePath { get; set; } = ScanHistoryService.DefaultDatabasePath;
        public int ScanHistoryMaximumScansPerPath { get; set; } = 30;
        public AppLogLevel LogLevel { get; set; } = AppLogLevel.Normal;
        public bool AutoSaveLog { get; set; }
        public int MaximumLogFileSizeMb { get; set; } = 4;

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

        public bool HasStorageHistoryWindowBounds { get; set; }
        public int StorageHistoryWindowLeft { get; set; }
        public int StorageHistoryWindowTop { get; set; }
        public int StorageHistoryWindowWidth { get; set; }
        public int StorageHistoryWindowHeight { get; set; }
        public int StorageHistoryGradientIntensityPercent { get; set; } = 55;

        public bool HasToolStripLayout { get; set; }
        public int ToolStripLayoutVersion { get; set; }
        public int ToolStripMainLeft { get; set; }
        public int ToolStripMainTop { get; set; }
        public int ToolStripViewModeLeft { get; set; }
        public int ToolStripViewModeTop { get; set; }
        public int ToolStripExportLeft { get; set; }
        public int ToolStripExportTop { get; set; }
        public int ToolStripFeaturesLeft { get; set; }
        public int ToolStripFeaturesTop { get; set; }

        public bool HasSplitterLayout { get; set; }
        public int SplitContainerMainDistance { get; set; }
        public int SplitContainerLeftDistance { get; set; }

        public bool HasColumnLayout { get; set; }
        public bool HasEntryColumnLayout { get; set; }
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

                settings = settings ?? new AppSettings();
                settings.LanguageCode = LocalizationService.NormalizeLanguageCode(settings.LanguageCode);
                settings.StorageHistoryGradientIntensityPercent = Math.Max(
                    0,
                    Math.Min(100, settings.StorageHistoryGradientIntensityPercent));
                settings.PartitionFillBrightnessLightPercent = Math.Max(
                    0,
                    Math.Min(200, settings.PartitionFillBrightnessLightPercent));
                settings.PartitionFillBrightnessDarkPercent = Math.Max(
                    0,
                    Math.Min(200, settings.PartitionFillBrightnessDarkPercent));
                settings.BarChartBarHeight = Math.Max(
                    5,
                    Math.Min(30, settings.BarChartBarHeight));
                settings.ScanHistoryDatabasePath = ScanHistoryService.NormalizeDatabasePath(
                    settings.ScanHistoryDatabasePath);
                settings.ScanHistoryMaximumScansPerPath = Math.Max(
                    1,
                    settings.ScanHistoryMaximumScansPerPath);
                settings.MaximumLogFileSizeMb = Math.Max(
                    1,
                    settings.MaximumLogFileSizeMb);
                ScanHistoryService.ConfigureDatabasePath(settings.ScanHistoryDatabasePath);
                ScanHistoryService.ConfigureRetention(
                    settings.ScanHistoryMaximumScansPerPath);

                return settings;
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
