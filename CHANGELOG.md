# Changelog

All notable changes to **Two Ladles** are documented in this file.

## 2026-11-11 — v1.4.7: Face-Friendly Carry & Smaller Portrait

- **Carried item floats higher** (1.85 → 2.3) so the chef's face stays visible.
- **Jiwon dating-app portrait:** head smaller on both platforms (zoom width 270 → 230); on mobile the sprite sits higher (offset -35 → -18).
- Tests: Test18 +3 — 356 total.

## 2026-11-11 — v1.4.6: Pause Settings & Storage Split

- **Pause button is bright yellow** so it stands out in the top bar.
- **BGM toggle moved to Pause → ⚙ Settings tab** (with a Back button), removed from the top bar.
- **2 ingredients per storage station, no duplicates:**
  - West fridge: toboki, egg. West shelf: gochujang, flour.
  - East fridge: raw meat, radish/carrot. East shelf: rice, baguette.
- **Storage wheel auto-closes** when the chef walks more than ~3m away from the fridge/shelf.
- Tests: Test20 +5 — 353 total.

## 2026-11-10 — v1.4.5: Balanced Storage

- **Fridge and shelf now hold 4 ingredients each, no duplicates** (same on Linh's west and Jiwon's east side).
  - Fridge: meat, radish/carrot, egg, toboki (rice cakes).
  - Shelf: gochujang, flour, rice, baguette.
- Tests: Test20 +3 — 348 total.

## 2026-11-10 — v1.4.4: Pause Menu & LAN Day Cycle

- **Pause menu**: the HUD "Lobby" button is now ⏸ (or Esc) and opens RESUME / LEAVE GAME. Solo freezes time; LAN keeps the shared kitchen running.
- **LAN end of day**: the host sends the day report to the guest (`DAY_REPORT`). The guest sees results, earns gold, can shop. Both press READY (toggle, `DAY_READY`); the host then starts the next day for both. LEVEL SELECT is hidden in LAN and closing the shop returns to results.
- **Guest can no longer end its own shift** (`ShiftManager.EndShift` guard).
- **LAN partner chef** flips direction and wobbles when walking.
- **North double door cleared**: decorative divider tables removed; the Serving Counter moved from z 4.2 to z 3.2 (bot nav updated).
- Tests: Test19 +11, new Test20 (11) — 345 total.

## 2026-11-10 — v1.4.3: LAN Story Sync & Portrait Fit

- **LAN co-op**: the joining player now sees the dating-app match (swipe) scene too. On Day 1 in LAN both phones always play it, so neither jumps ahead to the notification (`PhoneStoryUI.ShouldShowMatchScene`).
- **Jiwon swipe card**: sprite raised inside the square frame (mobile -70 → -35, desktop -24 → -4). Frame and card unchanged.
- Tests: Test18 LAN assertion flipped (user-approved requirement change) + 2 new (323 total).

## 2026-11-10 — v1.4.2: Menu Flow & Story Polish

- **Upgrades Shop**: opening it from the main menu now closes the menu window; closing the shop returns to the main menu.
- **Stories menu**: replaying a story hides the menu only while the phone plays, then returns to the Stories list. Esc no longer toggles the menu while the phone is open.
- **Jiwon swipe card**: portrait frame is now a square headshot (`PhoneStoryUI.PORTRAIT_FRAME_W/H` = 220); card top edge unchanged.
- **LAN co-op**: the host sees the full opening cutscene; the joining player skips the dating-app match (swipe) scene (`PhoneStoryUI.ShouldShowMatchScene`).
- Tests: +5 assertions in Test18 (321 total).

## 2026-11-10 — v1.4.1: Joystick Placement & Mobile 2-Bot QA Test

- **Mobile joystick moved up and right** (`KitchenHUD.MOBILE_JOYSTICK_POS` = 150,140; was 92,92) so it sits clear of the screen corner.
- **2-Bot QA test now runs on mobile**: Settings → pick test day / upgrades → 🤖 2-BOT TEST RUN. The report appears on screen at the end. Report files are saved in the app's own storage (`Application.persistentDataPath/qa_reports`) because the APK folder is read-only; a file-save error can no longer block the on-screen report.
- `BotLogger` writes `bot_qa.log` to app storage on mobile.
- Settings layout unified: UI size row (-280), 2-bot test (-325), QA day/upgrades (-370) on every platform.
- Tests: +3 assertions in Test17 (316 total).

## 2026-11-10 — v1.4.0: Opening Order, UI Size, Placeholder Chef & Fresh Translations

### Changed
- **Opening order:** Dating swipe card now plays before the notification lock screen (blackout → swipe → lock screen → chat).
- **Opening card lowered:** Whole Jiwon swipe card moved down on the phone screen (`-250` mobile / `-230` desktop) so it no longer hugs the top edge; portrait framing unchanged.
- **Translations refreshed:** `Localization.csv` rebuilt (132 keys); deleted dead keys, rewrote LAN/controls/level text, added keys for menu, popups, lobby, levels, settings and story. Lobby static labels now switch language live. Tutorial text uses "Rice Cake" instead of "Toboki".
- **Version bump:** `1.4.0` / Android code `140`; web download links use `?v=1.4.0`.

### Added
- **UI / HUD size option:** Settings → `[−] UI SIZE: 100% [+]` (80%–160%, 10% steps). Saved in PlayerPrefs and applied to every canvas (`UIScaleSettings.cs`).
- **Placeholder chef:** Chef switch in a LAN room cycles Linh → Jiwon → Placeholder. Placeholder never blocks the other player's pick, but cannot press READY and the shift cannot start while anyone uses it.

### Tests
- 10 new assertions (placeholder lobby rules, UI-size clamping) → **313 / 313 passed**.

## 2026-11-10 — Auto-Scan Prominence, Last-Resort Manual IP & v1.3.0 Web Release

### Changed
- **Auto-Scan Primary on Join Screen:** Swapped Join Screen layout so automatic Wi-Fi kitchen scanning is front and center with large, easy-to-tap slots (`520x42px`). Found rooms immediately display the room name, host chef, player count, and IP address.
- **Manual IP as Last Resort:** Direct IP input field relocated to the bottom under an explicit "OR ENTER HOST IP MANUALLY (LAST RESORT)" banner for networks where Wi-Fi broadcast/multicast is restricted.
- **Version Code Bump (v1.3.0 / Code 130):** Updated Unity PlayerSettings bundle version to `1.3.0` and Android bundle version code to `130`, ensuring Android devices recognize the APK as a genuine update over earlier installs.
- **Web Download Portal Updated:** Updated `web/index.html` download cards to `v1.3.0` with cache-busting query strings (`?v=1.3.0`) so browsers do not serve stale cached APK files. Added `v1.3.0` version indicator in the lobby menu.

## 2026-11-10 — Per-Chef Upgrades, Score-Based Hiring & Mobile Polish

### Added
- **Per-chef upgrades over LAN:** At shift start, client sends local purchased upgrade tiers to host via `UPGRADES` packet. Host validates and applies them to client's chef (`move_speed`, `cook_speed`, `prep_window`, and tier abilities like Linh's multi-tier chop cadence).
- **Synchronized Part-Timers:** Host broadcasts newly hired workers (`WORKER` packet with worker id, role, target station, and tier). Both host and client spawn and render the part-timer walking to their station.
- **Lobby Story & Cutscenes Replay:** Added `STORIES` button to the Main Menu allowing players to replay the opening swipe/match cutscene and Day 1–5 chef text conversations and recipe introductions at any time.
- **Messenger-Style Chat UI:** Texting screen updated with a Facebook Messenger-style decorative input field (`Aa  Type a message...`), smiley icon, and paper-plane send button in Messenger blue. Texting and reading animations are snappier.
- **Tutorial Overhaul:** Updated tutorial text to match active gameplay: Send-Out Window at north door, Pass Counter flow bonus (+15 Flow), throw mechanics ([Q] / right-click), holding counters, and part-timer hiring ([H] key). Removed obsolete dish-washing and submit-table text.

### Changed
- **Shift Score Hiring:** Hiring part-timers now costs 20 current shift score points instead of gold coins (`ShiftManager.Instance.totalScore`), preventing progression economy stalls.
- **Mobile Story Framing:** Lowered Jiwon's portrait offset on mobile screens so character art is properly framed below the status bar.
- **Test Suite:** 303 assertions across all 19 test suites, all passing cleanly.

## 2026-11-10 — LAN gameplay sync

### Fixed
- **Each device controls only its own chef** (the host laptop used to control both chefs).
- **Host-authoritative stations:** the client sends only intents (`INTERACT` with station id, `THROW`, `HIRE`); the host streams station contents, cooking state/timers, chop progress, assembly parts, pass slots and both chefs' held items 10×/s (`STATIONS` packet). Client cooking timers are paused so the two copies cannot drift.
- Part-Timer `[H]` hire now works from the client.
- Network packet handling wrapped so one bad packet cannot stop processing.
- Test suite: 286 assertions (was 268), all passing.

## 2026-11-10 — Manual IP Join (primary LAN path)

### Changed
- **Host shows its IP big and clear:** the room screen shows `YOUR IP: x.x.x.x (partner types this in JOIN)` in bold yellow (other adapters listed as "or a / b"), or a "connect this device to Wi-Fi" warning if none is found.
- **Join popup redesigned:** the IP box + **CONNECT** button are now at the top with a clear instruction; the auto-scan list is secondary below it. The button was renamed from "AUTO-JOIN LAN" to "CONNECT".
- Typed IPs are cleaned (spaces trimmed, commas → dots, `:port` removed); loopback/invalid input is rejected with a message.
- Connection status/errors now show inside the Join popup and room screen (they were hidden behind the popup before).
- Test suite: 268 assertions (was 261).

## 2026-11-10 — Recipe Discovery & LAN Wi-Fi Fixes

### Added
- **Recipe Book discovery:** a day's new recipe fades in on the HUD when that day starts; recipes not yet discovered stay as blank, empty slots.
- **LAN Wi-Fi scan:** the Join screen now scans the real Wi-Fi network (subnet broadcast + direct sweep of every address in the /24 subnet for 60 s) instead of localhost. Found kitchens fill the IP box automatically. The host's room screen shows its Wi-Fi IP for manual joining.
- **2-player ready gate:** a LAN co-op level starts only when 2 chefs are in the room, both pressed READY, and the host presses START. Chefs now start as not ready.
- **Reliable join:** the client re-sends HELLO every 0.5 s (up to 10 times) and shows a clear Wi-Fi / firewall hint if the host never answers.

### Fixed
- Direct-IP box no longer defaults to 127.0.0.1; loopback/invalid IPs are rejected with a helpful message.
- Android build forces the INTERNET permission (ForceInternetPermission: 1).
- Test suite: 261 assertions (was 233), all passing.

## 2026-11-10 — Final Hand-in Cleanup

### Changed
- **Pitch deck fixed** (`docs/two_ladles_concept.pptx`, generated by `scripts/generate_pitch_deck.py`): now describes Solo Mode with the AI partner, raw-UDP LAN networking (ports 24567 / 24568), Windows ↔ Android cross-play, 19 test suites / 233 assertions, the single Part-Timer [H] button, and code-generated music & sound effects. Old Netcode / "no AI teammates" claims removed.
- Documentation refreshed: Godot is described as retired, part-timer hiring described as one button that fills every free slot.
- Helper scripts (`import_itch_assets.py`, `generate_new_food_assets.py`, `generate_svg_icon.py`, `slice_character_sprites.py`) now write only to the Unity project / web folder.

### Removed
- Retired Godot prototype files from `main` (`project.godot`, `export_presets.cfg`, `scenes/`, GDScript `scripts/*/`, `tests/*.gd`, `assets/sprites/`, root `icon.svg`/`icon.txt`, `scripts/setup_android_tools.py`) and the Godot GitHub Actions workflows (`ci.yml`, `android-build.yml`). The `godot` branch was deleted.
- AI-agent notes and design drafts are now kept locally only and are no longer published.

## 2026-11-10 — Lobby Revamp & Docs Refresh

### Added
- **Lobby revamp for non-technical players** (`UI/LobbyUI.cs`): main menu (Host / Join / Tutorial / Upgrades / Settings), Host popup asking chef name + kitchen name (remembered via PlayerPrefs), Join popup listing LAN-discovered room slots plus a direct-IP box, Room panel with Day 1–5 / Endless selection and Ready / Start / Leave, Settings with language (EN / VI / KO), solo vs. LAN co-op, chef select. A faint (20% opacity) 2-Bot QA run button sits in the Settings popup (hidden on mobile).
- Purchased upgrades and chef tiers are cached and restored from the save file.
- **LAN order sync** (`ORDERS` packet): the host streams orders, patience timers, score, reputation, Kitchen Flow and Rush Rhythm to the client every 0.25 s; the client no longer spawns or expires orders on its own.
- **Recipe timing variety:** Day 3 Korean BBQ on Rice and Day 4 Broken Rice orders get +2 s patience (`ShiftManager.GetRecipePatienceBonus`).
- Test suite 19: LAN Order Sync, Recipe Timing & Bot Safety.
- **QA test options in Settings** (desktop only): `TEST DAY` button (All 1–5 or one day) and `ALL UPGRADES: ON/OFF` toggle (default ON). The QA run uses a temporary sandbox (`ProgressionManager.BeginQASandbox` / `EndQASandbox`), so the real save file is never changed.
- **Bot baguette rule:** a wrong or unneeded baguette / bánh mì item is thrown in the trash; other unneeded food is parked on a holding table.
- `BuildScript.BuildAndroid` points Unity at the local JDK 11 / Android SDK (cmdline-tools 6.0) / NDK r23b in `%LOCALAPPDATA%\Programs\two-ladles-tools\` when present. Unity 2022.3 rejects JDK 17, and the newest SDK command-line tools need JDK 17, so cmdline-tools 6.0 is used.

### Changed
- On-screen mobile controls now appear only on phones/tablets (`Application.isMobilePlatform`); on desktop they can be toggled with `F3`.
- Web mirror `web/downloads/TwoLadles-Windows-x64.zip` refreshed.
- Documentation updated to match the current Unity code (project path `unity/TwoLadles/Two Ladles/`, 19 test suites, raw-UDP networking, 5-day menu).
- Part-timer crew are bigger (height 1.75) so they are easier to see.
- One part-timer button (`[H]`) hires every free part-timer slot for Jiwon's tier at once (`RequestHireAvailableCrew`).
- Holding counters reject an item when they already hold the same item.
- **No washing chore:** plates and bowls are unlimited (taken from the quick-select wheel); serving never creates dirty plates. Test suite 7 is now "No Washing Chore" and the docs no longer describe washing.
- Day 5 Vietnamese story dialogue uses friendly `tớ / cậu` instead of `anh / em` (warm and light-hearted, not romantic). Day 5's first Korean line fixed (it contained Vietnamese).

### Fixed
- **Day 5 Bot Baguette Pipeline** (`AI/BotController.cs`): Linh fetches egg immediately once flour begins frying to create baguette dough on time; Linh handles and combines partial bánh mì components, baked baguettes, and cooked flour cakes; Linh retrieves passed `bulgogi_seared` from the pass counter; cooker BURNT pickup unblocks burned cookers; Jiwon caps beef pipeline taking `bulgogi_seared` and `bulgogi_portioned` into account.
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

