using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Filesystem.Ntfs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WTF
{
    public sealed class NtfsMftScanner
    {
        private const int ProgressReportIntervalNodes = 5000;

        private readonly AppSettings _settings;

        public NtfsMftScanner(AppSettings settings)
        {
            _settings = settings;
        }

        public static bool IsSupported(string rootPath)
        {
            if (!IsProcessElevated())
                return false;

            try
            {
                string driveRoot = Path.GetPathRoot(rootPath);

                if (string.IsNullOrWhiteSpace(driveRoot))
                    return false;

                DriveInfo driveInfo = new DriveInfo(driveRoot);

                return driveInfo.IsReady &&
                       driveInfo.DriveType == DriveType.Fixed &&
                       string.Equals(driveInfo.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        private static bool IsProcessElevated()
        {
            try
            {
                using System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);

                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
        public Task<FileSystemEntry> ScanAsync(string rootPath, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                string driveRoot = Path.GetPathRoot(rootPath);

                if (string.IsNullOrWhiteSpace(driveRoot))
                {
                    throw new InvalidOperationException(LocalizationService.GetText("Alert.InvalidNtfsDrive"));
                }

                DriveInfo driveInfo = new DriveInfo(driveRoot);
                NtfsReader reader = new NtfsReader(driveInfo, RetrieveMode.Minimal);
                List<INode> nodes = reader.GetNodes(driveRoot);

                FileSystemEntry rootEntry = CreateRootEntry(driveRoot);
                Dictionary<string, FileSystemEntry> directoryEntriesByPath = new Dictionary<string, FileSystemEntry>(StringComparer.OrdinalIgnoreCase)
                {
                    [NormalizeDirectoryPath(rootEntry.FullPath)] = rootEntry
                };

                int scannedDirectories = 1;
                int scannedFiles = 0;
                long scannedBytes = 0;
                int processedNodes = 0;

                foreach (INode node in nodes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (node == null || string.IsNullOrWhiteSpace(node.FullName))
                        continue;

                    string fullPath = NormalizePath(node.FullName);

                    if (string.Equals(NormalizeDirectoryPath(fullPath), NormalizeDirectoryPath(rootEntry.FullPath), StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!fullPath.StartsWith(rootEntry.FullPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool isDirectory = node.Attributes.HasFlag(System.IO.Filesystem.Ntfs.Attributes.Directory);

                    if (isDirectory)
                    {
                        FileSystemEntry directoryEntry = EnsureDirectoryEntry(fullPath, rootEntry, directoryEntriesByPath);

                        if (directoryEntry != null)
                        {
                            scannedDirectories++;
                        }
                    }
                    else
                    {
                        long nodeSize = ConvertNodeSize(node.Size);

                        scannedFiles++;
                        scannedBytes += nodeSize;

                        string parentPath = GetParentDirectoryPath(fullPath);

                        if (!string.IsNullOrWhiteSpace(parentPath))
                        {
                            FileSystemEntry parentEntry = EnsureDirectoryEntry(parentPath, rootEntry, directoryEntriesByPath);

                            if (parentEntry != null)
                            {
                                parentEntry.SizeBytes += nodeSize;

                                if (_settings.ShowFilesInTree)
                                {
                                    parentEntry.Children.Add(new FileSystemEntry
                                    {
                                        Name = Path.GetFileName(fullPath),
                                        FullPath = fullPath,
                                        SizeBytes = nodeSize,
                                        IsDirectory = false
                                    });
                                }
                            }
                        }
                    }

                    processedNodes++;

                    if (processedNodes % ProgressReportIntervalNodes == 0)
                    {
                        progress?.Report(new ScanProgress
                        {
                            CurrentPath = fullPath,
                            ScannedBytes = scannedBytes,
                            ScannedDirectories = scannedDirectories,
                            ScannedFiles = scannedFiles,
                            LiveRootEntry = CreateLiveSnapshot(rootEntry)
                        });
                    }
                }

                PropagateDirectorySizes(rootEntry);
                SortChildrenRecursive(rootEntry);

                progress?.Report(new ScanProgress
                {
                    CurrentPath = LocalizationService.GetText("Status.MftFastScanCompleted"),
                    ScannedBytes = rootEntry.SizeBytes,
                    ScannedDirectories = scannedDirectories,
                    ScannedFiles = scannedFiles,
                    LiveRootEntry = CreateLiveSnapshot(rootEntry)
                });

                return rootEntry;
            }, cancellationToken);
        }

        private FileSystemEntry CreateRootEntry(string rootPath)
        {
            string normalizedRootPath = NormalizeDirectoryPath(rootPath);

            return new FileSystemEntry
            {
                Name = normalizedRootPath,
                FullPath = normalizedRootPath,
                IsDirectory = true
            };
        }
        private long ConvertNodeSize(ulong size)
        {
            if (size > long.MaxValue)
            {
                return long.MaxValue;
            }

            return (long)size;
        }
        private FileSystemEntry EnsureDirectoryEntry(
            string directoryPath,
            FileSystemEntry rootEntry,
            Dictionary<string, FileSystemEntry> directoryEntriesByPath)
        {
            string normalizedDirectoryPath = NormalizeDirectoryPath(directoryPath);

            if (directoryEntriesByPath.TryGetValue(normalizedDirectoryPath, out FileSystemEntry existingEntry))
            {
                return existingEntry;
            }

            if (!normalizedDirectoryPath.StartsWith(rootEntry.FullPath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string parentPath = GetParentDirectoryPath(normalizedDirectoryPath);
            FileSystemEntry parentEntry = string.IsNullOrWhiteSpace(parentPath)
                ? rootEntry
                : EnsureDirectoryEntry(parentPath, rootEntry, directoryEntriesByPath);

            if (parentEntry == null)
            {
                return null;
            }

            FileSystemEntry directoryEntry = new FileSystemEntry
            {
                Name = GetDirectoryName(normalizedDirectoryPath),
                FullPath = normalizedDirectoryPath,
                IsDirectory = true
            };

            parentEntry.Children.Add(directoryEntry);
            directoryEntriesByPath[normalizedDirectoryPath] = directoryEntry;

            return directoryEntry;
        }

        private void PropagateDirectorySizes(FileSystemEntry entry)
        {
            foreach (FileSystemEntry child in entry.Children)
            {
                if (!child.IsDirectory)
                    continue;

                PropagateDirectorySizes(child);
                entry.SizeBytes += child.SizeBytes;
            }
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

        private FileSystemEntry CreateLiveSnapshot(FileSystemEntry rootEntry)
        {
            FileSystemEntry snapshot = new FileSystemEntry
            {
                Name = rootEntry.Name,
                FullPath = rootEntry.FullPath,
                SizeBytes = rootEntry.SizeBytes,
                IsDirectory = true
            };

            foreach (FileSystemEntry child in rootEntry.Children
                         .Where(child => child.IsDirectory || _settings.ShowFilesInTree)
                         .OrderByDescending(child => child.SizeBytes)
                         .ThenBy(child => child.Name)
                         .Take(100))
            {
                snapshot.Children.Add(new FileSystemEntry
                {
                    Name = child.Name,
                    FullPath = child.FullPath,
                    SizeBytes = child.SizeBytes,
                    IsDirectory = child.IsDirectory
                });
            }

            return snapshot;
        }

        private string NormalizePath(string path)
        {
            return Path.GetFullPath(path);
        }

        private string NormalizeDirectoryPath(string path)
        {
            string normalizedPath = Path.GetFullPath(path);

            if (!normalizedPath.EndsWith("\\", StringComparison.Ordinal))
            {
                normalizedPath += "\\";
            }

            return normalizedPath;
        }

        private string GetParentDirectoryPath(string path)
        {
            string normalizedPath = path.TrimEnd('\\');
            string parentPath = Path.GetDirectoryName(normalizedPath);

            if (string.IsNullOrWhiteSpace(parentPath))
            {
                return string.Empty;
            }

            return NormalizeDirectoryPath(parentPath);
        }

        private string GetDirectoryName(string directoryPath)
        {
            string normalizedPath = directoryPath.TrimEnd('\\');
            string name = Path.GetFileName(normalizedPath);

            return string.IsNullOrWhiteSpace(name) ? directoryPath : name;
        }
    }
}