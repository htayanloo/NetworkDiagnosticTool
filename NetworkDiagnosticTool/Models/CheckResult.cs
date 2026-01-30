using System;

namespace NetworkDiagnosticTool.Models
{
    public enum CheckStatus
    {
        Unknown,
        Checking,
        Success,
        Warning,
        Failure
    }

    public class CheckResult
    {
        public string Name { get; set; }
        public string Target { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public long? LatencyMs { get; set; }
        public DateTime Timestamp { get; set; }
        public CheckStatus Status { get; set; }
        public string ErrorDetails { get; set; }

        public CheckResult()
        {
            Timestamp = DateTime.Now;
            Status = CheckStatus.Unknown;
        }

        public static CheckResult CreateSuccess(string name, string target, string message, long? latencyMs = null)
        {
            return new CheckResult
            {
                Name = name,
                Target = target,
                Success = true,
                Message = message,
                LatencyMs = latencyMs,
                Status = CheckStatus.Success,
                Timestamp = DateTime.Now
            };
        }

        public static CheckResult CreateFailure(string name, string target, string message, string errorDetails = null)
        {
            return new CheckResult
            {
                Name = name,
                Target = target,
                Success = false,
                Message = message,
                Status = CheckStatus.Failure,
                ErrorDetails = errorDetails,
                Timestamp = DateTime.Now
            };
        }

        public static CheckResult CreateWarning(string name, string target, string message, long? latencyMs = null)
        {
            return new CheckResult
            {
                Name = name,
                Target = target,
                Success = true,
                Message = message,
                LatencyMs = latencyMs,
                Status = CheckStatus.Warning,
                Timestamp = DateTime.Now
            };
        }

        public static CheckResult CreateChecking(string name, string target)
        {
            return new CheckResult
            {
                Name = name,
                Target = target,
                Success = false,
                Message = "Checking...",
                Status = CheckStatus.Checking,
                Timestamp = DateTime.Now
            };
        }

        public string GetDisplayMessage()
        {
            if (LatencyMs.HasValue && Success)
            {
                return $"{LatencyMs}ms";
            }
            return Message ?? (Success ? "OK" : "FAIL");
        }
    }
}
