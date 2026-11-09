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
    /// Controls the non-human chef in Solo Mode (or both chefs in automated QA Autoplay).
    /// Runs on identical physics, stations, and interaction rules as human players.
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
        public float goalTimer = 0.0f;
        public float stuckTimer = 0.0f;

        [Header("Components")]
        public PlayerController playerController;

        // Cadence & Decision Timing
        private float decisionInterval = 0.25f;
        private float chopCadenceInterval = 0.45f;
        private float decisionTimer = 0.0f;
        private float chopTimer = 0.0f;
        private float workerHireCooldown = 0.0f;
        private Vector3 lastRecordedPosition = Vector3.zero;

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
                playerController.isLocalPlayer = false; // Bot drives input directly
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
                    chopCadenceInterval = 0.35f;
                    break;
                case BotDifficulty.Standard:
                    decisionInterval = 0.25f;
                    chopCadenceInterval = 0.45f;
                    break;
                case BotDifficulty.Challenge:
                    decisionInterval = 0.38f;
                    chopCadenceInterval = 0.55f;
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

            // Anti-Stuck Watchdog
            if (Vector3.Distance(transform.position, lastRecordedPosition) < 0.08f && targetStation != null)
            {
                stuckTimer += dt;
                if (stuckTimer > 2.6f)
                {
                    // If stuck trying to reach a station, abort goal and re-evaluate
                    targetStation = null;
                    desiredItemId = null;
                    stuckTimer = 0.0f;
                    goalTimer = 0.0f;
                }
            }
            else
            {
                stuckTimer = 0.0f;
                lastRecordedPosition = transform.position;
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
        /// Evaluates all available kitchen tasks deterministically and selects the highest-utility action.
        /// </summary>
        public void EvaluateAndSelectGoal()
        {
            if (KitchenController.Instance == null) return;

            // 1. URGENT OVERRIDE: Pan in danger of overcooking / burning (Score: 300)
            CookingStation urgentPan = FindUrgentPan();
            if (urgentPan != null)
            {
                if (playerController.carriedItem != null)
                {
                    // Hands full: Deposit raw ingredient on nearest holding counter first
                    StationBase dropCounter = FindNearestHoldingCounter();
                    if (dropCounter != null)
                    {
                        targetStation = dropCounter;
                        currentGoalName = "Drop Item for Pan Rescue";
                        return;
                    }
                }
                targetStation = urgentPan;
                currentGoalName = $"Rescue Pan: {urgentPan.stationId}";
                return;
            }

            // 2. DELIVER / PASS COMPLETED DISH (Score: 260)
            if (playerController.carriedItem != null && IsCompletedDish(playerController.carriedItem.itemId))
            {
                if (characterId == "linh" || transform.position.x < -1.0f)
                {
                    // Linh serves dish directly at Serving Window
                    StationBase serveWindow = GetStation("serving_station");
                    if (serveWindow != null)
                    {
                        targetStation = serveWindow;
                        currentGoalName = "Deliver Dish to Serving Window";
                        return;
                    }
                }
                else
                {
                    // Jiwon passes completed dish to Linh via Pass Counter
                    StationBase passCounter = GetStation("pass_counter");
                    if (passCounter != null)
                    {
                        targetStation = passCounter;
                        currentGoalName = "Pass Completed Dish to Linh";
                        return;
                    }
                }
            }

            // 3. RETRIEVE FROM PASS COUNTER (Score: 220)
            if (playerController.carriedItem == null)
            {
                PassCounter pass = GetStation("pass_counter") as PassCounter;
                if (pass != null)
                {
                    for (int i = 0; i < pass.slots.Length; i++)
                    {
                        var slot = pass.slots[i];
                        if (slot != null && slot.item != null && slot.placedByPeer != peerId)
                        {
                            // If Linh sees a dish in pass counter, take it to serve!
                            if (characterId == "linh" && IsCompletedDish(slot.item.itemId))
                            {
                                targetStation = pass;
                                currentGoalName = "Retrieve Dish from Pass Counter";
                                return;
                            }
                            // If Jiwon sees an ingredient passed by Linh, take it!
                            if (characterId == "jiwon" && !IsCompletedDish(slot.item.itemId))
                            {
                                targetStation = pass;
                                currentGoalName = "Retrieve Ingredient from Pass Counter";
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
                        workerHireCooldown = 15.0f;
                    }
                }
            }

            // 5. CULINARY ORDER PIPELINE (Score: 150)
            OrderTicket urgentTicket = GetMostUrgentOrder();
            if (urgentTicket != null)
            {
                PlanOrderTask(urgentTicket.recipeId);
                return;
            }

            // Default Idle
            currentGoalName = "Idle / Standby";
            targetStation = null;
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

        private void PlanTteokbokki()
        {
            CookingStation wok = (characterId == "jiwon" ? GetStation("simmer_wok") : GetStation("noodle_blancher")) as CookingStation;
            if (wok == null) return;

            // Step 1: Empty pan needs rice cakes
            if (wok.heldItem == null)
            {
                if (playerController.carriedItem == null)
                {
                    targetStation = characterId == "jiwon" ? GetStation("fridge_east") : GetStation("shelf_west");
                    desiredItemId = "tteok_portioned";
                    currentGoalName = "Get Rice Cakes for Tteokbokki";
                }
                else if (playerController.carriedItem.itemId == "tteok_portioned")
                {
                    targetStation = wok;
                    currentGoalName = "Place Rice Cakes in Pan";
                }
                return;
            }

            // Step 2: Pan has rice cakes, needs Gochujang
            if (wok.heldItem.itemId == "tteok_portioned" || wok.heldItem.itemId == "tteok_raw")
            {
                if (playerController.carriedItem == null)
                {
                    targetStation = characterId == "jiwon" ? GetStation("shelf_east") : GetStation("shelf_west");
                    desiredItemId = "gochujang";
                    currentGoalName = "Get Gochujang for Tteokbokki";
                }
                else if (playerController.carriedItem.itemId == "gochujang")
                {
                    targetStation = wok;
                    currentGoalName = "Add Gochujang to Pan";
                }
                return;
            }

            // Step 3: Simmering -> wait or pick up when ready
            if (wok.currentState == CookingState.OPTIMAL)
            {
                if (playerController.carriedItem == null)
                {
                    targetStation = wok;
                    currentGoalName = "Pick Up Cooked Tteokbokki";
                }
            }
        }

        private void PlanBotChien()
        {
            CookingStation pan = (characterId == "linh" ? GetStation("noodle_blancher") : GetStation("simmer_wok")) as CookingStation;
            if (pan == null) return;

            if (pan.heldItem == null)
            {
                if (playerController.carriedItem == null)
                {
                    targetStation = characterId == "linh" ? GetStation("shelf_west") : GetStation("shelf_east");
                    desiredItemId = "flour";
                    currentGoalName = "Get Rice Flour for Bột Chiên";
                }
                else if (playerController.carriedItem.itemId == "flour")
                {
                    targetStation = pan;
                    currentGoalName = "Place Flour in Pan";
                }
                return;
            }

            if (pan.heldItem.itemId == "flour" || pan.heldItem.itemId == "cooked_flour")
            {
                if (playerController.carriedItem == null)
                {
                    targetStation = characterId == "linh" ? GetStation("fridge_west") : GetStation("fridge_east");
                    desiredItemId = "egg_raw";
                    currentGoalName = "Get Egg for Bột Chiên";
                }
                else if (playerController.carriedItem.itemId == "egg_raw")
                {
                    targetStation = pan;
                    currentGoalName = "Add Egg to Cooked Flour";
                }
                return;
            }

            if (pan.currentState == CookingState.OPTIMAL)
            {
                if (playerController.carriedItem == null)
                {
                    targetStation = pan;
                    currentGoalName = "Pick Up Bột Chiên";
                }
            }
        }

        private void PlanKoreanBBQRice()
        {
            if (characterId == "jiwon")
            {
                // Jiwon: Cut meat at East Prep Table and grill on East Grill
                PrepStation prep = GetStation("prep_east") as PrepStation;
                CookingStation grill = GetStation("grill_pan") as CookingStation;

                if (playerController.carriedItem == null)
                {
                    if (prep != null && prep.heldItem != null && prep.strikesCompleted >= PrepStation.REQUIRED_STRIKES)
                    {
                        targetStation = prep;
                        currentGoalName = "Pick Up Sliced BBQ Meat";
                        return;
                    }
                    if (grill != null && grill.currentState == CookingState.OPTIMAL)
                    {
                        targetStation = grill;
                        currentGoalName = "Pick Up Grilled BBQ Beef";
                        return;
                    }
                    targetStation = GetStation("fridge_east");
                    desiredItemId = "unprocessed_raw_meat";
                    currentGoalName = "Get Raw Meat from Fridge";
                    return;
                }

                if (playerController.carriedItem.itemId == "unprocessed_raw_meat")
                {
                    targetStation = prep;
                    currentGoalName = "Cut Raw Meat at Prep Table";
                    return;
                }

                if (playerController.carriedItem.itemId == "korean_bbq_raw")
                {
                    targetStation = grill;
                    currentGoalName = "Grill Sliced BBQ Meat";
                    return;
                }

                if (playerController.carriedItem.itemId == "bulgogi" || playerController.carriedItem.itemId == "bbq_beef")
                {
                    targetStation = GetStation("pass_counter");
                    currentGoalName = "Pass Grilled Beef to Linh";
                    return;
                }
            }
            else
            {
                // Linh: Cook rice on West stove, assemble with beef, serve
                CookingStation stove = GetStation("noodle_blancher") as CookingStation;
                if (stove == null) return;

                if (stove.heldItem == null)
                {
                    if (playerController.carriedItem == null)
                    {
                        targetStation = GetStation("shelf_west");
                        desiredItemId = "rice_portioned";
                        currentGoalName = "Get Rice Portion";
                    }
                    else if (playerController.carriedItem.itemId == "rice_portioned")
                    {
                        targetStation = stove;
                        currentGoalName = "Cook Rice on Stove";
                    }
                    return;
                }

                if (stove.currentState == CookingState.OPTIMAL && playerController.carriedItem == null)
                {
                    targetStation = stove;
                    currentGoalName = "Pick Up Steamed Rice";
                    return;
                }

                if (playerController.carriedItem != null && playerController.carriedItem.itemId == "cooked_rice")
                {
                    // Check if counter has bbq_beef to combine
                    CounterStation counter = GetStation("counter_west") as CounterStation;
                    if (counter != null && counter.heldItem != null && (counter.heldItem.itemId == "bulgogi" || counter.heldItem.itemId == "bbq_beef"))
                    {
                        targetStation = counter;
                        currentGoalName = "Assemble BBQ Beef with Steamed Rice";
                        return;
                    }
                    // Place rice on holding table
                    targetStation = counter;
                    currentGoalName = "Place Rice on Holding Table";
                }
            }
        }

        private void PlanVietBrokenRice()
        {
            // Similar pipeline to Korean BBQ Rice + Egg
            PlanKoreanBBQRice();
        }

        private void PlanBulgogiBanhMi()
        {
            if (characterId == "linh")
            {
                // Linh: Bake Baguette (flour + egg) or prep pickled vegetables
                PrepStation prep = GetStation("prep_west") as PrepStation;
                CookingStation stove = GetStation("stockpot") as CookingStation;

                if (playerController.carriedItem == null)
                {
                    if (prep != null && prep.heldItem != null && prep.strikesCompleted >= PrepStation.REQUIRED_STRIKES)
                    {
                        targetStation = prep;
                        currentGoalName = "Pick Up Pickled Veg";
                        return;
                    }
                    if (stove != null && stove.currentState == CookingState.OPTIMAL)
                    {
                        targetStation = stove;
                        currentGoalName = "Pick Up Baked Baguette";
                        return;
                    }

                    targetStation = GetStation("fridge_west");
                    desiredItemId = "radish_carrot";
                    currentGoalName = "Get Radish for Pickling";
                    return;
                }

                if (playerController.carriedItem.itemId == "radish_carrot")
                {
                    targetStation = prep;
                    currentGoalName = "Chop Radish & Carrots";
                    return;
                }

                if (playerController.carriedItem.itemId == "pickled_veg" || playerController.carriedItem.itemId == "baguette")
                {
                    targetStation = GetStation("counter_west");
                    currentGoalName = "Assemble Bulgogi Bánh Mì on Counter";
                    return;
                }
            }
            else
            {
                // Jiwon: Double-cut meat into bulgogi portion, grill, and pass to Linh
                PrepStation prep = GetStation("prep_east") as PrepStation;
                CookingStation grill = GetStation("grill_pan") as CookingStation;

                if (playerController.carriedItem == null)
                {
                    if (prep != null && prep.heldItem != null && prep.strikesCompleted >= PrepStation.REQUIRED_STRIKES)
                    {
                        targetStation = prep;
                        currentGoalName = "Pick Up Bulgogi Portion";
                        return;
                    }
                    if (grill != null && grill.currentState == CookingState.OPTIMAL)
                    {
                        targetStation = grill;
                        currentGoalName = "Pick Up Grilled Bulgogi";
                        return;
                    }

                    targetStation = GetStation("fridge_east");
                    desiredItemId = "unprocessed_raw_meat";
                    currentGoalName = "Get Meat for Bulgogi";
                    return;
                }

                if (playerController.carriedItem.itemId == "unprocessed_raw_meat" || playerController.carriedItem.itemId == "korean_bbq_raw")
                {
                    targetStation = prep;
                    currentGoalName = "Cut Meat for Bulgogi";
                    return;
                }

                if (playerController.carriedItem.itemId == "bulgogi_portioned")
                {
                    targetStation = grill;
                    currentGoalName = "Grill Bulgogi Portion";
                    return;
                }

                if (playerController.carriedItem.itemId == "bulgogi")
                {
                    targetStation = GetStation("pass_counter");
                    currentGoalName = "Pass Bulgogi Beef to Linh";
                    return;
                }
            }
        }

        private void ExecuteMovementAndAction(float dt)
        {
            if (targetStation == null) return;

            Vector3 toStation = targetStation.transform.position - transform.position;
            toStation.y = 0.0f;
            float dist = toStation.magnitude;

            // Anti-Interference Partner Clearance
            PlayerController partner = GetPartnerChef();
            if (partner != null)
            {
                float partnerDist = Vector3.Distance(partner.transform.position, targetStation.transform.position);
                // If partner is actively interacting at this station, hold slightly back
                if (partnerDist < 1.4f && dist > 1.8f)
                {
                    return;
                }
            }

            // Movement towards station
            if (dist > 1.65f)
            {
                Vector2 moveDir = new Vector2(toStation.x, toStation.z).normalized;
                playerController.ApplyMovement(moveDir, dt);
                return;
            }

            // Arrived within reach -> Face station and execute interaction
            playerController.facingDirection = new Vector2(toStation.x, toStation.z).normalized;

            if (targetStation is PrepStation prep)
            {
                if (prep.heldItem == null && playerController.carriedItem != null)
                {
                    playerController.TryInteractPrimary();
                }
                else if (prep.heldItem != null)
                {
                    if (prep.strikesCompleted < PrepStation.REQUIRED_STRIKES)
                    {
                        chopTimer += dt;
                        if (chopTimer >= chopCadenceInterval)
                        {
                            chopTimer = 0.0f;
                            playerController.TryInteractSecondary();
                        }
                    }
                    else
                    {
                        // Check if a second cut cycle is needed (e.g. korean_bbq_raw -> bulgogi_portioned)
                        if (prep.heldItem.itemId == "korean_bbq_raw" && desiredItemId == "bulgogi_portioned")
                        {
                            chopTimer += dt;
                            if (chopTimer >= chopCadenceInterval)
                            {
                                chopTimer = 0.0f;
                                playerController.TryInteractSecondary();
                            }
                        }
                        else
                        {
                            playerController.TryInteractPrimary();
                        }
                    }
                }
            }
            else if (targetStation is CookingStation cook)
            {
                if (cook.currentState == CookingState.OPTIMAL || cook.currentState == CookingState.OVERCOOKED)
                {
                    playerController.TryInteractPrimary();
                }
                else if (cook.heldItem == null && playerController.carriedItem != null)
                {
                    playerController.TryInteractPrimary();
                }
                else if (cook.heldItem != null && playerController.carriedItem != null)
                {
                    playerController.TryInteractPrimary();
                }
            }
            else if (targetStation is CrateStation crate)
            {
                if (playerController.carriedItem == null && !string.IsNullOrEmpty(desiredItemId))
                {
                    crate.DispenseItemDirect(ref playerController.carriedItem, desiredItemId);
                    playerController.RefreshVisuals();
                    desiredItemId = null;
                }
            }
            else
            {
                playerController.TryInteractPrimary();
            }
        }

        private CookingStation FindUrgentPan()
        {
            if (KitchenController.Instance == null) return null;
            foreach (var st in KitchenController.Instance.stations.Values)
            {
                if (st is CookingStation cook && cook.heldItem != null)
                {
                    // Check if pan is on bot's zone or accessible
                    if (cook.currentState == CookingState.OPTIMAL || cook.currentState == CookingState.OVERCOOKED)
                    {
                        return cook;
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

        private PlayerController GetPartnerChef()
        {
            if (KitchenController.Instance == null) return null;
            int partnerPeer = (peerId == 1) ? 2 : 1;
            KitchenController.Instance.playersByPeer.TryGetValue(partnerPeer, out var p);
            return p;
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

        private bool IsCompletedDish(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            return itemId == "tteokbokki" || itemId == "bot_chien" || itemId == "korean_bbq_rice" || itemId == "viet_broken_rice" || itemId == "bulgogi_banh_mi";
        }
    }
}
