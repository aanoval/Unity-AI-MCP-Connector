#if UNITY_EDITOR
using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEngine;

namespace Alday.UnityAiGameMaker.Editor
{
    [Serializable]
    public sealed class UnityAiGameMakerConfig
    {
        public string bindHost = "127.0.0.1";
        public int port = 6421;
        public bool authRequired = true;
        public bool autoStart = true;
        public bool allowDangerousTools = false;
        public string token = "";

        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath) ?? Directory.GetCurrentDirectory();
        public static string ConfigPath => Path.Combine(ProjectRoot, "UserSettings", "UnityAiGameMaker.json");

        public static UnityAiGameMakerConfig LoadOrCreate()
        {
            UnityAiGameMakerConfig config = null;

            if (File.Exists(ConfigPath))
            {
                config = JsonConvert.DeserializeObject<UnityAiGameMakerConfig>(File.ReadAllText(ConfigPath));
            }

            config ??= new UnityAiGameMakerConfig();

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
