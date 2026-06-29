#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Alday.UnityAiGameMaker.Editor
{
    public static class UnityAiScreenshotTools
    {
        const int MaxScreenshotDimension = 3840;

        public static object Capture(JObject args)
        {
            EnsureGraphicsAvailable();

            var outputPath = ResolveOutputPath(args);
            var source = (args.Value<string>("source") ?? "camera").Trim();
            var width = args.Value<int?>("width");
            var height = args.Value<int?>("height");

            if (source.Equals("gameView", StringComparison.OrdinalIgnoreCase))
                return CaptureFromGameView(args, outputPath, width, height);

            return CaptureFromCamera(args, outputPath, width, height);
        }

        public static object CaptureScenes(JObject args)
        {
            EnsureGraphicsAvailable();

            var scenePaths = ResolveScenePaths(args);
            if (scenePaths.Length == 0)
                throw new InvalidOperationException("No scenes matched the requested filter.");

            var outputDir = ResolveOutputDirectory(args);
            Directory.CreateDirectory(outputDir);

            var width = args.Value<int?>("width") ?? 1080;
            var height = args.Value<int?>("height") ?? 1920;
            var source = args.Value<string>("source") ?? "camera";
            var namePattern = args.Value<string>("namePattern") ?? "{sceneName}";
            var captures = new List<object>();

            foreach (var scenePath in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid())
                    throw new InvalidOperationException("Scene could not be opened: " + scenePath);

                var sceneName = Path.GetFileNameWithoutExtension(scenePath);
                var fileName = namePattern.Replace("{sceneName}", sceneName) + ".png";
                var outputPath = Path.Combine(outputDir, fileName);

                var captureArgs = new JObject
                {
                    ["outputPath"] = outputPath,
                    ["width"] = width,
                    ["height"] = height,
                    ["source"] = source
                };

                var cameraPath = args.Value<string>("cameraPath");
                if (!string.IsNullOrWhiteSpace(cameraPath))
                    captureArgs["cameraPath"] = cameraPath;

                var capture = Capture(captureArgs);
                captures.Add(new
                {
                    scenePath,
                    sceneName,
                    outputPath,
                    capture
                });
            }

            return new
            {
                outputDir,
                count = captures.Count,
                width,
                height,
                source,
                filter = args.Value<string>("filter") ?? "buildSettings",
                captures
            };
        }

        static object CaptureFromCamera(JObject args, string outputPath, int? width, int? height)
        {
            var resolvedWidth = width ?? 1280;
            var resolvedHeight = height ?? 720;
            var camera = ResolveScreenshotCamera(args);
            var absolutePath = ToAbsolutePath(outputPath);

            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? ".");

            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var overlayCanvases = UnityAiTools.AllSceneObjects()
                .Select(go => go.GetComponent<Canvas>())
                .Where(canvas => canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                .ToArray();
            var previousCanvasCameras = overlayCanvases.Select(canvas => canvas.worldCamera).ToArray();
            var previousCanvasDistances = overlayCanvases.Select(canvas => canvas.planeDistance).ToArray();
            var renderTexture = new RenderTexture(resolvedWidth, resolvedHeight, 24);
            var texture = new Texture2D(resolvedWidth, resolvedHeight, TextureFormat.RGB24, false);

            try
            {
                for (var i = 0; i < overlayCanvases.Length; i++)
                {
                    overlayCanvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    overlayCanvases[i].worldCamera = camera;
                    overlayCanvases[i].planeDistance = 1f + i * 0.02f;
                }

                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                texture.ReadPixels(new Rect(0, 0, resolvedWidth, resolvedHeight), 0, 0);
                texture.Apply();
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
            finally
            {
                for (var i = 0; i < overlayCanvases.Length; i++)
                {
                    if (overlayCanvases[i] == null)
                        continue;

                    overlayCanvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                    overlayCanvases[i].worldCamera = previousCanvasCameras[i];
                    overlayCanvases[i].planeDistance = previousCanvasDistances[i];
                }

                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }

            RefreshAssetIfNeeded(outputPath);
            return BuildCaptureResult(outputPath, absolutePath, resolvedWidth, resolvedHeight, "camera", camera);
        }

        static object CaptureFromGameView(JObject args, string outputPath, int? width, int? height)
        {
            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
                throw new InvalidOperationException("GameView type not found. Use source \"camera\" instead.");

            var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
            if (gameView == null)
                throw new InvalidOperationException("No Game View window is available. Open the Game View tab in Unity or use source \"camera\".");

            gameView.Repaint();

            var rtField = gameViewType.GetField("m_RenderTexture", BindingFlags.NonPublic | BindingFlags.Instance);
            var sourceRt = rtField?.GetValue(gameView) as RenderTexture;
            if (sourceRt == null || !sourceRt.IsCreated())
            {
                throw new InvalidOperationException(
                    "Game View render texture is not available. Ensure the Game View is open and visible, " +
                    "or use source \"camera\" for batch captures.");
            }

            var gameViewSize = Handles.GetMainGameViewSize();
            var targetWidth = width ?? Mathf.RoundToInt(gameViewSize.x);
            var targetHeight = height ?? Mathf.RoundToInt(gameViewSize.y);
            (targetWidth, targetHeight) = ClampToTransportLimit(targetWidth, targetHeight);

            var absolutePath = ToAbsolutePath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? ".");

            var prevActive = RenderTexture.active;
            RenderTexture scaledRt = null;
            Texture2D texture = null;

            try
            {
                var readSource = sourceRt;
                if (targetWidth != sourceRt.width || targetHeight != sourceRt.height)
                {
                    scaledRt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, sourceRt.format);
                    Graphics.Blit(sourceRt, scaledRt);
                    readSource = scaledRt;
                }

                RenderTexture.active = readSource;
                texture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);

                if (SystemInfo.graphicsUVStartsAtTop)
                {
                    var pixels = texture.GetPixels32();
                    var flipped = new Color32[pixels.Length];
                    for (var y = 0; y < targetHeight; y++)
                    {
                        var srcRow = y * targetWidth;
                        var dstRow = (targetHeight - 1 - y) * targetWidth;
                        Array.Copy(pixels, srcRow, flipped, dstRow, targetWidth);
                    }

                    texture.SetPixels32(flipped);
                }

                texture.Apply();
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = prevActive;
                if (scaledRt != null)
                    RenderTexture.ReleaseTemporary(scaledRt);
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
            }

            RefreshAssetIfNeeded(outputPath);
            return BuildCaptureResult(outputPath, absolutePath, targetWidth, targetHeight, "gameView", null);
        }

        static void EnsureGraphicsAvailable()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                throw new InvalidOperationException(
                    "Screenshot capture requires a graphics device. " +
                    "Do not run Unity batch mode with -nographics. " +
                    "Use: Unity -batchmode -quit -executeMethod Alday.UnityAiGameMaker.Editor.UnityAiGameMakerBatch.RunFromEnvironment");
            }
        }

        static string ResolveOutputPath(JObject args)
        {
            var outputPath = args.Value<string>("outputPath")
                ?? args.Value<string>("path")
                ?? "Temp/UnityAiGameMaker/screenshot.png";
            return outputPath;
        }

        static string ResolveOutputDirectory(JObject args)
        {
            var outputDir = args.Value<string>("outputDir") ?? args.Value<string>("output");
            if (!string.IsNullOrWhiteSpace(outputDir))
                return Path.GetFullPath(outputDir);

            return Path.GetFullPath(Path.Combine(UnityAiGameMakerConfig.ProjectRoot, "..", "screenshots"));
        }

        static string[] ResolveScenePaths(JObject args)
        {
            var explicitScenes = args["scenes"]?.ToObject<string[]>();
            if (explicitScenes != null && explicitScenes.Length > 0)
                return explicitScenes;

            var filter = (args.Value<string>("filter") ?? "buildSettings").Trim();
            var buildScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            return filter.ToLowerInvariant() switch
            {
                "menu" => buildScenes.Where(IsMenuScene).ToArray(),
                "gameplay" => buildScenes.Where(path =>
                    path.IndexOf("Gameplay", StringComparison.OrdinalIgnoreCase) >= 0).ToArray(),
                "buildsettings" => buildScenes,
                "all" => buildScenes,
                _ => buildScenes
            };
        }

        static bool IsMenuScene(string path)
        {
            var sceneName = Path.GetFileNameWithoutExtension(path);
            return sceneName.StartsWith("Menu_", StringComparison.OrdinalIgnoreCase)
                || sceneName.Contains("Main_Menu", StringComparison.OrdinalIgnoreCase)
                || sceneName.Contains("MainMenu", StringComparison.OrdinalIgnoreCase);
        }

        static Camera ResolveScreenshotCamera(JObject args)
        {
            var cameraPath = args.Value<string>("cameraPath");
            if (!string.IsNullOrWhiteSpace(cameraPath))
            {
                var target = UnityAiTools.FindByPath(cameraPath);
                var camera = target == null ? null : target.GetComponent<Camera>();
                if (camera != null)
                    return camera;
            }

            if (Camera.main != null)
                return Camera.main;

            var found = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude).FirstOrDefault();
            if (found == null)
                throw new InvalidOperationException("No Camera found. Provide cameraPath or add a Camera to the scene.");

            return found;
        }

        static string ToAbsolutePath(string path)
        {
            return Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
        }

        static void RefreshAssetIfNeeded(string path)
        {
            if (path.StartsWith("Assets/", StringComparison.Ordinal))
                AssetDatabase.Refresh();
        }

        static object BuildCaptureResult(
            string outputPath,
            string absolutePath,
            int width,
            int height,
            string source,
            Camera camera)
        {
            return new
            {
                outputPath,
                path = outputPath,
                absolutePath,
                width,
                height,
                source,
                camera = camera == null ? null : UnityAiTools.GetPath(camera.gameObject)
            };
        }

        static (int width, int height) ClampToTransportLimit(int width, int height)
        {
            var longest = Mathf.Max(width, height);
            if (longest <= MaxScreenshotDimension)
                return (width, height);

            var scale = (float)MaxScreenshotDimension / longest;
            return (
                Mathf.Max(1, Mathf.RoundToInt(width * scale)),
                Mathf.Max(1, Mathf.RoundToInt(height * scale))
            );
        }
    }
}
#endif
