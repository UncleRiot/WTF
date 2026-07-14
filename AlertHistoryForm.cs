using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Lucid.Controls;
using Lucid.Controls.GridView;
using Lucid.Theming;

namespace WTF
{
    public sealed class AlertHistoryForm : Form
    {
        private readonly AppSettings _settings;
        private readonly Image _informationSymbolImage = StatusSymbolRenderer.CreateBitmap(StatusSymbolKind.Information);
        private readonly Image _warningSymbolImage = StatusSymbolRenderer.CreateBitmap(StatusSymbolKind.Warning);
        private readonly Image _errorSymbolImage = StatusSymbolRenderer.CreateBitmap(StatusSymbolKind.Error);

        private LucidDataGridView dataGridViewAlerts;
        private LucidTextBox textBoxDetails;
        private LucidButton buttonConfirm;
        private LucidButton buttonDelete;
        private LucidButton buttonConfirmAll;
        private LucidButton buttonDeleteAll;
        private LucidButton buttonClose;

        public AlertHistoryForm(AppSettings settings)
        {
            _settings = settings;

            LucidThemeService.Apply(_settings.Layout);
            InitializeComponent();
            WindowsFormStyler.Apply(this, _settings.Layout);
            ApplyTheme();
            LoadAlerts();

            AppAlertLog.Changed += AppAlertLog_Changed;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _informationSymbolImage.Dispose();
                _warningSymbolImage.Dispose();
                _errorSymbolImage.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            AppAlertLog.Changed -= AppAlertLog_Changed;
            base.OnFormClosed(e);
        }

        private void InitializeComponent()
        {
            Text = LocalizationService.GetText("AlertHistory.Title");
            StartPosition = FormStartPosition.CenterParent;
            Size = new System.Drawing.Size(820, 500);
            MinimumSize = new System.Drawing.Size(640, 380);
            ShowInTaskbar = false;

            dataGridViewAlerts = new LucidDataGridView
            {
                Name = "dataGridViewAlerts",
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true
            };

            dataGridViewAlerts.Columns.Add(new DataGridViewImageColumn
            {
                Name = "ColumnSeverity",
                HeaderText = LocalizationService.GetText("AlertHistory.Type"),
                Width = 90,
                ImageLayout = DataGridViewImageCellLayout.Normal,
                DefaultCellStyle =
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            });

            dataGridViewAlerts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnCategory",
                HeaderText = LocalizationService.GetText("AlertHistory.Category"),
                DataPropertyName = "Category",
                Width = 140
            });

            dataGridViewAlerts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnMessage",
                HeaderText = LocalizationService.GetText("AlertHistory.Message"),
                DataPropertyName = "Message",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dataGridViewAlerts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnCreatedAt",
                HeaderText = LocalizationService.GetText("AlertHistory.CreatedAt"),
                DataPropertyName = "CreatedAtText",
                Width = 140
            });

            dataGridViewAlerts.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColumnConfirmed",
                HeaderText = LocalizationService.GetText("AlertHistory.Confirmed"),
                DataPropertyName = "ConfirmedText",
                Width = 80
            });

            dataGridViewAlerts.CellFormatting += dataGridViewAlerts_CellFormatting;
            dataGridViewAlerts.SelectionChanged += dataGridViewAlerts_SelectionChanged;

            LucidLabel labelDetails = new LucidLabel
            {
                Name = "labelDetails",
                Text = LocalizationService.GetText("AlertHistory.Details"),
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            textBoxDetails = new LucidTextBox
            {
                Name = "textBoxDetails",
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            Panel detailsPanel = new Panel
            {
                Name = "detailsPanel",
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 0, 8, 4)
            };

            detailsPanel.Controls.Add(textBoxDetails);
            detailsPanel.Controls.Add(labelDetails);

            buttonConfirm = new LucidButton
            {
                Name = "buttonConfirm",
                ButtonStyle = LucidButtonStyle.Normal,
                Text = LocalizationService.GetText("AlertHistory.Confirm"),
                Size = new System.Drawing.Size(95, 30)
            };

            buttonDelete = new LucidButton
            {
                Name = "buttonDelete",
                ButtonStyle = LucidButtonStyle.Normal,
                Text = LocalizationService.GetText("AlertHistory.Delete"),
                Size = new System.Drawing.Size(85, 30)
            };

            buttonConfirmAll = new LucidButton
            {
                Name = "buttonConfirmAll",
                ButtonStyle = LucidButtonStyle.Normal,
                Text = LocalizationService.GetText("AlertHistory.ConfirmAll"),
                Size = new System.Drawing.Size(110, 30)
            };

            buttonDeleteAll = new LucidButton
            {
                Name = "buttonDeleteAll",
                ButtonStyle = LucidButtonStyle.Normal,
                Text = LocalizationService.GetText("AlertHistory.DeleteAll"),
                Size = new System.Drawing.Size(95, 30)
            };

            buttonClose = new LucidButton
            {
                Name = "buttonClose",
                ButtonStyle = LucidButtonStyle.Normal,
                Text = LocalizationService.GetText("Common.Close"),
                Size = new System.Drawing.Size(90, 30),
                DialogResult = DialogResult.OK
            };

            buttonConfirm.Click += buttonConfirm_Click;
            buttonDelete.Click += buttonDelete_Click;
            buttonConfirmAll.Click += buttonConfirmAll_Click;
            buttonDeleteAll.Click += buttonDeleteAll_Click;

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                Name = "buttonPanel",
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                Height = 48
            };

            buttonPanel.Controls.Add(buttonClose);
            buttonPanel.Controls.Add(buttonDeleteAll);
            buttonPanel.Controls.Add(buttonConfirmAll);
            buttonPanel.Controls.Add(buttonDelete);
            buttonPanel.Controls.Add(buttonConfirm);

            TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
            {
                Name = "tableLayoutPanel",
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };

            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            tableLayoutPanel.Controls.Add(dataGridViewAlerts, 0, 0);
            tableLayoutPanel.Controls.Add(detailsPanel, 0, 1);
            tableLayoutPanel.Controls.Add(buttonPanel, 0, 2);

            Controls.Add(tableLayoutPanel);
            AcceptButton = buttonClose;
            CancelButton = buttonClose;

            UpdateButtonState();
            UpdateDetails();
        }

        private void ApplyTheme()
        {
            Color backgroundColor = ThemeProvider.Theme.Colors.BackgroundPrimary;
            Color secondaryBackgroundColor = ThemeProvider.Theme.Colors.BackgroundSecondary;
            Color textColor = ThemeProvider.Theme.Colors.TextPrimary;
            Color borderColor = ThemeProvider.Theme.Colors.SurfaceHighlight;

            BackColor = backgroundColor;
            ForeColor = textColor;

            dataGridViewAlerts.BackgroundColor = backgroundColor;
            dataGridViewAlerts.BackColor = backgroundColor;
            dataGridViewAlerts.ForeColor = textColor;
            dataGridViewAlerts.GridColor = borderColor;
            dataGridViewAlerts.EnableHeadersVisualStyles = false;


            textBoxDetails.BackColor = backgroundColor;
            textBoxDetails.ForeColor = textColor;

            foreach (Control control in Controls)
            {
                ApplyContainerTheme(control, backgroundColor, textColor);
            }
        }

        private static void ApplyContainerTheme(
            Control control,
            Color backgroundColor,
            Color textColor)
        {
            if (control is Panel ||
                control is TableLayoutPanel ||
                control is FlowLayoutPanel)
            {
                control.BackColor = backgroundColor;
                control.ForeColor = textColor;
            }

            foreach (Control child in control.Controls)
            {
                ApplyContainerTheme(child, backgroundColor, textColor);
            }
        }

        private void AppAlertLog_Changed(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(LoadAlerts));
                return;
            }

            LoadAlerts();
        }

        private void LoadAlerts()
        {
            List<AppAlertEntry> selectedEntries = GetSelectedEntries();
            HashSet<Guid> selectedIds = new HashSet<Guid>(selectedEntries.Select(entry => entry.Id));

            dataGridViewAlerts.DataSource = AppAlertLog.GetEntries();

            foreach (DataGridViewRow row in dataGridViewAlerts.Rows)
            {
                if (row.DataBoundItem is AppAlertEntry entry && selectedIds.Contains(entry.Id))
                {
                    row.Selected = true;
                }
            }

            UpdateButtonState();
            UpdateDetails();
        }

        private void dataGridViewAlerts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (dataGridViewAlerts.Columns[e.ColumnIndex].Name != "ColumnSeverity")
                return;

            if (dataGridViewAlerts.Rows[e.RowIndex].DataBoundItem is not AppAlertEntry entry)
                return;

            e.Value = GetAlertSeverityImage(entry.Severity);
            e.FormattingApplied = true;
        }

        private Image GetAlertSeverityImage(AppAlertSeverity severity)
        {
            switch (severity)
            {
                case AppAlertSeverity.Warning:
                    return _warningSymbolImage;
                case AppAlertSeverity.Error:
                    return _errorSymbolImage;
                default:
                    return _informationSymbolImage;
            }
        }

        private void dataGridViewAlerts_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonState();
            UpdateDetails();
        }

        private void buttonConfirm_Click(object sender, EventArgs e)
        {
            AppAlertLog.Confirm(GetSelectedEntries().Select(entry => entry.Id));
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            AppAlertLog.Delete(GetSelectedEntries().Select(entry => entry.Id));
        }

        private void buttonConfirmAll_Click(object sender, EventArgs e)
        {
            AppAlertLog.ConfirmAll();
        }

        private void buttonDeleteAll_Click(object sender, EventArgs e)
        {
            AppAlertLog.DeleteAll();
        }

        private List<AppAlertEntry> GetSelectedEntries()
        {
            return dataGridViewAlerts.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(row => row.DataBoundItem as AppAlertEntry)
                .Where(entry => entry != null)
                .ToList();
        }

        private void UpdateButtonState()
        {
            bool hasSelection = dataGridViewAlerts.SelectedRows.Count > 0;
            bool hasEntries = dataGridViewAlerts.Rows.Count > 0;

            buttonConfirm.Enabled = hasSelection;
            buttonDelete.Enabled = hasSelection;
            buttonConfirmAll.Enabled = hasEntries;
            buttonDeleteAll.Enabled = hasEntries;
        }

        private void UpdateDetails()
        {
            List<AppAlertEntry> selectedEntries = GetSelectedEntries();

            if (selectedEntries.Count != 1)
            {
                textBoxDetails.Text = string.Empty;
                return;
            }

            AppAlertEntry selectedEntry = selectedEntries[0];

            if (!string.IsNullOrWhiteSpace(selectedEntry.Details))
            {
                textBoxDetails.Text = selectedEntry.Details;
                return;
            }

            textBoxDetails.Text = selectedEntry.Message ?? string.Empty;
        }
    }
}