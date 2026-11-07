# Changelog

All notable changes to **Two Ladles** are documented in this file.

## [0.4.0] - 2026-11-07 (Official Engine Transition: 100% Unity Focus)

### Changed
- **Engine Transition:** Officially stopped development on the Godot prototype and transitioned 100% of project engineering, physics, gameplay, and Korea Game Week 2026 tournament deliverables to **Unity 2022.3.62f3 C#** (`unity/TwoLadles/`).
- **Archival:** Godot `4.7.2` codebase is permanently archived on the `godot` branch.
- **Documentation:** Updated `AGENTS.md`, `README.md`, `ARCHITECTURE.md`, `DEVELOPMENT_ROADMAP.md`, `TESTING.md`, `ANDROID_BUILD.md`, and `NETWORKING.md` to reflect Unity as the sole active engine.
- **Verification Runner:** Official active test suite established as `unity/TwoLadles/Assets/Tests/TwoLadlesTestSuite.cs` (14 suites, 692 assertions).

## [0.3.0] - 2026-11-06 (Milestone 3 Character Signature Abilities, Campaign & Endless Mode)

### Added
- **Linh Execution Specialist Progression (Tiers 1–3):**
  - Tier 1 (Ordinary Chef): Standard 1.0x task cadence, no mechanical arm.
  - Tier 2 (Workshop Engineer): Unlocks after Level 3; 0.92x prep interval (~8% faster) with visible rustic brass/steel mechanical third arm.
  - Tier 3 (Precision Engineer): Unlocks after Level 5; 0.85x prep interval (~15% faster; 0.38s cadence) with sleek glowing precision white-and-blue robotic arm.
- **Jiwon Multitasking & Delegation Specialist ("Call in the Crew"):**
  - Host-authoritative hiring system with coin deduction (20 coins), contract duration (20s), and role agency cooldown (45s).
  - Tier 1: 0 slots; Tier 2: 1 slot; Tier 3: 2 simultaneous slots.
  - Deterministic 4-state `PartTimeWorker` agent (`WALKING_IN -> WORKING -> WALKING_OUT -> EXPIRED`) performing automated chops (`prep_assistant`) and scrubs (`dishwasher`).
  - Added "Call in the Crew" HUD bar with real-time status and cooldown badges.
- **5 Handcrafted Campaign Levels:**
  - Level 1: *First Customer* (Rural Vietnam food stand reopening; 2 orders max, 16s pacing).
  - Level 2: *Lunch Rush* (Village Market; introduces washing dirty plates under multi-order pressure).
  - Level 3: *Night Market* (Introduces Korean *Tteokbokki*; unlocks Character Tier 2!).
  - Level 4: *Fusion Special* (Street Food Market; introduces *Bulgogi Bánh Mì* fusion recipe).
  - Level 5: *Grand Opening* (Modern Fusion Restaurant; all 4 recipes; unlocks Character Tier 3!).
- **Endless Rush Mode:**
  - Unlimited continuous shift with dynamic difficulty scaling (spawn interval accelerates from 14s to 6.5s; concurrency scales from 2 to 4).
  - 3 lost tickets failure condition with persistent personal best tracking (score, orders, duration).
- **Persistent Progression & Kitchen Upgrade Shop (`ProgressionManager`):**
  - Persistent JSON save/load with dual-path resilience (`user://` and sandboxed workspace fallback).
  - Stage records (stars, best scores, unlock requirements).
  - Upgrade catalog with purchase transactions (e.g. Sharp Santoku Knives, Double-Walled Stockpot).
- **Automated Verification:**
  - Expanded test suite to **114 automated assertions across 13 suites** (`tests/test_suite_node.gd`), all passing with 100% success.
  - Re-exported Windows Desktop debug binary (`build/windows/two-ladles.exe`).

## [0.2.0] - 2026-11-06 (Milestones 0, 1 & 2 Vertical Slice)

### Added
- **Milestone 0 (Foundation & CI/CD):**
  - Configured Godot `4.7.2-stable` project (`project.godot`), `mobile` renderer, landscape `1280x720` layout, and export presets (`export_presets.cfg`) for Windows, Linux, and Android (`INTERNET` and Wi-Fi permissions enabled).
  - Created `.github/workflows/ci.yml` (automated headless import, 62-assertion test suite, and Windows/Linux build artifacts on push/PR) and `.github/workflows/android-build.yml` (manual + `v*` tag cloud APK build).
  - Added full documentation suite (`README.md`, `AGENTS.md`, `GLOSSARY.md`, and `docs/*.md`).
- **Milestone 1 (LAN Multiplayer & Pre-Shift Lobby):**
  - Implemented authoritative `ENetMultiplayerPeer` host/client networking on UDP port `24567` (`scripts/autoload/network_manager.gd`).
  - Added pre-shift lobby supporting character exclusivity (`Linh` vs `Jiwon`), 4 selectable specializations (`PREP MASTER`, `FLAME KEEPER`, `MARKET RUNNER`, `FUSION ARTIST`), automatic readiness reset on loadout change, and safe disconnect handling.
  - Built 2.5D orthographic kitchen scene (`scripts/kitchen/kitchen_controller.gd`) with simultaneous dual-chef movement, desktop keyboard controls (`WASD`/Arrows + `E`/`Space`/`F`), and Android touch D-pad + context buttons.
- **Milestone 2 (Complete Gameplay Loop & Cross-Prep Interdependence):**
  - Implemented 5 distinct station mechanics: discrete 4-strike rhythm `PrepStation`, 5-state timed `CookingStation` (`RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT`), vessel-validated `AssemblyStation`, authoritative `ServingStation`, and 4-stroke `WashingStation` with finite 4-plate pool.
  - Implemented 4 data-driven starter recipes (*Phở Bò*, *Bánh Mì Thịt*, *Tteokbokki*, *Bulgogi Bánh Mì*) with asymmetric West/East **Cross-Prep Interdependence** between Linh and Jiwon across the shared `PassCounter`.
  - Implemented anti-exploit Pass Counter handoff detection (`+15` Kitchen Flow within 4s, max once per order) and **Rush Rhythm** (`100` Flow resets to `0`, activates `15s` `1.5x` base score multiplier with paused Flow generation).
  - Added 62-assertion headless automated test suite (`tests/test_suite_node.gd`) and 2-instance localhost UDP smoke test (`--lan-smoke-host` / `--lan-smoke-client`).
