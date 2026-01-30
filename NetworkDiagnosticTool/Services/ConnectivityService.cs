using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using NetworkDiagnosticTool.Models;

namespace NetworkDiagnosticTool.Services
{
    public class ConnectivityService
    {
        private const int DefaultTimeoutMs = 5000;
        private const int WarningLatencyMs = 100;

        public async Task<CheckResult> TestDnsResolution(string hostname, int timeoutMs = DefaultTimeoutMs)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var task = Dns.GetHostAddressesAsync(hostname);
                if (await Task.WhenAny(task, Task.Delay(timeoutMs)) == task)
                {
                    stopwatch.Stop();
                    var addresses = await task;

                    if (addresses != null && addresses.Length > 0)
                    {
                        var ipv4 = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork);
                        var resolvedIp = ipv4?.ToString() ?? addresses[0].ToString();
                        var latency = stopwatch.ElapsedMilliseconds;

                        if (latency > WarningLatencyMs)
                        {
                            return CheckResult.CreateWarning(
                                "DNS Resolution",
                                hostname,
                                $"{hostname} → {resolvedIp}",
                                latency);
                        }

                        return CheckResult.CreateSuccess(
                            "DNS Resolution",
                            hostname,
                            $"{hostname} → {resolvedIp}",
                            latency);
                    }

                    return CheckResult.CreateFailure(
                        "DNS Resolution",
                        hostname,
                        "No addresses returned");
                }
                else
                {
                    return CheckResult.CreateFailure(
                        "DNS Resolution",
                        hostname,
                        "Timeout",
                        $"DNS resolution timed out after {timeoutMs}ms");
                }
            }
            catch (Exception ex)
            {
                return CheckResult.CreateFailure(
                    "DNS Resolution",
                    hostname,
                    "FAIL",
                    ex.Message);
            }
        }

        public async Task<CheckResult> PingHost(string host, int timeoutMs = DefaultTimeoutMs)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(host, timeoutMs);

                    if (reply.Status == IPStatus.Success)
                    {
                        var latency = reply.RoundtripTime;

                        if (latency > WarningLatencyMs)
                        {
                            return CheckResult.CreateWarning(
                                "Ping",
                                host,
                                $"{latency}ms",
                                latency);
                        }

                        return CheckResult.CreateSuccess(
                            "Ping",
                            host,
                            $"{latency}ms",
                            latency);
                    }

                    return CheckResult.CreateFailure(
                        "Ping",
                        host,
                        "FAIL",
                        $"Ping status: {reply.Status}");
                }
            }
            catch (PingException ex)
            {
                return CheckResult.CreateFailure(
                    "Ping",
                    host,
                    "FAIL",
                    ex.InnerException?.Message ?? ex.Message);
            }
            catch (Exception ex)
            {
                return CheckResult.CreateFailure(
                    "Ping",
                    host,
                    "FAIL",
                    ex.Message);
            }
        }

        public async Task<CheckResult> TestTcpPort(string host, int port, int timeoutMs = DefaultTimeoutMs)
        {
            var target = $"{host}:{port}";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);

                    if (await Task.WhenAny(connectTask, Task.Delay(timeoutMs)) == connectTask)
                    {
                        stopwatch.Stop();
                        await connectTask; // Propagate exceptions

                        if (client.Connected)
                        {
                            var latency = stopwatch.ElapsedMilliseconds;

                            if (latency > WarningLatencyMs)
                            {
                                return CheckResult.CreateWarning(
                                    "TCP Port",
                                    target,
                                    $"{latency}ms",
                                    latency);
                            }

                            return CheckResult.CreateSuccess(
                                "TCP Port",
                                target,
                                "OK",
                                latency);
                        }
                    }

                    return CheckResult.CreateFailure(
                        "TCP Port",
                        target,
                        "Timeout",
                        $"Connection timed out after {timeoutMs}ms");
                }
            }
            catch (SocketException ex)
            {
                return CheckResult.CreateFailure(
                    "TCP Port",
                    target,
                    "FAIL",
                    $"Socket error: {ex.SocketErrorCode}");
            }
            catch (Exception ex)
            {
                return CheckResult.CreateFailure(
                    "TCP Port",
                    target,
                    "FAIL",
                    ex.Message);
            }
        }

        public async Task<CheckResult> TestHttpEndpoint(string url, int timeoutMs = DefaultTimeoutMs)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Use HttpWebRequest for Windows 7 compatibility
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = timeoutMs;
                request.ReadWriteTimeout = timeoutMs;
                request.AllowAutoRedirect = true;
                request.UserAgent = "NetworkDiagnosticTool/1.0";

                // Ignore SSL certificate errors for diagnostic purposes
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, certificate, chain, sslPolicyErrors) => true;

                var responseTask = Task.Factory.FromAsync(
                    request.BeginGetResponse,
                    request.EndGetResponse,
                    null);

                if (await Task.WhenAny(responseTask, Task.Delay(timeoutMs)) == responseTask)
                {
                    using (var response = (HttpWebResponse)await responseTask)
                    {
                        stopwatch.Stop();
                        var latency = stopwatch.ElapsedMilliseconds;
                        var statusCode = (int)response.StatusCode;

                        if (statusCode >= 200 && statusCode < 400)
                        {
                            if (latency > WarningLatencyMs)
                            {
                                return CheckResult.CreateWarning(
                                    "HTTP",
                                    url,
                                    $"{statusCode} ({latency}ms)",
                                    latency);
                            }

                            return CheckResult.CreateSuccess(
                                "HTTP",
                                url,
                                $"{statusCode} OK",
                                latency);
                        }

                        return CheckResult.CreateFailure(
                            "HTTP",
                            url,
                            $"HTTP {statusCode}",
                            $"Server returned status code {statusCode}");
                    }
                }

                request.Abort();
                return CheckResult.CreateFailure(
                    "HTTP",
                    url,
                    "Timeout",
                    $"Request timed out after {timeoutMs}ms");
            }
            catch (WebException ex)
            {
                if (ex.Response is HttpWebResponse errorResponse)
                {
                    var statusCode = (int)errorResponse.StatusCode;
                    return CheckResult.CreateFailure(
                        "HTTP",
                        url,
                        $"HTTP {statusCode}",
                        ex.Message);
                }

                return CheckResult.CreateFailure(
                    "HTTP",
                    url,
                    "FAIL",
                    ex.Message);
            }
            catch (Exception ex)
            {
                return CheckResult.CreateFailure(
                    "HTTP",
                    url,
                    "FAIL",
                    ex.Message);
            }
        }

        public async Task<CheckResult> ExecuteCustomCheck(CustomCheck check)
        {
            CheckResult result;

            switch (check.Type?.ToLower())
            {
                case "ping":
                    result = await PingHost(check.Host, check.TimeoutMs);
                    result.Name = check.Name;
                    result.Target = check.Host;
                    break;

                case "tcp":
                    result = await TestTcpPort(check.Host, check.Port ?? 80, check.TimeoutMs);
                    result.Name = check.Name;
                    result.Target = $"{check.Host}:{check.Port}";
                    break;

                case "http":
                    result = await TestHttpEndpoint(check.Url, check.TimeoutMs);
                    result.Name = check.Name;
                    result.Target = check.Url;
                    break;

                default:
                    result = CheckResult.CreateFailure(
                        check.Name,
                        check.GetTarget(),
                        "Unknown check type",
                        $"Check type '{check.Type}' is not supported");
                    break;
            }

            return result;
        }

        public async Task<CheckResult> TestGatewayPing(string gatewayIp)
        {
            if (string.IsNullOrEmpty(gatewayIp) || gatewayIp == "N/A")
            {
                return CheckResult.CreateFailure(
                    "Gateway Ping",
                    "N/A",
                    "No gateway",
                    "No default gateway configured");
            }

            var result = await PingHost(gatewayIp);
            result.Name = "Gateway Ping";
            return result;
        }

        public async Task<CheckResult> TestInternetConnectivity()
        {
            var result = await PingHost("1.1.1.1");
            result.Name = "Internet";
            result.Target = "1.1.1.1";
            return result;
        }
    }
}
