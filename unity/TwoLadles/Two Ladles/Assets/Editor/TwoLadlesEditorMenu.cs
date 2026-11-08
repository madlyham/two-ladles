using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TwoLadles.Kitchen;
using TwoLadles.Core;
using TwoLadles.Networking;
using TwoLadles.UI;

public static class TwoLadlesEditorMenu
{
    [InitializeOnLoadMethod]
    private static void AutoSetupOnEditorOpen()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        EditorApplication.delayCall += () =>
        {
            RepairEditorWindowsAndCamera();

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
                return;

            if (Camera.main == null && GameObject.Find("Main Camera") == null)
            {
                Debug.Log("[TwoLadles] Empty scene detected on editor launch. Auto-generating kitchen scene...");
                BuildScript.GenerateMainScene();
            }

            // Populate kitchen in Edit Mode so the Scene View shows the full kitchen immediately
            EnsureEditModeKitchenVisible();
        };
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            FocusStandardGameView();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += () =>
            {
                RepairSceneViewCamera();
                EnsureEditModeKitchenVisible();
            };
        }
    }

    private static void EnsureEditModeKitchenVisible()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
            return;

        KitchenController kc = UnityEngine.Object.FindObjectOfType<KitchenController>();
        if (kc != null)
        {
            // Rebuild Edit-Mode kitchen if it was still using the old layout without v3 (Self-Assembly & East Prep)
            Transform wallRoot = kc.transform.Find("FloorTileGrid/VisualBackWall");
            if (wallRoot == null || wallRoot.Find("RestCorner_UpperLeft_v3") == null)
            {
                foreach (var oldChef in UnityEngine.Object.FindObjectsOfType<TwoLadles.Player.PlayerController>())
                {
                    if (oldChef != null)
                        UnityEngine.Object.DestroyImmediate(oldChef.gameObject);
                }
                UnityEngine.Object.DestroyImmediate(kc.gameObject);
                kc = null;
            }
        }

        // Apply Two Ladles Game Icon
        BuildScript.ApplyGameIcon();

        if (kc == null)
        {
            KitchenController.InvalidateSpriteCache();
            GameObject kcObj = new GameObject("KitchenController");
            kc = kcObj.AddComponent<KitchenController>();
            kc.autoBuildKitchenOnStart = true;
            kc.BuildEnvironment();
            kc.BuildPerimeterWalls();
            kc.BuildStations();
            kc.SpawnPlayers();
            ForceFrameSceneViewCamera();
            RenderLiveEditorPreview();
        }
    }

    public static void RenderLiveEditorPreview()
    {
        try
        {
            Camera cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindObjectOfType<Camera>();
            if (cam == null) return;

            int width = 1280;
            int height = 720;
            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture prevTarget = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] bytes = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            if (!System.IO.Directory.Exists("Builds/preview"))
                System.IO.Directory.CreateDirectory("Builds/preview");
            System.IO.File.WriteAllBytes("Builds/preview/unity_kitchen_gameplay_view.png", bytes);
            Debug.Log($"[TwoLadles] Live Editor preview saved to Builds/preview/unity_kitchen_gameplay_view.png ({bytes.Length} bytes)");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TwoLadles] RenderLiveEditorPreview failed: {ex.Message}");
        }
    }

    public static void RepairEditorWindowsAndCamera()
    {
        RepairSceneViewCamera();
        ReplaceCorruptedSimulatorWithGameView();
    }

    private static void ForceFrameSceneViewCamera()
    {
        if (SceneView.sceneViews == null) return;
        foreach (object obj in SceneView.sceneViews)
        {
            SceneView sv = obj as SceneView;
            if (sv == null) continue;
            sv.in2DMode = false;
            sv.orthographic = true;
            sv.pivot = new Vector3(0.0f, 0.0f, 2.0f);
            sv.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
            sv.size = 12.0f;
            sv.Repaint();
        }
    }

    private static void RepairSceneViewCamera()
    {
        if (SceneView.sceneViews == null) return;
        foreach (object obj in SceneView.sceneViews)
        {
            SceneView sv = obj as SceneView;
            if (sv == null) continue;

            // Fix SceneView if camera drifted far away (e.g. z=71009, size=189714)
            if (sv.size > 50f || float.IsNaN(sv.size) || sv.pivot.magnitude > 50f)
            {
                sv.in2DMode = false;
                sv.orthographic = true;
                sv.pivot = new Vector3(0.0f, 0.0f, 2.0f);
                sv.rotation = Quaternion.Euler(51.0f, 0.0f, 0.0f);
                sv.size = 12.0f;
                sv.Repaint();
            }
        }
    }

    private static void ReplaceCorruptedSimulatorWithGameView()
    {
        try
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            Type playModeViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.PlayModeView");

            // Close any corrupted Device Simulator window (which had controlPanelWidth: NaN and Play Unfocused)
            EditorWindow[] allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            bool closedSimulator = false;
            foreach (var win in allWindows)
            {
                if (win != null && win.GetType().FullName != null && win.GetType().FullName.Contains("SimulatorWindow"))
                {
                    win.Close();
                    closedSimulator = true;
                }
            }

            // Ensure standard GameView tab is open and docked next to SceneView
            if (gameViewType != null)
            {
                UnityEngine.Object[] existingGameViews = Resources.FindObjectsOfTypeAll(gameViewType);
                EditorWindow gv = (existingGameViews != null && existingGameViews.Length > 0)
                    ? existingGameViews[0] as EditorWindow
                    : EditorWindow.GetWindow(gameViewType, false, "Game", false);

                if (gv != null && playModeViewType != null)
                {
                    // Set m_EnterPlayModeBehavior = 0 (Play Focused) so pressing Play always switches to Game View
                    FieldInfo behaviorField = playModeViewType.GetField("m_EnterPlayModeBehavior", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (behaviorField != null)
                    {
                        behaviorField.SetValue(gv, 0);
                    }
                }

                // Keep SceneView active in Edit Mode if we aren't playing yet
                if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode && closedSimulator)
                {
                    if (SceneView.lastActiveSceneView != null)
                        SceneView.lastActiveSceneView.Focus();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TwoLadles] Could not auto-configure GameView window: {ex.Message}");
        }
    }

    private static void FocusStandardGameView()
    {
        try
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType != null)
            {
                EditorWindow gv = EditorWindow.GetWindow(gameViewType, false, "Game", true);
                if (gv != null)
                {
                    gv.Focus();
                    gv.Repaint();
                }
            }
        }
        catch (Exception) { }
    }

    [MenuItem("Two Ladles/Fix Editor Camera & Launch Play Mode")]
    public static void FixAndPlay()
    {
        RepairEditorWindowsAndCamera();
        EnsureEditModeKitchenVisible();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Two Ladles/Setup Kitchen Scene in Editor")]
    public static void SetupKitchenScene()
    {
        BuildScript.GenerateMainScene();
        EnsureEditModeKitchenVisible();
        RepairEditorWindowsAndCamera();
        Debug.Log("Two Ladles: Kitchen Scene setup and saved successfully!");
    }

    [MenuItem("Two Ladles/Build Standalone Windows EXE")]
    public static void BuildWindowsExe()
    {
        BuildScript.BuildWindows();
    }

    [MenuItem("Two Ladles/Build Android Standalone APK")]
    public static void BuildAndroidApk()
    {
        BuildScript.BuildAndroid();
    }

    [MenuItem("Two Ladles/Run Automated Test Suite")]
    public static void RunTestSuite()
    {
        var runnerObj = new GameObject("TestRunnerTemp");
        var suite = runnerObj.AddComponent<TwoLadles.Tests.TwoLadlesTestSuite>();
        suite.RunAllTests();
        UnityEngine.Object.DestroyImmediate(runnerObj);
    }
}
