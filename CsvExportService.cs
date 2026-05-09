using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WTF
{
    public sealed class CsvExportService : IExportService
    {
        public string FileFilter
        {
            get { return "CSV files (*.csv)|*.csv"; }
        }

        public void Export(string filePath, IEnumerable<FileSystemEntry> entries)
        {
            using StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8);
            writer.WriteLine("Name;Path;SizeBytes;Size");

            foreach (FileSystemEntry entry in entries)
            {
                WriteEntry(writer, entry);
            }
        }

        private void WriteEntry(StreamWriter writer, FileSystemEntry entry)
        {
            writer.WriteLine(string.Format(
                "\"{0}\";\"{1}\";{2};\"{3}\"",
                Escape(entry.Name),
                Escape(entry.FullPath),
                entry.SizeBytes,
                SizeFormatter.Format(entry.SizeBytes)));

            foreach (FileSystemEntry child in entry.Children)
            {
                WriteEntry(writer, child);
            }
        }

        private string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\"", "\"\"");
        }
    }
}