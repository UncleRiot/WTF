using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace WTF
{
    public sealed class NtQueryDirectoryScanner
    {
        private const int ProgressReportIntervalMilliseconds = 1000;
        private const int DirectoryQueryBufferSize = 4 * 1024 * 1024;
        private const int FileFullDirectoryInformationClass = 2;
        private const int FileFullDirectoryInformationFileNameOffset = 68;
        private const int FileIdFullDirectoryInformationClass = 38;
        private const int FileIdFullDirectoryInformationFileNameOffset = 80;

        private const uint FILE_LIST_DIRECTORY = 0x0001;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint FILE_SHARE_DELETE = 0x00000004;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        private const int STATUS_SUCCESS = 0x00000000;
        private const int STATUS_NO_MORE_FILES = unchecked((int)0x80000006);
        private const int STATUS_NO_SUCH_FILE = unchecked((int)0xC000000F);
        private const int STATUS_INVALID_INFO_CLASS = unchecked((int)0xC0000003);
        private const int STATUS_INVALID_PARAMETER = unchecked((int)0xC000000D);

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private readonly AppSettings _settings;

        private long _scannedBytes;
        private int _scannedDirectories;
        private int _scannedFiles;
        private long _lastProgressReportTickCount;
        private int _pendingDirectoryCount;
        private BlockingCollection<WorkItem> _workQueue;

        public NtQueryDirectoryScanner(AppSettings settings)
        {
            _settings = settings;
        }

        public Task<FileSystemEntry> ScanAsync(string rootPath, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                FileSystemEntry rootEntry = CreateDirectoryEntry(rootPath);

                _scannedBytes = 0;
                _scannedDirectories = 1;
                _scannedFiles = 0;
                _lastProgressReportTickCount = 0;
                _pendingDirectoryCount = 1;
                _workQueue = new BlockingCollection<WorkItem>();

                ReportProgress(rootPath, progress, true);

                int workerCount = Math.Max(1, Math.Min(Environment.ProcessorCount, 8));
                Task[] workerTasks = new Task[workerCount];

                using CancellationTokenRegistration cancellationTokenRegistration = cancellationToken.Register(() =>
                {
                    if (_workQueue != null && !_workQueue.IsAddingCompleted)
                    {
                        _workQueue.CompleteAdding();
                    }
                });

                for (int workerIndex = 0; workerIndex < workerTasks.Length; workerIndex++)
                {
                    workerTasks[workerIndex] = Task.Run(() => WorkerLoop(progress, cancellationToken), cancellationToken);
                }

                _workQueue.Add(new WorkItem(rootEntry), cancellationToken);

                try
                {
                    Task.WaitAll(workerTasks);
                }
                catch (AggregateException aggregateException)
                {
                    foreach (Exception innerException in aggregateException.InnerExceptions)
                    {
                        if (innerException is OperationCanceledException)
                        {
                            throw new OperationCanceledException(cancellationToken);
                        }
                    }

                    throw;
                }

                FinalizeDirectorySizes(rootEntry);
                SortChildrenRecursive(rootEntry);
                ReportProgress(rootPath, progress, true);

                return rootEntry;
            }, cancellationToken);
        }

        private void WorkerLoop(IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            IntPtr buffer = Marshal.AllocHGlobal(DirectoryQueryBufferSize);

            try
            {
                foreach (WorkItem workItem in _workQueue.GetConsumingEnumerable(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        ProcessDirectory(workItem.Entry, buffer, DirectoryQueryBufferSize, progress, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref _pendingDirectoryCount) == 0)
                        {
                            _workQueue.CompleteAdding();
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private void ProcessDirectory(FileSystemEntry directoryEntry, IntPtr buffer, int bufferLength, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            using SafeFileHandle directoryHandle = OpenDirectoryHandle(directoryEntry.FullPath);

            if (directoryHandle.IsInvalid)
                return;

            bool restartScan = true;
            int fileInformationClass = FileIdFullDirectoryInformationClass;
            int fileNameOffset = FileIdFullDirectoryInformationFileNameOffset;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                IO_STATUS_BLOCK ioStatusBlock = new IO_STATUS_BLOCK();

                int status = NtQueryDirectoryFile(
                    directoryHandle,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    ref ioStatusBlock,
                    buffer,
                    (uint)bufferLength,
                    fileInformationClass,
                    false,
                    IntPtr.Zero,
                    restartScan);

                restartScan = false;

                if (IsFileInformationClassUnsupported(status) && fileInformationClass == FileIdFullDirectoryInformationClass)
                {
                    fileInformationClass = FileFullDirectoryInformationClass;
                    fileNameOffset = FileFullDirectoryInformationFileNameOffset;
                    restartScan = true;
                    continue;
                }

                if (status == STATUS_NO_MORE_FILES || status == STATUS_NO_SUCH_FILE)
                    return;

                if (status < STATUS_SUCCESS)
                    return;

                ParseDirectoryBuffer(directoryEntry, buffer, fileNameOffset, progress, cancellationToken);
            }
        }

        private void ParseDirectoryBuffer(FileSystemEntry directoryEntry, IntPtr buffer, int fileNameOffset, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            IntPtr currentEntryPointer = buffer;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                uint nextEntryOffset = (uint)Marshal.ReadInt32(currentEntryPointer, 0);
                long endOfFile = Marshal.ReadInt64(currentEntryPointer, 40);
                FileAttributes attributes = (FileAttributes)Marshal.ReadInt32(currentEntryPointer, 56);
                int fileNameLength = Marshal.ReadInt32(currentEntryPointer, 60);

                if (fileNameLength > 0)
                {
                    string name = Marshal.PtrToStringUni(
                        IntPtr.Add(currentEntryPointer, fileNameOffset),
                        fileNameLength / 2);

                    if (!string.IsNullOrWhiteSpace(name) && name != "." && name != "..")
                    {
                        AddDirectoryEntryChild(
                            directoryEntry,
                            name,
                            attributes,
                            endOfFile,
                            progress,
                            cancellationToken);
                    }
                }

                if (nextEntryOffset == 0)
                    break;

                currentEntryPointer = IntPtr.Add(currentEntryPointer, (int)nextEntryOffset);
            }
        }

        private void AddDirectoryEntryChild(FileSystemEntry directoryEntry, string name, FileAttributes attributes, long sizeBytes, IProgress<ScanProgress> progress, CancellationToken cancellationToken)
        {
            bool isDirectory = attributes.HasFlag(FileAttributes.Directory);
            string fullPath = Path.Combine(directoryEntry.FullPath, name);

            if (isDirectory)
            {
                if (_settings.SkipReparsePoints && attributes.HasFlag(FileAttributes.ReparsePoint))
                    return;

                FileSystemEntry childEntry = new FileSystemEntry
                {
                    Name = name,
                    FullPath = fullPath,
                    IsDirectory = true
                };

                AddChildEntry(directoryEntry, childEntry);
                Interlocked.Increment(ref _scannedDirectories);
                Interlocked.Increment(ref _pendingDirectoryCount);

                try
                {
                    _workQueue.Add(new WorkItem(childEntry), cancellationToken);
                }
                catch
                {
                    Interlocked.Decrement(ref _pendingDirectoryCount);
                    throw;
                }

                ReportProgress(fullPath, progress, false);
                return;
            }

            long normalizedSizeBytes = Math.Max(0, sizeBytes);

            Interlocked.Increment(ref _scannedFiles);
            Interlocked.Add(ref _scannedBytes, normalizedSizeBytes);

            if (_settings.ShowFilesInTree)
            {
                AddChildEntry(
                    directoryEntry,
                    new FileSystemEntry
                    {
                        Name = name,
                        FullPath = fullPath,
                        SizeBytes = normalizedSizeBytes,
                        IsDirectory = false
                    });
            }
            else
            {
                lock (directoryEntry)
                {
                    directoryEntry.SizeBytes += normalizedSizeBytes;
                }
            }

            ReportProgress(fullPath, progress, false);
        }

        private static long FileTimeToUtcTicks(long fileTime)
        {
            try
            {
                return DateTime.FromFileTimeUtc(fileTime).Ticks;
            }
            catch
            {
                return 0;
            }
        }

        private static bool IsFileInformationClassUnsupported(int status)
        {
            return status == STATUS_INVALID_INFO_CLASS || status == STATUS_INVALID_PARAMETER;
        }

        private void AddChildEntry(FileSystemEntry parentEntry, FileSystemEntry childEntry)
        {
            lock (parentEntry.Children)
            {
                parentEntry.Children.Add(childEntry);
            }
        }

        private SafeFileHandle OpenDirectoryHandle(string directoryPath)
        {
            return CreateFile(
                NormalizePathForWin32(directoryPath),
                FILE_LIST_DIRECTORY,
                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);
        }

        private static string NormalizePathForWin32(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
                return path;

            if (path.StartsWith(@"\\", StringComparison.Ordinal))
                return @"\\?\UNC\" + path.Substring(2);

            return @"\\?\" + path;
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

        private long FinalizeDirectorySizes(FileSystemEntry entry)
        {
            if (!entry.IsDirectory)
                return entry.SizeBytes;

            long sizeBytes = entry.SizeBytes;

            foreach (FileSystemEntry child in entry.Children)
            {
                sizeBytes += FinalizeDirectorySizes(child);
            }

            entry.SizeBytes = sizeBytes;
            return sizeBytes;
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

        private void ReportProgress(string currentPath, IProgress<ScanProgress> progress, bool force)
        {
            if (!force && !ShouldReportProgress())
                return;

            progress?.Report(new ScanProgress
            {
                CurrentPath = currentPath,
                ScannedBytes = Interlocked.Read(ref _scannedBytes),
                ScannedDirectories = Volatile.Read(ref _scannedDirectories),
                ScannedFiles = Volatile.Read(ref _scannedFiles),
                LiveRootEntry = null,
                IsCacheVerification = false,
                IsCacheSavePhase = false
            });
        }

        private bool ShouldReportProgress()
        {
            long currentTickCount = Environment.TickCount64;
            long lastProgressReportTickCount = Volatile.Read(ref _lastProgressReportTickCount);

            if (currentTickCount - lastProgressReportTickCount < ProgressReportIntervalMilliseconds)
                return false;

            return Interlocked.CompareExchange(
                ref _lastProgressReportTickCount,
                currentTickCount,
                lastProgressReportTickCount) == lastProgressReportTickCount;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryDirectoryFile(
            SafeFileHandle fileHandle,
            IntPtr eventHandle,
            IntPtr apcRoutine,
            IntPtr apcContext,
            ref IO_STATUS_BLOCK ioStatusBlock,
            IntPtr fileInformation,
            uint length,
            int fileInformationClass,
            [MarshalAs(UnmanagedType.U1)] bool returnSingleEntry,
            IntPtr fileName,
            [MarshalAs(UnmanagedType.U1)] bool restartScan);

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_STATUS_BLOCK
        {
            public IntPtr Status;
            public IntPtr Information;
        }

        private sealed class WorkItem
        {
            public WorkItem(FileSystemEntry entry)
            {
                Entry = entry;
            }

            public FileSystemEntry Entry { get; }
        }
    }
}