using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NetworkDiagnosticTool.Models
{
    [DataContract]
    public class AppConfiguration
    {
        [DataMember(Name = "companyName")]
        public string CompanyName { get; set; }

        [DataMember(Name = "autoRefreshSeconds")]
        public int AutoRefreshSeconds { get; set; }

        [DataMember(Name = "checks")]
        public List<CustomCheck> Checks { get; set; }

        [DataMember(Name = "minimizeToTray")]
        public bool MinimizeToTray { get; set; }

        [DataMember(Name = "startMinimized")]
        public bool StartMinimized { get; set; }

        [DataMember(Name = "showBalloonNotifications")]
        public bool ShowBalloonNotifications { get; set; }

        public AppConfiguration()
        {
            Checks = new List<CustomCheck>();
            AutoRefreshSeconds = 30;
            MinimizeToTray = true;
            ShowBalloonNotifications = true;
        }

        public static AppConfiguration CreateDefault()
        {
            return new AppConfiguration
            {
                CompanyName = "Network Diagnostic Tool",
                AutoRefreshSeconds = 30,
                MinimizeToTray = true,
                ShowBalloonNotifications = true,
                Checks = new List<CustomCheck>()
            };
        }
    }
}
