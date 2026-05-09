using System.Collections.Generic;
using System.Linq;

namespace WTF
{
    public sealed class FileSystemEntry
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public long SizeBytes { get; set; }
        public bool IsDirectory { get; set; }
        public List<FileSystemEntry> Children { get; } = new List<FileSystemEntry>();

        public int DirectoryCount
        {
            get { return Children.Count(child => child.IsDirectory); }
        }

        public int FileCount
        {
            get { return Children.Count(child => !child.IsDirectory); }
        }
    }
}