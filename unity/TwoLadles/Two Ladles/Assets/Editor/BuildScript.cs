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
        camObj.transform.position = new Vector3(0.0f, 11.5f, -9.5f);
        camObj.transform.LookAt(new Vector3(0.0f, 0.0f, -0.2f), Vector3.up);

        GameObject lightObj = new GameObject("Directional Light");
        Light sun = lightObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1.0f, 0.94f, 0.84f);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(51.0f, 28.0f, 0.0f);

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Save scene to MainKitchen.unity
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log($"[BuildScript] MainKitchen.unity saved successfully to {SCENE_PATH}");

        // Also save to SampleScene.unity so opening the default scene shows full kitchen immediately
        string samplePath = "Assets/Scenes/SampleScene.unity";
        EditorSceneManager.SaveScene(scene, samplePath);
        Debug.Log($"[BuildScript] SampleScene.unity populated and saved to {samplePath}");

        // Assign scenes to EditorBuildSettings
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(SCENE_PATH, true),
            new EditorBuildSettingsScene(samplePath, true)
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

    public static void CaptureKitchenPreview()
    {
        Debug.Log("[BuildScript] Capturing Unity Kitchen Preview...");
        GenerateMainScene();

        Scene scene = EditorSceneManager.OpenScene(SCENE_PATH);

        // Build kitchen in scene
        GameObject kcObj = new GameObject("KitchenController");
        KitchenController kc = kcObj.AddComponent<KitchenController>();
        kc.autoBuildKitchenOnStart = false;
        kc.BuildEnvironment();
        kc.BuildPerimeterWalls();
        kc.BuildStations();
        kc.SpawnPlayers();

        // Create Canvas & HUD
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;

        Camera cam = Camera.main;
        if (cam == null)
            cam = GameObject.FindObjectOfType<Camera>();
        canvas.worldCamera = cam;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        GameObject hudObj = new GameObject("KitchenHUD");
        hudObj.transform.SetParent(canvasObj.transform, false);
        hudObj.AddComponent<KitchenHUD>();

        int width = 1280;
        int height = 720;
        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(rt);

        byte[] bytes = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);

        string previewDir = "Builds/preview";
        if (!Directory.Exists(previewDir))
            Directory.CreateDirectory(previewDir);
        string previewFile = $"{previewDir}/unity_kitchen_view.png";
        File.WriteAllBytes(previewFile, bytes);
        Debug.Log($"[BuildScript] Unity preview successfully rendered and saved to {previewFile} ({bytes.Length} bytes)");
        EditorApplication.Exit(0);
    }

    public static void RunTestSuite()
    {
        Debug.Log("[BuildScript] Running Automated C# Test Suite...");
        var go = new GameObject("TestSuiteRunner");
        var runner = go.AddComponent<TwoLadlesTestSuite>();
        runner.RunAllTests();
        if (runner.testsFailed > 0)
        {
            Debug.LogError($"[BuildScript] TEST SUITE FAILED: {runner.testsFailed} tests failed!");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log($"[BuildScript] ALL {runner.testsPassed} TESTS PASSED CLEANLY!");
            EditorApplication.Exit(0);
        }
    }
}
