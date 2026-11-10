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
        ApplyGameIcon();

        // Create new empty scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Camera & Lighting
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 10.6f;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camObj.transform.position = new Vector3(0.0f, 15.0f, -9.0f);
        camObj.transform.LookAt(new Vector3(0.0f, 0.0f, 2.6f), Vector3.up);

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

    public static void ApplyGameIcon()
    {
        try
        {
            Texture2D iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/two_ladles.png");
            if (iconTex == null)
                iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Sprites/UI/two_ladles.png");
            if (iconTex != null)
            {
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { iconTex });
            }
        }
        catch (Exception) { }
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
        PlayerSettings.bundleVersion = "1.4.8";
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

    public static void BuildAndroid()
    {
        Debug.Log("[BuildScript] Starting Android Standalone APK Build...");
        ConfigureAndroidTools();
        GenerateMainScene();

        string buildDir = "Builds/Android";
        if (!Directory.Exists(buildDir))
            Directory.CreateDirectory(buildDir);

        string apkPath = $"{buildDir}/TwoLadles.apk";

        PlayerSettings.productName = "Two Ladles";
        PlayerSettings.companyName = "Two Ladles Team";
        PlayerSettings.bundleVersion = "1.4.8";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.bundleVersionCode = 148;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new string[] { SCENE_PATH };
        buildPlayerOptions.locationPathName = apkPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] ANDROID BUILD SUCCESSFUL! APK generated at: {apkPath} ({summary.totalSize} bytes)");
        }
        else
        {
            Debug.LogError($"[BuildScript] ANDROID BUILD FAILED! Errors: {summary.totalErrors}, Result: {summary.result}");
            EditorApplication.Exit(1);
        }
    }

    // Points Unity at the locally installed JDK/SDK/NDK (only when those folders exist).
    private static void ConfigureAndroidTools()
    {
        try
        {
            string toolsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "two-ladles-tools");
            // Unity 2022.3 requires JDK 11 (JDK 17 is rejected as incompatible).
            string jdk = "";
            string jdkParent = Path.Combine(toolsRoot, "jdk");
            if (Directory.Exists(jdkParent))
            {
                string[] found = Directory.GetDirectories(jdkParent, "jdk-11*");
                if (found.Length > 0) jdk = found[0];
            }
            string sdk = Path.Combine(toolsRoot, "android-sdk");
            string ndk = Path.Combine(sdk, "ndk", "23.1.7779620");
            if (Directory.Exists(jdk)) { EditorPrefs.SetBool("JdkUseEmbedded", false); EditorPrefs.SetString("JdkPath", jdk); }
            if (Directory.Exists(sdk)) { EditorPrefs.SetBool("SdkUseEmbedded", false); EditorPrefs.SetString("AndroidSdkRoot", sdk); }
            if (Directory.Exists(ndk)) { EditorPrefs.SetBool("NdkUseEmbedded", false); EditorPrefs.SetString("AndroidNdkRoot", ndk); EditorPrefs.SetString("AndroidNdkRootR23b", ndk); }
            // Official API (UnityEditor.Android.AndroidExternalToolsSettings), via reflection so this
            // file still compiles when the Android module is not installed.
            Type ext = Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor.Android.Extensions");
            if (ext != null)
            {
                void SetProp(string name, string value)
                {
                    var p = ext.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (p != null && p.CanWrite && Directory.Exists(value))
                    {
                        try { p.SetValue(null, value); }
                        catch (Exception ex) { Debug.LogWarning($"[BuildScript] Could not set {name}: {(ex.InnerException ?? ex).Message}"); }
                    }
                }
                SetProp("jdkRootPath", jdk);
                SetProp("sdkRootPath", sdk);
                SetProp("ndkRootPath", ndk);
            }
            else
            {
                Debug.LogWarning("[BuildScript] AndroidExternalToolsSettings not found (Android module missing?).");
            }
            Debug.Log($"[BuildScript] Android tools: JDK={Directory.Exists(jdk)} SDK={Directory.Exists(sdk)} NDK={Directory.Exists(ndk)}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[BuildScript] Could not configure Android tools: {e.Message}");
        }
    }

    public static void CaptureKitchenPreview()
    {
        try
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

            Camera cam = Camera.main;
            if (cam == null)
                cam = GameObject.FindObjectOfType<Camera>();
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                cam = camObj.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 9.5f;
                cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                camObj.transform.position = new Vector3(0.0f, 15.0f, -11.0f);
                camObj.transform.LookAt(new Vector3(0.0f, 0.0f, 0.6f), Vector3.up);
            }

            // Create Canvas & HUD
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.sortingOrder = 2000;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            GameObject hudObj = new GameObject("KitchenHUD", typeof(RectTransform));
            hudObj.transform.SetParent(canvasObj.transform, false);
            var hud = hudObj.AddComponent<KitchenHUD>();
            hud.AutoBuildUI();

            GameObject lobbyObj = new GameObject("LobbyUI", typeof(RectTransform));
            lobbyObj.transform.SetParent(canvasObj.transform, false);
            var lobby = lobbyObj.AddComponent<LobbyUI>();
            lobby.AutoBuildUI();
            Canvas.ForceUpdateCanvases();

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

            // Also capture gameplay HUD view with Lobby hidden so exterior dining room, grass, reputation bar & joystick are clearly visible
            lobbyObj.SetActive(false);
            Canvas.ForceUpdateCanvases();
            RenderTexture rtGame = new RenderTexture(width, height, 24);
            cam.targetTexture = rtGame;
            cam.Render();
            RenderTexture.active = rtGame;
            Texture2D texGame = new Texture2D(width, height, TextureFormat.RGB24, false);
            texGame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texGame.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rtGame);
            byte[] gameBytes = texGame.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texGame);
            string gameplayPreviewFile = $"{previewDir}/unity_kitchen_gameplay_view.png";
            File.WriteAllBytes(gameplayPreviewFile, gameBytes);

            string webPreview = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../web/assets/kitchen_preview.png"));
            if (File.Exists(webPreview) || Directory.Exists(Path.GetDirectoryName(webPreview)))
            {
                File.WriteAllBytes(webPreview, gameBytes);
                Debug.Log($"[BuildScript] Web preview also updated at: {webPreview}");
            }
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BuildScript] CaptureKitchenPreview Exception: {ex}");
            EditorApplication.Exit(1);
        }
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

    public static void RunAutoplayQABatch()
    {
        Debug.Log("[BuildScript] Starting Headless Two-Bot Autoplay QA Batch...");
        var go = new GameObject("QAManagerRunner");
        var qa = go.AddComponent<TwoLadles.AI.TwoBotQAManager>();
        qa.exitOnCompletion = true;
        qa.ParseCommandLineArguments();
        qa.StartAutoplayRun(qa.startDay, qa.targetDay, qa.randomSeed, qa.simTimeScale);
    }
}
