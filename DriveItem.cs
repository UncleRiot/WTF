namespace WTF
{
    public sealed class DriveItem
    {
        public string DisplayName { get; set; }
        public string RootPath { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}