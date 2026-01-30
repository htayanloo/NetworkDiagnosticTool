using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NetworkDiagnosticTool.Models;

namespace NetworkDiagnosticTool.Services
{
    public class NetworkInfoService
    {
        public ComputerInfo GetComputerInfo()
        {
            var info = new ComputerInfo
            {
                Username = Environment.UserName,
                ComputerName = Environment.MachineName
            };

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var partOfDomain = obj["PartOfDomain"];
                        info.IsDomainJoined = partOfDomain != null && (bool)partOfDomain;

                        if (info.IsDomainJoined)
                        {
                            info.DomainOrWorkgroup = obj["Domain"]?.ToString() ?? "Unknown";
                        }
                        else
                        {
                            info.DomainOrWorkgroup = obj["Workgroup"]?.ToString() ?? "WORKGROUP";
                        }
                        break;
                    }
                }
            }
            catch (Exception)
            {
                info.DomainOrWorkgroup = Environment.UserDomainName;
                info.IsDomainJoined = !string.Equals(info.DomainOrWorkgroup, info.ComputerName,
                    StringComparison.OrdinalIgnoreCase);
            }

            return info;
        }

        public List<NetworkInterfaceInfo> GetNetworkInterfaces()
        {
            var interfaces = new List<NetworkInterfaceInfo>();

            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                 ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .OrderByDescending(ni => ni.OperationalStatus == OperationalStatus.Up)
                    .ThenBy(ni => ni.Name);

                foreach (var ni in networkInterfaces)
                {
                    var interfaceInfo = new NetworkInterfaceInfo
                    {
                        Id = ni.Id,
                        Name = ni.Name,
                        Description = ni.Description,
                        Status = ni.OperationalStatus.ToString(),
                        MacAddress = FormatMacAddress(ni.GetPhysicalAddress()),
                        InterfaceType = ni.NetworkInterfaceType.ToString(),
                        Speed = ni.Speed
                    };

                    try
                    {
                        var ipProperties = ni.GetIPProperties();

                        var ipv4Address = ipProperties.UnicastAddresses
                            .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork);

                        if (ipv4Address != null)
                        {
                            interfaceInfo.IPAddress = ipv4Address.Address.ToString();
                            interfaceInfo.SubnetMask = ipv4Address.IPv4Mask?.ToString() ?? "N/A";
                        }
                        else
                        {
                            interfaceInfo.IPAddress = "No IPv4";
                            interfaceInfo.SubnetMask = "N/A";
                        }

                        var gateway = ipProperties.GatewayAddresses
                            .FirstOrDefault(ga => ga.Address.AddressFamily == AddressFamily.InterNetwork);
                        interfaceInfo.Gateway = gateway?.Address.ToString() ?? "N/A";

                        interfaceInfo.DnsServers = ipProperties.DnsAddresses
                            .Where(dns => dns.AddressFamily == AddressFamily.InterNetwork)
                            .Select(dns => dns.ToString())
                            .ToList();

                        interfaceInfo.IsDhcp = GetDhcpStatus(ni);
                    }
                    catch (Exception)
                    {
                        interfaceInfo.IPAddress = "Error";
                        interfaceInfo.SubnetMask = "Error";
                        interfaceInfo.Gateway = "Error";
                    }

                    interfaces.Add(interfaceInfo);
                }
            }
            catch (Exception)
            {
                // Return empty list if we can't enumerate interfaces
            }

            return interfaces;
        }

        public NetworkInterfaceInfo GetPrimaryNetworkInterface()
        {
            var interfaces = GetNetworkInterfaces();

            // Prefer connected ethernet, then connected wireless, then any connected
            return interfaces.FirstOrDefault(i =>
                       i.Status == "Up" &&
                       i.InterfaceType == "Ethernet" &&
                       !string.IsNullOrEmpty(i.IPAddress) &&
                       i.IPAddress != "No IPv4") ??
                   interfaces.FirstOrDefault(i =>
                       i.Status == "Up" &&
                       i.InterfaceType.Contains("Wireless") &&
                       !string.IsNullOrEmpty(i.IPAddress) &&
                       i.IPAddress != "No IPv4") ??
                   interfaces.FirstOrDefault(i =>
                       i.Status == "Up" &&
                       !string.IsNullOrEmpty(i.IPAddress) &&
                       i.IPAddress != "No IPv4") ??
                   interfaces.FirstOrDefault();
        }

        public string GetDefaultGateway()
        {
            var primary = GetPrimaryNetworkInterface();
            return primary?.Gateway;
        }

        private bool GetDhcpStatus(NetworkInterface ni)
        {
            try
            {
                var ipProperties = ni.GetIPProperties();

                if (ipProperties.GetIPv4Properties() != null)
                {
                    return ipProperties.GetIPv4Properties().IsDhcpEnabled;
                }
            }
            catch (Exception)
            {
                // Some adapters don't support IPv4 properties
            }

            // Fallback: use WMI
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_NetworkAdapterConfiguration WHERE Description = '{ni.Description}'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var dhcpEnabled = obj["DHCPEnabled"];
                        if (dhcpEnabled != null)
                        {
                            return (bool)dhcpEnabled;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore WMI errors
            }

            return false;
        }

        private string FormatMacAddress(PhysicalAddress address)
        {
            if (address == null) return "N/A";

            var bytes = address.GetAddressBytes();
            if (bytes.Length == 0) return "N/A";

            return string.Join("-", bytes.Select(b => b.ToString("X2")));
        }
    }
}
