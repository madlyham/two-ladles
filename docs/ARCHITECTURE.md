# Architecture Overview — Two Ladles

> [!IMPORTANT]
> **Active engine:** Unity **2022.3.62f3** (C#). The Unity project lives at `unity/TwoLadles/Two Ladles/` (note the space). The earlier Godot 4.7.2 prototype has been retired and removed from the repository.

## 1. Project Structure (`unity/TwoLadles/Two Ladles/`)

```text
Two Ladles/
├── Assets/
│   ├── Editor/
│   │   ├── BuildScript.cs              # Scene generation, Windows/Android builds, preview capture, batch test & QA entry points
│   │   └── TwoLadlesEditorMenu.cs      # "Two Ladles/..." editor menu items
│   ├── Resources/                      # Runtime-loaded Sprites/ and Fonts/
│   ├── Scenes/MainKitchen.unity        # Main 2.5D kitchen scene
│   ├── Sprites/                        # Additional pixel-art sprites
│   ├── Scripts/
│   │   ├── AI/
│   │   │   ├── BotController.cs        # Deterministic utility-AI chef (solo partner + 2-bot QA)
│   │   │   ├── KitchenNavGraph.cs      # Waypoint graph + A* pathfinding
│   │   │   ├── BotCommunicationHub.cs  # Shared blackboard for bot coordination / handoffs
│   │   │   ├── TwoBotQAManager.cs      # Two-bot autoplay QA runner + reports (qa_reports/)
│   │   │   ├── BotLogger.cs            # Disk telemetry (Logs/bot_qa.log)
│   │   │   └── BotDebugHUD.cs          # F1 debug overlay (off by default)
│   │   ├── Core/
│   │   │   ├── AutoBootstrap.cs        # Creates managers, kitchen, canvas, HUD, lobby at scene load
│   │   │   ├── GameData.cs             # Ingredients, appliances, recipes, 5-day menu, specializations, workers
│   │   │   ├── ShiftManager.cs         # Orders, waves, reputation, Flow, Rush Rhythm, scoring, modes
│   │   │   ├── ProgressionManager.cs   # Coins, career score, stage unlocks, tiers, upgrades, endless bests
│   │   │   ├── LocalizationManager.cs  # EN / VI / KO strings, saved in PlayerPrefs
│   │   │   └── AudioManager.cs         # Procedural music and SFX
│   │   ├── Data/
│   │   │   ├── CampaignLevelSpec.cs    # Level + WaveSpec data
│   │   │   ├── CharacterProgressionData.cs # Linh/Jiwon tiers, worker specs
│   │   │   └── RecipeData.cs           # Recipe/step data models and enums
│   │   ├── IO/SaveSystem.cs            # JSON save at persistentDataPath/twoladles_save.json
│   │   ├── Kitchen/
│   │   │   ├── KitchenController.cs    # Builds layout, colliders, divider wall; routes interactions; hires crew
│   │   │   └── PartTimeWorker.cs       # WALKING_IN -> WORKING -> WALKING_OUT -> EXPIRED
│   │   ├── Networking/NetworkManager.cs # Host-authoritative LAN UDP (24567) + discovery (24568)
│   │   ├── Player/PlayerController.cs  # CharacterController movement, carried-item hover, throw
│   │   ├── Stations/
│   │   │   ├── StationBase.cs          # Range check (MAX_INTERACT_DISTANCE = 2.6f), interaction hooks
│   │   │   ├── PrepStation.cs          # Rhythm cadence (0.45 s ideal)
│   │   │   ├── CookingStation.cs       # RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT
│   │   │   ├── AssemblyStation.cs      # Vessel + component validation (legacy recipes)
│   │   │   ├── PassCounter.cs          # 3-slot shared counter, 4000 ms handoff bonus
│   │   │   ├── ServingStation.cs       # Order matching, scoring
│   │   │   ├── WashingStation.cs       # Legacy (unused): washing was cut; no sink is placed in the kitchen
│   │   │   ├── CounterStation.cs       # Holding counters (self-assembly surface)
│   │   │   ├── CrateStation.cs         # Ingredient dispensers (fridges, shelves)
│   │   │   └── TrashStation.cs         # Waste disposal
│   │   ├── Story/StoryScript.cs        # Day 1–5 phone-chat story text
│   │   ├── UI/
│   │   │   ├── LobbyUI.cs              # Main menu, Host/Join popups, room panel, settings, upgrades
│   │   │   ├── KitchenHUD.cs           # Tickets, Flow, reputation, waves, recipe book (R), mobile controls
│   │   │   ├── PhoneStoryUI.cs         # Day-start story phone
│   │   │   ├── QuickSelectWheelUI.cs   # Radial quick-select wheel
│   │   │   └── StoryDebugPanel.cs      # F9 story debug (desktop)
│   │   └── Visuals/DayNightCycleManager.cs # Per-wave lighting (visual only)
│   └── Tests/
│       ├── TwoLadlesTestSuite.cs       # 18 automated test suites
│       └── TwoLadlesTestRunner.cs      # Menu + batch runners, QA day selection
└── Builds/                             # Windows/ and Android/ output
```

## 2. Startup

`AutoBootstrap` runs after scene load (`RuntimeInitializeOnLoadMethod`) and creates the manager object (`GameData`, `ProgressionManager`, `ShiftManager`, `NetworkManager`, `AudioManager`), the `KitchenController`, a UI canvas + EventSystem, `KitchenHUD`, `LobbyUI`, and `BotDebugHUD`.

## 3. Separation of Concerns

1. **Data layer** — `GameData`, `RecipeData`, `CampaignLevelSpec`, `CharacterProgressionData`. Ingredients, appliances, recipes, the active 5-day menu (`dayMenuRecipes`), legacy recipes, specializations and worker catalog are declared in code.
2. **Domain layer (host-authoritative)** — `ShiftManager`, `ProgressionManager`, the station classes, `PartTimeWorker`. Deterministic state machines that the test suite drives directly without networking or rendering.
3. **Network & presentation** — `NetworkManager` (text packets over UDP), `KitchenController`, `PlayerController`, and UI classes. The client sends movement and interaction requests; the host validates and executes them. See [NETWORKING.md](NETWORKING.md).
4. **AI & tooling** — `BotController` + `KitchenNavGraph` + `BotCommunicationHub` drive the solo partner and two-bot QA; `BotLogger`/`BotDebugHUD` provide diagnostics.

## 4. Kitchen Layout

- West half = Linh, East half = Jiwon, separated by a full-height center divider wall (`CenterDivider_Full`). Items cross via the Pass Counter, Serving Counter, or by throwing.
- Appliances: `stove` (flour cakes, eggs), `simmer_wok` (rice, tteok), `grill_pan` (BBQ / bulgogi beef). Legacy `stockpot` and `noodle_blancher` remain in data for legacy recipes.
- Crates: fridges and shelves on both sides (`fridge_west/east`, `shelf_west/east`).
- All counters, appliances, crates and walls have static `BoxCollider`s; chefs use `CharacterController` (`radius = 0.45`, `height = 1.3`).
- Active-menu dishes have no vessel and are self-assembled on counters/pans.

## 5. Shift & Modes (`ShiftManager`)

- **Campaign Days 1–4:** 3 waves with rising order counts, shorter spawn intervals and lower patience; recipes from `GetDayAllowedRecipes(stage)`.
- **Day 5:** 180 s finale, 3 waves, Bulgogi Bánh Mì only.
- **Tutorial:** 1 wave, a single Tteokbokki order.
- **Endless Rush:** spawn interval `max(6.5, 14 − t/30)` s; concurrent orders grow 2 → 5; ends after 3 failed orders.
- **Reputation:** 0–100, −25 per expired order, defeat at 0. Victory when all waves are completed.
- Orders may contain multiple dishes.

## 6. Progression & Persistence

- `SaveSystem` writes JSON to `Application.persistentDataPath/twoladles_save.json`.
- `ProgressionManager` stores coins (default 150), career score, highest unlocked stage, selected stage, Linh/Jiwon tiers, per-stage records (stars, best score, best reputation, completed), endless bests, and purchased upgrades.
- On victory: coins awarded from remaining reputation (`CalculateGoldReward`); stars = 3 at ≥75 reputation, 2 at ≥40, else 1; next day unlocks (up to Day 5).
- Tiers: completing Day 3 → Tier 2, Day 5 → Tier 3 (both chefs). Tiers 2/3 are also sold in the upgrade shop.
- Upgrades: `sharp_santoku_knives`, `chef_speed_boots`, `turbo_wok_burners`, `specialist_tier_2`, `specialist_tier_3`, `high_pressure_sink`, `extra_plate_stack`, `insulated_broth_pot`.
- Language choice and lobby names (`TwoLadles_PlayerName`, `TwoLadles_RoomName`, `TwoLadles_LastJoinedIP`) are stored in `PlayerPrefs`.

## 7. Part-Time Workers

- Jiwon presses `H` (or the single **Part-Timer [H]** HUD button) to call `RequestHireAvailableCrew`, which fills **every free part-timer slot at once** with prep assistants (20 coins each, 20 s contract, 45 s shared cooldown). There is no separate dishwasher button because washing was removed.
- State machine `WALKING_IN → WORKING → WALKING_OUT → EXPIRED`. Only the host spawns workers, advances timers and deducts coins.

## 8. Presentation Extras

- `PhoneStoryUI` shows the day's story before campaign days 1–5 (not tutorial/endless/QA); the recipe book opens after it closes.
- `DayNightCycleManager` shifts lighting per wave (day → warm sunset → night); visual only.
- `AudioManager` generates music and sound effects procedurally.
- `KitchenHUD` shows mobile controls only when `Application.isMobilePlatform`; `F3` toggles them on desktop.
