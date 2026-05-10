namespace WTF
{
    public sealed class EntryChartItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public long SizeBytes { get; set; }
        public string FormattedSize { get; set; }
        public double Percent { get; set; }
    }
}