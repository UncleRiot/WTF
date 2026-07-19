using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Lucid.Controls;
using Lucid.Controls.GridView;

namespace WTF
{
    // Responsive Lucid table base: shared styling and percentage-based column sizing.
    // Done: AdvancedFeaturesForm.cs
    // Done: Chart_TableGridChart.cs
    // Todo: New Dialogs/functions -> route new responsive Lucid tables through this class.
    // Reminder: Scrollbars, tables not moving/shrinking, when horiz windows-size gets smaler
    // Reminder: Parent-/Container-/Layoutproblem !!!
    public class Chart_ResponsiveTableGrid : LucidDataGridView
    {
        private (string ColumnName, int Percentage)[] _responsiveColumns =
            Array.Empty<(string ColumnName, int Percentage)>();

        private bool _applyingColumnWidths;

        public Chart_ResponsiveTableGrid()
        {
            Dock = DockStyle.Fill;
            AutoSize = false;
            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            ScrollBars = ScrollBars.Vertical;
            BorderStyle = BorderStyle.FixedSingle;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            ParentChanged += Chart_ResponsiveTableGrid_ParentChanged;
            DataBindingComplete += Chart_ResponsiveTableGrid_DataBindingComplete;
            ColumnAdded += Chart_ResponsiveTableGrid_ColumnAdded;
        }

        public void SetResponsiveColumns(
            params (string ColumnName, int Percentage)[] responsiveColumns)
        {
            _responsiveColumns = responsiveColumns ??
                Array.Empty<(string ColumnName, int Percentage)>();

            FitToCurrentBounds();
        }

        public void ApplyLucidStyle()
        {
            DataGridView innerGrid = Controls
                .OfType<DataGridView>()
                .FirstOrDefault();

            if (innerGrid != null)
            {
                DialogTableStyle.Apply(innerGrid);
            }

            FitToCurrentBounds();
        }

        private void Chart_ResponsiveTableGrid_ParentChanged(object sender, EventArgs e)
        {
            FitToCurrentBounds();
        }

        private void Chart_ResponsiveTableGrid_DataBindingComplete(
            object sender,
            DataGridViewBindingCompleteEventArgs e)
        {
            FitToCurrentBounds();
        }

        private void Chart_ResponsiveTableGrid_ColumnAdded(
            object sender,
            DataGridViewColumnEventArgs e)
        {
            e.Column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            e.Column.MinimumWidth = 2;
            FitToCurrentBounds();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            FitToCurrentBounds();
            AlignLucidVerticalScrollBar();
        }

        private void AlignLucidVerticalScrollBar()
        {
            LucidScrollBar verticalScrollBar = Controls
                .OfType<LucidScrollBar>()
                .FirstOrDefault(scrollBar =>
                    scrollBar.ScrollOrientation == LucidScrollOrientation.Vertical);

            if (verticalScrollBar == null || !verticalScrollBar.Visible)
                return;

            const int borderWidth = 1;

            verticalScrollBar.SetBounds(
                Math.Max(
                    borderWidth,
                    ClientSize.Width - verticalScrollBar.Width - borderWidth),
                borderWidth,
                verticalScrollBar.Width,
                Math.Max(0, ClientSize.Height - borderWidth * 2));

            verticalScrollBar.BringToFront();
        }

        private void FitToCurrentBounds()
        {
            if (_applyingColumnWidths)
                return;

            Dock = DockStyle.Fill;
            AutoSize = false;
            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            ScrollBars = ScrollBars.Vertical;
            HorizontalScrollingOffset = 0;

            if (_responsiveColumns.Length == 0 || ClientSize.Width <= 0)
                return;

            int totalPercentage = 0;

            foreach ((string ColumnName, int Percentage) columnDefinition in _responsiveColumns)
            {
                if (!Columns.Contains(columnDefinition.ColumnName))
                    return;

                totalPercentage += Math.Max(0, columnDefinition.Percentage);
            }

            if (totalPercentage <= 0)
                return;

            int availableWidth =
                ClientSize.Width -
                SystemInformation.VerticalScrollBarWidth -
                2;

            availableWidth = Math.Max(
                availableWidth,
                _responsiveColumns.Length * 2);

            SuspendLayout();
            _applyingColumnWidths = true;

            try
            {
                int assignedWidth = 0;

                for (int index = 0; index < _responsiveColumns.Length; index++)
                {
                    (string ColumnName, int Percentage) definition =
                        _responsiveColumns[index];

                    DataGridViewColumn column = Columns[definition.ColumnName];
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    column.MinimumWidth = 2;

                    int width = index == _responsiveColumns.Length - 1
                        ? availableWidth - assignedWidth
                        : availableWidth *
                            Math.Max(0, definition.Percentage) /
                            totalPercentage;

                    column.Width = Math.Max(2, width);
                    assignedWidth += column.Width;
                }
            }
            finally
            {
                _applyingColumnWidths = false;
                ResumeLayout();
            }

            Invalidate();
            AlignLucidVerticalScrollBar();
        }
    }
}
