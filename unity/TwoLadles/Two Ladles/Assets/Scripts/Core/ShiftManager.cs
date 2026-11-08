using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;

namespace TwoLadles.Core
{
    public enum GameMode
    {
        CAMPAIGN = 0,
        ENDLESS_RUSH = 1
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
            if (!shiftActive) return;

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

            switch (tutorialStep)
            {
                case 1:
                    tutorialStepTitle = "STEP 1: MOVE";
                    tutorialStepInstruction = "[WASD] or Joystick to walk.";
                    break;
                case 2:
                    tutorialStepTitle = "STEP 2: GET PORK";
                    tutorialStepInstruction = "Hold [TAB] (or tap Wheel) -> Select 'Pork Belly'.";
                    break;
                case 3:
                    tutorialStepTitle = "STEP 3: SLICE PORK";
                    tutorialStepInstruction = "Follow arrow to Prep Table: [E] Place, then [F] x4.";
                    break;
                case 4:
                    tutorialStepTitle = "STEP 4: PASS COUNTER";
                    tutorialStepInstruction = "Follow arrow to center Pass Counter: press [E].";
                    break;
                case 5:
                    tutorialStepTitle = "STEP 5: SWITCH CHEF";
                    tutorialStepInstruction = "Press [C] or 'Switch Chef' to control Jiwon.";
                    break;
                case 6:
                    tutorialStepTitle = "STEP 6: GRILL PORK";
                    tutorialStepInstruction = "[E] at Pass Counter (+15 Flow), then [E] at Grill.";
                    break;
                case 7:
                    tutorialStepTitle = "STEP 7: GREEN BAR";
                    tutorialStepInstruction = "When Grill bar turns GREEN, press [E] to pick up.";
                    break;
                case 8:
                    tutorialStepTitle = "STEP 8: ASSEMBLE";
                    tutorialStepInstruction = "Put Clean Plate + Baguette + Grilled Pork + Pickled Veg on Assembly Table [E].";
                    break;
                case 9:
                    tutorialStepTitle = "STEP 9: SERVE";
                    tutorialStepInstruction = "Bring Bánh Mì to Serving Counter [E]!";
                    break;
                case 10:
                    tutorialStepTitle = "★ COMPLETE! ★";
                    tutorialStepInstruction = "You're ready! Click 'Return to Lobby' to start Level 1.";
                    break;
            }

            OnTutorialStepChanged?.Invoke(tutorialStep, tutorialStepTitle, tutorialStepInstruction);
        }

        public void StartShift(int stageId, GameMode mode = GameMode.CAMPAIGN)
        {
            currentStageId = stageId;
            currentMode = mode;
            shiftActive = true;
            shiftElapsedSeconds = 0.0f;
            reputation = MAX_REPUTATION;
            levelVictory = false;
            lastGoldEarned = 0;
            currentShiftSessionToken = $"shift_{stageId}_{DateTime.UtcNow.Ticks}_{UnityEngine.Random.Range(1000, 9999)}";

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
                currentLevelWaves = spec != null && spec.waves != null ? spec.waves : new List<WaveSpec> { new WaveSpec(1, 1, 60f, 600f, 1, new List<string> { "banh_mi_thit" }) };
                StartWave(0);

                // Starter Bánh Mì Thịt order
                ticketCounter++;
                waveOrdersSpawned = 1;
                var ticket = new OrderTicket($"ticket_{ticketCounter}", "banh_mi_thit", "plate", 600.0f);
                activeOrders.Add(ticket);
                OnOrderQueueUpdated?.Invoke(activeOrders);
            }
            else if (currentMode == GameMode.CAMPAIGN && GameData.Instance != null && GameData.Instance.campaignLevels.TryGetValue(stageId, out var spec))
            {
                duration = spec.shiftDuration;
                currentLevelWaves = spec.waves;
                StartWave(0);
            }
            else if (currentMode == GameMode.ENDLESS_RUSH)
            {
                duration = 99999.0f;
                orderSpawnInterval = 14.0f;
                dynamicMaxOrders = 2;
                currentWavePatience = 65.0f;
                currentLevelWaves = new List<WaveSpec> { new WaveSpec(1, 999, 14.0f, 65.0f, 2, new List<string>()) };
                currentWaveIndex = 0;
                waveOrdersRequired = 999;
                waveOrdersSpawned = 0;
                waveOrdersCompleted = 0;
            }
            shiftTimeRemaining = duration;

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
            shiftActive = false;

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
            if (ProgressionManager.Instance != null)
            {
                if (currentMode == GameMode.CAMPAIGN && !isTutorial)
                {
                    var res = ProgressionManager.Instance.RecordLevelWaveCompletion(
                        currentStageId, reputation, victory, totalScore, currentShiftSessionToken);
                    goldAwarded = res.goldEarned;
                }
                else if (currentMode == GameMode.ENDLESS_RUSH)
                {
                    ProgressionManager.Instance.RecordEndlessRun(totalScore, ordersServed, shiftElapsedSeconds);
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
                if (ord.recipeId == dish.itemId)
                {
                    var spec = GameData.Instance != null && GameData.Instance.recipes.TryGetValue(ord.recipeId, out var r) ? r : null;
                    int baseScore = spec != null ? spec.baseScore : 100;

                    if (rushRhythmActive)
                    {
                        baseScore = (int)Math.Round(baseScore * RUSH_RHYTHM_MULTIPLIER);
                    }

                    float pct = Mathf.Clamp01(ord.timeRemaining / ord.initialPatience);
                    int tip = (int)Math.Round(pct * 25.0f);
                    int finalScore = baseScore + tip;

                    totalScore += finalScore;
                    ordersServed++;
                    waveOrdersCompleted++;
                    activeOrders.RemoveAt(i);

                    if (isTutorial && (tutorialStep == 8 || tutorialStep == 9))
                    {
                        AdvanceTutorialStep(10);
                    }

                    float flowBonus = dish.quality == QualityTier.OPTIMAL ? 20.0f : 12.0f;
                    AddKitchenFlow(flowBonus);

                    dirtyPlatesAtReturn++;

                    OnOrderQueueUpdated?.Invoke(activeOrders);
                    OnFeedbackBanner?.Invoke($"+{finalScore} pts", new Color(0.2f, 1.0f, 0.4f));

                    CheckWaveProgression();
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
            List<string> allowedRecipes = new List<string> { "banh_mi_thit" };

            if (isTutorial)
            {
                var spec = GameData.Instance != null ? GameData.Instance.tutorialLevel : null;
                maxOrders = spec != null ? spec.maxConcurrentOrders : 1;
                interval = spec != null ? spec.orderSpawnInterval : 45.0f;
                allowedRecipes = spec != null ? spec.allowedRecipes : new List<string> { "banh_mi_thit" };
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
                else if (GameData.Instance != null && GameData.Instance.campaignLevels.TryGetValue(currentStageId, out var spec))
                {
                    maxOrders = spec.maxConcurrentOrders;
                    interval = spec.orderSpawnInterval;
                    allowedRecipes = spec.allowedRecipes;
                }
            }
            else if (currentMode == GameMode.ENDLESS_RUSH)
            {
                float t = Mathf.Clamp01(shiftElapsedSeconds / 180.0f);
                maxOrders = Mathf.RoundToInt(Mathf.Lerp(2.0f, 4.0f, t));
                interval = Mathf.Lerp(14.0f, 6.5f, t);
                if (GameData.Instance != null)
                {
                    allowedRecipes = new List<string>(GameData.Instance.recipes.Keys);
                }
            }

            orderSpawnTimer = interval;

            if (activeOrders.Count >= maxOrders || allowedRecipes == null || allowedRecipes.Count == 0) return;
            if (currentMode == GameMode.CAMPAIGN && !isTutorial && waveOrdersSpawned >= waveOrdersRequired) return;

            string selectedRecipe = allowedRecipes[UnityEngine.Random.Range(0, allowedRecipes.Count)];
            var recipeSpec = GameData.Instance != null && GameData.Instance.recipes.TryGetValue(selectedRecipe, out var rs) ? rs : null;
            string vessel = recipeSpec != null ? recipeSpec.requiredVessel : "plate";
            if (patience <= 0.0f)
                patience = recipeSpec != null ? recipeSpec.targetPatienceSeconds : 60.0f;

            ticketCounter++;
            waveOrdersSpawned++;
            var ticket = new OrderTicket($"ticket_{ticketCounter}", selectedRecipe, vessel, patience);
            activeOrders.Add(ticket);

            OnOrderQueueUpdated?.Invoke(activeOrders);
            OnWaveUpdated?.Invoke(currentWaveNumber, totalWaves, waveOrdersCompleted, waveOrdersRequired);
        }
    }
}
