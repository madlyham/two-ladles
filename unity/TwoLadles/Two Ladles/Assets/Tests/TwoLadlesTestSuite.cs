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
            ProgressionManager.Instance.coins = 100;
            var unifiedHire = kc.RequestHireAvailableCrew(2);
            Assert(unifiedHire.ok, "Jiwon Tier 3 unified single-button hire triggers crew deployment");
            Assert(unifiedHire.hiredCount == 2, "Unified single-button hire deploys both available part-timers (2 workers)");
            Assert(kc.activeWorkers.Count == 2, "Kitchen has 2 active workers after unified single-button hire");
            Assert(ProgressionManager.Instance.coins == 60, "40 coins deducted for hiring both part-timers at Tier 3 (Balance: 60)");

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

            // Lighting changes in one stage per wave
            Assert(Visuals.DayNightCycleManager.StageForWave(1, 3) == 0, "3 waves: wave 1 = day");
            Assert(Visuals.DayNightCycleManager.StageForWave(2, 3) == 1, "3 waves: wave 2 = red noon");
            Assert(Visuals.DayNightCycleManager.StageForWave(3, 3) == 2, "3 waves: wave 3 = night");
            Assert(Visuals.DayNightCycleManager.StageForWave(0, 3) == -1, "No lighting stage before first wave");
            Assert(Visuals.DayNightCycleManager.StageForWave(2, 2) == 2, "2 waves: last wave reaches night");
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
        }
    }
}
