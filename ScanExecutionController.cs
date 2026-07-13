using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WTF
{
    public sealed class ScanExecutionController
    {
        private readonly AppSettings _settings;
        private readonly StatusMainFormController _statusMainFormController;

        public ScanExecutionController(AppSettings settings, StatusMainFormController statusMainFormController)
        {
            _settings = settings;
            _statusMainFormController = statusMainFormController;
        }

        public async Task<FileSystemEntry> ScanAsync(
            string rootPath,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken,
            PauseToken pauseToken,
            Action<string> statusKeyChanged = null)
        {
            DirectoryScanner directoryScanner = new DirectoryScanner(_settings);
            NtQueryDirectoryScanner ntQueryDirectoryScanner = new NtQueryDirectoryScanner(_settings);

            if (IsRootDrivePath(rootPath) && NtfsMftScanner.IsSupported(rootPath))
            {
                try
                {
                    SetStatusTextByKey("Status.MftFastScanRunning", statusKeyChanged);
                    NtfsMftScanner ntfsMftScanner = new NtfsMftScanner(_settings);
                    return await ntfsMftScanner.ScanAsync(rootPath, progress, cancellationToken, pauseToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception mftException)
                {
                    AppAlertLog.AddWarning(
                        LocalizationService.GetText("Alert.Scan"),
                        LocalizationService.Format("Alert.MftUnavailable", mftException.Message));

                    return await ScanWithNtQueryFallbackAsync(
                        rootPath,
                        progress,
                        cancellationToken,
                        ntQueryDirectoryScanner,
                        directoryScanner,
                        "Status.MftUnavailableNtQuery",
                        pauseToken,
                        statusKeyChanged);
                }
            }

            return await ScanWithNtQueryFallbackAsync(
                rootPath,
                progress,
                cancellationToken,
                ntQueryDirectoryScanner,
                directoryScanner,
                "Status.NtQueryRunning",
                pauseToken,
                statusKeyChanged);
        }

        private async Task<FileSystemEntry> ScanWithNtQueryFallbackAsync(
            string rootPath,
            IProgress<ScanProgress> progress,
            CancellationToken cancellationToken,
            NtQueryDirectoryScanner ntQueryDirectoryScanner,
            DirectoryScanner directoryScanner,
            string ntQueryStatusKey,
            PauseToken pauseToken,
            Action<string> statusKeyChanged)
        {
            try
            {
                SetStatusTextByKey(ntQueryStatusKey, statusKeyChanged);
                return await ntQueryDirectoryScanner.ScanAsync(rootPath, progress, cancellationToken, pauseToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ntQueryException)
            {
                AppAlertLog.AddWarning(
                    LocalizationService.GetText("Alert.Scan"),
                    LocalizationService.Format("Alert.NtQueryUnavailable", ntQueryException.Message));

                SetStatusTextByKey("Status.NtQueryUnavailableNormal", statusKeyChanged);
                return await directoryScanner.ScanAsync(rootPath, progress, cancellationToken, pauseToken);
            }
        }



        private void SetStatusTextByKey(string statusKey, Action<string> statusKeyChanged)
        {
            if (statusKeyChanged != null)
            {
                statusKeyChanged(statusKey);
                return;
            }

            _statusMainFormController.SetStatusTextByKey(statusKey);
        }
        private static bool IsRootDrivePath(string rootPath)
        {
            string pathRoot = Path.GetPathRoot(rootPath);

            return !string.IsNullOrWhiteSpace(pathRoot) &&
                string.Equals(
                    Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    pathRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
