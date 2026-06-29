#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alday.UnityAiGameMaker.Editor
{
    public static class UnityAiRunner3DSampleBuilder
    {
        const string Root = "Assets/UnityAiGameMakerSample";
        const string Scenes = Root + "/Scenes";
        const string Scripts = Root + "/Scripts";
        const string Prefabs = Root + "/Prefabs";
        const string Materials = Root + "/Materials";

        public static object CreateScripts()
        {
            EnsureFolders();

            WriteScript("RunnerGameManager.cs", RunnerGameManagerSource);
            WriteScript("RunnerPlayerController.cs", RunnerPlayerControllerSource);
            WriteScript("RunnerCoin.cs", RunnerCoinSource);
            WriteScript("RunnerObstacle.cs", RunnerObstacleSource);
            WriteScript("RunnerFinish.cs", RunnerFinishSource);
            WriteScript("RunnerCameraFollow.cs", RunnerCameraFollowSource);
            WriteScript("RunnerMainMenu.cs", RunnerMainMenuSource);

            AssetDatabase.Refresh();
            return new
            {
                root = Root,
                scripts = Directory.GetFiles(Scripts, "*.cs").Select(path => path.Replace("\\", "/")).ToArray()
            };
        }

        public static object CreateContent()
        {
            EnsureFolders();

            var green = CreateMaterial("PlayerGreen", new Color(0.1f, 0.8f, 0.35f));
            var gold = CreateMaterial("CoinGold", new Color(1f, 0.75f, 0.05f));
            var red = CreateMaterial("ObstacleRed", new Color(0.95f, 0.12f, 0.1f));
            var blue = CreateMaterial("GoalBlue", new Color(0.1f, 0.45f, 1f));
            var floor = CreateMaterial("FloorSlate", new Color(0.18f, 0.22f, 0.26f));

            var playerPrefab = CreatePrimitivePrefab("Player.prefab", PrimitiveType.Capsule, green, new Vector3(1f, 1.8f, 1f));
            var coinPrefab = CreatePrimitivePrefab("Coin.prefab", PrimitiveType.Cylinder, gold, new Vector3(0.55f, 0.08f, 0.55f));
            var obstaclePrefab = CreatePrimitivePrefab("Obstacle.prefab", PrimitiveType.Cube, red, new Vector3(1.2f, 1.2f, 1.2f));
            var goalPrefab = CreatePrimitivePrefab("Goal.prefab", PrimitiveType.Cube, blue, new Vector3(5f, 3f, 0.25f));

            AddScriptComponent(playerPrefab, "RunnerPlayerController");
            AddScriptComponent(coinPrefab, "RunnerCoin");
            AddScriptComponent(obstaclePrefab, "RunnerObstacle");
            AddScriptComponent(goalPrefab, "RunnerFinish");
            playerPrefab.tag = "Player";
            SetTrigger(coinPrefab, true);
            SetTrigger(obstaclePrefab, true);
            SetTrigger(goalPrefab, true);
            PrefabUtility.SavePrefabAsset(playerPrefab);
            PrefabUtility.SavePrefabAsset(coinPrefab);
            PrefabUtility.SavePrefabAsset(obstaclePrefab);
            PrefabUtility.SavePrefabAsset(goalPrefab);

            CreateMenuScene();
            CreateGameplayScene(floor, playerPrefab, coinPrefab, obstaclePrefab, goalPrefab);
            SetBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new
            {
                root = Root,
                menuScene = Scenes + "/MainMenu.unity",
                gameplayScene = Scenes + "/Gameplay.unity",
                prefabs = new[]
                {
                    Prefabs + "/Player.prefab",
                    Prefabs + "/Coin.prefab",
                    Prefabs + "/Obstacle.prefab",
                    Prefabs + "/Goal.prefab"
                }
            };
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets", "UnityAiGameMakerSample");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Scripts");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "Materials");
        }

        static void EnsureFolder(string parent, string name)
        {
            var path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        static void WriteScript(string fileName, string source)
        {
            File.WriteAllText(Path.Combine(Scripts, fileName), source);
        }

        static Material CreateMaterial(string name, Color color)
        {
            var path = Materials + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject CreatePrimitivePrefab(string fileName, PrimitiveType type, Material material, Vector3 scale)
        {
            var path = Prefabs + "/" + fileName;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                return prefab;

            var temp = GameObject.CreatePrimitive(type);
            temp.name = Path.GetFileNameWithoutExtension(fileName);
            temp.transform.localScale = scale;
            temp.GetComponent<Renderer>().sharedMaterial = material;

            var saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            UnityEngine.Object.DestroyImmediate(temp);
            return saved;
        }

        static void CreateMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            CreateLight();
            var camera = CreateCamera(new Vector3(0, 4, -9), new Vector3(20, 0, 0));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.13f);

            var logo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            logo.name = "Menu Runner Mascot";
            logo.transform.position = new Vector3(0, 1.2f, 0);
            logo.transform.localScale = new Vector3(1.2f, 1.8f, 1.2f);
            logo.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/PlayerGreen.mat");

            var canvas = CreateCanvas("Main Menu Canvas");
            CreateText(canvas.transform, "Title", "UNITY AI RUNNER", 48, new Vector2(0, 155), new Vector2(720, 80));
            CreateText(canvas.transform, "Subtitle", "Built end-to-end by Unity AI MCP Game Maker", 22, new Vector2(0, 95), new Vector2(720, 44));
            CreateButton(canvas.transform, "Play Button", "PLAY", new Vector2(0, 20), "Gameplay");
            CreateText(canvas.transform, "Footer", "Collect coins, dodge red blocks, reach the blue gate.", 18, new Vector2(0, -115), new Vector2(780, 40));

            SaveScene(scene, Scenes + "/MainMenu.unity");
        }

        static void CreateGameplayScene(Material floorMaterial, GameObject playerPrefab, GameObject coinPrefab, GameObject obstaclePrefab, GameObject goalPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Gameplay";

            CreateLight();
            var camera = CreateCamera(new Vector3(0, 5, -9), new Vector3(25, 0, 0));
            var follow = AddScriptComponent(camera.gameObject, "RunnerCameraFollow");

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Track";
            ground.transform.position = new Vector3(0, -0.15f, 18);
            ground.transform.localScale = new Vector3(8, 0.3f, 42);
            ground.GetComponent<Renderer>().sharedMaterial = floorMaterial;

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.position = new Vector3(0, 0.9f, -7);
            SetField(follow, "target", player.transform);

            for (var i = 0; i < 12; i++)
            {
                var coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);
                coin.name = "Coin " + (i + 1);
                coin.transform.position = new Vector3(((i % 3) - 1) * 2f, 0.65f, -2 + i * 2.6f);
                coin.transform.eulerAngles = new Vector3(90, 0, 0);
            }

            for (var i = 0; i < 7; i++)
            {
                var obstacle = (GameObject)PrefabUtility.InstantiatePrefab(obstaclePrefab);
                obstacle.name = "Obstacle " + (i + 1);
                obstacle.transform.position = new Vector3(((i + 1) % 3 - 1) * 2f, 0.6f, 2 + i * 4f);
            }

            var goal = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab);
            goal.name = "Finish Gate";
            goal.transform.position = new Vector3(0, 1.5f, 32);

            var manager = AddScriptComponent(new GameObject("Game Manager"), "RunnerGameManager");
            SetField(manager, "player", player.transform);
            SetField(manager, "menuSceneName", "MainMenu");

            var canvas = CreateCanvas("Gameplay Canvas");
            SetField(manager, "scoreText", CreateText(canvas.transform, "Score Text", "Score: 0", 24, new Vector2(-270, 190), new Vector2(260, 46)));
            SetField(manager, "statusText", CreateText(canvas.transform, "Status Text", "", 30, new Vector2(0, 145), new Vector2(760, 54)));
            CreateText(canvas.transform, "Help Text", "A/D or Arrow Keys: move lanes | Touch left/right on mobile", 17, new Vector2(0, -205), new Vector2(780, 40));

            SaveScene(scene, Scenes + "/Gameplay.unity");
        }

        static Light CreateLight()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.eulerAngles = new Vector3(50, -30, 0);
            return light;
        }

        static Camera CreateCamera(Vector3 position, Vector3 euler)
        {
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = position;
            camera.transform.eulerAngles = euler;
            return camera;
        }

        static Canvas CreateCanvas(string name)
        {
            var canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 450);

            if (FindEventSystem() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            return canvas;
        }

        static EventSystem FindEventSystem()
        {
#if UNITY_6000_0_OR_NEWER
            return UnityEngine.Object.FindAnyObjectByType<EventSystem>();
#else
            return UnityEngine.Object.FindObjectOfType<EventSystem>();
#endif
        }

        static void SetTrigger(GameObject target, bool isTrigger)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = isTrigger;
        }

        static Text CreateText(Transform parent, string name, string text, int size, Vector2 position, Vector2 rectSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = rectSize;
            rect.anchoredPosition = position;
            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            return label;
        }

        static Button CreateButton(Transform parent, string name, string label, Vector2 position, string sceneName)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220, 64);
            rect.anchoredPosition = position;
            go.GetComponent<Image>().color = new Color(0.1f, 0.45f, 1f);
            SetField(AddScriptComponent(go, "RunnerMainMenu"), "gameplaySceneName", sceneName);
            CreateText(go.transform, "Label", label, 28, Vector2.zero, new Vector2(220, 64));
            return go.GetComponent<Button>();
        }

        static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
        }

        static void SetBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Scenes + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(Scenes + "/Gameplay.unity", true)
            };
        }

        static Component AddScriptComponent(GameObject go, string typeName)
        {
            var type = FindType(typeName);
            var component = go.GetComponent(type);
            if (component == null)
                component = go.AddComponent(type);
            return component;
        }

        static Type FindType(string typeName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(typeName))
                .FirstOrDefault(found => found != null);

            if (type == null)
                throw new InvalidOperationException("Runtime sample script type not found: " + typeName + ". Run sample.runner3D.createScripts first, then reopen/compile Unity before sample.runner3D.createContent.");

            return type;
        }

        static void SetField(Component component, string fieldName, object value)
        {
            var field = component.GetType().GetField(fieldName);
            if (field == null)
                throw new InvalidOperationException("Field not found: " + component.GetType().Name + "." + fieldName);
            field.SetValue(component, value);
        }

        const string RunnerGameManagerSource = @"
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunnerGameManager : MonoBehaviour
{
    public Transform player;
    public Text scoreText;
    public Text statusText;
    public string menuSceneName = ""MainMenu"";
    int score;
    bool ended;

    void Start()
    {
        Time.timeScale = 1f;
        SetStatus("""");
        AddScore(0);
    }

    void Update()
    {
        if (!ended && player != null && player.position.y < -3f)
            Lose();

        if (ended && Input.GetKeyDown(KeyCode.Space))
            SceneManager.LoadScene(menuSceneName);
    }

    public void AddScore(int amount)
    {
        score += amount;
        if (scoreText != null)
            scoreText.text = ""Score: "" + score;
    }

    public void Win()
    {
        if (ended) return;
        ended = true;
        SetStatus(""YOU WIN! Press Space for menu"");
        Time.timeScale = 0f;
    }

    public void Lose()
    {
        if (ended) return;
        ended = true;
        SetStatus(""CRASH! Press Space for menu"");
        Time.timeScale = 0f;
    }

    void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
}
";

        const string RunnerPlayerControllerSource = @"
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RunnerPlayerController : MonoBehaviour
{
    public float forwardSpeed = 7f;
    public float laneDistance = 2f;
    public float laneChangeSpeed = 9f;
    int lane;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            lane = Mathf.Max(-1, lane - 1);
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            lane = Mathf.Min(1, lane + 1);

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            lane = Input.GetTouch(0).position.x < Screen.width * 0.5f ? Mathf.Max(-1, lane - 1) : Mathf.Min(1, lane + 1);

        var targetX = lane * laneDistance;
        var pos = transform.position;
        pos.z += forwardSpeed * Time.deltaTime;
        pos.x = Mathf.Lerp(pos.x, targetX, laneChangeSpeed * Time.deltaTime);
        transform.position = pos;
    }
}
";

        const string RunnerCoinSource = @"
using UnityEngine;

public class RunnerCoin : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(0f, 180f * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(""Player"")) return;
        var manager = FindAnyObjectByType<RunnerGameManager>();
        if (manager != null)
            manager.AddScore(10);
        Destroy(gameObject);
    }

    void Reset()
    {
        var collider = GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;
    }
}
";

        const string RunnerObstacleSource = @"
using UnityEngine;

public class RunnerObstacle : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(""Player"")) return;
        var manager = FindAnyObjectByType<RunnerGameManager>();
        if (manager != null)
            manager.Lose();
    }

    void Reset()
    {
        var collider = GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;
    }
}
";

        const string RunnerFinishSource = @"
using UnityEngine;

public class RunnerFinish : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(""Player"")) return;
        var manager = FindAnyObjectByType<RunnerGameManager>();
        if (manager != null)
            manager.Win();
    }

    void Reset()
    {
        var collider = GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = true;
    }
}
";

        const string RunnerCameraFollowSource = @"
using UnityEngine;

public class RunnerCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 5f, -8f);
    public float followSpeed = 6f;

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.Lerp(transform.position, target.position + offset, followSpeed * Time.deltaTime);
    }
}
";

        const string RunnerMainMenuSource = @"
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunnerMainMenu : MonoBehaviour
{
    public string gameplaySceneName = ""Gameplay"";

    public void StartGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    void Awake()
    {
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button != null)
            button.onClick.AddListener(StartGame);
    }
}
";
    }
}
#endif
