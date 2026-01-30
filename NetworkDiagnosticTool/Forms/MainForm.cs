using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetworkDiagnosticTool.Controls;
using NetworkDiagnosticTool.Models;
using NetworkDiagnosticTool.Services;

namespace NetworkDiagnosticTool.Forms
{
    public partial class MainForm : Form
    {
        private readonly NetworkInfoService _networkInfoService;
        private readonly ConnectivityService _connectivityService;
        private readonly ConfigurationService _configService;
        private readonly ReportExportService _reportService;

        private AppConfiguration _config;
        private ComputerInfo _computerInfo;
        private List<NetworkInterfaceInfo> _networkInterfaces;
        private NetworkInterfaceInfo _selectedInterface;
        private List<CheckResult> _connectivityResults;
        private List<CheckResult> _serviceResults;

        private Timer _autoRefreshTimer;
        private bool _isRefreshing = false;
        private DateTime _lastRefresh;

        // UI Controls
        private Panel _headerPanel;
        private Label _titleLabel;
        private Label _companyLabel;
        private Label _autoRefreshLabel;
        private CheckBox _autoRefreshCheckbox;

        private GroupBox _computerGroupBox;
        private Label _usernameLabel;
        private Label _computerNameLabel;
        private Label _domainLabel;

        private GroupBox _networkGroupBox;
        private ComboBox _adapterComboBox;
        private StatusIndicator _networkStatusIndicator;
        private Label _networkStatusLabel;
        private Label _dhcpLabel;
        private Label _ipAddressLabel;
        private Label _subnetLabel;
        private Label _gatewayLabel;
        private Label _dns1Label;
        private Label _dns2Label;
        private Label _macLabel;
        private Label _speedLabel;

        private GroupBox _connectivityGroupBox;
        private Panel _connectivityResultsPanel;

        private GroupBox _servicesGroupBox;
        private Panel _servicesResultsPanel;

        private Panel _buttonPanel;
        private Button _refreshButton;
        private Button _copyButton;
        private Button _exportButton;
        private Button _configButton;
        private Label _lastUpdateLabel;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;

        public MainForm()
        {
            _networkInfoService = new NetworkInfoService();
            _connectivityService = new ConnectivityService();
            _configService = new ConfigurationService();
            _reportService = new ReportExportService();

            _connectivityResults = new List<CheckResult>();
            _serviceResults = new List<CheckResult>();

            InitializeComponent();
            InitializeCustomComponents();
            LoadConfiguration();
            SetupAutoRefresh();
            SetupTrayIcon();
        }

        private void InitializeCustomComponents()
        {
            // Form settings
            this.Text = "Network Diagnostic Tool";
            this.Size = new Size(650, 780);
            this.MinimumSize = new Size(600, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 245, 245);

            CreateHeaderPanel();
            CreateComputerGroupBox();
            CreateNetworkGroupBox();
            CreateConnectivityGroupBox();
            CreateServicesGroupBox();
            CreateButtonPanel();
        }

        private void CreateHeaderPanel()
        {
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(0, 102, 204),
                Padding = new Padding(15, 10, 15, 10)
            };

            _titleLabel = new Label
            {
                Text = "Network Diagnostic Tool",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 10)
            };

            _companyLabel = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 220, 255),
                AutoSize = true,
                Location = new Point(15, 40)
            };

            _autoRefreshCheckbox = new CheckBox
            {
                Text = "Auto-refresh",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_headerPanel.Width - 150, 15)
            };
            _autoRefreshCheckbox.CheckedChanged += AutoRefreshCheckbox_CheckedChanged;

            _autoRefreshLabel = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(200, 220, 255),
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(_headerPanel.Width - 150, 38)
            };

            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_companyLabel);
            _headerPanel.Controls.Add(_autoRefreshCheckbox);
            _headerPanel.Controls.Add(_autoRefreshLabel);
            this.Controls.Add(_headerPanel);

            _headerPanel.Resize += (s, e) =>
            {
                _autoRefreshCheckbox.Location = new Point(_headerPanel.Width - 150, 15);
                _autoRefreshLabel.Location = new Point(_headerPanel.Width - 150, 38);
            };
        }

        private void CreateComputerGroupBox()
        {
            _computerGroupBox = new GroupBox
            {
                Text = "  COMPUTER  ",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                Location = new Point(15, 85),
                Size = new Size(600, 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var regularFont = new Font("Segoe UI", 10F, FontStyle.Regular);
            var valueFont = new Font("Segoe UI", 10F, FontStyle.Bold);

            var lblUsername = CreateInfoLabel("Username:", 20, 30, 120, regularFont);
            _usernameLabel = CreateValueLabel("", 140, 30, 200, valueFont);

            var lblComputer = CreateInfoLabel("Computer:", 20, 55, 120, regularFont);
            _computerNameLabel = CreateValueLabel("", 140, 55, 200, valueFont);

            var lblDomain = CreateInfoLabel("Domain:", 350, 30, 80, regularFont);
            _domainLabel = CreateValueLabel("", 430, 30, 150, valueFont);

            _computerGroupBox.Controls.AddRange(new Control[] {
                lblUsername, _usernameLabel,
                lblComputer, _computerNameLabel,
                lblDomain, _domainLabel
            });

            this.Controls.Add(_computerGroupBox);
        }

        private void CreateNetworkGroupBox()
        {
            _networkGroupBox = new GroupBox
            {
                Text = "  NETWORK ADAPTER  ",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                Location = new Point(15, 195),
                Size = new Size(600, 200),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var regularFont = new Font("Segoe UI", 10F, FontStyle.Regular);
            var valueFont = new Font("Segoe UI", 10F, FontStyle.Bold);
            var largeFont = new Font("Segoe UI", 11F, FontStyle.Bold);

            _adapterComboBox = new ComboBox
            {
                Location = new Point(20, 28),
                Size = new Size(400, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            _adapterComboBox.SelectedIndexChanged += AdapterComboBox_SelectedIndexChanged;

            _networkStatusIndicator = new StatusIndicator
            {
                Location = new Point(440, 30),
                Size = new Size(18, 18)
            };

            _networkStatusLabel = new Label
            {
                Location = new Point(462, 28),
                Size = new Size(120, 24),
                Font = largeFont,
                ForeColor = Color.FromArgb(40, 167, 69)
            };

            var lblDhcp = CreateInfoLabel("Mode:", 20, 60, 80, regularFont);
            _dhcpLabel = new Label
            {
                Location = new Point(100, 58),
                Size = new Size(80, 24),
                Font = largeFont,
                ForeColor = Color.FromArgb(0, 123, 255),
                Text = "DHCP"
            };

            var lblIp = CreateInfoLabel("IP Address:", 20, 90, 100, regularFont);
            _ipAddressLabel = CreateValueLabel("", 125, 90, 150, valueFont);

            var lblSubnet = CreateInfoLabel("Subnet Mask:", 280, 90, 110, regularFont);
            _subnetLabel = CreateValueLabel("", 395, 90, 150, valueFont);

            var lblGateway = CreateInfoLabel("Gateway:", 20, 115, 100, regularFont);
            _gatewayLabel = CreateValueLabel("", 125, 115, 150, valueFont);

            var lblMac = CreateInfoLabel("MAC:", 280, 115, 110, regularFont);
            _macLabel = CreateValueLabel("", 395, 115, 180, valueFont);

            var lblDns1 = CreateInfoLabel("DNS Server 1:", 20, 140, 110, regularFont);
            _dns1Label = CreateValueLabel("", 135, 140, 140, valueFont);

            var lblDns2 = CreateInfoLabel("DNS Server 2:", 280, 140, 110, regularFont);
            _dns2Label = CreateValueLabel("", 395, 140, 140, valueFont);

            var lblSpeed = CreateInfoLabel("Speed:", 20, 165, 100, regularFont);
            _speedLabel = CreateValueLabel("", 125, 165, 150, valueFont);

            _networkGroupBox.Controls.AddRange(new Control[] {
                _adapterComboBox, _networkStatusIndicator, _networkStatusLabel,
                lblDhcp, _dhcpLabel,
                lblIp, _ipAddressLabel,
                lblSubnet, _subnetLabel,
                lblGateway, _gatewayLabel,
                lblMac, _macLabel,
                lblDns1, _dns1Label,
                lblDns2, _dns2Label,
                lblSpeed, _speedLabel
            });

            this.Controls.Add(_networkGroupBox);
        }

        private void CreateConnectivityGroupBox()
        {
            _connectivityGroupBox = new GroupBox
            {
                Text = "  CONNECTIVITY TESTS  ",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                Location = new Point(15, 405),
                Size = new Size(600, 110),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _connectivityResultsPanel = new Panel
            {
                Location = new Point(10, 25),
                Size = new Size(580, 75),
                AutoScroll = true
            };

            _connectivityGroupBox.Controls.Add(_connectivityResultsPanel);
            this.Controls.Add(_connectivityGroupBox);
        }

        private void CreateServicesGroupBox()
        {
            _servicesGroupBox = new GroupBox
            {
                Text = "  SERVICE CHECKS  ",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                Location = new Point(15, 525),
                Size = new Size(600, 130),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            _servicesResultsPanel = new Panel
            {
                Location = new Point(10, 25),
                Size = new Size(580, 95),
                AutoScroll = true
            };

            _servicesGroupBox.Controls.Add(_servicesResultsPanel);
            this.Controls.Add(_servicesGroupBox);
        }

        private void CreateButtonPanel()
        {
            _buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 70,
                BackColor = Color.FromArgb(240, 240, 240),
                Padding = new Padding(15, 10, 15, 10)
            };

            var buttonFont = new Font("Segoe UI", 10F, FontStyle.Regular);

            _refreshButton = new Button
            {
                Text = "Refresh Now",
                Size = new Size(120, 35),
                Location = new Point(15, 17),
                Font = buttonFont,
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _refreshButton.FlatAppearance.BorderSize = 0;
            _refreshButton.Click += RefreshButton_Click;

            _copyButton = new Button
            {
                Text = "Copy to Clipboard",
                Size = new Size(140, 35),
                Location = new Point(145, 17),
                Font = buttonFont,
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _copyButton.FlatAppearance.BorderSize = 0;
            _copyButton.Click += CopyButton_Click;

            _exportButton = new Button
            {
                Text = "Export Report",
                Size = new Size(120, 35),
                Location = new Point(295, 17),
                Font = buttonFont,
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _exportButton.FlatAppearance.BorderSize = 0;
            _exportButton.Click += ExportButton_Click;

            _configButton = new Button
            {
                Text = "Configure",
                Size = new Size(100, 35),
                Location = new Point(425, 17),
                Font = buttonFont,
                BackColor = Color.FromArgb(255, 193, 7),
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _configButton.FlatAppearance.BorderSize = 0;
            _configButton.Click += ConfigButton_Click;

            _lastUpdateLabel = new Label
            {
                Text = "Last updated: Never",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(450, 55)
            };

            _buttonPanel.Controls.AddRange(new Control[] {
                _refreshButton, _copyButton, _exportButton, _configButton, _lastUpdateLabel
            });

            _buttonPanel.Resize += (s, e) =>
            {
                _lastUpdateLabel.Location = new Point(_buttonPanel.Width - _lastUpdateLabel.Width - 20, 55);
            };

            this.Controls.Add(_buttonPanel);
        }

        private Label CreateInfoLabel(string text, int x, int y, int width, Font font)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 22),
                Font = font,
                ForeColor = Color.FromArgb(80, 80, 80)
            };
        }

        private Label CreateValueLabel(string text, int x, int y, int width, Font font)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 22),
                Font = font,
                ForeColor = Color.FromArgb(30, 30, 30)
            };
        }

        private void SetupTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Show", null, (s, e) => ShowFromTray());
            _trayMenu.Items.Add("Refresh", null, async (s, e) => await RefreshAllAsync());
            _trayMenu.Items.Add("-");
            _trayMenu.Items.Add("Exit", null, (s, e) => ExitApplication());

            _trayIcon = new NotifyIcon
            {
                Text = "Network Diagnostic Tool",
                ContextMenuStrip = _trayMenu,
                Visible = false
            };

            // Use a simple icon (we'll create one programmatically)
            _trayIcon.Icon = CreateSimpleIcon();
            _trayIcon.DoubleClick += (s, e) => ShowFromTray();
        }

        private Icon CreateSimpleIcon()
        {
            using (var bmp = new Bitmap(16, 16))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.FromArgb(0, 123, 255)))
                {
                    g.FillEllipse(brush, 1, 1, 14, 14);
                }
                using (var pen = new Pen(Color.White, 2))
                {
                    g.DrawLine(pen, 5, 8, 8, 11);
                    g.DrawLine(pen, 8, 11, 12, 5);
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        private void LoadConfiguration()
        {
            _config = _configService.LoadConfiguration();
            _companyLabel.Text = _config.CompanyName;
            _autoRefreshCheckbox.Checked = _config.AutoRefreshSeconds > 0;
        }

        private void SetupAutoRefresh()
        {
            _autoRefreshTimer = new Timer();
            _autoRefreshTimer.Interval = (_config?.AutoRefreshSeconds ?? 30) * 1000;
            _autoRefreshTimer.Tick += async (s, e) => await RefreshAllAsync();

            if (_config?.AutoRefreshSeconds > 0)
            {
                _autoRefreshTimer.Start();
                UpdateAutoRefreshLabel();
            }
        }

        private void UpdateAutoRefreshLabel()
        {
            if (_config?.AutoRefreshSeconds > 0)
            {
                _autoRefreshLabel.Text = $"Every {_config.AutoRefreshSeconds}s";
            }
            else
            {
                _autoRefreshLabel.Text = "Disabled";
            }
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await RefreshAllAsync();
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            await RefreshAllAsync();
        }

        private async Task RefreshAllAsync()
        {
            if (_isRefreshing) return;
            _isRefreshing = true;

            try
            {
                _refreshButton.Enabled = false;
                _refreshButton.Text = "Refreshing...";

                // Refresh computer info
                _computerInfo = _networkInfoService.GetComputerInfo();
                UpdateComputerInfoDisplay();

                // Refresh network interfaces
                _networkInterfaces = _networkInfoService.GetNetworkInterfaces();
                UpdateAdapterComboBox();

                // Select primary interface if nothing selected
                if (_selectedInterface == null)
                {
                    _selectedInterface = _networkInfoService.GetPrimaryNetworkInterface();
                    SelectInterfaceInComboBox();
                }

                UpdateNetworkInfoDisplay();

                // Run connectivity tests
                await RunConnectivityTestsAsync();

                // Run service checks
                await RunServiceChecksAsync();

                _lastRefresh = DateTime.Now;
                _lastUpdateLabel.Text = $"Last updated: {_lastRefresh:HH:mm:ss}";
            }
            finally
            {
                _refreshButton.Enabled = true;
                _refreshButton.Text = "Refresh Now";
                _isRefreshing = false;
            }
        }

        private void UpdateComputerInfoDisplay()
        {
            _usernameLabel.Text = _computerInfo?.Username ?? "N/A";
            _computerNameLabel.Text = _computerInfo?.ComputerName ?? "N/A";
            _domainLabel.Text = _computerInfo?.DomainOrWorkgroup ?? "N/A";
        }

        private void UpdateAdapterComboBox()
        {
            _adapterComboBox.Items.Clear();

            if (_networkInterfaces != null)
            {
                foreach (var ni in _networkInterfaces)
                {
                    var status = ni.Status == "Up" ? "" : $" [{ni.Status}]";
                    _adapterComboBox.Items.Add($"{ni.Name}{status}");
                }
            }
        }

        private void SelectInterfaceInComboBox()
        {
            if (_selectedInterface != null && _networkInterfaces != null)
            {
                var index = _networkInterfaces.FindIndex(n => n.Id == _selectedInterface.Id);
                if (index >= 0 && index < _adapterComboBox.Items.Count)
                {
                    _adapterComboBox.SelectedIndex = index;
                }
            }
        }

        private void AdapterComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_adapterComboBox.SelectedIndex >= 0 && _networkInterfaces != null)
            {
                _selectedInterface = _networkInterfaces[_adapterComboBox.SelectedIndex];
                UpdateNetworkInfoDisplay();
            }
        }

        private void UpdateNetworkInfoDisplay()
        {
            if (_selectedInterface == null)
            {
                _networkStatusIndicator.SetUnknown();
                _networkStatusLabel.Text = "No Adapter";
                _dhcpLabel.Text = "N/A";
                _ipAddressLabel.Text = "N/A";
                _subnetLabel.Text = "N/A";
                _gatewayLabel.Text = "N/A";
                _dns1Label.Text = "N/A";
                _dns2Label.Text = "N/A";
                _macLabel.Text = "N/A";
                _speedLabel.Text = "N/A";
                return;
            }

            // Status
            if (_selectedInterface.Status == "Up")
            {
                _networkStatusIndicator.SetSuccess();
                _networkStatusLabel.Text = "CONNECTED";
                _networkStatusLabel.ForeColor = Color.FromArgb(40, 167, 69);
            }
            else
            {
                _networkStatusIndicator.SetFailure();
                _networkStatusLabel.Text = _selectedInterface.GetStatusDisplay().ToUpper();
                _networkStatusLabel.ForeColor = Color.FromArgb(220, 53, 69);
            }

            // DHCP
            if (_selectedInterface.IsDhcp)
            {
                _dhcpLabel.Text = "DHCP";
                _dhcpLabel.ForeColor = Color.FromArgb(0, 123, 255);
            }
            else
            {
                _dhcpLabel.Text = "STATIC";
                _dhcpLabel.ForeColor = Color.FromArgb(255, 152, 0);
            }

            // IP info
            _ipAddressLabel.Text = _selectedInterface.IPAddress ?? "N/A";
            _subnetLabel.Text = _selectedInterface.SubnetMask ?? "N/A";
            _gatewayLabel.Text = _selectedInterface.Gateway ?? "N/A";
            _macLabel.Text = _selectedInterface.MacAddress ?? "N/A";
            _speedLabel.Text = _selectedInterface.GetFormattedSpeed();

            // DNS
            if (_selectedInterface.DnsServers != null && _selectedInterface.DnsServers.Count > 0)
            {
                _dns1Label.Text = _selectedInterface.DnsServers[0];
                _dns2Label.Text = _selectedInterface.DnsServers.Count > 1 ? _selectedInterface.DnsServers[1] : "N/A";
            }
            else
            {
                _dns1Label.Text = "N/A";
                _dns2Label.Text = "N/A";
            }
        }

        private async Task RunConnectivityTestsAsync()
        {
            _connectivityResults.Clear();
            _connectivityResultsPanel.Controls.Clear();

            // DNS Resolution
            var dnsResult = await _connectivityService.TestDnsResolution("google.com");
            _connectivityResults.Add(dnsResult);

            // Gateway Ping
            var gateway = _selectedInterface?.Gateway;
            var gatewayResult = await _connectivityService.TestGatewayPing(gateway);
            _connectivityResults.Add(gatewayResult);

            // Internet Check
            var internetResult = await _connectivityService.TestInternetConnectivity();
            _connectivityResults.Add(internetResult);

            UpdateConnectivityDisplay();
        }

        private async Task RunServiceChecksAsync()
        {
            _serviceResults.Clear();
            _servicesResultsPanel.Controls.Clear();

            if (_config?.Checks != null)
            {
                foreach (var check in _config.Checks.Where(c => c.IsValid()))
                {
                    var result = await _connectivityService.ExecuteCustomCheck(check);
                    _serviceResults.Add(result);
                }
            }

            UpdateServicesDisplay();
        }

        private void UpdateConnectivityDisplay()
        {
            UpdateResultsPanel(_connectivityResultsPanel, _connectivityResults);
        }

        private void UpdateServicesDisplay()
        {
            UpdateResultsPanel(_servicesResultsPanel, _serviceResults);

            if (_serviceResults.Count == 0)
            {
                var noChecksLabel = new Label
                {
                    Text = "No service checks configured. Click Configure to add checks.",
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    Location = new Point(5, 10),
                    AutoSize = true
                };
                _servicesResultsPanel.Controls.Add(noChecksLabel);
            }
        }

        private void UpdateResultsPanel(Panel panel, List<CheckResult> results)
        {
            panel.Controls.Clear();
            int y = 5;

            foreach (var result in results)
            {
                var rowPanel = new Panel
                {
                    Location = new Point(0, y),
                    Size = new Size(panel.Width - 20, 22),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                var indicator = new StatusIndicator
                {
                    Location = new Point(5, 3),
                    Size = new Size(16, 16)
                };

                if (!result.Success)
                    indicator.SetFailure();
                else if (result.Status == CheckStatus.Warning)
                    indicator.SetWarning();
                else
                    indicator.SetSuccess();

                var nameLabel = new Label
                {
                    Text = result.Name,
                    Location = new Point(28, 2),
                    Size = new Size(150, 20),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
                };

                var targetLabel = new Label
                {
                    Text = result.Target,
                    Location = new Point(180, 2),
                    Size = new Size(200, 20),
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    ForeColor = Color.Gray
                };

                var statusLabel = new Label
                {
                    Text = result.GetDisplayMessage(),
                    Location = new Point(390, 2),
                    Size = new Size(100, 20),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = result.Success
                        ? (result.Status == CheckStatus.Warning ? Color.FromArgb(255, 152, 0) : Color.FromArgb(40, 167, 69))
                        : Color.FromArgb(220, 53, 69)
                };

                rowPanel.Controls.AddRange(new Control[] { indicator, nameLabel, targetLabel, statusLabel });
                panel.Controls.Add(rowPanel);

                y += 25;
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            var text = _reportService.GenerateClipboardText(
                _computerInfo,
                _selectedInterface,
                _connectivityResults,
                _serviceResults);

            if (_reportService.CopyToClipboard(text))
            {
                MessageBox.Show("Report copied to clipboard!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed to copy to clipboard.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "HTML Report (*.html)|*.html|Text Report (*.txt)|*.txt";
                dialog.DefaultExt = "html";
                dialog.FileName = $"NetworkDiagnostic_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string content;
                    if (dialog.FilterIndex == 1) // HTML
                    {
                        content = _reportService.GenerateHtmlReport(
                            _computerInfo,
                            _selectedInterface,
                            _connectivityResults,
                            _serviceResults,
                            _config?.CompanyName);
                    }
                    else // Text
                    {
                        content = _reportService.GenerateTextReport(
                            _computerInfo,
                            _selectedInterface,
                            _connectivityResults,
                            _serviceResults,
                            _config?.CompanyName);
                    }

                    if (_reportService.SaveToFile(content, dialog.FileName))
                    {
                        MessageBox.Show($"Report saved to:\n{dialog.FileName}", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to save report.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ConfigButton_Click(object sender, EventArgs e)
        {
            using (var configForm = new ConfigForm(_config, _configService))
            {
                if (configForm.ShowDialog(this) == DialogResult.OK)
                {
                    _config = configForm.Configuration;
                    _companyLabel.Text = _config.CompanyName;
                    UpdateAutoRefreshSettings();
                }
            }
        }

        private void UpdateAutoRefreshSettings()
        {
            _autoRefreshTimer.Stop();

            if (_config.AutoRefreshSeconds > 0)
            {
                _autoRefreshTimer.Interval = _config.AutoRefreshSeconds * 1000;
                _autoRefreshTimer.Start();
                _autoRefreshCheckbox.Checked = true;
            }
            else
            {
                _autoRefreshCheckbox.Checked = false;
            }

            UpdateAutoRefreshLabel();
        }

        private void AutoRefreshCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (_autoRefreshCheckbox.Checked)
            {
                if (_config.AutoRefreshSeconds <= 0)
                {
                    _config.AutoRefreshSeconds = 30;
                }
                _autoRefreshTimer.Interval = _config.AutoRefreshSeconds * 1000;
                _autoRefreshTimer.Start();
            }
            else
            {
                _autoRefreshTimer.Stop();
            }
            UpdateAutoRefreshLabel();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_config?.MinimizeToTray == true && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MinimizeToTray();
            }
            else
            {
                _trayIcon.Visible = false;
                _autoRefreshTimer?.Stop();
            }
            base.OnFormClosing(e);
        }

        private void MinimizeToTray()
        {
            this.Hide();
            _trayIcon.Visible = true;
            _trayIcon.ShowBalloonTip(1000, "Network Diagnostic Tool",
                "Application minimized to system tray.", ToolTipIcon.Info);
        }

        private void ShowFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            _trayIcon.Visible = false;
        }

        private void ExitApplication()
        {
            _config.MinimizeToTray = false; // Prevent minimize on close
            _trayIcon.Visible = false;
            Application.Exit();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // Adjust groupbox widths
            var width = this.ClientSize.Width - 30;
            _computerGroupBox.Width = width;
            _networkGroupBox.Width = width;
            _connectivityGroupBox.Width = width;
            _servicesGroupBox.Width = width;
        }
    }
}
