using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Kitchen;
using TwoLadles.Core;
using TwoLadles.Stations;

namespace TwoLadles.AI
{
    /// <summary>
    /// Real-time on-screen telemetry overlay for Autonomous Bot actions and state.
    /// Rendered via OnGUI() so it requires zero canvas/prefab setup and works instantly in Play Mode.
    /// Toggle visibility anytime with [F1].
    /// </summary>
    public class BotDebugHUD : MonoBehaviour
    {
        public static BotDebugHUD Instance { get; private set; }
        /// <summary>Hidden by default; F1 shows it. Phones/tablets never show it (no F1 key).</summary>
        public bool showHUD = false;

        /// <summary>Shared opacity for all developer/test overlays (TAS board, QA report, story tab).</summary>
        public const float DEBUG_UI_ALPHA = 0.55f;

        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyleLinh;
        private GUIStyle _labelStyleJiwon;
        private GUIStyle _logStyle;
        private GUIStyle _warningLogStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!Application.isMobilePlatform && Input.GetKeyDown(KeyCode.F1))
            {
                showHUD = !showHUD;
            }
        }

        private void InitStyles()
        {
            if (_boxStyle != null) return;

            Texture2D darkBg = new Texture2D(1, 1);
            darkBg.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.10f, DEBUG_UI_ALPHA));
            darkBg.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = darkBg;
            _boxStyle.padding = new RectOffset(10, 10, 8, 8);

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.40f, 0.90f, 1.0f) }
            };

            _labelStyleLinh = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 1.0f, 0.65f) }
            };

            _labelStyleJiwon = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.85f, 0.35f) }
            };

            _logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.85f, 0.88f, 0.92f) },
                wordWrap = true
            };

            _warningLogStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1.0f, 0.40f, 0.35f) },
                wordWrap = true
            };
        }

        private void OnGUI()
        {
            if (!showHUD || Application.isMobilePlatform) return;

            // Only show when bots or QA mode exist
            bool hasBots = false;
            BotController linhBot = null;
            BotController jiwonBot = null;

            if (KitchenController.Instance != null)
            {
                foreach (var chef in KitchenController.Instance.playersByPeer.Values)
                {
                    if (chef != null)
                    {
                        var b = chef.GetComponent<BotController>();
                        if (b != null && b.botActive)
                        {
                            hasBots = true;
                            if (b.characterId == "linh") linhBot = b;
                            else if (b.characterId == "jiwon") jiwonBot = b;
                        }
                    }
                }
            }

            if (!hasBots && (ShiftManager.Instance == null || !ShiftManager.Instance.isAutoplayQAMode))
            {
                return;
            }

            InitStyles();

            // Render telemetry box in bottom-left corner
            float panelWidth = 430f;
            float panelHeight = 270f;
            float margin = 10f;
            float x = margin;
            float y = Screen.height - panelHeight - margin;

            GUILayout.BeginArea(new Rect(x, y, panelWidth, panelHeight), _boxStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("BOT TAS TELEMETRY (Press [F1] to Hide)", _headerStyle);
            if (GUILayout.Button("Clear", GUILayout.Width(45), GUILayout.Height(18)))
            {
                BotLogger.ClearLogs();
            }
            GUILayout.EndHorizontal();

            // Linh Bot Info
            if (linhBot != null)
            {
                string held = linhBot.playerController != null && linhBot.playerController.carriedItem != null ? linhBot.playerController.carriedItem.displayName : "None";
                string stName = linhBot.targetStation != null ? linhBot.targetStation.stationId : "None";
                float dist = linhBot.targetStation != null ? Vector3.Distance(linhBot.transform.position, linhBot.targetStation.transform.position) : 0f;
                GUILayout.Label($"LINH:  Goal='{linhBot.currentGoalName}' | Target={stName} ({dist:F1}m) | Held={held} | Delay={linhBot.interactCooldown:F1}s | Stuck={linhBot.stuckTimer:F1}s", _labelStyleLinh);
            }
            else
            {
                GUILayout.Label("LINH: [Human Player / Inactive]", _labelStyleLinh);
            }

            // Jiwon Bot Info
            if (jiwonBot != null)
            {
                string held = jiwonBot.playerController != null && jiwonBot.playerController.carriedItem != null ? jiwonBot.playerController.carriedItem.displayName : "None";
                string stName = jiwonBot.targetStation != null ? jiwonBot.targetStation.stationId : "None";
                float dist = jiwonBot.targetStation != null ? Vector3.Distance(jiwonBot.transform.position, jiwonBot.targetStation.transform.position) : 0f;
                GUILayout.Label($"JIWON: Goal='{jiwonBot.currentGoalName}' | Target={stName} ({dist:F1}m) | Held={held} | Delay={jiwonBot.interactCooldown:F1}s | Stuck={jiwonBot.stuckTimer:F1}s", _labelStyleJiwon);
            }
            else
            {
                GUILayout.Label("JIWON: [Human Player / Inactive]", _labelStyleJiwon);
            }

            // Pass Counter Slot Summary
            if (KitchenController.Instance != null && KitchenController.Instance.stations.TryGetValue("pass_counter", out var passSt) && passSt is PassCounter pc)
            {
                string s0 = pc.slots[0].item != null ? $"{pc.slots[0].item.itemId} (P{pc.slots[0].placedByPeer})" : "empty";
                string s1 = pc.slots[1].item != null ? $"{pc.slots[1].item.itemId} (P{pc.slots[1].placedByPeer})" : "empty";
                string s2 = pc.slots[2].item != null ? $"{pc.slots[2].item.itemId} (P{pc.slots[2].placedByPeer})" : "empty";
                GUILayout.Label($"Pass Counter Slots: [0: {s0}] [1: {s1}] [2: {s2}]", _logStyle);
            }

            GUILayout.Space(4);
            GUILayout.Label("--- LIVE EVENT LOG (Logs/bot_qa.log) ---", _headerStyle);

            var logs = BotLogger.GetRecentLogs();
            int startIdx = Mathf.Max(0, logs.Count - 6);
            for (int i = startIdx; i < logs.Count; i++)
            {
                string l = logs[i];
                bool isWarn = l.Contains("[WARNING]") || l.Contains("[STUCK]");
                GUILayout.Label(l, isWarn ? _warningLogStyle : _logStyle);
            }

            GUILayout.EndArea();
        }
    }
}
