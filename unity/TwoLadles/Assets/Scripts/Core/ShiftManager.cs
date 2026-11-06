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
        public static ShiftManager Instance { get; private set; }

        public const float MAX_FLOW = 100.0f;
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

        public bool shiftActive = false;
        public GameMode currentMode = GameMode.CAMPAIGN;
        public int currentStageId = 1;
        public float shiftTimeRemaining = 180.0f;
        public float shiftElapsedSeconds = 0.0f;

        public int totalScore = 0;
        public int ordersServed = 0;
        public int ordersFailed = 0;
        public int handoffsCompleted = 0;
        public int rushRhythmsTriggered = 0;

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

            float dt = Time.deltaTime;
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
                    OnFeedbackBanner?.Invoke("Rush Rhythm Concluded! Standard Service Resumed.", new Color(0.8f, 0.8f, 0.8f));
                }
                OnFlowUpdated?.Invoke(kitchenFlow, rushRhythmActive, rushRhythmRemaining);
            }

            // Tick shift duration in Campaign mode
            if (currentMode == GameMode.CAMPAIGN)
            {
                shiftTimeRemaining -= dt;
                if (shiftTimeRemaining <= 0.0f)
                {
                    EndShift("Shift Ended! Service Complete.");
                    return;
                }
            }

            // Tick active orders patience
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
                    OnFeedbackBanner?.Invoke($"Order Expired! (-15 Flow)", new Color(0.95f, 0.25f, 0.2f));

                    if (currentMode == GameMode.ENDLESS_RUSH && ordersFailed >= 3)
                    {
                        EndShift("Shift Over! 3 Orders Missed.");
                        return;
                    }
                }
            }

            // Spawn incoming orders
            orderSpawnTimer -= dt;
            if (orderSpawnTimer <= 0.0f)
            {
                TrySpawnNextOrder();
            }
        }

        public void StartShift(int stageId, GameMode mode = GameMode.CAMPAIGN)
        {
            currentStageId = stageId;
            currentMode = mode;
            shiftActive = true;
            shiftElapsedSeconds = 0.0f;
            totalScore = 0;
            ordersServed = 0;
            ordersFailed = 0;
            handoffsCompleted = 0;
            rushRhythmsTriggered = 0;
            kitchenFlow = 0.0f;
            rushRhythmActive = false;
            rushRhythmRemaining = 0.0f;
            cleanPlates = INITIAL_CLEAN_PLATES;
            dirtyPlatesAtReturn = 0;

            activeOrders.Clear();
            claimedHandoffOrders.Clear();
            ticketCounter = 0;

            float duration = DEFAULT_SHIFT_DURATION;
            if (currentMode == GameMode.CAMPAIGN && GameData.Instance != null && GameData.Instance.campaignLevels.TryGetValue(stageId, out var spec))
            {
                duration = spec.shiftDuration;
            }
            shiftTimeRemaining = duration;

            orderSpawnTimer = 1.0f;
            OnFlowUpdated?.Invoke(kitchenFlow, false, 0.0f);
        }

        public void EndShift(string endReason = "")
        {
            if (!shiftActive) return;
            shiftActive = false;

            int stars = 0;
            if (currentMode == GameMode.CAMPAIGN && GameData.Instance != null && GameData.Instance.campaignLevels.TryGetValue(currentStageId, out var spec))
            {
                stars = spec.EvaluateStars(totalScore);
            }

            var summary = new Dictionary<string, object>
            {
                { "stage_id", currentStageId },
                { "mode", currentMode },
                { "total_score", totalScore },
                { "stars", stars },
                { "orders_served", ordersServed },
                { "orders_failed", ordersFailed },
                { "handoffs_completed", handoffsCompleted },
                { "rush_rhythms_triggered", rushRhythmsTriggered },
                { "shift_duration", shiftElapsedSeconds },
                { "reason", endReason }
            };

            if (ProgressionManager.Instance != null)
            {
                if (currentMode == GameMode.CAMPAIGN)
                {
                    ProgressionManager.Instance.RecordShiftResults(currentStageId, totalScore, stars, handoffsCompleted, rushRhythmsTriggered);
                }
                else
                {
                    ProgressionManager.Instance.RecordEndlessRun(totalScore, ordersServed, shiftElapsedSeconds);
                }
            }

            OnShiftEnded?.Invoke(summary);
        }

        public void AddKitchenFlow(float delta)
        {
            if (rushRhythmActive && delta > 0.0f)
            {
                // Flow generation paused during Rush Rhythm
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
            OnFeedbackBanner?.Invoke("★ RUSH RHYTHM ACTIVATED! 1.5x BONUS SCORE! ★", new Color(1.0f, 0.85f, 0.2f));
            OnFlowUpdated?.Invoke(kitchenFlow, rushRhythmActive, rushRhythmRemaining);
        }

        public bool RecordHandoff(string itemId, int placerPeer, int retrieverPeer, float timeElapsedSec)
        {
            if (placerPeer == retrieverPeer) return false;
            if (timeElapsedSec > (HANDOFF_WINDOW_MS / 1000.0f)) return false;

            // Find matching unclaimed order
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
            OnFeedbackBanner?.Invoke("Coordinated Handoff! +15 Kitchen Flow!", new Color(0.3f, 1.0f, 0.5f));
            return true;
        }

        public (bool success, int scoreAwarded) DeliverDish(KitchenItem dish, int deliveringPeer)
        {
            for (int i = 0; i < activeOrders.Count; i++)
            {
                var ord = activeOrders[i];
                if (ord.recipeId == dish.itemId)
                {
                    // Matched!
                    var spec = GameData.Instance != null && GameData.Instance.recipes.TryGetValue(ord.recipeId, out var r) ? r : null;
                    int baseScore = spec != null ? spec.baseScore : 100;

                    // Rush Rhythm multiplier
                    if (rushRhythmActive)
                    {
                        baseScore = (int)Math.Round(baseScore * RUSH_RHYTHM_MULTIPLIER);
                    }

                    // Patience tip
                    float pct = Mathf.Clamp01(ord.timeRemaining / ord.initialPatience);
                    int tip = (int)Math.Round(pct * 25.0f);
                    int finalScore = baseScore + tip;

                    totalScore += finalScore;
                    ordersServed++;
                    activeOrders.RemoveAt(i);

                    // Flow award
                    float flowBonus = dish.quality == QualityTier.OPTIMAL ? 20.0f : 12.0f;
                    AddKitchenFlow(flowBonus);

                    // Plate cycle: 1 dirty plate to return tray
                    dirtyPlatesAtReturn++;

                    OnOrderQueueUpdated?.Invoke(activeOrders);
                    OnFeedbackBanner?.Invoke($"Order Delivered! +{finalScore} pts", new Color(0.2f, 1.0f, 0.4f));
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
            int maxOrders = 3;
            float interval = 12.0f;
            List<string> allowedRecipes = new List<string> { "banh_mi_thit", "pho_bo" };

            if (currentMode == GameMode.CAMPAIGN && GameData.Instance != null && GameData.Instance.campaignLevels.TryGetValue(currentStageId, out var spec))
            {
                maxOrders = spec.maxConcurrentOrders;
                interval = spec.orderSpawnInterval;
                allowedRecipes = spec.allowedRecipes;
            }
            else if (currentMode == GameMode.ENDLESS_RUSH)
            {
                // Dynamic scaling
                float t = Mathf.Clamp01(shiftElapsedSeconds / 180.0f);
                maxOrders = Mathf.RoundToInt(Mathf.Lerp(2.0f, 4.0f, t));
                interval = Mathf.Lerp(14.0f, 6.5f, t);
                if (GameData.Instance != null)
                {
                    allowedRecipes = new List<string>(GameData.Instance.recipes.Keys);
                }
            }

            orderSpawnTimer = interval;

            if (activeOrders.Count >= maxOrders || allowedRecipes.Count == 0) return;

            string selectedRecipe = allowedRecipes[UnityEngine.Random.Range(0, allowedRecipes.Count)];
            var recipeSpec = GameData.Instance != null && GameData.Instance.recipes.TryGetValue(selectedRecipe, out var rs) ? rs : null;
            string vessel = recipeSpec != null ? recipeSpec.requiredVessel : "plate";
            float patience = recipeSpec != null ? recipeSpec.targetPatienceSeconds : 60.0f;

            ticketCounter++;
            var ticket = new OrderTicket($"ticket_{ticketCounter}", selectedRecipe, vessel, patience);
            activeOrders.Add(ticket);

            OnOrderQueueUpdated?.Invoke(activeOrders);
        }
    }
}
