# Architecture Overview — Two Ladles

> [!IMPORTANT]
> **Engine Transition Notice:** Active development on the Godot prototype has officially **stopped**. The active codebase is 100% focused on the **Unity 2022.3.62f3 C#** project located at `unity/TwoLadles/`. The Godot files remain archived on the `godot` branch for historical reference.

## 1. Active Unity Codebase Structure (`unity/TwoLadles/`)

```text
unity/TwoLadles/
├── Assets/
│   ├── Editor/
│   │   └── BuildScript.cs               # Headless build & automated scene builder
│   ├── Resources/
│   │   └── Sprites/                     # Sliced pixel-art kitchen tiles, appliances & item icons
│   ├── Scenes/
│   │   └── MainKitchen.unity            # Primary 2.5D kitchen game scene
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GameData.cs              # Recipes, ingredients, appliances, dishes registry
│   │   │   ├── ShiftManager.cs          # Orders, Flow (0-100), Rush Rhythm, scoring, plate lifecycle
│   │   │   └── ProgressionManager.cs    # Career coins, unlocks, Linh/Jiwon tiers, JSON save/load
│   │   ├── Data/
│   │   │   ├── CampaignLevelSpec.cs     # 5-level campaign targets, recipe subsets, pacing
│   │   │   ├── CharacterProgressionData.cs # Linh/Jiwon specialty perks and part-time worker roles
│   │   │   └── RecipeData.cs            # Typed recipe step data models and enums
│   │   ├── Kitchen/
│   │   │   ├── KitchenController.cs     # 3D layout builder, physical BoxColliders & station router
│   │   │   └── PartTimeWorker.cs        # Jiwon part-timer state machine (Walk In -> Work -> Walk Out)
│   │   ├── Networking/
│   │   │   └── NetworkManager.cs        # Authoritative UDP 24567 host/client synchronization
│   │   ├── Player/
│   │   │   └── PlayerController.cs       # Movement, physical collisions, item hover tilt & throw
│   │   ├── Stations/
│   │   │   ├── StationBase.cs           # Distance checks, interaction hooks, 3D world labels
│   │   │   ├── PrepStation.cs           # 4-strike rhythm cadence mechanic
│   │   │   ├── CookingStation.cs        # 5-state heat machine (Raw -> Cooking -> Optimal -> Overcooked -> Burnt)
│   │   │   ├── AssemblyStation.cs       # Vessel + ingredients matcher & broth-last validation
│   │   │   ├── PassCounterStation.cs    # 3-slot counter with 4.0s partner handoff flow bonus
│   │   │   ├── ServingStation.cs        # Order matching, score calculation & dirty plate return
│   │   │   ├── WashingStation.cs        # 4-stroke scrub cycle & clean plate stack replenishment
│   │   │   ├── CounterStation.cs        # Holding counter tables (store, retrieve, swap items)
│   │   │   ├── CrateStation.cs          # Raw ingredient dispensers
│   │   │   └── TrashStation.cs          # Food waste and burnt dish disposal
│   │   └── UI/
│   │       ├── LobbyUI.cs               # Pre-shift lobby, chef selection exclusivity & readiness
│   │       └── KitchenHUD.cs            # Live order tickets, Flow meter, Rush timer & prompts
│   └── Tests/
│       └── TwoLadlesTestSuite.cs        # 14-suite automated test suite (692 assertions)
```

## 2. Archived Godot Prototype Structure (Historical Reference)

```text
two-ladles/
├── project.godot                     # Godot 4.7.2 config, Mobile renderer, Autoloads, Inputs
├── export_presets.cfg                # Windows, Linux, and Android export presets
├── scenes/
│   └── main.tscn                     # Entry scene hosting Lobby, HUD, and 3D Kitchen
├── scripts/
│   ├── main.gd                       # UI/Lobby/HUD coordinator & CLI smoke-test hooks
│   ├── autoload/
│   │   ├── game_data.gd              # Data-driven registry (Recipes, Ingredients, Appliances, Perks, Tiers)
│   │   ├── network_manager.gd        # Authoritative ENet UDP multiplayer & Lobby validation
│   │   ├── progression_manager.gd    # Persistent career score, coins, stage unlocks, character tiers, save/load
│   │   └── shift_manager.gd          # Order queue, Scoring, Kitchen Flow, Rush Rhythm, Plates, Endless mode
│   ├── data/
│   │   ├── campaign_level_spec.gd    # Typed Campaign Level specification (star targets, recipes, pacing)
│   │   ├── character_progression_data.gd # Linh/Jiwon progression tiers and Worker catalog specs
│   │   └── recipe_data.gd            # Typed RefCounted specs & enums (RecipeData)
│   ├── kitchen/
│   │   ├── kitchen_controller.gd     # 2.5D isometric kitchen builder & authoritative station router
│   │   └── part_time_worker.gd       # Jiwon hired worker deterministic state machine (WALKING_IN -> WORKING -> WALKING_OUT)
│   ├── player/
│   │   └── player_controller.gd      # 3D chef movement, bounds clamping, carried item display, Linh 3rd arm tiers
│   └── stations/
│       ├── station_base.gd           # Base interactive station with range check & 3D billboard
│       ├── prep_station.gd           # 4-strike rhythm cadence mechanic
│       ├── cooking_station.gd        # RAW->COOKING->OPTIMAL->OVERCOOKED->BURNT state machine
│       ├── pass_counter.gd           # 3-slot Pass Counter with 4s partner handoff verification
│       ├── assembly_station.gd       # Vessel + recipe component validator
│       ├── serving_station.gd        # Order delivery & dirty plate generator
│       └── washing_station.gd        # 4-stroke scrub mechanic & finite plate dispenser
└── tests/
    ├── test_runner.gd                # SceneTree test wrapper
    └── test_suite_node.gd            # 114-assertion automated test suite across 13 suites
```

## 2. Separation of Concerns

1. **Data Layer (`RecipeData`, `CampaignLevelSpec`, `CharacterProgressionData`, `GameData`):** Pure data specifications. Recipes, appliances, campaign levels, character tiers, and worker catalogs are configured declaratively in `GameData`.
2. **Authoritative Domain Layer (`ShiftManager`, `ProgressionManager`, `PrepStation`, `CookingStation`, `PassCounter`, `AssemblyStation`, `ServingStation`, `WashingStation`, `PartTimeWorker`):** Deterministic state machines that can be instantiated and verified in headless tests in milliseconds without requiring network sockets or GPU rendering.
3. **Network & Presentation Layer (`NetworkManager`, `KitchenController`, `main.gd`):** Routes player inputs via RPC to the host, validates actions, and mirrors state dictionaries to connected clients.

## 3. Progression & Persistence Pipeline (`ProgressionManager`)

- **Dual-Storage Resilience:** Detects sandboxed environments and seamlessly falls back between `user://progression.json` (device standard) and workspace storage (`res://.user/progression.json`).
- **Stage Records & Unlocks:** Tracks 1-3 star achievements, coins, and unlocks higher levels when a player earns at least 2 stars.
- **Character Tier Progression:** Automatically triggers Linh's execution speed tiers and Jiwon's hiring capacity tiers upon completing Level 3 (Tier 2) and Level 5 (Tier 3).
- **Kitchen Shop:** Manages purchases (e.g. Sharp Santoku Knives, Double-Walled Stockpot) and checks coin balances.

## 4. Part-Time Worker Lifecycle (`PartTimeWorker`)

- **State Machine:** `WALKING_IN` (traverses towards target counter) → `WORKING` (executes periodic strikes/scrubs) → `WALKING_OUT` (traverses toward exit on expiration) → `EXPIRED` (queue_free).
- **Host Authoritative:** Only the host spawns workers, advances their timers, and deducts coins. Clients render synchronized 3D representations through `_rpc_sync_spawn_worker`.
