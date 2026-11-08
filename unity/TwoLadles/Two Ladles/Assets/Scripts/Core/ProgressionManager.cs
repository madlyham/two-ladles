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
        public float bestReputation;
        public bool completed;
    }

    [Serializable]
    public class UpgradeItem
    {
        public string id;
        public string category;
        public string displayName;
        public int cost;
        public string description;
        public int level;
        public int maxLevel = 1;
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
        public static ProgressionManager Instance { get; set; }

        public event Action OnProgressionUpdated;
        public event Action<string> OnUpgradePurchased;
        public event Action<int> OnStageUnlocked;

        // Gold is the unified progression currency (aliased with coins for test compatibility)
        public int coins = 150;
        public int gold
        {
            get => coins;
            set => coins = Mathf.Max(0, value);
        }

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
        private HashSet<string> claimedRewardTokens = new HashSet<string>();

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

        public void InitDefaults()
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, "progression.json");
            claimedRewardTokens.Clear();

            for (int i = 1; i <= 8; i++)
            {
                stageRecords[i] = new StageRecord { stars = 0, bestScore = 0, bestReputation = 0f, completed = false };
            }

            catalogUpgrades.Clear();

            catalogUpgrades["sharp_santoku_knives"] = new UpgradeItem
            {
                id = "sharp_santoku_knives",
                category = "Prep Speed",
                displayName = "Sharp Santoku Knife Set",
                cost = 100,
                description = "+15% Prep Counter speed & cadence sweet-spot.",
                level = 0,
                maxLevel = 1,
                purchased = false,
                effectType = "prep_window",
                value = 1.15f
            };

            catalogUpgrades["chef_speed_boots"] = new UpgradeItem
            {
                id = "chef_speed_boots",
                category = "Movement Speed",
                displayName = "Non-Slip Kitchen Footwear",
                cost = 100,
                description = "+18% Movement Speed for both Linh & Jiwon.",
                level = 0,
                maxLevel = 1,
                purchased = false,
                effectType = "move_speed",
                value = 1.18f
            };

            catalogUpgrades["turbo_wok_burners"] = new UpgradeItem
            {
                id = "turbo_wok_burners",
                category = "Cooking Speed",
                displayName = "High-Output Jet Burners",
                cost = 150,
                description = "+25% Faster Cooking Speed on Grill, Wok & Blancher.",
                level = 0,
                maxLevel = 1,
                purchased = false,
                effectType = "cook_speed",
                value = 1.25f
            };

            catalogUpgrades["high_pressure_sink"] = new UpgradeItem
            {
                id = "high_pressure_sink",
                category = "Interaction Speed",
                displayName = "High-Pressure Rinse Sprayer",
                cost = 150,
                description = "Cleans dirty plates in 3 scrubs instead of 4.",
                level = 0,
                maxLevel = 1,
                purchased = false,
                effectType = "wash_speed",
                value = 0.75f
            };

            catalogUpgrades["extra_plate_stack"] = new UpgradeItem
            {
                id = "extra_plate_stack",
                category = "Station Efficiency",
                displayName = "Porcelain Dish Rack Expansion",
                cost = 200,
                description = "Starts every level with 6 Clean Vessels instead of 4.",
                level = 0,
                maxLevel = 1,
                purchased = false,
                effectType = "extra_plates",
                value = 2.0f
            };

            catalogUpgrades["insulated_broth_pot"] = new UpgradeItem
            {
                id = "insulated_broth_pot",
                category = "Appliance Mastery",
                displayName = "Double-Walled Phở Stockpot",
                cost = 250,
                description = "Appliances remain in OPTIMAL window for +50% longer.",
                level = 0,
                maxLevel = 1,
                purchased = false,
                effectType = "stockpot_window",
                value = 1.50f
            };
        }

        /// <summary>
        /// Calculates the Gold Reward for completing a level based on remaining reputation.
        /// Formula: Gold Reward = floor(200 * (Remaining Reputation / 100))
        /// Failed levels (victory == false or remainingReputation <= 0) grant 0 Gold.
        /// </summary>
        public static int CalculateGoldReward(float remainingReputation, bool victory = true)
        {
            if (!victory || remainingReputation <= 0.0f)
                return 0;

            float clampedRep = Mathf.Clamp(remainingReputation, 0.0f, 100.0f);
            return Mathf.FloorToInt(200.0f * ((clampedRep / 100.0f) + 1e-6f));
        }

        /// <summary>
        /// Records completion of a wave-based level using Reputation -> Gold scaling.
        /// Prevents duplicate reward claims via claimToken.
        /// </summary>
        public (bool ok, int goldEarned, bool newlyUnlocked, bool tierUpgraded) RecordLevelWaveCompletion(
            int stageId,
            float remainingReputation,
            bool victory,
            int score,
            string claimToken = "")
        {
            if (!string.IsNullOrEmpty(claimToken))
            {
                if (claimedRewardTokens.Contains(claimToken))
                {
                    return (false, 0, false, false);
                }
                claimedRewardTokens.Add(claimToken);
            }

            if (!victory || remainingReputation <= 0.0f)
            {
                SaveProgression();
                OnProgressionUpdated?.Invoke();
                return (true, 0, false, false);
            }

            int earnedGold = CalculateGoldReward(remainingReputation, true);
            coins += earnedGold;
            totalCareerScore += score;

            if (!stageRecords.ContainsKey(stageId))
                stageRecords[stageId] = new StageRecord();

            var rec = stageRecords[stageId];
            int stars = remainingReputation >= 75f ? 3 : (remainingReputation >= 40f ? 2 : 1);
            rec.stars = Math.Max(rec.stars, stars);
            rec.bestScore = Math.Max(rec.bestScore, score);
            rec.bestReputation = Mathf.Max(rec.bestReputation, remainingReputation);
            rec.completed = true;

            bool newlyUnlocked = false;
            if (stageId >= 1 && stageId == highestUnlockedStage && highestUnlockedStage < 5)
            {
                highestUnlockedStage = stageId + 1;
                newlyUnlocked = true;
                OnStageUnlocked?.Invoke(highestUnlockedStage);
            }

            bool tierUpgraded = false;
            if (stageId == 3 && linhTier < 2)
            {
                linhTier = 2;
                jiwonTier = 2;
                tierUpgraded = true;
            }
            if (stageId == 5 && linhTier < 3)
            {
                linhTier = 3;
                jiwonTier = 3;
                tierUpgraded = true;
            }

            SaveProgression();
            OnProgressionUpdated?.Invoke();
            return (true, earnedGold, newlyUnlocked, tierUpgraded);
        }

        public (bool ok, string reason, int earnedCoins, bool newlyUnlocked, bool tierUpgraded) RecordShiftResults(
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

            bool tierUpgraded = false;
            if (stageId == 3 && stars >= 1 && linhTier < 2)
            {
                linhTier = 2;
                jiwonTier = 2;
                tierUpgraded = true;
            }
            if (stageId == 5 && stars >= 1 && linhTier < 3)
            {
                linhTier = 3;
                jiwonTier = 3;
                tierUpgraded = true;
            }

            SaveProgression();
            OnProgressionUpdated?.Invoke();

            return (true, "", earnedCoins, newlyUnlocked, tierUpgraded);
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
            if (up.purchased || up.level >= up.maxLevel)
                return (false, "Already owned.");

            if (coins < up.cost)
                return (false, $"Not enough gold ({coins}/{up.cost}).");

            coins -= up.cost;
            up.level++;
            up.purchased = (up.level >= up.maxLevel);

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

        public bool IsLevelUnlocked(int stageId)
        {
            if (stageId <= 1) return true;
            if (stageId <= highestUnlockedStage) return true;
            if (stageRecords.TryGetValue(stageId - 1, out var prevRec) && prevRec.completed) return true;
            return false;
        }

        public bool IsLevelCompleted(int stageId)
        {
            return stageRecords.TryGetValue(stageId, out var rec) && rec.completed;
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

                foreach (var up in catalogUpgrades.Values)
                {
                    up.purchased = false;
                    up.level = 0;
                }

                if (data.purchasedUpgrades != null)
                {
                    foreach (var uid in data.purchasedUpgrades)
                    {
                        if (catalogUpgrades.ContainsKey(uid))
                        {
                            catalogUpgrades[uid].purchased = true;
                            catalogUpgrades[uid].level = catalogUpgrades[uid].maxLevel;
                        }
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
            claimedRewardTokens.Clear();

            foreach (var k in new List<int>(stageRecords.Keys))
            {
                stageRecords[k] = new StageRecord { stars = 0, bestScore = 0, bestReputation = 0f, completed = false };
            }
            foreach (var k in new List<string>(catalogUpgrades.Keys))
            {
                catalogUpgrades[k].purchased = false;
                catalogUpgrades[k].level = 0;
            }

            SaveProgression();
            OnProgressionUpdated?.Invoke();
        }
    }
}
