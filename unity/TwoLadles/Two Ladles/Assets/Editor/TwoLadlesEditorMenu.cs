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
