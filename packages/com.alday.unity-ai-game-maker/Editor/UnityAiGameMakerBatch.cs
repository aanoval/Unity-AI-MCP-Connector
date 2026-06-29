#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

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

        [MenuItem("Tools/Unity AI Game Maker/Run Batch File...")]
        public static void RunBatchFromFileDialog()
        {
            var inputPath = EditorUtility.OpenFilePanel(
                "Unity AI Game Maker Batch",
                UnityAiGameMakerConfig.ProjectRoot,
                "json");
            if (string.IsNullOrWhiteSpace(inputPath))
                return;

            var outputPath = inputPath + ".out.json";
            try
            {
                var result = RunBatchFile(inputPath, outputPath);
                Debug.Log($"Unity AI Game Maker batch completed: {outputPath}\n{JsonConvert.SerializeObject(result, Formatting.Indented)}");
                EditorUtility.RevealInFinder(outputPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unity AI Game Maker batch failed: {ex.Message}");
            }
        }

        [MenuItem("Tools/Unity AI Game Maker/Capture Menu Screenshots")]
        public static void CaptureMenuScreenshotsFromMenu()
        {
            try
            {
                var result = UnityAiScreenshotTools.CaptureScenes(new JObject
                {
                    ["filter"] = "menu",
                    ["width"] = 1080,
                    ["height"] = 1920,
                    ["source"] = "camera"
                });
                Debug.Log($"Unity AI Game Maker menu screenshots completed:\n{JsonConvert.SerializeObject(result, Formatting.Indented)}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unity AI Game Maker menu screenshot capture failed: {ex.Message}");
            }
        }

        public static void RunFromEnvironment()
        {
            var inputPath = Environment.GetEnvironmentVariable("UNITY_AI_GAME_MAKER_BATCH_FILE")
                ?? Environment.GetEnvironmentVariable("UNITY_AI_CONNECTOR_BATCH_FILE");
            var outputPath = Environment.GetEnvironmentVariable("UNITY_AI_GAME_MAKER_BATCH_OUT")
                ?? Environment.GetEnvironmentVariable("UNITY_AI_CONNECTOR_BATCH_OUT");

            if (string.IsNullOrWhiteSpace(inputPath))
                throw new InvalidOperationException(
                    "UNITY_AI_GAME_MAKER_BATCH_FILE is required. " +
                    "Example: Unity -batchmode -quit -executeMethod Alday.UnityAiGameMaker.Editor.UnityAiGameMakerBatch.RunFromEnvironment");

            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = inputPath + ".out.json";

            RunBatchFile(inputPath, outputPath);
        }

        public static object RunBatchFile(string inputPath, string outputPath)
        {
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

                var response = new { ok = true, results };
                WriteJson(outputPath, response);
                return response;
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
