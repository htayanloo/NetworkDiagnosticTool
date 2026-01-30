using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NetworkDiagnosticTool.Models;
using NetworkDiagnosticTool.Services;

namespace NetworkDiagnosticTool.Forms
{
    public partial class ConfigForm : Form
    {
        private readonly ConfigurationService _configService;

        public AppConfiguration Configuration { get; private set; }

        private TextBox _companyNameTextBox;
        private ComboBox _autoRefreshComboBox;
        private CheckBox _minimizeToTrayCheckBox;
        private CheckBox _showNotificationsCheckBox;
        private DataGridView _checksGrid;
        private Button _addButton;
        private Button _removeButton;
        private Button _saveButton;
        private Button _cancelButton;
        private Label _configPathLabel;

        public ConfigForm(AppConfiguration config, ConfigurationService configService)
        {
            _configService = configService;
            Configuration = CloneConfiguration(config);

            InitializeComponent();
            InitializeCustomComponents();
            LoadConfigurationToUI();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Configuration";
            this.Size = new Size(600, 550);
            this.MinimumSize = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 10F);
            this.BackColor = Color.FromArgb(245, 245, 245);

            CreateGeneralSettingsPanel();
            CreateChecksPanel();
            CreateButtonPanel();
        }

        private void CreateGeneralSettingsPanel()
        {
            var settingsGroup = new GroupBox
            {
                Text = "  General Settings  ",
                Location = new Point(15, 15),
                Size = new Size(555, 140),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };

            var regularFont = new Font("Segoe UI", 10F, FontStyle.Regular);

            var companyLabel = new Label
            {
                Text = "Company Name:",
                Location = new Point(15, 30),
                Size = new Size(120, 23),
                Font = regularFont,
                ForeColor = Color.Black
            };

            _companyNameTextBox = new TextBox
            {
                Location = new Point(140, 28),
                Size = new Size(250, 25),
                Font = regularFont
            };

            var refreshLabel = new Label
            {
                Text = "Auto-refresh:",
                Location = new Point(15, 65),
                Size = new Size(120, 23),
                Font = regularFont,
                ForeColor = Color.Black
            };

            _autoRefreshComboBox = new ComboBox
            {
                Location = new Point(140, 63),
                Size = new Size(150, 25),
                Font = regularFont,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _autoRefreshComboBox.Items.AddRange(new object[]
            {
                "Disabled",
                "15 seconds",
                "30 seconds",
                "60 seconds",
                "2 minutes",
                "5 minutes"
            });

            _minimizeToTrayCheckBox = new CheckBox
            {
                Text = "Minimize to system tray when closing",
                Location = new Point(15, 100),
                Size = new Size(280, 25),
                Font = regularFont,
                ForeColor = Color.Black
            };

            _showNotificationsCheckBox = new CheckBox
            {
                Text = "Show balloon notifications",
                Location = new Point(310, 100),
                Size = new Size(220, 25),
                Font = regularFont,
                ForeColor = Color.Black
            };

            settingsGroup.Controls.AddRange(new Control[]
            {
                companyLabel, _companyNameTextBox,
                refreshLabel, _autoRefreshComboBox,
                _minimizeToTrayCheckBox, _showNotificationsCheckBox
            });

            this.Controls.Add(settingsGroup);
        }

        private void CreateChecksPanel()
        {
            var checksGroup = new GroupBox
            {
                Text = "  Service Checks  ",
                Location = new Point(15, 165),
                Size = new Size(555, 280),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            _checksGrid = new DataGridView
            {
                Location = new Point(15, 28),
                Size = new Size(440, 200),
                Font = new Font("Segoe UI", 9F),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            _checksGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Name",
                FillWeight = 25
            });

            _checksGrid.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "Type",
                HeaderText = "Type",
                FillWeight = 15,
                Items = { "ping", "tcp", "http" }
            });

            _checksGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Host",
                HeaderText = "Host",
                FillWeight = 25
            });

            _checksGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Port",
                HeaderText = "Port",
                FillWeight = 10
            });

            _checksGrid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Url",
                HeaderText = "URL",
                FillWeight = 25
            });

            _checksGrid.CellValueChanged += ChecksGrid_CellValueChanged;
            _checksGrid.CurrentCellDirtyStateChanged += ChecksGrid_CurrentCellDirtyStateChanged;

            var buttonFont = new Font("Segoe UI", 9F);

            _addButton = new Button
            {
                Text = "Add",
                Location = new Point(465, 28),
                Size = new Size(75, 30),
                Font = buttonFont,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _addButton.FlatAppearance.BorderSize = 0;
            _addButton.Click += AddButton_Click;

            _removeButton = new Button
            {
                Text = "Remove",
                Location = new Point(465, 65),
                Size = new Size(75, 30),
                Font = buttonFont,
                BackColor = Color.FromArgb(220, 53, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _removeButton.FlatAppearance.BorderSize = 0;
            _removeButton.Click += RemoveButton_Click;

            _configPathLabel = new Label
            {
                Text = $"Config file: {_configService.ConfigPath}",
                Location = new Point(15, 240),
                Size = new Size(520, 20),
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.Gray,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            checksGroup.Controls.AddRange(new Control[]
            {
                _checksGrid, _addButton, _removeButton, _configPathLabel
            });

            this.Controls.Add(checksGroup);
        }

        private void CreateButtonPanel()
        {
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.FromArgb(240, 240, 240)
            };

            var buttonFont = new Font("Segoe UI", 10F);

            _saveButton = new Button
            {
                Text = "Save",
                Size = new Size(100, 35),
                Location = new Point(350, 10),
                Font = buttonFont,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _saveButton.FlatAppearance.BorderSize = 0;
            _saveButton.Click += SaveButton_Click;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                Location = new Point(460, 10),
                Font = buttonFont,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _cancelButton.FlatAppearance.BorderSize = 0;

            buttonPanel.Controls.AddRange(new Control[] { _saveButton, _cancelButton });
            this.Controls.Add(buttonPanel);

            this.AcceptButton = _saveButton;
            this.CancelButton = _cancelButton;
        }

        private void LoadConfigurationToUI()
        {
            _companyNameTextBox.Text = Configuration.CompanyName;
            _minimizeToTrayCheckBox.Checked = Configuration.MinimizeToTray;
            _showNotificationsCheckBox.Checked = Configuration.ShowBalloonNotifications;

            // Set auto-refresh dropdown
            switch (Configuration.AutoRefreshSeconds)
            {
                case 0:
                    _autoRefreshComboBox.SelectedIndex = 0;
                    break;
                case 15:
                    _autoRefreshComboBox.SelectedIndex = 1;
                    break;
                case 30:
                    _autoRefreshComboBox.SelectedIndex = 2;
                    break;
                case 60:
                    _autoRefreshComboBox.SelectedIndex = 3;
                    break;
                case 120:
                    _autoRefreshComboBox.SelectedIndex = 4;
                    break;
                case 300:
                    _autoRefreshComboBox.SelectedIndex = 5;
                    break;
                default:
                    _autoRefreshComboBox.SelectedIndex = 2; // Default to 30 seconds
                    break;
            }

            // Load checks into grid
            RefreshChecksGrid();
        }

        private void RefreshChecksGrid()
        {
            _checksGrid.Rows.Clear();

            if (Configuration.Checks != null)
            {
                foreach (var check in Configuration.Checks)
                {
                    var rowIndex = _checksGrid.Rows.Add();
                    var row = _checksGrid.Rows[rowIndex];

                    row.Cells["Name"].Value = check.Name;
                    row.Cells["Type"].Value = check.Type;
                    row.Cells["Host"].Value = check.Host;
                    row.Cells["Port"].Value = check.Port?.ToString();
                    row.Cells["Url"].Value = check.Url;
                }
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            var newCheck = new CustomCheck
            {
                Name = "New Check",
                Type = "ping",
                Host = "example.com",
                TimeoutMs = 5000
            };

            Configuration.Checks.Add(newCheck);

            var rowIndex = _checksGrid.Rows.Add();
            var row = _checksGrid.Rows[rowIndex];
            row.Cells["Name"].Value = newCheck.Name;
            row.Cells["Type"].Value = newCheck.Type;
            row.Cells["Host"].Value = newCheck.Host;

            _checksGrid.CurrentCell = row.Cells["Name"];
            _checksGrid.BeginEdit(true);
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (_checksGrid.SelectedRows.Count > 0)
            {
                var index = _checksGrid.SelectedRows[0].Index;
                if (index >= 0 && index < Configuration.Checks.Count)
                {
                    Configuration.Checks.RemoveAt(index);
                    _checksGrid.Rows.RemoveAt(index);
                }
            }
        }

        private void ChecksGrid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_checksGrid.IsCurrentCellDirty)
            {
                _checksGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void ChecksGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= Configuration.Checks.Count)
                return;

            var check = Configuration.Checks[e.RowIndex];
            var row = _checksGrid.Rows[e.RowIndex];

            check.Name = row.Cells["Name"].Value?.ToString();
            check.Type = row.Cells["Type"].Value?.ToString();
            check.Host = row.Cells["Host"].Value?.ToString();

            var portStr = row.Cells["Port"].Value?.ToString();
            check.Port = int.TryParse(portStr, out var port) ? port : (int?)null;

            check.Url = row.Cells["Url"].Value?.ToString();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // Update configuration from UI
            Configuration.CompanyName = _companyNameTextBox.Text;
            Configuration.MinimizeToTray = _minimizeToTrayCheckBox.Checked;
            Configuration.ShowBalloonNotifications = _showNotificationsCheckBox.Checked;

            // Set auto-refresh seconds
            switch (_autoRefreshComboBox.SelectedIndex)
            {
                case 0:
                    Configuration.AutoRefreshSeconds = 0;
                    break;
                case 1:
                    Configuration.AutoRefreshSeconds = 15;
                    break;
                case 2:
                    Configuration.AutoRefreshSeconds = 30;
                    break;
                case 3:
                    Configuration.AutoRefreshSeconds = 60;
                    break;
                case 4:
                    Configuration.AutoRefreshSeconds = 120;
                    break;
                case 5:
                    Configuration.AutoRefreshSeconds = 300;
                    break;
            }

            // Validate checks
            var invalidChecks = Configuration.Checks.Where(c => !c.IsValid()).ToList();
            if (invalidChecks.Any())
            {
                var message = "The following checks are invalid and will be skipped:\n\n";
                message += string.Join("\n", invalidChecks.Select(c => $"• {c.Name}: Missing required fields"));
                MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Save to file
            if (_configService.SaveConfiguration(Configuration))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Failed to save configuration file.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private AppConfiguration CloneConfiguration(AppConfiguration source)
        {
            return new AppConfiguration
            {
                CompanyName = source.CompanyName,
                AutoRefreshSeconds = source.AutoRefreshSeconds,
                MinimizeToTray = source.MinimizeToTray,
                ShowBalloonNotifications = source.ShowBalloonNotifications,
                Checks = source.Checks?.Select(c => new CustomCheck
                {
                    Name = c.Name,
                    Type = c.Type,
                    Host = c.Host,
                    Port = c.Port,
                    Url = c.Url,
                    TimeoutMs = c.TimeoutMs
                }).ToList() ?? new System.Collections.Generic.List<CustomCheck>()
            };
        }
    }
}
