#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Alday.UnityAiConnector.Editor
{
    public sealed class UnityAiGameStylePreset
    {
        public string name;
        public Color primary;
        public Color secondary;
        public Color accent;
        public Color danger;
        public Color text;
        public Color mutedText;
        public Color panel;
        public Color shadow;
        public int titleSize;
        public int bodySize;
        public int buttonSize;
        public Vector2 buttonSizeDelta;
        public Vector2 smallButtonSizeDelta;
    }

    public static class UnityAiGameStyle
    {
        const string GeneratedFolder = "Assets/UnityAiConnectorGenerated/UI";
        const string RoundedSpritePath = GeneratedFolder + "/rounded_rect_32.png";

        public static UnityAiGameStylePreset FromArgs(JObject args)
        {
            return Get(args.Value<string>("style") ?? args.Value<string>("preset") ?? "arcade");
        }

        public static UnityAiGameStylePreset Get(string name)
        {
            switch ((name ?? "arcade").Trim().ToLowerInvariant())
            {
                case "soccer":
                case "soccer_mobile":
                    return new UnityAiGameStylePreset
                    {
                        name = "soccer_mobile",
                        primary = new Color(0.95f, 0.73f, 0.18f, 1f),
                        secondary = new Color(0.05f, 0.18f, 0.14f, 0.94f),
                        accent = new Color(0.18f, 0.76f, 0.35f, 1f),
                        danger = new Color(0.92f, 0.18f, 0.22f, 1f),
                        text = Color.white,
                        mutedText = new Color(0.80f, 0.92f, 0.84f, 1f),
                        panel = new Color(0.03f, 0.12f, 0.09f, 0.86f),
                        shadow = new Color(0f, 0f, 0f, 0.46f),
                        titleSize = 46,
                        bodySize = 20,
                        buttonSize = 25,
                        buttonSizeDelta = new Vector2(244, 68),
                        smallButtonSizeDelta = new Vector2(150, 56)
                    };
                case "premium":
                    return new UnityAiGameStylePreset
                    {
                        name = "premium",
                        primary = new Color(0.92f, 0.78f, 0.42f, 1f),
                        secondary = new Color(0.09f, 0.10f, 0.12f, 0.94f),
                        accent = new Color(0.46f, 0.76f, 1f, 1f),
                        danger = new Color(0.86f, 0.18f, 0.25f, 1f),
                        text = Color.white,
                        mutedText = new Color(0.78f, 0.80f, 0.86f, 1f),
                        panel = new Color(0.06f, 0.07f, 0.09f, 0.88f),
                        shadow = new Color(0f, 0f, 0f, 0.55f),
                        titleSize = 44,
                        bodySize = 19,
                        buttonSize = 24,
                        buttonSizeDelta = new Vector2(236, 66),
                        smallButtonSizeDelta = new Vector2(148, 54)
                    };
                case "dark":
                    return new UnityAiGameStylePreset
                    {
                        name = "dark",
                        primary = new Color(0.25f, 0.55f, 0.98f, 1f),
                        secondary = new Color(0.08f, 0.10f, 0.14f, 0.92f),
                        accent = new Color(0.20f, 0.86f, 0.72f, 1f),
                        danger = new Color(0.92f, 0.20f, 0.28f, 1f),
                        text = Color.white,
                        mutedText = new Color(0.72f, 0.78f, 0.88f, 1f),
                        panel = new Color(0.05f, 0.06f, 0.09f, 0.86f),
                        shadow = new Color(0f, 0f, 0f, 0.50f),
                        titleSize = 42,
                        bodySize = 18,
                        buttonSize = 23,
                        buttonSizeDelta = new Vector2(228, 64),
                        smallButtonSizeDelta = new Vector2(144, 52)
                    };
                case "casual":
                    return new UnityAiGameStylePreset
                    {
                        name = "casual",
                        primary = new Color(0.16f, 0.62f, 0.95f, 1f),
                        secondary = new Color(0.12f, 0.20f, 0.32f, 0.90f),
                        accent = new Color(1.00f, 0.55f, 0.25f, 1f),
                        danger = new Color(0.94f, 0.24f, 0.28f, 1f),
                        text = Color.white,
                        mutedText = new Color(0.84f, 0.90f, 1f, 1f),
                        panel = new Color(0.08f, 0.16f, 0.26f, 0.82f),
                        shadow = new Color(0f, 0f, 0f, 0.38f),
                        titleSize = 43,
                        bodySize = 19,
                        buttonSize = 24,
                        buttonSizeDelta = new Vector2(232, 64),
                        smallButtonSizeDelta = new Vector2(146, 54)
                    };
                default:
                    return new UnityAiGameStylePreset
                    {
                        name = "arcade",
                        primary = new Color(0.98f, 0.55f, 0.14f, 1f),
                        secondary = new Color(0.12f, 0.18f, 0.30f, 0.92f),
                        accent = new Color(0.20f, 0.76f, 1.00f, 1f),
                        danger = new Color(0.93f, 0.16f, 0.24f, 1f),
                        text = Color.white,
                        mutedText = new Color(0.86f, 0.92f, 1f, 1f),
                        panel = new Color(0.08f, 0.12f, 0.22f, 0.84f),
                        shadow = new Color(0f, 0f, 0f, 0.44f),
                        titleSize = 44,
                        bodySize = 19,
                        buttonSize = 25,
                        buttonSizeDelta = new Vector2(240, 66),
                        smallButtonSizeDelta = new Vector2(148, 54)
                    };
            }
        }

        public static void ApplyText(Text text, UnityAiGameStylePreset style, JObject args, string role = "body")
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = ParseAnchor(args.Value<string>("alignment"), TextAnchor.MiddleCenter);
            text.resizeTextForBestFit = args.Value<bool?>("bestFit") ?? true;
            text.resizeTextMinSize = args.Value<int?>("minFontSize") ?? 14;
            text.resizeTextMaxSize = args.Value<int?>("fontSize") ?? FontSizeForRole(style, role);
            text.fontSize = text.resizeTextMaxSize;
            text.color = style.text;
            UnityAiEditorControlTools.ApplyColor(args["color"], value => text.color = value);

            var shadow = text.GetComponent<Shadow>();
            if (shadow == null)
                shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = style.shadow;
            shadow.effectDistance = args["shadowDistance"] == null
                ? new Vector2(0, -2)
                : UnityAiEditorControlTools.ReadVector2(args["shadowDistance"], new Vector2(0, -2));

            if (args.Value<bool?>("outline") ?? (role == "title" || role == "button"))
            {
                var outline = text.GetComponent<Outline>();
                if (outline == null)
                    outline = text.gameObject.AddComponent<Outline>();
                outline.effectColor = args["outlineColor"] == null ? new Color(0f, 0f, 0f, 0.46f) : ReadColor(args["outlineColor"]);
                outline.effectDistance = new Vector2(1.4f, -1.4f);
            }
        }

        public static GameObject CreateStyledButton(Transform parent, string name, string label, JObject args)
        {
            var style = FromArgs(args);
            var variant = args.Value<string>("variant") ?? "primary";
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Shadow));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = UnityAiEditorControlTools.ReadVector2(args["sizeDelta"], style.buttonSizeDelta);
            rect.anchoredPosition = UnityAiEditorControlTools.ReadVector2(args["anchoredPosition"], Vector2.zero);
            rect.anchorMin = UnityAiEditorControlTools.ReadVector2(args["anchorMin"], new Vector2(0.5f, 0.5f));
            rect.anchorMax = UnityAiEditorControlTools.ReadVector2(args["anchorMax"], new Vector2(0.5f, 0.5f));
            rect.pivot = UnityAiEditorControlTools.ReadVector2(args["pivot"], new Vector2(0.5f, 0.5f));

            var image = go.GetComponent<Image>();
            image.sprite = EnsureRoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = ColorForVariant(style, variant);
            UnityAiEditorControlTools.ApplyColor(args["backgroundColor"], value => image.color = value);

            var shadow = go.GetComponent<Shadow>();
            shadow.effectColor = style.shadow;
            shadow.effectDistance = new Vector2(0, -5);

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.55f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow), typeof(Outline));
            labelObject.transform.SetParent(go.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12, 4);
            labelRect.offsetMax = new Vector2(-12, -4);

            var text = labelObject.GetComponent<Text>();
            text.text = label;
            ApplyText(text, style, args, "button");
            UnityAiEditorControlTools.ApplyColor(args["textColor"], value => text.color = value);

            return go;
        }

        public static GameObject CreateStyledText(Transform parent, string name, string value, JObject args, string role = "body")
        {
            var style = FromArgs(args);
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            ApplyText(text, style, args, role);
            UnityAiEditorControlTools.ApplyRectTransform(go.GetComponent<RectTransform>(), args);
            return go;
        }

        public static Sprite EnsureRoundedSprite()
        {
            UnityAiEditorControlTools.EnsureAssetParentFolder(RoundedSpritePath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
            if (sprite != null)
                return sprite;

            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    var dx = Mathf.Max(7 - x, x - 24, 0);
                    var dy = Mathf.Max(7 - y, y - 24, 0);
                    var alpha = dx * dx + dy * dy <= 49 ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            texture.Apply();
            File.WriteAllBytes(RoundedSpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(RoundedSpritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(RoundedSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.spriteBorder = new Vector4(8, 8, 8, 8);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(RoundedSpritePath);
        }

        public static Color ColorForVariant(UnityAiGameStylePreset style, string variant)
        {
            switch ((variant ?? "primary").Trim().ToLowerInvariant())
            {
                case "secondary":
                    return style.secondary;
                case "accent":
                    return style.accent;
                case "danger":
                    return style.danger;
                case "panel":
                    return style.panel;
                default:
                    return style.primary;
            }
        }

        static int FontSizeForRole(UnityAiGameStylePreset style, string role)
        {
            switch ((role ?? "body").ToLowerInvariant())
            {
                case "title":
                    return style.titleSize;
                case "button":
                    return style.buttonSize;
                default:
                    return style.bodySize;
            }
        }

        static TextAnchor ParseAnchor(string value, TextAnchor fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;
            return (TextAnchor)Enum.Parse(typeof(TextAnchor), value, ignoreCase: true);
        }

        static Color ReadColor(JToken token)
        {
            var values = token.ToObject<float[]>();
            if (values == null || (values.Length != 3 && values.Length != 4))
                throw new ArgumentException("Color values must be [r, g, b] or [r, g, b, a].");
            return new Color(values[0], values[1], values[2], values.Length == 4 ? values[3] : 1f);
        }
    }
}
#endif
