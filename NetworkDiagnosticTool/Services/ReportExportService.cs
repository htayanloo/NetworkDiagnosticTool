using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NetworkDiagnosticTool.Models;

namespace NetworkDiagnosticTool.Services
{
    public class ReportExportService
    {
        public string GenerateTextReport(
            ComputerInfo computerInfo,
            NetworkInterfaceInfo networkInfo,
            List<CheckResult> connectivityResults,
            List<CheckResult> serviceResults,
            string companyName)
        {
            var sb = new StringBuilder();
            var separator = new string('=', 60);
            var subSeparator = new string('-', 60);

            sb.AppendLine(separator);
            sb.AppendLine($"  NETWORK DIAGNOSTIC REPORT");
            if (!string.IsNullOrEmpty(companyName))
            {
                sb.AppendLine($"  {companyName}");
            }
            sb.AppendLine($"  Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(separator);
            sb.AppendLine();

            // Computer Information
            sb.AppendLine("COMPUTER INFORMATION");
            sb.AppendLine(subSeparator);
            sb.AppendLine($"  Username:       {computerInfo?.Username ?? "N/A"}");
            sb.AppendLine($"  Computer Name:  {computerInfo?.ComputerName ?? "N/A"}");
            sb.AppendLine($"  Domain:         {computerInfo?.DomainOrWorkgroup ?? "N/A"}");
            sb.AppendLine();

            // Network Adapter Information
            sb.AppendLine("NETWORK ADAPTER");
            sb.AppendLine(subSeparator);
            if (networkInfo != null)
            {
                sb.AppendLine($"  Adapter:        {networkInfo.Name}");
                sb.AppendLine($"  Description:    {networkInfo.Description}");
                sb.AppendLine($"  Status:         {networkInfo.GetStatusDisplay()}");
                sb.AppendLine($"  Type:           {networkInfo.InterfaceType}");
                sb.AppendLine($"  Speed:          {networkInfo.GetFormattedSpeed()}");
                sb.AppendLine($"  MAC Address:    {networkInfo.MacAddress}");
                sb.AppendLine();
                sb.AppendLine($"  IP Address:     {networkInfo.IPAddress}");
                sb.AppendLine($"  Subnet Mask:    {networkInfo.SubnetMask}");
                sb.AppendLine($"  Gateway:        {networkInfo.Gateway}");
                sb.AppendLine($"  DHCP:           {(networkInfo.IsDhcp ? "Enabled" : "Static")}");

                if (networkInfo.DnsServers != null && networkInfo.DnsServers.Any())
                {
                    for (int i = 0; i < networkInfo.DnsServers.Count; i++)
                    {
                        sb.AppendLine($"  DNS Server {i + 1}:   {networkInfo.DnsServers[i]}");
                    }
                }
            }
            else
            {
                sb.AppendLine("  No network adapter information available");
            }
            sb.AppendLine();

            // Connectivity Tests
            sb.AppendLine("CONNECTIVITY TESTS");
            sb.AppendLine(subSeparator);
            if (connectivityResults != null && connectivityResults.Any())
            {
                foreach (var result in connectivityResults)
                {
                    var status = result.Success ? "[OK]  " : "[FAIL]";
                    var latency = result.LatencyMs.HasValue ? $" ({result.LatencyMs}ms)" : "";
                    sb.AppendLine($"  {status} {result.Name,-20} {result.Target,-25} {result.Message}{latency}");
                }
            }
            else
            {
                sb.AppendLine("  No connectivity tests performed");
            }
            sb.AppendLine();

            // Service Checks
            if (serviceResults != null && serviceResults.Any())
            {
                sb.AppendLine("SERVICE CHECKS");
                sb.AppendLine(subSeparator);
                foreach (var result in serviceResults)
                {
                    var status = result.Success ? "[OK]  " : "[FAIL]";
                    var latency = result.LatencyMs.HasValue ? $" ({result.LatencyMs}ms)" : "";
                    sb.AppendLine($"  {status} {result.Name,-20} {result.Target,-25} {result.Message}{latency}");
                }
                sb.AppendLine();
            }

            sb.AppendLine(separator);
            sb.AppendLine("  End of Report");
            sb.AppendLine(separator);

            return sb.ToString();
        }

        public string GenerateHtmlReport(
            ComputerInfo computerInfo,
            NetworkInterfaceInfo networkInfo,
            List<CheckResult> connectivityResults,
            List<CheckResult> serviceResults,
            string companyName)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"  <title>Network Diagnostic Report - {DateTime.Now:yyyy-MM-dd}</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; background: #f5f5f5; }");
            sb.AppendLine("    .container { max-width: 800px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            sb.AppendLine("    h1 { color: #333; border-bottom: 3px solid #0066cc; padding-bottom: 10px; margin-bottom: 5px; }");
            sb.AppendLine("    .subtitle { color: #666; margin-bottom: 20px; }");
            sb.AppendLine("    h2 { color: #0066cc; margin-top: 25px; margin-bottom: 15px; font-size: 1.2em; }");
            sb.AppendLine("    .section { background: #fafafa; border: 1px solid #e0e0e0; border-radius: 4px; padding: 15px; margin-bottom: 20px; }");
            sb.AppendLine("    .info-row { display: flex; padding: 8px 0; border-bottom: 1px solid #eee; }");
            sb.AppendLine("    .info-row:last-child { border-bottom: none; }");
            sb.AppendLine("    .info-label { font-weight: 600; color: #555; width: 150px; flex-shrink: 0; }");
            sb.AppendLine("    .info-value { color: #333; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; }");
            sb.AppendLine("    th { background: #0066cc; color: white; padding: 10px; text-align: left; }");
            sb.AppendLine("    td { padding: 10px; border-bottom: 1px solid #eee; }");
            sb.AppendLine("    tr:hover { background: #f0f7ff; }");
            sb.AppendLine("    .status-ok { color: #28a745; font-weight: bold; }");
            sb.AppendLine("    .status-warning { color: #ffc107; font-weight: bold; }");
            sb.AppendLine("    .status-fail { color: #dc3545; font-weight: bold; }");
            sb.AppendLine("    .status-indicator { display: inline-block; width: 12px; height: 12px; border-radius: 50%; margin-right: 8px; }");
            sb.AppendLine("    .indicator-ok { background: #28a745; }");
            sb.AppendLine("    .indicator-warning { background: #ffc107; }");
            sb.AppendLine("    .indicator-fail { background: #dc3545; }");
            sb.AppendLine("    .footer { margin-top: 30px; padding-top: 15px; border-top: 1px solid #e0e0e0; color: #888; font-size: 0.9em; text-align: center; }");
            sb.AppendLine("    .dhcp-badge { display: inline-block; padding: 3px 10px; border-radius: 12px; font-size: 0.85em; font-weight: 600; }");
            sb.AppendLine("    .dhcp-enabled { background: #e3f2fd; color: #1976d2; }");
            sb.AppendLine("    .dhcp-static { background: #fff3e0; color: #e65100; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class=\"container\">");

            // Header
            sb.AppendLine("    <h1>Network Diagnostic Report</h1>");
            if (!string.IsNullOrEmpty(companyName))
            {
                sb.AppendLine($"    <div class=\"subtitle\">{HtmlEncode(companyName)}</div>");
            }
            sb.AppendLine($"    <div class=\"subtitle\">Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>");

            // Computer Information
            sb.AppendLine("    <h2>Computer Information</h2>");
            sb.AppendLine("    <div class=\"section\">");
            AppendInfoRow(sb, "Username", computerInfo?.Username);
            AppendInfoRow(sb, "Computer Name", computerInfo?.ComputerName);
            AppendInfoRow(sb, "Domain/Workgroup", computerInfo?.DomainOrWorkgroup);
            sb.AppendLine("    </div>");

            // Network Adapter
            sb.AppendLine("    <h2>Network Adapter</h2>");
            sb.AppendLine("    <div class=\"section\">");
            if (networkInfo != null)
            {
                AppendInfoRow(sb, "Adapter", networkInfo.Name);
                AppendInfoRow(sb, "Description", networkInfo.Description);
                AppendInfoRow(sb, "Status", networkInfo.GetStatusDisplay(),
                    networkInfo.Status == "Up" ? "status-ok" : "status-fail");
                AppendInfoRow(sb, "Type", networkInfo.InterfaceType);
                AppendInfoRow(sb, "Speed", networkInfo.GetFormattedSpeed());
                AppendInfoRow(sb, "MAC Address", networkInfo.MacAddress);

                var dhcpHtml = networkInfo.IsDhcp
                    ? "<span class=\"dhcp-badge dhcp-enabled\">DHCP</span>"
                    : "<span class=\"dhcp-badge dhcp-static\">Static</span>";
                AppendInfoRowRaw(sb, "IP Configuration", dhcpHtml);

                AppendInfoRow(sb, "IP Address", networkInfo.IPAddress);
                AppendInfoRow(sb, "Subnet Mask", networkInfo.SubnetMask);
                AppendInfoRow(sb, "Gateway", networkInfo.Gateway);

                if (networkInfo.DnsServers != null)
                {
                    for (int i = 0; i < networkInfo.DnsServers.Count; i++)
                    {
                        AppendInfoRow(sb, $"DNS Server {i + 1}", networkInfo.DnsServers[i]);
                    }
                }
            }
            else
            {
                sb.AppendLine("      <p>No network adapter information available</p>");
            }
            sb.AppendLine("    </div>");

            // Connectivity Tests
            sb.AppendLine("    <h2>Connectivity Tests</h2>");
            AppendResultsTable(sb, connectivityResults);

            // Service Checks
            if (serviceResults != null && serviceResults.Any())
            {
                sb.AppendLine("    <h2>Service Checks</h2>");
                AppendResultsTable(sb, serviceResults);
            }

            // Footer
            sb.AppendLine("    <div class=\"footer\">");
            sb.AppendLine("      Generated by Network Diagnostic Tool");
            sb.AppendLine("    </div>");

            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        public string GenerateClipboardText(
            ComputerInfo computerInfo,
            NetworkInterfaceInfo networkInfo,
            List<CheckResult> connectivityResults,
            List<CheckResult> serviceResults)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Network Diagnostic Summary ===");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine($"User: {computerInfo?.Username} @ {computerInfo?.ComputerName}");
            sb.AppendLine($"Domain: {computerInfo?.DomainOrWorkgroup}");
            sb.AppendLine();

            if (networkInfo != null)
            {
                sb.AppendLine($"Adapter: {networkInfo.Name} ({networkInfo.GetStatusDisplay()})");
                sb.AppendLine($"IP: {networkInfo.IPAddress} / {networkInfo.SubnetMask}");
                sb.AppendLine($"Gateway: {networkInfo.Gateway}");
                sb.AppendLine($"DHCP: {(networkInfo.IsDhcp ? "Yes" : "No")}");
                if (networkInfo.DnsServers != null && networkInfo.DnsServers.Any())
                {
                    sb.AppendLine($"DNS: {string.Join(", ", networkInfo.DnsServers)}");
                }
            }
            sb.AppendLine();

            if (connectivityResults != null && connectivityResults.Any())
            {
                sb.AppendLine("Connectivity:");
                foreach (var r in connectivityResults)
                {
                    var status = r.Success ? "OK" : "FAIL";
                    var latency = r.LatencyMs.HasValue ? $" {r.LatencyMs}ms" : "";
                    sb.AppendLine($"  [{status}] {r.Name}: {r.Message}{latency}");
                }
            }

            if (serviceResults != null && serviceResults.Any())
            {
                sb.AppendLine("Services:");
                foreach (var r in serviceResults)
                {
                    var status = r.Success ? "OK" : "FAIL";
                    var latency = r.LatencyMs.HasValue ? $" {r.LatencyMs}ms" : "";
                    sb.AppendLine($"  [{status}] {r.Name}: {r.Message}{latency}");
                }
            }

            return sb.ToString();
        }

        public bool CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SaveToFile(string content, string filePath)
        {
            try
            {
                System.IO.File.WriteAllText(filePath, content, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void AppendInfoRow(StringBuilder sb, string label, string value, string cssClass = null)
        {
            var valueHtml = HtmlEncode(value ?? "N/A");
            if (!string.IsNullOrEmpty(cssClass))
            {
                valueHtml = $"<span class=\"{cssClass}\">{valueHtml}</span>";
            }
            sb.AppendLine($"      <div class=\"info-row\"><span class=\"info-label\">{HtmlEncode(label)}:</span><span class=\"info-value\">{valueHtml}</span></div>");
        }

        private void AppendInfoRowRaw(StringBuilder sb, string label, string valueHtml)
        {
            sb.AppendLine($"      <div class=\"info-row\"><span class=\"info-label\">{HtmlEncode(label)}:</span><span class=\"info-value\">{valueHtml}</span></div>");
        }

        private void AppendResultsTable(StringBuilder sb, List<CheckResult> results)
        {
            sb.AppendLine("    <div class=\"section\">");
            if (results != null && results.Any())
            {
                sb.AppendLine("      <table>");
                sb.AppendLine("        <tr><th>Status</th><th>Test</th><th>Target</th><th>Result</th></tr>");
                foreach (var result in results)
                {
                    string indicatorClass, statusClass;
                    if (!result.Success)
                    {
                        indicatorClass = "indicator-fail";
                        statusClass = "status-fail";
                    }
                    else if (result.Status == CheckStatus.Warning)
                    {
                        indicatorClass = "indicator-warning";
                        statusClass = "status-warning";
                    }
                    else
                    {
                        indicatorClass = "indicator-ok";
                        statusClass = "status-ok";
                    }

                    var resultText = result.LatencyMs.HasValue
                        ? $"{result.Message} ({result.LatencyMs}ms)"
                        : result.Message;

                    sb.AppendLine($"        <tr>");
                    sb.AppendLine($"          <td><span class=\"status-indicator {indicatorClass}\"></span></td>");
                    sb.AppendLine($"          <td>{HtmlEncode(result.Name)}</td>");
                    sb.AppendLine($"          <td>{HtmlEncode(result.Target)}</td>");
                    sb.AppendLine($"          <td class=\"{statusClass}\">{HtmlEncode(resultText)}</td>");
                    sb.AppendLine($"        </tr>");
                }
                sb.AppendLine("      </table>");
            }
            else
            {
                sb.AppendLine("      <p>No tests performed</p>");
            }
            sb.AppendLine("    </div>");
        }

        private string HtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text)) return "N/A";
            return System.Net.WebUtility.HtmlEncode(text);
        }
    }
}
