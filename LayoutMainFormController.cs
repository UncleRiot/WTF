using System;
using System.Drawing;
using System.Windows.Forms;

namespace WTF
{
    public sealed class LayoutMainFormController
    {
        private readonly AppSettings _settings;
        private readonly Form _form;
        private readonly SplitContainer _splitContainerMain;
        private readonly SplitContainer _splitContainerLeft;
        private readonly ToolStripPanel _toolStripPanelMain;
        private readonly ToolStrip _toolStripMain;
        private readonly ToolStrip _toolStripViewMode;
        private readonly ToolStrip _toolStripExport;
        private readonly Panel _panelRightViewHost;
        private readonly Chart_TableGridChart _dataGridViewEntries;
        private readonly Chart_PieChart _pieChartView;
        private readonly Chart_BarChart _barChartView;
        private readonly ToolStripButton _toolStripButtonTable;
        private readonly ToolStripButton _toolStripButtonPieChart;
        private readonly ToolStripButton _toolStripButtonBarChart;
        private ViewMode _viewMode;

        public LayoutMainFormController(
            AppSettings settings,
            Form form,
            SplitContainer splitContainerMain,
            SplitContainer splitContainerLeft,
            ToolStripPanel toolStripPanelMain,
            ToolStrip toolStripMain,
            ToolStrip toolStripViewMode,
            ToolStrip toolStripExport,
            Panel panelRightViewHost,
            Chart_TableGridChart dataGridViewEntries,
            Chart_PieChart pieChartView,
            Chart_BarChart barChartView,
            ToolStripButton toolStripButtonTable,
            ToolStripButton toolStripButtonPieChart,
            ToolStripButton toolStripButtonBarChart,
            ViewMode viewMode)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _splitContainerMain = splitContainerMain ?? throw new ArgumentNullException(nameof(splitContainerMain));
            _splitContainerLeft = splitContainerLeft ?? throw new ArgumentNullException(nameof(splitContainerLeft));
            _toolStripPanelMain = toolStripPanelMain ?? throw new ArgumentNullException(nameof(toolStripPanelMain));
            _toolStripMain = toolStripMain ?? throw new ArgumentNullException(nameof(toolStripMain));
            _toolStripViewMode = toolStripViewMode ?? throw new ArgumentNullException(nameof(toolStripViewMode));
            _toolStripExport = toolStripExport ?? throw new ArgumentNullException(nameof(toolStripExport));
            _panelRightViewHost = panelRightViewHost ?? throw new ArgumentNullException(nameof(panelRightViewHost));
            _dataGridViewEntries = dataGridViewEntries ?? throw new ArgumentNullException(nameof(dataGridViewEntries));
            _pieChartView = pieChartView ?? throw new ArgumentNullException(nameof(pieChartView));
            _barChartView = barChartView ?? throw new ArgumentNullException(nameof(barChartView));
            _toolStripButtonTable = toolStripButtonTable ?? throw new ArgumentNullException(nameof(toolStripButtonTable));
            _toolStripButtonPieChart = toolStripButtonPieChart ?? throw new ArgumentNullException(nameof(toolStripButtonPieChart));
            _toolStripButtonBarChart = toolStripButtonBarChart ?? throw new ArgumentNullException(nameof(toolStripButtonBarChart));
            _viewMode = viewMode;
        }

        public void SavePersistentSettings(bool suspendPersistentSettingsSave)
        {
            if (suspendPersistentSettingsSave)
                return;

            SaveViewSettings();
            _settings.Save();
        }

        public void ApplyMainWindowSettings()
        {
            if (!_settings.HasMainWindowBounds)
                return;

            if (_settings.MainWindowWidth < _form.MinimumSize.Width || _settings.MainWindowHeight < _form.MinimumSize.Height)
                return;

            Rectangle savedBounds = new Rectangle(
                _settings.MainWindowLeft,
                _settings.MainWindowTop,
                _settings.MainWindowWidth,
                _settings.MainWindowHeight);

            if (!IsVisibleOnAnyScreen(savedBounds))
                return;

            _form.StartPosition = FormStartPosition.Manual;
            _form.Bounds = savedBounds;

            if (_settings.MainWindowMaximized)
            {
                _form.WindowState = FormWindowState.Maximized;
            }
        }

        public void SaveMainWindowSettings()
        {
            Rectangle bounds = _form.WindowState == FormWindowState.Normal
                ? _form.Bounds
                : _form.RestoreBounds;

            _settings.HasMainWindowBounds = true;
            _settings.MainWindowLeft = bounds.Left;
            _settings.MainWindowTop = bounds.Top;
            _settings.MainWindowWidth = bounds.Width;
            _settings.MainWindowHeight = bounds.Height;
            _settings.MainWindowMaximized = _form.WindowState == FormWindowState.Maximized;
        }

        public void SaveSplitterLayout()
        {
            _settings.HasSplitterLayout = true;
            _settings.SplitContainerMainDistance = _splitContainerMain.SplitterDistance;
            _settings.SplitContainerLeftDistance = _splitContainerLeft.Height - _splitContainerLeft.SplitterDistance - _splitContainerLeft.SplitterWidth;
        }

        public void ApplySplitterLayout()
        {
            if (!_settings.HasSplitterLayout)
                return;

            if (_settings.SplitContainerMainDistance >= _splitContainerMain.Panel1MinSize &&
                _settings.SplitContainerMainDistance <= _splitContainerMain.Width - _splitContainerMain.Panel2MinSize)
            {
                _splitContainerMain.SplitterDistance = _settings.SplitContainerMainDistance;
            }

            int splitContainerLeftDistance = _splitContainerLeft.Height - _settings.SplitContainerLeftDistance - _splitContainerLeft.SplitterWidth;

            if (splitContainerLeftDistance >= _splitContainerLeft.Panel1MinSize &&
                splitContainerLeftDistance <= _splitContainerLeft.Height - _splitContainerLeft.Panel2MinSize)
            {
                _splitContainerLeft.SplitterDistance = splitContainerLeftDistance;
            }
        }

        public void ApplyDefaultToolStripLayout()
        {
            _toolStripPanelMain.Join(_toolStripMain, 0, 0);
            _toolStripPanelMain.Join(_toolStripViewMode, 390, 0);
            _toolStripPanelMain.Join(_toolStripExport, 610, 0);
        }

        public void ApplyToolStripLayout()
        {
            if (!_settings.HasToolStripLayout)
                return;

            if (_settings.ToolStripLayoutVersion != 1)
                return;

            _toolStripPanelMain.Join(
                _toolStripMain,
                Math.Max(0, _settings.ToolStripMainLeft),
                Math.Max(0, _settings.ToolStripMainTop));

            _toolStripPanelMain.Join(
                _toolStripViewMode,
                Math.Max(0, _settings.ToolStripViewModeLeft),
                Math.Max(0, _settings.ToolStripViewModeTop));

            _toolStripPanelMain.Join(
                _toolStripExport,
                Math.Max(0, _settings.ToolStripExportLeft),
                Math.Max(0, _settings.ToolStripExportTop));
        }

        public void SaveToolStripLayout()
        {
            _settings.HasToolStripLayout = true;
            _settings.ToolStripLayoutVersion = 1;

            _settings.ToolStripMainLeft = _toolStripMain.Left;
            _settings.ToolStripMainTop = _toolStripMain.Top;

            _settings.ToolStripViewModeLeft = _toolStripViewMode.Left;
            _settings.ToolStripViewModeTop = _toolStripViewMode.Top;

            _settings.ToolStripExportLeft = _toolStripExport.Left;
            _settings.ToolStripExportTop = _toolStripExport.Top;
        }

        public void SaveViewSettings()
        {
            _settings.SelectedViewMode = _viewMode;
        }

        public void BindGrid(FileSystemEntry entry)
        {
            _dataGridViewEntries.SetEntry(entry);
            _pieChartView.SetEntry(entry);
            _barChartView.SetEntry(entry);
            UpdateRightView();
        }

        public void SetViewMode(ViewMode viewMode, bool suspendPersistentSettingsSave)
        {
            _viewMode = viewMode;
            _settings.SelectedViewMode = viewMode;
            UpdateViewModeButtons();
            UpdateRightView();
            SavePersistentSettings(suspendPersistentSettingsSave);
        }

        public void UpdateRightViewBounds()
        {
            if (_panelRightViewHost == null)
                return;

            if (_dataGridViewEntries != null)
            {
                _dataGridViewEntries.Dock = DockStyle.Fill;
                _dataGridViewEntries.ApplyEntryGridColumnWidths();
            }

            if (_pieChartView != null)
            {
                _pieChartView.Dock = DockStyle.Fill;
                _pieChartView.Invalidate();
            }

            if (_barChartView != null)
            {
                _barChartView.Dock = DockStyle.Fill;
                _barChartView.Invalidate();
            }
        }

        private void UpdateViewModeButtons()
        {
            _toolStripButtonTable.Checked = _viewMode == ViewMode.Table;
            _toolStripButtonPieChart.Checked = _viewMode == ViewMode.PieChart;
            _toolStripButtonBarChart.Checked = _viewMode == ViewMode.BarChart;
        }

        private void UpdateRightView()
        {
            UpdateRightViewBounds();

            _dataGridViewEntries.Visible = _viewMode == ViewMode.Table;
            _pieChartView.Visible = _viewMode == ViewMode.PieChart;
            _barChartView.Visible = _viewMode == ViewMode.BarChart;

            if (_dataGridViewEntries.Visible)
            {
                _dataGridViewEntries.BringToFront();
            }
            else if (_pieChartView.Visible)
            {
                _pieChartView.BringToFront();
                _pieChartView.Invalidate();
            }
            else if (_barChartView.Visible)
            {
                _barChartView.BringToFront();
                _barChartView.Invalidate();
            }
        }

        private bool IsVisibleOnAnyScreen(Rectangle bounds)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(bounds))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
