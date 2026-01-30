using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using NetworkDiagnosticTool.Models;

namespace NetworkDiagnosticTool.Services
{
    public class ConfigurationService
    {
        private readonly string _configPath;
        private const string EmbeddedResourceName = "NetworkDiagnosticTool.checks.json";

        public ConfigurationService()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var exeDir = Path.GetDirectoryName(exePath);
            _configPath = Path.Combine(exeDir, "checks.json");
        }

        public ConfigurationService(string configPath)
        {
            _configPath = configPath;
        }

        public string ConfigPath => _configPath;

        public AppConfiguration LoadConfiguration()
        {
            try
            {
                // If external config doesn't exist, extract from embedded resource
                if (!File.Exists(_configPath))
                {
                    ExtractEmbeddedConfig();
                }

                // If still doesn't exist (extraction failed), use default
                if (!File.Exists(_configPath))
                {
                    return LoadFromEmbeddedResource() ?? AppConfiguration.CreateDefault();
                }

                var json = File.ReadAllText(_configPath, Encoding.UTF8);
                return DeserializeJson<AppConfiguration>(json);
            }
            catch (Exception)
            {
                // Try loading from embedded resource as fallback
                return LoadFromEmbeddedResource() ?? AppConfiguration.CreateDefault();
            }
        }

        private void ExtractEmbeddedConfig()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(EmbeddedResourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            var content = reader.ReadToEnd();
                            File.WriteAllText(_configPath, content, Encoding.UTF8);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore extraction errors - will fall back to default
            }
        }

        private AppConfiguration LoadFromEmbeddedResource()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(EmbeddedResourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            var json = reader.ReadToEnd();
                            return DeserializeJson<AppConfiguration>(json);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore - will return null
            }
            return null;
        }

        public bool SaveConfiguration(AppConfiguration config)
        {
            try
            {
                var json = SerializeJson(config);
                File.WriteAllText(_configPath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ConfigurationExists()
        {
            return File.Exists(_configPath);
        }

        public bool ResetToDefault()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    File.Delete(_configPath);
                }
                ExtractEmbeddedConfig();
                return File.Exists(_configPath);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string SerializeJson<T>(T obj)
        {
            var serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            });

            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, obj);
                var bytes = stream.ToArray();
                var json = Encoding.UTF8.GetString(bytes);

                // Format the JSON for readability
                return FormatJson(json);
            }
        }

        private T DeserializeJson<T>(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (T)serializer.ReadObject(stream);
            }
        }

        private string FormatJson(string json)
        {
            var sb = new StringBuilder();
            var indent = 0;
            var inString = false;
            var escaped = false;

            foreach (var c in json)
            {
                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    sb.Append(c);
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    sb.Append(c);
                    continue;
                }

                if (inString)
                {
                    sb.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '[':
                        sb.Append(c);
                        sb.AppendLine();
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                        break;

                    case '}':
                    case ']':
                        sb.AppendLine();
                        indent--;
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(c);
                        break;

                    case ',':
                        sb.Append(c);
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 2));
                        break;

                    case ':':
                        sb.Append(c);
                        sb.Append(' ');
                        break;

                    default:
                        if (!char.IsWhiteSpace(c))
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
