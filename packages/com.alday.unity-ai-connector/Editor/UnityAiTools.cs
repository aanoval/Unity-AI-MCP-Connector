#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alday.UnityAiConnector.Editor
{
    public static class UnityAiTools
    {
        public static readonly string[] ToolNames =
        {
            "scene.listOpen",
            "scene.save",
            "gameObject.find",
            "gameObject.create",
            "gameObject.setTransform",
            "component.list",
            "asset.find"
        };

        public static object Invoke(string tool, JObject args, UnityAiConnectorConfig config)
        {
            if (string.IsNullOrWhiteSpace(tool))
                throw new ArgumentException("Tool is required.");

            return tool switch
            {
                "scene.listOpen" => ListOpenScenes(),
                "scene.save" => SaveScene(args),
                "gameObject.find" => FindGameObjects(args),
                "gameObject.create" => CreateGameObject(args),
                "gameObject.setTransform" => SetTransform(args),
                "component.list" => ListComponents(args),
                "asset.find" => FindAssets(args),
                _ => throw new InvalidOperationException($"Unknown tool: {tool}")
            };
        }

        static object ListOpenScenes()
        {
            var scenes = new List<object>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                scenes.Add(new
                {
                    scene.name,
                    scene.path,
                    scene.isLoaded,
                    scene.isDirty,
                    rootCount = scene.rootCount
                });
            }

            return scenes;
        }

        static object SaveScene(JObject args)
        {
            var path = args.Value<string>("path");
            var scene = string.IsNullOrWhiteSpace(path)
                ? SceneManager.GetActiveScene()
                : SceneManager.GetSceneByPath(path);

            if (!scene.IsValid())
                throw new InvalidOperationException("Scene not found.");

            var saved = EditorSceneManager.SaveScene(scene);
            return new { saved, scene.path };
        }

        static object FindGameObjects(JObject args)
        {
            var name = args.Value<string>("name");
            var path = args.Value<string>("path");
            var includeInactive = args.Value<bool?>("includeInactive") ?? true;

            return AllSceneObjects()
                .Where(go => includeInactive || go.activeInHierarchy)
                .Where(go => string.IsNullOrWhiteSpace(name) || go.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(go => string.IsNullOrWhiteSpace(path) || GetPath(go).IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(go => new
                {
                    go.name,
                    path = GetPath(go),
                    id = GetObjectId(go),
                    activeSelf = go.activeSelf,
                    scene = go.scene.name
                })
                .Take(args.Value<int?>("limit") ?? 100)
                .ToArray();
        }

        static object CreateGameObject(JObject args)
        {
            var name = args.Value<string>("name") ?? "New GameObject";
            var parentPath = args.Value<string>("parentPath");
            var go = new GameObject(name);

            if (!string.IsNullOrWhiteSpace(parentPath))
            {
                var parent = FindByPath(parentPath);
                if (parent == null)
                    throw new InvalidOperationException($"Parent not found: {parentPath}");
                go.transform.SetParent(parent.transform, false);
            }

            Undo.RegisterCreatedObjectUndo(go, "Create GameObject via Unity AI Connector");
            EditorSceneManager.MarkSceneDirty(go.scene);
            return new { go.name, path = GetPath(go), id = GetObjectId(go) };
        }

        static object SetTransform(JObject args)
        {
            var target = ResolveTarget(args);
            var transform = target.transform;
            Undo.RecordObject(transform, "Set Transform via Unity AI Connector");

            ApplyVector(args["position"], value => transform.localPosition = value);
            ApplyVector(args["rotation"], value => transform.localEulerAngles = value);
            ApplyVector(args["scale"], value => transform.localScale = value);

            EditorSceneManager.MarkSceneDirty(target.scene);
            return new
            {
                target.name,
                path = GetPath(target),
                position = ToArray(transform.localPosition),
                rotation = ToArray(transform.localEulerAngles),
                scale = ToArray(transform.localScale)
            };
        }

        static object ListComponents(JObject args)
        {
            var target = ResolveTarget(args);
            return target.GetComponents<Component>()
                .Where(component => component != null)
                .Select(component => new
                {
                    type = component.GetType().FullName,
                    component.name,
                    id = GetObjectId(component)
                })
                .ToArray();
        }

        static object FindAssets(JObject args)
        {
            var filter = args.Value<string>("filter") ?? "";
            var folders = args["folders"]?.ToObject<string[]>();
            var guids = folders == null || folders.Length == 0
                ? AssetDatabase.FindAssets(filter)
                : AssetDatabase.FindAssets(filter, folders);

            return guids
                .Take(args.Value<int?>("limit") ?? 100)
                .Select(guid =>
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    return new
                    {
                        guid,
                        path = assetPath,
                        type = AssetDatabase.GetMainAssetTypeAtPath(assetPath)?.FullName
                    };
                })
                .ToArray();
        }

        static GameObject ResolveTarget(JObject args)
        {
            var path = args.Value<string>("path");
            if (!string.IsNullOrWhiteSpace(path))
            {
                var byPath = FindByPath(path);
                if (byPath != null)
                    return byPath;
            }

            var name = args.Value<string>("name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                var byName = AllSceneObjects().FirstOrDefault(go => go.name == name);
                if (byName != null)
                    return byName;
            }

            throw new InvalidOperationException("Target GameObject not found. Provide path or name.");
        }

        static void ApplyVector(JToken token, Action<Vector3> apply)
        {
            if (token == null)
                return;

            var values = token.ToObject<float[]>();
            if (values == null || values.Length != 3)
                throw new ArgumentException("Vector values must be [x, y, z].");

            apply(new Vector3(values[0], values[1], values[2]));
        }

        static float[] ToArray(Vector3 value)
        {
            return new[] { value.x, value.y, value.z };
        }

        static IEnumerable<GameObject> AllSceneObjects()
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.scene.IsValid())
                .Where(go => !EditorUtility.IsPersistent(go));
        }

        static GameObject FindByPath(string path)
        {
            return AllSceneObjects().FirstOrDefault(go => GetPath(go) == path);
        }

        static string GetPath(GameObject go)
        {
            var names = new Stack<string>();
            var current = go.transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        static string GetObjectId(UnityEngine.Object target)
        {
#if UNITY_6000_0_OR_NEWER
            return target.GetEntityId().ToString();
#else
            return target.GetInstanceID().ToString();
#endif
        }
    }
}
#endif
