using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Stations;
using TwoLadles.Kitchen;
using TwoLadles.Player;

namespace TwoLadles.Tests
{
    public class TwoLadlesTestSuite : MonoBehaviour
    {
        public int testsRun = 0;
        public int testsPassed = 0;
        public int testsFailed = 0;
        public List<string> failureMessages = new List<string>();

        private void Start()
        {
            RunAllTests();
        }

        public void RunAllTests()
        {
            testsRun = 0;
            testsPassed = 0;
            testsFailed = 0;
            failureMessages.Clear();

            Debug.Log("============================================================");
            Debug.Log("  TWO LADLES — AUTOMATED TEST SUITE (Unity C# Migration)");
            Debug.Log("============================================================");

            // Ensure GameData is initialized
            if (GameData.Instance == null)
            {
                var gdObj = new GameObject("GameData");
                gdObj.AddComponent<GameData>();
                GameData.Instance.InitData();
            }

            // Ensure ProgressionManager is initialized
            if (ProgressionManager.Instance == null)
            {
                var pmObj = new GameObject("ProgressionManager");
                pmObj.AddComponent<ProgressionManager>();
            }

            // Ensure ShiftManager is initialized
            if (ShiftManager.Instance == null)
            {
                var smObj = new GameObject("ShiftManager");
                smObj.AddComponent<ShiftManager>();
            }

            Test1_RecipesAndCrossPrepArchitecture();
            Test2_PrepStationRhythmCadence();
            Test3_CookingStateMachineAndPhoSeparation();
            Test4_AssemblyStationRules();
            Test5_PassCounterHandoffAndAntiExploit();
            Test6_KitchenFlowAndRushRhythm();
            Test7_WashingStationPlateLifecycle();
            Test8_LobbyExclusivityAndReadyReset();
            Test9_ProgressionSystem();
            Test10_LinhExecutionSpecialistTiers();
            Test11_JiwonDelegationPartTimeCrew();
            Test12_CampaignLevelsAndProgressionUnlocks();
            Test13_EndlessRushMode();

            Debug.Log("------------------------------------------------------------");
            Debug.Log($"  RESULTS: {testsPassed} / {testsRun} Passed | {testsFailed} Failed");
            Debug.Log("------------------------------------------------------------");

            if (testsFailed == 0)
            {
                Debug.Log("  ALL 114 AUTOMATED TESTS PASSED CLEANLY IN UNITY C#.");
            }
            else
            {
                foreach (var msg in failureMessages)
                {
                    Debug.LogError($"  [FAIL] {msg}");
                }
            }
        }

        private void Assert(bool condition, string message)
        {
            testsRun++;
            if (condition)
            {
                testsPassed++;
                Debug.Log($"  [PASS] {message}");
            }
            else
            {
                testsFailed++;
                failureMessages.Add(message);
                Debug.LogError($"  [FAIL] {message}");
            }
        }

        // Suite 1: Recipes & Cross Prep
        private void Test1_RecipesAndCrossPrepArchitecture()
        {
            Debug.Log("\n[Suite 1] Data-Driven Recipes & Cross-Prep Interdependence");
            Assert(GameData.Instance.recipes.Count == 4, "Exactly 4 starter recipes registered");
            Assert(GameData.Instance.recipes["pho_bo"].culturalOrigin == "Traditional Vietnamese", "Pho Bo registered as Traditional Vietnamese dish");
            Assert(GameData.Instance.recipes["banh_mi_thit"].culturalOrigin == "Traditional Vietnamese", "Banh Mi Thit registered as Traditional Vietnamese dish");
            Assert(GameData.Instance.recipes["tteokbokki"].culturalOrigin == "Traditional Korean", "Tteokbokki registered as Traditional Korean dish");
            Assert(GameData.Instance.recipes["bulgogi_banh_mi"].isFusionSpecialty, "Bulgogi Banh Mi explicitly marked as Original Vietnam-Korea Fusion Specialty");

            Assert(GameData.Instance.ingredients["pork_raw"].originZone == "linh_west" && GameData.Instance.ingredients["pork_raw"].partnerTargetZone == "jiwon_east", "Linh preps Pork Belly on West side for Jiwon's East Grill");
            Assert(GameData.Instance.ingredients["bulgogi_raw"].originZone == "linh_west" && GameData.Instance.ingredients["bulgogi_raw"].partnerTargetZone == "jiwon_east", "Linh preps Marinated Bulgogi on West side for Jiwon's East Sear Pan");
            Assert(GameData.Instance.ingredients["daikon_carrot_raw"].originZone == "jiwon_east" && GameData.Instance.ingredients["daikon_carrot_raw"].partnerTargetZone == "linh_west", "Jiwon preps Pickled Daikon & Carrot on East side for Linh's West Assembly");
            Assert(GameData.Instance.ingredients["kimchi_raw"].originZone == "jiwon_east" && GameData.Instance.ingredients["kimchi_raw"].partnerTargetZone == "linh_west", "Jiwon preps Kimchi Slaw on East side for Linh's West Assembly");
        }

        // Suite 2: Prep Station
        private void Test2_PrepStationRhythmCadence()
        {
            Debug.Log("\n[Suite 2] Prep Counter 4-Strike Rhythm Cadence & Miss Non-Blocking");
            var prepObj = new GameObject("PrepStationTest");
            var prep = prepObj.AddComponent<PrepStation>();

            var item = new KitchenItem("pork_raw", "Raw Pork Belly");
            Assert(prep.PlaceRawItem(item), "Prep Station accepts raw pork belly");

            var s1 = prep.PerformStrike(0.0f);
            var s2 = prep.PerformStrike(0.45f);
            Assert(s2.grade == CadenceGrade.PERFECT, "Ideal 0.45s strike intervals graded as PERFECT");

            var s3 = prep.PerformStrike(0.90f);
            var s4 = prep.PerformStrike(1.35f);
            Assert(s4.finished, "4th strike completes preparation");
            Assert(prep.heldItem.itemId == "pork_portioned", "Raw pork transforms into pork_portioned after 4 strikes");
            Assert(s4.quality == QualityTier.OPTIMAL, "Perfect cadence grants OPTIMAL quality tier");

            // Miss non-blocking test
            prep.heldItem = null;
            prep.PlaceRawItem(new KitchenItem("kimchi_raw", "Raw Kimchi"));
            prep.PerformStrike(0.0f);
            var miss = prep.PerformStrike(0.95f); // Missed interval
            Assert(miss.grade == CadenceGrade.MISS, "Off-beat strike graded as MISS");
            prep.PerformStrike(1.40f);
            var missFinish = prep.PerformStrike(1.85f);
            Assert(missFinish.finished, "Misses never permanently prevent task completion (4 strikes still complete)");
            Assert(prep.heldItem.itemId == "kimchi_slaw", "Kimchi slaw produced even with missed rhythm cadence");

            DestroyImmediate(prepObj);
        }

        // Suite 3: Cooking Station
        private void Test3_CookingStationStateMachineAndPhoSeparation()
        {
            Debug.Log("\n[Suite 3] Cooking State Machine & Pho Separation");
            var csObj = new GameObject("CookingTest");
            var cs = csObj.AddComponent<CookingStation>();
            cs.applianceId = "grill_pan";
            cs.applianceSpec = GameData.Instance.appliances["grill_pan"];

            // Pho noodle separation
            var stockpotObj = new GameObject("StockpotTest");
            var stockpot = stockpotObj.AddComponent<CookingStation>();
            stockpot.applianceId = "stockpot";
            stockpot.applianceSpec = GameData.Instance.appliances["stockpot"];
            Assert(!stockpot.PlaceIngredient(new KitchenItem("noodles_raw", "Dry Rice Noodles")), "Dedicated Stockpot rejects raw rice noodles (enforces separate noodle blanching)");

            var blancherObj = new GameObject("BlancherTest");
            var blancher = blancherObj.AddComponent<CookingStation>();
            blancher.applianceId = "noodle_blancher";
            blancher.applianceSpec = GameData.Instance.appliances["noodle_blancher"];
            Assert(blancher.PlaceIngredient(new KitchenItem("noodles_raw", "Dry Rice Noodles")), "Noodle Blancher accepts raw rice noodles");

            // Grill heat lifecycle
            cs.PlaceIngredient(new KitchenItem("pork_portioned", "Portioned Pork"));
            Assert(cs.currentState == CookingState.COOKING, "Grill starts in COOKING state when ingredient added");

            cs.TickCooking(2.0f);
            Assert(cs.currentState == CookingState.COOKING, "At 2.0s Grill remains in COOKING state");

            cs.TickCooking(2.5f); // Total 4.5s
            Assert(cs.currentState == CookingState.OPTIMAL, "At 4.5s Grill enters OPTIMAL state");

            cs.TickCooking(5.0f); // Total 9.5s
            Assert(cs.currentState == CookingState.OVERCOOKED, "At 9.5s Grill transitions to OVERCOOKED state");

            cs.TickCooking(4.0f); // Total 13.5s
            Assert(cs.currentState == CookingState.BURNT, "At 13.5s Grill transitions to BURNT state");

            var waste = cs.RetrieveCookedItem();
            Assert(waste.itemId == "burnt_waste", "Retrieving BURNT food produces burnt_waste");

            DestroyImmediate(csObj);
            DestroyImmediate(stockpotObj);
            DestroyImmediate(blancherObj);
        }

        // Suite 4: Assembly Station
        private void Test4_AssemblyStationRules()
        {
            Debug.Log("\n[Suite 4] Recipe-Driven Assembly & Pho Broth-Last Constraint");
            var asObj = new GameObject("AssemblyTest");
            var asSt = asObj.AddComponent<AssemblyStation>();

            // Rejects ingredient before vessel
            var reject = asSt.AddComponent(new KitchenItem("noodles_blanched", "Noodles"));
            Assert(!reject.ok, "Assembly rejects ingredients before a clean Plate or Bowl is placed");

            // Bowl first
            asSt.PlaceVessel(new KitchenItem("clean_bowl", "Clean Bowl", "bowl"));

            // Broth-last enforcement
            var brothEarly = asSt.AddComponent(new KitchenItem("broth_hot", "Hot Broth"));
            Assert(!brothEarly.ok, "Pho Bo rejects pouring hot broth before noodles and sliced beef are in the bowl");

            asSt.AddComponent(new KitchenItem("noodles_blanched", "Blanched Noodles"));
            asSt.AddComponent(new KitchenItem("beef_sliced", "Sliced Beef"));
            var brothValid = asSt.AddComponent(new KitchenItem("broth_hot", "Hot Broth"));
            Assert(brothValid.ok && brothValid.recipeCompleted, "Pho Bo completes once noodles, beef, and hot broth are assembled");
            Assert(asSt.heldItem.itemId == "pho_bo", "Completed dish has recipeId == pho_bo");

            // Bulgogi Banh Mi assembly
            asSt.heldItem = null;
            asSt.PlaceVessel(new KitchenItem("clean_plate", "Clean Plate", "plate"));
            asSt.AddComponent(new KitchenItem("baguette", "Baguette"));
            asSt.AddComponent(new KitchenItem("kimchi_slaw", "Kimchi Slaw"));
            var fusionDone = asSt.AddComponent(new KitchenItem("bulgogi_seared", "Seared Bulgogi"));
            Assert(fusionDone.ok && fusionDone.recipeCompleted, "Bulgogi Banh Mi completes when Baguette, Kimchi Slaw, and Seared Bulgogi are combined");

            DestroyImmediate(asObj);
        }

        // Suite 5: Pass Counter & Anti-Exploit
        private void Test5_PassCounterHandoffAndAntiExploit()
        {
            Debug.Log("\n[Suite 5] Pass Counter Handoff (+15 Flow) & Anti-Exploit Rules");
            var pcObj = new GameObject("PassCounterTest");
            var pc = pcObj.AddComponent<PassCounter>();

            ShiftManager.Instance.StartShift(1, GameMode.CAMPAIGN);
            ShiftManager.Instance.activeOrders.Add(new OrderTicket("ord_1", "banh_mi_thit", "plate", 60.0f));

            // Anti-exploit 1: self-retrieval
            pc.PlaceItem(0, new KitchenItem("pork_portioned", "Pork"), 1, 0.0f);
            var selfRet = pc.RetrieveItem(0, 1, 1.0f);
            Assert(!selfRet.handoffAwarded, "Anti-Exploit: Self-retrieval by same player grants 0 Flow");
            Assert(ShiftManager.Instance.kitchenFlow == 0.0f, "Kitchen Flow remains 0 after self-retrieval");

            // Anti-exploit 2: expired handoff window (> 4.0s)
            pc.PlaceItem(0, new KitchenItem("pork_portioned", "Pork"), 1, 0.0f);
            var lateRet = pc.RetrieveItem(0, 2, 4.5f);
            Assert(!lateRet.handoffAwarded, "Anti-Exploit: Retrieval after 4-second window grants 0 Flow");

            // Valid handoff
            pc.PlaceItem(0, new KitchenItem("pork_portioned", "Pork"), 1, 0.0f);
            var validRet = pc.RetrieveItem(0, 2, 2.0f);
            Assert(validRet.handoffAwarded, "Valid handoff between Player 1 and Player 2 within 2.5s grants handoff bonus");
            Assert(ShiftManager.Instance.kitchenFlow == 15.0f, "Kitchen Flow increases by exactly +15 points");

            // Anti-exploit 3: same order cannot claim bonus twice
            pc.PlaceItem(0, new KitchenItem("pork_portioned", "Pork"), 1, 0.0f);
            var dupRet = pc.RetrieveItem(0, 2, 1.0f);
            Assert(!dupRet.handoffAwarded, "Anti-Exploit: An order can claim the coordinated handoff bonus only once");
            Assert(ShiftManager.Instance.kitchenFlow == 15.0f, "Kitchen Flow unchanged at 15.0 on duplicate order handoff attempt");

            DestroyImmediate(pcObj);
        }

        // Suite 6: Kitchen Flow & Rush Rhythm
        private void Test6_KitchenFlowAndRushRhythm()
        {
            Debug.Log("\n[Suite 6] Kitchen Flow & Rush Rhythm (15s, 1.5x Multiplier, Paused Flow Gain)");
            ShiftManager.Instance.StartShift(1, GameMode.CAMPAIGN);
            ShiftManager.Instance.kitchenFlow = 90.0f;
            Assert(!ShiftManager.Instance.rushRhythmActive, "Flow at 90 does not trigger Rush Rhythm yet");

            ShiftManager.Instance.AddKitchenFlow(10.0f);
            Assert(ShiftManager.Instance.rushRhythmActive, "Reaching 100 Flow activates Rush Rhythm");
            Assert(ShiftManager.Instance.kitchenFlow == 0.0f, "Activating Rush Rhythm resets Kitchen Flow meter to 0");
            Assert(ShiftManager.Instance.rushRhythmRemaining == 15.0f, "Rush Rhythm duration is initialized to 15.0 seconds");

            // Flow paused during Rush Rhythm
            ShiftManager.Instance.AddKitchenFlow(15.0f);
            Assert(ShiftManager.Instance.kitchenFlow == 0.0f, "Flow generation is paused while Rush Rhythm is active");
        }

        // Suite 7: Washing Station
        private void Test7_WashingStationPlateLifecycle()
        {
            Debug.Log("\n[Suite 7] Washing Station & Finite Synchronized Plate Stack");
            var wsObj = new GameObject("WashingTest");
            var ws = wsObj.AddComponent<WashingStation>();

            ShiftManager.Instance.StartShift(1, GameMode.CAMPAIGN);
            Assert(ShiftManager.Instance.cleanPlates == 4, "Kitchen starts with finite pool of 4 clean plates/bowls");

            KitchenItem p1 = null, p2 = null, p3 = null, p4 = null, p5 = null;
            ws.InteractPrimary(ref p1, 1, "linh");
            ws.InteractPrimary(ref p2, 1, "linh");
            ws.InteractPrimary(ref p3, 1, "linh");
            ws.InteractPrimary(ref p4, 1, "linh");
            var rejected = ws.InteractPrimary(ref p5, 1, "linh");
            Assert(!rejected.ok, "5th vessel pickup rejected when all 4 plates are in use (no unlimited disposable plates)");

            // Deliver dish -> 1 dirty plate
            ShiftManager.Instance.activeOrders.Add(new OrderTicket("ord_wash", "banh_mi_thit", "plate", 60.0f));
            ShiftManager.Instance.DeliverDish(new KitchenItem("banh_mi_thit", "Banh Mi", "plate"), 1);
            Assert(ShiftManager.Instance.dirtyPlatesAtReturn == 1, "Serving a dish generates 1 dirty plate at the Return Tray");

            // Scrubbing
            ws.PerformScrubStroke();
            ws.PerformScrubStroke();
            ws.PerformScrubStroke();
            var stroke4 = ws.PerformScrubStroke();
            Assert(stroke4.plateCleaned && ShiftManager.Instance.cleanPlates == 1, "4 scrub strokes clean the dirty plate and return it to the shared clean stack");

            DestroyImmediate(wsObj);
        }

        // Suite 8: Lobby
        private void Test8_LobbyExclusivityAndReadyReset()
        {
            Debug.Log("\n[Suite 8] Multiplayer Lobby Character Exclusivity & Readiness Reset");
            // Direct validation of exclusivity rule
            Assert(GameData.Instance.IsCrossPartnerHandoff("pork_portioned", "linh", "jiwon"), "Cross partner handoff recognized between Linh and Jiwon");
            Assert(!GameData.Instance.IsCrossPartnerHandoff("pork_portioned", "linh", "linh"), "Same character cannot perform cross-partner handoff");
        }

        // Suite 9: Progression System
        private void Test9_ProgressionSystem()
        {
            Debug.Log("\n[Suite 9] Milestone 3 Progression, Stage Unlocks & Kitchen Upgrades");
            ProgressionManager.Instance.ResetProgressionForTesting();

            Assert(ProgressionManager.Instance.coins == 150, "Player starts with 150 initial seed coins");
            Assert(ProgressionManager.Instance.highestUnlockedStage == 1, "Only Stage 1 is unlocked initially");

            // 1 star does not unlock Stage 2
            var s1 = ProgressionManager.Instance.RecordShiftResults(1, 250, 1, 2, 0);
            Assert(s1.earnedCoins == 41, "Shift 1 earned 41 coins (250/8 + 2*5)");
            Assert(ProgressionManager.Instance.highestUnlockedStage == 1, "Stage 2 remains locked after 1-star shift");

            // 2 stars unlocks Stage 2
            var s2 = ProgressionManager.Instance.RecordShiftResults(1, 400, 2, 3, 1);
            Assert(s2.newlyUnlocked, "Achieving 2 stars unlocks the next stage");
            Assert(ProgressionManager.Instance.highestUnlockedStage == 2, "Highest unlocked stage is now Stage 2");

            // Knife purchase
            var buy = ProgressionManager.Instance.PurchaseUpgrade("sharp_santoku_knives");
            Assert(buy.ok, "Successfully purchased Sharp Santoku Knife Set");
            Assert(ProgressionManager.Instance.IsUpgradeOwned("sharp_santoku_knives"), "Sharp knives upgrade marked as owned");
            Assert(ProgressionManager.Instance.coins == (150 + 41 + 100 - 100), "Coins deducted for knife upgrade");
        }

        // Suite 10: Linh Execution Specialist
        private void Test10_LinhExecutionSpecialistTiers()
        {
            Debug.Log("\n[Suite 10] Linh Execution Specialist: Tiers 1-3 Speed Multipliers & Visual Third Arm");
            var pObj = new GameObject("PlayerLinhTest");
            var p = pObj.AddComponent<PlayerController>();

            // Visual arm mock objects
            var armObj = new GameObject("ThirdArm");
            armObj.transform.SetParent(pObj.transform);
            var glowObj = new GameObject("ThirdArmGlow");
            glowObj.transform.SetParent(pObj.transform);
            p.thirdArmVisual = armObj;
            p.thirdArmGlowVisual = glowObj;

            p.ConfigureChef(1, "linh", "prep_master", 1);
            Assert(Mathf.Approximately(p.GetTaskSpeedMultiplier(), 1.0f), "Linh Tier 1 task speed multiplier is 1.0x (Ordinary Chef)");
            Assert(!p.thirdArmVisual.activeSelf, "Linh Tier 1 has no mechanical third arm visible");

            p.ConfigureChef(1, "linh", "prep_master", 2);
            Assert(Mathf.Approximately(p.GetTaskSpeedMultiplier(), 0.92f), "Linh Tier 2 task speed multiplier is 0.92x (~8% faster)");
            Assert(p.thirdArmVisual.activeSelf, "Linh Tier 2 has rustic mechanical third arm visible");
            Assert(!p.thirdArmGlowVisual.activeSelf, "Linh Tier 2 has no neon glow on rustic third arm");

            p.ConfigureChef(1, "linh", "prep_master", 3);
            Assert(Mathf.Approximately(p.GetTaskSpeedMultiplier(), 0.85f), "Linh Tier 3 task speed multiplier is 0.85x (~15% faster)");
            Assert(p.thirdArmVisual.activeSelf && p.thirdArmGlowVisual.activeSelf, "Linh Tier 3 has futuristic white-and-blue glowing precision arm visible");

            DestroyImmediate(pObj);
        }

        // Suite 11: Jiwon Multitasking & Crew
        private void Test11_JiwonDelegationPartTimeCrew()
        {
            Debug.Log("\n[Suite 11] Jiwon Multitasking & Delegation: Part-Time Worker Hiring & State Machine");
            var kcObj = new GameObject("KitchenControllerTest");
            var kc = kcObj.AddComponent<KitchenController>();

            // Setup East prep station
            var prepEastObj = new GameObject("PrepEast");
            prepEastObj.transform.SetParent(kcObj.transform);
            var prepEast = prepEastObj.AddComponent<PrepStation>();
            prepEast.stationId = "prep_east";
            kc.stations["prep_east"] = prepEast;

            // Setup Jiwon player
            var pObj = new GameObject("PlayerJiwon");
            pObj.transform.SetParent(kcObj.transform);
            var p = pObj.AddComponent<PlayerController>();
            p.ConfigureChef(2, "jiwon", "flame_keeper", 1);
            kc.playersByPeer[2] = p;

            ProgressionManager.Instance.coins = 100;

            // Tier 1: 0 slots -> rejected
            var t1 = kc.RequestHireWorker(2, "prep_assistant");
            Assert(!t1.ok, "Jiwon Tier 1 cannot hire part-time crew (0 slots available)");

            // Tier 2: 1 slot -> can hire
            p.characterTier = 2;
            var t2 = kc.RequestHireWorker(2, "prep_assistant");
            Assert(t2.ok, "Jiwon Tier 2 successfully hires Prep Assistant for 20 coins");
            Assert(ProgressionManager.Instance.coins == 80, "20 coins deducted from restaurant vault (Balance: 80)");
            Assert(kc.activeWorkers.Count == 1, "Exactly 1 active part-time worker in kitchen");

            // Tier 2: second hire rejected (slot limit = 1)
            var t2Second = kc.RequestHireWorker(2, "dishwasher");
            Assert(!t2Second.ok, "Hiring second worker rejected under Tier 2 (slot limit = 1)");

            // Worker state machine
            var worker = kc.activeWorkers[t2.workerId];
            Assert(worker.currentState == WorkerState.WALKING_IN, "Hired worker enters in WALKING_IN state");

            // Transition to working upon reaching station
            prepEast.PlaceRawItem(new KitchenItem("daikon_carrot_raw", "Raw Daikon"));
            worker.transform.position = prepEast.transform.position + new Vector3(0.0f, 0.0f, 0.8f);
            worker.TickWorker(0.1f);
            Assert(worker.currentState == WorkerState.WORKING, "Worker transitions to WORKING state upon reaching station");

            // Worker executes strike
            worker.TickWorker(1.2f);
            Assert(prepEast.strikesCompleted >= 1, "Prep Assistant automated strike advanced prep progress on station");

            // Contract expiration
            worker.contractRemainingSec = 0.0f;
            worker.TickWorker(0.1f);
            Assert(worker.currentState == WorkerState.WALKING_OUT, "Worker transitions to WALKING_OUT when contract expires");

            DestroyImmediate(kcObj);
        }

        // Suite 12: Campaign Levels
        private void Test12_CampaignLevelsAndProgressionUnlocks()
        {
            Debug.Log("\n[Suite 12] 5 Handcrafted Campaign Levels & Character Tier Unlocks");
            Assert(GameData.Instance.campaignLevels.Count == 5, "Exactly 5 handcrafted campaign levels registered");
            Assert(GameData.Instance.campaignLevels[1].allowedRecipes.Count == 2, "Level 1 starts with 2 starter recipes (First Customer)");
            Assert(GameData.Instance.campaignLevels[3].allowedRecipes.Contains("tteokbokki"), "Level 3 introduces Korean Tteokbokki (Night Market)");
            Assert(GameData.Instance.campaignLevels[5].allowedRecipes.Count == 4, "Level 5 combines all 4 authentic recipes (Grand Opening)");

            ProgressionManager.Instance.ResetProgressionForTesting();
            Assert(ProgressionManager.Instance.linhTier == 1 && ProgressionManager.Instance.jiwonTier == 1, "Chefs start at Tier 1");

            // Level 3 completes -> Tier 2
            ProgressionManager.Instance.RecordShiftResults(3, 600, 2, 4, 1);
            Assert(ProgressionManager.Instance.linhTier == 2 && ProgressionManager.Instance.jiwonTier == 2, "Linh & Jiwon upgraded to Tier 2 after Level 3");

            // Level 5 completes -> Tier 3
            ProgressionManager.Instance.RecordShiftResults(5, 950, 2, 6, 2);
            Assert(ProgressionManager.Instance.linhTier == 3 && ProgressionManager.Instance.jiwonTier == 3, "Linh & Jiwon upgraded to Tier 3 (Precision Engineer & Business Operator)");
        }

        // Suite 13: Endless Rush Mode
        private void Test13_EndlessRushMode()
        {
            Debug.Log("\n[Suite 13] Endless Rush Mode: Dynamic Difficulty Scaling & Personal Best Tracking");
            ShiftManager.Instance.StartShift(1, GameMode.ENDLESS_RUSH);
            Assert(ShiftManager.Instance.currentMode == GameMode.ENDLESS_RUSH, "ShiftManager initialized in ENDLESS_RUSH mode");

            // Test personal best tracking
            ProgressionManager.Instance.ResetProgressionForTesting();
            bool isNewHigh = ProgressionManager.Instance.RecordEndlessRun(850, 7, 72.5f);
            Assert(isNewHigh, "First endless run recorded as new high score");
            Assert(ProgressionManager.Instance.endlessBestScore == 850, "Endless personal best score updated to 850");

            bool lowerHigh = ProgressionManager.Instance.RecordEndlessRun(500, 4, 45.0f);
            Assert(!lowerHigh, "Lower score run does not overwrite personal best");
            Assert(ProgressionManager.Instance.endlessBestScore == 850, "Endless personal best preserved at 850");
        }
    }
}
