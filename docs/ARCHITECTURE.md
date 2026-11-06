# Architecture Overview — Two Ladles

## 1. Directory & Module Structure

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
