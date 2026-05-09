using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WTF
{
    public sealed class DirectoryScanner
    {
        private const int ProgressReportIntervalMilliseconds = 1000;
        private const int LiveSnapshotDepth = 1;
        private const int MaxLiveChildrenPerDirectory = 100;

        private readonly AppSettings _settings;

        private long _scannedBytes;
        private int _scannedDirectories;
        private int _scannedFiles;
        private long _lastProgressReportTickCount;
        private FileSystemEntry _liveRootEntry;
        private ScanCacheService _scanCacheService;

        public DirectoryScanner(AppSettings settings)
        {
            _settings = settings;
        }

        public Task<FileSystemEntry> ScanAsync(string rootPath, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                _scanCacheService = ScanCacheService.Load(rootPath);

                FileSystemEntry rootEntry = CreateDirectoryEntry(rootPath);
                _liveRootEntry = rootEntry;
                _scannedDirectories++;

                ReportProgress(rootPath, progress, true);
                ScanDirectoryContents(rootEntry, progress, cancellationToken, null);
                SortChildrenRecursive(rootEntry);
                ReportProgress(rootPath, progress, true);

                ReportProgress("Scan abgeschlossen, Cache wird gespeichert...", progress, true);
                _scanCacheService.Save();

                return rootEntry;
            }, cancellationToken);
        }

        private void ScanDirectoryContents(FileSystemEntry entry, IProgress<ScanProgress> progress, CancellationToken cancellationToken, Action<long> addSizeToAncestors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (string fileSystemEntryPath in EnumerateFileSystemEntriesSafe(entry.FullPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!TryGetAttributes(fileSystemEntryPath, out FileAttributes attributes))
                    continue;

                bool isDirectory = attributes.HasFlag(FileAttributes.Directory);

                if (isDirectory)
                {
                    if (_settings.SkipReparsePoints && attributes.HasFlag(FileAttributes.ReparsePoint))
                        continue;

                    FileSystemEntry childEntry = CreateDirectoryEntry(fileSystemEntryPath);
                    entry.Children.Add(childEntry);
                    _scannedDirectories++;

                    ReportProgress(childEntry.FullPath, progress, false);

                    ScanDirectoryContents(
                        childEntry,
                        progress,
                        cancellationToken,
                        sizeDelta =>
                        {
                            entry.SizeBytes += sizeDelta;
                            addSizeToAncestors?.Invoke(sizeDelta);
                        });

                    ReportProgress(childEntry.FullPath, progress, false);
                    continue;
                }

                long fileLength = _scanCacheService.GetLengthAndUpdate(new FileInfo(fileSystemEntryPath));

                _scannedFiles++;
                _scannedBytes += fileLength;
                entry.SizeBytes += fileLength;
                addSizeToAncestors?.Invoke(fileLength);

                if (_settings.ShowFilesInTree)
                {
                    entry.Children.Add(new FileSystemEntry
                    {
                        Name = Path.GetFileName(fileSystemEntryPath),
                        FullPath = fileSystemEntryPath,
                        SizeBytes = fileLength,
                        IsDirectory = false
                    });
                }

                ReportProgress(fileSystemEntryPath, progress, false);
            }
        }

        private IEnumerable<string> EnumerateFileSystemEntriesSafe(string directoryPath)
        {
            EnumerationOptions enumerationOptions = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = false,
                RecurseSubdirectories = false
            };

            IEnumerator<string> enumerator;

            try
            {
                enumerator = Directory.EnumerateFileSystemEntries(directoryPath, "*", enumerationOptions).GetEnumerator();
            }
            catch
            {
                yield break;
            }

            using (enumerator)
            {
                while (true)
                {
                    string currentPath;

                    try
                    {
                        if (!enumerator.MoveNext())
                            yield break;

                        currentPath = enumerator.Current;
                    }
                    catch
                    {
                        yield break;
                    }

                    yield return currentPath;
                }
            }
        }

        private bool TryGetAttributes(string path, out FileAttributes attributes)
        {
            attributes = 0;

            try
            {
                attributes = File.GetAttributes(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private FileSystemEntry CreateDirectoryEntry(string path)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            return new FileSystemEntry
            {
                Name = string.IsNullOrWhiteSpace(directoryInfo.Name) ? directoryInfo.FullName : directoryInfo.Name,
                FullPath = directoryInfo.FullName,
                IsDirectory = true
            };
        }

        private void ReportProgress(string currentPath, IProgress<ScanProgress> progress, bool force)
        {
            if (!force && !ShouldReportProgress())
                return;

            progress?.Report(new ScanProgress
            {
                CurrentPath = currentPath,
                ScannedBytes = _scannedBytes,
                ScannedDirectories = _scannedDirectories,
                ScannedFiles = _scannedFiles,
                LiveRootEntry = CreateLiveSnapshot(_liveRootEntry, LiveSnapshotDepth)
            });
        }

        private bool ShouldReportProgress()
        {
            long currentTickCount = Environment.TickCount64;

            if (currentTickCount - _lastProgressReportTickCount < ProgressReportIntervalMilliseconds)
            {
                return false;
            }

            _lastProgressReportTickCount = currentTickCount;
            return true;
        }

        private FileSystemEntry CreateLiveSnapshot(FileSystemEntry entry, int remainingDepth)
        {
            if (entry == null)
            {
                return null;
            }

            FileSystemEntry snapshot = new FileSystemEntry
            {
                Name = entry.Name,
                FullPath = entry.FullPath,
                SizeBytes = entry.SizeBytes,
                IsDirectory = entry.IsDirectory
            };

            if (remainingDepth <= 0)
            {
                return snapshot;
            }

            foreach (FileSystemEntry child in entry.Children
                         .Where(child => child.IsDirectory || _settings.ShowFilesInTree)
                         .OrderByDescending(child => child.SizeBytes)
                         .ThenBy(child => child.Name)
                         .Take(MaxLiveChildrenPerDirectory))
            {
                snapshot.Children.Add(CreateLiveSnapshot(child, remainingDepth - 1));
            }

            return snapshot;
        }

        private void SortChildrenRecursive(FileSystemEntry entry)
        {
            foreach (FileSystemEntry child in entry.Children)
            {
                if (child.IsDirectory)
                {
                    SortChildrenRecursive(child);
                }
            }

            entry.Children.Sort((left, right) =>
            {
                int sizeCompare = right.SizeBytes.CompareTo(left.SizeBytes);

                if (sizeCompare != 0)
                {
                    return sizeCompare;
                }

                return string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}