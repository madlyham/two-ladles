using System;
using System.IO;
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
                ShiftManager.Instance.OnShiftEnded += HandleShiftEnded;
            }

            currentDayRunning = startDay;
            LaunchDay(currentDayRunning);
        }

        public void LaunchDay(int dayId)
        {
            Debug.Log($"[TwoBotQAManager] Launching Day {dayId} in Automated Two-Bot Mode...");

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

            int day = currentDayRunning;
            bool victory = results != null && results.TryGetValue("victory", out var v) && (bool)v;
            int score = results != null && results.TryGetValue("score", out var s) ? Convert.ToInt32(s) : 0;
            int ordersServed = results != null && results.TryGetValue("ordersServed", out var os) ? Convert.ToInt32(os) : 0;
            int ordersFailed = results != null && results.TryGetValue("ordersFailed", out var of) ? Convert.ToInt32(of) : 0;
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

            if (currentDayRunning < targetDay)
            {
                currentDayRunning++;
                LaunchDay(currentDayRunning);
            }
            else
            {
                FinishQARun();
            }
        }

        public void FinishQARun()
        {
            isQARunning = false;
            Time.timeScale = 1.0f;

            if (ShiftManager.Instance != null)
            {
                ShiftManager.Instance.OnShiftEnded -= HandleShiftEnded;
            }

            int totalOrders = currentReport.totalOrdersServed + currentReport.totalOrdersFailed;
            currentReport.overallSuccessRate = totalOrders > 0 ? (float)currentReport.totalOrdersServed / totalOrders : 0.0f;

            SaveReportsToDisk(currentReport, GetReportsDirectory());

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

        public static string GetReportsDirectory()
        {
            string baseDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../qa_reports"));
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
