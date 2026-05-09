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