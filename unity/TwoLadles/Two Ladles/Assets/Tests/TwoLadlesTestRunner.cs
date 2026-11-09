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
            qa.StartAutoplayRun(EditorPrefs.GetInt("TwoLadles_QADay", 1), 5, UnityEngine.Random.Range(10, 9999), 2.0f);
        }

        // Pick which day the Two-Bot QA (TAS) run starts from.
        [MenuItem("Two Ladles/QA Test From Day/Day 1")] public static void QAFromDay1() { RunQAFromDay(1); }
        [MenuItem("Two Ladles/QA Test From Day/Day 2")] public static void QAFromDay2() { RunQAFromDay(2); }
        [MenuItem("Two Ladles/QA Test From Day/Day 3")] public static void QAFromDay3() { RunQAFromDay(3); }
        [MenuItem("Two Ladles/QA Test From Day/Day 4")] public static void QAFromDay4() { RunQAFromDay(4); }
        [MenuItem("Two Ladles/QA Test From Day/Day 5")] public static void QAFromDay5() { RunQAFromDay(5); }

        private static void RunQAFromDay(int day)
        {
            EditorPrefs.SetInt("TwoLadles_QADay", day);
            RunTwoBotQATest();
        }
    }
}
#endif
