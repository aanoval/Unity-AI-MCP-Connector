#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEngine;

namespace Alday.UnityAiConnector.Editor
{
    [Serializable]
    public sealed class UnityAiConnectorConfig
    {
        public string bindHost = "127.0.0.1";
        public int port = 6421;
        public bool authRequired = true;
        public bool autoStart = false;
        public bool allowDangerousTools = false;
        public string token = "";

        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath) ?? Directory.GetCurrentDirectory();
        public static string ConfigPath => Path.Combine(ProjectRoot, "UserSettings", "UnityAiConnector.json");

        public static UnityAiConnectorConfig LoadOrCreate()
        {
            UnityAiConnectorConfig config = null;

            if (File.Exists(ConfigPath))
            {
                config = JsonConvert.DeserializeObject<UnityAiConnectorConfig>(File.ReadAllText(ConfigPath));
            }

            config ??= new UnityAiConnectorConfig();

            if (string.IsNullOrWhiteSpace(config.bindHost))
                config.bindHost = "127.0.0.1";

            if (config.port <= 0 || config.port > 65535)
                config.port = 6421;

            if (string.IsNullOrWhiteSpace(config.token))
                config.token = GenerateToken();

            config.Save();
            return config;
        }

        public void Save()
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        static string GenerateToken()
        {
            var bytes = new byte[32];
            using (var generator = RandomNumberGenerator.Create())
                generator.GetBytes(bytes);

            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
#endif
