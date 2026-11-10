# AGENTS.md — Two Ladles Engineering Guidelines & Constraints

This file is the permanent engineering contract for all AI agents and human developers working on **Two Ladles**. The repository is the single source of truth.

> [!IMPORTANT]
> **Active Engine Notice:** Development on the Godot version has officially **STOPPED**. All active engineering, gameplay features, physics, sprites, tests, builds, and tournament submissions are **100% focused on the Unity 2022.3.62f3 C# version** located at `unity/TwoLadles/Two Ladles/`. Do not write new gameplay code or features for Godot.

## 1. Engine & Competition Mandatory Constraints

- **Official Competition Engine:** Unity `2022.3.62f3` (C# with static typing) located at `unity/TwoLadles/Two Ladles/`.
- **Target Platforms (Korea Game Week 2026 Submission):**
  - **Windows Desktop Standalone (`TwoLadles.exe`):** For desktop testing and laptop co-op host.
  - **Android Standalone APK (`TwoLadles.apk`):** For mobile touchscreen co-op client.
- **Godot Status (Archived Testbed):** Godot `4.7.2` was used strictly as a rapid prototyping sandbox. Development on the Godot version has completely stopped and it is permanently archived on the `godot` branch. All active development, physics, sprites, gameplay, builds, and tournament submissions are strictly Unity.
- **Zero Third-Party Networking Bloat:** Raw UDP sockets via standard .NET `UdpClient`:
  - Port `24567`: Gameplay UDP state synchronization (30Hz player movement transforms).
  - Port `24568`: LAN room discovery broadcast beacon (1.2s broadcast interval, 5.0s stale room pruning).
- **Co-op First Design & Solo AI Mode:** Simultaneous 2-player co-op (Linh & Jiwon) is the core gameplay experience. A Solo Mode with a deterministic AI partner (`BotController`, with Assistive, Standard, and Challenge difficulty profiles) is fully supported for single-player testing and play.

## 2. Core Gameplay & Architecture Rules

1. **Authoritative Host Model (`Scripts/Networking/NetworkManager.cs` & `Scripts/Core/ShiftManager.cs`):**
   - Host (Peer ID `1`) is authoritative over customer orders, order timers, cooking transitions, Pass Counter handoffs, Kitchen Flow, Rush Rhythm, plate supply, wave progression, and scoring.
   - Host broadcasts network state packets at 4Hz (`ShiftManager.EncodeNetworkState` / `DecodeNetworkState`) synchronizing active orders, remaining patience, score, reputation, kitchen flow, and wave progress to client.
   - Client sends movement vectors (30Hz) and station interaction requests.
   - Host validates interaction range (`StationBase.MAX_INTERACT_DISTANCE = 2.6f`) before mutating state.
2. **Cross-Prep Interdependence (`Scripts/Core/GameData.cs` & `Scripts/Kitchen/KitchenController.cs`):**
   - Asymmetric West/East kitchen layout:
     - **Linh's West Zone:** Prepares proteins/starches (`pork_portioned`, `bulgogi_portioned`, `tteok_portioned`, `rice_portioned`) for Jiwon's appliances, cooks dedicated Phở stockpot broth & noodle blancher, and assembles Vietnamese dishes.
     - **Jiwon's East Zone:** Prepares vegetables/slaw/fish cakes (`pickled_veg`, `kimchi_slaw`, `eomuk_prepared`), operates Grill/Sear pan & Gochujang Simmer Wok, and provides clean vessels. Plates and bowls are unlimited; the dirty dish washing chore has been removed to keep gameplay flow fast and rhythmic.
3. **Authentic Culinary Sequences & 5-Day Progression:**
   - **5-Day Campaign Menu (`Scripts/Story/StoryScript.cs` & `Scripts/Core/GameData.cs`):**
     - **Day 1: Traditional Korean *Tteokbokki*** — Cooked tteok portion + simmered gochujang wok sauce (served in **Bowl**; strictly rejects flat plates).
     - **Day 2: Traditional Saigon *Bột Chiên*** — Pan-fried rice flour cake cubes with egg (served on **Plate**).
     - **Day 3: Traditional Korean BBQ on Rice (*Cơm Bò Nướng Kiểu Hàn*)** — Grilled sliced beef / bulgogi served over steamed rice (served on **Plate**; grants `+2.0s` extra customer patience).
     - **Day 4: Traditional Vietnamese Broken Rice (*Cơm Tấm*)** — Broken rice with grilled BBQ beef, egg, and seasoning (served on **Plate**; grants `+2.0s` extra customer patience).
     - **Day 5: *"Bulgogi Bánh Mì"*** — Vietnam–Korea Fusion Specialty (Baguette + seared bulgogi beef + pickled daikon/carrot/kimchi slaw on **Plate**).
   - **Authentic Preparation Invariants:**
     - Traditional Vietnamese *Phở Bò* requires a **Bowl**, blanched rice noodles, sliced beef, with hot stockpot broth poured strictly last.
     - Traditional Vietnamese *Bánh Mì Thịt* requires a **Plate**, baguette, grilled pork, and pickled vegetables.
4. **Anti-Exploit Pass Counter & Rush Rhythm:**
   - Pass Counter handoffs grant `+15` Kitchen Flow only when retrieved by the **other** chef within `4000 ms` for an active order that has not yet claimed a bonus.
   - Reaching `100` Flow activates **Rush Rhythm** (`1.5x` score multiplier) for `15.0` seconds.
5. **Physical Collisions & Character Phasing Prevention:**
   - All station counters, appliances, crates, and perimeter walls have physical `BoxCollider` static bodies.
   - Chefs move via `CharacterController` with collision boundaries (`radius = 0.45`, `height = 1.3`), preventing characters from ever phasing through counters, appliances, or walls.
6. **Carried Item Visuals & Throw Mechanics:**
   - Carried ingredients and dishes hover over the chef's head with subtle side-to-side tilt (`±10°`) and floating vertical bobbing (`0.08m`).
   - Throwing (`Q` or right-click) launches the carried item toward the partner chef (who catches it with empty hands), empty holding counter, pass counter slot, or trash bin.
7. **Jiwon Tier-3 Delegation & Part-Timer Crew:**
   - Part-Timer worker sprite is rendered at `KitchenController.PART_TIMER_HEIGHT = 1.75f` (readable scale) and automatically chops ingredients at the East prep counter.
   - A single unified Part-Timer button (`HirePrepBtn` / `[H]` key) hires every free part-timer slot for the tier at once (`RequestHireAvailableCrew`; no separate dishwasher button as washing was removed).

## 3. Testing & CI Discipline

- **Automated C# Test Suite (`Assets/Tests/TwoLadlesTestSuite.cs`):**
  19 comprehensive test suites (233 assertions) covering:
  1. Recipes & Cross-Prep Architecture (`Test1_RecipesAndCrossPrepArchitecture`)
  2. Prep Station Rhythm Cadence (`Test2_PrepStationRhythmCadence`)
  3. Cooking State Machine & Phở Separation (`Test3_CookingStationStateMachineAndPhoSeparation`)
  4. Assembly Station Rules & Vessel Rejections (`Test4_AssemblyStationRules`)
  5. Pass Counter Handoff & Anti-Exploit Window (`Test5_PassCounterHandoffAndAntiExploit`)
  6. Kitchen Flow & Rush Rhythm (`Test6_KitchenFlowAndRushRhythm`)
  7. No Washing Chore / Unlimited Plate Supply (`Test7_NoWashingPlateSupply`)
  8. Lobby Exclusivity & Ready Resets (`Test8_LobbyExclusivityAndReadyReset`)
  9. Permanent Career Progression & Kitchen Upgrades (`Test9_ProgressionSystem`)
  10. Linh Execution Specialist Tiers — Third Arm Visuals & Speed (`Test10_LinhExecutionSpecialistTiers`)
  11. Jiwon Delegation Specialist Tiers — Part-Timer Crew Hiring (`Test11_JiwonDelegationPartTimeCrew`)
  12. 5-Level Campaign Progression & Unlock Gates (`Test12_CampaignLevelsAndProgressionUnlocks`)
  13. Endless Rush Survival Mode & High Scores (`Test13_EndlessRushMode`)
  14. Holding Counters, Hover Tilt Animation & Throw Mechanics (`Test14_HoldingCounterAndThrowMechanics`)
  15. Wave Progression, Reputation, Gold Economy, Shop & Level Replay (`Test15_WaveProgressionReputationAndGoldEconomy`)
  16. Solo Mode & Deterministic AI Partner (`Test16_SoloModeAndAIPartner`)
  17. Two-Bot QA Autoplay Manager & Reporting (`Test17_TwoBotQAManagerAndReporting`)
  18. Story Phone Script & Day-Night Lighting (`Test18_StoryPhoneAndDayNightCycle`)
  19. LAN Sync, Recipe Timing, Part-Timer Size & Bot Safety (`Test19_LanSyncTimingAndBotSafety`)
- **Test Immutability:** Never weaken, skip, or delete assertions in `TwoLadlesTestSuite.cs` to make a build pass.
