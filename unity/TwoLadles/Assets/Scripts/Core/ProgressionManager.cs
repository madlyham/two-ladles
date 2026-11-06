using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace TwoLadles.Core
{
    [Serializable]
    public class StageRecord
    {
        public int stars;
        public int bestScore;
        public bool completed;
    }

    [Serializable]
    public class UpgradeItem
    {
        public string id;
        public string displayName;
        public int cost;
        public string description;
        public bool purchased;
        public string effectType;
        public float value;
    }

    [Serializable]
    public class ProgressionData
    {
        public int coins = 150;
        public int totalCareerScore = 0;
        public int highestUnlockedStage = 1;
        public int selectedStage = 1;
        public int linhTier = 1;
        public int jiwonTier = 1;
        public int endlessBestScore = 0;
        public int endlessBestOrders = 0;
        public float endlessBestDuration = 0.0f;
        public List<string> purchasedUpgrades = new List<string>();
        public List<int> stageKeys = new List<int>();
        public List<StageRecord> stageValues = new List<StageRecord>();
    }

    public class ProgressionManager : MonoBehaviour
    {
        public static ProgressionManager Instance { get; private set; }

        public event Action OnProgressionUpdated;
        public event Action<string> OnUpgradePurchased;
        public event Action<int> OnStageUnlocked;

        public int coins = 150;
        public int totalCareerScore = 0;
        public int highestUnlockedStage = 1;
        public int selectedStage = 1;
        public int linhTier = 1;
        public int jiwonTier = 1;

        public int endlessBestScore = 0;
        public int endlessBestOrders = 0;
        public float endlessBestDuration = 0.0f;

        public Dictionary<int, StageRecord> stageRecords = new Dictionary<int, StageRecord>();
        public Dictionary<string, UpgradeItem> catalogUpgrades = new Dictionary<string, UpgradeItem>();

        private string saveFilePath;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitDefaults();
                LoadProgression();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitDefaults()
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, "progression.json");

            for (int i = 1; i <= 8; i++)
            {
                stageRecords[i] = new StageRecord { stars = 0, bestScore = 0, completed = false };
            }

            catalogUpgrades["sharp_santoku_knives"] = new UpgradeItem
            {
                id = "sharp_santoku_knives",
                displayName = "Sharp Santoku Knife Set",
                cost = 100,
                description = "Widens Prep Counter cadence sweet-spot by +15% for both chefs.",
                purchased = false,
                effectType = "prep_window",
                value = 1.15f
            };

            catalogUpgrades["insulated_broth_pot"] = new UpgradeItem
            {
                id = "insulated_broth_pot",
                displayName = "Double-Walled Phở Stockpot",
                cost = 250,
                description = "Stockpot remains in OPTIMAL window for +50% longer duration.",
                purchased = false,
                effectType = "stockpot_window",
                value = 1.50f
            };
        }

        public (bool ok, string reason, int earnedCoins, bool newlyUnlocked) RecordShiftResults(
            int stageId, int score, int stars, int handoffs, int rushTriggers)
        {
            int earnedCoins = (int)Math.Floor(score / 8.0f) + (handoffs * 5) + (rushTriggers * 20);
            coins += earnedCoins;
            totalCareerScore += score;

            if (!stageRecords.ContainsKey(stageId))
                stageRecords[stageId] = new StageRecord();

            var rec = stageRecords[stageId];
            rec.stars = Math.Max(rec.stars, stars);
            rec.bestScore = Math.Max(rec.bestScore, score);
            rec.completed = true;

            bool newlyUnlocked = false;
            if (stars >= 2 && stageId == highestUnlockedStage && highestUnlockedStage < 8)
            {
                highestUnlockedStage++;
                newlyUnlocked = true;
                OnStageUnlocked?.Invoke(highestUnlockedStage);
            }

            // Milestone 3 Character Tier Unlocks based on campaign progress:
            if (stageId == 3 && stars >= 1 && linhTier < 2)
            {
                linhTier = 2;
                jiwonTier = 2;
            }
            if (stageId == 5 && stars >= 1 && linhTier < 3)
            {
                linhTier = 3;
                jiwonTier = 3;
            }

            SaveProgression();
            OnProgressionUpdated?.Invoke();

            return (true, "", earnedCoins, newlyUnlocked);
        }

        public bool RecordEndlessRun(int score, int orders, float duration)
        {
            bool isNewRecord = false;
            if (score > endlessBestScore)
            {
                endlessBestScore = score;
                isNewRecord = true;
            }
            if (orders > endlessBestOrders)
            {
                endlessBestOrders = orders;
            }
            if (duration > endlessBestDuration)
            {
                endlessBestDuration = duration;
            }

            SaveProgression();
            OnProgressionUpdated?.Invoke();
            return isNewRecord;
        }

        public (bool ok, string reason) PurchaseUpgrade(string upgradeId)
        {
            if (!catalogUpgrades.ContainsKey(upgradeId))
                return (false, "Upgrade not found.");

            var up = catalogUpgrades[upgradeId];
            if (up.purchased)
                return (false, "Already owned.");

            if (coins < up.cost)
                return (false, $"Not enough coins ({coins}/{up.cost}).");

            coins -= up.cost;
            up.purchased = true;

            SaveProgression();
            OnUpgradePurchased?.Invoke(upgradeId);
            OnProgressionUpdated?.Invoke();

            return (true, "");
        }

        public bool IsUpgradeOwned(string upgradeId)
        {
            return catalogUpgrades.TryGetValue(upgradeId, out var up) && up.purchased;
        }

        public float GetUpgradeValue(string effectType, float defaultValue = 1.0f)
        {
            foreach (var up in catalogUpgrades.Values)
            {
                if (up.purchased && up.effectType == effectType)
                    return up.value;
            }
            return defaultValue;
        }

        public bool SaveProgression()
        {
            try
            {
                var data = new ProgressionData
                {
                    coins = coins,
                    totalCareerScore = totalCareerScore,
                    highestUnlockedStage = highestUnlockedStage,
                    selectedStage = selectedStage,
                    linhTier = linhTier,
                    jiwonTier = jiwonTier,
                    endlessBestScore = endlessBestScore,
                    endlessBestOrders = endlessBestOrders,
                    endlessBestDuration = endlessBestDuration
                };

                foreach (var kvp in catalogUpgrades)
                {
                    if (kvp.Value.purchased)
                        data.purchasedUpgrades.Add(kvp.Key);
                }

                foreach (var kvp in stageRecords)
                {
                    data.stageKeys.Add(kvp.Key);
                    data.stageValues.Add(kvp.Value);
                }

                string dir = Path.GetDirectoryName(saveFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(saveFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProgressionManager] Save failed: {ex.Message}");
                return false;
            }
        }

        public bool LoadProgression()
        {
            if (!File.Exists(saveFilePath)) return false;

            try
            {
                string json = File.ReadAllText(saveFilePath);
                var data = JsonUtility.FromJson<ProgressionData>(json);
                if (data == null) return false;

                coins = data.coins;
                totalCareerScore = data.totalCareerScore;
                highestUnlockedStage = data.highestUnlockedStage;
                selectedStage = data.selectedStage;
                linhTier = data.linhTier;
                jiwonTier = data.jiwonTier;
                endlessBestScore = data.endlessBestScore;
                endlessBestOrders = data.endlessBestOrders;
                endlessBestDuration = data.endlessBestDuration;

                if (data.purchasedUpgrades != null)
                {
                    foreach (var uid in data.purchasedUpgrades)
                    {
                        if (catalogUpgrades.ContainsKey(uid))
                            catalogUpgrades[uid].purchased = true;
                    }
                }

                if (data.stageKeys != null && data.stageValues != null)
                {
                    for (int i = 0; i < Math.Min(data.stageKeys.Count, data.stageValues.Count); i++)
                    {
                        stageRecords[data.stageKeys[i]] = data.stageValues[i];
                    }
                }

                OnProgressionUpdated?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProgressionManager] Load failed: {ex.Message}");
                return false;
            }
        }

        public void ResetProgressionForTesting()
        {
            coins = 150;
            totalCareerScore = 0;
            highestUnlockedStage = 1;
            selectedStage = 1;
            linhTier = 1;
            jiwonTier = 1;
            endlessBestScore = 0;
            endlessBestOrders = 0;
            endlessBestDuration = 0.0f;

            foreach (var k in new List<int>(stageRecords.Keys))
            {
                stageRecords[k] = new StageRecord { stars = 0, bestScore = 0, completed = false };
            }
            foreach (var k in new List<string>(catalogUpgrades.Keys))
            {
                catalogUpgrades[k].purchased = false;
            }

            SaveProgression();
            OnProgressionUpdated?.Invoke();
        }
    }
}
