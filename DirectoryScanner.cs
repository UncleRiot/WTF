using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WTF
{
    public sealed class DirectoryScanner
    {
        private readonly AppSettings _settings;

        private long _scannedBytes;
        private int _scannedDirectories;
        private int _scannedFiles;

        public DirectoryScanner(AppSettings settings)
        {
            _settings = settings;
        }

        public Task<FileSystemEntry> ScanAsync(string rootPath, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() => ScanDirectory(rootPath, progress, cancellationToken), cancellationToken);
        }

        private FileSystemEntry ScanDirectory(string path, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DirectoryInfo directoryInfo = new DirectoryInfo(path);

            FileSystemEntry entry = new FileSystemEntry
            {
                Name = string.IsNullOrWhiteSpace(directoryInfo.Name) ? directoryInfo.FullName : directoryInfo.Name,
                FullPath = directoryInfo.FullName,
                IsDirectory = true
            };

            _scannedDirectories++;
            ReportProgress(path, progress);

            FileInfo[] files = GetFilesSafe(directoryInfo);

            foreach (FileInfo fileInfo in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                long fileLength = 0;

                try
                {
                    fileLength = fileInfo.Length;
                }
                catch
                {
                    fileLength = 0;
                }

                _scannedFiles++;
                _scannedBytes += fileLength;
                entry.SizeBytes += fileLength;

                if (_settings.ShowFilesInTree)
                {
                    entry.Children.Add(new FileSystemEntry
                    {
                        Name = fileInfo.Name,
                        FullPath = fileInfo.FullName,
                        SizeBytes = fileLength,
                        IsDirectory = false
                    });
                }
            }

            DirectoryInfo[] directories = GetDirectoriesSafe(directoryInfo);

            foreach (DirectoryInfo childDirectoryInfo in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_settings.SkipReparsePoints && childDirectoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    continue;

                FileSystemEntry childEntry = ScanDirectory(childDirectoryInfo.FullName, progress, cancellationToken);
                entry.Children.Add(childEntry);
                entry.SizeBytes += childEntry.SizeBytes;
            }

            entry.Children.Sort((left, right) => right.SizeBytes.CompareTo(left.SizeBytes));

            return entry;
        }

        private FileInfo[] GetFilesSafe(DirectoryInfo directoryInfo)
        {
            try
            {
                return directoryInfo.GetFiles();
            }
            catch
            {
                return Array.Empty<FileInfo>();
            }
        }

        private DirectoryInfo[] GetDirectoriesSafe(DirectoryInfo directoryInfo)
        {
            try
            {
                return directoryInfo.GetDirectories()
                    .OrderBy(directory => directory.Name)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<DirectoryInfo>();
            }
        }

        private void ReportProgress(string currentPath, IProgress<ScanProgress> progress)
        {
            progress?.Report(new ScanProgress
            {
                CurrentPath = currentPath,
                ScannedBytes = _scannedBytes,
                ScannedDirectories = _scannedDirectories,
                ScannedFiles = _scannedFiles
            });
        }
    }
}