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
│   │   ├── game_data.gd              # Data-driven registry (Recipes, Ingredients, Appliances, Perks)
│   │   ├── network_manager.gd        # Authoritative ENet UDP multiplayer & Lobby validation
│   │   └── shift_manager.gd          # Order queue, Scoring, Kitchen Flow, Rush Rhythm, Plates
│   ├── data/
│   │   └── recipe_data.gd            # Typed RefCounted specs & enums (RecipeData)
│   ├── kitchen/
│   │   └── kitchen_controller.gd     # 2.5D isometric kitchen builder & authoritative station router
│   ├── player/
│   │   └── player_controller.gd      # 3D chef movement, bounds clamping, carried item display
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
    └── test_suite_node.gd            # 62-assertion automated test suite
```

## 2. Separation of Concerns

1. **Data Layer (`RecipeData`, `GameData`):** Pure data specifications. Adding a new recipe or appliance in Milestone 3 requires only registering a new `RecipeSpec` or `ApplianceSpec` in `GameData` without modifying station classes.
2. **Authoritative Domain Layer (`ShiftManager`, `PrepStation`, `CookingStation`, `PassCounter`, `AssemblyStation`, `ServingStation`, `WashingStation`):** Deterministic state machines that can be instantiated and verified in headless tests in milliseconds without requiring network sockets or GPU rendering.
3. **Network & Presentation Layer (`NetworkManager`, `KitchenController`, `main.gd`):** Routes player inputs via RPC to the host and mirrors validated state dictionaries to connected clients.
