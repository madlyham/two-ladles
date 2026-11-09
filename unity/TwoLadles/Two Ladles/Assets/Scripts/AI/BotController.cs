using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Stations;
using TwoLadles.Kitchen;
using TwoLadles.Player;
using TwoLadles.Networking;

namespace TwoLadles.AI
{
    /// <summary>
    /// Local, offline, deterministic utility-based AI Chef Partner.
    /// Controls the non-human chef in Solo Mode (or both chefs in automated Two-Bot QA Autoplay).
    /// Features:
    /// - 2.5D A* Waypoint Pathfinding via KitchenNavGraph (100% collision-free corridor navigation)
    /// - TAS (Tool-Assisted Speedrun) input control with camera aspect-ratio compensated steering
    /// - Inter-Bot Communication Hub blackboard coordination for 2-sided cross-prep recipes
    /// - Dynamic cadence chopping and pass counter handoffs with +15 Flow timing
    /// </summary>
    public class BotController : MonoBehaviour
    {
        [Header("AI Identity & State")]
        public bool botActive = true;
        public int peerId = 2;
        public string characterId = "jiwon";
        public BotDifficulty difficulty = BotDifficulty.Standard;

        [Header("Current Goal")]
        public string currentGoalName = "Idle";
        public StationBase targetStation = null;
        public string desiredItemId = null;
        public string activeRecipeId = "tteokbokki";
        public float goalTimer = 0.0f;
        public float stuckTimer = 0.0f;

        [Header("Navigation & TAS Waypoints")]
        public List<Vector3> currentPath = new List<Vector3>();
        public int currentWaypointIndex = 0;
        public Vector3 targetApproachPosition = Vector3.zero;

        [Header("Components")]
        public PlayerController playerController;

        [Header("Interaction Timing & Anti-Ping-Pong")]
        public float interactCooldown = 0.0f;
        public const float INTERACTION_DELAY = 1.0f;
        public const float PLACED_ITEM_COOLDOWN = 5.0f;
        public Dictionary<StationBase, float> recentlyPlacedCooldowns = new Dictionary<StationBase, float>();

        // Cadence & Decision Timing
        private float decisionInterval = 0.20f;
        private float chopCadenceInterval = 0.40f;
        private float decisionTimer = 0.0f;
        private float chopTimer = 0.0f;
        private float workerHireCooldown = 0.0f;
        private Vector3 lastRecordedPosition = Vector3.zero;
        private StationBase lastTargetStation = null;

        private void Start()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
            if (playerController != null)
            {
                peerId = playerController.peerId;
                characterId = playerController.characterId;
                playerController.isLocalPlayer = false; // Bot drives movement & actions directly
            }

            ConfigureDifficulty(difficulty);
            lastRecordedPosition = transform.position;
        }

        public void ConfigureDifficulty(BotDifficulty diff)
        {
            difficulty = diff;
            switch (difficulty)
            {
                case BotDifficulty.Assistive:
                    decisionInterval = 0.15f;
                    chopCadenceInterval = 0.32f;
                    break;
                case BotDifficulty.Standard:
                    decisionInterval = 0.22f;
                    chopCadenceInterval = 0.40f;
                    break;
                case BotDifficulty.Challenge:
                    decisionInterval = 0.35f;
                    chopCadenceInterval = 0.52f;
                    break;
            }
        }

        private void Update()
        {
            if (!botActive || playerController == null) return;
            if (ShiftManager.Instance == null || !ShiftManager.Instance.shiftActive) return;

            float dt = Time.deltaTime;
            decisionTimer += dt;
            goalTimer += dt;
            if (workerHireCooldown > 0.0f) workerHireCooldown -= dt;

            // Anti-Stuck Watchdog: Catches bot standing still or unable to reach/interact
            if (Vector3.Distance(transform.position, lastRecordedPosition) < 0.08f)
            {
                stuckTimer += dt;
                if (stuckTimer > 3.0f)
                {
                    BotLogger.LogStuck(characterId, $"Stationary for {stuckTimer:F1}s at {(targetStation != null ? targetStation.stationId : "none")} (Goal: '{currentGoalName}')", transform.position);
                    UpdatePathToTarget();
                    EvaluateAndSelectGoal();
                    stuckTimer = 0.0f;
                }
            }
            else
            {
                stuckTimer = 0.0f;
                lastRecordedPosition = transform.position;
            }

            // Decrement interaction delay and anti-ping-pong placement timers
            if (interactCooldown > 0.0f)
            {
                interactCooldown -= dt;
            }

            if (recentlyPlacedCooldowns.Count > 0)
            {
                var keys = new List<StationBase>(recentlyPlacedCooldowns.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    var k = keys[i];
                    recentlyPlacedCooldowns[k] -= dt;
                    if (recentlyPlacedCooldowns[k] <= 0.0f)
                    {
                        recentlyPlacedCooldowns.Remove(k);
                    }
                }
            }

            // Periodic Utility Decision Cycle
            if (decisionTimer >= decisionInterval)
            {
                decisionTimer = 0.0f;
                EvaluateAndSelectGoal();
            }

            ExecuteMovementAndAction(dt);
        }

        /// <summary>
        /// Recalculates A* path to the walkable approach position for the current target station.
        /// </summary>
        public void UpdatePathToTarget()
        {
            if (targetStation == null)
            {
                currentPath.Clear();
                currentWaypointIndex = 0;
                lastTargetStation = null;
                return;
            }

            targetApproachPosition = KitchenNavGraph.GetStationApproachPosition(targetStation, peerId);
            currentPath = KitchenNavGraph.FindPath(transform.position, targetApproachPosition, peerId);
            currentWaypointIndex = 0;
            lastTargetStation = targetStation;
            int count = currentPath != null ? currentPath.Count : 0;
            BotLogger.Log(characterId, $"Path to [{targetStation.stationId}] approach ({targetApproachPosition.x:F1}, {targetApproachPosition.z:F1}) has {count} waypoints.");
        }

        /// <summary>
        /// Evaluates all available kitchen tasks deterministically and selects the highest-utility action.
        /// </summary>
        public void EvaluateAndSelectGoal()
        {
            if (KitchenController.Instance == null) return;

            // Determine active target recipe
            OrderTicket urgentTicket = GetMostUrgentOrder();
            if (urgentTicket != null)
            {
                activeRecipeId = urgentTicket.recipeId;
            }
            else
            {
                activeRecipeId = GetDefaultDayRecipe();
            }

            // 1. URGENT OVERRIDE: Pan in danger of overcooking / burning (Score: 300)
            CookingStation urgentPan = FindUrgentPan();
            if (urgentPan != null)
            {
                if (playerController.carriedItem != null)
                {
                    StationBase dropCounter = FindNearestHoldingCounter();
                    if (dropCounter != null)
                    {
                        SetTarget(dropCounter, "Drop Item for Pan Rescue");
                        return;
                    }
                }
                SetTarget(urgentPan, $"Rescue Pan: {urgentPan.stationId}");
                return;
            }

            // 2. DELIVER / PASS COMPLETED DISH (Score: 260)
            if (playerController.carriedItem != null && IsCompletedDish(playerController.carriedItem.itemId))
            {
                if (characterId == "linh" || transform.position.x < -1.0f)
                {
                    StationBase serveWindow = GetStation("serving_station");
                    if (serveWindow != null)
                    {
                        SetTarget(serveWindow, "Deliver Dish to Serving Window");
                        return;
                    }
                }
                else
                {
                    StationBase passCounter = GetStation("pass_counter");
                    if (passCounter != null)
                    {
                        SetTarget(passCounter, "Pass Completed Dish to Linh");
                        return;
                    }
                }
            }

            // 3. RETRIEVE FROM PASS COUNTER VIA BOT COMMS (Score: 230)
            if (playerController.carriedItem == null)
            {
                var pendingHandoff = BotCommunicationHub.GetPendingHandoffFor(peerId);
                if (pendingHandoff != null)
                {
                    StationBase passCounter = GetStation("pass_counter");
                    if (passCounter != null)
                    {
                        SetTarget(passCounter, $"Retrieve {pendingHandoff.itemId} from Pass Counter");
                        return;
                    }
                }

                // Check physical pass counter slots if comms missed
                PassCounter pass = GetStation("pass_counter") as PassCounter;
                if (pass != null)
                {
                    for (int i = 0; i < pass.slots.Length; i++)
                    {
                        var slot = pass.slots[i];
                        if (slot != null && slot.item != null && slot.placedByPeer != peerId)
                        {
                            if (characterId == "linh" && IsCompletedDish(slot.item.itemId))
                            {
                                SetTarget(pass, "Retrieve Dish from Pass Counter");
                                return;
                            }
                            if (characterId == "jiwon" && !IsCompletedDish(slot.item.itemId))
                            {
                                SetTarget(pass, "Retrieve Ingredient from Pass Counter");
                                return;
                            }
                            if (characterId == "linh" && (slot.item.itemId == "bbq_beef" || slot.item.itemId == "bulgogi"))
                            {
                                SetTarget(pass, $"Retrieve {slot.item.itemId} from Pass Counter");
                                return;
                            }
                        }
                    }
                }
            }

            // 4. JIWON CREW DELEGATION (Score: 190)
            if (characterId == "jiwon" && workerHireCooldown <= 0.0f && ShiftManager.Instance.totalScore >= 40)
            {
                if (ShiftManager.Instance.activeOrders != null && ShiftManager.Instance.activeOrders.Count >= 2)
                {
                    var res = KitchenController.Instance.RequestHireWorker(peerId, "prep_assistant");
                    if (res.ok)
                    {
                        workerHireCooldown = 18.0f;
                    }
                }
            }

            // 5. CULINARY ORDER PIPELINE (Score: 150)
            PlanOrderTask(activeRecipeId);

            // Broadcast Intent to partner chef
            BotCommunicationHub.BroadcastIntent(peerId, characterId, currentGoalName, targetStation != null ? targetStation.stationId : null, activeRecipeId);
        }

        private void SetTarget(StationBase station, string goalName)
        {
            if (targetStation != station || currentGoalName != goalName)
            {
                targetStation = station;
                currentGoalName = goalName;
                UpdatePathToTarget();
                BotLogger.LogGoal(characterId, currentGoalName, targetStation != null ? targetStation.stationId : "null");
            }
        }

        private void PlanOrderTask(string recipeId)
        {
            switch (recipeId)
            {
                case "tteokbokki":
                    PlanTteokbokki();
                    break;
                case "bot_chien":
                    PlanBotChien();
                    break;
                case "korean_bbq_rice":
                    PlanKoreanBBQRice();
                    break;
                case "viet_broken_rice":
                    PlanVietBrokenRice();
                    break;
                case "bulgogi_banh_mi":
                    PlanBulgogiBanhMi();
                    break;
                default:
                    PlanTteokbokki();
                    break;
            }
        }

        // --- DAY 1: TTEOKBOKKI ---
        private void PlanTteokbokki()
        {
            if (characterId == "jiwon")
            {
                // Jiwon: Prepares and cooks Tteokbokki in simmer_wok with Gochujang (supports in-wok or table self-assembly)
                CookingStation wok = GetStation("simmer_wok") as CookingStation;
                CounterStation counter = GetStation("counter_east") as CounterStation;
                PassCounter pass = GetStation("pass_counter") as PassCounter;
                CrateStation fridge = GetStation("fridge_east") as CrateStation;
                CrateStation shelf = GetStation("shelf_east") as CrateStation;
                if (wok == null) return;

                // 1. Holding Completed Dish
                if (playerController.carriedItem != null && playerController.carriedItem.itemId == "tteokbokki")
                {
                    SetTarget(pass, "Pass Completed Tteokbokki to Linh");
                    return;
                }

                // 2. Holding Rice Cakes
                if (playerController.carriedItem != null && (playerController.carriedItem.itemId == "tteok_portioned" || playerController.carriedItem.itemId == "tteok_raw"))
                {
                    if (wok.heldItem == null)
                    {
                        SetTarget(wok, "Place Rice Cakes in Pan");
                        return;
                    }
                    else if (counter != null && counter.heldItem == null)
                    {
                        SetTarget(counter, "Place Rice Cakes on Table");
                        return;
                    }
                }

                // 3. Holding Gochujang
                if (playerController.carriedItem != null && playerController.carriedItem.itemId == "gochujang")
                {
                    // If wok has rice cakes (raw, cooking, or already simmered), add gochujang directly to wok!
                    if (wok.heldItem != null && (wok.heldItem.itemId == "tteok_portioned" || wok.heldItem.itemId == "tteok_raw" ||
                        wok.heldItem.itemId == "tteokbokki_simmered" || wok.heldItem.itemId == "tteok_cooked" || wok.heldItem.itemId == "tteokbokki_raw"))
                    {
                        SetTarget(wok, "Add Gochujang to Wok");
                        return;
                    }
                    // If table has rice cakes, assemble on table!
                    if (counter != null && counter.heldItem != null && (counter.heldItem.itemId == "tteok_portioned" || counter.heldItem.itemId == "tteok_raw" ||
                        counter.heldItem.itemId == "tteokbokki_simmered" || counter.heldItem.itemId == "tteok_cooked"))
                    {
                        SetTarget(counter, "Assemble Tteokbokki on Table");
                        return;
                    }
                }

                // 4. Holding Simmered/Cooked Rice Cakes (e.g. taken out of pan)
                if (playerController.carriedItem != null && (playerController.carriedItem.itemId == "tteokbokki_simmered" || playerController.carriedItem.itemId == "tteok_cooked"))
                {
                    if (counter != null && counter.heldItem == null)
                    {
                        SetTarget(counter, "Place Simmered Toboki on Table");
                        return;
                    }
                }

                // 5. Holding Raw Tteokbokki (already combined, ready to cook)
                if (playerController.carriedItem != null && playerController.carriedItem.itemId == "tteokbokki_raw")
                {
                    if (wok.heldItem == null)
                    {
                        SetTarget(wok, "Cook Tteokbokki in Wok");
                        return;
                    }
                }

                // 6. Empty Hands (playerController.carriedItem == null)
                if (playerController.carriedItem == null)
                {
                    // Check if table has completed Tteokbokki
                    if (counter != null && counter.heldItem != null && counter.heldItem.itemId == "tteokbokki")
                    {
                        SetTarget(counter, "Pick Up Completed Tteokbokki");
                        return;
                    }

                    // Check if table has simmered rice cakes waiting for Gochujang
                    if (counter != null && counter.heldItem != null && (counter.heldItem.itemId == "tteokbokki_simmered" || counter.heldItem.itemId == "tteok_portioned" || counter.heldItem.itemId == "tteok_cooked"))
                    {
                        desiredItemId = "gochujang";
                        SetTarget(shelf, "Get Gochujang for Table Assembly");
                        return;
                    }

                    // Check Wok contents:
                    if (wok.heldItem != null)
                    {
                        // Case A: Finished Tteokbokki in wok -> pick up!
                        if (wok.heldItem.itemId == "tteokbokki" || (wok.heldItem.itemId == "tteokbokki_raw" && (wok.currentState == CookingState.OPTIMAL || wok.currentState == CookingState.OVERCOOKED)))
                        {
                            SetTarget(wok, "Pick Up Cooked Tteokbokki");
                            return;
                        }

                        // Case B: Raw Tteokbokki actively cooking in wok -> monitor!
                        if (wok.heldItem.itemId == "tteokbokki_raw")
                        {
                            SetTarget(wok, "Monitor Tteokbokki Simmering in Wok");
                            return;
                        }

                        // Case C: Wok has rice cakes (raw, cooking, or simmered) -> Go get Gochujang to add to wok!
                        if (wok.heldItem.itemId == "tteok_portioned" || wok.heldItem.itemId == "tteok_raw" ||
                            wok.heldItem.itemId == "tteokbokki_simmered" || wok.heldItem.itemId == "tteok_cooked")
                        {
                            desiredItemId = "gochujang";
                            SetTarget(shelf, "Get Gochujang for Wok");
                            return;
                        }
                    }

                    // Case D: Wok is empty. Check Pass Counter for rice cakes from Linh:
                    if (wok.heldItem == null)
                    {
                        if (pass != null)
                        {
                            for (int i = 0; i < pass.slots.Length; i++)
                            {
                                if (pass.slots[i] != null && pass.slots[i].item != null && pass.slots[i].placedByPeer != peerId)
                                {
                                    SetTarget(pass, "Retrieve Rice Cakes from Pass Counter");
                                    return;
                                }
                            }
                        }

                        // Otherwise get rice cakes from fridge
                        desiredItemId = "tteok_portioned";
                        SetTarget(fridge, "Get Rice Cakes from Fridge");
                        return;
                    }
                }
            }
            else
            {
                // Linh: Supports with pass counter handoff or stands by to serve completed dish
                if (playerController.carriedItem == null)
                {
                    CookingStation eastWok = GetStation("simmer_wok") as CookingStation;
                    PassCounter pass = GetStation("pass_counter") as PassCounter;
                    bool passHasItem = pass != null && pass.slots[0].item != null;

                    if (eastWok != null && eastWok.heldItem == null && !passHasItem)
                    {
                        desiredItemId = "tteok_portioned";
                        SetTarget(GetStation("shelf_west"), "Get Rice Cakes to Pass to Jiwon");
                    }
                    else
                    {
                        SetTarget(GetStation("pass_counter"), "Standby at Pass Counter");
                    }
                }
                else if (playerController.carriedItem.itemId == "tteok_portioned")
                {
                    SetTarget(GetStation("pass_counter"), "Pass Rice Cakes to Jiwon");
                }
            }
        }

        // --- DAY 2: BỘT CHIÊN ---
        private void PlanBotChien()
        {
            if (characterId == "linh")
            {
                CookingStation stove = GetStation("noodle_blancher") as CookingStation;
                if (stove == null) stove = GetStation("stockpot") as CookingStation;
                if (stove == null) return;

                if (stove.heldItem == null)
                {
                    if (playerController.carriedItem == null)
                    {
                        desiredItemId = "flour";
                        SetTarget(GetStation("shelf_west"), "Get Rice Flour from Shelf");
                    }
                    else if (playerController.carriedItem.itemId == "flour")
                    {
                        SetTarget(stove, "Place Flour on Stove");
                    }
                    return;
                }

                if (stove.heldItem.itemId == "flour" || stove.heldItem.itemId == "cooked_flour")
                {
                    if (stove.currentState == CookingState.OPTIMAL || stove.currentState == CookingState.OVERCOOKED)
                    {
                        if (playerController.carriedItem == null)
                        {
                            desiredItemId = "egg_raw";
                            SetTarget(GetStation("fridge_west"), "Get Egg from Fridge");
                        }
                        else if (playerController.carriedItem.itemId == "egg_raw")
                        {
                            SetTarget(stove, "Add Egg to Cooked Flour");
                        }
                    }
                    else
                    {
                        SetTarget(stove, "Wait for Flour Cake to Cook");
                    }
                    return;
                }

                if (stove.heldItem.itemId == "bot_chien_batter" || stove.heldItem.itemId == "bot_chien")
                {
                    if (stove.currentState == CookingState.OPTIMAL || stove.currentState == CookingState.OVERCOOKED)
                    {
                        if (playerController.carriedItem == null)
                        {
                            SetTarget(stove, "Pick Up Bột Chiên");
                        }
                    }
                    else
                    {
                        SetTarget(stove, "Monitor Bột Chiên Frying");
                    }
                    return;
                }
            }
            else
            {
                // Jiwon: Cooks parallel batch in simmer_wok
                CookingStation wok = GetStation("simmer_wok") as CookingStation;
                if (wok == null) return;

                if (wok.heldItem == null)
                {
                    if (playerController.carriedItem == null)
                    {
                        desiredItemId = "flour";
                        SetTarget(GetStation("shelf_east"), "Get Flour for Second Batch");
                    }
                    else if (playerController.carriedItem.itemId == "flour")
                    {
                        SetTarget(wok, "Place Flour in Wok");
                    }
                    return;
                }

                if (wok.heldItem.itemId == "flour" || wok.heldItem.itemId == "cooked_flour")
                {
                    if (wok.currentState == CookingState.OPTIMAL || wok.currentState == CookingState.OVERCOOKED)
                    {
                        if (playerController.carriedItem == null)
                        {
                            desiredItemId = "egg_raw";
                            SetTarget(GetStation("fridge_east"), "Get Egg for Second Batch");
                        }
                        else if (playerController.carriedItem.itemId == "egg_raw")
                        {
                            SetTarget(wok, "Add Egg to Wok");
                        }
                    }
                    else
                    {
                        SetTarget(wok, "Wait for Flour Cake to Cook");
                    }
                    return;
                }

                if (wok.heldItem.itemId == "bot_chien_batter" || wok.heldItem.itemId == "bot_chien")
                {
                    if (wok.currentState == CookingState.OPTIMAL || wok.currentState == CookingState.OVERCOOKED)
                    {
                        if (playerController.carriedItem == null)
                        {
                            SetTarget(wok, "Pick Up Bột Chiên Batch");
                        }
                    }
                    else
                    {
                        SetTarget(wok, "Monitor Bột Chiên Frying");
                    }
                    return;
                }
            }
        }

        // --- DAY 3: KOREAN BBQ ON RICE ---
        private void PlanKoreanBBQRice()
        {
            if (characterId == "jiwon")
            {
                // Jiwon: Slice raw meat once at prep table -> Korean BBQ meat -> Grill to BBQ beef -> Pass to Linh
                PrepStation prep = GetStation("prep_east") as PrepStation;
                CookingStation grill = GetStation("grill_pan") as CookingStation;

                if (playerController.carriedItem == null)
                {
                    if (grill != null && grill.heldItem != null)
                    {
                        if (grill.currentState == CookingState.OPTIMAL || grill.currentState == CookingState.OVERCOOKED)
                        {
                            SetTarget(grill, "Pick Up Grilled BBQ Beef");
                            return;
                        }
                        else
                        {
                            SetTarget(grill, "Monitor BBQ Beef Grilling");
                            return;
                        }
                    }
                    if (prep != null && prep.heldItem != null)
                    {
                        if (prep.strikesCompleted >= PrepStation.REQUIRED_STRIKES)
                        {
                            SetTarget(prep, "Pick Up Sliced BBQ Meat");
                            return;
                        }
                        else
                        {
                            SetTarget(prep, "Chop Raw Meat");
                            return;
                        }
                    }
                    if (grill != null && grill.heldItem == null)
                    {
                        desiredItemId = "unprocessed_raw_meat";
                        SetTarget(GetStation("fridge_east"), "Get Raw Meat from Fridge");
                        return;
                    }
                }

                if (playerController.carriedItem != null)
                {
                    if (playerController.carriedItem.itemId == "unprocessed_raw_meat")
                    {
                        SetTarget(prep, "Cut Raw Meat at Prep Table");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "korean_bbq_raw")
                    {
                        SetTarget(grill, "Grill Sliced BBQ Meat");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "bulgogi" || playerController.carriedItem.itemId == "bbq_beef")
                    {
                        SetTarget(GetStation("pass_counter"), "Pass Grilled Beef to Linh");
                        return;
                    }
                }
            }
            else
            {
                // Linh: Cook rice portion on West stove, assemble with passed BBQ beef, serve
                CookingStation stove = GetStation("noodle_blancher") as CookingStation;
                CounterStation counter = GetStation("counter_west") as CounterStation;

                if (playerController.carriedItem == null)
                {
                    if (stove != null && stove.heldItem != null)
                    {
                        if (stove.currentState == CookingState.OPTIMAL || stove.currentState == CookingState.OVERCOOKED)
                        {
                            SetTarget(stove, "Pick Up Steamed Rice");
                            return;
                        }
                        else
                        {
                            SetTarget(stove, "Wait for Rice to Steam");
                            return;
                        }
                    }
                    if (counter != null && counter.heldItem != null && counter.heldItem.itemId == "korean_bbq_rice")
                    {
                        SetTarget(counter, "Pick Up Korean BBQ on Rice");
                        return;
                    }
                    if (counter != null && counter.heldItem != null && counter.heldItem.itemId == "cooked_rice")
                    {
                        // Holding counter has rice, waiting for BBQ beef from Pass Counter
                        SetTarget(GetStation("pass_counter"), "Standby at Pass Counter for BBQ Beef");
                        return;
                    }
                    if (stove != null && stove.heldItem == null)
                    {
                        desiredItemId = "rice_portioned";
                        SetTarget(GetStation("shelf_west"), "Get Rice Portion");
                        return;
                    }
                }
                else
                {
                    if (playerController.carriedItem.itemId == "rice_portioned")
                    {
                        SetTarget(stove, "Cook Rice on Stove");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "cooked_rice")
                    {
                        SetTarget(counter, "Place Steamed Rice on Table");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "bbq_beef" || playerController.carriedItem.itemId == "bulgogi")
                    {
                        SetTarget(counter, "Assemble BBQ Beef with Rice");
                        return;
                    }
                }
            }
        }

        // --- DAY 4: VIET BROKEN RICE ---
        private void PlanVietBrokenRice()
        {
            if (characterId == "jiwon")
            {
                // Jiwon: Preps & grills BBQ beef -> passes to Linh
                PlanKoreanBBQRice();
            }
            else
            {
                // Linh: Steamed rice + egg + BBQ beef -> Viet Broken Rice
                CookingStation stove = GetStation("noodle_blancher") as CookingStation;
                CounterStation counter = GetStation("counter_west") as CounterStation;

                if (playerController.carriedItem == null)
                {
                    if (counter != null && counter.heldItem != null && counter.heldItem.itemId == "viet_broken_rice")
                    {
                        SetTarget(counter, "Pick Up Viet Broken Rice");
                        return;
                    }
                    if (counter != null && counter.heldItem != null && counter.heldItem.itemId == "cooked_rice")
                    {
                        desiredItemId = "egg_raw";
                        SetTarget(GetStation("fridge_west"), "Get Egg for Broken Rice");
                        return;
                    }
                    if (stove != null && stove.heldItem != null)
                    {
                        if (stove.currentState == CookingState.OPTIMAL || stove.currentState == CookingState.OVERCOOKED)
                        {
                            SetTarget(stove, "Pick Up Steamed Rice");
                            return;
                        }
                        else
                        {
                            SetTarget(stove, "Wait for Rice to Steam");
                            return;
                        }
                    }
                    if (stove != null && stove.heldItem == null && (counter == null || counter.heldItem == null))
                    {
                        desiredItemId = "rice_portioned";
                        SetTarget(GetStation("shelf_west"), "Get Rice Portion");
                        return;
                    }
                }
                else
                {
                    if (playerController.carriedItem.itemId == "rice_portioned")
                    {
                        SetTarget(stove, "Cook Rice on Stove");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "cooked_rice")
                    {
                        SetTarget(counter, "Place Steamed Rice on Table");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "egg_raw")
                    {
                        SetTarget(counter, "Add Egg to Broken Rice on Table");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "bbq_beef" || playerController.carriedItem.itemId == "bulgogi")
                    {
                        SetTarget(counter, "Assemble BBQ Beef with Broken Rice");
                        return;
                    }
                }
            }
        }

        // --- DAY 5: BULGOGI BÁNH MÌ ---
        private void PlanBulgogiBanhMi()
        {
            if (characterId == "linh")
            {
                // Linh: Bakes Baguette & Preps Pickled Veg -> Assembles with Jiwon's Bulgogi
                PrepStation prep = GetStation("prep_west") as PrepStation;
                CookingStation stove = GetStation("stockpot") as CookingStation;
                CounterStation counter = GetStation("counter_west") as CounterStation;

                if (playerController.carriedItem == null)
                {
                    if (counter != null && counter.heldItem != null && counter.heldItem.itemId == "bulgogi_banh_mi")
                    {
                        SetTarget(counter, "Pick Up Bulgogi Bánh Mì");
                        return;
                    }
                    if (prep != null && prep.heldItem != null)
                    {
                        if (prep.strikesCompleted >= PrepStation.REQUIRED_STRIKES)
                        {
                            SetTarget(prep, "Pick Up Pickled Veg");
                            return;
                        }
                        else
                        {
                            SetTarget(prep, "Chop Radish & Carrots");
                            return;
                        }
                    }
                    if (stove != null && stove.heldItem != null)
                    {
                        if (stove.currentState == CookingState.OPTIMAL || stove.currentState == CookingState.OVERCOOKED)
                        {
                            SetTarget(stove, "Pick Up Baked Baguette");
                            return;
                        }
                        else
                        {
                            SetTarget(stove, "Wait for Baguette to Bake");
                            return;
                        }
                    }
                    if (counter == null || counter.heldItem == null)
                    {
                        desiredItemId = "radish_carrot";
                        SetTarget(GetStation("fridge_west"), "Get Radish for Pickling");
                        return;
                    }
                    if (counter != null && counter.heldItem.itemId == "pickled_veg" && stove != null && stove.heldItem == null)
                    {
                        desiredItemId = "flour";
                        SetTarget(GetStation("shelf_west"), "Get Flour for Baguette");
                        return;
                    }
                }
                else
                {
                    if (playerController.carriedItem.itemId == "radish_carrot")
                    {
                        SetTarget(prep, "Cut Radish & Carrots at Prep Table");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "pickled_veg")
                    {
                        SetTarget(counter, "Place Pickled Veg on Table");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "flour")
                    {
                        SetTarget(stove, "Place Flour in Oven");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "egg_raw")
                    {
                        SetTarget(stove, "Add Egg to Baguette Dough");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "baguette")
                    {
                        SetTarget(counter, "Assemble Baguette with Pickled Veg");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "bulgogi" || playerController.carriedItem.itemId == "bulgogi_seared")
                    {
                        SetTarget(counter, "Assemble Bulgogi into Bánh Mì");
                        return;
                    }
                }
            }
            else
            {
                // Jiwon: Cuts raw meat 2x into bulgogi portion, grills, and passes to Linh
                PrepStation prep = GetStation("prep_east") as PrepStation;
                CookingStation grill = GetStation("grill_pan") as CookingStation;

                if (playerController.carriedItem == null)
                {
                    if (grill != null && grill.heldItem != null)
                    {
                        if (grill.currentState == CookingState.OPTIMAL || grill.currentState == CookingState.OVERCOOKED)
                        {
                            SetTarget(grill, "Pick Up Grilled Bulgogi");
                            return;
                        }
                        else
                        {
                            SetTarget(grill, "Monitor Bulgogi Grilling");
                            return;
                        }
                    }
                    if (prep != null && prep.heldItem != null)
                    {
                        if (prep.strikesCompleted < PrepStation.REQUIRED_STRIKES)
                        {
                            SetTarget(prep, "Chop Meat for Bulgogi");
                            return;
                        }
                        if (prep.heldItem.itemId == "korean_bbq_raw")
                        {
                            desiredItemId = "bulgogi_portioned";
                            SetTarget(prep, "Cut Meat Second Time for Bulgogi");
                            return;
                        }
                        SetTarget(prep, "Pick Up Bulgogi Portion");
                        return;
                    }
                    if (grill != null && grill.heldItem == null)
                    {
                        desiredItemId = "unprocessed_raw_meat";
                        SetTarget(GetStation("fridge_east"), "Get Meat for Bulgogi");
                        return;
                    }
                }
                else
                {
                    if (playerController.carriedItem.itemId == "unprocessed_raw_meat")
                    {
                        SetTarget(prep, "Cut Raw Meat First Time");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "korean_bbq_raw")
                    {
                        desiredItemId = "bulgogi_portioned";
                        SetTarget(prep, "Cut Meat Second Time");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "bulgogi_portioned")
                    {
                        SetTarget(grill, "Grill Bulgogi Portion");
                        return;
                    }
                    if (playerController.carriedItem.itemId == "bulgogi" || playerController.carriedItem.itemId == "bulgogi_seared")
                    {
                        SetTarget(GetStation("pass_counter"), "Pass Bulgogi to Linh");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Executes TAS-precision waypoint navigation and frame-accurate station interactions.
        /// </summary>
        private void ExecuteMovementAndAction(float dt)
        {
            if (targetStation == null)
            {
                playerController.ApplyMovement(Vector2.zero, dt);
                return;
            }

            if (targetStation != lastTargetStation || currentPath == null || currentPath.Count == 0)
            {
                UpdatePathToTarget();
            }

            if (currentPath == null || currentPath.Count == 0)
            {
                UpdatePathToTarget();
            }

            // 1. Waypoint Progression along A* Path
            if (currentPath != null && currentWaypointIndex < currentPath.Count)
            {
                Vector3 wp = currentPath[currentWaypointIndex];
                Vector3 diff = wp - transform.position;
                diff.y = 0.0f;
                float distToWp = diff.magnitude;

                bool isFinalWp = (currentWaypointIndex == currentPath.Count - 1);
                float arrivalThreshold = isFinalWp ? 0.35f : 0.45f;

                if (distToWp <= arrivalThreshold)
                {
                    currentWaypointIndex++;
                }
            }

            // 2. Active Steering toward Current Waypoint
            if (currentPath != null && currentWaypointIndex < currentPath.Count)
            {
                Vector3 wp = currentPath[currentWaypointIndex];
                Vector3 diff = wp - transform.position;
                diff.y = 0.0f;

                // TAS Direction: Compensates for PlayerController 1.25x vertical scaling (camera foreshortening)
                Vector2 moveDir = new Vector2(diff.x, diff.z / 1.25f).normalized;
                playerController.ApplyMovement(moveDir, dt);
                playerController.facingDirection = new Vector2(diff.x, diff.z).normalized;
                return;
            }

            // 3. Arrived at Station Approach Position
            playerController.ApplyMovement(Vector2.zero, dt);

            Vector3 toStation = targetStation.transform.position - transform.position;
            toStation.y = 0.0f;
            if (toStation.sqrMagnitude > 0.001f)
            {
                playerController.facingDirection = new Vector2(toStation.x, toStation.z).normalized;
            }

            // 4. Frame-Accurate Station Interactions (Guarded by 1.0s Debounce Delay)
            if (interactCooldown > 0.0f)
            {
                return;
            }

            if (targetStation is PrepStation prep)
            {
                if (prep.heldItem == null && playerController.carriedItem != null)
                {
                    var res = playerController.TryInteractPrimary(targetStation);
                    BotLogger.LogAction(characterId, $"Place '{playerController.carriedItem?.itemId}' on Prep", targetStation.stationId, res.ok, res.reason);
                    if (res.ok)
                    {
                        interactCooldown = INTERACTION_DELAY;
                    }
                }
                else if (prep.heldItem != null)
                {
                    if (prep.strikesCompleted < PrepStation.REQUIRED_STRIKES)
                    {
                        chopTimer += dt;
                        if (chopTimer >= chopCadenceInterval)
                        {
                            chopTimer = 0.0f;
                            var res = playerController.TryInteractSecondary(targetStation);
                            BotLogger.LogAction(characterId, $"Chop Strike ({prep.strikesCompleted}/{PrepStation.REQUIRED_STRIKES})", targetStation.stationId, res.ok, res.reason);
                        }
                    }
                    else
                    {
                        // Check if a 2nd cut cycle is needed (e.g. Day 5 Jiwon cuts korean_bbq_raw -> bulgogi_portioned)
                        if (prep.heldItem.itemId == "korean_bbq_raw" && desiredItemId == "bulgogi_portioned")
                        {
                            chopTimer += dt;
                            if (chopTimer >= chopCadenceInterval)
                            {
                                chopTimer = 0.0f;
                                var res = playerController.TryInteractSecondary(targetStation);
                                BotLogger.LogAction(characterId, "Chop 2nd Cut Strike", targetStation.stationId, res.ok, res.reason);
                            }
                        }
                        else
                        {
                            var res = playerController.TryInteractPrimary(targetStation);
                            BotLogger.LogAction(characterId, "Pick up Prepped Item", targetStation.stationId, res.ok, res.reason);
                            if (res.ok)
                            {
                                interactCooldown = INTERACTION_DELAY;
                            }
                        }
                    }
                }
            }
            else if (targetStation is CookingStation cook)
            {
                if (cook.currentState == CookingState.OPTIMAL || cook.currentState == CookingState.OVERCOOKED)
                {
                    if (playerController.carriedItem == null)
                    {
                        var res = playerController.TryInteractPrimary(targetStation);
                        BotLogger.LogAction(characterId, $"Pick up Cooked '{cook.heldItem?.itemId}'", targetStation.stationId, res.ok, res.reason);
                        if (res.ok)
                        {
                            interactCooldown = INTERACTION_DELAY;
                        }
                    }
                }
                else if (cook.heldItem == null && playerController.carriedItem != null)
                {
                    var res = playerController.TryInteractPrimary(targetStation);
                    BotLogger.LogAction(characterId, $"Place '{playerController.carriedItem?.itemId}' into Cooker", targetStation.stationId, res.ok, res.reason);
                    if (res.ok)
                    {
                        recentlyPlacedCooldowns[targetStation] = PLACED_ITEM_COOLDOWN;
                        interactCooldown = INTERACTION_DELAY;
                    }
                }
                else if (cook.heldItem != null && playerController.carriedItem != null)
                {
                    var res = playerController.TryInteractPrimary(targetStation);
                    BotLogger.LogAction(characterId, $"Add '{playerController.carriedItem?.itemId}' to Cooker", targetStation.stationId, res.ok, res.reason);
                    if (res.ok)
                    {
                        interactCooldown = INTERACTION_DELAY;
                    }
                }
            }
            else if (targetStation is PassCounter pass)
            {
                if (playerController.carriedItem != null)
                {
                    string passedItemId = playerController.carriedItem.itemId;
                    var res = playerController.TryInteractPrimary(targetStation);
                    BotLogger.LogAction(characterId, $"Pass '{passedItemId}'", targetStation.stationId, res.ok, res.reason);
                    if (res.ok)
                    {
                        interactCooldown = INTERACTION_DELAY;
                        for (int s = 0; s < pass.slots.Length; s++)
                        {
                            if (pass.slots[s] != null && pass.slots[s].item != null && pass.slots[s].placedByPeer == peerId)
                            {
                                BotCommunicationHub.PostHandoff(s, passedItemId, peerId, activeRecipeId);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Only retrieve if there is a pending handoff for this bot or an item from partner chef
                    var handoff = BotCommunicationHub.GetPendingHandoffFor(peerId);
                    bool hasPartnerItem = false;
                    for (int s = 0; s < pass.slots.Length; s++)
                    {
                        if (pass.slots[s] != null && pass.slots[s].item != null && pass.slots[s].placedByPeer != peerId)
                        {
                            hasPartnerItem = true;
                            break;
                        }
                    }

                    if (handoff != null || hasPartnerItem)
                    {
                        int slotToClaim = handoff != null ? handoff.slotIndex : -1;
                        var res = playerController.TryInteractPrimary(targetStation);
                        BotLogger.LogAction(characterId, "Retrieve from Pass Counter", targetStation.stationId, res.ok, res.reason);
                        if (res.ok)
                        {
                            interactCooldown = INTERACTION_DELAY;
                            if (slotToClaim >= 0)
                            {
                                BotCommunicationHub.ClaimHandoff(slotToClaim, peerId);
                            }
                        }
                    }
                }
            }
            else if (targetStation is CounterStation counter)
            {
                if (playerController.carriedItem != null)
                {
                    var res = playerController.TryInteractPrimary(targetStation);
                    BotLogger.LogAction(characterId, $"Place '{playerController.carriedItem?.itemId}' on Table", targetStation.stationId, res.ok, res.reason);
                    if (res.ok)
                    {
                        recentlyPlacedCooldowns[targetStation] = PLACED_ITEM_COOLDOWN;
                        interactCooldown = INTERACTION_DELAY;
                    }
                }
                else
                {
                    if (!recentlyPlacedCooldowns.ContainsKey(targetStation))
                    {
                        var res = playerController.TryInteractPrimary(targetStation);
                        BotLogger.LogAction(characterId, "Pick up from Table", targetStation.stationId, res.ok, res.reason);
                        if (res.ok)
                        {
                            interactCooldown = INTERACTION_DELAY;
                        }
                    }
                }
            }
            else if (targetStation is CrateStation crate)
            {
                if (playerController.carriedItem == null && !string.IsNullOrEmpty(desiredItemId))
                {
                    var res = crate.DispenseItemDirect(ref playerController.carriedItem, desiredItemId);
                    BotLogger.LogAction(characterId, $"Dispense '{desiredItemId}'", crate.stationId, res.ok, res.reason);
                    playerController.RefreshVisuals();
                    desiredItemId = null;
                    interactCooldown = INTERACTION_DELAY;
                }
            }
            else
            {
                var res = playerController.TryInteractPrimary(targetStation);
                BotLogger.LogAction(characterId, $"Interact [{targetStation.stationId}]", targetStation.stationId, res.ok, res.reason);
                if (res.ok)
                {
                    interactCooldown = INTERACTION_DELAY;
                }
            }
        }

        private CookingStation FindUrgentPan()
        {
            if (KitchenController.Instance == null) return null;
            foreach (var st in KitchenController.Instance.stations.Values)
            {
                if (st is CookingStation cook && cook.heldItem != null)
                {
                    // Only OVERCOOKED (in imminent danger of burning into waste) triggers an urgent pan rescue!
                    // OPTIMAL is the normal recipe completion/combination window and must not trigger false panic aborts.
                    if (cook.currentState == CookingState.OVERCOOKED)
                    {
                        // Check if station belongs to this bot's zone or is shared
                        if (peerId == 1 && cook.transform.position.x <= 0f) return cook;
                        if (peerId == 2 && cook.transform.position.x >= 0f) return cook;
                    }
                }
            }
            return null;
        }

        private StationBase FindNearestHoldingCounter()
        {
            if (KitchenController.Instance == null) return null;
            StationBase best = null;
            float minD = 999f;
            foreach (var st in KitchenController.Instance.stations.Values)
            {
                if (st is CounterStation c && c.heldItem == null)
                {
                    // Prefer counter on bot's own side
                    if (peerId == 1 && st.transform.position.x > 0f) continue;
                    if (peerId == 2 && st.transform.position.x < 0f) continue;

                    float d = Vector3.Distance(st.transform.position, transform.position);
                    if (d < minD)
                    {
                        minD = d;
                        best = st;
                    }
                }
            }
            return best;
        }

        private StationBase GetStation(string stationId)
        {
            if (KitchenController.Instance != null && KitchenController.Instance.stations.TryGetValue(stationId, out var st))
            {
                return st;
            }
            return null;
        }

        private OrderTicket GetMostUrgentOrder()
        {
            if (ShiftManager.Instance == null || ShiftManager.Instance.activeOrders == null || ShiftManager.Instance.activeOrders.Count == 0)
                return null;

            OrderTicket mostUrgent = null;
            float lowestTime = float.MaxValue;
            for (int i = 0; i < ShiftManager.Instance.activeOrders.Count; i++)
            {
                var o = ShiftManager.Instance.activeOrders[i];
                if (!o.isCompleted && !o.isFailed && o.timeRemaining < lowestTime)
                {
                    lowestTime = o.timeRemaining;
                    mostUrgent = o;
                }
            }
            return mostUrgent;
        }

        private string GetDefaultDayRecipe()
        {
            int day = ShiftManager.Instance != null ? ShiftManager.Instance.currentStageId : 1;
            switch (day)
            {
                case 1: return "tteokbokki";
                case 2: return "bot_chien";
                case 3: return "korean_bbq_rice";
                case 4: return "viet_broken_rice";
                case 5: return "bulgogi_banh_mi";
                default: return "tteokbokki";
            }
        }

        private bool IsCompletedDish(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            return itemId == "tteokbokki" || itemId == "bot_chien" || itemId == "korean_bbq_rice" || itemId == "viet_broken_rice" || itemId == "bulgogi_banh_mi";
        }
    }
}
