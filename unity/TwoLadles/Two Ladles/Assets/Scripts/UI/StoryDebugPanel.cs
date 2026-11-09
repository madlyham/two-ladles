using UnityEngine;
using TwoLadles.Core;
using TwoLadles.Visuals;

namespace TwoLadles.UI
{
    /// <summary>
    /// Developer-only "STORY" tab for previewing story visuals without playing through days:
    /// phone opening (swipe), Day 1-5 chats + dish cards, language switch, opening-flag reset,
    /// and day/night lighting stages. Editor and Development builds only. Toggle: STORY button or F9.
    /// </summary>
    public class StoryDebugPanel : MonoBehaviour
    {
        private bool open;
        private Rect window = new Rect(10, 40, 260, 330);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (Application.isMobilePlatform) return; // mobile players never see test tools
            if (FindObjectOfType<StoryDebugPanel>() != null) return;
            var go = new GameObject("StoryDebugPanel");
            DontDestroyOnLoad(go);
            go.AddComponent<StoryDebugPanel>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9)) open = !open;
        }

        private void OnGUI()
        {
            GUI.depth = -1000;
            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, TwoLadles.AI.BotDebugHUD.DEBUG_UI_ALPHA);
            if (GUI.Button(new Rect(10, 10, 70, 24), open ? "STORY x" : "STORY")) open = !open;
            if (open) window = GUI.Window(924601, window, DrawWindow, "Story / Visual Test (F9)");
            GUI.color = prevColor;
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label("Phone story");
            if (GUILayout.Button("Opening (swipe) + Day 1"))
            {
                PlayerPrefs.DeleteKey(PhoneStoryUI.OPENING_SEEN_PREF);
                ShowDay(1);
            }
            GUILayout.BeginHorizontal();
            for (int d = 1; d <= 5; d++)
                if (GUILayout.Button("Day " + d)) ShowDay(d);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Close phone")) PhoneStoryUI.Close();
            if (GUILayout.Button("Reset 'opening seen' flag"))
            {
                PlayerPrefs.DeleteKey(PhoneStoryUI.OPENING_SEEN_PREF);
                PlayerPrefs.Save();
            }

            GUILayout.Space(6);
            string lang = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLanguage : "?";
            GUILayout.Label("Language (now: " + lang + ")");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("EN")) SetLang(LocalizationManager.LANG_EN);
            if (GUILayout.Button("VI")) SetLang(LocalizationManager.LANG_VI);
            if (GUILayout.Button("KO")) SetLang(LocalizationManager.LANG_KO);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("Day/night lighting");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Day")) SetLight(0);
            if (GUILayout.Button("Noon")) SetLight(1);
            if (GUILayout.Button("Night")) SetLight(2);
            if (GUILayout.Button("Off")) SetLight(-1);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Lighting: follow waves (auto)") && DayNightCycleManager.Instance != null)
                DayNightCycleManager.Instance.ResumeAuto();

            GUI.DragWindow();
        }

        private void ShowDay(int day)
        {
            StopAllCoroutines();
            StartCoroutine(ShowDayRoutine(day));
        }

        // Close() destroys the phone at end of frame, so wait until it is gone before reopening.
        private System.Collections.IEnumerator ShowDayRoutine(int day)
        {
            PhoneStoryUI.Close();
            while (PhoneStoryUI.IsShowing) yield return null;
            PhoneStoryUI.Show(day, null);
        }

        private static void SetLang(string code)
        {
            if (LocalizationManager.Instance != null) LocalizationManager.Instance.SetLanguage(code);
        }

        private static void SetLight(int stage)
        {
            if (DayNightCycleManager.Instance != null) DayNightCycleManager.Instance.PreviewStage(stage);
        }
    }
}
