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
    }
}
#endif
