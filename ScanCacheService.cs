using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WTF
{
    public sealed class ScanCacheService
    {
        private const int CacheVersion = 1;
        private const int RetentionDays = 30;

        private readonly string _cacheFilePath;
        private readonly Dictionary<string, ScanCacheFileEntry> _fileEntries;
        private readonly HashSet<string> _seenFilePaths;

        private ScanCacheService(string cacheFilePath, Dictionary<string, ScanCacheFileEntry> fileEntries)
        {
            _cacheFilePath = cacheFilePath;
            _fileEntries = fileEntries;
            _seenFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public static ScanCacheService Load(string rootPath)
        {
            string cacheDirectoryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WTF",
                "ScanCache");

            Directory.CreateDirectory(cacheDirectoryPath);

            string cacheFilePath = Path.Combine(
                cacheDirectoryPath,
                CreateCacheFileName(rootPath));

            if (!File.Exists(cacheFilePath))
            {
                return new ScanCacheService(cacheFilePath, new Dictionary<string, ScanCacheFileEntry>(StringComparer.OrdinalIgnoreCase));
            }

            try
            {
                string json = File.ReadAllText(cacheFilePath);
                ScanCacheDatabase database = JsonSerializer.Deserialize<ScanCacheDatabase>(json);

                if (database == null || database.Version != CacheVersion || database.Files == null)
                {
                    return new ScanCacheService(cacheFilePath, new Dictionary<string, ScanCacheFileEntry>(StringComparer.OrdinalIgnoreCase));
                }

                Dictionary<string, ScanCacheFileEntry> fileEntries = new Dictionary<string, ScanCacheFileEntry>(StringComparer.OrdinalIgnoreCase);

                foreach (ScanCacheFileEntry fileEntry in database.Files)
                {
                    if (!string.IsNullOrWhiteSpace(fileEntry.FullPath))
                    {
                        fileEntries[fileEntry.FullPath] = fileEntry;
                    }
                }

                return new ScanCacheService(cacheFilePath, fileEntries);
            }
            catch
            {
                return new ScanCacheService(cacheFilePath, new Dictionary<string, ScanCacheFileEntry>(StringComparer.OrdinalIgnoreCase));
            }
        }

        public long GetLengthAndUpdate(FileInfo fileInfo)
        {
            if (!TryReadFileMetadata(fileInfo, out string fullPath, out long length, out long lastWriteTimeUtcTicks, out int attributes))
            {
                return 0;
            }

            DateTime lastSeenUtc = DateTime.UtcNow;

            if (_fileEntries.TryGetValue(fullPath, out ScanCacheFileEntry existingEntry) &&
                existingEntry.SizeBytes == length &&
                existingEntry.LastWriteTimeUtcTicks == lastWriteTimeUtcTicks &&
                existingEntry.Attributes == attributes)
            {
                existingEntry.LastSeenUtcTicks = lastSeenUtc.Ticks;
                _seenFilePaths.Add(fullPath);
                return existingEntry.SizeBytes;
            }

            _fileEntries[fullPath] = new ScanCacheFileEntry
            {
                FullPath = fullPath,
                SizeBytes = length,
                LastWriteTimeUtcTicks = lastWriteTimeUtcTicks,
                Attributes = attributes,
                LastSeenUtcTicks = lastSeenUtc.Ticks
            };

            _seenFilePaths.Add(fullPath);
            return length;
        }

        public void Save()
        {
            DateTime retentionLimitUtc = DateTime.UtcNow.AddDays(-RetentionDays);

            List<ScanCacheFileEntry> fileEntries = new List<ScanCacheFileEntry>();

            foreach (ScanCacheFileEntry fileEntry in _fileEntries.Values)
            {
                if (!_seenFilePaths.Contains(fileEntry.FullPath))
                    continue;

                if (fileEntry.LastSeenUtcTicks < retentionLimitUtc.Ticks)
                    continue;

                fileEntries.Add(fileEntry);
            }

            ScanCacheDatabase database = new ScanCacheDatabase
            {
                Version = CacheVersion,
                CreatedUtcTicks = DateTime.UtcNow.Ticks,
                Files = fileEntries
            };

            Directory.CreateDirectory(Path.GetDirectoryName(_cacheFilePath));

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = false
            };

            string temporaryFilePath = _cacheFilePath + ".tmp";
            string json = JsonSerializer.Serialize(database, options);

            File.WriteAllText(temporaryFilePath, json, Encoding.UTF8);

            if (File.Exists(_cacheFilePath))
            {
                File.Delete(_cacheFilePath);
            }

            File.Move(temporaryFilePath, _cacheFilePath);
        }

        private static bool TryReadFileMetadata(
            FileInfo fileInfo,
            out string fullPath,
            out long length,
            out long lastWriteTimeUtcTicks,
            out int attributes)
        {
            fullPath = string.Empty;
            length = 0;
            lastWriteTimeUtcTicks = 0;
            attributes = 0;

            try
            {
                fullPath = fileInfo.FullName;
                length = fileInfo.Length;
                lastWriteTimeUtcTicks = fileInfo.LastWriteTimeUtc.Ticks;
                attributes = (int)fileInfo.Attributes;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string CreateCacheFileName(string rootPath)
        {
            string normalizedRootPath = string.IsNullOrWhiteSpace(rootPath)
                ? "unknown"
                : rootPath.Trim().ToUpperInvariant();

            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedRootPath));
            return Convert.ToHexString(hashBytes) + ".json";
        }

        private sealed class ScanCacheDatabase
        {
            public int Version { get; set; }
            public long CreatedUtcTicks { get; set; }
            public List<ScanCacheFileEntry> Files { get; set; }
        }

        private sealed class ScanCacheFileEntry
        {
            public string FullPath { get; set; }
            public long SizeBytes { get; set; }
            public long LastWriteTimeUtcTicks { get; set; }
            public int Attributes { get; set; }
            public long LastSeenUtcTicks { get; set; }
        }
    }
}