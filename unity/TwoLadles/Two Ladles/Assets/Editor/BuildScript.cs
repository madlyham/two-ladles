using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TwoLadles.Core;
using TwoLadles.Kitchen;
using TwoLadles.Networking;
using TwoLadles.UI;
using TwoLadles.Tests;

public class BuildScript
{
    private const string SCENE_PATH = "Assets/Scenes/MainKitchen.unity";

    public static void GenerateMainScene()
    {
        Debug.Log("[BuildScript] Generating MainKitchen.unity scene...");

        // Ensure directories exist
        if (!Directory.Exists("Assets/Scenes"))
            Directory.CreateDirectory("Assets/Scenes");

        // Set all png textures in Assets/Resources as Sprite (2D and UI) with Point filter
        SetupSpriteImporters();

        // Create new empty scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Camera & Lighting
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7.0f;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camObj.transform.position = new Vector3(0.0f, 11.5f, 9.5f);
        camObj.transform.LookAt(new Vector3(0.0f, 0.0f, 0.2f), Vector3.up);

        GameObject lightObj = new GameObject("Directional Light");
        Light sun = lightObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1.0f, 0.94f, 0.84f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(52.0f, -28.0f, 0.0f);

        // 2. Core Game Managers
        GameObject managersObj = new GameObject("GameManagers");
        managersObj.AddComponent<GameData>();
        managersObj.AddComponent<ProgressionManager>();
        managersObj.AddComponent<ShiftManager>();
        managersObj.AddComponent<NetworkManager>();

        // 3. Kitchen Controller (Authoritative 2.5D Kitchen Scene)
        GameObject kitchenObj = new GameObject("KitchenController");
        var kc = kitchenObj.AddComponent<KitchenController>();
        kc.autoBuildKitchenOnStart = true;

        // 4. Test Suite Runner
        GameObject testRunnerObj = new GameObject("TestRunner");
        testRunnerObj.AddComponent<TwoLadlesTestSuite>();

        // 5. UI Canvas & EventSystem
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Lobby UI Panel
        GameObject lobbyObj = new GameObject("LobbyUI");
        lobbyObj.transform.SetParent(canvasObj.transform, false);
        var lobbyUI = lobbyObj.AddComponent<LobbyUI>();

        // Kitchen HUD Panel
        GameObject hudObj = new GameObject("KitchenHUD");
        hudObj.transform.SetParent(canvasObj.transform, false);
        var hudUI = hudObj.AddComponent<KitchenHUD>();

        // Create UI elements for HUD
        CreateHudElements(hudObj, hudUI);

        // Save scene
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log($"[BuildScript] MainKitchen.unity saved successfully to {SCENE_PATH}");

        // Assign scene to EditorBuildSettings
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(SCENE_PATH, true)
        };
        AssetDatabase.SaveAssets();
    }

    private static void SetupSpriteImporters()
    {
        string[] searchFolders = new string[] { "Assets/Resources", "Assets/Sprites" };
        foreach (var folder in searchFolders)
        {
            if (!Directory.Exists(folder)) continue;
            string[] pngFiles = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);
            foreach (var file in pngFiles)
            {
                string assetPath = file.Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    bool changed = false;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        changed = true;
                    }
                    if (importer.spritePixelsPerUnit != 32)
                    {
                        importer.spritePixelsPerUnit = 32;
                        changed = true;
                    }
                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        changed = true;
                    }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        changed = true;
                    }
                    if (changed)
                    {
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                    }
                }
            }
        }
    }

    private static void CreateHudElements(GameObject hudObj, KitchenHUD hud)
    {
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Top bar
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(hudObj.transform, false);
        RectTransform rtTop = topBar.AddComponent<RectTransform>();
        rtTop.anchorMin = new Vector2(0f, 1f);
        rtTop.anchorMax = new Vector2(1f, 1f);
        rtTop.pivot = new Vector2(0.5f, 1f);
        rtTop.sizeDelta = new Vector2(0f, 50f);

        // Timer Text
        GameObject timerObj = new GameObject("ShiftTimerText");
        timerObj.transform.SetParent(topBar.transform, false);
        Text timerText = timerObj.AddComponent<Text>();
        timerText.font = defaultFont;
        timerText.fontSize = 20;
        timerText.text = "TIME: 03:00";
        timerText.color = Color.white;
        timerText.alignment = TextAnchor.MiddleCenter;
        RectTransform rtTimer = timerObj.GetComponent<RectTransform>();
        rtTimer.anchorMin = new Vector2(0.5f, 0.5f);
        rtTimer.anchorMax = new Vector2(0.5f, 0.5f);
        rtTimer.sizeDelta = new Vector2(200f, 40f);
        hud.shiftTimerText = timerText;

        // Score Text
        GameObject scoreObj = new GameObject("ScoreText");
        scoreObj.transform.SetParent(topBar.transform, false);
        Text scoreText = scoreObj.AddComponent<Text>();
        scoreText.font = defaultFont;
        scoreText.fontSize = 20;
        scoreText.text = "SCORE: 0";
        scoreText.color = new Color(1f, 0.9f, 0.3f);
        scoreText.alignment = TextAnchor.MiddleLeft;
        RectTransform rtScore = scoreObj.GetComponent<RectTransform>();
        rtScore.anchorMin = new Vector2(0f, 0.5f);
        rtScore.anchorMax = new Vector2(0f, 0.5f);
        rtScore.anchoredPosition = new Vector2(20f, 0f);
        rtScore.sizeDelta = new Vector2(200f, 40f);
        hud.scoreText = scoreText;

        // Flow Text
        GameObject flowObj = new GameObject("FlowText");
        flowObj.transform.SetParent(topBar.transform, false);
        Text flowText = flowObj.AddComponent<Text>();
        flowText.font = defaultFont;
        flowText.fontSize = 18;
        flowText.text = "FLOW: 0/100";
        flowText.color = new Color(0.4f, 0.9f, 1f);
        flowText.alignment = TextAnchor.MiddleRight;
        RectTransform rtFlow = flowObj.GetComponent<RectTransform>();
        rtFlow.anchorMin = new Vector2(1f, 0.5f);
        rtFlow.anchorMax = new Vector2(1f, 0.5f);
        rtFlow.anchoredPosition = new Vector2(-20f, 0f);
        rtFlow.sizeDelta = new Vector2(250f, 40f);
        hud.flowText = flowText;

        // Prompt Text (Bottom interaction prompt)
        GameObject promptObj = new GameObject("PromptText");
        promptObj.transform.SetParent(hudObj.transform, false);
        Text promptText = promptObj.AddComponent<Text>();
        promptText.font = defaultFont;
        promptText.fontSize = 20;
        promptText.text = "";
        promptText.color = new Color(1f, 1f, 0.5f);
        promptText.alignment = TextAnchor.MiddleCenter;
        RectTransform rtPrompt = promptObj.GetComponent<RectTransform>();
        rtPrompt.anchorMin = new Vector2(0f, 0f);
        rtPrompt.anchorMax = new Vector2(1f, 0f);
        rtPrompt.pivot = new Vector2(0.5f, 0f);
        rtPrompt.anchoredPosition = new Vector2(0f, 25f);
        rtPrompt.sizeDelta = new Vector2(0f, 40f);
        hud.promptText = promptText;

        // Order Queue Text
        GameObject ordersObj = new GameObject("OrderListText");
        ordersObj.transform.SetParent(hudObj.transform, false);
        Text ordersText = ordersObj.AddComponent<Text>();
        ordersText.font = defaultFont;
        ordersText.fontSize = 16;
        ordersText.text = "ACTIVE ORDERS:";
        ordersText.color = Color.white;
        ordersText.alignment = TextAnchor.UpperLeft;
        RectTransform rtOrders = ordersObj.GetComponent<RectTransform>();
        rtOrders.anchorMin = new Vector2(0f, 1f);
        rtOrders.anchorMax = new Vector2(0f, 1f);
        rtOrders.pivot = new Vector2(0f, 1f);
        rtOrders.anchoredPosition = new Vector2(20f, -60f);
        rtOrders.sizeDelta = new Vector2(450f, 200f);
        hud.orderListText = ordersText;

        // Feedback Banner
        GameObject feedbackObj = new GameObject("FeedbackBanner");
        feedbackObj.transform.SetParent(hudObj.transform, false);
        Text feedbackText = feedbackObj.AddComponent<Text>();
        feedbackText.font = defaultFont;
        feedbackText.fontSize = 22;
        feedbackText.text = "";
        feedbackText.color = Color.white;
        feedbackText.alignment = TextAnchor.MiddleCenter;
        RectTransform rtFeedback = feedbackObj.GetComponent<RectTransform>();
        rtFeedback.anchorMin = new Vector2(0f, 0.5f);
        rtFeedback.anchorMax = new Vector2(1f, 0.5f);
        rtFeedback.sizeDelta = new Vector2(0f, 50f);
        feedbackObj.SetActive(false);
        hud.feedbackBannerText = feedbackText;
    }

    public static void BuildWindows()
    {
        Debug.Log("[BuildScript] Starting Standalone Windows 64-bit Build...");
        GenerateMainScene();

        string buildDir = "Builds/Windows";
        if (!Directory.Exists(buildDir))
            Directory.CreateDirectory(buildDir);

        string exePath = $"{buildDir}/TwoLadles.exe";

        PlayerSettings.productName = "Two Ladles";
        PlayerSettings.companyName = "Two Ladles Team";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new string[] { SCENE_PATH };
        buildPlayerOptions.locationPathName = exePath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] BUILD SUCCESSFUL! Executable generated at: {exePath} ({summary.totalSize} bytes)");
        }
        else
        {
            Debug.LogError($"[BuildScript] BUILD FAILED! Errors: {summary.totalErrors}, Result: {summary.result}");
            EditorApplication.Exit(1);
        }
    }
}
