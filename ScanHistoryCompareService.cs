using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WTF
{
    public sealed class ScanHistoryCompareService
    {
        public ScanHistoryComparisonResult Compare(
            ScanHistoryInfo baselineScanInfo,
            ScanHistoryInfo compareScanInfo)
        {
            if (baselineScanInfo == null)
                throw new ArgumentNullException(nameof(baselineScanInfo));

            if (compareScanInfo == null)
                throw new ArgumentNullException(nameof(compareScanInfo));

            ScanHistorySnapshot baselineSnapshot = ScanHistoryService.Load(baselineScanInfo.FilePath);
            ScanHistorySnapshot compareSnapshot = ScanHistoryService.Load(compareScanInfo.FilePath);

            Dictionary<string, FileSystemEntry> baselineFiles = FlattenFiles(baselineSnapshot.RootEntry);
            Dictionary<string, FileSystemEntry> compareFiles = FlattenFiles(compareSnapshot.RootEntry);

            ScanHistoryComparisonResult result = new ScanHistoryComparisonResult
            {
                BaselineScan = baselineScanInfo,
                CompareScan = compareScanInfo,
                BaselineSizeBytes = baselineSnapshot.RootEntry?.SizeBytes ?? baselineSnapshot.RootSizeBytes,
                CompareSizeBytes = compareSnapshot.RootEntry?.SizeBytes ?? compareSnapshot.RootSizeBytes,
                BaselineFileCount = baselineFiles.Count,
                CompareFileCount = compareFiles.Count
            };

            result.SizeDeltaBytes = result.CompareSizeBytes - result.BaselineSizeBytes;

            foreach (KeyValuePair<string, FileSystemEntry> compareFile in compareFiles)
            {
                if (!baselineFiles.TryGetValue(compareFile.Key, out FileSystemEntry baselineFile))
                {
                    ScanHistoryFileChange newFile = CreateFileChange(
                        compareFile.Value,
                        0,
                        compareFile.Value.SizeBytes);

                    result.NewFiles.Add(newFile);
                    AddFolderDeltas(result.FolderGrowth, compareSnapshot.RootPath, newFile.ParentPath, 0, compareFile.Value.SizeBytes, true, false);
                    continue;
                }

                if (baselineFile.SizeBytes != compareFile.Value.SizeBytes)
                {
                    ScanHistoryFileChange changedFile = CreateFileChange(
                        compareFile.Value,
                        baselineFile.SizeBytes,
                        compareFile.Value.SizeBytes);

                    result.ChangedFiles.Add(changedFile);
                    AddFolderDeltas(
                        result.FolderGrowth,
                        compareSnapshot.RootPath,
                        changedFile.ParentPath,
                        baselineFile.SizeBytes,
                        compareFile.Value.SizeBytes,
                        false,
                        true);
                }
            }

            foreach (KeyValuePair<string, FileSystemEntry> baselineFile in baselineFiles)
            {
                if (compareFiles.ContainsKey(baselineFile.Key))
                    continue;

                ScanHistoryFileChange deletedFile = CreateFileChange(
                    baselineFile.Value,
                    baselineFile.Value.SizeBytes,
                    0);

                result.DeletedFiles.Add(deletedFile);
                AddFolderDeltas(result.FolderGrowth, baselineSnapshot.RootPath, deletedFile.ParentPath, baselineFile.Value.SizeBytes, 0, false, false);
            }

            result.NewFileCount = result.NewFiles.Count;
            result.DeletedFileCount = result.DeletedFiles.Count;
            result.ChangedFileCount = result.ChangedFiles.Count;

            result.NewFiles.Sort(CompareFileChangeByLargestDelta);
            result.DeletedFiles.Sort(CompareFileChangeByLargestDelta);
            result.ChangedFiles.Sort(CompareFileChangeByLargestDelta);
            result.FolderGrowth.Sort(CompareFolderGrowthByLargestDelta);

            return result;
        }

        private static Dictionary<string, FileSystemEntry> FlattenFiles(FileSystemEntry rootEntry)
        {
            Dictionary<string, FileSystemEntry> files = new Dictionary<string, FileSystemEntry>(StringComparer.OrdinalIgnoreCase);

            if (rootEntry == null)
                return files;

            AddFiles(rootEntry, files);
            return files;
        }

        private static void AddFiles(FileSystemEntry entry, Dictionary<string, FileSystemEntry> files)
        {
            if (entry == null)
                return;

            if (!entry.IsDirectory)
            {
                AddFile(entry, files);
                return;
            }

            if (entry.AllFiles != null)
            {
                foreach (FileSystemEntry file in entry.AllFiles)
                {
                    AddFile(file, files);
                }
            }

            foreach (FileSystemEntry child in entry.Children)
            {
                AddFiles(child, files);
            }
        }

        private static void AddFile(FileSystemEntry file, Dictionary<string, FileSystemEntry> files)
        {
            if (file == null || file.IsDirectory || string.IsNullOrWhiteSpace(file.FullPath))
                return;

            files[file.FullPath] = file;
        }

        private static ScanHistoryFileChange CreateFileChange(
            FileSystemEntry file,
            long baselineSizeBytes,
            long compareSizeBytes)
        {
            long deltaBytes = compareSizeBytes - baselineSizeBytes;
            string parentPath = string.Empty;

            try
            {
                parentPath = Path.GetDirectoryName(file.FullPath) ?? string.Empty;
            }
            catch
            {
                parentPath = string.Empty;
            }

            return new ScanHistoryFileChange
            {
                Path = file.FullPath,
                ParentPath = parentPath,
                BaselineSizeBytes = baselineSizeBytes,
                CompareSizeBytes = compareSizeBytes,
                DeltaBytes = deltaBytes,
                Size = SizeFormatter.Format(compareSizeBytes),
                BaselineSize = SizeFormatter.Format(baselineSizeBytes),
                CompareSize = SizeFormatter.Format(compareSizeBytes),
                Delta = FormatSignedSize(deltaBytes),
                LastWriteTimeUtc = file.LastWriteTimeUtc == DateTime.MinValue
                    ? string.Empty
                    : TimeZoneInfo.ConvertTimeFromUtc(
                            DateTime.SpecifyKind(file.LastWriteTimeUtc, DateTimeKind.Utc),
                            TimeZoneInfo.Local)
                        .ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture)
            };
        }

        private static void AddFolderDeltas(
            List<ScanHistoryFolderGrowth> folderGrowth,
            string rootPath,
            string path,
            long baselineSizeBytes,
            long compareSizeBytes,
            bool isNewFile,
            bool isChangedFile)
        {
            string currentPath = path;

            while (!string.IsNullOrWhiteSpace(currentPath))
            {
                AddFolderDelta(
                    folderGrowth,
                    currentPath,
                    baselineSizeBytes,
                    compareSizeBytes,
                    isNewFile,
                    isChangedFile);

                if (string.Equals(
                        TrimDirectorySeparator(currentPath),
                        TrimDirectorySeparator(rootPath),
                        StringComparison.OrdinalIgnoreCase))
                    break;

                string parentPath;

                try
                {
                    parentPath = Path.GetDirectoryName(TrimDirectorySeparator(currentPath));
                }
                catch
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(parentPath) ||
                    string.Equals(parentPath, currentPath, StringComparison.OrdinalIgnoreCase))
                    break;

                currentPath = parentPath;
            }
        }

        private static string TrimDirectorySeparator(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static void AddFolderDelta(
            List<ScanHistoryFolderGrowth> folderGrowth,
            string path,
            long baselineSizeBytes,
            long compareSizeBytes,
            bool isNewFile,
            bool isChangedFile)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            long deltaBytes = compareSizeBytes - baselineSizeBytes;
            ScanHistoryFolderGrowth item = folderGrowth.FirstOrDefault(
                existingItem => string.Equals(existingItem.Path, path, StringComparison.OrdinalIgnoreCase));

            if (item == null)
            {
                item = new ScanHistoryFolderGrowth
                {
                    Path = path
                };

                folderGrowth.Add(item);
            }

            item.BaselineSizeBytes += baselineSizeBytes;
            item.CompareSizeBytes += compareSizeBytes;
            item.DeltaBytes += deltaBytes;

            if (isNewFile)
            {
                item.NewFileCount++;
            }

            if (isChangedFile)
            {
                item.ChangedFileCount++;
            }

            item.BaselineSize = SizeFormatter.Format(item.BaselineSizeBytes);
            item.CompareSize = SizeFormatter.Format(item.CompareSizeBytes);
            item.Delta = FormatSignedSize(item.DeltaBytes);
        }

        private static int CompareFileChangeByLargestDelta(
            ScanHistoryFileChange left,
            ScanHistoryFileChange right)
        {
            return Math.Abs(right.DeltaBytes).CompareTo(Math.Abs(left.DeltaBytes));
        }

        private static int CompareFolderGrowthByLargestDelta(
            ScanHistoryFolderGrowth left,
            ScanHistoryFolderGrowth right)
        {
            return Math.Abs(right.DeltaBytes).CompareTo(Math.Abs(left.DeltaBytes));
        }

        private static string FormatSignedSize(long bytes)
        {
            if (bytes > 0)
                return "+" + SizeFormatter.Format(bytes);

            if (bytes < 0)
                return "-" + SizeFormatter.Format(Math.Abs(bytes));

            return SizeFormatter.Format(0);
        }
    }
}
