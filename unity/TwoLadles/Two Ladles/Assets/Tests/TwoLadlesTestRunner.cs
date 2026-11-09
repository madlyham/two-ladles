#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace TwoLadles.Tests
{
    public static class TwoLadlesTestRunner
    {
        [MenuItem("Two Ladles/Run All Tests")]
        public static void RunAllTestsBatch()
        {
            try
            {
                Debug.Log("[TestRunner] Starting headless Unity test execution...");
                var testObj = new GameObject("TestRunner");
                var suite = testObj.AddComponent<TwoLadlesTestSuite>();
                suite.RunAllTests();

                if (suite.testsFailed > 0)
                {
                    Debug.LogError($"[TestRunner] TEST SUITE FAILED: {suite.testsFailed} failures, {suite.testsPassed} passed.");
                    EditorApplication.Exit(1);
                }
                else
                {
                    Debug.Log($"[TestRunner] TEST SUITE SUCCEEDED: All {suite.testsPassed} assertions passed cleanly!");
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestRunner] UNHANDLED EXCEPTION IN TEST RUNNER: {ex.Message}\n{ex.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Two Ladles/🤖 Run Two-Bot QA Test")]
        public static void RunTwoBotQATest()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.isPlaying = true;
            }

            var qa = UnityEngine.Object.FindObjectOfType<TwoLadles.AI.TwoBotQAManager>();
            if (qa == null)
            {
                var go = new GameObject("TwoBotQAManager");
                qa = go.AddComponent<TwoLadles.AI.TwoBotQAManager>();
            }
            qa.StartAutoplayRun(1, 5, UnityEngine.Random.Range(10, 9999), 2.0f);
        }
    }
}
#endif
