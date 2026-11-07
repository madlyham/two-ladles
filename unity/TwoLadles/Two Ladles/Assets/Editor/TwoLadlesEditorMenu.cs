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
        EditorApplication.delayCall += () =>
        {
            if (GameObject.Find("KitchenController") == null)
            {
                Debug.Log("[TwoLadles] Empty scene detected on editor launch. Auto-generating kitchen scene...");
                BuildScript.GenerateMainScene();
            }
        };
    }

    [MenuItem("Two Ladles/Setup Kitchen Scene in Editor")]
    public static void SetupKitchenScene()
    {
        BuildScript.GenerateMainScene();
        Debug.Log("Two Ladles: Kitchen Scene setup and saved successfully!");
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
