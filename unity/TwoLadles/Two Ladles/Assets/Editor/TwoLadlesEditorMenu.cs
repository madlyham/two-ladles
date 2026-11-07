using UnityEditor;
using UnityEngine;
using TwoLadles.Kitchen;
using TwoLadles.Core;
using TwoLadles.Networking;
using TwoLadles.UI;

public static class TwoLadlesEditorMenu
{
    [MenuItem("Two Ladles/Setup Kitchen Scene in Editor")]
    public static void SetupKitchenScene()
    {
        // 1. Ensure Managers exist
        GameObject mgr = GameObject.Find("GameManagers");
        if (mgr == null)
        {
            mgr = new GameObject("GameManagers");
            mgr.AddComponent<GameData>();
            mgr.AddComponent<ProgressionManager>();
            mgr.AddComponent<ShiftManager>();
            mgr.AddComponent<NetworkManager>();
            Undo.RegisterCreatedObjectUndo(mgr, "Create GameManagers");
        }

        // 2. Ensure KitchenController exists
        GameObject kcObj = GameObject.Find("KitchenController");
        if (kcObj == null)
        {
            kcObj = new GameObject("KitchenController");
            Undo.RegisterCreatedObjectUndo(kcObj, "Create KitchenController");
        }
        var kc = kcObj.GetComponent<KitchenController>();
        if (kc == null) kc = kcObj.AddComponent<KitchenController>();

        // Build Environment, Walls, Stations, Players
        kc.BuildEnvironment();
        kc.BuildPerimeterWalls();
        kc.BuildStations();
        kc.SpawnPlayers();

        // 3. Setup UI Canvas
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");

            GameObject esObj = GameObject.Find("EventSystem");
            if (esObj == null)
            {
                esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            GameObject hudObj = new GameObject("KitchenHUD");
            hudObj.transform.SetParent(canvasObj.transform, false);
            hudObj.AddComponent<KitchenHUD>();
        }

        Debug.Log("Two Ladles: Kitchen Scene setup completed successfully!");
    }

    [MenuItem("Two Ladles/Build Standalone Windows EXE")]
    public static void BuildWindowsExe()
    {
        BuildScript.BuildWindows();
    }

    [MenuItem("Two Ladles/Run Automated Test Suite")]
    public static void RunTestSuite()
    {
        var runnerObj = new GameObject("TestRunnerTemp");
        var suite = runnerObj.AddComponent<TwoLadles.Tests.TwoLadlesTestSuite>();
        suite.RunAllTests();
        Object.DestroyImmediate(runnerObj);
    }
}
