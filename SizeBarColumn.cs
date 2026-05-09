using System.Windows.Forms;


namespace WTF
{
    public sealed class SizeBarColumn : DataGridViewColumn
    {
        public SizeBarColumn()
            : base(new SizeBarCell())
        {
            ValueType = typeof(double);
            SortMode = DataGridViewColumnSortMode.Automatic;
        }
    }
}