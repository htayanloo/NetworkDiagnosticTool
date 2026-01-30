namespace NetworkDiagnosticTool.Models
{
    public class ComputerInfo
    {
        public string Username { get; set; }
        public string ComputerName { get; set; }
        public string DomainOrWorkgroup { get; set; }
        public bool IsDomainJoined { get; set; }
    }
}
