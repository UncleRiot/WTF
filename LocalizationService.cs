using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WTF
{
    public static class LocalizationService
    {
        public const string GermanLanguageCode = "de";
        public const string EnglishLanguageCode = "en";

        private static readonly object SyncRoot = new object();
        private static Dictionary<string, string> _texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string CurrentLanguageCode { get; private set; } = GermanLanguageCode;

        public static void Initialize(string languageCode)
        {
            EnsureLanguageFiles();
            Load(languageCode);
        }

        public static void Load(string languageCode)
        {
            EnsureLanguageFiles();

            string normalizedLanguageCode = NormalizeLanguageCode(languageCode);
            Dictionary<string, string> fallbackTexts = CreateGermanTexts();
            Dictionary<string, string> loadedTexts = LoadLanguageFile(normalizedLanguageCode);

            foreach (KeyValuePair<string, string> fallbackText in fallbackTexts)
            {
                if (!loadedTexts.ContainsKey(fallbackText.Key))
                {
                    loadedTexts[fallbackText.Key] = fallbackText.Value;
                }
            }

            lock (SyncRoot)
            {
                CurrentLanguageCode = normalizedLanguageCode;
                _texts = loadedTexts;
            }
        }

        public static string GetText(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            lock (SyncRoot)
            {
                if (_texts.TryGetValue(key, out string value))
                {
                    return value ?? string.Empty;
                }
            }

            Dictionary<string, string> germanTexts = CreateGermanTexts();

            if (germanTexts.TryGetValue(key, out string fallbackValue))
            {
                return fallbackValue ?? string.Empty;
            }

            return key;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(GetText(key), args);
        }

        public static string NormalizeLanguageCode(string languageCode)
        {
            if (string.Equals(languageCode, EnglishLanguageCode, StringComparison.OrdinalIgnoreCase))
                return EnglishLanguageCode;

            return GermanLanguageCode;
        }

        public static string GetLanguageFilePath(string languageCode)
        {
            return Path.Combine(GetSettingsDirectoryPath(), "lang_" + NormalizeLanguageCode(languageCode) + ".json");
        }

        public static string GetSettingsDirectoryPath()
        {
            return Path.Combine(AppContext.BaseDirectory, "Settings");
        }

        public static void EnsureLanguageFiles()
        {
            try
            {
                Directory.CreateDirectory(GetSettingsDirectoryPath());
                EnsureLanguageFile(GermanLanguageCode, CreateGermanTexts());
                EnsureLanguageFile(EnglishLanguageCode, CreateEnglishTexts());
            }
            catch
            {
            }
        }

        private static void EnsureLanguageFile(string languageCode, Dictionary<string, string> defaultTexts)
        {
            string languageFilePath = GetLanguageFilePath(languageCode);
            Dictionary<string, string> fileTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(languageFilePath))
            {
                try
                {
                    string json = File.ReadAllText(languageFilePath);
                    Dictionary<string, string> loadedTexts = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (loadedTexts != null)
                    {
                        foreach (KeyValuePair<string, string> loadedText in loadedTexts)
                        {
                            if (!string.IsNullOrWhiteSpace(loadedText.Key))
                            {
                                fileTexts[loadedText.Key] = loadedText.Value ?? string.Empty;
                            }
                        }
                    }
                }
                catch
                {
                    fileTexts.Clear();
                }
            }

            bool changed = false;

            foreach (KeyValuePair<string, string> defaultText in defaultTexts)
            {
                if (!fileTexts.ContainsKey(defaultText.Key))
                {
                    fileTexts[defaultText.Key] = defaultText.Value;
                    changed = true;
                }
            }

            if (!File.Exists(languageFilePath) || changed)
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                File.WriteAllText(languageFilePath, JsonSerializer.Serialize(fileTexts, options));
            }
        }

        private static Dictionary<string, string> LoadLanguageFile(string languageCode)
        {
            string languageFilePath = GetLanguageFilePath(languageCode);

            try
            {
                string json = File.ReadAllText(languageFilePath);
                Dictionary<string, string> loadedTexts = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (loadedTexts != null)
                {
                    return new Dictionary<string, string>(loadedTexts, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch
            {
            }

            return CreateGermanTexts();
        }

        private static Dictionary<string, string> CreateGermanTexts()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["App.Title"] = "WTF - Where’s The Filespace",
                ["Common.OK"] = "OK",
                ["Common.Cancel"] = "Abbrechen",
                ["Common.Yes"] = "Ja",
                ["Common.No"] = "Nein",
                ["Common.Close"] = "Schließen",
                ["Common.Ready"] = "Bereit",
                ["Common.Unknown"] = "Unbekannt",
                ["Common.Name"] = "Name",
                ["Common.Size"] = "Größe",
                ["Common.Free"] = "Frei",
                ["Common.FreePercent"] = "% Frei",
                ["Common.Bytes"] = "Bytes",
                ["Common.Percent"] = "Anteil",
                ["Chart.TableUsage"] = "Belegung",
                ["Common.Path"] = "Pfad",
                ["Common.Folder"] = "Ordner",
                ["Common.Folders"] = "Ordner",
                ["Common.Files"] = "Dateien",
                ["Common.Information"] = "Information",
                ["Common.Warning"] = "Warnung",
                ["Common.Error"] = "Fehler",
                ["Common.General"] = "Allgemein",
                ["Menu.File"] = "Datei",
                ["Menu.ExportCsv"] = "Export CSV",
                ["Menu.Settings"] = "Einstellungen",
                ["Menu.Exit"] = "Beenden",
                ["Menu.Help"] = "Hilfe",
                ["Menu.About"] = "Über",
                ["Toolbar.Drive"] = "Laufwerk:",
                ["Toolbar.Open"] = "Öffnen",
                ["Toolbar.ScanStart"] = "Scan starten",
                ["Toolbar.ScanCancel"] = "Scan abbrechen",
                ["Toolbar.SelectFolderAndScan"] = "Ordner auswählen und scannen",
                ["Toolbar.Table"] = "▦ Tabelle",
                ["Toolbar.PieChart"] = "◔ Pie-Chart",
                ["Toolbar.BarChart"] = "▥ Balkenchart",
                ["Toolbar.Export"] = "Export",
                ["Toolbar.ExportCsv"] = "CSV exportieren",
                ["Context.OpenInExplorer"] = "Im Explorer öffnen",
                ["Context.Export"] = "Export",
                ["Context.CopyToClipboard"] = "In Zwischenablage kopieren",
                ["Dialog.SelectFolder"] = "Ordner zum Scannen auswählen",
                ["Message.NoPathSelected"] = "Kein Pfad ausgewählt.",
                ["Message.PathNotFoundPrefix"] = "Pfad nicht gefunden: ",
                ["Message.SettingsSaveFailedPrefix"] = "Einstellungen konnten nicht gespeichert werden: ",
                ["Message.SettingsSaveFailed"] = "Die Einstellungen konnten nicht gespeichert werden.",
                ["Status.FreeSpace"] = "Freier Speicherplatz {0}: {1} (von {2}), Clustersize: {3}",
                ["Status.ScanCacheSave"] = "{0} | {1} | Ordner: {2} | Dateien: {3}",
                ["Status.CacheVerification"] = "Cache geladen - überprüfe Änderungen: {0} | {1} | Ordner: {2} | Dateien: {3}",
                ["Status.FastScan"] = "Schnellscan: {0} | {1} | Ordner: {2} | Dateien: {3}",
                ["Status.MftFastScanRunning"] = "NTFS-MFT-Schnellscan läuft...",
                ["Status.MftUnavailableNtQuery"] = "MFT-Schnellscan nicht verfügbar - NT-API-Schnellscan läuft...",
                ["Status.NtQueryUnavailableNormal"] = "NT-API-Schnellscan nicht verfügbar - normaler Scan läuft...",
                ["Status.NtQueryRunning"] = "NT-API-Schnellscan läuft...",
                ["Status.ScanCanceled"] = "Scan abgebrochen",
                ["Status.TitleCacheVerification"] = "Cache geladen / überprüfe Änderungen",
                ["Status.ScanCompletedTitle"] = "Scan: 100% / abgeschlossen",
                ["Status.ExportCopied"] = "Export in Zwischenablage kopiert: ",
                ["Status.ExportSaved"] = "Export gespeichert: ",
                ["Status.CacheSave"] = "Scan abgeschlossen, Cache wird gespeichert...",
                ["Alert.Scan"] = "Scan",
                ["Alert.ToolTipInformation"] = "Informationen anzeigen",
                ["Alert.ToolTipWarning"] = "Warnungen anzeigen",
                ["Alert.ToolTipError"] = "Fehler anzeigen",
                ["Alert.MftUnavailable"] = "MFT-Schnellscan nicht verfügbar: {0}",
                ["Alert.NtQueryUnavailable"] = "NT-API-Schnellscan nicht verfügbar: {0}",
                ["Alert.ExpectedSystemDirectorySingle"] = "1 Systemordner wurde erwartungsgemäß übersprungen.",
                ["Alert.ExpectedSystemDirectoryMultiple"] = "{0} Systemordner wurden erwartungsgemäß übersprungen.",
                ["Alert.SkippedDirectorySingle"] = "1 Ordner konnte nicht gelesen werden.",
                ["Alert.SkippedDirectoryMultiple"] = "{0} Ordner konnten nicht gelesen werden.",
                ["Alert.UnknownSkippedDirectories"] = "{0} weitere Ordner konnten nicht gelesen werden. Details wurden nicht erfasst.",
                ["Alert.Reason"] = "Grund: {0}",
                ["Alert.UnknownReason"] = "Unbekannt",
                ["Alert.Win32Error"] = "Win32-Fehler {0}: {1}",
                ["Alert.NtStatusOpen"] = "Ordner konnte nicht geöffnet werden. NTSTATUS: {0}",
                ["Alert.NtStatusRead"] = "Ordner konnte nicht gelesen werden. NTSTATUS: {0}",
                ["Alert.NtQueryRootOpenFailed"] = "NT-API-Schnellscan konnte den Root-Pfad nicht öffnen: {0}",
                ["Alert.NtQueryRootReadFailed"] = "NT-API-Schnellscan konnte den Root-Pfad nicht lesen: {0}",
                ["Alert.InvalidNtfsDrive"] = "Kein gültiges NTFS-Laufwerk.",
                ["Status.MftFastScanCompleted"] = "MFT-Schnellscan abgeschlossen",
                ["Settings.Title"] = "Einstellungen",
                ["Settings.General"] = "Allgemein",
                ["Settings.Export"] = "Export",
                ["Settings.Language"] = "Sprache:",
                ["Settings.LanguageGerman"] = "Deutsch",
                ["Settings.LanguageEnglish"] = "Englisch",
                ["Settings.ShowFilesInTree"] = "Dateien im Baum anzeigen",
                ["Settings.SkipReparsePoints"] = "Reparse Points / Junctions überspringen",
                ["Settings.ShowPartitionPanel"] = "Partitionsfenster anzeigen",
                ["Settings.StartElevated"] = "Starten mit erhöhten Rechten",
                ["Settings.ShowElevationPrompt"] = "Admin-Hinweis beim Start anzeigen",
                ["Settings.ShellContextMenu"] = "Explorer-Kontextmenüeintrag für Ordner und Laufwerke anzeigen",
                ["Settings.Layout"] = "Layout:",
                ["Settings.LayoutWindowsDefault"] = "Windows default",
                ["Settings.LayoutWindowsLight"] = "Windows light mode",
                ["Settings.LayoutWindowsDark"] = "Windows dark mode",
                ["Settings.ExportPath"] = "Path exportieren",
                ["Settings.ExportSizeGb"] = "Size (GB) exportieren",
                ["Settings.ExportSizeMb"] = "Size (MB) exportieren",
                ["Settings.ExportMaxDepth"] = "Maximale Ebenen/Tiefe:",
                ["Settings.ExportMaxDepthInvalid"] = "Die maximale Ebenen/Tiefe muss leer oder eine Zahl ab 0 sein.",
                ["Settings.ShellContextMenuFailed"] = "Der Explorer-Kontextmenüeintrag konnte nicht aktualisiert werden.",
                ["AlertHistory.Title"] = "Kurzprotokoll",
                ["AlertHistory.Type"] = "Typ",
                ["AlertHistory.Category"] = "Kategorie",
                ["AlertHistory.Message"] = "Meldung",
                ["AlertHistory.Details"] = "Details:",
                ["AlertHistory.CreatedAt"] = "Datum und Zeit",
                ["AlertHistory.Confirmed"] = "Bestätigt",
                ["AlertHistory.Confirm"] = "Bestätigen",
                ["AlertHistory.Delete"] = "Löschen",
                ["AlertHistory.ConfirmAll"] = "Alle bestätigen",
                ["AlertHistory.DeleteAll"] = "Alle löschen",
                ["About.Title"] = "Über WTF",
                ["About.VersionPrefix"] = "Version: ",
                ["About.UpdateChecking"] = "Update wird geprüft...",
                ["About.GitHubUnavailable"] = "GitHub nicht erreichbar",
                ["About.NoNewVersion"] = "Keine neue Version verfügbar",
                ["About.UpdateAvailable"] = "Update verfügbar: {0}",
                ["About.FreeText"] = "WTF ist kostenlos nutzbar.",
                ["About.SupportText"] = "Wenn dir dieses Tool hilft, kannst du die Entwicklung hier unterstützen:",
                ["Elevation.Title"] = "WTF",
                ["Elevation.Message"] = "Möchten Sie WTF mit erhöhten Rechten ausführen, um die\nScangeschwindigkeit und Genauigkeit zu steigern?",
                ["Elevation.DoNotShowAgain"] = "Diese Meldung nicht mehr anzeigen",
                ["Chart.NoData"] = "Keine Daten vorhanden.",
                ["Chart.Other"] = "Sonstige",
                ["Chart.TooltipDates"] = "Erstellt: {0}{1}Geändert: {2}{1}Letzter Zugriff: {3}",
                ["Chart.PieTooltip"] = "{0}{1}Erstellt: {2}{1}Geändert: {3}{1}Letzter Zugriff: {4}",
                ["Chart.ItemLabel"] = "{0} - {1} ({2:0.0} %)",
                ["Chart.Directory"] = "Directory",
                ["Chart.FilePrefix"] = "File:",
                ["Csv.FileFilter"] = "CSV files (*.csv)|*.csv",
                ["Csv.Path"] = "Path",
                ["Csv.Level"] = "Ebene",
                ["Csv.SizeGb"] = "Size (GB)",
                ["Csv.SizeMb"] = "Size (MB)",
                ["Csv.Root"] = "Root",
                ["Drive.LocalDisk"] = "Local Disk",
                ["Drive.Display"] = "{0} ({1})",
                ["Shell.ContextMenuText"] = "WTF: Start Size Scan",
                ["Status.ScanTitlePrefix"] = "Scan: "
            };
        }

        private static Dictionary<string, string> CreateEnglishTexts()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["App.Title"] = "WTF - Where’s The Filespace",
                ["Common.OK"] = "OK",
                ["Common.Cancel"] = "Cancel",
                ["Common.Yes"] = "Yes",
                ["Common.No"] = "No",
                ["Common.Close"] = "Close",
                ["Common.Ready"] = "Ready",
                ["Common.Unknown"] = "Unknown",
                ["Common.Name"] = "Name",
                ["Common.Size"] = "Size",
                ["Common.Free"] = "Free",
                ["Common.FreePercent"] = "% Free",
                ["Common.Bytes"] = "Bytes",
                ["Common.Percent"] = "Share",
                ["Chart.TableUsage"] = "Usage",
                ["Common.Path"] = "Path",
                ["Common.Folder"] = "Folder",
                ["Common.Folders"] = "Folders",
                ["Common.Files"] = "Files",
                ["Common.Information"] = "Information",
                ["Common.Warning"] = "Warning",
                ["Common.Error"] = "Error",
                ["Common.General"] = "General",
                ["Menu.File"] = "File",
                ["Menu.ExportCsv"] = "Export CSV",
                ["Menu.Settings"] = "Settings",
                ["Menu.Exit"] = "Exit",
                ["Menu.Help"] = "Help",
                ["Menu.About"] = "About",
                ["Toolbar.Drive"] = "Drive:",
                ["Toolbar.Open"] = "Open",
                ["Toolbar.ScanStart"] = "Start scan",
                ["Toolbar.ScanCancel"] = "Cancel scan",
                ["Toolbar.SelectFolderAndScan"] = "Select folder and scan",
                ["Toolbar.Table"] = "▦ Table",
                ["Toolbar.PieChart"] = "◔ Pie chart",
                ["Toolbar.BarChart"] = "▥ Bar chart",
                ["Toolbar.Export"] = "Export",
                ["Toolbar.ExportCsv"] = "Export CSV",
                ["Context.OpenInExplorer"] = "Open in Explorer",
                ["Context.Export"] = "Export",
                ["Context.CopyToClipboard"] = "Copy to clipboard",
                ["Dialog.SelectFolder"] = "Select folder to scan",
                ["Message.NoPathSelected"] = "No path selected.",
                ["Message.PathNotFoundPrefix"] = "Path not found: ",
                ["Message.SettingsSaveFailedPrefix"] = "Settings could not be saved: ",
                ["Message.SettingsSaveFailed"] = "The settings could not be saved.",
                ["Status.FreeSpace"] = "Free space {0}: {1} (of {2}), cluster size: {3}",
                ["Status.ScanCacheSave"] = "{0} | {1} | Folders: {2} | Files: {3}",
                ["Status.CacheVerification"] = "Cache loaded - verifying changes: {0} | {1} | Folders: {2} | Files: {3}",
                ["Status.FastScan"] = "Fast scan: {0} | {1} | Folders: {2} | Files: {3}",
                ["Status.MftFastScanRunning"] = "NTFS MFT fast scan is running...",
                ["Status.MftUnavailableNtQuery"] = "MFT fast scan unavailable - NT API fast scan is running...",
                ["Status.NtQueryUnavailableNormal"] = "NT API fast scan unavailable - normal scan is running...",
                ["Status.NtQueryRunning"] = "NT API fast scan is running...",
                ["Status.ScanCanceled"] = "Scan canceled",
                ["Status.TitleCacheVerification"] = "Cache loaded / verifying changes",
                ["Status.ScanCompletedTitle"] = "Scan: 100% / completed",
                ["Status.ExportCopied"] = "Export copied to clipboard: ",
                ["Status.ExportSaved"] = "Export saved: ",
                ["Status.CacheSave"] = "Scan completed, saving cache...",
                ["Alert.Scan"] = "Scan",
                ["Alert.ToolTipInformation"] = "Show information",
                ["Alert.ToolTipWarning"] = "Show warnings",
                ["Alert.ToolTipError"] = "Show errors",
                ["Alert.MftUnavailable"] = "MFT fast scan unavailable: {0}",
                ["Alert.NtQueryUnavailable"] = "NT API fast scan unavailable: {0}",
                ["Alert.ExpectedSystemDirectorySingle"] = "1 system folder was skipped as expected.",
                ["Alert.ExpectedSystemDirectoryMultiple"] = "{0} system folders were skipped as expected.",
                ["Alert.SkippedDirectorySingle"] = "1 folder could not be read.",
                ["Alert.SkippedDirectoryMultiple"] = "{0} folders could not be read.",
                ["Alert.UnknownSkippedDirectories"] = "{0} additional folders could not be read. Details were not captured.",
                ["Alert.Reason"] = "Reason: {0}",
                ["Alert.UnknownReason"] = "Unknown",
                ["Alert.Win32Error"] = "Win32 error {0}: {1}",
                ["Alert.NtStatusOpen"] = "Folder could not be opened. NTSTATUS: {0}",
                ["Alert.NtStatusRead"] = "Folder could not be read. NTSTATUS: {0}",
                ["Alert.NtQueryRootOpenFailed"] = "NT API fast scan could not open the root path: {0}",
                ["Alert.NtQueryRootReadFailed"] = "NT API fast scan could not read the root path: {0}",
                ["Alert.InvalidNtfsDrive"] = "No valid NTFS drive.",
                ["Status.MftFastScanCompleted"] = "MFT fast scan completed",
                ["Settings.Title"] = "Settings",
                ["Settings.General"] = "General",
                ["Settings.Export"] = "Export",
                ["Settings.Language"] = "Language:",
                ["Settings.LanguageGerman"] = "German",
                ["Settings.LanguageEnglish"] = "English",
                ["Settings.ShowFilesInTree"] = "Show files in tree",
                ["Settings.SkipReparsePoints"] = "Skip reparse points / junctions",
                ["Settings.ShowPartitionPanel"] = "Show partition panel",
                ["Settings.StartElevated"] = "Start with elevated privileges",
                ["Settings.ShowElevationPrompt"] = "Show admin notice at startup",
                ["Settings.ShellContextMenu"] = "Show Explorer context menu entry for folders and drives",
                ["Settings.Layout"] = "Layout:",
                ["Settings.LayoutWindowsDefault"] = "Windows default",
                ["Settings.LayoutWindowsLight"] = "Windows light mode",
                ["Settings.LayoutWindowsDark"] = "Windows dark mode",
                ["Settings.ExportPath"] = "Export path",
                ["Settings.ExportSizeGb"] = "Export size (GB)",
                ["Settings.ExportSizeMb"] = "Export size (MB)",
                ["Settings.ExportMaxDepth"] = "Maximum levels/depth:",
                ["Settings.ExportMaxDepthInvalid"] = "The maximum levels/depth must be empty or a number from 0 upward.",
                ["Settings.ShellContextMenuFailed"] = "The Explorer context menu entry could not be updated.",
                ["AlertHistory.Title"] = "Short log",
                ["AlertHistory.Type"] = "Type",
                ["AlertHistory.Category"] = "Category",
                ["AlertHistory.Message"] = "Message",
                ["AlertHistory.Details"] = "Details:",
                ["AlertHistory.CreatedAt"] = "Date and time",
                ["AlertHistory.Confirmed"] = "Confirmed",
                ["AlertHistory.Confirm"] = "Confirm",
                ["AlertHistory.Delete"] = "Delete",
                ["AlertHistory.ConfirmAll"] = "Confirm all",
                ["AlertHistory.DeleteAll"] = "Delete all",
                ["About.Title"] = "About WTF",
                ["About.VersionPrefix"] = "Version: ",
                ["About.UpdateChecking"] = "Checking for update...",
                ["About.GitHubUnavailable"] = "GitHub unreachable",
                ["About.NoNewVersion"] = "No new version available",
                ["About.UpdateAvailable"] = "Update available: {0}",
                ["About.FreeText"] = "WTF can be used free of charge.",
                ["About.SupportText"] = "If this tool helps you, you can support development here:",
                ["Elevation.Title"] = "WTF",
                ["Elevation.Message"] = "Would you like to run WTF with elevated privileges to\nincrease scan speed and accuracy?",
                ["Elevation.DoNotShowAgain"] = "Do not show this message again",
                ["Chart.NoData"] = "No data available.",
                ["Chart.Other"] = "Other",
                ["Chart.TooltipDates"] = "Created: {0}{1}Modified: {2}{1}Last access: {3}",
                ["Chart.PieTooltip"] = "{0}{1}Created: {2}{1}Modified: {3}{1}Last access: {4}",
                ["Chart.ItemLabel"] = "{0} - {1} ({2:0.0} %)",
                ["Chart.Directory"] = "Directory",
                ["Chart.FilePrefix"] = "File:",
                ["Csv.FileFilter"] = "CSV files (*.csv)|*.csv",
                ["Csv.Path"] = "Path",
                ["Csv.Level"] = "Level",
                ["Csv.SizeGb"] = "Size (GB)",
                ["Csv.SizeMb"] = "Size (MB)",
                ["Csv.Root"] = "Root",
                ["Drive.LocalDisk"] = "Local Disk",
                ["Drive.Display"] = "{0} ({1})",
                ["Shell.ContextMenuText"] = "WTF: Start Size Scan",
                ["Status.ScanTitlePrefix"] = "Scan: "
            };
        }
    }
}
