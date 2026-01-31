using System.Collections.Generic;

namespace NetworkDiagnosticTool.Models
{
    public class NetworkInterfaceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IPAddress { get; set; }
        public string SubnetMask { get; set; }
        public string Gateway { get; set; }
        public List<string> DnsServers { get; set; }
        public bool IsDhcp { get; set; }
        public string Status { get; set; }
        public string MacAddress { get; set; }
        public string InterfaceType { get; set; }
        public long Speed { get; set; }

        // WiFi specific
        public bool IsWiFi { get; set; }
        public string SSID { get; set; }
        public int SignalQuality { get; set; }

        public NetworkInterfaceInfo()
        {
            DnsServers = new List<string>();
        }

        public string GetStatusDisplay()
        {
            switch (Status?.ToLower())
            {
                case "up":
                    return "Connected";
                case "down":
                    return "Disconnected";
                default:
                    return Status ?? "Unknown";
            }
        }

        public string GetFormattedSpeed()
        {
            if (Speed <= 0) return "Unknown";
            if (Speed >= 1000000000) return $"{Speed / 1000000000} Gbps";
            if (Speed >= 1000000) return $"{Speed / 1000000} Mbps";
            if (Speed >= 1000) return $"{Speed / 1000} Kbps";
            return $"{Speed} bps";
        }
    }
}
