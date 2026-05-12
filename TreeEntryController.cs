using System;
using System.Windows.Forms;

namespace WTF
{
    public sealed class TreeEntryController
    {
        private readonly TreeEntrySizeBarView _treeViewEntries;
        private readonly ContextMenuStrip _contextMenuStripTreeEntries;
        private readonly ToolStripMenuItem _contextMenuItemOpenInExplorer;
        private readonly ToolStripMenuItem _contextMenuItemExport;
        private readonly ToolStripMenuItem _contextMenuItemCopyToClipboard;
        private readonly Action<FileSystemEntry> _selectedEntryChanged;
        private System.Windows.Forms.Timer _liveTreeUpdateTimer;
        private ScanProgress _pendingLiveTreeScanProgress;
        private bool _liveTreeUpdateInProgress;

        public TreeEntryController(
            TreeEntrySizeBarView treeViewEntries,
            ImageList imageListEntries,
            ShellIconService shellIconService,
            ContextMenuStrip contextMenuStripTreeEntries,
            ToolStripMenuItem contextMenuItemOpenInExplorer,
            ToolStripMenuItem contextMenuItemExport,
            ToolStripMenuItem contextMenuItemCopyToClipboard,
            Action<FileSystemEntry> selectedEntryChanged,
            Func<FileSystemEntry> currentRootEntryProvider)
        {
            _treeViewEntries = treeViewEntries;
            _contextMenuStripTreeEntries = contextMenuStripTreeEntries;
            _contextMenuItemOpenInExplorer = contextMenuItemOpenInExplorer;
            _contextMenuItemExport = contextMenuItemExport;
            _contextMenuItemCopyToClipboard = contextMenuItemCopyToClipboard;
            _selectedEntryChanged = selectedEntryChanged;

            _treeViewEntries.EntryImageList = imageListEntries;
            _treeViewEntries.ShellIconService = shellIconService;
            _treeViewEntries.SelectedEntryChanged += treeViewEntries_SelectedEntryChanged;
            _treeViewEntries.EntryMouseClick += treeViewEntries_EntryMouseClick;

            ConfigureLiveTreeUpdateTimer();
        }

        public FileSystemEntry ContextMenuEntry { get; private set; }

        public void ClearEntries()
        {
            ContextMenuEntry = null;
            _treeViewEntries.ClearEntries();
        }

        public void ClearPendingLiveTreeUpdate()
        {
            _pendingLiveTreeScanProgress = null;
        }

        public void FlushPendingLiveTreeUpdate()
        {
            if (_liveTreeUpdateInProgress)
                return;

            ScanProgress scanProgress = _pendingLiveTreeScanProgress;

            if (scanProgress == null)
                return;

            _pendingLiveTreeScanProgress = null;
            _liveTreeUpdateInProgress = true;

            try
            {
                ApplyScanProgressToLiveTree(scanProgress);
            }
            finally
            {
                _liveTreeUpdateInProgress = false;
            }
        }

        public void QueueLiveTreeUpdate(ScanProgress scanProgress)
        {
            if (scanProgress == null)
                return;

            if (scanProgress.LiveRootEntry == null)
                return;

            _pendingLiveTreeScanProgress = scanProgress;

            if (_liveTreeUpdateTimer != null && !_liveTreeUpdateTimer.Enabled)
            {
                _liveTreeUpdateTimer.Start();
            }
        }

        public void StopLiveTreeUpdateTimer()
        {
            if (_liveTreeUpdateTimer != null)
            {
                _liveTreeUpdateTimer.Stop();
            }
        }

        public void ApplyScanProgressToLiveTree(ScanProgress scanProgress)
        {
            if (scanProgress == null)
                return;

            if (scanProgress.LiveRootEntry == null)
                return;

            _treeViewEntries.UpdateRootEntry(scanProgress.LiveRootEntry);
        }

        public void RenderScanResult(FileSystemEntry rootEntry)
        {
            if (rootEntry == null)
                return;

            _treeViewEntries.SetRootEntry(rootEntry);
        }

        private void ConfigureLiveTreeUpdateTimer()
        {
            _liveTreeUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = 250
            };

            _liveTreeUpdateTimer.Tick += liveTreeUpdateTimer_Tick;
        }

        private void liveTreeUpdateTimer_Tick(object sender, EventArgs e)
        {
            FlushPendingLiveTreeUpdate();
        }

        private void treeViewEntries_SelectedEntryChanged(object sender, TreeEntrySizeBarView.SelectedEntryChangedEventArgs e)
        {
            _selectedEntryChanged?.Invoke(e.Entry);
        }

        private void treeViewEntries_EntryMouseClick(object sender, TreeEntrySizeBarView.EntryMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            if (e.Entry != null && e.Entry.IsDirectory)
            {
                ContextMenuEntry = e.Entry;
                _contextMenuItemOpenInExplorer.Enabled = true;
                _contextMenuItemExport.Enabled = true;
                _contextMenuItemCopyToClipboard.Enabled = true;
                _contextMenuStripTreeEntries.Show(_treeViewEntries, e.Location);
                return;
            }

            ContextMenuEntry = null;
        }
    }
}
