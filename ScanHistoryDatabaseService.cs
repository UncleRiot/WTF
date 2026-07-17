using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace WTF
{
    public static class ScanHistoryDatabaseService
    {
        private const int ScanHistoryVersion = 4;
        private const string DatabaseFileName = "scan_history.db";

        private static readonly object SyncRoot = new object();

        private static readonly string ScanHistoryDirectoryPath = Path.Combine(
            AppContext.BaseDirectory,
            "ScanHistory");

        private static readonly string DefaultDatabaseFilePath = Path.Combine(
            ScanHistoryDirectoryPath,
            DatabaseFileName);

        private static string databaseFilePath = DefaultDatabaseFilePath;

        public static string DefaultDatabasePath => DefaultDatabaseFilePath;

        public static string DatabasePath => databaseFilePath;

        public static void ConfigureDatabasePath(string databasePath)
        {
            databaseFilePath = NormalizeDatabasePath(databasePath);
        }

        public static string NormalizeDatabasePath(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                return DefaultDatabaseFilePath;

            try
            {
                return Path.GetFullPath(databasePath.Trim());
            }
            catch
            {
                return DefaultDatabaseFilePath;
            }
        }

        public static void MoveDatabase(string targetDatabasePath)
        {
            lock (SyncRoot)
            {
                string sourceDatabasePath = NormalizeDatabasePath(databaseFilePath);
                string normalizedTargetDatabasePath = NormalizeDatabasePath(targetDatabasePath);

                if (string.Equals(
                        sourceDatabasePath,
                        normalizedTargetDatabasePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    databaseFilePath = normalizedTargetDatabasePath;
                    return;
                }

                string targetDirectoryPath = Path.GetDirectoryName(normalizedTargetDatabasePath);

                if (string.IsNullOrWhiteSpace(targetDirectoryPath))
                    throw new IOException("Database directory path is empty.");

                Directory.CreateDirectory(targetDirectoryPath);

                if (File.Exists(normalizedTargetDatabasePath))
                    throw new IOException("The selected database file already exists.");

                SqliteConnection.ClearAllPools();

                MoveDatabaseSidecarFile(sourceDatabasePath, normalizedTargetDatabasePath, string.Empty);
                MoveDatabaseSidecarFile(sourceDatabasePath, normalizedTargetDatabasePath, "-wal");
                MoveDatabaseSidecarFile(sourceDatabasePath, normalizedTargetDatabasePath, "-shm");
                MoveDatabaseSidecarFile(sourceDatabasePath, normalizedTargetDatabasePath, "-journal");

                databaseFilePath = normalizedTargetDatabasePath;
            }
        }

        private static void MoveDatabaseSidecarFile(
            string sourceDatabasePath,
            string targetDatabasePath,
            string suffix)
        {
            string sourcePath = sourceDatabasePath + suffix;
            string targetPath = targetDatabasePath + suffix;

            if (!File.Exists(sourcePath))
                return;

            File.Move(sourcePath, targetPath);
        }

        public static string Save(FileSystemEntry rootEntry)
        {
            if (rootEntry == null)
                throw new ArgumentNullException(nameof(rootEntry));

            if (string.IsNullOrWhiteSpace(rootEntry.FullPath))
                throw new InvalidOperationException("Scan root path is empty.");

            Directory.CreateDirectory(GetDatabaseDirectoryPath());
            EnsureDatabase();

            string scanId = Guid.NewGuid().ToString("N");
            DateTime createdUtc = DateTime.UtcNow;

            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            using (SqliteCommand scanCommand = connection.CreateCommand())
            {
                scanCommand.Transaction = transaction;
                scanCommand.CommandText =
                    "INSERT INTO scans " +
                    "(scan_id, created_utc_ticks, root_path, root_size_bytes, file_count, directory_count) " +
                    "VALUES ($scan_id, $created_utc_ticks, $root_path, $root_size_bytes, 0, 0);";

                scanCommand.Parameters.Add("$scan_id", SqliteType.Text).Value = scanId;
                scanCommand.Parameters.Add("$created_utc_ticks", SqliteType.Integer).Value = createdUtc.Ticks;
                scanCommand.Parameters.Add("$root_path", SqliteType.Text).Value = rootEntry.FullPath;
                scanCommand.Parameters.Add("$root_size_bytes", SqliteType.Integer).Value = rootEntry.SizeBytes;

                scanCommand.ExecuteNonQuery();
            }

            ScanHistoryEntryCounter counter = new ScanHistoryEntryCounter();

            using (SqliteCommand entryCommand = CreateInsertEntryCommand(connection, transaction))
            {
                HashSet<string> insertedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                InsertEntry(entryCommand, scanId, rootEntry, insertedPaths, counter);
            }

            using (SqliteCommand updateScanCommand = connection.CreateCommand())
            {
                updateScanCommand.Transaction = transaction;
                updateScanCommand.CommandText =
                    "UPDATE scans " +
                    "SET file_count = $file_count, directory_count = $directory_count " +
                    "WHERE scan_id = $scan_id;";

                updateScanCommand.Parameters.Add("$file_count", SqliteType.Integer).Value = counter.FileCount;
                updateScanCommand.Parameters.Add("$directory_count", SqliteType.Integer).Value = counter.DirectoryCount;
                updateScanCommand.Parameters.Add("$scan_id", SqliteType.Text).Value = scanId;

                updateScanCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            return scanId;
        }

        public static IReadOnlyList<ScanHistoryInfo> List()
        {
            EnsureDatabase();

            List<ScanHistoryInfo> scanHistoryInfos = new List<ScanHistoryInfo>();

            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText =
                "SELECT scan_id, created_utc_ticks, root_path, root_size_bytes, file_count, directory_count " +
                "FROM scans " +
                "ORDER BY created_utc_ticks DESC, root_path COLLATE NOCASE ASC;";

            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string scanId = reader.GetString(0);

                scanHistoryInfos.Add(new ScanHistoryInfo
                {
                    FilePath = scanId,
                    ScanId = scanId,
                    CreatedUtc = CreateUtcDateTime(reader.GetInt64(1)),
                    RootPath = reader.GetString(2),
                    RootSizeBytes = reader.GetInt64(3),
                    FileCount = reader.GetInt32(4),
                    DirectoryCount = reader.GetInt32(5)
                });
            }

            return scanHistoryInfos;
        }

        public static ScanHistorySnapshot Load(string scanId)
        {
            if (string.IsNullOrWhiteSpace(scanId))
                throw new ArgumentException("Scan id is empty.", nameof(scanId));

            EnsureDatabase();

            using SqliteConnection connection = OpenConnection();

            ScanHistorySnapshot snapshot = LoadSnapshotHeader(connection, scanId);
            snapshot.RootEntry = LoadRootEntry(connection, snapshot);

            return snapshot;
        }

        private static void EnsureDatabase()
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(GetDatabaseDirectoryPath());

                bool vacuumDatabase;

                using (SqliteConnection connection = OpenConnection())
                {
                    int databaseVersion = GetDatabaseVersion(connection);

                    CreateScanTable(connection);
                    CreateDeduplicatedTables(connection);
                    vacuumDatabase =
                        MigrateLegacyEntries(connection) ||
                        databaseVersion < ScanHistoryVersion;
                    CreateIndexes(connection);
                }

                if (vacuumDatabase)
                {
                    VacuumDatabase();
                }

                using SqliteConnection versionConnection = OpenConnection();
                SetDatabaseVersion(versionConnection);
            }
        }

        private static void CreateScanTable(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "CREATE TABLE IF NOT EXISTS scans (" +
                "scan_id TEXT NOT NULL PRIMARY KEY, " +
                "created_utc_ticks INTEGER NOT NULL, " +
                "root_path TEXT NOT NULL, " +
                "root_size_bytes INTEGER NOT NULL, " +
                "file_count INTEGER NOT NULL, " +
                "directory_count INTEGER NOT NULL);";

            command.ExecuteNonQuery();
        }

        private static void CreateDeduplicatedTables(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "CREATE TABLE IF NOT EXISTS paths (" +
                "path_id INTEGER NOT NULL PRIMARY KEY, " +
                "full_path TEXT NOT NULL COLLATE NOCASE, " +
                "name TEXT NOT NULL, " +
                "is_directory INTEGER NOT NULL, " +
                "UNIQUE (full_path, is_directory)); " +
                "CREATE TABLE IF NOT EXISTS entry_versions (" +
                "version_id INTEGER NOT NULL PRIMARY KEY, " +
                "path_id INTEGER NOT NULL, " +
                "size_bytes INTEGER NOT NULL, " +
                "last_write_utc_ticks INTEGER NOT NULL, " +
                "UNIQUE (path_id, size_bytes, last_write_utc_ticks)); " +
                "CREATE TABLE IF NOT EXISTS scan_entries (" +
                "scan_id TEXT NOT NULL, " +
                "path_id INTEGER NOT NULL, " +
                "version_id INTEGER NOT NULL, " +
                "PRIMARY KEY (scan_id, path_id));";

            command.ExecuteNonQuery();
        }

        private static bool MigrateLegacyEntries(SqliteConnection connection)
        {
            using (SqliteCommand tableCommand = connection.CreateCommand())
            {
                tableCommand.CommandText =
                    "SELECT COUNT(*) " +
                    "FROM sqlite_master " +
                    "WHERE type = 'table' AND name = 'entries';";

                if (Convert.ToInt32(tableCommand.ExecuteScalar()) == 0)
                    return false;
            }

            using SqliteTransaction transaction = connection.BeginTransaction();
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "INSERT OR IGNORE INTO paths (full_path, name, is_directory) " +
                "SELECT full_path, name, is_directory " +
                "FROM entries " +
                "GROUP BY full_path COLLATE NOCASE, is_directory; " +
                "INSERT OR IGNORE INTO entry_versions (path_id, size_bytes, last_write_utc_ticks) " +
                "SELECT paths.path_id, entries.size_bytes, entries.last_write_utc_ticks " +
                "FROM entries " +
                "INNER JOIN paths ON " +
                "paths.full_path = entries.full_path COLLATE NOCASE AND " +
                "paths.is_directory = entries.is_directory; " +
                "INSERT OR REPLACE INTO scan_entries (scan_id, path_id, version_id) " +
                "SELECT entries.scan_id, paths.path_id, entry_versions.version_id " +
                "FROM entries " +
                "INNER JOIN paths ON " +
                "paths.full_path = entries.full_path COLLATE NOCASE AND " +
                "paths.is_directory = entries.is_directory " +
                "INNER JOIN entry_versions ON " +
                "entry_versions.path_id = paths.path_id AND " +
                "entry_versions.size_bytes = entries.size_bytes AND " +
                "entry_versions.last_write_utc_ticks = entries.last_write_utc_ticks; " +
                "DROP TABLE entries;";

            command.ExecuteNonQuery();
            transaction.Commit();

            return true;
        }

        private static void CreateIndexes(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "CREATE INDEX IF NOT EXISTS IX_scans_created_utc_ticks " +
                "ON scans (created_utc_ticks); " +
                "CREATE INDEX IF NOT EXISTS IX_paths_full_path " +
                "ON paths (full_path COLLATE NOCASE); " +
                "CREATE INDEX IF NOT EXISTS IX_entry_versions_path_id " +
                "ON entry_versions (path_id);";

            command.ExecuteNonQuery();
        }

        private static int GetDatabaseVersion(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version;";

            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static void SetDatabaseVersion(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = " + ScanHistoryVersion + ";";
            command.ExecuteNonQuery();
        }

        private static void VacuumDatabase()
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "VACUUM;";
            command.ExecuteNonQuery();
        }

        private static string GetDatabaseDirectoryPath()
        {
            string directoryPath = Path.GetDirectoryName(databaseFilePath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
                return directoryPath;

            return ScanHistoryDirectoryPath;
        }

        private static SqliteConnection OpenConnection()
        {
            SqliteConnectionStringBuilder connectionStringBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = databaseFilePath,
                Mode = SqliteOpenMode.ReadWriteCreate
            };

            SqliteConnection connection = new SqliteConnection(connectionStringBuilder.ToString());
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA busy_timeout = 5000;";
            command.ExecuteNonQuery();

            return connection;
        }

        private static SqliteCommand CreateInsertEntryCommand(
            SqliteConnection connection,
            SqliteTransaction transaction)
        {
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "INSERT OR IGNORE INTO paths " +
                "(full_path, name, is_directory) " +
                "VALUES ($full_path, $name, $is_directory); " +
                "INSERT OR IGNORE INTO entry_versions " +
                "(path_id, size_bytes, last_write_utc_ticks) " +
                "SELECT path_id, $size_bytes, $last_write_utc_ticks " +
                "FROM paths " +
                "WHERE full_path = $full_path AND is_directory = $is_directory; " +
                "INSERT INTO scan_entries " +
                "(scan_id, path_id, version_id) " +
                "SELECT $scan_id, paths.path_id, entry_versions.version_id " +
                "FROM paths " +
                "INNER JOIN entry_versions ON " +
                "entry_versions.path_id = paths.path_id AND " +
                "entry_versions.size_bytes = $size_bytes AND " +
                "entry_versions.last_write_utc_ticks = $last_write_utc_ticks " +
                "WHERE paths.full_path = $full_path AND paths.is_directory = $is_directory " +
                "ON CONFLICT (scan_id, path_id) DO UPDATE SET version_id = excluded.version_id;";

            command.Parameters.Add("$scan_id", SqliteType.Text);
            command.Parameters.Add("$full_path", SqliteType.Text);
            command.Parameters.Add("$name", SqliteType.Text);
            command.Parameters.Add("$is_directory", SqliteType.Integer);
            command.Parameters.Add("$size_bytes", SqliteType.Integer);
            command.Parameters.Add("$last_write_utc_ticks", SqliteType.Integer);

            return command;
        }

        private static void InsertEntry(
            SqliteCommand command,
            string scanId,
            FileSystemEntry entry,
            HashSet<string> insertedPaths,
            ScanHistoryEntryCounter counter)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.FullPath))
                return;

            if (!insertedPaths.Add(entry.FullPath))
                return;

            command.Parameters["$scan_id"].Value = scanId;
            command.Parameters["$full_path"].Value = entry.FullPath;
            command.Parameters["$name"].Value =
                string.IsNullOrWhiteSpace(entry.Name)
                    ? GetEntryName(entry.FullPath)
                    : entry.Name;
            command.Parameters["$is_directory"].Value = entry.IsDirectory ? 1 : 0;
            command.Parameters["$size_bytes"].Value = entry.SizeBytes;
            command.Parameters["$last_write_utc_ticks"].Value = GetUtcTicks(entry.LastWriteTimeUtc);

            command.ExecuteNonQuery();

            if (entry.IsDirectory)
            {
                counter.DirectoryCount++;
            }
            else
            {
                counter.FileCount++;
            }

            if (entry.AllFiles != null)
            {
                foreach (FileSystemEntry file in entry.AllFiles)
                {
                    InsertEntry(command, scanId, file, insertedPaths, counter);
                }
            }

            foreach (FileSystemEntry child in entry.Children)
            {
                InsertEntry(command, scanId, child, insertedPaths, counter);
            }
        }

        private static ScanHistorySnapshot LoadSnapshotHeader(
            SqliteConnection connection,
            string scanId)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT scan_id, created_utc_ticks, root_path, root_size_bytes " +
                "FROM scans " +
                "WHERE scan_id = $scan_id;";

            command.Parameters.Add("$scan_id", SqliteType.Text).Value = scanId;

            using SqliteDataReader reader = command.ExecuteReader();

            if (!reader.Read())
                throw new FileNotFoundException("Scan history entry was not found.", scanId);

            return new ScanHistorySnapshot
            {
                Version = ScanHistoryVersion,
                ScanId = reader.GetString(0),
                CreatedUtc = CreateUtcDateTime(reader.GetInt64(1)),
                RootPath = reader.GetString(2),
                RootSizeBytes = reader.GetInt64(3)
            };
        }

        private static FileSystemEntry LoadRootEntry(
            SqliteConnection connection,
            ScanHistorySnapshot snapshot)
        {
            FileSystemEntry rootEntry = new FileSystemEntry
            {
                Name = GetEntryName(snapshot.RootPath),
                FullPath = snapshot.RootPath,
                SizeBytes = snapshot.RootSizeBytes,
                IsDirectory = true,
                LastWriteTimeUtc = DateTime.MinValue
            };

            Dictionary<string, FileSystemEntry> directories =
                new Dictionary<string, FileSystemEntry>(StringComparer.OrdinalIgnoreCase)
                {
                    [rootEntry.FullPath] = rootEntry
                };

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT paths.full_path, paths.name, paths.is_directory, " +
                "entry_versions.size_bytes, entry_versions.last_write_utc_ticks " +
                "FROM scan_entries " +
                "INNER JOIN paths ON paths.path_id = scan_entries.path_id " +
                "INNER JOIN entry_versions ON entry_versions.version_id = scan_entries.version_id " +
                "WHERE scan_entries.scan_id = $scan_id " +
                "ORDER BY paths.is_directory DESC, paths.full_path COLLATE NOCASE ASC;";

            command.Parameters.Add("$scan_id", SqliteType.Text).Value = snapshot.ScanId;

            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                string fullPath = reader.GetString(0);
                string name = reader.GetString(1);
                bool isDirectory = reader.GetInt32(2) != 0;
                long sizeBytes = reader.GetInt64(3);
                long lastWriteUtcTicks = reader.GetInt64(4);

                if (string.Equals(
                        fullPath,
                        rootEntry.FullPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    rootEntry.Name = name;
                    rootEntry.SizeBytes = sizeBytes;
                    rootEntry.LastWriteTimeUtc =
                        CreateUtcDateTimeOrMinValue(lastWriteUtcTicks);
                    continue;
                }

                string parentPath = GetParentPath(fullPath);

                FileSystemEntry entry = new FileSystemEntry
                {
                    Name = name,
                    FullPath = fullPath,
                    SizeBytes = sizeBytes,
                    IsDirectory = isDirectory,
                    LastWriteTimeUtc = CreateUtcDateTimeOrMinValue(lastWriteUtcTicks)
                };

                if (isDirectory)
                {
                    directories[fullPath] = entry;
                }
                else
                {
                    rootEntry.AllFiles.Add(entry);
                }

                if (directories.TryGetValue(parentPath, out FileSystemEntry parentDirectory))
                {
                    parentDirectory.Children.Add(entry);
                }
            }

            return rootEntry;
        }

        private static string GetParentPath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return string.Empty;

            try
            {
                string trimmedPath = fullPath.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                string parentPath = Path.GetDirectoryName(trimmedPath);

                return parentPath ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetEntryName(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return string.Empty;

            try
            {
                string trimmedPath = fullPath.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);
                string fileName = Path.GetFileName(trimmedPath);

                if (!string.IsNullOrWhiteSpace(fileName))
                    return fileName;

                return trimmedPath;
            }
            catch
            {
                return fullPath;
            }
        }

        private static long GetUtcTicks(DateTime value)
        {
            if (value == DateTime.MinValue)
                return DateTime.MinValue.Ticks;

            if (value.Kind == DateTimeKind.Unspecified)
                return DateTime.SpecifyKind(value, DateTimeKind.Utc).Ticks;

            return value.ToUniversalTime().Ticks;
        }

        private static DateTime CreateUtcDateTime(long ticks)
        {
            if (ticks <= DateTime.MinValue.Ticks)
                return DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);

            if (ticks >= DateTime.MaxValue.Ticks)
                return DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

            return new DateTime(ticks, DateTimeKind.Utc);
        }

        private static DateTime CreateUtcDateTimeOrMinValue(long ticks)
        {
            if (ticks <= DateTime.MinValue.Ticks)
                return DateTime.MinValue;

            return CreateUtcDateTime(ticks);
        }

        private sealed class ScanHistoryEntryCounter
        {
            public int FileCount { get; set; }
            public int DirectoryCount { get; set; }
        }
    }
}
