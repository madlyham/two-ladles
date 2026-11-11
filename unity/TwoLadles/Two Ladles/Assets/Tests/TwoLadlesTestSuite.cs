using System;
using System.Collections.Generic;
using UnityEngine;
using TwoLadles.Data;
using TwoLadles.Core;
using TwoLadles.Stations;
using TwoLadles.Kitchen;
using TwoLadles.Player;
using TwoLadles.Networking;
using TwoLadles.AI;
using TwoLadles.UI;

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
                var gd = gdObj.AddComponent<GameData>();
                GameData.Instance = gd;
                gd.InitData();
            }

            // Ensure ProgressionManager is initialized
            if (ProgressionManager.Instance == null)
            {
                var pmObj = new GameObject("ProgressionManager");
                var pm = pmObj.AddComponent<ProgressionManager>();
                ProgressionManager.Instance = pm;
                pm.InitDefaults();
            }

            // Ensure ShiftManager is initialized
            if (ShiftManager.Instance == null)
            {
                var smObj = new GameObject("ShiftManager");
                var sm = smObj.AddComponent<ShiftManager>();
                ShiftManager.Instance = sm;
            }

            // Ensure NetworkManager is initialized
            if (NetworkManager.Instance == null)
            {
                var nmObj = new GameObject("NetworkManager");
                var nm = nmObj.AddComponent<NetworkManager>();
                NetworkManager.Instance = nm;
            }

            Test1_RecipesAndCrossPrepArchitecture();
            Test2_PrepStationRhythmCadence();
            Test3_CookingStationStateMachineAndPhoSeparation();
            Test4_AssemblyStationRules();
            Test5_PassCounterHandoffAndAntiExploit();
            Test6_KitchenFlowAndRushRhythm();
            Test7_NoWashingPlateSupply();
            Test8_LobbyExclusivityAndReadyReset();
            Test9_ProgressionSystem();
            Test10_LinhExecutionSpecialistTiers();
            Test11_JiwonDelegationPartTimeCrew();
            Test12_CampaignLevelsAndProgressionUnlocks();
            Test13_EndlessRushMode();
            Test14_HoldingCounterAndThrowMechanics();
            Test15_WaveProgressionReputationAndGoldEconomy();
            Test16_SoloModeAndAIPartner();
            Test17_TwoBotQAManagerAndReporting();
            Test18_StoryPhoneAndDayNightCycle();
            Test19_LanSyncTimingAndBotSafety();
            Test20_PauseAndLanDayCycle();

            Debug.Log("------------------------------------------------------------");
            Debug.Log($"  RESULTS: {testsPassed} / {testsRun} Passed | {testsFailed} Failed");
            Debug.Log("------------------------------------------------------------");

            if (testsFailed == 0)
            {
                Debug.Log($"  ALL {testsPassed} AUTOMATED TESTS PASSED CLEANLY IN UNITY C#.");
            }
            else
            {
                Debug.LogError($"  TEST FAILURES ENCOUNTERED ({testsFailed} failed assertions):");
                foreach (var msg in failureMessages)
                {
                    Debug.LogError("  " + msg);
                }
            }
        }

        private void Assert(bool condition, string testName)
        {
            testsRun++;
            if (condition)
            {
                testsPassed++;
                Debug.Log($"  [PASS] {testName}");
            }
            else
            {
                testsFailed++;
                string failMsg = $"[FAIL] {testName}";
                failureMessages.Add(failMsg);
                Debug.LogError($"  {failMsg}");
            }
        }

        // Suite 1: Recipes & Cross Prep (9 assertions)
        private void Test1_RecipesAndCrossPrepArchitecture()
        {
            Debug.Log("\n[Suite 1] Data-Driven Recipes & Cross-Prep Interdependence");
            Assert(GameData.Instance.recipes.Count == 5, "Exactly 5 starter recipes registered");
            Assert(GameData.Instance.recipes["pho_bo"].culturalOrigin == "Traditional Vietnamese", "Pho Bo registered as Traditional Vietnamese dish");
            Assert(GameData.Instance.recipes["banh_mi_thit"].culturalOrigin == "Traditional Vietnamese", "Banh Mi Thit registered as Traditional Vietnamese dish");
            Assert(GameData.Instance.recipes["tteokbokki"].culturalOrigin == "Traditional Korean", "Tteokbokki registered as Traditional Korean dish");
            Assert(GameData.Instance.recipes["kimchi_bokkeumbap"].culturalOrigin == "Traditional Korean", "Kimchi Bokkeumbap registered as Traditional Korean dish");
            Assert(GameData.Instance.recipes["bulgogi_banh_mi"].isFusionSpecialty, "Bulgogi Banh Mi explicitly marked as Original Vietnam-Korea Fusion Specialty");

            Assert(GameData.Instance.ingredients["pork_raw"].originZone == "linh_west" && GameData.Instance.ingredients["pork_raw"].partnerTargetZone == "jiwon_east", "Linh preps Pork Belly on West side for Jiwon's East Grill");
            Assert(GameData.Instance.ingredients["bulgogi_raw"].originZone == "linh_west" && GameData.Instance.ingredients["bulgogi_raw"].partnerTargetZone == "jiwon_east", "Linh preps Marinated Bulgogi on West side for Jiwon's East Sear Pan");
            Assert(GameData.Instance.ingredients["daikon_carrot_raw"].originZone == "jiwon_east" && GameData.Instance.ingredients["daikon_carrot_raw"].partnerTargetZone == "linh_west", "Jiwon preps Pickled Daikon & Carrot on East side for Linh's West Assembly");
            Assert(GameData.Instance.ingredients["kimchi_raw"].originZone == "jiwon_east" && GameData.Instance.ingredients["kimchi_raw"].partnerTargetZone == "linh_west", "Jiwon preps Kimchi Slaw on East side for Linh's West Assembly");
        }

        // Suite 2: Prep Station (9 assertions)
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

            // Prep Master specialization bonus (+15 bonus on top of +10 Perfect cadence bonus)
            prep.heldItem = null;
            prep.PlaceRawItem(new KitchenItem("pork_raw", "Raw Pork Belly"));
            prep.PerformStrike(0.0f, "prep_master");
            var specStrike = prep.PerformStrike(0.45f, "prep_master");
            Assert(specStrike.scoreBonus == 25, "Prep Master specialization adds +15 bonus on top of +10 Perfect cadence bonus");

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

        // Suite 3: Cooking Station (9 assertions)
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

            // Flame Keeper extends OPTIMAL cooking window (+35%)
            var fkObj = new GameObject("FlameKeeperGrill");
            var fkCs = fkObj.AddComponent<CookingStation>();
            fkCs.applianceId = "grill_pan";
            fkCs.applianceSpec = GameData.Instance.appliances["grill_pan"];
            fkCs.flameKeeperEquipped = true;
            fkCs.PlaceIngredient(new KitchenItem("pork_portioned", "Portioned Pork"));
            fkCs.TickCooking(9.8f); // With +35%, 9.8s remains OPTIMAL
            Assert(fkCs.currentState == CookingState.OPTIMAL, "Flame Keeper specialization extends OPTIMAL cooking window (+35%)");

            DestroyImmediate(fkObj);
            DestroyImmediate(csObj);
            DestroyImmediate(stockpotObj);
            DestroyImmediate(blancherObj);
        }

        // Suite 4: Assembly Station (7 assertions)
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
            Assert(brothValid.ok, "Valid Pho Bo sequence accepted");
            Assert(brothValid.recipeCompleted, "Pho Bo completes once noodles, beef, and hot broth are assembled");
            Assert(asSt.heldItem.itemId == "pho_bo", "Completed dish has recipe_id == pho_bo");

            // Vessel type check: Tteokbokki on a flat plate is rejected
            asSt.heldItem = null;
            asSt.currentVessel = null;
            asSt.assembledIngredients.Clear();
            asSt.PlaceVessel(new KitchenItem("clean_plate", "Clean Plate", "plate"));
            var tteokReject = asSt.AddComponent(new KitchenItem("tteok_cooked", "Cooked Rice Cakes"));
            Assert(!tteokReject.ok, "Assembly rejects Tteokbokki on a flat Plate");

            // Bulgogi Banh Mi assembly
            asSt.heldItem = null;
            asSt.currentVessel = null;
            asSt.assembledIngredients.Clear();
            asSt.PlaceVessel(new KitchenItem("clean_plate", "Clean Plate", "plate"));
            asSt.AddComponent(new KitchenItem("baguette", "Baguette"));
            asSt.AddComponent(new KitchenItem("kimchi_slaw", "Kimchi Slaw"));
            var fusionDone = asSt.AddComponent(new KitchenItem("bulgogi_seared", "Seared Bulgogi"));
            Assert(fusionDone.ok && fusionDone.recipeCompleted, "Bulgogi Banh Mi completes when Baguette, Kimchi Slaw, and Seared Bulgogi are combined");

            DestroyImmediate(asObj);
        }

        // Suite 5: Pass Counter & Anti-Exploit (7 assertions)
        private void Test5_PassCounterHandoffAndAntiExploit()
        {
            Debug.Log("\n[Suite 5] Pass Counter Handoff (+15 Flow) & Anti-Exploit Rules");
            var pcObj = new GameObject("PassCounterTest");
            var pc = pcObj.AddComponent<PassCounter>();

            ShiftManager.Instance.kitchenFlow = 0.0f;
            ShiftManager.Instance.claimedHandoffOrders.Clear();
            ShiftManager.Instance.activeOrders.Clear();
            ShiftManager.Instance.activeOrders.Add(new OrderTicket("ord_pass_1", "banh_mi_thit", "plate", 60.0f));

            // Anti-exploit 1: self-retrieval
            pc.PlaceItem(0, new KitchenItem("pork_portioned", "Pork"), 1, 0.0f);
            var selfRet = pc.RetrieveItem(0, 1, 1.0f);
            Assert(!selfRet.handoffAwarded, "Anti-Exploit: Self-retrieval by same player grants 0 Flow");
            Assert(ShiftManager.Instance.kitchenFlow == 0.0f, "Kitchen Flow remains 0 after self-retrieval");

            // Anti-exploit 2: timeout > 4.0s
            pc.PlaceItem(0, new KitchenItem("pork_portioned", "Pork"), 1, 0.0f);
            var timeoutRet = pc.RetrieveItem(0, 2, 5.0f);
            Assert(!timeoutRet.handoffAwarded, "Anti-Exploit: Retrieval after 4-second window grants 0 Flow");

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

        // Suite 6: Kitchen Flow & Rush Rhythm (8 assertions)
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

            // 1.5x multiplier on served dish
            ShiftManager.Instance.activeOrders.Clear();
            ShiftManager.Instance.activeOrders.Add(new OrderTicket("ord_rush_test", "banh_mi_thit", "plate", 60.0f));
            var serveRes = ShiftManager.Instance.DeliverDish(new KitchenItem("banh_mi_thit", "Banh Mi", "plate"), 1);
            Assert(serveRes.success && serveRes.scoreAwarded == 190, "Rush Rhythm applies 1.5x multiplier to base order score (165 + 25 tip = 190)");

            // Deactivates after 15s
            ShiftManager.Instance.TickShift(15.1f);
            Assert(!ShiftManager.Instance.rushRhythmActive, "Rush Rhythm deactivates after 15 seconds");

            // Flow resumes
            ShiftManager.Instance.AddKitchenFlow(20.0f);
            Assert(ShiftManager.Instance.kitchenFlow == 20.0f, "Flow generation resumes cleanly after Rush Rhythm ends");
        }

        // Suite 7: No Washing Chore — plates are free from the quick-select wheel (8 assertions)
        // Requirement change (user-approved 2026-11-10): the washing sink was removed to keep the game simple.
        // Serving never creates dirty plates and never runs the kitchen out of vessels.
        private void Test7_NoWashingPlateSupply()
        {
            Debug.Log("\n[Suite 7] No Washing Chore — Unlimited Plates");
            ShiftManager.Instance.StartShift(1, GameMode.CAMPAIGN);
            int scoreBefore = ShiftManager.Instance.totalScore;

            // Serve more dishes than the old 4-plate pool allowed; every delivery must succeed.
            for (int i = 0; i < 6; i++)
            {
                ShiftManager.Instance.activeOrders.Add(new OrderTicket($"ord_noplate_{i}", "banh_mi_thit", "plate", 60.0f));
                var res = ShiftManager.Instance.DeliverDish(new KitchenItem("banh_mi_thit", "Banh Mi", "plate"), 1);
                Assert(res.success, $"Delivery {i + 1} succeeds without any plate washing");
            }

            Assert(ShiftManager.Instance.dirtyPlatesAtReturn == 0, "Serving dishes never creates dirty plates (no washing chore)");
            Assert(ShiftManager.Instance.totalScore > scoreBefore, "Served dishes still award score");
        }

        // Suite 8: Lobby Exclusivity & Readiness Reset (5 assertions)
        private void Test8_LobbyExclusivityAndReadyReset()
        {
            Debug.Log("\n[Suite 8] Multiplayer Lobby Character Exclusivity & Readiness Reset");
            NetworkManager.Instance.players.Clear();
            NetworkManager.Instance.players[1] = new PlayerLobbyInfo { peerId = 1, name = "Host", character = "linh", specialization = "prep_master", ready = true };
            NetworkManager.Instance.players[2] = new PlayerLobbyInfo { peerId = 2, name = "Client", character = "jiwon", specialization = "flame_keeper", ready = true };

            var startOk = NetworkManager.Instance.CanStartShift(true);
            Assert(startOk.ok, "Host can start shift when both Linh and Jiwon are connected and Ready");

            var dupChar = NetworkManager.Instance.ApplyPlayerSelection(2, "linh", "flame_keeper");
            Assert(!dupChar.ok, "Lobby rejects selecting Linh when partner already selected Linh");

            var sameSpec = NetworkManager.Instance.ApplyPlayerSelection(2, "jiwon", "prep_master");
            Assert(sameSpec.ok, "Lobby allows both players to equip the same specialization (PREP MASTER)");
            Assert(!NetworkManager.Instance.players[2].ready, "Changing specialization automatically resets player Ready status to false");

            var startBlocked = NetworkManager.Instance.CanStartShift(true);
            Assert(!startBlocked.ok, "Host cannot start shift after a player's Ready status was reset");

            // Placeholder chef: selectable, never ready-able, never blocks the other chef.
            NetworkManager.Instance.players[1].ready = true;
            var phSel = NetworkManager.Instance.ApplyPlayerSelection(2, "placeholder", "prep_master");
            Assert(phSel.ok, "Lobby allows switching to the PLACEHOLDER character");
            Assert(!NetworkManager.Instance.players[2].ready, "Switching to the placeholder resets Ready to false");
            var phReady = NetworkManager.Instance.SetPlayerReady(2, true);
            Assert(!phReady.ok, "SetPlayerReady rejects a player using the placeholder character");
            Assert(!NetworkManager.Instance.players[2].ready, "Placeholder player's Ready stays false after a rejected ready");
            Assert(!NetworkManager.CanReadyAs("placeholder") && NetworkManager.CanReadyAs("linh") && NetworkManager.CanReadyAs("jiwon"),
                "CanReadyAs blocks only the placeholder character");
            var phBoth = NetworkManager.Instance.ApplyPlayerSelection(1, "placeholder", "prep_master");
            Assert(phBoth.ok, "Both players may sit on the placeholder at once (no exclusivity)");
            NetworkManager.Instance.players[1].ready = true; NetworkManager.Instance.players[2].ready = true; // forced (e.g. stale packet)
            Assert(!NetworkManager.Instance.CanStartShift(true).ok, "Shift cannot start while any player uses the placeholder");
            NetworkManager.Instance.ApplyPlayerSelection(1, "linh", "prep_master");
            NetworkManager.Instance.ApplyPlayerSelection(2, "jiwon", "flame_keeper");
            var backReady1 = NetworkManager.Instance.SetPlayerReady(1, true);
            var backReady2 = NetworkManager.Instance.SetPlayerReady(2, true);
            Assert(backReady1.ok && backReady2.ok && NetworkManager.Instance.CanStartShift(true).ok,
                "After leaving the placeholder, both chefs can Ready and the shift can start");
        }

        // Suite 9: Progression System (14 assertions)
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
            Assert(ProgressionManager.Instance.highestUnlockedStage == 2, "Highest unlocked stage is now Stage 2 (Village Market Restaurant)");

            // Knife purchase
            int prevBalance = ProgressionManager.Instance.coins;
            var buy = ProgressionManager.Instance.PurchaseUpgrade("sharp_santoku_knives");
            Assert(buy.ok, "Successfully purchased Sharp Santoku Knife Set");
            Assert(ProgressionManager.Instance.IsUpgradeOwned("sharp_santoku_knives"), "Sharp knives upgrade marked as owned");
            Assert(ProgressionManager.Instance.coins == prevBalance - 100, "100 coins deducted for knife upgrade");

            // Duplicate purchase rejected
            var dupBuy = ProgressionManager.Instance.PurchaseUpgrade("sharp_santoku_knives");
            Assert(!dupBuy.ok, "Duplicate upgrade purchase is rejected");

            // Knife upgrade effect on Prep Station rhythm window
            var prepObj = new GameObject("KnifeTestPrep");
            var prep = prepObj.AddComponent<PrepStation>();
            prep.PlaceRawItem(new KitchenItem("pork_raw", "Raw Pork Belly"));
            prep.PerformStrike(0.0f, "", "linh");
            var upgradedStrike = prep.PerformStrike(0.75f, "", "linh");
            Assert(upgradedStrike.grade == CadenceGrade.GOOD, "Purchased knife upgrade widened cadence tolerance window");
            DestroyImmediate(prepObj);

            // Test JSON persistence save and reload
            ProgressionManager.Instance.SaveProgression();
            bool reloadOk = ProgressionManager.Instance.LoadProgression();
            Assert(reloadOk, "Progression data saved and reloaded from JSON file successfully");
            Assert(ProgressionManager.Instance.highestUnlockedStage == 2, "Reloaded progression preserves unlocked stages");
            Assert(ProgressionManager.Instance.IsUpgradeOwned("sharp_santoku_knives"), "Reloaded progression preserves owned upgrades");
        }

        // Suite 10: Linh Execution Specialist (8 assertions)
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

            // Verify strike execution timing with Linh Tier 3
            var prepObj = new GameObject("Tier3PrepTest");
            var prep = prepObj.AddComponent<PrepStation>();
            prep.PlaceRawItem(new KitchenItem("pork_raw", "Raw Pork Belly"));
            prep.PerformStrike(0.0f, "", "linh", 0.85f);
            var fastStrike = prep.PerformStrike(0.385f, "", "linh", 0.85f);
            Assert(fastStrike.grade == CadenceGrade.PERFECT, "Linh Tier 3 executes perfect cadence strikes at faster 0.38s intervals");

            DestroyImmediate(prepObj);
            DestroyImmediate(pObj);
        }

        // Suite 11: Jiwon Multitasking & Crew (12 assertions)
        private void Test11_JiwonDelegationPartTimeCrew()
        {
            Debug.Log("\n[Suite 11] Jiwon Multitasking & Delegation: Part-Time Worker Hiring & State Machine");
            var kcObj = new GameObject("KitchenControllerTest");
            var kc = kcObj.AddComponent<KitchenController>();
            KitchenController.Instance = kc;

            // Setup East prep station
            var prepEastObj = new GameObject("PrepEast");
            prepEastObj.transform.SetParent(kcObj.transform);
            var prepEast = prepEastObj.AddComponent<PrepStation>();
            prepEast.stationId = "prep_east";
            kc.stations["prep_east"] = prepEast;

            // Setup West prep station
            var prepWestObj = new GameObject("PrepWest");
            prepWestObj.transform.SetParent(kcObj.transform);
            var prepWest = prepWestObj.AddComponent<PrepStation>();
            prepWest.stationId = "prep_west";
            kc.stations["prep_west"] = prepWest;

            // Setup Washing station
            var wsObj = new GameObject("WashingStation");
            wsObj.transform.SetParent(kcObj.transform);
            var ws = wsObj.AddComponent<WashingStation>();
            ws.stationId = "washing_station";
            kc.stations["washing_station"] = ws;

            // Setup Jiwon player
            var pObj = new GameObject("PlayerJiwon");
            pObj.transform.SetParent(kcObj.transform);
            var p = pObj.AddComponent<PlayerController>();
            p.ConfigureChef(2, "jiwon", "flame_keeper", 1);
            kc.playersByPeer[2] = p;

            ShiftManager.Instance.totalScore = 100;
            int coinsBeforeHire = ProgressionManager.Instance.coins;

            // Tier 1: 0 slots -> rejected
            var t1 = kc.RequestHireWorker(2, "prep_assistant");
            Assert(!t1.ok, "Jiwon Tier 1 cannot hire part-time crew (0 slots available)");

            // Tier 2: 1 slot -> can hire
            p.characterTier = 2;
            var t2 = kc.RequestHireWorker(2, "prep_assistant");
            Assert(t2.ok, "Jiwon Tier 2 successfully hires Prep Assistant for 20 score");
            Assert(ShiftManager.Instance.totalScore == 80, "20 score deducted from shift score (Score: 80)");
            Assert(ProgressionManager.Instance.coins == coinsBeforeHire, "Hiring part-timers never spends coins");
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

            // Cooldown check
            var cdHire = kc.RequestHireWorker(2, "prep_assistant");
            Assert(!cdHire.ok, "Hiring worker on active agency cooldown is rejected");

            // Tier 3: 2 simultaneous part-timers
            p.characterTier = 3;
            kc.activeWorkers.Clear();
            kc.workerCooldowns.Clear();
            var hire1 = kc.RequestHireWorker(2, "prep_assistant");
            var hire2 = kc.RequestHireWorker(2, "dishwasher");
            Assert(hire1.ok && hire2.ok, "Jiwon Tier 3 can hire 2 simultaneous part-timers");
            Assert(kc.activeWorkers.Count == 2, "Kitchen manages 2 simultaneous part-time workers at Tier 3");

            // Tier 3 Unified Single-Button Hire: spawns all available part-timers as prep assistants on free stations
            kc.activeWorkers.Clear();
            kc.workerCooldowns.Clear();
            ShiftManager.Instance.totalScore = 100;
            var unifiedHire = kc.RequestHireAvailableCrew(2);
            Assert(unifiedHire.ok, "Jiwon Tier 3 unified single-button hire triggers crew deployment");
            Assert(unifiedHire.hiredCount == 2, "Unified single-button hire deploys both available part-timers (2 workers)");
            Assert(kc.activeWorkers.Count == 2, "Kitchen has 2 active workers after unified single-button hire");
            Assert(ShiftManager.Instance.totalScore == 60, "40 score deducted for hiring both part-timers at Tier 3 (Score: 60)");

            // Not enough score -> hire rejected, score never goes negative
            kc.activeWorkers.Clear();
            kc.workerCooldowns.Clear();
            ShiftManager.Instance.totalScore = 10;
            var poorHire = kc.RequestHireWorker(2, "prep_assistant");
            Assert(!poorHire.ok && ShiftManager.Instance.totalScore == 10, "Hire rejected when shift score is below 20 (score unchanged)");
            ShiftManager.Instance.totalScore = 100;
            unifiedHire = kc.RequestHireAvailableCrew(2);

            PartTimeWorker w1 = null;
            PartTimeWorker w2 = null;
            foreach (var w in kc.activeWorkers.Values)
            {
                if (w1 == null) w1 = w;
                else w2 = w;
            }
            Assert(w1 != null && w2 != null && w1.targetStation != w2.targetStation, "Part-timers are assigned distinct preparation stations (East and West)");
            Assert(w1.targetStation is PrepStation && w2.targetStation is PrepStation, "Both part-timers are prep/chopping assistants (washing eliminated)");

            DestroyImmediate(kcObj);
        }

        // Suite 12: Campaign Levels (9 assertions)
        private void Test12_CampaignLevelsAndProgressionUnlocks()
        {
            Debug.Log("\n[Suite 12] 5 Handcrafted Campaign Levels & Milestone 3 Character Tier Unlocks");
            Assert(GameData.Instance.campaignLevels.Count == 5, "Exactly 5 handcrafted campaign levels registered");

            var lvl1 = GameData.Instance.campaignLevels[1];
            var lvl3 = GameData.Instance.campaignLevels[3];
            var lvl5 = GameData.Instance.campaignLevels[5];

            Assert(lvl1.allowedRecipes.Count == 1, "Level 1 starts with 1 starter recipe (First Customer: Banh Mi Thit)");
            Assert(lvl3.allowedRecipes.Contains("tteokbokki"), "Level 3 introduces Korean Tteokbokki (Night Market)");
            Assert(lvl5.allowedRecipes.Count == 5, "Level 5 combines all 5 authentic recipes (Grand Opening)");

            ProgressionManager.Instance.ResetProgressionForTesting();
            Assert(ProgressionManager.Instance.linhTier == 1 && ProgressionManager.Instance.jiwonTier == 1, "Chefs start at Tier 1");

            // Level 3 completes -> Tier 2
            var l3Res = ProgressionManager.Instance.RecordShiftResults(3, 600, 2, 4, 1);
            Assert(l3Res.tierUpgraded, "Completing Level 3 triggers character tier upgrade");
            Assert(ProgressionManager.Instance.linhTier == 2 && ProgressionManager.Instance.jiwonTier == 2, "Linh & Jiwon upgraded to Tier 2 after Level 3");

            // Level 5 completes -> Tier 3
            var l5Res = ProgressionManager.Instance.RecordShiftResults(5, 950, 2, 6, 2);
            Assert(l5Res.tierUpgraded, "Completing Level 5 triggers Tier 3 upgrade");
            Assert(ProgressionManager.Instance.linhTier == 3 && ProgressionManager.Instance.jiwonTier == 3, "Linh & Jiwon upgraded to Tier 3 (Precision Engineer & Business Operator)");
        }

        // Suite 13: Endless Rush Mode (9 assertions)
        private void Test13_EndlessRushMode()
        {
            Debug.Log("\n[Suite 13] Endless Rush Mode: Dynamic Difficulty Scaling & Personal Best Tracking");
            ShiftManager.Instance.StartShift(1, GameMode.ENDLESS_RUSH);
            Assert(ShiftManager.Instance.currentMode == GameMode.ENDLESS_RUSH, "ShiftManager initialized in ENDLESS_RUSH mode");
            Assert(ShiftManager.Instance.dynamicMaxOrders == 2, "Endless mode begins with 2 concurrent orders");

            // Tick 65 seconds -> dynamic scaling increases pressure
            ShiftManager.Instance.TickShift(65.0f);
            Assert(ShiftManager.Instance.dynamicMaxOrders >= 4, "Endless mode dynamically scales concurrent order pressure over time");
            Assert(ShiftManager.Instance.orderSpawnInterval < 12.0f, "Endless mode dynamically accelerates order spawn tempo");

            // 3 failed orders -> shift ends
            ShiftManager.Instance.ordersFailed = 3;
            ShiftManager.Instance.TickShift(1.0f);
            Assert(!ShiftManager.Instance.shiftActive, "3 failed customer orders triggers Endless Mode shift end");

            // Test personal best tracking
            ProgressionManager.Instance.ResetProgressionForTesting();
            bool isNewHigh = ProgressionManager.Instance.RecordEndlessRun(850, 7, 72.5f);
            Assert(isNewHigh, "First endless run recorded as new high score");
            Assert(ProgressionManager.Instance.endlessBestScore == 850, "Endless personal best score updated to 850");

            bool lowerHigh = ProgressionManager.Instance.RecordEndlessRun(500, 4, 45.0f);
            Assert(!lowerHigh, "Lower score run does not overwrite personal best");
            Assert(ProgressionManager.Instance.endlessBestScore == 850, "Endless personal best preserved at 850");
        }

        // Suite 14: Holding Counters, Hover Animation & Throw Mechanics
        private void Test14_HoldingCounterAndThrowMechanics()
        {
            Debug.Log("\n[Suite 14] Holding Counters, Hover Tilt Animation & Throw Mechanics");

            // 1. CounterStation place, pickup & swap
            var counterObj = new GameObject("TestCounterStation");
            var counter = counterObj.AddComponent<CounterStation>();
            counter.stationId = "counter_west_1";

            KitchenItem held = new KitchenItem("pork_portioned", "Portioned Pork Belly");
            var res1 = counter.InteractPrimary(ref held, 1, "linh");
            Assert(res1.ok && held == null && counter.heldItem != null, "Holding counter accepts held item from player");

            var res2 = counter.InteractPrimary(ref held, 1, "linh");
            Assert(res2.ok && held != null && counter.heldItem == null, "Player retrieves held item from counter with empty hands");

            counter.heldItem = new KitchenItem("pickled_veg", "Pickled Veg");
            var swapRes = counter.InteractPrimary(ref held, 1, "linh");
            Assert(swapRes.ok && held.itemId == "pickled_veg" && counter.heldItem.itemId == "pork_portioned", "Holding counter cleanly swaps items when both player and counter have items");

            // 2. Player hover tilt animation
            var pObj = new GameObject("TestChefLinh");
            var p1 = pObj.AddComponent<PlayerController>();
            p1.ConfigureChef(1, "linh", "prep_master", 1);
            p1.SetCarriedItem(held);
            Assert(p1.carriedItem != null, "Chef successfully holding carried item");

            p1.walkAnimTime = 0.5f;
            p1.UpdateCarriedItemHover(0.1f);
            Assert(p1.carriedItemSpriteRenderer == null || p1.carriedItemSpriteRenderer.transform != null, "Carried item hover update calculates smoothly without error");

            // 3. Throw mechanics & partner catch
            var p2Obj = new GameObject("TestChefJiwon");
            var p2 = p2Obj.AddComponent<PlayerController>();
            p2.ConfigureChef(2, "jiwon", "flame_keeper", 1);
            p2.transform.position = p1.transform.position + new Vector3(4.0f, 0.0f, 0.0f);

            var kcObj = new GameObject("KitchenControllerTest");
            var kc = kcObj.AddComponent<KitchenController>();
            KitchenController.Instance = kc;
            kc.playersByPeer[1] = p1;
            kc.playersByPeer[2] = p2;
            kc.stations["counter_west_1"] = counter;

            // Throw item toward partner (aim right)
            bool threw = p1.ThrowItem(Vector2.right);
            Assert(threw, "Chef successfully throws carried item");
            Assert(p1.carriedItem == null, "Chef hands empty after throwing item");
            Assert(p2.carriedItem != null && p2.carriedItem.itemId == "pickled_veg", "Partner chef automatically catches thrown item with empty hands");

            // 4. Tight throw snapping: lands on first table in path, never silently deleted
            Assert(PlayerController.THROW_SNAP_RADIUS < StationBase.MAX_INTERACT_DISTANCE, "Throw snap radius is tighter than interact radius");
            kc.stations.Clear();
            p2.carriedItem = null;
            p2.transform.position = new Vector3(-9.0f, 0.0f, -6.0f); // partner far away (no catch)
            var tPass = new GameObject("ThrowTest_Pass").AddComponent<PassCounter>();
            tPass.stationId = "pass_counter"; tPass.InitSlots();
            tPass.transform.position = new Vector3(0.0f, 0.0f, 0.5f);
            var tTrash = new GameObject("ThrowTest_Trash").AddComponent<TrashStation>();
            tTrash.stationId = "trash_bin";
            tTrash.transform.position = new Vector3(0.0f, 0.0f, -6.0f);
            kc.stations["pass_counter"] = tPass;
            kc.stations["trash_bin"] = tTrash;

            p1.transform.position = new Vector3(-2.5f, 0.0f, 1.8f);
            p1.SetCarriedItem(new KitchenItem("pork_portioned", "Pork"));
            p1.ThrowItem(Vector2.right);
            bool inPass = false;
            for (int i = 0; i < PassCounter.NUM_SLOTS; i++) if (tPass.slots[i] != null && tPass.slots[i].item != null) inPass = true;
            Assert(inPass && p1.carriedItem == null, "Throw at middle exchange table lands in a Pass Counter slot (not trash)");

            p1.transform.position = new Vector3(-3.5f, 0.0f, -4.0f);
            p1.SetCarriedItem(new KitchenItem("pork_portioned", "Pork"));
            bool threwNowhere = p1.ThrowItem(Vector2.right);
            Assert(!threwNowhere && p1.carriedItem != null, "Throw with no table in range keeps item in chef's hands");

            kc.stations.Clear();
            var tCounterObj = new GameObject("ThrowTest_Counter");
            var tCounter = tCounterObj.AddComponent<CounterStation>();
            tCounter.stationId = "counter_east";
            tCounter.transform.position = new Vector3(4.4f, 0.0f, 1.8f);
            kc.stations["counter_east"] = tCounter;
            p1.transform.position = new Vector3(-3.0f, 0.0f, 1.8f);
            p1.ThrowItem(Vector2.right);
            Assert(tCounter.heldItem == null && p1.carriedItem != null, "Far-side table outside snap radius does not grab thrown item");

            DestroyImmediate(tPass.gameObject);
            DestroyImmediate(tTrash.gameObject);
            DestroyImmediate(tCounterObj);

            // Cleanup
            DestroyImmediate(counterObj);
            DestroyImmediate(pObj);
            DestroyImmediate(p2Obj);
            DestroyImmediate(kcObj);
        }

        // Suite 15: Wave Progression, Reputation Health, Gold Economy, Shop & Level Replay
        private void Test15_WaveProgressionReputationAndGoldEconomy()
        {
            Debug.Log("\n[Suite 15] Wave Progression, Reputation Health, Gold Economy, Shop & Level Replay");

            // 1. Gold Reward Formula Verification (floor(200 * (Remaining Reputation / 100)))
            Assert(ProgressionManager.CalculateGoldReward(100f, true) == 200, "100 Reputation awards 200 Gold");
            Assert(ProgressionManager.CalculateGoldReward(75f, true) == 150, "75 Reputation awards 150 Gold");
            Assert(ProgressionManager.CalculateGoldReward(50f, true) == 100, "50 Reputation awards 100 Gold");
            Assert(ProgressionManager.CalculateGoldReward(25f, true) == 50, "25 Reputation awards 50 Gold");
            Assert(ProgressionManager.CalculateGoldReward(10f, true) == 20, "10 Reputation awards 20 Gold");
            Assert(ProgressionManager.CalculateGoldReward(0f, true) == 0, "0 Reputation awards 0 Gold");
            Assert(ProgressionManager.CalculateGoldReward(80f, false) == 0, "Failed level awards 0 Gold regardless of remaining reputation");

            // 2. Reputation Health & Wave Initialization
            ProgressionManager.Instance.ResetProgressionForTesting();
            ShiftManager.Instance.StartShift(1, GameMode.CAMPAIGN);
            Assert(Mathf.Approximately(ShiftManager.Instance.reputation, 100.0f), "Shift begins at 100 Reputation health");
            Assert(ShiftManager.Instance.currentWaveNumber == 1 && ShiftManager.Instance.totalWaves >= 3, "Campaign level initializes at Wave 1 of 3 escalating waves");

            // 3. Reputation Reduction & Clamping
            ShiftManager.Instance.ApplyReputationDamage(25.0f, "Expired customer order");
            Assert(Mathf.Approximately(ShiftManager.Instance.reputation, 75.0f), "Expired order reduces Reputation from 100 to 75");

            // 4. Wave Progression Transition
            int wave1Required = ShiftManager.Instance.waveOrdersRequired;
            for (int i = 0; i < wave1Required; i++)
            {
                if (ShiftManager.Instance.activeOrders.Count == 0)
                {
                    ShiftManager.Instance.activeOrders.Add(new OrderTicket($"test_w1_{i}", "banh_mi_thit", "plate", 60.0f));
                }
                var order = ShiftManager.Instance.activeOrders[0];
                var dish = new KitchenItem(order.recipeId, order.recipeId, "plate");
                ShiftManager.Instance.DeliverDish(dish, 1);
            }
            if (ShiftManager.Instance.isWaveTransitioning)
            {
                ShiftManager.Instance.TickShift(2.0f);
            }
            Assert(ShiftManager.Instance.currentWaveNumber == 2, "Completing all Wave 1 orders advances level to Wave 2");

            // 5. Gold Reward Persistence & Duplicate Claim Prevention
            int goldBefore = ProgressionManager.Instance.gold;
            string token = "test_session_lvl1_claim";
            var claim1 = ProgressionManager.Instance.RecordLevelWaveCompletion(1, 75.0f, true, 600, token);
            Assert(claim1.goldEarned == 150, "Completing Level 1 with 75 Reputation grants 150 Gold");
            Assert(ProgressionManager.Instance.gold == goldBefore + 150, "Player Gold balance increases by 150");
            Assert(ProgressionManager.Instance.IsLevelCompleted(1) && ProgressionManager.Instance.IsLevelUnlocked(2), "Level 1 marked completed and Level 2 unlocked");

            var duplicateClaim = ProgressionManager.Instance.RecordLevelWaveCompletion(1, 75.0f, true, 600, token);
            Assert(duplicateClaim.goldEarned == 0 && ProgressionManager.Instance.gold == goldBefore + 150, "Duplicate result-screen claim token grants 0 Gold");

            // 6. Upgrade Shop & Level Replay Preservation
            var buyBoots = ProgressionManager.Instance.PurchaseUpgrade("chef_speed_boots");
            Assert(buyBoots.ok && ProgressionManager.Instance.IsUpgradeOwned("chef_speed_boots"), "Purchasing Chef Speed Boots succeeds and marks upgrade owned");
            int goldAfterShop = ProgressionManager.Instance.gold;

            // Replay Level 1 with 50 Reputation -> earns 100 Gold, preserves upgrades and Level 2 unlock
            var replayRes = ProgressionManager.Instance.RecordLevelWaveCompletion(1, 50.0f, true, 500, "test_session_lvl1_replay");
            Assert(replayRes.goldEarned == 100 && ProgressionManager.Instance.gold == goldAfterShop + 100, "Replaying completed Level 1 awards 100 Gold at 50 Reputation");
            Assert(ProgressionManager.Instance.IsUpgradeOwned("chef_speed_boots") && ProgressionManager.Instance.IsLevelUnlocked(2), "Replaying earlier level preserves existing upgrades and unlocked levels");

            // 7. Zero Reputation Defeat Condition
            ShiftManager.Instance.StartShift(1, GameMode.CAMPAIGN);
            ShiftManager.Instance.ApplyReputationDamage(120.0f, "Massive kitchen failure");
            Assert(Mathf.Approximately(ShiftManager.Instance.reputation, 0.0f), "Reputation clamps cleanly at 0 and never goes negative");
            Assert(!ShiftManager.Instance.shiftActive && !ShiftManager.Instance.levelVictory, "Reaching 0 Reputation immediately ends level in Defeat");
        }

        // Suite 16: Solo Mode & AI Partner Architecture (6 assertions)
        private void Test16_SoloModeAndAIPartner()
        {
            Debug.Log("\n[Suite 16] Solo Mode & Deterministic AI Partner");

            // Setup mock kitchen controller with 2 chefs
            var kcObj = new GameObject("TestKitchen_SoloAI");
            var kc = kcObj.AddComponent<KitchenController>();
            KitchenController.Instance = kc;

            var linhGo = new GameObject("Chef_Linh");
            var linhPlayer = linhGo.AddComponent<PlayerController>();
            linhPlayer.peerId = 1;
            linhPlayer.characterId = "linh";
            kc.playersByPeer[1] = linhPlayer;

            var jiwonGo = new GameObject("Chef_Jiwon");
            var jiwonPlayer = jiwonGo.AddComponent<PlayerController>();
            jiwonPlayer.peerId = 2;
            jiwonPlayer.characterId = "jiwon";
            kc.playersByPeer[2] = jiwonPlayer;

            // 1. Solo Mode Configuration with Human Linh & AI Jiwon
            kc.ConfigureSoloAndAIMode(true, 1, BotDifficulty.Standard);
            Assert(linhPlayer.isLocalPlayer == true, "Human chef (Linh, Peer 1) is flagged as isLocalPlayer");
            Assert(jiwonPlayer.isLocalPlayer == false, "AI chef (Jiwon, Peer 2) is flagged as !isLocalPlayer");

            var bot = jiwonGo.GetComponent<AI.BotController>();
            Assert(bot != null && bot.botActive == true && bot.characterId == "jiwon", "BotController successfully attached and activated on Jiwon");

            // 2. Difficulty Tiers Verification
            bot.ConfigureDifficulty(BotDifficulty.Assistive);
            Assert(bot.difficulty == BotDifficulty.Assistive, "Bot difficulty successfully reconfigures to Assistive");

            // 3. Direct Item Dispense to Bot Hands without Opening Human UI
            var crateGo = new GameObject("TestCrate");
            var crate = crateGo.AddComponent<CrateStation>();
            crate.dispensedItemId = "tteok_portioned";
            KitchenItem botItem = null;
            var dispenseRes = crate.DispenseItemDirect(ref botItem, "tteok_portioned");
            Assert(dispenseRes.ok && botItem != null && botItem.itemId == "tteok_portioned", "CrateStation directly dispenses requested ingredient to bot hands");

            // 4. Clean Switch to Human Jiwon & AI Linh
            kc.ConfigureSoloAndAIMode(true, 2, BotDifficulty.Challenge);
            Assert(jiwonPlayer.isLocalPlayer == true, "Switching solo role sets Jiwon as isLocalPlayer");
            Assert(linhPlayer.isLocalPlayer == false, "Switching solo role sets Linh as !isLocalPlayer");
            var linhBot = linhGo.GetComponent<AI.BotController>();
            Assert(linhBot != null && linhBot.botActive == true && linhBot.difficulty == BotDifficulty.Challenge, "BotController attaches to Linh with Challenge difficulty");

            // 5. Human Jiwon gets input focus, fridge/shelf wheel, and own catalogs
            Assert(NetworkManager.Instance != null && NetworkManager.Instance.localPeerId == 2, "Solo as Jiwon makes Jiwon the local (controlled) peer");
            Assert(CrateStation.IsHumanInteractor(2) && !CrateStation.IsHumanInteractor(1), "Jiwon (human) opens fridge/shelf wheel; Linh bot does not");
            var eastFridge = QuickSelectWheelUI.GetStationCatalogIds("fridge_east");
            var eastShelf = QuickSelectWheelUI.GetStationCatalogIds("shelf_east");
            var westFridge = new System.Collections.Generic.List<string>(QuickSelectWheelUI.GetStationCatalogIds("fridge_west"));
            var westShelf = new System.Collections.Generic.List<string>(QuickSelectWheelUI.GetStationCatalogIds("shelf_west"));
            bool overlap = false;
            foreach (var id in eastFridge) if (westFridge.Contains(id) || westShelf.Contains(id)) overlap = true;
            foreach (var id in eastShelf) if (westFridge.Contains(id) || westShelf.Contains(id)) overlap = true;
            Assert(eastFridge.Length == 2 && eastShelf.Length == 2 && !overlap, "Jiwon fridge & shelf each offer 2 items not in Linh's fridge/shelf");

            // Back to Linh human: focus returns to Peer 1
            kc.ConfigureSoloAndAIMode(true, 1, BotDifficulty.Standard);
            Assert(NetworkManager.Instance.localPeerId == 1 && linhPlayer.isLocalPlayer && !jiwonPlayer.isLocalPlayer, "Solo as Linh restores Linh as local peer");

            // Cleanup test objects
            DestroyImmediate(crateGo);
            DestroyImmediate(linhGo);
            DestroyImmediate(jiwonGo);
            DestroyImmediate(kcObj);
        }

        // Suite 17: Two-Bot QA Manager & Report Generation (5 assertions)
        private void Test17_TwoBotQAManagerAndReporting()
        {
            Debug.Log("\n[Suite 17] Two-Bot QA Autoplay Manager & Reporting");

            var qaGo = new GameObject("TestQAManager");
            var qa = qaGo.AddComponent<AI.TwoBotQAManager>();

            // Setup mock chefs
            var kcObj = new GameObject("TestKitchen_QA");
            var kc = kcObj.AddComponent<KitchenController>();
            KitchenController.Instance = kc;

            var linhGo = new GameObject("Chef_Linh");
            var linhPlayer = linhGo.AddComponent<PlayerController>();
            linhPlayer.peerId = 1;
            linhPlayer.characterId = "linh";
            kc.playersByPeer[1] = linhPlayer;

            var jiwonGo = new GameObject("Chef_Jiwon");
            var jiwonPlayer = jiwonGo.AddComponent<PlayerController>();
            jiwonPlayer.peerId = 2;
            jiwonPlayer.characterId = "jiwon";
            kc.playersByPeer[2] = jiwonPlayer;

            // 1. Two-Bot Configuration
            qa.ConfigureTwoBots();
            Assert(linhPlayer.isLocalPlayer == false && jiwonPlayer.isLocalPlayer == false, "Both chefs set to isLocalPlayer == false in Two-Bot QA mode");
            Assert(linhGo.GetComponent<AI.BotController>() != null && jiwonGo.GetComponent<AI.BotController>() != null, "Both Linh and Jiwon equipped with active BotControllers");

            // 2. Structured Report Serialization & File Generation
            string tempReportDir = System.IO.Path.Combine(Application.temporaryCachePath, "test_qa_reports");
            var sampleReport = new AI.QARunReport
            {
                timestamp = "2026-11-09 12:00:00 UTC",
                seed = 12345,
                daysAttempted = 5,
                daysCleared = 5,
                totalScore = 2450,
                totalOrdersServed = 28,
                totalOrdersFailed = 0,
                overallSuccessRate = 1.0f
            };
            sampleReport.dayResults.Add(new AI.QADayResult { day = 1, cleared = true, score = 400, ordersServed = 5, ordersFailed = 0, remainingReputation = 100f, durationSeconds = 45f });
            sampleReport.dayResults.Add(new AI.QADayResult { day = 2, cleared = true, score = 480, ordersServed = 6, ordersFailed = 0, remainingReputation = 100f, durationSeconds = 50f });

            AI.TwoBotQAManager.SaveReportsToDisk(sampleReport, tempReportDir);

            string jsonPath = System.IO.Path.Combine(tempReportDir, "latest_results.json");
            string summaryPath = System.IO.Path.Combine(tempReportDir, "latest_summary.txt");
            string csvPath = System.IO.Path.Combine(tempReportDir, "run_history.csv");

            Assert(System.IO.File.Exists(jsonPath), "latest_results.json successfully generated on disk");
            Assert(System.IO.File.Exists(summaryPath), "latest_summary.txt successfully generated on disk");
            Assert(System.IO.File.Exists(csvPath), "run_history.csv successfully appended on disk");

            // 3. A* Waypoint Pathfinding via KitchenNavGraph
            AI.KitchenNavGraph.EnsureInitialized();
            Vector3 startPos = new Vector3(-8.8f, 0f, 4.2f); // NW_Outer
            Vector3 targetPos = new Vector3(-2.6f, 0f, -4.8f); // W_Serving_App
            var navPath = AI.KitchenNavGraph.FindPath(startPos, targetPos, 1);
            Assert(navPath != null && navPath.Count >= 2, "KitchenNavGraph generates valid multi-waypoint A* corridor path");
            Assert(Vector3.Distance(navPath[navPath.Count - 1], targetPos) < 0.01f, "A* path ends exactly at target approach waypoint");

            // 4. Inter-Bot Communication Hub Coordination
            AI.BotCommunicationHub.Reset();
            AI.BotCommunicationHub.BroadcastIntent(1, "linh", "Cook Rice", "noodle_blancher", "korean_bbq_rice");
            var partnerIntent = AI.BotCommunicationHub.GetPartnerIntent(2);
            Assert(partnerIntent != null && partnerIntent.currentTask == "Cook Rice", "BotCommunicationHub shares live chef intent with partner");

            AI.BotCommunicationHub.PostHandoff(1, "bbq_beef", 2, "korean_bbq_rice");
            var pendingHandoff = AI.BotCommunicationHub.GetPendingHandoffFor(1);
            Assert(pendingHandoff != null && pendingHandoff.itemId == "bbq_beef" && pendingHandoff.slotIndex == 1, "Pending pass counter handoff notified to receiving bot");

            AI.BotCommunicationHub.ClaimHandoff(1, 1);
            var claimedCheck = AI.BotCommunicationHub.GetPendingHandoffFor(1);
            Assert(claimedCheck == null, "Claimed handoff is cleared from pending notifications");

            // 5. Mobile QA: reports must go to writable app storage (APK dataPath is read-only)
            string mobileDir = AI.TwoBotQAManager.ResolveReportsDirectory(true, "/data/app/base.apk", "/storage/emulated/0/Android/data/twoladles/files");
            Assert(mobileDir.Replace('\\', '/').StartsWith("/storage/emulated/0/Android/data/twoladles/files"), "Mobile QA reports saved under persistentDataPath");
            string deskDir = AI.TwoBotQAManager.ResolveReportsDirectory(false, "C:/Game/TwoLadles_Data", "C:/Users/x/AppData");
            Assert(!deskDir.Replace('\\', '/').StartsWith("C:/Users/x/AppData"), "Desktop QA reports keep project qa_reports folder");

            // 6. Mobile joystick sits further up and right than the old (92,92) corner spot
            Assert(UI.KitchenHUD.MOBILE_JOYSTICK_POS.x > 92f && UI.KitchenHUD.MOBILE_JOYSTICK_POS.y > 92f, "Mobile joystick moved up and right from the corner");

            // Cleanup
            if (System.IO.Directory.Exists(tempReportDir)) System.IO.Directory.Delete(tempReportDir, true);
            DestroyImmediate(linhGo);
            DestroyImmediate(jiwonGo);
            DestroyImmediate(kcObj);
            DestroyImmediate(qaGo);
        }

        private void Test18_StoryPhoneAndDayNightCycle()
        {
            Debug.Log("\n[TEST 18] Story Phone Script & Day-Night Lighting Stages");

            // Script structure (final five-day script, verbatim)
            Assert(Story.StoryScript.Opening.Length == 8, "Opening chat has 8 messages");
            Assert(Story.StoryScript.Days.Length == 5, "Story has 5 days");
            int[] expectedCounts = { 0, 7, 8, 9, 9 };
            string[] expectedPresenters = { "jiwon", "linh", "jiwon", "linh", "both" };
            string[] expectedDishes = { "tteokbokki", "bot_chien", "korean_bbq_rice", "viet_broken_rice", "bulgogi_banh_mi" };
            for (int i = 0; i < 5; i++)
            {
                var d = Story.StoryScript.GetDay(i + 1);
                Assert(d != null && d.chat.Length == expectedCounts[i], $"Day {i + 1} chat has {expectedCounts[i]} messages");
                Assert(d != null && d.presenter == expectedPresenters[i], $"Day {i + 1} dish presented by {expectedPresenters[i]}");
                Assert(d != null && d.dishId == expectedDishes[i], $"Day {i + 1} dish is {expectedDishes[i]}");
            }
            Assert(Story.StoryScript.GetDay(0) == null && Story.StoryScript.GetDay(6) == null, "No story outside days 1-5");

            // Every line and card exists in all three languages
            bool allFilled = true;
            foreach (var l in Story.StoryScript.Opening)
                if (string.IsNullOrEmpty(l.en) || string.IsNullOrEmpty(l.vi) || string.IsNullOrEmpty(l.ko)) allFilled = false;
            foreach (var d in Story.StoryScript.Days)
            {
                foreach (var l in d.chat)
                    if (string.IsNullOrEmpty(l.en) || string.IsNullOrEmpty(l.vi) || string.IsNullOrEmpty(l.ko)) allFilled = false;
                if (string.IsNullOrEmpty(d.descEn) || string.IsNullOrEmpty(d.descVi) || string.IsNullOrEmpty(d.descKo)) allFilled = false;
            }
            Assert(allFilled, "All story lines and dish cards have EN/VI/KO text");

            // Vietnamese casual texting preserved exactly
            Assert(Story.StoryScript.Opening[7].vi == "Ko sạch ko làm nha.", "Vietnamese slang preserved verbatim (opening)");
            Assert(Story.StoryScript.Opening[7].Get("ko") == "안 깨끗하면 일 안 할 거야.", "Korean line selected by language code");
            Assert(Story.StoryScript.Opening[7].Get("en") == "If it's not clean, I'm not working.", "English line selected by language code");

            // Story only shows on campaign days 1-5 (never tutorial / QA autoplay / endless)
            Assert(UI.PhoneStoryUI.ShouldShowStory(1, GameMode.CAMPAIGN, false), "Story shows on campaign Day 1");
            Assert(UI.PhoneStoryUI.ShouldShowStory(5, GameMode.CAMPAIGN, false), "Story shows on campaign Day 5");
            Assert(!UI.PhoneStoryUI.ShouldShowStory(0, GameMode.CAMPAIGN, false), "Story never shows in tutorial");
            Assert(!UI.PhoneStoryUI.ShouldShowStory(3, GameMode.CAMPAIGN, true), "Story never shows in QA autoplay");
            Assert(!UI.PhoneStoryUI.ShouldShowStory(3, GameMode.ENDLESS_RUSH, false), "Story never shows in endless mode");

            // Dating-app match scene: host / solo see it first time; LAN client always skips it
            Assert(UI.PhoneStoryUI.ShouldShowMatchScene(1, true, false), "Match scene shows on Day 1 first time for host/solo");
            Assert(UI.PhoneStoryUI.ShouldShowMatchScene(1, true, true), "LAN guest also sees the match scene");
            Assert(UI.PhoneStoryUI.ShouldShowMatchScene(1, false, true), "LAN co-op: both phones show the match scene on Day 1 even if already seen (stay in sync)");
            Assert(!UI.PhoneStoryUI.ShouldShowMatchScene(2, true, true), "LAN co-op: match scene still only on Day 1");
            Assert(!UI.PhoneStoryUI.ShouldShowMatchScene(1, false, false), "Match scene not repeated once seen");
            Assert(!UI.PhoneStoryUI.ShouldShowMatchScene(2, true, false), "Match scene only on Day 1");
            Assert(Mathf.Approximately(UI.PhoneStoryUI.PORTRAIT_FRAME_W, UI.PhoneStoryUI.PORTRAIT_FRAME_H), "Jiwon swipe-card portrait frame is square");
            Assert(UI.PhoneStoryUI.PORTRAIT_ZOOM_W < 270f, "Jiwon swipe-card head drawn smaller (less zoom) on all platforms");
            Assert(UI.PhoneStoryUI.PortraitDrop(true) > -35f, "Jiwon swipe-card sprite sits higher on mobile than before");
            Assert(Player.PlayerController.CARRIED_ITEM_HEIGHT >= 2.2f, "Carried item floats high enough to keep the chef's face visible");

            // Lighting changes in one stage per wave
            Assert(Visuals.DayNightCycleManager.StageForWave(1, 3) == 0, "3 waves: wave 1 = day");
            Assert(Visuals.DayNightCycleManager.StageForWave(2, 3) == 1, "3 waves: wave 2 = red noon");
            Assert(Visuals.DayNightCycleManager.StageForWave(3, 3) == 2, "3 waves: wave 3 = night");
            Assert(Visuals.DayNightCycleManager.StageForWave(0, 3) == -1, "No lighting stage before first wave");
            Assert(Visuals.DayNightCycleManager.StageForWave(2, 2) == 2, "2 waves: last wave reaches night");

            // Recipe Book discovery: future days stay blank until reached; today's recipe is the new one
            Assert(UI.KitchenHUD.IsRecipeUnlocked(1, 1, GameMode.CAMPAIGN), "Day 1: Day 1 recipe discovered");
            Assert(!UI.KitchenHUD.IsRecipeUnlocked(2, 1, GameMode.CAMPAIGN), "Day 1: Day 2 recipe still unknown");
            Assert(UI.KitchenHUD.IsRecipeUnlocked(3, 3, GameMode.CAMPAIGN) && UI.KitchenHUD.IsRecipeUnlocked(1, 3, GameMode.CAMPAIGN), "Day 3: Days 1-3 recipes discovered");
            Assert(!UI.KitchenHUD.IsRecipeUnlocked(4, 3, GameMode.CAMPAIGN), "Day 3: Day 4 recipe still unknown");
            Assert(UI.KitchenHUD.IsRecipeUnlocked(5, 5, GameMode.CAMPAIGN), "Day 5: all recipes discovered");
            Assert(UI.KitchenHUD.IsRecipeUnlocked(1, 0, GameMode.CAMPAIGN) && !UI.KitchenHUD.IsRecipeUnlocked(2, 0, GameMode.CAMPAIGN), "Tutorial: only Day 1 recipe shown");
            Assert(UI.KitchenHUD.IsRecipeUnlocked(5, 1, GameMode.ENDLESS_RUSH), "Endless Rush: all recipes shown");
            Assert(UI.KitchenHUD.IsNewRecipe(3, 3, GameMode.CAMPAIGN), "Day 3 recipe is the new (fade-in) recipe on Day 3");
            Assert(!UI.KitchenHUD.IsNewRecipe(2, 3, GameMode.CAMPAIGN), "Day 2 recipe is not new on Day 3");
            Assert(!UI.KitchenHUD.IsNewRecipe(5, 5, GameMode.ENDLESS_RUSH), "No new-recipe fade in Endless Rush");
        }

        // Suite 19: LAN Order Sync, Recipe Timing Variety, Part-Timer Size & Bot Counter Safety
        private void Test19_LanSyncTimingAndBotSafety()
        {
            Debug.Log("\n[Suite 19] LAN Order Sync, Recipe Timing, Part-Timer Size & Counter Safety");

            // 1. Part-timer worker sprite is readable (not tiny)
            Assert(KitchenController.PART_TIMER_HEIGHT >= 1.7f, "Part-timer worker drawn at least 1.7 units tall");

            // 2. Counter never swaps identical items (prevents endless beef-for-beef swap loop)
            var cObj = new GameObject("TestCounterSame");
            var c = cObj.AddComponent<CounterStation>();
            c.stationId = "counter_west_9";
            c.heldItem = new KitchenItem("bbq_beef", "BBQ Beef");
            KitchenItem hand = new KitchenItem("bbq_beef", "BBQ Beef");
            var sameRes = c.InteractPrimary(ref hand, 1, "linh");
            Assert(!sameRes.ok, "Counter rejects swapping an identical item");
            Assert(hand != null && hand.itemId == "bbq_beef" && c.heldItem != null && c.heldItem.itemId == "bbq_beef", "Identical-item rejection leaves both items in place");
            DestroyImmediate(cObj);

            // 3. Harder Day 3/4 recipes get 1-2 extra seconds of customer patience
            float bbqBonus = ShiftManager.GetRecipePatienceBonus("korean_bbq_rice");
            float brokenBonus = ShiftManager.GetRecipePatienceBonus("viet_broken_rice");
            Assert(bbqBonus >= 1f && bbqBonus <= 2f, "Korean BBQ Rice (Day 3) gets 1-2s extra patience");
            Assert(brokenBonus >= 1f && brokenBonus <= 2f, "Viet Broken Rice (Day 4) gets 1-2s extra patience");
            Assert(ShiftManager.GetRecipePatienceBonus("tteokbokki") == 0f, "Day 1 Tteokbokki gets no extra patience");

            // 4. Host -> client order/score sync round-trip
            var t1 = new OrderTicket("ticket_7", new List<string> { "tteokbokki", "bot_chien" }, "bowl", 72.5f);
            t1.timeRemaining = 40.25f;
            t1.MarkItemFulfilled(1);
            var t2 = new OrderTicket("tick|et;8,x", "korean_bbq_rice", "plate", 67f);
            var state = new NetworkShiftState
            {
                score = 1234, reputation = 75f, flow = 42.5f, rushActive = true, rushRemaining = 9.5f,
                waveCompleted = 2, waveRequired = 5,
                orders = new List<OrderTicket> { t1, t2 }
            };
            string packet = ShiftManager.EncodeNetworkState(state);
            var decoded = ShiftManager.DecodeNetworkState(packet);
            Assert(decoded != null, "Order sync packet decodes");
            Assert(decoded != null && decoded.score == 1234 && Mathf.Approximately(decoded.reputation, 75f) && Mathf.Approximately(decoded.flow, 42.5f), "Score, reputation and flow survive sync");
            Assert(decoded != null && decoded.rushActive && Mathf.Approximately(decoded.rushRemaining, 9.5f) && decoded.waveCompleted == 2 && decoded.waveRequired == 5, "Rush and wave progress survive sync");
            Assert(decoded != null && decoded.orders.Count == 2, "Both orders survive sync");
            if (decoded != null && decoded.orders.Count == 2)
            {
                var d1 = decoded.orders[0];
                Assert(d1.ticketId == "ticket_7" && d1.requiredItems.Count == 2 && d1.requiredItems[1] == "bot_chien" && d1.targetVessel == "bowl", "Order ids, items and vessel preserved");
                Assert(Mathf.Abs(d1.timeRemaining - 40.25f) < 0.01f && Mathf.Abs(d1.initialPatience - 72.5f) < 0.01f, "Order timers preserved");
                Assert(d1.fulfilledIndices.Contains(1) && !d1.fulfilledIndices.Contains(0) && d1.recipeId == "tteokbokki", "Fulfilled dishes preserved");
                var d2 = decoded.orders[1];
                Assert(!d2.ticketId.Contains("|") && !d2.ticketId.Contains(";") && !d2.ticketId.Contains(","), "Separator characters stripped from synced ids");
                Assert(d2.requiredItems.Count == 1 && d2.requiredItems[0] == "korean_bbq_rice", "Malicious id does not corrupt following fields");
            }
            Assert(ShiftManager.DecodeNetworkState("ORDERS|garbage") == null, "Malformed sync packet rejected safely");
            Assert(ShiftManager.DecodeNetworkState(null) == null, "Null sync packet rejected safely");

            // 5. Bots trash wrong/unneeded baguettes; other unneeded food is parked on a holding table
            Assert(BotController.IsBaguetteItem("baguette") && BotController.IsBaguetteItem("banh_mi_partial_bp") && BotController.IsBaguetteItem("baguette_dough"), "Baguette-family items recognised");
            Assert(!BotController.IsBaguetteItem("cooked_flour") && !BotController.IsBaguetteItem("bbq_beef"), "Non-baguette items not treated as baguette");
            Assert(BotController.ShouldTrashInsteadOfHold("baguette", false), "Unneeded baguette goes to trash");
            Assert(BotController.ShouldTrashInsteadOfHold("banh_mi_partial_bb", false), "Wrong partial bánh mì goes to trash");
            Assert(!BotController.ShouldTrashInsteadOfHold("baguette", true), "Baguette still needed by an order is kept");
            Assert(!BotController.ShouldTrashInsteadOfHold("cooked_flour", false), "Other unneeded food is kept on a holding table");

            // 6. Settings QA test: day selection + temporary all-upgrades sandbox that never touches the real save
            var r0 = TwoBotQAManager.ResolveDayRange(0);
            var r3 = TwoBotQAManager.ResolveDayRange(3);
            var r9 = TwoBotQAManager.ResolveDayRange(9);
            Assert(r0.start == 1 && r0.end == 5, "QA day option 'All' runs Days 1-5");
            Assert(r3.start == 3 && r3.end == 3, "QA day option 3 runs only Day 3");
            Assert(r9.start == 5 && r9.end == 5, "QA day option clamps to Day 5");

            var pmQA = ProgressionManager.Instance;
            pmQA.ResetProgressionForTesting();
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, "progression.json");
            string saveBefore = System.IO.File.Exists(savePath) ? System.IO.File.ReadAllText(savePath) : "";
            pmQA.BeginQASandbox(true);
            Assert(pmQA.qaSandboxActive, "QA sandbox active during bot test");
            Assert(pmQA.linhTier == 3 && pmQA.jiwonTier == 3, "QA all-upgrades gives both chefs Tier 3");
            Assert(pmQA.IsUpgradeOwned("specialist_tier_3") && pmQA.IsUpgradeOwned("specialist_tier_2"), "QA all-upgrades owns specialist tiers");
            Assert(pmQA.coins >= 1000, "QA all-upgrades has gold to hire part-timers");
            pmQA.coins += 5;
            pmQA.SaveProgression();
            string saveDuring = System.IO.File.Exists(savePath) ? System.IO.File.ReadAllText(savePath) : "";
            Assert(saveDuring == saveBefore, "Saving during QA sandbox does not overwrite the real save file");
            pmQA.EndQASandbox();
            Assert(!pmQA.qaSandboxActive, "QA sandbox ends after bot test");
            Assert(pmQA.coins == 150 && pmQA.linhTier == 1 && pmQA.jiwonTier == 1, "Real gold and tiers restored after QA");
            Assert(!pmQA.IsUpgradeOwned("specialist_tier_3"), "QA upgrades removed after bot test");

            // 7. LAN Wi-Fi scan helpers: never use localhost / link-local, sweep the whole /24 Wi-Fi subnet
            Assert(!NetworkManager.IsUsableLanAddress("127.0.0.1"), "Localhost is not a usable Wi-Fi address");
            Assert(!NetworkManager.IsUsableLanAddress("169.254.3.4"), "Link-local (no DHCP) is not a usable Wi-Fi address");
            Assert(!NetworkManager.IsUsableLanAddress("") && !NetworkManager.IsUsableLanAddress("not-an-ip") && !NetworkManager.IsUsableLanAddress(null), "Empty / garbage addresses rejected");
            Assert(NetworkManager.IsUsableLanAddress("192.168.1.5") && NetworkManager.IsUsableLanAddress("10.0.0.7"), "Normal Wi-Fi addresses accepted");
            Assert(NetworkManager.GetSubnetBroadcast("192.168.1.37", "255.255.255.0") == "192.168.1.255", "Subnet broadcast for /24 Wi-Fi");
            Assert(NetworkManager.GetSubnetBroadcast("10.0.5.9", "255.255.0.0") == "10.0.255.255", "Subnet broadcast for /16 network");
            Assert(NetworkManager.GetSubnetBroadcast("garbage", "255.255.255.0") == null, "Invalid subnet input returns null");
            var sweep = NetworkManager.BuildSubnetSweepTargets("192.168.1.37");
            Assert(sweep.Count == 253, "Wi-Fi sweep probes 253 neighbours (.1-.254 minus self)");
            Assert(!sweep.Contains("192.168.1.37") && !sweep.Contains("192.168.1.0") && !sweep.Contains("192.168.1.255"), "Sweep skips self, network and broadcast addresses");
            Assert(sweep.Contains("192.168.1.1") && sweep.Contains("192.168.1.254"), "Sweep covers router and last host");
            Assert(NetworkManager.BuildSubnetSweepTargets("127.0.0.1").Count == 0, "No sweep when only localhost is available");
            Assert(NetworkManager.GetSubnetPrefix("192.168.1.37") == "192.168.1." && NetworkManager.GetSubnetPrefix("127.0.0.1") == "", "Join box pre-fills Wi-Fi prefix, never localhost");

            // 8. Lobby: nobody starts Ready, HELLO says not-ready, 2 ready chefs required to start
            Assert(!NetworkManager.CreateHostPlayer("Host").ready && !NetworkManager.CreateClientPlayer("Guest").ready, "Host and joining chef both start NOT Ready");
            string hello = NetworkManager.BuildHelloPacket("Bad|Name;x");
            Assert(hello.StartsWith("HELLO|2|") && hello.EndsWith("|jiwon|flame_keeper|0"), "HELLO packet announces client as not Ready");
            Assert(hello.Split('|').Length == 6 && !hello.Contains(";"), "Player name separators stripped from HELLO packet");
            var nmLan = NetworkManager.Instance;
            nmLan.players.Clear();
            nmLan.players[1] = NetworkManager.CreateHostPlayer("Host");
            nmLan.players[1].ready = true;
            Assert(!nmLan.CanStartShift(true).ok, "Host cannot start a LAN shift alone");
            nmLan.players[2] = NetworkManager.CreateClientPlayer("Guest");
            Assert(!nmLan.CanStartShift(true).ok, "Host cannot start while partner is not Ready");
            nmLan.players[2].ready = true;
            Assert(nmLan.CanStartShift(true).ok, "Shift starts once both chefs are Ready");
            nmLan.players.Clear();

            // 9. Manual IP join: host shows its Wi-Fi IP(s), joiner types it in
            Assert(NetworkManager.NormalizeTypedIp(" 192.168.1.5 ") == "192.168.1.5", "Typed IP is trimmed");
            Assert(NetworkManager.NormalizeTypedIp("192,168,1,5") == "192.168.1.5", "Commas typed on phone keypads become dots");
            Assert(NetworkManager.NormalizeTypedIp("192.168.1.5:24567") == "192.168.1.5", "Port suffix is stripped from typed IP");
            Assert(NetworkManager.NormalizeTypedIp("192.168.1.") == "" && NetworkManager.NormalizeTypedIp("hello") == "" && NetworkManager.NormalizeTypedIp(null) == "", "Incomplete / garbage typed IP rejected");
            Assert(NetworkManager.NormalizeTypedIp("127.0.0.1") == "", "Typed localhost rejected (partner must use host's Wi-Fi IP)");
            var allIps = NetworkManager.GetAllUsableLanIPv4();
            Assert(allIps != null, "Host IP list is never null");
            bool allUsable = true;
            foreach (var a in allIps) if (!NetworkManager.IsUsableLanAddress(a)) allUsable = false;
            Assert(allUsable, "Host IP list never shows localhost / link-local");

            // 10. LAN gameplay sync: each device drives only its own chef; host owns station state
            var nmSync = NetworkManager.Instance;
            bool prevQA = ShiftManager.Instance != null && ShiftManager.Instance.isAutoplayQAMode;
            if (ShiftManager.Instance != null) ShiftManager.Instance.isAutoplayQAMode = false;
            nmSync.isLanActive = true;
            nmSync.localPeerId = 2;

            var kcSyncObj = new GameObject("KC_Sync");
            var kcSync = kcSyncObj.AddComponent<KitchenController>();
            KitchenController.Instance = kcSync;
            var sp1 = new GameObject("SyncChef1").AddComponent<PlayerController>();
            sp1.ConfigureChef(1, "linh", "prep_master", 1);
            var sp2 = new GameObject("SyncChef2").AddComponent<PlayerController>();
            sp2.ConfigureChef(2, "jiwon", "flame_keeper", 1);
            kcSync.playersByPeer[1] = sp1;
            kcSync.playersByPeer[2] = sp2;
            kcSync.ConfigureSoloAndAIMode(false, 1, BotDifficulty.Standard);
            Assert(!sp1.isLocalPlayer && sp2.isLocalPlayer, "LAN client device controls only its own chef (not both)");
            nmSync.localPeerId = 1;
            kcSync.ConfigureSoloAndAIMode(false, 1, BotDifficulty.Standard);
            Assert(sp1.isLocalPlayer && !sp2.isLocalPlayer, "LAN host device controls only its own chef (not both)");

            // Host-side stations
            var hCounter = new GameObject("H_Counter").AddComponent<CounterStation>(); hCounter.stationId = "counter_sync";
            hCounter.heldItem = new KitchenItem("bad|id;x,y~z", "Bad");
            var hCook = new GameObject("H_Cook").AddComponent<CookingStation>(); hCook.stationId = "grill_sync";
            hCook.heldItem = new KitchenItem("bulgogi_portioned", "Bulgogi");
            hCook.currentState = CookingState.COOKING; hCook.cookingElapsedSeconds = 3.5f;
            var hPass = new GameObject("H_Pass").AddComponent<PassCounter>(); hPass.stationId = "pass_sync"; hPass.InitSlots();
            hPass.slots[1].item = new KitchenItem("rice_portioned", "Rice"); hPass.slots[1].placedByPeer = 2;
            var hAsm = new GameObject("H_Asm").AddComponent<AssemblyStation>(); hAsm.stationId = "asm_sync";
            hAsm.heldItem = new KitchenItem("plate", "Plate", "plate", QualityTier.OPTIMAL);
            hAsm.heldItem.assembledComponents.Add("baguette"); hAsm.heldItem.assembledComponents.Add("bbq_beef");
            hAsm.placedComponents.Add("baguette"); hAsm.placedComponents.Add("bbq_beef");
            var hPrep = new GameObject("H_Prep").AddComponent<PrepStation>(); hPrep.stationId = "prep_sync";
            hPrep.heldItem = new KitchenItem("pork_raw", "Pork"); hPrep.strikesCompleted = 2;
            sp1.carriedItem = new KitchenItem("tteok_portioned", "Tteok", "none", QualityTier.OPTIMAL);
            sp2.carriedItem = null;

            string snap = NetworkManager.EncodeStationSnapshot(new StationBase[] { hCounter, hCook, hPass, hAsm, hPrep, hCook }, kcSync.playersByPeer);
            Assert(snap != null && snap.StartsWith("STATIONS|"), "Host encodes a STATIONS snapshot packet");
            Assert(snap.Split('|').Length == 1 + 5 + 2, "Snapshot holds each station once plus both chefs' hands");

            // Client-side copies (same ids, different/empty state)
            var cCounter = new GameObject("C_Counter").AddComponent<CounterStation>(); cCounter.stationId = "counter_sync";
            var cCook = new GameObject("C_Cook").AddComponent<CookingStation>(); cCook.stationId = "grill_sync";
            var cPass = new GameObject("C_Pass").AddComponent<PassCounter>(); cPass.stationId = "pass_sync"; cPass.InitSlots();
            cPass.slots[0].item = new KitchenItem("stale", "Stale");
            var cAsm = new GameObject("C_Asm").AddComponent<AssemblyStation>(); cAsm.stationId = "asm_sync";
            var cPrep = new GameObject("C_Prep").AddComponent<PrepStation>(); cPrep.stationId = "prep_sync";
            var cStations = new Dictionary<string, StationBase>
            {
                { "counter_sync", cCounter }, { "grill_sync", cCook }, { "pass_sync", cPass }, { "asm_sync", cAsm }, { "prep_sync", cPrep }
            };
            var cc1 = new GameObject("C_Chef1").AddComponent<PlayerController>(); cc1.ConfigureChef(1, "linh", "prep_master", 1);
            var cc2 = new GameObject("C_Chef2").AddComponent<PlayerController>(); cc2.ConfigureChef(2, "jiwon", "flame_keeper", 1);
            cc2.carriedItem = new KitchenItem("junk", "Junk");
            var cChefs = new Dictionary<int, PlayerController> { { 1, cc1 }, { 2, cc2 } };

            Assert(NetworkManager.ApplyStationSnapshot(snap, cStations, cChefs), "Client applies host STATIONS snapshot");
            Assert(cCounter.heldItem != null && cCounter.heldItem.itemId == "badidxyz", "Counter item synced (separator chars stripped)");
            Assert(cCook.heldItem != null && cCook.heldItem.itemId == "bulgogi_portioned" && cCook.currentState == CookingState.COOKING && Mathf.Abs(cCook.cookingElapsedSeconds - 3.5f) < 0.02f, "Cooking item, state and timer synced");
            Assert(cPass.slots[0].item == null && cPass.slots[1].item != null && cPass.slots[1].item.itemId == "rice_portioned" && cPass.slots[1].placedByPeer == 2 && cPass.slots[2].item == null, "Pass Counter slots synced (stale slot cleared)");
            Assert(cAsm.heldItem != null && cAsm.heldItem.vesselType == "plate" && cAsm.heldItem.quality == QualityTier.OPTIMAL && cAsm.heldItem.assembledComponents.Count == 2 && cAsm.placedComponents.Count == 2 && cAsm.placedComponents[1] == "bbq_beef", "Assembly vessel, quality and components synced");
            Assert(cPrep.heldItem != null && cPrep.heldItem.itemId == "pork_raw" && cPrep.strikesCompleted == 2, "Prep item and chop progress synced");
            Assert(cc1.carriedItem != null && cc1.carriedItem.itemId == "tteok_portioned" && cc1.carriedItem.quality == QualityTier.OPTIMAL, "Partner chef's held item synced");
            Assert(cc2.carriedItem == null, "Own held item follows host (empty hands cleared)");

            hCounter.heldItem = null;
            string snap2 = NetworkManager.EncodeStationSnapshot(new StationBase[] { hCounter }, kcSync.playersByPeer);
            NetworkManager.ApplyStationSnapshot(snap2, cStations, cChefs);
            Assert(cCounter.heldItem == null, "Item removed on host is removed on client");

            bool threw = false;
            try
            {
                Assert(!NetworkManager.ApplyStationSnapshot(null, cStations, cChefs), "Null snapshot rejected");
                Assert(!NetworkManager.ApplyStationSnapshot("garbage", cStations, cChefs), "Garbage snapshot rejected");
                NetworkManager.ApplyStationSnapshot("STATIONS|S;x;;|H;abc", cStations, cChefs);
                NetworkManager.ApplyStationSnapshot("STATIONS|S;grill_sync;a;9;b;c;zz;nan;q;r;p0~x", cStations, null);
            }
            catch { threw = true; }
            Assert(!threw, "Malformed snapshots never crash the client");

            // LAN client never mutates station state locally: it only sends an intent to the host
            nmSync.localPeerId = 2;
            var lanCounter = new GameObject("LanCounter").AddComponent<CounterStation>(); lanCounter.stationId = "lan_counter";
            lanCounter.transform.position = Vector3.zero;
            lanCounter.heldItem = new KitchenItem("rice_portioned", "Rice");
            sp2.transform.position = new Vector3(1.0f, 0.0f, 0.0f);
            sp2.carriedItem = null;
            kcSync.stations["lan_counter"] = lanCounter;
            var lanRes = sp2.TryInteractPrimary(lanCounter);
            Assert(lanRes.ok && lanCounter.heldItem != null && lanCounter.heldItem.itemId == "rice_portioned" && sp2.carriedItem == null, "LAN client interact sends intent only (host decides)");
            sp2.carriedItem = new KitchenItem("tteok_portioned", "Tteok");
            sp2.ThrowItem(Vector2.right);
            Assert(sp2.carriedItem != null, "LAN client throw waits for host result");

            // 11. Per-chef upgrades over LAN (mixed tiers) + part-timer visuals on both devices
            var upDict = new Dictionary<string, float> { { "move_speed", 1.2f }, { "cook_speed", 1.5f }, { "stockpot_window", 1.0f }, { "prep_window", 1.3f } };
            string upPkt = NetworkManager.EncodeUpgrades(1, upDict);
            Assert(upPkt != null && upPkt.StartsWith("UPGRADES|1|"), "Client encodes its tier + upgrades into an UPGRADES packet");
            Assert(NetworkManager.DecodeUpgrades(upPkt, out int decTier, out var decUp) && decTier == 1 && decUp.Count == 4
                && Mathf.Abs(decUp["move_speed"] - 1.2f) < 0.001f && Mathf.Abs(decUp["cook_speed"] - 1.5f) < 0.001f, "UPGRADES packet round-trips tier + values");
            bool upThrew = false;
            try
            {
                Assert(!NetworkManager.DecodeUpgrades(null, out _, out _) && !NetworkManager.DecodeUpgrades("garbage", out _, out _), "Garbage UPGRADES packet rejected");
                Assert(NetworkManager.DecodeUpgrades("UPGRADES|9|move_speed=99,cook_speed=nan,bad,x=1", out int clampTier, out var clampUp)
                    && clampTier == 3 && clampUp["move_speed"] <= 3.0f && !clampUp.ContainsKey("cook_speed") && !clampUp.ContainsKey("x"), "Hostile UPGRADES values clamped / dropped");
            }
            catch { upThrew = true; }
            Assert(!upThrew, "Malformed UPGRADES packets never crash the host");

            sp1.characterTier = 2;
            Assert(NetworkManager.ApplyUpgradesToChef(sp2, decTier, decUp), "Host applies partner's upgrades to partner's chef");
            Assert(sp1.characterTier == 2 && sp2.characterTier == 1, "Mixed tiers work (Linh T2 host, Jiwon T1 partner)");
            Assert(Mathf.Abs(sp2.GetEffectiveMoveSpeed() - PlayerController.BASE_MOVE_SPEED * 1.2f) < 0.01f, "Partner walk speed uses partner's own upgrades");
            nmSync.localPeerId = 1;
            if (ProgressionManager.Instance != null) ProgressionManager.Instance.UpdateLiveChefsTiers();
            Assert(sp2.characterTier == 1, "Host's own save never overwrites partner chef tier during LAN");

            var upCookA = new GameObject("UpCookA").AddComponent<CookingStation>();
            var upCookB = new GameObject("UpCookB").AddComponent<CookingStation>();
            sp2.transform.position = Vector3.zero; upCookA.transform.position = Vector3.zero; upCookB.transform.position = Vector3.zero;
            KitchenItem upItemA = new KitchenItem("tteok_portioned", "Tteok");
            KitchenItem upItemB = new KitchenItem("tteok_portioned", "Tteok");
            upCookA.InteractPrimary(ref upItemA, 2, "jiwon"); // partner (cook_speed 1.5)
            upCookB.PlaceIngredient(upItemB);                   // no chef
            upCookA.TickCooking(1.0f); upCookB.TickCooking(1.0f);
            Assert(upCookA.cookingElapsedSeconds > upCookB.cookingElapsedSeconds + 0.3f, "Cook speed follows the chef who placed the food");

            // Part-timer: host sends target station; both devices play the walk-in
            var wkPrep = new GameObject("WkPrep").AddComponent<PrepStation>(); wkPrep.stationId = "prep_east";
            kcSync.stations["prep_east"] = wkPrep;
            Assert(NetworkManager.EncodeWorker("worker_7", "prep_assistant", "prep_east") == "WORKER|worker_7|prep_assistant|prep_east", "Host encodes WORKER packet with target station");
            var wkVis = kcSync.SpawnWorkerVisual("worker_7", "prep_assistant", wkPrep, 7, 2);
            Assert(wkVis != null && wkVis.targetStation == wkPrep && wkVis.currentState == WorkerState.WALKING_IN && kcSync.activeWorkers.ContainsKey("worker_7"), "Client spawns part-timer walking to the host-chosen station");
            Assert(Mathf.Abs(wkVis.transform.localScale.y * (wkVis.GetComponent<SpriteRenderer>().sprite != null ? wkVis.GetComponent<SpriteRenderer>().sprite.bounds.size.y : KitchenController.PART_TIMER_HEIGHT / wkVis.transform.localScale.y) - KitchenController.PART_TIMER_HEIGHT) < 0.05f || wkVis.GetComponent<SpriteRenderer>().sprite == null, "Client part-timer uses readable 1.75 height");
            nmSync.localPeerId = 2;
            wkPrep.PlaceRawItem(new KitchenItem("daikon_carrot_raw", "Raw Daikon"));
            wkVis.currentState = WorkerState.WORKING;
            wkVis.TickWorker(2.0f);
            Assert(wkPrep.strikesCompleted == 0, "Client part-timer only animates (host owns chopping)");
            Assert(!NetworkManager.ApplyWorkerPacket("WORKER|w|prep_assistant|no_such_station", kcSync) && !NetworkManager.ApplyWorkerPacket("junk", kcSync), "Bad WORKER packets ignored");
            nmSync.localPeerId = 1;

            // 12. UI / HUD size option stays in a safe readable range
            Assert(Mathf.Abs(TwoLadles.UI.UIScaleSettings.Clamp(2.0f) - TwoLadles.UI.UIScaleSettings.MAX) < 0.001f
                && Mathf.Abs(TwoLadles.UI.UIScaleSettings.Clamp(0.1f) - TwoLadles.UI.UIScaleSettings.MIN) < 0.001f
                && Mathf.Abs(TwoLadles.UI.UIScaleSettings.Clamp(float.NaN) - 1.0f) < 0.001f, "UI scale clamps to safe range (NaN -> 100%)");
            Assert(Mathf.Abs(TwoLadles.UI.UIScaleSettings.ScaledReference(new Vector2(1280f, 720f), 1.25f).x - 1024f) < 0.5f, "Bigger UI scale shrinks the canvas reference resolution");

            // 13. LAN partner chef turns + wobbles like the local chef
            Assert(PlayerController.ComputeFlipX(0.5f, false) && !PlayerController.ComputeFlipX(-0.5f, true), "Partner chef faces right when moving right, left when moving left");
            Assert(PlayerController.ComputeFlipX(0.0f, true) && !PlayerController.ComputeFlipX(0.01f, false), "Partner chef keeps facing when moving straight up/down");
            Assert(Mathf.Abs(PlayerController.ComputeWalkSway(Mathf.PI * 0.5f) - 7.5f) < 0.01f && Mathf.Abs(PlayerController.ComputeWalkSway(0f)) < 0.01f, "Walk wobble sways up to 7.5 degrees");
            var remChef = new GameObject("RemoteWobbleChef").AddComponent<PlayerController>();
            var remSpr = new GameObject("RemoteWobbleSprite").AddComponent<SpriteRenderer>();
            remSpr.transform.SetParent(remChef.transform);
            remChef.characterSpriteRenderer = remSpr;
            remChef.isLocalPlayer = false;
            remChef.transform.position = Vector3.zero;
            remChef.UpdateRemoteWalkVisuals(0.016f);
            remChef.transform.position = new Vector3(0.2f, 0f, 0f);
            remChef.UpdateRemoteWalkVisuals(0.1f);
            float remTilt = Mathf.DeltaAngle(0f, remSpr.transform.localRotation.eulerAngles.z);
            Assert(remSpr.flipX && Mathf.Abs(remTilt) > 0.5f, "Partner chef moving right flips and wobbles from position updates");
            remChef.transform.position = new Vector3(-0.2f, 0f, 0f);
            remChef.UpdateRemoteWalkVisuals(0.016f);
            Assert(!remSpr.flipX, "Partner chef moving left faces left");
            for (int i = 0; i < 60; i++) remChef.UpdateRemoteWalkVisuals(0.05f);
            Assert(Mathf.Abs(Mathf.DeltaAngle(0f, remSpr.transform.localRotation.eulerAngles.z)) < 0.5f, "Partner chef stands upright again when it stops");
            UnityEngine.Object.DestroyImmediate(remChef.gameObject);

            // 14. North double door is not blocked by a table
            foreach (float dz in KitchenController.DIVIDER_DECOR_Z)
                Assert(dz < 2.0f, $"Divider decor table at z={dz} stays clear of the door area");
            Assert(KitchenController.SERVING_STATION_POS.z + 1.1f <= 4.3f, "Serving Counter sits south of the double door, leaving the doorway clear");

            nmSync.isLanActive = false;
            nmSync.localPeerId = 1;
            if (ShiftManager.Instance != null) ShiftManager.Instance.isAutoplayQAMode = prevQA;
            KitchenController.Instance = null;
            foreach (var o in new GameObject[] { kcSyncObj, sp1.gameObject, sp2.gameObject, hCounter.gameObject, hCook.gameObject, hPass.gameObject, hAsm.gameObject, hPrep.gameObject,
                cCounter.gameObject, cCook.gameObject, cPass.gameObject, cAsm.gameObject, cPrep.gameObject, cc1.gameObject, cc2.gameObject, lanCounter.gameObject, upCookA.gameObject, upCookB.gameObject, wkPrep.gameObject, wkVis != null ? wkVis.gameObject : null })
            {
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            }
        }
        private void Test20_PauseAndLanDayCycle()
        {
            Debug.Log("[Test 20] Pause Menu & LAN Day Report / Shop / Double-Ready");

            // 1. Day report packet round-trip
            string pkt = NetworkManager.EncodeDayReport(3, true, 62.5f, 4, 4, 11, 870, 900);
            var rep = NetworkManager.DecodeDayReport(pkt);
            Assert(rep != null && (int)rep["stage_id"] == 3 && (bool)rep["victory"] && Mathf.Abs((float)rep["reputation"] - 62.5f) < 0.01f
                && (int)rep["waves_completed"] == 4 && (int)rep["total_waves"] == 4 && (int)rep["ordersServed"] == 11
                && (int)rep["total_score"] == 870 && (int)rep["high_score"] == 900, "Day report packet round-trips host stats to the guest");

            // 2. Malformed packets rejected
            Assert(NetworkManager.DecodeDayReport("junk") == null && NetworkManager.DecodeDayReport("DAY_REPORT|x") == null, "Malformed day report packets are rejected");

            // 3. Next day only starts when both chefs are ready
            Assert(NetworkManager.BothDayReady(true, true) && !NetworkManager.BothDayReady(true, false)
                && !NetworkManager.BothDayReady(false, true) && !NetworkManager.BothDayReady(false, false), "Next day needs both chefs ready");

            // 4. Next stage rules
            Assert(ShiftManager.ComputeNextStage(1, true) == 2 && ShiftManager.ComputeNextStage(5, true) == 5
                && ShiftManager.ComputeNextStage(2, false) == 2 && ShiftManager.ComputeNextStage(0, false) == 1, "Next day stage: advance on win, replay on loss, tutorial leads to Day 1");

            // 5. Guest cannot end its own day
            var sm = ShiftManager.Instance;
            bool prevQA = sm.isAutoplayQAMode;
            sm.isAutoplayQAMode = true;
            ProgressionManager.Instance.ResetProgressionForTesting();
            sm.StartShift(1, GameMode.CAMPAIGN);
            sm.isNetworkClient = true;
            sm.EndShift("guest tried");
            Assert(sm.shiftActive, "LAN guest cannot end the day locally (host decides)");

            // 6. Host report ends guest day and pays gold once
            int goldBefore = ProgressionManager.Instance.gold;
            var hostRep = NetworkManager.DecodeDayReport(NetworkManager.EncodeDayReport(1, true, 75f, 3, 3, 8, 600, 600));
            sm.ApplyDayReport(hostRep);
            int goldAfter = ProgressionManager.Instance.gold;
            Assert(!sm.shiftActive && sm.levelVictory && sm.totalScore == 600, "Host day report ends the guest's day with host stats");
            Assert(goldAfter > goldBefore, "Guest earns gold from the host day report so it can shop");
            sm.ApplyDayReport(hostRep);
            Assert(ProgressionManager.Instance.gold == goldAfter, "Repeated day report does not pay gold twice");

            // 7. Pause freezes time in solo only
            Assert(KitchenHUD.PauseTimeScale(false, false) == 1f, "Unpaused game runs at normal speed");
            Assert(KitchenHUD.PauseTimeScale(true, true) == 1f, "LAN pause keeps the shared kitchen running");
            Assert(KitchenHUD.PauseTimeScale(true, false) == 0f, "Solo pause freezes the game");

            // Storage balance: fridge and shelf wheels hold an equal number of distinct items
            var fridgeIds = QuickSelectWheelUI.GetCatalogIds(true);
            var shelfIds = QuickSelectWheelUI.GetCatalogIds(false);
            Assert(fridgeIds.Length == shelfIds.Length, "Fridge and shelf hold the same number of ingredients");
            Assert(Array.TrueForAll(fridgeIds, id => Array.IndexOf(shelfIds, id) < 0), "No ingredient is duplicated across fridge and shelf");
            var allIds = new List<string>(fridgeIds); allIds.AddRange(shelfIds);
            Assert(Array.TrueForAll(new[] { "unprocessed_raw_meat", "radish_carrot", "egg_raw", "tteok_portioned", "gochujang", "flour", "rice_portioned", "baguette" }, id => allIds.Contains(id)), "Every base ingredient is still available from storage");

            // Per-station storage: both fridges and both shelves hold exactly 2 distinct items each
            var stationIds = new[] { "fridge_west", "fridge_east", "shelf_west", "shelf_east" };
            var seen = new List<string>();
            bool allTwo = true;
            foreach (var st in stationIds)
            {
                var ids = QuickSelectWheelUI.GetStationCatalogIds(st);
                if (ids == null || ids.Length != 2) allTwo = false;
                else seen.AddRange(ids);
            }
            Assert(allTwo, "Each fridge and shelf holds exactly 2 ingredients");
            Assert(seen.Count == 8 && new HashSet<string>(seen).Count == 8, "No ingredient is duplicated across the 4 storage stations");
            Assert(Array.TrueForAll(new[] { "unprocessed_raw_meat", "radish_carrot", "egg_raw", "tteok_portioned", "gochujang", "flour", "rice_portioned", "baguette" }, id => seen.Contains(id)), "All 8 base ingredients are reachable across the 4 storage stations");

            // Wheel auto-closes when the chef walks away from the storage station
            Assert(!QuickSelectWheelUI.ShouldAutoClose(2.0f), "Wheel stays open while the chef stands at the station");
            Assert(QuickSelectWheelUI.ShouldAutoClose(4.0f), "Wheel closes when the chef walks away from the station");

            // Double door swings open for 1 second whenever a dish is served
            var doorKcObj = new GameObject("KitchenControllerDoorTest");
            var doorKc = doorKcObj.AddComponent<KitchenController>();
            doorKc.autoBuildKitchenOnStart = false;
            var prevKc = KitchenController.Instance;
            KitchenController.Instance = doorKc;
            sm.activeOrders.Clear();
            sm.activeOrders.Add(new OrderTicket("ord_door_test", "banh_mi_thit", "plate", 60.0f));
            var doorServe = sm.DeliverDish(new KitchenItem("banh_mi_thit", "Banh Mi", "plate"), 1);
            Assert(Mathf.Approximately(KitchenController.DOOR_OPEN_SECONDS, 1.0f), "Door stays open for exactly 1 second");
            Assert(doorServe.success && doorKc.DoorOpen, "Serving a dish swings the double door open");
            doorKc.TickDoor(0.5f);
            Assert(doorKc.DoorOpen, "Door is still open half a second after serving");
            doorKc.TickDoor(0.6f);
            Assert(!doorKc.DoorOpen, "Door closes again after 1 second");
            KitchenController.Instance = prevKc;
            UnityEngine.Object.DestroyImmediate(doorKcObj);

            // Leaving a 2-bot QA run fully switches the bots off for the next game
            var abortKcObj = new GameObject("KitchenControllerQAAbortTest");
            var abortKc = abortKcObj.AddComponent<KitchenController>();
            abortKc.autoBuildKitchenOnStart = false;
            var prevKc2 = KitchenController.Instance;
            KitchenController.Instance = abortKc;
            var abLinhGo = new GameObject("AbortChef_Linh");
            var abLinh = abLinhGo.AddComponent<PlayerController>();
            abLinh.peerId = 1; abLinh.characterId = "linh";
            abortKc.playersByPeer[1] = abLinh;
            var abJiwonGo = new GameObject("AbortChef_Jiwon");
            var abJiwon = abJiwonGo.AddComponent<PlayerController>();
            abJiwon.peerId = 2; abJiwon.characterId = "jiwon";
            abortKc.playersByPeer[2] = abJiwon;
            var abQaGo = new GameObject("AbortQAManager");
            var abQa = abQaGo.AddComponent<TwoBotQAManager>();
            abQa.ConfigureTwoBots();
            abQa.isQARunning = true;
            sm.isAutoplayQAMode = true;
            abQa.AbortQARun();
            bool noBotActive = Array.TrueForAll(new[] { abLinhGo, abJiwonGo }, g => { var b = g.GetComponent<BotController>(); return b == null || !b.botActive; });
            Assert(!abQa.isQARunning && !sm.isAutoplayQAMode && Time.timeScale == 1f, "Aborting QA clears QA mode and restores normal speed");
            Assert(noBotActive, "Aborting QA switches off both bot controllers");
            abortKc.ConfigureSoloAndAIMode(false, 1, BotDifficulty.Standard);
            noBotActive = Array.TrueForAll(new[] { abLinhGo, abJiwonGo }, g => { var b = g.GetComponent<BotController>(); return b == null || !b.botActive; });
            Assert(noBotActive && abLinh.isLocalPlayer, "Next normal game after QA has no bots playing for the user");
            KitchenController.Instance = prevKc2;
            UnityEngine.Object.DestroyImmediate(abQaGo);
            UnityEngine.Object.DestroyImmediate(abLinhGo);
            UnityEngine.Object.DestroyImmediate(abJiwonGo);
            UnityEngine.Object.DestroyImmediate(abortKcObj);

            // Pause button is a plain word, inset from the rounded phone corner
            Assert(KitchenHUD.PAUSE_LABEL == "PAUSE", "Pause button shows the word PAUSE");
            Assert(KitchenHUD.PAUSE_BUTTON_X <= -60f, "Pause button sits away from the rounded screen corner");

            // Mobile camera zooms in closer than desktop
            Assert(KitchenController.CameraOrthoSize(false) == 11.2f, "Desktop camera zoom unchanged");
            Assert(KitchenController.CameraOrthoSize(true) < KitchenController.CameraOrthoSize(false), "Mobile camera is zoomed in more");

            // Yard decor: enough trees and bushes, all on grass outside kitchen and dining room
            var yard = KitchenController.YARD_DECOR;
            Assert(yard != null && yard.Length >= 12, "Yard has at least 12 decorations");
            bool allOnGrass = yard != null && Array.TrueForAll(yard, d =>
            {
                var p = d.pos;
                bool inBuilding = Mathf.Abs(p.x) <= 11.5f && p.z >= -6.5f && p.z <= 17f;
                bool inGrid = Mathf.Abs(p.x) <= 26f && p.z >= -14f && p.z <= 16f;
                return !inBuilding && inGrid;
            });
            Assert(allOnGrass, "Every yard decoration sits on grass outside the building");

            // Forgiving mobile joystick: bigger touch zone, finger outside the pad still steers at full tilt
            Assert(KitchenHUD.JOYSTICK_TOUCH_ZONE >= 240f, "Joystick touch zone is much bigger than the 128px pad");
            Vector2 farOut = KitchenHUD.JoystickVector(new Vector2(400f, 0f));
            Assert(Mathf.Abs(farOut.x - 1f) < 0.001f && Mathf.Abs(farOut.y) < 0.001f, "Finger far outside the pad still steers full right");
            Vector2 halfway = KitchenHUD.JoystickVector(new Vector2(0f, -KitchenHUD.JOYSTICK_MAX_RADIUS * 0.5f));
            Assert(Mathf.Abs(halfway.y + 0.5f) < 0.001f, "Half drag gives half speed");
            Assert(KitchenHUD.JoystickVector(new Vector2(2f, 1f)) == Vector2.zero, "Tiny finger wobble near centre is ignored");

            // WASD / arrows / Space must move the chef, never steer menus or re-press the last clicked button
            var esGo = new GameObject("TestEventSystem");
            var es = esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            Assert(es.sendNavigationEvents, "Fresh EventSystem starts with keyboard menu navigation on");
            AutoBootstrap.DisableKeyboardMenuNavigation();
            Assert(!es.sendNavigationEvents, "Keyboard menu navigation is off so WASD only moves the chef");
            UnityEngine.Object.DestroyImmediate(esGo);

            sm.isNetworkClient = false;
            sm.shiftActive = false;
            sm.isAutoplayQAMode = prevQA;
        }
    }
}
