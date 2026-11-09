// SaveSystem.cs – lightweight JSON persistence for player gold and progress
// Located in Assets/Scripts/IO/
using System;
using System.IO;
using UnityEngine;
using TwoLadles.Core;

namespace TwoLadles.IO
{
    [Serializable]
    public class SaveData
    {
        public int gold = 0;
        public int campaignStage = 1; // current day/level
        public int linhTier = 1;
        public int jiwonTier = 1;
        public System.Collections.Generic.List<string> purchasedUpgrades = new System.Collections.Generic.List<string>();
        // add any other persistent fields you need
    }

    public static class SaveSystem
    {
        private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "twoladles_save.json");

        public static void Save(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] Game saved to {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
            }
        }

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    Debug.Log("[SaveSystem] No save file found – returning default data.");
                    return new SaveData();
                }

                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveSystem] Game loaded from {SavePath}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load: {e.Message}");
                return new SaveData();
            }
        }
    }
}
