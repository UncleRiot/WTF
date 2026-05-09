using System.Collections.Generic;

namespace WTF
{
    public interface IExportService
    {
        string FileFilter { get; }

        void Export(string filePath, IEnumerable<FileSystemEntry> entries, AppSettings settings);

        string ExportToString(IEnumerable<FileSystemEntry> entries, AppSettings settings);
    }
}
