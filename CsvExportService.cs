using System;
using System.Collections.Generic;
using System.Globalization;
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

        public void Export(string filePath, IEnumerable<FileSystemEntry> entries, AppSettings settings)
        {
            using StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8);
            WriteExport(writer, entries, settings);
        }

        public string ExportToString(IEnumerable<FileSystemEntry> entries, AppSettings settings)
        {
            using StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
            WriteExport(writer, entries, settings);
            return writer.ToString();
        }

        private void WriteExport(TextWriter writer, IEnumerable<FileSystemEntry> entries, AppSettings settings)
        {
            WriteHeader(writer, settings);

            foreach (FileSystemEntry entry in entries)
            {
                WriteEntry(writer, entry, settings, 0);
            }
        }

        private void WriteHeader(TextWriter writer, AppSettings settings)
        {
            List<string> columns = new List<string>();

            if (settings.ExportPath)
            {
                columns.Add("Path");
            }

            columns.Add("Ebene");

            if (settings.ExportSizeGb)
            {
                columns.Add("Size (GB)");
            }

            if (settings.ExportSizeMb)
            {
                columns.Add("Size (MB)");
            }

            writer.WriteLine(string.Join(";", columns));
        }

        private void WriteEntry(TextWriter writer, FileSystemEntry entry, AppSettings settings, int level)
        {
            if (settings.ExportMaxDepth.HasValue && level > settings.ExportMaxDepth.Value)
                return;

            List<string> values = new List<string>();

            if (settings.ExportPath)
            {
                values.Add("\"" + Escape(entry.FullPath) + "\"");
            }

            values.Add(level == 0 ? "Root" : level.ToString(CultureInfo.InvariantCulture));

            if (settings.ExportSizeGb)
            {
                values.Add(FormatDecimal(entry.SizeBytes / 1024D / 1024D / 1024D));
            }

            if (settings.ExportSizeMb)
            {
                values.Add(FormatDecimal(entry.SizeBytes / 1024D / 1024D));
            }

            writer.WriteLine(string.Join(";", values));

            foreach (FileSystemEntry child in entry.Children)
            {
                WriteEntry(writer, child, settings, level + 1);
            }
        }

        private string FormatDecimal(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\"", "\"\"");
        }
    }
}
