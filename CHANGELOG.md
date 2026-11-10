# Changelog

All notable changes to **Two Ladles** are documented in this file.

## 2026-11-10 — Lobby Revamp & Docs Refresh

### Added
- **Lobby revamp for non-technical players** (`UI/LobbyUI.cs`): main menu (Host / Join / Tutorial / Upgrades / Settings), Host popup asking chef name + kitchen name (remembered via PlayerPrefs), Join popup listing LAN-discovered room slots plus a direct-IP box, Room panel with Day 1–5 / Endless selection and Ready / Start / Leave, Settings with language (EN / VI / KO), solo vs. LAN co-op, chef select. A faint (20% opacity) 2-Bot QA run button sits in the Settings popup (hidden on mobile).
- Purchased upgrades and chef tiers are cached and restored from the save file.
- **LAN order sync** (`ORDERS` packet): the host streams orders, patience timers, score, reputation, Kitchen Flow and Rush Rhythm to the client every 0.25 s; the client no longer spawns or expires orders on its own.
- **Recipe timing variety:** Day 3 Korean BBQ on Rice and Day 4 Broken Rice orders get +2 s patience (`ShiftManager.GetRecipePatienceBonus`).
- Test suite 19: LAN Order Sync, Recipe Timing & Bot Safety.

### Changed
- On-screen mobile controls now appear only on phones/tablets (`Application.isMobilePlatform`); on desktop they can be toggled with `F3`.
- Web mirror `web/downloads/TwoLadles-Windows-x64.zip` refreshed.
- Documentation updated to match the current Unity code (project path `unity/TwoLadles/Two Ladles/`, 19 test suites, raw-UDP networking, 5-day menu).
- Part-timer crew are bigger (height 1.75) so they are easier to see.
- Holding counters reject an item when they already hold the same item.
- **No washing chore:** plates and bowls are unlimited (taken from the quick-select wheel); serving never creates dirty plates. Test suite 7 is now "No Washing Chore" and the docs no longer describe washing.
- Day 5 Vietnamese story dialogue uses friendly `tớ / cậu` instead of `anh / em` (warm and light-hearted, not romantic). Day 5's first Korean line fixed (it contained Vietnamese).

### Fixed
- **Bot stuck fixes** (`AI/BotController.cs`): bots place or combine onto any free/matching counter instead of always one fixed table; Jiwon stops making extra beef once enough is in the pipeline; Linh's Day 3/4 plan reads the counters instead of a fixed script; rescue-pan flour is parked on a table; bots hold a new plan for 4 s after an unstick; idle bots park items while waiting for an order.
- Lobby **LEAVE** button called a nonexistent `NetworkManager.Disconnect`; it now calls `NetworkManager.Instance.ReturnToMenu("Partner left the room.")`, which sends a `DISCONNECT` packet and closes the connection.
- Recipe-book pause no longer blocks headless (batch-mode) tests.

## 2026-11-09 — 5-Day Menu, Localization, Solo AI & Bot QA

### Added
- **5-day menu revamp:** Day 1 Tteokbokki, Day 2 Bột Chiên, Day 3 Korean BBQ on Rice, Day 4 Vietnamese Broken Rice (Cơm Tấm), Day 5 Bulgogi Bánh Mì finale; updated kitchen layout and web codex.
- **Localization** (English / Tiếng Việt / 한국어), choice persisted between sessions.
- **Solo mode** with a deterministic utility-AI partner chef (`AI/BotController.cs`).
- **Two-bot autoplay QA** runner with report (`AI/TwoBotQAManager.cs`), in-game 2-bot test button and QA report modal.
- A* waypoint navigation (`AI/KitchenNavGraph.cs`), TAS-style steering, and inter-bot communication (`AI/BotCommunicationHub.cs`).
- `AI/BotLogger.cs` disk telemetry and `AI/BotDebugHUD.cs` (F1, off by default).

### Fixed
- Crosswalk collider snags and lost cooking targets.
- 1 s interact debounce, 5 s anti-re-pickup, stove premature-pickup guard.
- Pass Counter handoffs between bots unblocked.
- Simmer wok cook-reset loop; tteokbokki assembly in the wok / on tables.
- Hands-full cooker retry loop, table item duplication, false "standby stuck" alerts.
- Serving window deadlock, table dish pickup, QA day-progression guard.
- Station ping-pong loops, bots freezing at shift end; kitchen now cleaned each day; multi-item tickets supported.
- Bột Chiên requires eggs; bots fetch eggs; counter-overflow freeze.

## 2026-11-08 — Kitchen Visuals, Tutorial, Economy & LAN Discovery

### Added
- Free pixel-art beef / noodle / flour sprites and crate mappings; standardized kitchen sprites.
- Editor build menus and web portal with download mirrors (`web/`).
- Procedural tiled floor, entrance apron, exterior restaurant and grass; responsive prep tables; quick-select radial wheel (`UI/QuickSelectWheelUI.cs`); chef scaling.
- Full-screen HUD, grounded appliances, tighter colliders, **central divider wall**, tall back wall.
- **Kitchen Academy tutorial** with step-guidance HUD; chef switching (`C` key).
- Procedural `Core/AudioManager.cs` (music + SFX).
- **LAN room discovery**, reputation health bar, waves, gold economy, upgrade shop, recipe book, mobile joystick.
- First Windows build and web zip deployed.

### Fixed
- RectTransform UI parenting, wall sorting and floor Z-order.

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
  - Level 2: *Lunch Rush* (Village Market; introduces washing dirty plates under multi-order pressure; washing was later cut from the game).
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

