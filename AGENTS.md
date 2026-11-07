# AGENTS.md — Two Ladles Engineering Guidelines & Constraints

This file is the permanent engineering contract for all AI agents and human developers working on **Two Ladles**. The repository is the single source of truth.

## 1. Engine & Competition Mandatory Constraints

- **Official Competition Engine:** Unity `2022.3.62f3` (C# with static typing) located at `unity/TwoLadles/`.
- **Target Platforms (Korea Game Week 2026 Submission):**
  - **Windows Desktop Standalone (`TwoLadles.exe`):** For desktop testing and laptop co-op host.
  - **Android Standalone APK (`TwoLadles.apk`):** For mobile touchscreen co-op client.
- **Godot Status (Archived Testbed):** Godot `4.7.2` was used strictly as a rapid prototyping sandbox. It is permanently archived on the `godot` branch. All active development, physics, sprites, gameplay, builds, and tournament submissions are strictly Unity.
- **Zero Third-Party Networking Bloat:** ENet/UDP network communication over port `24567`.
- **No Single-Player AI Replacement:** Mandatory simultaneous 2-player co-op (Linh & Jiwon).

## 2. Core Gameplay & Architecture Rules

1. **Authoritative Host Model (`Scripts/Networking/NetworkManager.cs` & `Scripts/Core/ShiftManager.cs`):**
   - Host (Peer ID `1`) is authoritative over customer orders, timers, cooking transitions, Pass Counter handoffs, Kitchen Flow, Rush Rhythm, plate stacks, and scoring.
   - Client sends movement vectors and station interaction requests.
   - Host validates interaction range (`StationBase.MAX_INTERACT_DISTANCE = 2.6f`) before mutating state.
2. **Cross-Prep Interdependence (`Scripts/Core/GameData.cs` & `Scripts/Kitchen/KitchenController.cs`):**
   - Asymmetric West/East kitchen layout:
     - **Linh's West Zone:** Prepares proteins/starches (`pork_portioned`, `bulgogi_portioned`, `tteok_portioned`, `rice_portioned`) for Jiwon's appliances, cooks dedicated Phở stockpot broth & noodle blancher, and assembles Vietnamese dishes.
     - **Jiwon's East Zone:** Prepares vegetables/slaw/fish cakes (`pickled_veg`, `kimchi_slaw`, `eomuk_prepared`), operates Grill/Sear pan & Gochujang Simmer Wok, washes dirty dishes, and provides clean vessels.
3. **Authentic Culinary Sequences (5-Level Progression):**
   - Level 1: Traditional Vietnamese *Bánh Mì Thịt* (Baguette + grilled pork + pickled veg + cilantro/sauce on plate).
   - Level 2: Traditional Vietnamese *Phở Bò* (Bowl + blanched rice noodles + sliced beef + hot stockpot broth poured last).
   - Level 3: Traditional Korean *Tteokbokki* (Bowl + simmered rice cakes + prepared fish cakes + gochujang sauce). Rejects flat plates!
   - Level 4: Traditional Korean *Kimchi Bokkeumbap* (Plate + stir-fried rice + aged kimchi + grilled protein).
   - Level 5: *"Bulgogi Bánh Mì"* (Vietnam–Korea Fusion Specialty — Baguette + grilled bulgogi beef + kimchi slaw + gochujang glaze).
4. **Anti-Exploit Pass Counter & Rush Rhythm:**
   - Pass Counter handoffs grant `+15` Kitchen Flow only when retrieved by the **other** chef within `4000 ms` for an active order that has not yet claimed a bonus.
   - Reaching `100` Flow activates **Rush Rhythm** (`1.5x` score multiplier) for `15.0` seconds.
5. **Physical Collisions & Character Phasing Prevention:**
   - All station counters, appliances, crates, and perimeter walls have physical `BoxCollider` static bodies.
   - Chefs move via `CharacterController` with collision boundaries (`radius = 0.45`, `height = 1.3`), preventing characters from ever phasing through counters, appliances, or walls.
6. **Carried Item Visuals & Throw Mechanics:**
   - Carried ingredients and dishes hover over the chef's head with subtle side-to-side tilt (`±10°`) and floating vertical bobbing (`0.08m`).
   - Throwing (`Q` or right-click) launches the carried item toward the partner chef (who catches it with empty hands), empty holding counter, pass counter slot, or trash bin.

## 3. Testing & CI Discipline

- **Automated C# Test Suite (`Assets/Tests/TwoLadlesTestSuite.cs`):**
  14 test suites covering:
  1. Recipes & Cross-Prep Architecture
  2. Prep Station Rhythm Cadence
  3. Cooking State Machine & Phở Separation
  4. Assembly Station Rules & Vessel Rejections
  5. Pass Counter Handoff & Anti-Exploit Window
  6. Kitchen Flow & Rush Rhythm
  7. Washing Station Plate Lifecycle
  8. Lobby Exclusivity & Ready Resets
  9. Permanent Career Progression & Kitchen Upgrades
  10. Linh Execution Specialist Tiers (Third Arm Visuals & Speed)
  11. Jiwon Delegation Specialist Tiers (Part-Timer Crew Hiring)
  12. 5-Level Campaign Progression & Unlock Gates
  13. Endless Rush Survival Mode & High Scores
  14. Holding Counters, Hover Tilt Animation & Throw Mechanics
- **Test Immutability:** Never weaken, skip, or delete assertions in `TwoLadlesTestSuite.cs` to make a build pass.
