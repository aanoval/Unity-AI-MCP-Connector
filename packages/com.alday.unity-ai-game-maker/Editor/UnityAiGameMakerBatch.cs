#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace Alday.UnityAiGameMaker.Editor
{
    public static class UnityAiGameMakerBatch
    {
        sealed class BatchPayload
        {
            public List<BatchCommand> commands = new List<BatchCommand>();
        }

        sealed class BatchCommand
        {
            public string tool = "";
            public JObject args = new JObject();
        }

        public static void RunFromEnvironment()
        {
            var inputPath = Environment.GetEnvironmentVariable("UNITY_AI_GAME_MAKER_BATCH_FILE")
                ?? Environment.GetEnvironmentVariable("UNITY_AI_CONNECTOR_BATCH_FILE");
            var outputPath = Environment.GetEnvironmentVariable("UNITY_AI_GAME_MAKER_BATCH_OUT")
                ?? Environment.GetEnvironmentVariable("UNITY_AI_CONNECTOR_BATCH_OUT");

            if (string.IsNullOrWhiteSpace(inputPath))
                throw new InvalidOperationException("UNITY_AI_GAME_MAKER_BATCH_FILE is required.");

            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = inputPath + ".out.json";

            try
            {
                var payload = JsonConvert.DeserializeObject<BatchPayload>(File.ReadAllText(inputPath))
                    ?? new BatchPayload();
                var config = UnityAiGameMakerConfig.LoadOrCreate();
                var results = new List<object>();

                foreach (var command in payload.commands)
                {
                    results.Add(new
                    {
                        command.tool,
                        result = UnityAiTools.Invoke(command.tool, command.args ?? new JObject(), config)
                    });
                }

                WriteJson(outputPath, new { ok = true, results });
            }
            catch (Exception ex)
            {
                WriteJson(outputPath, new { ok = false, error = ex.ToString() });
                throw;
            }
        }

        static void WriteJson(string path, object payload)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonConvert.SerializeObject(payload, Formatting.Indented));
            AssetDatabase.Refresh();
        }
    }
}
#endif
