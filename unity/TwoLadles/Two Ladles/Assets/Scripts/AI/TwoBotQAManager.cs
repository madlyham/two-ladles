using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Kitchen;
using TwoLadles.Player;
using TwoLadles.UI;

namespace TwoLadles.AI
{
    [Serializable]
    public class QADayResult
    {
        public int day;
        public bool cleared;
        public int score;
        public int ordersServed;
        public int ordersFailed;
        public float remainingReputation;
        public float durationSeconds;
    }

    [Serializable]
    public class QARunReport
    {
        public string timestamp;
        public int seed;
        public int daysAttempted;
        public int daysCleared;
        public int totalScore;
        public int totalOrdersServed;
        public int totalOrdersFailed;
        public float overallSuccessRate;
        public List<QADayResult> dayResults = new List<QADayResult>();
    }

    /// <summary>
    /// Automated Two-Bot QA and Autoplay System.
    /// Controls BOTH Linh and Jiwon simultaneously using local, deterministic BotControllers.
    /// Supports command-line switches (-autoplayQA -seed <N> -days <1..5>), headless simulation,
    /// metric tracking, anti-stuck watchdogs, and structured reporting in qa_reports/.
    /// </summary>
    public class TwoBotQAManager : MonoBehaviour
    {
        public static TwoBotQAManager Instance { get; private set; }

        [Header("QA Configuration")]
        public bool isQARunning = false;
        public int randomSeed = 42;
        public int startDay = 1;
        public int targetDay = 5;
        public float simTimeScale = 1.0f;
        public bool exitOnCompletion = false;
        public bool qaAllUpgrades = true; // Settings toggle: give both bots Tier 3 + every upgrade for the run

        // Settings day option: 0 = All days (1-5), otherwise a single day (clamped 1-5).
        public static (int start, int end) ResolveDayRange(int option)
        {
            if (option <= 0) return (1, 5);
            int d = Mathf.Clamp(option, 1, 5);
            return (d, d);
        }

        [Header("Runtime State")]
        public int currentDayRunning = 1;
        public QARunReport currentReport = new QARunReport();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            ParseCommandLineArguments();
        }

        private void Start()
        {
            if (isQARunning)
            {
                StartAutoplayRun(startDay, targetDay, randomSeed, simTimeScale);
            }
        }

        public void ParseCommandLineArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLower();
                if (a == "-autoplayqa" || a == "--autoplayqa")
                {
                    isQARunning = true;
                    exitOnCompletion = Application.isBatchMode;
                }
                else if ((a == "-seed" || a == "--seed") && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int s)) randomSeed = s;
                }
                else if ((a == "-days" || a == "--days") && i + 1 < args.Length)
                {
                    string dayArg = args[i + 1];
                    if (dayArg.Contains("-") || dayArg.Contains(".."))
                    {
                        var parts = dayArg.Split(new[] { '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && int.TryParse(parts[0], out int d1) && int.TryParse(parts[1], out int d2))
                        {
                            startDay = d1;
                            targetDay = d2;
                        }
                    }
                    else if (int.TryParse(dayArg, out int d))
                    {
                        startDay = 1;
                        targetDay = d;
                    }
                }
                else if ((a == "-timescale" || a == "--timescale") && i + 1 < args.Length)
                {
                    if (float.TryParse(args[i + 1], out float ts)) simTimeScale = Mathf.Clamp(ts, 0.5f, 10.0f);
                }
            }
        }

        public void StartAutoplayRun(int pStartDay, int pTargetDay, int pSeed, float pTimeScale = 1.0f)
        {
            isQARunning = true;
            startDay = Mathf.Clamp(pStartDay, 1, 5);
            targetDay = Mathf.Clamp(pTargetDay, startDay, 5);
            randomSeed = pSeed;
            simTimeScale = pTimeScale;
            Time.timeScale = simTimeScale;

            UnityEngine.Random.InitState(randomSeed);

            currentReport = new QARunReport
            {
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                seed = randomSeed,
                daysAttempted = 0,
                daysCleared = 0,
                totalScore = 0,
                totalOrdersServed = 0,
                totalOrdersFailed = 0,
                overallSuccessRate = 0.0f
            };

            Debug.Log($"[TwoBotQAManager] Starting Two-Bot QA Autoplay Run (Days {startDay} to {targetDay}, Seed: {randomSeed}, TimeScale: {simTimeScale}x)...");

            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.isAutoplayQAMode = true;
                ShiftManager.Instance.OnShiftEnded -= HandleShiftEnded;
                ShiftManager.Instance.OnShiftEnded += HandleShiftEnded;
            }
            BotCommunicationHub.Reset();

            // QA always runs in a progression sandbox so bot gold/progress never touches the real save.
            ProgressionManager.Instance?.BeginQASandbox(qaAllUpgrades);

            currentDayRunning = startDay;
            LaunchDay(currentDayRunning);
        }

        public void LaunchDay(int dayId)
        {
            Debug.Log($"[TwoBotQAManager] Launching Day {dayId} in Automated Two-Bot Mode...");
            BotLogger.Log("QA", $"=== Launching Day {dayId} of {targetDay} in Automated Two-Bot Mode (TimeScale: {simTimeScale}x) ===");

            // Ensure KitchenController exists
            if (KitchenController.Instance == null)
            {
                var kcObj = new GameObject("KitchenController");
                kcObj.AddComponent<KitchenController>();
            }

            // Ensure Lobby is hidden
            var lobby = FindObjectOfType<LobbyUI>();
            if (lobby != null)
            {
                lobby.gameObject.SetActive(false);
            }

            // Configure BOTH Linh (Peer 1) and Jiwon (Peer 2) with active BotControllers
            ConfigureTwoBots();
            ProgressionManager.Instance?.UpdateLiveChefsTiers();

            // Show Live QA Banner on HUD
            if (KitchenHUD.Instance != null)
            {
                KitchenHUD.Instance.SetQAModeBanner(true, dayId, targetDay, simTimeScale);
            }

            // Start the shift
            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.StartShift(dayId, GameMode.CAMPAIGN);
            }
        }

        public void ConfigureTwoBots()
        {
            if (KitchenController.Instance == null) return;

            foreach (var kvp in KitchenController.Instance.playersByPeer)
            {
                int peer = kvp.Key;
                var chef = kvp.Value;
                if (chef == null) continue;

                chef.isLocalPlayer = false; // Both chefs driven autonomously by bots
                var bot = chef.GetComponent<BotController>();
                if (bot == null)
                {
                    bot = chef.gameObject.AddComponent<BotController>();
                }
                bot.botActive = true;
                bot.peerId = peer;
                bot.characterId = chef.characterId;
                bot.ConfigureDifficulty(BotDifficulty.Assistive); // Assistive responsiveness for QA throughput
            }
        }

        private void HandleShiftEnded(Dictionary<string, object> results)
        {
            if (!isQARunning) return;

            // Reject duplicate or stale events for a different stage
            int finishedStage = (results != null && results.TryGetValue("stage_id", out var sid)) ? Convert.ToInt32(sid) : currentDayRunning;
            if (finishedStage != currentDayRunning)
            {
                Debug.LogWarning($"[TwoBotQAManager] Ignored mismatched shift end event for Stage {finishedStage} (Expected: {currentDayRunning})");
                return;
            }

            int day = currentDayRunning;
            bool victory = results != null && results.TryGetValue("victory", out var v) && (bool)v;
            int score = 0;
            if (results != null)
            {
                if (results.TryGetValue("total_score", out var ts)) score = Convert.ToInt32(ts);
                else if (results.TryGetValue("finalScore", out var fs)) score = Convert.ToInt32(fs);
                else if (results.TryGetValue("score", out var sc)) score = Convert.ToInt32(sc);
            }
            int ordersServed = 0;
            if (results != null)
            {
                if (results.TryGetValue("orders_served", out var os1)) ordersServed = Convert.ToInt32(os1);
                else if (results.TryGetValue("ordersServed", out var os2)) ordersServed = Convert.ToInt32(os2);
            }
            int ordersFailed = 0;
            if (results != null)
            {
                if (results.TryGetValue("orders_failed", out var of1)) ordersFailed = Convert.ToInt32(of1);
                else if (results.TryGetValue("ordersFailed", out var of2)) ordersFailed = Convert.ToInt32(of2);
            }
            float rep = ShiftManager.Instance != null ? ShiftManager.Instance.reputation : 0.0f;
            float dur = ShiftManager.Instance != null ? ShiftManager.Instance.shiftElapsedSeconds : 0.0f;

            var dayRes = new QADayResult
            {
                day = day,
                cleared = victory,
                score = score,
                ordersServed = ordersServed,
                ordersFailed = ordersFailed,
                remainingReputation = rep,
                durationSeconds = dur
            };

            currentReport.dayResults.Add(dayRes);
            currentReport.daysAttempted++;
            if (victory) currentReport.daysCleared++;
            currentReport.totalScore += score;
            currentReport.totalOrdersServed += ordersServed;
            currentReport.totalOrdersFailed += ordersFailed;

            Debug.Log($"[TwoBotQAManager] Day {day} Result: {(victory ? "CLEARED" : "FAILED")} | Score: {score} | Orders: {ordersServed}/{ordersServed + ordersFailed} | Rep: {rep:F0}/100");
            BotLogger.Log("QA", $"Day {day} Completed: {(victory ? "CLEARED" : "FAILED")} | Score: {score} | Orders: {ordersServed}/{ordersServed + ordersFailed} | Rep: {rep:F0}/100");

            if (currentDayRunning < targetDay)
            {
                StartCoroutine(TransitionToNextDayRoutine(currentDayRunning + 1));
            }
            else
            {
                FinishQARun();
            }
        }

        private IEnumerator TransitionToNextDayRoutine(int nextDay)
        {
            if (KitchenController.Instance != null)
            {
                KitchenController.Instance.TriggerStatusToast($"Day {currentDayRunning} Cleared! Menu open for 5s... Starting Day {nextDay} next!", new Color(0.2f, 0.9f, 0.4f));
            }

            // Results menu stays open for 5 seconds as requested
            yield return new WaitForSeconds(5.0f);

            // Cleanly clear and dismiss the end shift menu before starting next level
            if (KitchenHUD.Instance != null && KitchenHUD.Instance.endShiftModal != null)
            {
                KitchenHUD.Instance.endShiftModal.SetActive(false);
            }

            if (KitchenController.Instance != null)
            {
                KitchenController.Instance.ClearKitchenDaily();
            }

            currentDayRunning = nextDay;
            LaunchDay(currentDayRunning);
        }

        /// <summary>
        /// Stops a QA run early (user left mid-run). Restores speed and real save,
        /// clears QA mode and switches every bot off so the next game is played by humans.
        /// No report is saved or shown.
        /// </summary>
        public void AbortQARun()
        {
            StopAllCoroutines();
            isQARunning = false;
            Time.timeScale = 1.0f;
            ProgressionManager.Instance?.EndQASandbox();

            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.isAutoplayQAMode = false;
                ShiftManager.Instance.OnShiftEnded -= HandleShiftEnded;
            }
            BotCommunicationHub.Reset();
            KitchenHUD.Instance?.SetQAModeBanner(false, 0, 0, 1.0f);

            var kc = KitchenController.Instance;
            if (kc != null)
            {
                foreach (var p in kc.playersByPeer.Values)
                {
                    if (p == null) continue;
                    var bot = p.GetComponent<BotController>();
                    if (bot != null) bot.botActive = false;
                }
            }
        }

        /// <summary>Call before leaving or starting any normal game: ends any QA run and clears QA mode.</summary>
        public static void AbortIfRunning()
        {
            if (Instance != null && Instance.isQARunning) Instance.AbortQARun();
            if (ShiftManager.Instance != null) ShiftManager.Instance.isAutoplayQAMode = false;
        }

        public void FinishQARun()
        {
            isQARunning = false;
            Time.timeScale = 1.0f;
            ProgressionManager.Instance?.EndQASandbox(); // restore real save state

            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.isAutoplayQAMode = false;
                ShiftManager.Instance.OnShiftEnded -= HandleShiftEnded;
            }
            BotCommunicationHub.Reset();

            int totalOrders = currentReport.totalOrdersServed + currentReport.totalOrdersFailed;
            currentReport.overallSuccessRate = totalOrders > 0 ? (float)currentReport.totalOrdersServed / totalOrders : 0.0f;

            try
            {
                SaveReportsToDisk(currentReport, GetReportsDirectory());
            }
            catch (System.Exception e)
            {
                // File errors must never block the on-screen report.
                Debug.LogWarning($"[TwoBotQAManager] Could not save QA report files: {e.Message}");
            }

            // Report results back to user on-screen
            if (KitchenHUD.Instance != null)
            {
                KitchenHUD.Instance.SetQAModeBanner(false, 0, 0, 1.0f);
                KitchenHUD.Instance.ShowQAReport(currentReport);
            }

            Debug.Log("============================================================");
            Debug.Log($"  TWO-BOT QA AUTOPLAY FINISHED: {currentReport.daysCleared}/{currentReport.daysAttempted} Days Cleared");
            Debug.Log($"  Total Score: {currentReport.totalScore} | Success Rate: {currentReport.overallSuccessRate * 100f:F1}%");
            Debug.Log("============================================================");

            if (exitOnCompletion)
            {
                int exitCode = (currentReport.daysCleared == currentReport.daysAttempted && currentReport.daysCleared > 0) ? 0 : 1;
                Application.Quit(exitCode);
            }
        }

        /// <summary>
        /// Pure path resolver (no disk access). Mobile builds cannot write next to the
        /// read-only APK, so reports go to the app's persistent storage there.
        /// </summary>
        public static string ResolveReportsDirectory(bool isMobile, string dataPath, string persistentDataPath)
        {
            if (isMobile) return Path.Combine(persistentDataPath, "qa_reports");
            return Path.GetFullPath(Path.Combine(dataPath, "../../../qa_reports"));
        }

        public static string GetReportsDirectory()
        {
            string baseDir = ResolveReportsDirectory(Application.isMobilePlatform, Application.dataPath, Application.persistentDataPath);
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }
            return baseDir;
        }

        public static void SaveReportsToDisk(QARunReport report, string reportsDir)
        {
            if (!Directory.Exists(reportsDir)) Directory.CreateDirectory(reportsDir);

            // 1. JSON Report
            string jsonFile = Path.Combine(reportsDir, "latest_results.json");
            string jsonContent = JsonUtility.ToJson(report, true);
            File.WriteAllText(jsonFile, jsonContent);

            // 2. Human-Readable Text Summary
            string summaryFile = Path.Combine(reportsDir, "latest_summary.txt");
            using (var sw = new StreamWriter(summaryFile, false))
            {
                sw.WriteLine("==================================================================");
                sw.WriteLine("         TWO LADLES — AUTOMATED TWO-BOT QA AUTOPLAY REPORT        ");
                sw.WriteLine("==================================================================");
                sw.WriteLine($"Execution Timestamp:  {report.timestamp}");
                sw.WriteLine($"Random Seed:          {report.seed}");
                sw.WriteLine($"Days Attempted:       {report.daysAttempted}");
                sw.WriteLine($"Days Cleared:         {report.daysCleared} / {report.daysAttempted}");
                sw.WriteLine($"Total Score:          {report.totalScore} pts");
                sw.WriteLine($"Orders Served:        {report.totalOrdersServed}");
                sw.WriteLine($"Orders Failed:        {report.totalOrdersFailed}");
                sw.WriteLine($"Overall Order Rate:   {report.overallSuccessRate * 100f:F1}%");
                sw.WriteLine("------------------------------------------------------------------");
                sw.WriteLine("PER-DAY BREAKDOWN:");
                foreach (var d in report.dayResults)
                {
                    string status = d.cleared ? "[CLEARED]" : "[FAILED] ";
                    sw.WriteLine($"  Day {d.day}: {status} Score: {d.score,4} | Orders: {d.ordersServed,2} served, {d.ordersFailed,2} failed | Rep: {d.remainingReputation,3:F0}/100 | Time: {d.durationSeconds,4:F1}s");
                }
                sw.WriteLine("==================================================================");
            }

            // 3. Append to Run History CSV
            string csvFile = Path.Combine(reportsDir, "run_history.csv");
            bool writeHeader = !File.Exists(csvFile);
            using (var sw = new StreamWriter(csvFile, true))
            {
                if (writeHeader)
                {
                    sw.WriteLine("timestamp,seed,daysAttempted,daysCleared,totalScore,ordersServed,ordersFailed,successRate");
                }
                sw.WriteLine($"\"{report.timestamp}\",{report.seed},{report.daysAttempted},{report.daysCleared},{report.totalScore},{report.totalOrdersServed},{report.totalOrdersFailed},{report.overallSuccessRate:F3}");
            }

            Debug.Log($"[TwoBotQAManager] Reports saved cleanly to {reportsDir}");
        }

        private void OnDestroy()
        {
            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.OnShiftEnded -= HandleShiftEnded;
            }
        }
    }
}
