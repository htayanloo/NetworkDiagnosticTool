using System.Runtime.Serialization;

namespace NetworkDiagnosticTool.Models
{
    [DataContract]
    public class CustomCheck
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "host")]
        public string Host { get; set; }

        [DataMember(Name = "port")]
        public int? Port { get; set; }

        [DataMember(Name = "url")]
        public string Url { get; set; }

        [DataMember(Name = "timeoutMs")]
        public int TimeoutMs { get; set; } = 5000;

        public string GetTarget()
        {
            switch (Type?.ToLower())
            {
                case "http":
                    return Url ?? "N/A";
                case "tcp":
                case "udp":
                    return $"{Host ?? "unknown"}:{Port}";
                case "ping":
                default:
                    return Host ?? "N/A";
            }
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Name)) return false;
            if (string.IsNullOrWhiteSpace(Type)) return false;

            switch (Type.ToLower())
            {
                case "http":
                    return !string.IsNullOrWhiteSpace(Url);
                case "tcp":
                case "udp":
                    return !string.IsNullOrWhiteSpace(Host) && Port.HasValue && Port > 0 && Port <= 65535;
                case "ping":
                    return !string.IsNullOrWhiteSpace(Host);
                default:
                    return false;
            }
        }
    }
}
