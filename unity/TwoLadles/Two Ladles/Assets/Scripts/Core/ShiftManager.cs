using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Kitchen;

namespace TwoLadles.Core
{
    public enum GameMode
    {
        CAMPAIGN = 0,
        ENDLESS_RUSH = 1
    }

    public enum BotDifficulty
    {
        Assistive = 0,
        Standard = 1,
        Challenge = 2
    }

    [Serializable]
    public class OrderTicket
    {
        public string ticketId;
        public string recipeId;
        public string targetVessel;
        public float timeRemaining;
        public float initialPatience;
        public bool isRushTicket;
        public bool handoffBonusClaimed;
        public bool isCompleted;
        public bool isFailed;

        // Multi-item order support
        public List<string> requiredItems = new List<string>();
        public List<int> fulfilledIndices = new List<int>();

        public OrderTicket(string id, string recipe, string vessel, float patience, bool rush = false)
        {
            ticketId = id;
            recipeId = recipe;
            targetVessel = vessel;
            timeRemaining = patience;
            initialPatience = patience;
            isRushTicket = rush;
            handoffBonusClaimed = false;
            isCompleted = false;
            isFailed = false;
            requiredItems = new List<string> { recipe };
            fulfilledIndices = new List<int>();
        }

        public OrderTicket(string id, List<string> recipes, string vessel, float patience, bool rush = false)
        {
            ticketId = id;
            requiredItems = (recipes != null && recipes.Count > 0) ? new List<string>(recipes) : new List<string> { "tteokbokki" };
            recipeId = requiredItems[0];
            targetVessel = vessel;
            timeRemaining = patience;
            initialPatience = patience;
            isRushTicket = rush;
            handoffBonusClaimed = false;
            isCompleted = false;
            isFailed = false;
            fulfilledIndices = new List<int>();
        }

        public bool NeedsDish(string dishId)
        {
            if (isCompleted || isFailed) return false;
            for (int i = 0; i < requiredItems.Count; i++)
            {
                if (requiredItems[i] == dishId && !fulfilledIndices.Contains(i))
                    return true;
            }
            return false;
        }

        public int GetUnfulfilledItemIndex(string dishId)
        {
            if (isCompleted || isFailed) return -1;
            for (int i = 0; i < requiredItems.Count; i++)
            {
                if (requiredItems[i] == dishId && !fulfilledIndices.Contains(i))
                    return i;
            }
            return -1;
        }

        public void MarkItemFulfilled(int index)
        {
            if (!fulfilledIndices.Contains(index))
            {
                fulfilledIndices.Add(index);
            }
            recipeId = GetNextUnfulfilledRecipeId();
            if (IsFullyFulfilled())
            {
                isCompleted = true;
            }
        }

        public string GetNextUnfulfilledRecipeId()
        {
            for (int i = 0; i < requiredItems.Count; i++)
            {
                if (!fulfilledIndices.Contains(i)) return requiredItems[i];
            }
            return requiredItems.Count > 0 ? requiredItems[0] : "";
        }

        public bool IsFullyFulfilled()
        {
            return fulfilledIndices.Count >= requiredItems.Count;
        }

        public string GetFormattedDisplayLabel()
        {
            if (requiredItems == null || requiredItems.Count <= 1)
            {
                return GetDishDisplayName(recipeId);
            }

            var dict = new Dictionary<string, int>();
            for (int i = 0; i < requiredItems.Count; i++)
            {
                if (fulfilledIndices.Contains(i)) continue;
                string item = requiredItems[i];
                if (!dict.ContainsKey(item)) dict[item] = 0;
                dict[item]++;
            }

            if (dict.Count == 0)
            {
                return "Complete!";
            }

            List<string> parts = new List<string>();
            foreach (var kvp in dict)
            {
                string dishName = GetDishDisplayName(kvp.Key);
                if (kvp.Value > 1) parts.Add($"{kvp.Value}x {dishName}");
                else parts.Add(dishName);
            }

            string label = string.Join(" + ", parts);
            if (fulfilledIndices.Count > 0)
            {
                label = $"({fulfilledIndices.Count}/{requiredItems.Count}) " + label;
            }
            return label;
        }

        public static string GetDishDisplayName(string id)
        {
            if (LocalizationManager.Instance != null)
            {
                string loc = LocalizationManager.Instance.Get("dish." + id + ".name", null);
                if (!string.IsNullOrEmpty(loc)) return loc;
            }
            if (GameData.Instance != null)
            {
                var spec = GameData.Instance.GetRecipeSpec(id);
                if (spec != null) return spec.displayName;
            }
            return id;
        }
    }

    public class ShiftManager : MonoBehaviour
    {
        public static ShiftManager Instance { get; set; }

        public const float MAX_FLOW = 100.0f;
        public const float MAX_REPUTATION = 100.0f;
        public const float REPUTATION_LOSS_PER_FAILURE = 25.0f;
        public const float HANDOFF_FLOW_BONUS = 15.0f;
        public const float RUSH_RHYTHM_DURATION = 15.0f;
        public const float RUSH_RHYTHM_MULTIPLIER = 1.5f;
        public const int HANDOFF_WINDOW_MS = 4000;
        public const int INITIAL_CLEAN_PLATES = 4;
        public const float DEFAULT_SHIFT_DURATION = 180.0f;
        public const int MAX_ACTIVE_ORDERS = 4;

        [Header("Single Player & AI Partner Mode")]
        public bool isSoloMode = false;
        public int humanChefPeerId = 1;
        public BotDifficulty botDifficulty = BotDifficulty.Standard;
        public bool isAutoplayQAMode = false;

        public event Action<float, bool, float> OnFlowUpdated;
        public event Action<bool> OnRushRhythmChanged;
        public event Action<List<OrderTicket>> OnOrderQueueUpdated;
        public event Action<string, Color> OnFeedbackBanner;
        public event Action<Dictionary<string, object>> OnShiftEnded;
        public event Action<int, string, string> OnTutorialStepChanged;
        public event Action<float, float> OnReputationChanged;
        public event Action<int, int, int, int> OnWaveUpdated;

        public bool shiftActive = false;
        public GameMode currentMode = GameMode.CAMPAIGN;
        public int currentStageId = 1;
        public bool isTutorial => currentStageId == 0;
        public int tutorialStep = 1;
        public string tutorialStepTitle = "";
        public string tutorialStepInstruction = "";
        public float shiftTimeRemaining = 180.0f;
        public float shiftElapsedSeconds = 0.0f;

        // Reputation System (Player Health: 0 to 100)
        public float reputation = MAX_REPUTATION;
        public bool levelVictory = false;
        public int lastGoldEarned = 0;
        private string currentShiftSessionToken = "";

        // Wave Progression System
        public List<WaveSpec> currentLevelWaves = new List<WaveSpec>();
        public int currentWaveIndex = 0;
        public int currentWaveNumber => currentWaveIndex + 1;
        public int totalWaves => currentLevelWaves != null && currentLevelWaves.Count > 0 ? currentLevelWaves.Count : 1;
        public int waveOrdersRequired = 2;
        public int waveOrdersSpawned = 0;
        public int waveOrdersCompleted = 0;
        public int wavesClearedCount = 0;
        public bool isWaveTransitioning = false;
        public float waveTransitionTimer = 0.0f;

        public int totalScore = 0;
        public int ordersServed = 0;
        public int ordersFailed = 0;
        public int handoffsCompleted = 0;
        public int rushRhythmsTriggered = 0;

        public int dynamicMaxOrders = MAX_ACTIVE_ORDERS;
        public bool isWaitingForRecipeBook = false;
        public float orderSpawnInterval = 14.0f;
        public float currentWavePatience = 65.0f;

        public float kitchenFlow = 0.0f;
        public bool rushRhythmActive = false;
        public float rushRhythmRemaining = 0.0f;

        public int cleanPlates = 4;
        public int dirtyPlatesAtReturn = 0;

        public List<OrderTicket> activeOrders = new List<OrderTicket>();
        public HashSet<string> claimedHandoffOrders = new HashSet<string>();

        private float orderSpawnTimer = 0.0f;
        private int ticketCounter = 0;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!shiftActive) return;
            TickShift(Time.deltaTime);
        }

        public void ApplyReputationDamage(float amount, string reason = "Order Failed!")
        {
            if (!shiftActive || isTutorial) return;
            if (amount <= 0.0f) return;

            float prev = reputation;
            reputation = Mathf.Clamp(reputation - amount, 0.0f, MAX_REPUTATION);
            OnReputationChanged?.Invoke(reputation, prev);

            if (!string.IsNullOrEmpty(reason))
            {
                OnFeedbackBanner?.Invoke($"{reason} (-{amount:F0} Rep)", new Color(0.95f, 0.25f, 0.2f));
            }

            if (reputation <= 0.0f)
            {
                reputation = 0.0f;
                EndShift("Defeat! Reputation reached 0.", false);
            }
        }

        public void TickShift(float dt)
        {
            if (!shiftActive || isWaitingForRecipeBook) return;

            shiftElapsedSeconds += dt;

            // Tick Rush Rhythm
            if (rushRhythmActive)
            {
                rushRhythmRemaining -= dt;
                if (rushRhythmRemaining <= 0.0f)
                {
                    rushRhythmActive = false;
                    rushRhythmRemaining = 0.0f;
                    OnRushRhythmChanged?.Invoke(false);
                }
                OnFlowUpdated?.Invoke(kitchenFlow, rushRhythmActive, rushRhythmRemaining);
            }

            if (currentMode == GameMode.ENDLESS_RUSH)
            {
                orderSpawnInterval = Mathf.Max(6.5f, 14.0f - (shiftElapsedSeconds / 30.0f));
                if (shiftElapsedSeconds > 120.0f)
                    dynamicMaxOrders = 5;
                else if (shiftElapsedSeconds > 60.0f)
                    dynamicMaxOrders = 4;
                else if (shiftElapsedSeconds > 30.0f)
                    dynamicMaxOrders = 3;

                if (ordersFailed >= 3)
                {
                    reputation = 0.0f;
                    OnReputationChanged?.Invoke(0.0f, 0.0f);
                    EndShift("Shift Over! 3 Orders Missed.", false);
                    return;
                }
            }
            else if (currentMode == GameMode.CAMPAIGN && !isTutorial)
            {
                shiftTimeRemaining = Mathf.Max(0.0f, shiftTimeRemaining - dt);

                // Day 5 is a 3-Minute (180s) Bulgogi Bánh Mì High-Score Finale!
                if (currentStageId == 5 && shiftTimeRemaining <= 0.0f)
                {
                    EndShift("3-Minute Bulgogi Bánh Mì Rush Complete!", true);
                    return;
                }

                if (isWaveTransitioning)
                {
                    waveTransitionTimer -= dt;
                    if (waveTransitionTimer <= 0.0f)
                    {
                        isWaveTransitioning = false;
                        StartWave(currentWaveIndex + 1);
                    }
                    return;
                }
            }

            // Tutorial Step 1 progress check
            if (isTutorial && tutorialStep == 1 && shiftElapsedSeconds > 2.0f)
            {
                AdvanceTutorialStep(2);
            }

            // LAN client: host owns order timers, expiry and spawning (synced via ORDERS packets)
            if (isNetworkClient) return;

            // Tick active orders patience (frozen during tutorial so players can learn without stress)
            if (!isTutorial)
            {
                for (int i = activeOrders.Count - 1; i >= 0; i--)
                {
                    var ord = activeOrders[i];
                    ord.timeRemaining -= dt;
                    if (ord.timeRemaining <= 0.0f)
                    {
                        ord.isFailed = true;
                        ordersFailed++;
                        activeOrders.RemoveAt(i);
                        AddKitchenFlow(-15.0f);
                        OnOrderQueueUpdated?.Invoke(activeOrders);

                        if (currentMode == GameMode.ENDLESS_RUSH && ordersFailed >= 3)
                        {
                            ApplyReputationDamage(100.0f, "3 Orders Missed!");
                            if (shiftActive) EndShift("Shift Over! 3 Orders Missed.", false);
                            return;
                        }

                        // Deduct Reputation health on expired customer timer
                        ApplyReputationDamage(REPUTATION_LOSS_PER_FAILURE, "Order Expired!");
                        if (!shiftActive) return;

                        // If a wave order expired, allow spawning replacement so player can still complete required wave orders if reputation > 0
                        if (currentMode == GameMode.CAMPAIGN && waveOrdersSpawned > waveOrdersCompleted + activeOrders.Count)
                        {
                            waveOrdersSpawned = waveOrdersCompleted + activeOrders.Count;
                        }
                    }
                }
            }

            // Spawn incoming orders
            orderSpawnTimer -= dt;
            bool canSpawnMoreInWave = (currentMode != GameMode.CAMPAIGN) || isTutorial || (waveOrdersSpawned < waveOrdersRequired);
            if (orderSpawnTimer <= 0.0f && activeOrders.Count < dynamicMaxOrders && canSpawnMoreInWave)
            {
                TrySpawnNextOrder();
            }
        }

        private void StartWave(int waveIdx)
        {
            if (currentLevelWaves == null || currentLevelWaves.Count == 0)
                return;

            currentWaveIndex = Mathf.Clamp(waveIdx, 0, currentLevelWaves.Count - 1);
            var wave = currentLevelWaves[currentWaveIndex];
            waveOrdersRequired = Mathf.Max(1, wave.totalOrders);
            waveOrdersSpawned = 0;
            waveOrdersCompleted = 0;
            dynamicMaxOrders = Mathf.Max(1, wave.maxConcurrentOrders);
            orderSpawnInterval = Mathf.Max(2.0f, wave.orderSpawnInterval);
            currentWavePatience = Mathf.Max(20.0f, wave.customerPatienceSeconds);
            orderSpawnTimer = 0.5f;

            OnWaveUpdated?.Invoke(currentWaveNumber, totalWaves, waveOrdersCompleted, waveOrdersRequired);
            if (!isTutorial && currentWaveNumber > 1)
            {
                OnFeedbackBanner?.Invoke($"WAVE {currentWaveNumber} / {totalWaves}", new Color(0.35f, 0.9f, 1.0f));
            }
        }

        private void CheckWaveProgression()
        {
            if (currentMode != GameMode.CAMPAIGN || isTutorial) return;

            OnWaveUpdated?.Invoke(currentWaveNumber, totalWaves, waveOrdersCompleted, waveOrdersRequired);

            if (waveOrdersCompleted >= waveOrdersRequired)
            {
                wavesClearedCount = currentWaveNumber;
                if (currentWaveIndex + 1 < totalWaves)
                {
                    isWaveTransitioning = true;
                    waveTransitionTimer = 1.6f;
                    activeOrders.Clear();
                    OnOrderQueueUpdated?.Invoke(activeOrders);
                    OnFeedbackBanner?.Invoke($"Wave {currentWaveNumber} Complete!", new Color(0.3f, 1.0f, 0.5f));
                }
                else
                {
                    // Final wave completed -> Victory!
                    EndShift("Victory! All waves completed!", true);
                }
            }
        }

        public void AdvanceTutorialStep(int step)
        {
            if (!isTutorial) return;
            if (step <= tutorialStep && step != 1) return;
            tutorialStep = step;

            var lm = LocalizationManager.Instance;
            switch (tutorialStep)
            {
                case 1:
                    tutorialStepTitle = lm != null ? lm.Get("tutorial.step1_title") : "STEP 1: MOVE TO FRIDGE";
                    tutorialStepInstruction = lm != null ? lm.Get("tutorial.step1_desc") : "Use [WASD] or Joystick to walk up to the Fridge.";
                    break;
                case 2:
                    tutorialStepTitle = lm != null ? lm.Get("tutorial.step2_title") : "STEP 2: GET RICE CAKE PORTION";
                    tutorialStepInstruction = lm != null ? lm.Get("tutorial.step2_desc") : "Walk to Fridge -> Press [E] (INTERACT) -> Select 'Rice Cake Portion'.";
                    break;
                case 3:
                    tutorialStepTitle = lm != null ? lm.Get("tutorial.step3_title") : "STEP 3: GET GOCHUJANG";
                    tutorialStepInstruction = lm != null ? lm.Get("tutorial.step3_desc") : "Place Rice Cake in Stove/Counter [E], then grab 'Gochujang' from Shelf [E].";
                    break;
                case 4:
                    tutorialStepTitle = lm != null ? lm.Get("tutorial.step4_title") : "STEP 4: COOK TTEOKBOKKI";
                    tutorialStepInstruction = lm != null ? lm.Get("tutorial.step4_desc") : "Press [E] with Gochujang at the Stove to combine & cook Tteokbokki!";
                    break;
                case 5:
                    tutorialStepTitle = lm != null ? lm.Get("tutorial.step5_title") : "STEP 5: PICK UP AT GREEN BAR";
                    tutorialStepInstruction = lm != null ? lm.Get("tutorial.step5_desc") : "Wait for the Stove bar to turn GREEN, then press [E] to pick up!";
                    break;
                case 6:
                case 7:
                case 8:
                case 9:
                    tutorialStepTitle = lm != null ? lm.Get("tutorial.step6_title") : "STEP 6: BOWL IT & SEND IT OUT!";
                    tutorialStepInstruction = lm != null ? lm.Get("tutorial.step6_desc") : "Put Tteokbokki in a BOWL (bowls are unlimited - no washing) then press [E] at the Send-Out Window by the north door. Tip: hand food over on the Pass Counter for +15 Flow or throw it with [Q] / right-click.";
                    break;
                case 10:
                    tutorialStepTitle = lm != null ? lm.Get("tutorial.complete_title") : "★ COMPLETE! ★";
                    tutorialStepInstruction = lm != null ? lm.Get("tutorial.complete_desc") : "You're ready! Park items on holding counters. Once Jiwon reaches Tier 2 press [H] to hire a Part-Timer (costs 20 score). Open the pause menu (⏸) and leave to start Day 1: Tteokbokki!";
                    break;
            }

            OnTutorialStepChanged?.Invoke(tutorialStep, tutorialStepTitle, tutorialStepInstruction);
        }

        public void StartShift(int stageId, GameMode mode = GameMode.CAMPAIGN)
        {
            currentStageId = stageId;
            currentMode = mode;
            shiftActive = true;
            isNetworkClient = false; // LAN client re-sets this right after StartShift
            shiftElapsedSeconds = 0.0f;
            reputation = MAX_REPUTATION;
            levelVictory = false;
            lastGoldEarned = 0;
            currentShiftSessionToken = $"shift_{stageId}_{DateTime.UtcNow.Ticks}_{UnityEngine.Random.Range(1000, 9999)}";

            if (KitchenController.Instance != null)
            {
                KitchenController.Instance.ClearKitchenDaily();
                if (isAutoplayQAMode)
                {
                    KitchenController.Instance.ConfigureTwoBotsQA();
                }
                else
                {
                    KitchenController.Instance.ConfigureSoloAndAIMode(isSoloMode, humanChefPeerId, botDifficulty);
                }
            }

            totalScore = 0;
            ordersServed = 0;
            ordersFailed = 0;
            handoffsCompleted = 0;
            rushRhythmsTriggered = 0;
            kitchenFlow = 0.0f;
            rushRhythmActive = false;
            rushRhythmRemaining = 0.0f;

            int extraPlates = 0;
            if (ProgressionManager.Instance != null && ProgressionManager.Instance.IsUpgradeOwned("extra_plate_stack"))
            {
                extraPlates = 2;
            }
            cleanPlates = INITIAL_CLEAN_PLATES + extraPlates;
            dirtyPlatesAtReturn = 0;

            activeOrders.Clear();
            claimedHandoffOrders.Clear();
            ticketCounter = 0;
            wavesClearedCount = 0;
            isWaveTransitioning = false;
            waveTransitionTimer = 0.0f;

            float duration = DEFAULT_SHIFT_DURATION;
            if (isTutorial)
            {
                var spec = GameData.Instance != null ? GameData.Instance.tutorialLevel : null;
                duration = spec != null ? spec.shiftDuration : 600.0f;
                orderSpawnInterval = spec != null ? spec.orderSpawnInterval : 45.0f;
                dynamicMaxOrders = 1;
                currentLevelWaves = new List<WaveSpec> { new WaveSpec(1, 1, 60f, 600f, 1, new List<string> { "tteokbokki" }) };
                StartWave(0);

                // Starter Day 1 Tteokbokki order
                ticketCounter++;
                waveOrdersSpawned = 1;
                var ticket = new OrderTicket($"ticket_{ticketCounter}", "tteokbokki", "none", 600.0f);
                activeOrders.Add(ticket);
                OnOrderQueueUpdated?.Invoke(activeOrders);
            }
            else if (currentMode == GameMode.CAMPAIGN && GameData.Instance != null && GameData.Instance.campaignLevels.TryGetValue(stageId, out var spec))
            {
                List<string> dayRecipes = GameData.Instance.GetDayAllowedRecipes(stageId);
                if (stageId == 5)
                {
                    // Day 5: 3-Minute (180s) Single-Recipe Bulgogi Bánh Mì High-Score Rush!
                    duration = 180.0f;
                    currentLevelWaves = new List<WaveSpec>
                    {
                        new WaveSpec(1, 99, 10.0f, 75.0f, 3, dayRecipes),
                        new WaveSpec(2, 99, 8.5f, 70.0f, 4, dayRecipes),
                        new WaveSpec(3, 99, 7.5f, 65.0f, 4, dayRecipes)
                    };
                }
                else
                {
                    duration = spec.shiftDuration;
                    int baseOrders = 2 + (stageId - 1);
                    float baseInterval = Mathf.Max(8.5f, 16.0f - (stageId * 1.4f));
                    float basePatience = Mathf.Max(50.0f, 72.0f - (stageId * 3.0f));
                    currentLevelWaves = new List<WaveSpec>
                    {
                        new WaveSpec(1, baseOrders, baseInterval, basePatience, Mathf.Min(2, 1 + stageId / 2), dayRecipes),
                        new WaveSpec(2, baseOrders + 1, Mathf.Max(7.0f, baseInterval - 2.0f), Mathf.Max(45.0f, basePatience - 5.0f), Mathf.Min(3, 2 + stageId / 2), dayRecipes),
                        new WaveSpec(3, baseOrders + 2, Mathf.Max(6.0f, baseInterval - 3.5f), Mathf.Max(40.0f, basePatience - 10.0f), Mathf.Min(4, 2 + (stageId + 1) / 2), dayRecipes)
                    };
                }
                StartWave(0);
            }
            else if (currentMode == GameMode.ENDLESS_RUSH)
            {
                duration = 99999.0f;
                orderSpawnInterval = 14.0f;
                dynamicMaxOrders = 2;
                currentWavePatience = 65.0f;
                List<string> allDayRecipes = GameData.Instance != null ? GameData.Instance.GetDayAllowedRecipes(99) : new List<string> { "tteokbokki" };
                currentLevelWaves = new List<WaveSpec> { new WaveSpec(1, 999, 14.0f, 65.0f, 2, allDayRecipes) };
                currentWaveIndex = 0;
                waveOrdersRequired = 999;
                waveOrdersSpawned = 0;
                waveOrdersCompleted = 0;
            }
            shiftTimeRemaining = duration;
            // Only pause for the recipe book when a live HUD exists to close it (fail-safe: headless tests never deadlock)
            isWaitingForRecipeBook = (!isTutorial && !isAutoplayQAMode && Application.isPlaying);

            orderSpawnTimer = isTutorial ? 99999.0f : 1.0f;
            OnFlowUpdated?.Invoke(kitchenFlow, false, 0.0f);
            OnReputationChanged?.Invoke(reputation, reputation);
            OnWaveUpdated?.Invoke(currentWaveNumber, totalWaves, waveOrdersCompleted, waveOrdersRequired);

            if (isTutorial)
            {
                AdvanceTutorialStep(1);
            }
        }

        public void EndShift(string endReason = "", bool? explicitVictory = null)
        {
            if (!shiftActive) return;
            // LAN guest never ends its own day; it waits for the host's DAY_REPORT.
            if (isNetworkClient && !applyingHostReport) return;
            shiftActive = false;

            orderSpawnTimer = 999999.0f;
            activeOrders.Clear();
            OnOrderQueueUpdated?.Invoke(activeOrders);

            if (KitchenController.Instance != null)
            {
                KitchenController.Instance.ClearKitchenDaily();
            }

            bool victory = explicitVictory ?? (reputation > 0.0f && (isTutorial || wavesClearedCount >= totalWaves || ordersServed > 0));
            if (reputation <= 0.0f)
            {
                victory = false;
            }
            levelVictory = victory;

            int stars = 0;
            if (victory)
            {
                stars = reputation >= 75.0f ? 3 : (reputation >= 40.0f ? 2 : 1);
            }

            int goldAwarded = 0;
            bool isNewHighScore = false;
            int highScore = totalScore;
            if (ProgressionManager.Instance != null)
            {
                if (currentMode == GameMode.CAMPAIGN && !isTutorial)
                {
                    var res = ProgressionManager.Instance.RecordLevelWaveCompletion(
                        currentStageId, reputation, victory, totalScore, currentShiftSessionToken);
                    goldAwarded = res.goldEarned;

                    if (currentStageId == 5)
                    {
                        isNewHighScore = ProgressionManager.Instance.RecordEndlessRun(totalScore, ordersServed, 180.0f);
                        highScore = Mathf.Max(totalScore, ProgressionManager.Instance.endlessBestScore);
                    }
                }
                else if (currentMode == GameMode.ENDLESS_RUSH)
                {
                    isNewHighScore = ProgressionManager.Instance.RecordEndlessRun(totalScore, ordersServed, shiftElapsedSeconds);
                    highScore = Mathf.Max(totalScore, ProgressionManager.Instance.endlessBestScore);
                    if (ordersServed > 0 && reputation > 0f)
                    {
                        goldAwarded = ProgressionManager.CalculateGoldReward(reputation, true);
                        ProgressionManager.Instance.gold += goldAwarded;
                        ProgressionManager.Instance.SaveProgression();
                    }
                }
            }
            lastGoldEarned = goldAwarded;

            var summary = new Dictionary<string, object>
            {
                { "stage_id", currentStageId },
                { "mode", currentMode },
                { "victory", victory },
                { "reputation", reputation },
                { "gold_earned", goldAwarded },
                { "coinsEarned", goldAwarded },
                { "waves_completed", wavesClearedCount },
                { "total_waves", totalWaves },
                { "total_score", totalScore },
                { "finalScore", totalScore },
                { "high_score", highScore },
                { "is_new_high_score", isNewHighScore },
                { "stars", stars },
                { "orders_served", ordersServed },
                { "ordersServed", ordersServed },
                { "orders_failed", ordersFailed },
                { "ordersFailed", ordersFailed },
                { "handoffs_completed", handoffsCompleted },
                { "rush_rhythms_triggered", rushRhythmsTriggered },
                { "rushSeconds", rushRhythmsTriggered * RUSH_RHYTHM_DURATION },
                { "shift_duration", shiftElapsedSeconds },
                { "reason", endReason }
            };

            OnShiftEnded?.Invoke(summary);
        }

        public void AddKitchenFlow(float delta)
        {
            if (rushRhythmActive && delta > 0.0f)
            {
                return;
            }

            kitchenFlow = Mathf.Clamp(kitchenFlow + delta, 0.0f, MAX_FLOW);

            if (kitchenFlow >= MAX_FLOW && !rushRhythmActive)
            {
                TriggerRushRhythm();
            }
            else
            {
                OnFlowUpdated?.Invoke(kitchenFlow, rushRhythmActive, rushRhythmRemaining);
            }
        }

        private void TriggerRushRhythm()
        {
            kitchenFlow = 0.0f;
            rushRhythmActive = true;
            rushRhythmRemaining = RUSH_RHYTHM_DURATION;
            rushRhythmsTriggered++;

            OnRushRhythmChanged?.Invoke(true);
            OnFeedbackBanner?.Invoke("★ RUSH RHYTHM! 1.5x SCORE ★", new Color(1.0f, 0.85f, 0.2f));
            OnFlowUpdated?.Invoke(kitchenFlow, rushRhythmActive, rushRhythmRemaining);
        }

        public bool RecordHandoff(string itemId, int placerPeer, int retrieverPeer, float timeElapsedSec)
        {
            if (placerPeer == retrieverPeer) return false;
            if (timeElapsedSec > (HANDOFF_WINDOW_MS / 1000.0f)) return false;

            OrderTicket targetOrder = null;
            foreach (var ord in activeOrders)
            {
                if (!ord.handoffBonusClaimed && !claimedHandoffOrders.Contains(ord.ticketId))
                {
                    targetOrder = ord;
                    break;
                }
            }

            if (targetOrder == null) return false;

            targetOrder.handoffBonusClaimed = true;
            claimedHandoffOrders.Add(targetOrder.ticketId);
            handoffsCompleted++;
            AddKitchenFlow(HANDOFF_FLOW_BONUS);
            OnFeedbackBanner?.Invoke("Handoff! +15 Flow", new Color(0.3f, 1.0f, 0.5f));
            return true;
        }

        public (bool success, int scoreAwarded) DeliverDish(KitchenItem dish, int deliveringPeer)
        {
            for (int i = 0; i < activeOrders.Count; i++)
            {
                var ord = activeOrders[i];
                int unfulfilledIdx = ord.GetUnfulfilledItemIndex(dish.itemId);
                if (unfulfilledIdx >= 0)
                {
                    ord.MarkItemFulfilled(unfulfilledIdx);

                    var spec = GameData.Instance != null ? GameData.Instance.GetRecipeSpec(dish.itemId) : null;
                    int baseScore = spec != null ? spec.baseScore : 100;

                    if (rushRhythmActive)
                    {
                        baseScore = (int)Math.Round(baseScore * RUSH_RHYTHM_MULTIPLIER);
                    }

                    float pct = Mathf.Clamp01(ord.timeRemaining / ord.initialPatience);
                    int tip = (int)Math.Round(pct * 25.0f);
                    int finalScore = baseScore + tip;

                    totalScore += finalScore;

                    float flowBonus = dish.quality == QualityTier.OPTIMAL ? 20.0f : 12.0f;
                    AddKitchenFlow(flowBonus);

                    if (ord.IsFullyFulfilled())
                    {
                        ordersServed++;
                        waveOrdersCompleted++;
                        activeOrders.RemoveAt(i);

                        if (isTutorial && tutorialStep < 10)
                        {
                            AdvanceTutorialStep(10);
                        }

                        OnOrderQueueUpdated?.Invoke(activeOrders);
                        OnFeedbackBanner?.Invoke($"Order Complete! +{finalScore} pts", new Color(0.2f, 1.0f, 0.4f));
                        CheckWaveProgression();
                    }
                    else
                    {
                        OnOrderQueueUpdated?.Invoke(activeOrders);
                        OnFeedbackBanner?.Invoke($"Delivered! +{finalScore} pts", new Color(0.3f, 0.85f, 1.0f));
                    }

                    return (true, finalScore);
                }
            }

            return (false, 0);
        }

        public bool TakeCleanPlate()
        {
            if (cleanPlates > 0)
            {
                cleanPlates--;
                return true;
            }
            return false;
        }

        public void ReturnCleanPlate()
        {
            cleanPlates++;
        }

        private void TrySpawnNextOrder()
        {
            int maxOrders = dynamicMaxOrders;
            float interval = orderSpawnInterval;
            float patience = currentWavePatience;
            List<string> allowedRecipes = new List<string> { "tteokbokki" };

            if (isTutorial)
            {
                var spec = GameData.Instance != null ? GameData.Instance.tutorialLevel : null;
                maxOrders = spec != null ? spec.maxConcurrentOrders : 1;
                interval = spec != null ? spec.orderSpawnInterval : 45.0f;
                allowedRecipes = new List<string> { "tteokbokki" };
                patience = 600.0f;
            }
            else if (currentMode == GameMode.CAMPAIGN)
            {
                if (currentLevelWaves != null && currentWaveIndex < currentLevelWaves.Count)
                {
                    var w = currentLevelWaves[currentWaveIndex];
                    maxOrders = w.maxConcurrentOrders;
                    interval = w.orderSpawnInterval;
                    patience = w.customerPatienceSeconds;
                    allowedRecipes = w.allowedRecipes;
                }
                else if (GameData.Instance != null)
                {
                    allowedRecipes = GameData.Instance.GetDayAllowedRecipes(currentStageId);
                }
            }
            else if (currentMode == GameMode.ENDLESS_RUSH)
            {
                float t = Mathf.Clamp01(shiftElapsedSeconds / 180.0f);
                maxOrders = Mathf.RoundToInt(Mathf.Lerp(2.0f, 4.0f, t));
                interval = Mathf.Lerp(14.0f, 6.5f, t);
                if (GameData.Instance != null)
                {
                    allowedRecipes = GameData.Instance.GetDayAllowedRecipes(99);
                }
            }

            orderSpawnTimer = interval;

            if (activeOrders.Count >= maxOrders || allowedRecipes == null || allowedRecipes.Count == 0) return;
            if (currentMode == GameMode.CAMPAIGN && !isTutorial && waveOrdersSpawned >= waveOrdersRequired) return;

            // Determine multi-item count (1, 2, or 3 items per customer ticket)
            int itemCount = 1;
            if (!isTutorial)
            {
                float roll = UnityEngine.Random.value;
                if (currentStageId >= 3 || currentMode == GameMode.ENDLESS_RUSH)
                {
                    if (roll < 0.45f) itemCount = 1;
                    else if (roll < 0.80f) itemCount = 2;
                    else itemCount = 3;
                }
                else if (currentStageId == 2)
                {
                    if (roll < 0.55f) itemCount = 1;
                    else itemCount = 2;
                }
                else // Stage 1 (Tteokbokki)
                {
                    if (roll < 0.70f) itemCount = 1;
                    else itemCount = 2; // Can order 2x Tteokbokki!
                }
            }

            List<string> selectedItems = new List<string>();
            for (int k = 0; k < itemCount; k++)
            {
                selectedItems.Add(allowedRecipes[UnityEngine.Random.Range(0, allowedRecipes.Count)]);
            }

            string primaryRecipe = selectedItems[0];
            var recipeSpec = GameData.Instance != null ? GameData.Instance.GetRecipeSpec(primaryRecipe) : null;
            string vessel = recipeSpec != null ? recipeSpec.requiredVessel : "none";
            if (patience <= 0.0f)
                patience = recipeSpec != null ? recipeSpec.targetPatienceSeconds : 60.0f;

            // Extra patience for combo orders (+22s per extra dish)
            if (itemCount > 1)
            {
                patience += (itemCount - 1) * 22.0f;
            }

            // Harder recipes (Day 3 BBQ Rice, Day 4 Broken Rice) get a little extra time
            foreach (var item in selectedItems)
            {
                patience += GetRecipePatienceBonus(item);
            }

            ticketCounter++;
            waveOrdersSpawned++;
            var ticket = new OrderTicket($"ticket_{ticketCounter}", selectedItems, vessel, patience);
            activeOrders.Add(ticket);

            OnOrderQueueUpdated?.Invoke(activeOrders);
            OnWaveUpdated?.Invoke(currentWaveNumber, totalWaves, waveOrdersCompleted, waveOrdersRequired);
        }

        /// <summary>Extra customer patience (seconds) for recipes with more steps.</summary>
        public static float GetRecipePatienceBonus(string recipeId)
        {
            switch (recipeId)
            {
                case "korean_bbq_rice":   // Day 3: grill + rice + assembly
                case "viet_broken_rice":  // Day 4: more parts to assemble
                    return 2.0f;
                default:
                    return 0.0f;
            }
        }

        // ------------------------------------------------------------------
        // LAN host -> client order / score sync
        // ------------------------------------------------------------------

        /// <summary>True on a LAN client: the host owns orders, timers and score; this side only displays them.</summary>
        public bool isNetworkClient = false;
        private bool applyingHostReport = false;

        /// <summary>Next day after a finished one: tutorial leads to Day 1, a win advances (max Day 5), a loss replays.</summary>
        public static int ComputeNextStage(int stage, bool victory)
        {
            if (stage <= 0) return 1;
            if (victory && stage < 5) return stage + 1;
            return stage;
        }

        /// <summary>LAN guest: end the local day using the host's results (gold is earned once per device per shift).</summary>
        public void ApplyDayReport(Dictionary<string, object> r)
        {
            if (r == null || !shiftActive) return;
            if (r.TryGetValue("reputation", out var rp)) reputation = (float)rp;
            if (r.TryGetValue("total_score", out var sc)) totalScore = (int)sc;
            if (r.TryGetValue("ordersServed", out var os)) ordersServed = (int)os;
            if (r.TryGetValue("waves_completed", out var wc)) wavesClearedCount = (int)wc;
            bool victory = r.TryGetValue("victory", out var v) && (bool)v;
            applyingHostReport = true;
            try { EndShift("Host ended the day", victory); }
            finally { applyingHostReport = false; }
        }

        public const string ORDERS_PACKET = "ORDERS";
        private const int ORDERS_HEADER_FIELDS = 9; // ORDERS + 8 state fields (last = order count)
        private const int MAX_SYNC_ORDERS = 16;

        private static string SyncSafe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("|", "").Replace(";", "").Replace(",", "");
        }

        private static string F(float v) => v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        private static bool TryF(string s, out float v) =>
            float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v)
            && !float.IsNaN(v) && !float.IsInfinity(v);

        public NetworkShiftState CaptureNetworkState()
        {
            return new NetworkShiftState
            {
                score = totalScore,
                reputation = reputation,
                flow = kitchenFlow,
                rushActive = rushRhythmActive,
                rushRemaining = rushRhythmRemaining,
                waveCompleted = waveOrdersCompleted,
                waveRequired = waveOrdersRequired,
                orders = activeOrders
            };
        }

        public static string EncodeNetworkState(NetworkShiftState st)
        {
            var sb = new System.Text.StringBuilder();
            var orders = st.orders ?? new List<OrderTicket>();
            int n = Mathf.Min(orders.Count, MAX_SYNC_ORDERS);
            sb.Append(ORDERS_PACKET).Append('|')
              .Append(st.score).Append('|')
              .Append(F(st.reputation)).Append('|')
              .Append(F(st.flow)).Append('|')
              .Append(st.rushActive ? "1" : "0").Append('|')
              .Append(F(st.rushRemaining)).Append('|')
              .Append(st.waveCompleted).Append('|')
              .Append(st.waveRequired).Append('|')
              .Append(n);
            for (int i = 0; i < n; i++)
            {
                var o = orders[i];
                var items = new List<string>();
                foreach (var it in o.requiredItems) items.Add(SyncSafe(it));
                var done = new List<string>();
                foreach (var idx in o.fulfilledIndices) done.Add(idx.ToString(System.Globalization.CultureInfo.InvariantCulture));
                sb.Append('|')
                  .Append(SyncSafe(o.ticketId)).Append(';')
                  .Append(string.Join(",", items)).Append(';')
                  .Append(SyncSafe(o.targetVessel)).Append(';')
                  .Append(F(o.timeRemaining)).Append(';')
                  .Append(F(o.initialPatience)).Append(';')
                  .Append(string.Join(",", done));
            }
            return sb.ToString();
        }

        /// <summary>Parses an ORDERS packet. Returns null on any malformed input (never throws).</summary>
        public static NetworkShiftState DecodeNetworkState(string packet)
        {
            if (string.IsNullOrEmpty(packet) || packet.Length > 8192) return null;
            var parts = packet.Split('|');
            if (parts.Length < ORDERS_HEADER_FIELDS || parts[0] != ORDERS_PACKET) return null;

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            if (!int.TryParse(parts[1], System.Globalization.NumberStyles.Integer, inv, out int score)) return null;
            if (!TryF(parts[2], out float rep) || !TryF(parts[3], out float flow)) return null;
            bool rush = parts[4] == "1";
            if (!TryF(parts[5], out float rushRem)) return null;
            if (!int.TryParse(parts[6], System.Globalization.NumberStyles.Integer, inv, out int wc)) return null;
            if (!int.TryParse(parts[7], System.Globalization.NumberStyles.Integer, inv, out int wr)) return null;
            if (!int.TryParse(parts[8], System.Globalization.NumberStyles.Integer, inv, out int n)) return null;
            if (n < 0 || n > MAX_SYNC_ORDERS || parts.Length != ORDERS_HEADER_FIELDS + n) return null;

            var st = new NetworkShiftState
            {
                score = score,
                reputation = Mathf.Clamp(rep, 0f, MAX_REPUTATION),
                flow = Mathf.Clamp(flow, 0f, MAX_FLOW),
                rushActive = rush,
                rushRemaining = Mathf.Max(0f, rushRem),
                waveCompleted = wc,
                waveRequired = wr,
                orders = new List<OrderTicket>()
            };

            for (int i = 0; i < n; i++)
            {
                var f = parts[ORDERS_HEADER_FIELDS + i].Split(';');
                if (f.Length != 6) return null;
                var items = new List<string>();
                foreach (var it in f[1].Split(',')) if (!string.IsNullOrEmpty(it)) items.Add(it);
                if (items.Count == 0 || items.Count > 8) return null;
                if (!TryF(f[3], out float tr) || !TryF(f[4], out float ip)) return null;

                var ticket = new OrderTicket(f[0], items, f[2], Mathf.Max(0.1f, ip));
                ticket.timeRemaining = Mathf.Max(0f, tr);
                if (!string.IsNullOrEmpty(f[5]))
                {
                    foreach (var d in f[5].Split(','))
                    {
                        if (int.TryParse(d, System.Globalization.NumberStyles.Integer, inv, out int idx) && idx >= 0 && idx < items.Count)
                        {
                            if (!ticket.fulfilledIndices.Contains(idx)) ticket.fulfilledIndices.Add(idx);
                        }
                    }
                }
                ticket.recipeId = ticket.GetNextUnfulfilledRecipeId();
                st.orders.Add(ticket);
            }
            return st;
        }

        /// <summary>Client side: replace local orders/score with the host's authoritative copy.</summary>
        public void ApplyNetworkState(NetworkShiftState st)
        {
            if (st == null || !shiftActive) return;

            float prevRep = reputation;
            bool prevRush = rushRhythmActive;

            totalScore = st.score;
            reputation = st.reputation;
            kitchenFlow = st.flow;
            rushRhythmActive = st.rushActive;
            rushRhythmRemaining = st.rushRemaining;
            waveOrdersCompleted = st.waveCompleted;
            waveOrdersRequired = st.waveRequired;
            activeOrders = st.orders;

            OnOrderQueueUpdated?.Invoke(activeOrders);
            if (!Mathf.Approximately(prevRep, reputation)) OnReputationChanged?.Invoke(reputation, prevRep);
            if (prevRush != rushRhythmActive) OnRushRhythmChanged?.Invoke(rushRhythmActive);
            OnFlowUpdated?.Invoke(kitchenFlow, rushRhythmActive, rushRhythmRemaining);
            OnWaveUpdated?.Invoke(currentWaveNumber, totalWaves, waveOrdersCompleted, waveOrdersRequired);
        }
    }

    /// <summary>Snapshot of host-owned shift state sent to the LAN client.</summary>
    public class NetworkShiftState
    {
        public int score;
        public float reputation;
        public float flow;
        public bool rushActive;
        public float rushRemaining;
        public int waveCompleted;
        public int waveRequired;
        public List<OrderTicket> orders = new List<OrderTicket>();
    }
}
