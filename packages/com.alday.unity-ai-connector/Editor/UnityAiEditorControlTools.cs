#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alday.UnityAiConnector.Editor
{
    public static class UnityAiEditorControlTools
    {
        public static object OpenScene(JObject args)
        {
            var path = RequiredString(args, "path");
            var modeValue = args.Value<string>("mode") ?? "Single";
            var mode = modeValue.Equals("Additive", StringComparison.OrdinalIgnoreCase)
                ? OpenSceneMode.Additive
                : OpenSceneMode.Single;

            var scene = EditorSceneManager.OpenScene(path, mode);
            if (!scene.IsValid())
                throw new InvalidOperationException("Scene could not be opened: " + path);

            return DescribeScene(scene);
        }

        public static object CreateScene(JObject args)
        {
            var setupValue = args.Value<string>("setup") ?? "DefaultGameObjects";
            var setup = setupValue.Equals("Empty", StringComparison.OrdinalIgnoreCase)
                ? NewSceneSetup.EmptyScene
                : NewSceneSetup.DefaultGameObjects;
            var modeValue = args.Value<string>("mode") ?? "Single";
            var mode = modeValue.Equals("Additive", StringComparison.OrdinalIgnoreCase)
                ? NewSceneMode.Additive
                : NewSceneMode.Single;

            var scene = EditorSceneManager.NewScene(setup, mode);
            var path = args.Value<string>("path");
            if (!string.IsNullOrWhiteSpace(path))
            {
                EnsureAssetParentFolder(path);
                EditorSceneManager.SaveScene(scene, path);
            }

            return DescribeScene(scene);
        }

        public static object SaveSceneAs(JObject args)
        {
            var path = RequiredString(args, "path");
            var sourcePath = args.Value<string>("sourcePath");
            var scene = string.IsNullOrWhiteSpace(sourcePath)
                ? SceneManager.GetActiveScene()
                : SceneManager.GetSceneByPath(sourcePath);

            if (!scene.IsValid())
                throw new InvalidOperationException("Scene not found.");

            EnsureAssetParentFolder(path);
            var saved = EditorSceneManager.SaveScene(scene, path);
            return new { saved, scene.name, path = scene.path };
        }

        public static object SceneHierarchy(JObject args)
        {
            var includeInactive = args.Value<bool?>("includeInactive") ?? true;
            var sceneName = args.Value<string>("scene");
            var maxDepth = args.Value<int?>("maxDepth") ?? 64;

            var roots = new List<object>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;
                if (!string.IsNullOrWhiteSpace(sceneName) && scene.name != sceneName)
                    continue;

                roots.Add(new
                {
                    scene.name,
                    scene.path,
                    roots = scene.GetRootGameObjects()
                        .Where(go => includeInactive || go.activeInHierarchy)
                        .Select(go => SerializeGameObject(go, includeInactive, 0, maxDepth))
                        .ToArray()
                });
            }

            return roots;
        }

        public static object DeleteGameObject(JObject args)
        {
            var target = UnityAiTools.ResolveTarget(args);
            var path = UnityAiTools.GetPath(target);
            Undo.DestroyObjectImmediate(target);
            MarkAllScenesDirty();
            return new { deleted = true, path };
        }

        public static object SetActive(JObject args)
        {
            var target = UnityAiTools.ResolveTarget(args);
            var active = args.Value<bool?>("active") ?? true;
            Undo.RecordObject(target, "Set Active via Unity AI Connector");
            target.SetActive(active);
            EditorSceneManager.MarkSceneDirty(target.scene);
            return DescribeGameObject(target);
        }

        public static object SetParent(JObject args)
        {
            var target = UnityAiTools.ResolveTarget(args);
            var parentPath = args.Value<string>("parentPath");
            var worldPositionStays = args.Value<bool?>("worldPositionStays") ?? false;
            var parent = string.IsNullOrWhiteSpace(parentPath) ? null : UnityAiTools.FindByPath(parentPath);

            if (!string.IsNullOrWhiteSpace(parentPath) && parent == null)
                throw new InvalidOperationException("Parent not found: " + parentPath);

            Undo.SetTransformParent(target.transform, parent == null ? null : parent.transform, "Set Parent via Unity AI Connector");
            target.transform.SetParent(parent == null ? null : parent.transform, worldPositionStays);
            EditorSceneManager.MarkSceneDirty(target.scene);
            return DescribeGameObject(target);
        }

        public static object AddComponent(JObject args)
        {
            var target = UnityAiTools.ResolveTarget(args);
            var typeName = RequiredString(args, "type");
            var type = FindType(typeName);

            if (!typeof(Component).IsAssignableFrom(type))
                throw new InvalidOperationException(typeName + " is not a Component type.");

            var existing = args.Value<bool?>("reuseExisting") == true ? target.GetComponent(type) : null;
            var component = existing ?? Undo.AddComponent(target, type);
            EditorSceneManager.MarkSceneDirty(target.scene);
            return DescribeComponent(component);
        }

        public static object SetComponentField(JObject args)
        {
            var target = UnityAiTools.ResolveTarget(args);
            var typeName = RequiredString(args, "type");
            var fieldName = RequiredString(args, "field");
            var component = target.GetComponent(FindType(typeName));
            if (component == null)
                throw new InvalidOperationException("Component not found: " + typeName);

            var field = component.GetType().GetField(fieldName);
            if (field == null)
                throw new InvalidOperationException("Public field not found: " + typeName + "." + fieldName);

            Undo.RecordObject(component, "Set Component Field via Unity AI Connector");
            field.SetValue(component, ConvertToken(args["value"], field.FieldType));
            EditorUtility.SetDirty(component);
            EditorSceneManager.MarkSceneDirty(target.scene);
            return DescribeComponent(component);
        }

        public static object CreateMaterial(JObject args)
        {
            var path = RequiredString(args, "path");
            EnsureAssetParentFolder(path);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(args.Value<string>("shader") ?? "Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            ApplyColor(args["color"], color => material.color = color);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return new { path, type = typeof(Material).FullName };
        }

        public static object CreateCamera(JObject args)
        {
            var name = args.Value<string>("name") ?? "Main Camera";
            var go = new GameObject(name, typeof(Camera));
            if (args.Value<bool?>("mainCamera") ?? true)
                go.tag = "MainCamera";

            ApplyTransform(go, args);
            ApplyCamera(go.GetComponent<Camera>(), args);
            Undo.RegisterCreatedObjectUndo(go, "Create Camera via Unity AI Connector");
            EditorSceneManager.MarkSceneDirty(go.scene);
            return DescribeGameObject(go);
        }

        public static object SetCamera(JObject args)
        {
            var target = UnityAiTools.ResolveTarget(args);
            var camera = target.GetComponent<Camera>();
            if (camera == null)
                throw new InvalidOperationException("Target does not have a Camera component.");

            Undo.RecordObject(camera, "Set Camera via Unity AI Connector");
            ApplyCamera(camera, args);
            EditorUtility.SetDirty(camera);
            EditorSceneManager.MarkSceneDirty(target.scene);
            return new
            {
                target.name,
                path = UnityAiTools.GetPath(target),
                camera.fieldOfView,
                camera.nearClipPlane,
                camera.farClipPlane,
                backgroundColor = ColorToArray(camera.backgroundColor)
            };
        }

        public static object InstantiatePrefab(JObject args)
        {
            var prefabPath = RequiredString(args, "prefabPath");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Prefab not found: " + prefabPath);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = args.Value<string>("name") ?? prefab.name;
            ApplyParent(instance, args);
            ApplyTransform(instance, args);
            Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab via Unity AI Connector");
            EditorSceneManager.MarkSceneDirty(instance.scene);
            return DescribeGameObject(instance);
        }

        public static object CreatePrefabFromGameObject(JObject args)
        {
            var target = UnityAiTools.ResolveTarget(args);
            var prefabPath = RequiredString(args, "prefabPath");
            EnsureAssetParentFolder(prefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(target, prefabPath);
            AssetDatabase.SaveAssets();
            return new { prefabPath, sourcePath = UnityAiTools.GetPath(target), prefabName = prefab.name };
        }

        public static object CreateCanvas(JObject args)
        {
            var name = args.Value<string>("name") ?? "Canvas";
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ApplyParent(go, args);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReadVector2(args["referenceResolution"], new Vector2(800, 450));
            EnsureEventSystem();
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas via Unity AI Connector");
            EditorSceneManager.MarkSceneDirty(go.scene);
            return DescribeGameObject(go);
        }

        public static object CreateText(JObject args)
        {
            var parentPath = RequiredString(args, "parentPath");
            var parent = UnityAiTools.FindByPath(parentPath);
            if (parent == null)
                throw new InvalidOperationException("Parent not found: " + parentPath);

            var go = UnityAiGameStyle.CreateStyledText(
                parent.transform,
                args.Value<string>("name") ?? "Text",
                args.Value<string>("text") ?? "Text",
                args,
                args.Value<string>("role") ?? "body");
            Undo.RegisterCreatedObjectUndo(go, "Create Text via Unity AI Connector");
            EditorSceneManager.MarkSceneDirty(go.scene);
            return DescribeGameObject(go);
        }

        public static object CreateButton(JObject args)
        {
            var parentPath = RequiredString(args, "parentPath");
            var parent = UnityAiTools.FindByPath(parentPath);
            if (parent == null)
                throw new InvalidOperationException("Parent not found: " + parentPath);

            var go = UnityAiGameStyle.CreateStyledButton(
                parent.transform,
                args.Value<string>("name") ?? "Button",
                args.Value<string>("text") ?? "Button",
                args);
            Undo.RegisterCreatedObjectUndo(go, "Create Button via Unity AI Connector");
            EditorSceneManager.MarkSceneDirty(go.scene);
            return DescribeGameObject(go);
        }

        public static object SetRectTransform(JObject args)
        {
            var target = UnityAiTools.ResolveTarget(args);
            var rect = target.GetComponent<RectTransform>();
            if (rect == null)
                throw new InvalidOperationException("Target does not have RectTransform.");

            Undo.RecordObject(rect, "Set RectTransform via Unity AI Connector");
            ApplyRectTransform(rect, args);
            EditorUtility.SetDirty(rect);
            EditorSceneManager.MarkSceneDirty(target.scene);
            return DescribeRectTransform(target, rect);
        }

        static object SerializeGameObject(GameObject go, bool includeInactive, int depth, int maxDepth)
        {
            var children = new List<object>();
            if (depth < maxDepth)
            {
                foreach (Transform child in go.transform)
                {
                    if (includeInactive || child.gameObject.activeInHierarchy)
                        children.Add(SerializeGameObject(child.gameObject, includeInactive, depth + 1, maxDepth));
                }
            }

            return new
            {
                go.name,
                path = UnityAiTools.GetPath(go),
                id = UnityAiTools.GetObjectId(go),
                activeSelf = go.activeSelf,
                components = go.GetComponents<Component>().Where(component => component != null).Select(component => component.GetType().FullName).ToArray(),
                children
            };
        }

        static object DescribeGameObject(GameObject go)
        {
            return new
            {
                go.name,
                path = UnityAiTools.GetPath(go),
                id = UnityAiTools.GetObjectId(go),
                activeSelf = go.activeSelf,
                scene = go.scene.name,
                position = UnityAiTools.ToArray(go.transform.localPosition),
                rotation = UnityAiTools.ToArray(go.transform.localEulerAngles),
                scale = UnityAiTools.ToArray(go.transform.localScale)
            };
        }

        static object DescribeScene(Scene scene)
        {
            return new
            {
                scene.name,
                scene.path,
                scene.isLoaded,
                scene.isDirty,
                rootCount = scene.rootCount
            };
        }

        static object DescribeComponent(Component component)
        {
            return new
            {
                type = component.GetType().FullName,
                component.name,
                id = UnityAiTools.GetObjectId(component),
                gameObjectPath = UnityAiTools.GetPath(component.gameObject)
            };
        }

        static object DescribeRectTransform(GameObject go, RectTransform rect)
        {
            return new
            {
                go.name,
                path = UnityAiTools.GetPath(go),
                anchoredPosition = Vector2ToArray(rect.anchoredPosition),
                sizeDelta = Vector2ToArray(rect.sizeDelta),
                anchorMin = Vector2ToArray(rect.anchorMin),
                anchorMax = Vector2ToArray(rect.anchorMax),
                pivot = Vector2ToArray(rect.pivot)
            };
        }

        internal static void ApplyTransform(GameObject go, JObject args)
        {
            UnityAiTools.ApplyVector(args["position"], value => go.transform.localPosition = value);
            UnityAiTools.ApplyVector(args["rotation"], value => go.transform.localEulerAngles = value);
            UnityAiTools.ApplyVector(args["scale"], value => go.transform.localScale = value);
        }

        internal static void ApplyParent(GameObject go, JObject args)
        {
            var parentPath = args.Value<string>("parentPath");
            if (string.IsNullOrWhiteSpace(parentPath))
                return;

            var parent = UnityAiTools.FindByPath(parentPath);
            if (parent == null)
                throw new InvalidOperationException("Parent not found: " + parentPath);
            go.transform.SetParent(parent.transform, false);
        }

        internal static void ApplyCamera(Camera camera, JObject args)
        {
            if (args["fieldOfView"] != null)
                camera.fieldOfView = args.Value<float>("fieldOfView");
            if (args["nearClipPlane"] != null)
                camera.nearClipPlane = args.Value<float>("nearClipPlane");
            if (args["farClipPlane"] != null)
                camera.farClipPlane = args.Value<float>("farClipPlane");
            ApplyColor(args["backgroundColor"], color => camera.backgroundColor = color);
        }

        internal static void ApplyRectTransform(RectTransform rect, JObject args)
        {
            if (args["anchoredPosition"] != null)
                rect.anchoredPosition = ReadVector2(args["anchoredPosition"], rect.anchoredPosition);
            if (args["sizeDelta"] != null)
                rect.sizeDelta = ReadVector2(args["sizeDelta"], rect.sizeDelta);
            if (args["anchorMin"] != null)
                rect.anchorMin = ReadVector2(args["anchorMin"], rect.anchorMin);
            if (args["anchorMax"] != null)
                rect.anchorMax = ReadVector2(args["anchorMax"], rect.anchorMax);
            if (args["pivot"] != null)
                rect.pivot = ReadVector2(args["pivot"], rect.pivot);
        }

        internal static void ApplyColor(JToken token, Action<Color> apply)
        {
            if (token == null)
                return;

            var values = token.ToObject<float[]>();
            if (values == null || (values.Length != 3 && values.Length != 4))
                throw new ArgumentException("Color values must be [r, g, b] or [r, g, b, a].");
            apply(new Color(values[0], values[1], values[2], values.Length == 4 ? values[3] : 1f));
        }

        internal static object ConvertToken(JToken token, Type type)
        {
            if (token == null || token.Type == JTokenType.Null)
                return type.IsValueType ? Activator.CreateInstance(type) : null;

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return ResolveUnityObject(token, type);

            if (type == typeof(Vector3))
            {
                var values = token.ToObject<float[]>();
                if (values == null || values.Length != 3)
                    throw new ArgumentException("Vector3 field requires [x, y, z].");
                return new Vector3(values[0], values[1], values[2]);
            }
            if (type == typeof(Vector2))
                return ReadVector2(token, Vector2.zero);
            if (type == typeof(Color))
            {
                var values = token.ToObject<float[]>();
                if (values == null || (values.Length != 3 && values.Length != 4))
                    throw new ArgumentException("Color field requires [r, g, b] or [r, g, b, a].");
                return new Color(values[0], values[1], values[2], values.Length == 4 ? values[3] : 1f);
            }
            if (type.IsEnum)
                return Enum.Parse(type, token.ToString(), ignoreCase: true);
            return token.ToObject(type);
        }

        internal static Type FindType(string typeName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(found => found != null);

            if (type != null)
                return type;

            type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .FirstOrDefault(candidate => candidate.Name == typeName);

            if (type == null)
                throw new InvalidOperationException("Type not found: " + typeName);
            return type;
        }

        internal static string RequiredString(JObject args, string name)
        {
            var value = args.Value<string>(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(name + " is required.");
            return value;
        }

        internal static Vector2 ReadVector2(JToken token, Vector2 fallback)
        {
            if (token == null)
                return fallback;

            var values = token.ToObject<float[]>();
            if (values == null || values.Length != 2)
                throw new ArgumentException("Vector2 values must be [x, y].");
            return new Vector2(values[0], values[1]);
        }

        internal static float[] Vector2ToArray(Vector2 value)
        {
            return new[] { value.x, value.y };
        }

        internal static float[] ColorToArray(Color value)
        {
            return new[] { value.r, value.g, value.b, value.a };
        }

        static TextAnchor ParseTextAnchor(string value, TextAnchor fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;
            return (TextAnchor)Enum.Parse(typeof(TextAnchor), value, ignoreCase: true);
        }

        internal static void EnsureAssetParentFolder(string assetPath)
        {
            var folder = System.IO.Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        internal static void EnsureEventSystem()
        {
#if UNITY_6000_0_OR_NEWER
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
                return;
#else
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
                return;
#endif
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        internal static void MarkAllScenesDirty()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        static object ResolveUnityObject(JToken token, Type type)
        {
            string assetPath = null;
            string objectPath = null;
            string objectName = null;
            string componentType = null;

            if (token.Type == JTokenType.String)
            {
                assetPath = token.Value<string>();
                objectPath = assetPath;
            }
            else if (token is JObject obj)
            {
                assetPath = obj.Value<string>("assetPath");
                objectPath = obj.Value<string>("path");
                objectName = obj.Value<string>("name");
                componentType = obj.Value<string>("componentType");
            }

            if (!string.IsNullOrWhiteSpace(assetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);
                if (asset != null)
                    return asset;
            }

            GameObject go = null;
            if (!string.IsNullOrWhiteSpace(objectPath))
                go = UnityAiTools.FindByPath(objectPath);
            if (go == null && !string.IsNullOrWhiteSpace(objectName))
                go = UnityAiTools.AllSceneObjects().FirstOrDefault(candidate => candidate.name == objectName);

            if (go != null)
            {
                if (type == typeof(GameObject))
                    return go;
                if (type == typeof(Transform))
                    return go.transform;
                if (typeof(Component).IsAssignableFrom(type))
                    return go.GetComponent(type);
                if (!string.IsNullOrWhiteSpace(componentType))
                    return go.GetComponent(FindType(componentType));
            }

            return null;
        }
    }
}
#endif
