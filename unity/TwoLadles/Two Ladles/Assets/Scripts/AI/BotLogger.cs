using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TwoLadles.AI
{
    /// <summary>
    /// High-resolution diagnostic logger for Autonomous Bot TAS execution.
    /// Writes real-time telemetry to:
    /// 1. Disk log file at Logs/bot_qa.log (instant auto-flush).
    /// 2. Unity Console via Debug.Log.
    /// 3. In-memory circular buffer for on-screen BotDebugHUD overlay.
    /// </summary>
    public static class BotLogger
    {
        private static readonly object _fileLock = new object();
        private static string _logFilePath = null;
        private static List<string> _recentLogs = new List<string>();
        private const int MAX_RECENT_LOGS = 25;

        public static string LogFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_logFilePath))
                {
                    try
                    {
                        string logsDir = Path.Combine(Application.dataPath, "../Logs");
                        if (!Directory.Exists(logsDir))
                        {
                            Directory.CreateDirectory(logsDir);
                        }
                        _logFilePath = Path.Combine(logsDir, "bot_qa.log");
                    }
                    catch
                    {
                        _logFilePath = Path.Combine(Application.persistentDataPath, "bot_qa.log");
                    }
                }
                return _logFilePath;
            }
        }

        public static void ClearLogs()
        {
            lock (_fileLock)
            {
                _recentLogs.Clear();
                try
                {
                    string path = LogFilePath;
                    if (File.Exists(path))
                    {
                        File.WriteAllText(path, $"=== TWO LADLES BOT SESSION STARTED: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
                    }
                }
                catch { }
            }
        }

        public static void Log(string characterId, string message)
        {
            string timeStr = DateTime.Now.ToString("HH:mm:ss.fff");
            string charTag = string.IsNullOrEmpty(characterId) ? "SYS" : characterId.ToUpper();
            string line = $"[{timeStr}][BOT-{charTag}] {message}";

            lock (_fileLock)
            {
                _recentLogs.Add(line);
                if (_recentLogs.Count > MAX_RECENT_LOGS)
                {
                    _recentLogs.RemoveAt(0);
                }

                try
                {
                    File.AppendAllText(LogFilePath, line + Environment.NewLine);
                }
                catch { }
            }

            Debug.Log(line);
        }

        public static void LogGoal(string characterId, string goalName, string stationId)
        {
            Log(characterId, $"GOAL: '{goalName}' => Target: {(string.IsNullOrEmpty(stationId) ? "none" : stationId)}");
        }

        public static void LogAction(string characterId, string actionName, string stationId, bool ok, string reason)
        {
            string status = ok ? "SUCCESS" : "FAIL";
            string msg = $"ACTION: {actionName} on [{stationId}] => {status} ({reason})";
            if (!ok)
            {
                string timeStr = DateTime.Now.ToString("HH:mm:ss.fff");
                string charTag = string.IsNullOrEmpty(characterId) ? "SYS" : characterId.ToUpper();
                string line = $"[{timeStr}][BOT-{charTag}] [WARNING] {msg}";

                lock (_fileLock)
                {
                    _recentLogs.Add(line);
                    if (_recentLogs.Count > MAX_RECENT_LOGS) _recentLogs.RemoveAt(0);
                    try { File.AppendAllText(LogFilePath, line + Environment.NewLine); } catch { }
                }
                Debug.LogWarning(line);
            }
            else
            {
                Log(characterId, msg);
            }
        }

        public static void LogStuck(string characterId, string reason, Vector3 position)
        {
            string msg = $"STUCK WATCHDOG TRIGGERED: {reason} at pos ({position.x:F2}, {position.z:F2})";
            string timeStr = DateTime.Now.ToString("HH:mm:ss.fff");
            string charTag = string.IsNullOrEmpty(characterId) ? "SYS" : characterId.ToUpper();
            string line = $"[{timeStr}][BOT-{charTag}] [STUCK] {msg}";

            lock (_fileLock)
            {
                _recentLogs.Add(line);
                if (_recentLogs.Count > MAX_RECENT_LOGS) _recentLogs.RemoveAt(0);
                try { File.AppendAllText(LogFilePath, line + Environment.NewLine); } catch { }
            }
            Debug.LogWarning(line);
        }

        public static List<string> GetRecentLogs()
        {
            lock (_fileLock)
            {
                return new List<string>(_recentLogs);
            }
        }
    }
}
