#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Alday.UnityAiConnector.Editor
{
    [InitializeOnLoad]
    public static class UnityAiConnectorServer
    {
        static HttpListener listener;
        static Thread serverThread;
        static bool isRunning;
        static UnityAiConnectorConfig config;

        static UnityAiConnectorServer()
        {
            config = UnityAiConnectorConfig.LoadOrCreate();
            EditorApplication.update += UnityAiMainThread.Pump;

            if (config.autoStart)
                Start();
        }

        [MenuItem("Tools/Unity AI Connector/Start Local Server")]
        public static void StartFromMenu()
        {
            config = UnityAiConnectorConfig.LoadOrCreate();
            Start();
        }

        [MenuItem("Tools/Unity AI Connector/Stop Local Server")]
        public static void StopFromMenu()
        {
            Stop();
        }

        [MenuItem("Tools/Unity AI Connector/Print Token")]
        public static void PrintToken()
        {
            config = UnityAiConnectorConfig.LoadOrCreate();
            Debug.Log($"Unity AI Connector token: {config.token}");
            Debug.Log($"Unity AI Connector config: {UnityAiConnectorConfig.ConfigPath}");
        }

        [MenuItem("Tools/Unity AI Connector/Open Config")]
        public static void OpenConfig()
        {
            config = UnityAiConnectorConfig.LoadOrCreate();
            EditorUtility.RevealInFinder(UnityAiConnectorConfig.ConfigPath);
        }

        public static void Start()
        {
            config = UnityAiConnectorConfig.LoadOrCreate();
            if (isRunning)
            {
                Debug.Log("Unity AI Connector server is already running.");
                return;
            }

            if (config.bindHost != "127.0.0.1" && config.bindHost != "localhost")
            {
                Debug.LogError("Unity AI Connector refuses to bind to non-loopback host by default.");
                return;
            }

            listener = new HttpListener();
            listener.Prefixes.Add($"http://{config.bindHost}:{config.port}/");
            listener.Start();

            isRunning = true;
            serverThread = new Thread(ServerLoop)
            {
                IsBackground = true,
                Name = "unity-ai-connector"
            };
            serverThread.Start();

            Debug.Log($"Unity AI Connector listening on http://{config.bindHost}:{config.port}");
        }

        public static void Stop()
        {
            isRunning = false;

            try
            {
                listener?.Stop();
                listener?.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Unity AI Connector stop warning: {ex.Message}");
            }
            finally
            {
                listener = null;
            }
        }

        static void ServerLoop()
        {
            while (isRunning && listener != null)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => Handle(context));
                }
                catch
                {
                    if (isRunning)
                        Thread.Sleep(50);
                }
            }
        }

        static void Handle(HttpListenerContext context)
        {
            try
            {
                var path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? "";
                if (string.IsNullOrEmpty(path))
                    path = "/";

                if (context.Request.HttpMethod == "GET" && path == "/health")
                {
                    WriteJson(context, 200, new
                    {
                        ok = true,
                        name = "Unity AI Connector",
                        version = "0.2.0",
                        bindHost = config.bindHost,
                        port = config.port,
                        authRequired = config.authRequired
                    });
                    return;
                }

                if (!IsAuthorized(context))
                {
                    WriteJson(context, 401, new { ok = false, error = "Unauthorized" });
                    return;
                }

                if (context.Request.HttpMethod == "GET" && path == "/tools")
                {
                    WriteJson(context, 200, new
                    {
                        ok = true,
                        count = UnityAiTools.ToolNames.Length,
                        tools = UnityAiTools.ToolNames
                    });
                    return;
                }

                if (context.Request.HttpMethod == "POST" && path == "/rpc")
                {
                    var body = ReadBody(context.Request);
                    var request = JObject.Parse(body);
                    var tool = request.Value<string>("tool");
                    var args = request["args"] as JObject ?? new JObject();

                    var result = UnityAiMainThread.Run(() => UnityAiTools.Invoke(tool, args, config));
                    WriteJson(context, 200, new { ok = true, result });
                    return;
                }

                WriteJson(context, 404, new { ok = false, error = "Not found" });
            }
            catch (Exception ex)
            {
                WriteJson(context, 500, new { ok = false, error = ex.Message });
            }
        }

        static bool IsAuthorized(HttpListenerContext context)
        {
            if (!config.authRequired)
                return true;

            if (string.IsNullOrWhiteSpace(config.token))
                return false;

            var header = context.Request.Headers["Authorization"];
            return header == $"Bearer {config.token}";
        }

        static string ReadBody(HttpListenerRequest request)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
            return reader.ReadToEnd();
        }

        static void WriteJson(HttpListenerContext context, int statusCode, object payload)
        {
            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
            var bytes = Encoding.UTF8.GetBytes(json);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }
    }
}
#endif
