using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace WTF
{
    public static class ScanHistoryDatabaseService
    {
        private const int ScanHistoryVersion = 7;
        private const int ChangeTypeUpsert = 1;
        private const int ChangeTypeDelete = 2;
        private const string DatabaseFileName = "scan_history.db";

        private static readonly object SyncRoot = new object();

        private static readonly string ScanHistoryDirectoryPath = Path.Combine(
            AppContext.BaseDirectory,
            "ScanHistory");

        private static readonly string DefaultDatabaseFilePath = Path.Combine(
            ScanHistoryDirectoryPath,
            DatabaseFileName);

        private static string databaseFilePath = DefaultDatabaseFilePath;
        private static int maximumScansPerPath = 30;

        public static string DefaultDatabasePath => DefaultDatabaseFilePath;

        public static string DatabasePath => databaseFilePath;

        public static void ConfigureDatabasePath(string databasePath)
        {
            databaseFilePath = NormalizeDatabasePath(databasePath);
        }

        public static void ConfigureRetention(int maximumScans)
        {
            maximumScansPerPath = Math.Max(1, maximumScans);
        }

        public static bool IsMaintenanceRequired()
        {
            lock (SyncRoot)
            {
                if (!File.Exists(databaseFilePath))
                    return false;

                using SqliteConnection connection = OpenConnection();

                if (GetDatabaseVersion(connection) < ScanHistoryVersion)
                    return true;

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText =
                    "SELECT COUNT(*) " +
                    "FROM sqlite_master " +
                    "WHERE type = 'table' AND name = 'entries';";

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
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

            lock (SyncRoot)
            {
                Directory.CreateDirectory(GetDatabaseDirectoryPath());
                EnsureDatabase();

                string scanId = Guid.NewGuid().ToString("N");
                DateTime createdUtc = DateTime.UtcNow;
                Dictionary<string, EntryData> currentEntries = CollectEntries(
                    rootEntry,
                    out int fileCount,
                    out int directoryCount);

                using SqliteConnection connection = OpenConnection();
                string previousScanId = GetLatestScanId(connection, rootEntry.FullPath);
                Dictionary<string, EntryData> previousEntries =
                    string.IsNullOrWhiteSpace(previousScanId)
                        ? new Dictionary<string, EntryData>(StringComparer.OrdinalIgnoreCase)
                        : LoadEntryState(connection, previousScanId);

                using SqliteTransaction transaction = connection.BeginTransaction();

                InsertScan(
                    connection,
                    transaction,
                    scanId,
                    createdUtc,
                    rootEntry,
                    fileCount,
                    directoryCount,
                    previousScanId,
                    string.IsNullOrWhiteSpace(previousScanId));

                foreach (KeyValuePair<string, EntryData> currentEntry in currentEntries)
                {
                    if (previousEntries.TryGetValue(currentEntry.Key, out EntryData previousEntry) &&
                        previousEntry.HasSameVersion(currentEntry.Value))
                    {
                        continue;
                    }

                    InsertDeltaEntry(
                        connection,
                        transaction,
                        scanId,
                        currentEntry.Value,
                        ChangeTypeUpsert);
                }

                foreach (KeyValuePair<string, EntryData> previousEntry in previousEntries)
                {
                    if (currentEntries.ContainsKey(previousEntry.Key))
                        continue;

                    InsertDeltaEntry(
                        connection,
                        transaction,
                        scanId,
                        previousEntry.Value,
                        ChangeTypeDelete);
                }

                transaction.Commit();

                bool pruned = ApplyRetention(connection, rootEntry.FullPath);

                if (pruned)
                {
                    CleanupOrphans(connection);
                    connection.Close();
                    SqliteConnection.ClearAllPools();
                    VacuumDatabase();
                }

                return scanId;
            }
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
            Dictionary<string, EntryData> entries = LoadEntryState(connection, scanId);
            snapshot.RootEntry = BuildRootEntry(snapshot, entries);

            return snapshot;
        }

        private static void EnsureDatabase()
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(GetDatabaseDirectoryPath());

                bool vacuumDatabase = false;

                using (SqliteConnection connection = OpenConnection())
                {
                    int databaseVersion = GetDatabaseVersion(connection);

                    CreateScanTable(connection);
                    CreateDeduplicatedTables(connection);
                    EnsureDeltaColumns(connection);

                    if (MigrateLegacyEntries(connection))
                    {
                        vacuumDatabase = true;
                    }

                    CreateIndexes(connection);

                    if (databaseVersion < ScanHistoryVersion)
                    {
                        RebuildAllHistoriesAsDeltas(connection);
                        CleanupOrphans(connection);
                        vacuumDatabase = true;
                    }
                }

                if (vacuumDatabase)
                {
                    SqliteConnection.ClearAllPools();
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
                "directory_count INTEGER NOT NULL, " +
                "previous_scan_id TEXT NULL, " +
                "is_baseline INTEGER NOT NULL DEFAULT 1);";

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
                "change_type INTEGER NOT NULL DEFAULT 1, " +
                "PRIMARY KEY (scan_id, path_id));";

            command.ExecuteNonQuery();
        }

        private static void EnsureDeltaColumns(SqliteConnection connection)
        {
            EnsureColumn(
                connection,
                "scans",
                "previous_scan_id",
                "ALTER TABLE scans ADD COLUMN previous_scan_id TEXT NULL;");

            EnsureColumn(
                connection,
                "scans",
                "is_baseline",
                "ALTER TABLE scans ADD COLUMN is_baseline INTEGER NOT NULL DEFAULT 1;");

            EnsureColumn(
                connection,
                "scan_entries",
                "change_type",
                "ALTER TABLE scan_entries ADD COLUMN change_type INTEGER NOT NULL DEFAULT 1;");
        }

        private static void EnsureColumn(
            SqliteConnection connection,
            string tableName,
            string columnName,
            string alterStatement)
        {
            using SqliteCommand checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "PRAGMA table_info(" + tableName + ");";

            using SqliteDataReader reader = checkCommand.ExecuteReader();

            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            reader.Close();

            using SqliteCommand alterCommand = connection.CreateCommand();
            alterCommand.CommandText = alterStatement;
            alterCommand.ExecuteNonQuery();
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
                "INSERT OR REPLACE INTO scan_entries (scan_id, path_id, version_id, change_type) " +
                "SELECT entries.scan_id, paths.path_id, entry_versions.version_id, 1 " +
                "FROM entries " +
                "INNER JOIN paths ON " +
                "paths.full_path = entries.full_path COLLATE NOCASE AND " +
                "paths.is_directory = entries.is_directory " +
                "INNER JOIN entry_versions ON " +
                "entry_versions.path_id = paths.path_id AND " +
                "entry_versions.size_bytes = entries.size_bytes AND " +
                "entry_versions.last_write_utc_ticks = entries.last_write_utc_ticks; " +
                "UPDATE scans SET previous_scan_id = NULL, is_baseline = 1; " +
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
                "CREATE INDEX IF NOT EXISTS IX_scans_root_created " +
                "ON scans (root_path COLLATE NOCASE, created_utc_ticks); " +
                "CREATE INDEX IF NOT EXISTS IX_paths_full_path " +
                "ON paths (full_path COLLATE NOCASE); " +
                "CREATE INDEX IF NOT EXISTS IX_entry_versions_path_id " +
                "ON entry_versions (path_id); " +
                "CREATE INDEX IF NOT EXISTS IX_scan_entries_scan_id " +
                "ON scan_entries (scan_id);";

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

        private static void InsertScan(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string scanId,
            DateTime createdUtc,
            FileSystemEntry rootEntry,
            int fileCount,
            int directoryCount,
            string previousScanId,
            bool isBaseline)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "INSERT INTO scans " +
                "(scan_id, created_utc_ticks, root_path, root_size_bytes, file_count, directory_count, previous_scan_id, is_baseline) " +
                "VALUES ($scan_id, $created_utc_ticks, $root_path, $root_size_bytes, $file_count, $directory_count, $previous_scan_id, $is_baseline);";

            command.Parameters.Add("$scan_id", SqliteType.Text).Value = scanId;
            command.Parameters.Add("$created_utc_ticks", SqliteType.Integer).Value = createdUtc.Ticks;
            command.Parameters.Add("$root_path", SqliteType.Text).Value = rootEntry.FullPath;
            command.Parameters.Add("$root_size_bytes", SqliteType.Integer).Value = rootEntry.SizeBytes;
            command.Parameters.Add("$file_count", SqliteType.Integer).Value = fileCount;
            command.Parameters.Add("$directory_count", SqliteType.Integer).Value = directoryCount;
            command.Parameters.Add("$previous_scan_id", SqliteType.Text).Value =
                string.IsNullOrWhiteSpace(previousScanId)
                    ? DBNull.Value
                    : previousScanId;
            command.Parameters.Add("$is_baseline", SqliteType.Integer).Value = isBaseline ? 1 : 0;

            command.ExecuteNonQuery();
        }

        private static string GetLatestScanId(
            SqliteConnection connection,
            string rootPath)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "SELECT scan_id " +
                "FROM scans " +
                "WHERE root_path = $root_path COLLATE NOCASE " +
                "ORDER BY created_utc_ticks DESC " +
                "LIMIT 1;";

            command.Parameters.Add("$root_path", SqliteType.Text).Value = rootPath;
            object result = command.ExecuteScalar();

            return result == null || result == DBNull.Value
                ? null
                : Convert.ToString(result);
        }

        private static void InsertDeltaEntry(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string scanId,
            EntryData entry,
            int changeType)
        {
            long pathId = EnsurePath(connection, transaction, entry);
            long versionId = changeType == ChangeTypeDelete
                ? 0
                : EnsureVersion(connection, transaction, pathId, entry);

            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                "INSERT OR REPLACE INTO scan_entries " +
                "(scan_id, path_id, version_id, change_type) " +
                "VALUES ($scan_id, $path_id, $version_id, $change_type);";

            command.Parameters.Add("$scan_id", SqliteType.Text).Value = scanId;
            command.Parameters.Add("$path_id", SqliteType.Integer).Value = pathId;
            command.Parameters.Add("$version_id", SqliteType.Integer).Value = versionId;
            command.Parameters.Add("$change_type", SqliteType.Integer).Value = changeType;

            command.ExecuteNonQuery();
        }

        private static long EnsurePath(
            SqliteConnection connection,
            SqliteTransaction transaction,
            EntryData entry)
        {
            using (SqliteCommand insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText =
                    "INSERT OR IGNORE INTO paths " +
                    "(full_path, name, is_directory) " +
                    "VALUES ($full_path, $name, $is_directory);";

                insertCommand.Parameters.Add("$full_path", SqliteType.Text).Value = entry.FullPath;
                insertCommand.Parameters.Add("$name", SqliteType.Text).Value = entry.Name;
                insertCommand.Parameters.Add("$is_directory", SqliteType.Integer).Value =
                    entry.IsDirectory ? 1 : 0;

                insertCommand.ExecuteNonQuery();
            }

            using SqliteCommand selectCommand = connection.CreateCommand();
            selectCommand.Transaction = transaction;
            selectCommand.CommandText =
                "SELECT path_id " +
                "FROM paths " +
                "WHERE full_path = $full_path COLLATE NOCASE AND is_directory = $is_directory;";

            selectCommand.Parameters.Add("$full_path", SqliteType.Text).Value = entry.FullPath;
            selectCommand.Parameters.Add("$is_directory", SqliteType.Integer).Value =
                entry.IsDirectory ? 1 : 0;

            return Convert.ToInt64(selectCommand.ExecuteScalar());
        }

        private static long EnsureVersion(
            SqliteConnection connection,
            SqliteTransaction transaction,
            long pathId,
            EntryData entry)
        {
            using (SqliteCommand insertCommand = connection.CreateCommand())
            {
                insertCommand.Transaction = transaction;
                insertCommand.CommandText =
                    "INSERT OR IGNORE INTO entry_versions " +
                    "(path_id, size_bytes, last_write_utc_ticks) " +
                    "VALUES ($path_id, $size_bytes, $last_write_utc_ticks);";

                insertCommand.Parameters.Add("$path_id", SqliteType.Integer).Value = pathId;
                insertCommand.Parameters.Add("$size_bytes", SqliteType.Integer).Value = entry.SizeBytes;
                insertCommand.Parameters.Add("$last_write_utc_ticks", SqliteType.Integer).Value =
                    entry.LastWriteUtcTicks;

                insertCommand.ExecuteNonQuery();
            }

            using SqliteCommand selectCommand = connection.CreateCommand();
            selectCommand.Transaction = transaction;
            selectCommand.CommandText =
                "SELECT version_id " +
                "FROM entry_versions " +
                "WHERE path_id = $path_id AND size_bytes = $size_bytes AND last_write_utc_ticks = $last_write_utc_ticks;";

            selectCommand.Parameters.Add("$path_id", SqliteType.Integer).Value = pathId;
            selectCommand.Parameters.Add("$size_bytes", SqliteType.Integer).Value = entry.SizeBytes;
            selectCommand.Parameters.Add("$last_write_utc_ticks", SqliteType.Integer).Value =
                entry.LastWriteUtcTicks;

            return Convert.ToInt64(selectCommand.ExecuteScalar());
        }

        private static Dictionary<string, EntryData> CollectEntries(
            FileSystemEntry rootEntry,
            out int fileCount,
            out int directoryCount)
        {
            Dictionary<string, EntryData> entries =
                new Dictionary<string, EntryData>(StringComparer.OrdinalIgnoreCase);

            fileCount = 0;
            directoryCount = 0;

            AddEntry(rootEntry, entries, ref fileCount, ref directoryCount);

            return entries;
        }

        private static void AddEntry(
            FileSystemEntry entry,
            Dictionary<string, EntryData> entries,
            ref int fileCount,
            ref int directoryCount)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.FullPath))
                return;

            if (entries.ContainsKey(entry.FullPath))
                return;

            entries[entry.FullPath] = EntryData.FromFileSystemEntry(entry);

            if (entry.IsDirectory)
                directoryCount++;
            else
                fileCount++;

            if (entry.AllFiles != null)
            {
                foreach (FileSystemEntry file in entry.AllFiles)
                {
                    AddEntry(file, entries, ref fileCount, ref directoryCount);
                }
            }

            foreach (FileSystemEntry child in entry.Children)
            {
                AddEntry(child, entries, ref fileCount, ref directoryCount);
            }
        }

        private static Dictionary<string, EntryData> LoadEntryState(
            SqliteConnection connection,
            string scanId)
        {
            List<string> chain = LoadScanChain(connection, scanId);
            Dictionary<string, EntryData> entries =
                new Dictionary<string, EntryData>(StringComparer.OrdinalIgnoreCase);

            foreach (string chainScanId in chain)
            {
                using SqliteCommand command = connection.CreateCommand();
                command.CommandText =
                    "SELECT paths.full_path, paths.name, paths.is_directory, " +
                    "scan_entries.change_type, entry_versions.size_bytes, entry_versions.last_write_utc_ticks " +
                    "FROM scan_entries " +
                    "INNER JOIN paths ON paths.path_id = scan_entries.path_id " +
                    "LEFT JOIN entry_versions ON entry_versions.version_id = scan_entries.version_id " +
                    "WHERE scan_entries.scan_id = $scan_id;";

                command.Parameters.Add("$scan_id", SqliteType.Text).Value = chainScanId;

                using SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string fullPath = reader.GetString(0);
                    int changeType = reader.GetInt32(3);

                    if (changeType == ChangeTypeDelete)
                    {
                        entries.Remove(fullPath);
                        continue;
                    }

                    entries[fullPath] = new EntryData
                    {
                        FullPath = fullPath,
                        Name = reader.GetString(1),
                        IsDirectory = reader.GetInt32(2) != 0,
                        SizeBytes = reader.IsDBNull(4) ? 0 : reader.GetInt64(4),
                        LastWriteUtcTicks = reader.IsDBNull(5)
                            ? DateTime.MinValue.Ticks
                            : reader.GetInt64(5)
                    };
                }
            }

            return entries;
        }

        private static List<string> LoadScanChain(
            SqliteConnection connection,
            string scanId)
        {
            List<string> chain = new List<string>();
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string currentScanId = scanId;

            while (!string.IsNullOrWhiteSpace(currentScanId))
            {
                if (!visited.Add(currentScanId))
                    throw new InvalidDataException("Scan history chain contains a cycle.");

                using SqliteCommand command = connection.CreateCommand();
                command.CommandText =
                    "SELECT previous_scan_id, is_baseline " +
                    "FROM scans " +
                    "WHERE scan_id = $scan_id;";

                command.Parameters.Add("$scan_id", SqliteType.Text).Value = currentScanId;

                using SqliteDataReader reader = command.ExecuteReader();

                if (!reader.Read())
                    throw new FileNotFoundException("Scan history entry was not found.", currentScanId);

                chain.Add(currentScanId);

                bool isBaseline = reader.GetInt32(1) != 0;

                if (isBaseline)
                    break;

                currentScanId = reader.IsDBNull(0)
                    ? null
                    : reader.GetString(0);
            }

            chain.Reverse();
            return chain;
        }

        private static bool ApplyRetention(
            SqliteConnection connection,
            string rootPath)
        {
            List<string> scanIds = new List<string>();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT scan_id " +
                    "FROM scans " +
                    "WHERE root_path = $root_path COLLATE NOCASE " +
                    "ORDER BY created_utc_ticks ASC;";

                command.Parameters.Add("$root_path", SqliteType.Text).Value = rootPath;

                using SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    scanIds.Add(reader.GetString(0));
                }
            }

            if (scanIds.Count <= maximumScansPerPath)
                return false;

            int removeCount = scanIds.Count - maximumScansPerPath;
            string newBaselineScanId = scanIds[removeCount];
            Dictionary<string, EntryData> newBaselineEntries =
                LoadEntryState(connection, newBaselineScanId);

            using SqliteTransaction transaction = connection.BeginTransaction();

            using (SqliteCommand deleteEntriesCommand = connection.CreateCommand())
            {
                deleteEntriesCommand.Transaction = transaction;
                deleteEntriesCommand.CommandText =
                    "DELETE FROM scan_entries WHERE scan_id = $scan_id;";
                deleteEntriesCommand.Parameters.Add("$scan_id", SqliteType.Text).Value =
                    newBaselineScanId;
                deleteEntriesCommand.ExecuteNonQuery();
            }

            foreach (EntryData entry in newBaselineEntries.Values)
            {
                InsertDeltaEntry(
                    connection,
                    transaction,
                    newBaselineScanId,
                    entry,
                    ChangeTypeUpsert);
            }

            using (SqliteCommand updateBaselineCommand = connection.CreateCommand())
            {
                updateBaselineCommand.Transaction = transaction;
                updateBaselineCommand.CommandText =
                    "UPDATE scans " +
                    "SET previous_scan_id = NULL, is_baseline = 1 " +
                    "WHERE scan_id = $scan_id;";

                updateBaselineCommand.Parameters.Add("$scan_id", SqliteType.Text).Value =
                    newBaselineScanId;
                updateBaselineCommand.ExecuteNonQuery();
            }

            for (int index = 0; index < removeCount; index++)
            {
                string scanIdToDelete = scanIds[index];

                using SqliteCommand deleteEntriesCommand = connection.CreateCommand();
                deleteEntriesCommand.Transaction = transaction;
                deleteEntriesCommand.CommandText =
                    "DELETE FROM scan_entries WHERE scan_id = $scan_id;";
                deleteEntriesCommand.Parameters.Add("$scan_id", SqliteType.Text).Value =
                    scanIdToDelete;
                deleteEntriesCommand.ExecuteNonQuery();

                using SqliteCommand deleteScanCommand = connection.CreateCommand();
                deleteScanCommand.Transaction = transaction;
                deleteScanCommand.CommandText =
                    "DELETE FROM scans WHERE scan_id = $scan_id;";
                deleteScanCommand.Parameters.Add("$scan_id", SqliteType.Text).Value =
                    scanIdToDelete;
                deleteScanCommand.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }


        private static void RebuildAllHistoriesAsDeltas(
            SqliteConnection connection)
        {
            List<string> rootPaths = new List<string>();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT DISTINCT root_path " +
                    "FROM scans " +
                    "ORDER BY root_path COLLATE NOCASE ASC;";

                using SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    rootPaths.Add(reader.GetString(0));
                }
            }

            foreach (string rootPath in rootPaths)
            {
                RebuildHistoryAsDeltas(connection, rootPath);
            }
        }

        private static void RebuildHistoryAsDeltas(
            SqliteConnection connection,
            string rootPath)
        {
            List<string> scanIds = new List<string>();

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText =
                    "SELECT scan_id " +
                    "FROM scans " +
                    "WHERE root_path = $root_path COLLATE NOCASE " +
                    "ORDER BY created_utc_ticks ASC;";

                command.Parameters.Add("$root_path", SqliteType.Text).Value = rootPath;

                using SqliteDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    scanIds.Add(reader.GetString(0));
                }
            }

            if (scanIds.Count == 0)
                return;

            int firstRetainedIndex = Math.Max(
                0,
                scanIds.Count - maximumScansPerPath);

            List<string> retainedScanIds = scanIds
                .Skip(firstRetainedIndex)
                .ToList();

            Dictionary<string, EntryData> previousEntries = null;
            string previousScanId = null;

            foreach (string retainedScanId in retainedScanIds)
            {
                Dictionary<string, EntryData> currentEntries =
                    LoadEntryState(connection, retainedScanId);

                RewriteScanAsDelta(
                    connection,
                    retainedScanId,
                    previousScanId,
                    previousEntries,
                    currentEntries);

                previousScanId = retainedScanId;
                previousEntries = currentEntries;
            }

            if (firstRetainedIndex == 0)
                return;

            using SqliteTransaction transaction = connection.BeginTransaction();

            for (int index = 0; index < firstRetainedIndex; index++)
            {
                string scanIdToDelete = scanIds[index];

                using (SqliteCommand deleteEntriesCommand = connection.CreateCommand())
                {
                    deleteEntriesCommand.Transaction = transaction;
                    deleteEntriesCommand.CommandText =
                        "DELETE FROM scan_entries WHERE scan_id = $scan_id;";

                    deleteEntriesCommand.Parameters.Add("$scan_id", SqliteType.Text).Value =
                        scanIdToDelete;

                    deleteEntriesCommand.ExecuteNonQuery();
                }

                using SqliteCommand deleteScanCommand = connection.CreateCommand();
                deleteScanCommand.Transaction = transaction;
                deleteScanCommand.CommandText =
                    "DELETE FROM scans WHERE scan_id = $scan_id;";

                deleteScanCommand.Parameters.Add("$scan_id", SqliteType.Text).Value =
                    scanIdToDelete;

                deleteScanCommand.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        private static void RewriteScanAsDelta(
            SqliteConnection connection,
            string scanId,
            string previousScanId,
            Dictionary<string, EntryData> previousEntries,
            Dictionary<string, EntryData> currentEntries)
        {
            using SqliteTransaction transaction = connection.BeginTransaction();

            using (SqliteCommand deleteEntriesCommand = connection.CreateCommand())
            {
                deleteEntriesCommand.Transaction = transaction;
                deleteEntriesCommand.CommandText =
                    "DELETE FROM scan_entries WHERE scan_id = $scan_id;";

                deleteEntriesCommand.Parameters.Add("$scan_id", SqliteType.Text).Value =
                    scanId;

                deleteEntriesCommand.ExecuteNonQuery();
            }

            if (previousEntries == null)
            {
                foreach (EntryData currentEntry in currentEntries.Values)
                {
                    InsertDeltaEntry(
                        connection,
                        transaction,
                        scanId,
                        currentEntry,
                        ChangeTypeUpsert);
                }
            }
            else
            {
                foreach (KeyValuePair<string, EntryData> currentEntry in currentEntries)
                {
                    if (previousEntries.TryGetValue(
                            currentEntry.Key,
                            out EntryData previousEntry) &&
                        previousEntry.HasSameVersion(currentEntry.Value))
                    {
                        continue;
                    }

                    InsertDeltaEntry(
                        connection,
                        transaction,
                        scanId,
                        currentEntry.Value,
                        ChangeTypeUpsert);
                }

                foreach (KeyValuePair<string, EntryData> previousEntry in previousEntries)
                {
                    if (currentEntries.ContainsKey(previousEntry.Key))
                        continue;

                    InsertDeltaEntry(
                        connection,
                        transaction,
                        scanId,
                        previousEntry.Value,
                        ChangeTypeDelete);
                }
            }

            using SqliteCommand updateScanCommand = connection.CreateCommand();
            updateScanCommand.Transaction = transaction;
            updateScanCommand.CommandText =
                "UPDATE scans " +
                "SET previous_scan_id = $previous_scan_id, is_baseline = $is_baseline " +
                "WHERE scan_id = $scan_id;";

            updateScanCommand.Parameters.Add("$previous_scan_id", SqliteType.Text).Value =
                string.IsNullOrWhiteSpace(previousScanId)
                    ? DBNull.Value
                    : previousScanId;

            updateScanCommand.Parameters.Add("$is_baseline", SqliteType.Integer).Value =
                previousEntries == null ? 1 : 0;

            updateScanCommand.Parameters.Add("$scan_id", SqliteType.Text).Value =
                scanId;

            updateScanCommand.ExecuteNonQuery();
            transaction.Commit();
        }

        private static void CleanupOrphans(SqliteConnection connection)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "DELETE FROM entry_versions " +
                "WHERE version_id NOT IN (" +
                "SELECT version_id FROM scan_entries WHERE change_type = 1); " +
                "DELETE FROM paths " +
                "WHERE path_id NOT IN (SELECT path_id FROM scan_entries);";

            command.ExecuteNonQuery();
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

        private static FileSystemEntry BuildRootEntry(
            ScanHistorySnapshot snapshot,
            Dictionary<string, EntryData> entries)
        {
            FileSystemEntry rootEntry = new FileSystemEntry
            {
                Name = GetEntryName(snapshot.RootPath),
                FullPath = snapshot.RootPath,
                SizeBytes = snapshot.RootSizeBytes,
                IsDirectory = true,
                LastWriteTimeUtc = DateTime.MinValue
            };

            Dictionary<string, FileSystemEntry> materializedEntries =
                new Dictionary<string, FileSystemEntry>(StringComparer.OrdinalIgnoreCase)
                {
                    [rootEntry.FullPath] = rootEntry
                };

            foreach (EntryData entryData in entries.Values
                         .OrderBy(entry => entry.IsDirectory ? 0 : 1)
                         .ThenBy(entry => entry.FullPath, StringComparer.OrdinalIgnoreCase))
            {
                FileSystemEntry entry;

                if (string.Equals(
                        entryData.FullPath,
                        rootEntry.FullPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    entry = rootEntry;
                }
                else
                {
                    entry = new FileSystemEntry
                    {
                        Name = entryData.Name,
                        FullPath = entryData.FullPath,
                        SizeBytes = entryData.SizeBytes,
                        IsDirectory = entryData.IsDirectory,
                        LastWriteTimeUtc = CreateUtcDateTimeOrMinValue(
                            entryData.LastWriteUtcTicks)
                    };
                }

                materializedEntries[entry.FullPath] = entry;
            }

            foreach (FileSystemEntry entry in materializedEntries.Values
                         .Where(entry => !ReferenceEquals(entry, rootEntry))
                         .OrderBy(entry => entry.FullPath, StringComparer.OrdinalIgnoreCase))
            {
                string parentPath = GetParentPath(entry.FullPath);

                if (materializedEntries.TryGetValue(parentPath, out FileSystemEntry parentEntry))
                {
                    parentEntry.Children.Add(entry);
                }

                if (!entry.IsDirectory)
                {
                    rootEntry.AllFiles.Add(entry);
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

        private sealed class EntryData
        {
            public string FullPath { get; set; }
            public string Name { get; set; }
            public bool IsDirectory { get; set; }
            public long SizeBytes { get; set; }
            public long LastWriteUtcTicks { get; set; }

            public static EntryData FromFileSystemEntry(FileSystemEntry entry)
            {
                return new EntryData
                {
                    FullPath = entry.FullPath,
                    Name = string.IsNullOrWhiteSpace(entry.Name)
                        ? GetEntryName(entry.FullPath)
                        : entry.Name,
                    IsDirectory = entry.IsDirectory,
                    SizeBytes = entry.SizeBytes,
                    LastWriteUtcTicks = GetUtcTicks(entry.LastWriteTimeUtc)
                };
            }

            public bool HasSameVersion(EntryData other)
            {
                if (other == null)
                    return false;

                return IsDirectory == other.IsDirectory &&
                       SizeBytes == other.SizeBytes &&
                       LastWriteUtcTicks == other.LastWriteUtcTicks;
            }
        }
    }
}
