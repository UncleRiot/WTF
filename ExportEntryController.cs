using System;
using System.IO;
using System.Windows.Forms;

namespace WTF
{
    public sealed class ExportEntryController
    {
        private readonly CsvExportService _csvExportService;
        private readonly AppSettings _settings;
        private readonly IWin32Window _owner;
        private readonly Action<string> _setStatusText;

        public ExportEntryController(
            CsvExportService csvExportService,
            AppSettings settings,
            IWin32Window owner,
            Action<string> setStatusText)
        {
            _csvExportService = csvExportService ?? throw new ArgumentNullException(nameof(csvExportService));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _owner = owner;
            _setStatusText = setStatusText ?? throw new ArgumentNullException(nameof(setStatusText));
        }

        public void CopyEntryExportToClipboard(FileSystemEntry rootEntry)
        {
            if (rootEntry == null)
                return;

            string csvText = _csvExportService.ExportToString(new[] { rootEntry }, _settings);

            if (string.IsNullOrEmpty(csvText))
                return;

            Clipboard.SetText(csvText, TextDataFormat.UnicodeText);
            _setStatusText(LocalizationService.GetText("Status.ExportCopied") + rootEntry.FullPath);
        }

        public void ExportEntry(FileSystemEntry rootEntry)
        {
            if (rootEntry == null)
                return;

            using SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = _csvExportService.FileFilter,
                FileName = CreateExportFileName(rootEntry)
            };

            DialogResult dialogResult = _owner == null
                ? saveFileDialog.ShowDialog()
                : saveFileDialog.ShowDialog(_owner);

            if (dialogResult != DialogResult.OK)
                return;

            _csvExportService.Export(saveFileDialog.FileName, new[] { rootEntry }, _settings);
            _setStatusText(LocalizationService.GetText("Status.ExportSaved") + saveFileDialog.FileName);
        }

        private string CreateExportFileName(FileSystemEntry entry)
        {
            string name = string.IsNullOrWhiteSpace(entry.Name) ? "wtf-scan" : entry.Name;

            foreach (char invalidFileNameChar in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalidFileNameChar, '_');
            }

            return name + ".csv";
        }
    }
}
