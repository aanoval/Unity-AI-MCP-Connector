#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Alday.UnityAiGameMaker.Editor
{
    public static class UnityAiTools
    {
        public static readonly string[] ToolNames =
        {
            "scene.listOpen",
            "scene.open",
            "scene.create",
            "scene.save",
            "scene.saveAs",
            "scene.hierarchy",
            "gameObject.find",
            "gameObject.create",
            "gameObject.delete",
            "gameObject.setTransform",
            "gameObject.setActive",
            "gameObject.setParent",
            "component.add",
            "component.list",
            "component.setField",
            "component.setProperty",
            "asset.find",
            "asset.material.create",
            "script.create",
            "physics.rigidbody.add",
            "physics.rigidbody.set",
            "physics.collider.add",
            "physics.collider.set",
            "scene.buildSettings.set",
            "playmode.start",
            "playmode.stop",
            "console.clear",
            "console.read",
            "screenshot.capture",
            "screenshots.captureScenes",
            "light.create",
            "light.set",
            "audio.source.add",
            "audio.source.set",
            "camera.create",
            "camera.set",
            "prefab.instantiate",
            "prefab.createFromGameObject",
            "prefab.child.create",
            "prefab.child.delete",
            "prefab.component.add",
            "prefab.component.setProperty",
            "ui.canvas.create",
            "ui.text.create",
            "ui.button.create",
            "ui.menu.create",
            "ui.hud.create",
            "ui.validate",
            "ui.virtualButton.create",
            "ui.joystick.create",
            "ui.mobileControls.create",
            "ui.rectTransform.set",
            "sample.runner3D.createScripts",
            "sample.runner3D.createContent"
        };

        public static object Invoke(string tool, JObject args, UnityAiGameMakerConfig config)
        {
            if (string.IsNullOrWhiteSpace(tool))
                throw new ArgumentException("Tool is required.");

            return tool switch
            {
                "scene.listOpen" => ListOpenScenes(),
                "scene.open" => UnityAiEditorControlTools.OpenScene(args),
                "scene.create" => UnityAiEditorControlTools.CreateScene(args),
                "scene.save" => SaveScene(args),
                "scene.saveAs" => UnityAiEditorControlTools.SaveSceneAs(args),
                "scene.hierarchy" => UnityAiEditorControlTools.SceneHierarchy(args),
                "gameObject.find" => FindGameObjects(args),
                "gameObject.create" => CreateGameObject(args),
                "gameObject.delete" => UnityAiEditorControlTools.DeleteGameObject(args),
                "gameObject.setTransform" => SetTransform(args),
                "gameObject.setActive" => UnityAiEditorControlTools.SetActive(args),
                "gameObject.setParent" => UnityAiEditorControlTools.SetParent(args),
                "component.add" => UnityAiEditorControlTools.AddComponent(args),
                "component.list" => ListComponents(args),
                "component.setField" => UnityAiEditorControlTools.SetComponentField(args),
                "component.setProperty" => UnityAiGameCreationTools.SetComponentProperty(args),
                "asset.find" => FindAssets(args),
                "asset.material.create" => UnityAiEditorControlTools.CreateMaterial(args),
                "script.create" => UnityAiGameCreationTools.CreateScript(args),
                "physics.rigidbody.add" => UnityAiGameCreationTools.AddRigidbody(args),
                "physics.rigidbody.set" => UnityAiGameCreationTools.SetRigidbody(args),
                "physics.collider.add" => UnityAiGameCreationTools.AddCollider(args),
                "physics.collider.set" => UnityAiGameCreationTools.SetCollider(args),
                "scene.buildSettings.set" => UnityAiGameCreationTools.SetBuildSettings(args),
                "playmode.start" => UnityAiGameCreationTools.StartPlaymode(),
                "playmode.stop" => UnityAiGameCreationTools.StopPlaymode(),
                "console.clear" => UnityAiGameCreationTools.ClearConsole(),
                "console.read" => UnityAiGameCreationTools.ReadConsole(args),
                "screenshot.capture" => UnityAiScreenshotTools.Capture(args),
                "screenshots.captureScenes" => UnityAiScreenshotTools.CaptureScenes(args),
                "light.create" => UnityAiGameCreationTools.CreateLight(args),
                "light.set" => UnityAiGameCreationTools.SetLight(args),
                "audio.source.add" => UnityAiGameCreationTools.AddAudioSource(args),
                "audio.source.set" => UnityAiGameCreationTools.SetAudioSource(args),
                "camera.create" => UnityAiEditorControlTools.CreateCamera(args),
                "camera.set" => UnityAiEditorControlTools.SetCamera(args),
                "prefab.instantiate" => UnityAiEditorControlTools.InstantiatePrefab(args),
                "prefab.createFromGameObject" => UnityAiEditorControlTools.CreatePrefabFromGameObject(args),
                "prefab.child.create" => UnityAiGameCreationTools.CreatePrefabChild(args),
                "prefab.child.delete" => UnityAiGameCreationTools.DeletePrefabChild(args),
                "prefab.component.add" => UnityAiGameCreationTools.AddPrefabComponent(args),
                "prefab.component.setProperty" => UnityAiGameCreationTools.SetPrefabComponentProperty(args),
                "ui.canvas.create" => UnityAiEditorControlTools.CreateCanvas(args),
                "ui.text.create" => UnityAiEditorControlTools.CreateText(args),
                "ui.button.create" => UnityAiEditorControlTools.CreateButton(args),
                "ui.menu.create" => UnityAiGameCreationTools.CreateMenu(args),
                "ui.hud.create" => UnityAiGameCreationTools.CreateHud(args),
                "ui.validate" => UnityAiGameCreationTools.ValidateUi(args),
                "ui.virtualButton.create" => UnityAiGameCreationTools.CreateVirtualButton(args),
                "ui.joystick.create" => UnityAiGameCreationTools.CreateJoystick(args),
                "ui.mobileControls.create" => UnityAiGameCreationTools.CreateMobileControls(args),
                "ui.rectTransform.set" => UnityAiEditorControlTools.SetRectTransform(args),
                "sample.runner3D.createScripts" => UnityAiRunner3DSampleBuilder.CreateScripts(),
                "sample.runner3D.createContent" => UnityAiRunner3DSampleBuilder.CreateContent(),
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

            Undo.RegisterCreatedObjectUndo(go, "Create GameObject via Unity AI Game Maker");
            EditorSceneManager.MarkSceneDirty(go.scene);
            return new { go.name, path = GetPath(go), id = GetObjectId(go) };
        }

        static object SetTransform(JObject args)
        {
            var target = ResolveTarget(args);
            var transform = target.transform;
            Undo.RecordObject(transform, "Set Transform via Unity AI Game Maker");

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

        internal static GameObject ResolveTarget(JObject args)
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

        internal static void ApplyVector(JToken token, Action<Vector3> apply)
        {
            if (token == null)
                return;

            var values = token.ToObject<float[]>();
            if (values == null || values.Length != 3)
                throw new ArgumentException("Vector values must be [x, y, z].");

            apply(new Vector3(values[0], values[1], values[2]));
        }

        internal static float[] ToArray(Vector3 value)
        {
            return new[] { value.x, value.y, value.z };
        }

        internal static IEnumerable<GameObject> AllSceneObjects()
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(go => go.scene.IsValid())
                .Where(go => !EditorUtility.IsPersistent(go));
        }

        internal static GameObject FindByPath(string path)
        {
            return AllSceneObjects().FirstOrDefault(go => GetPath(go) == path);
        }

        internal static string GetPath(GameObject go)
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

        internal static string GetObjectId(UnityEngine.Object target)
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
