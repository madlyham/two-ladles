# TWO LADLES — Domain Glossary (`GLOSSARY.md`)

Canonical terms used across the Unity codebase (`unity/TwoLadles/Two Ladles/Assets/Scripts/`), design documents, and automated tests.

## Networking & Modes

| Term | Canonical Definition |
| :--- | :--- |
| **Authoritative Host** | Peer ID `1`. Runs `NetworkManager` as host on UDP port `24567` (raw `System.Net.Sockets.UdpClient`, plain-text `CMD|...` packets). Owns order timers, cooking states, Pass Counter handoff validation, Kitchen Flow, Rush Rhythm, reputation, waves, and scoring. |
| **Client** | Peer ID `2` connected to the host over local Wi-Fi (or `127.0.0.1` for dual-instance desktop testing). Sends `POS` movement updates, `INTERACT` and `THROW` requests; the host validates range (`StationBase.MAX_INTERACT_DISTANCE = 2.6f`) and executes them. |
| **Order Sync (`ORDERS`)** | Host packet sent every 0.25 s with the active orders, patience timers, score, reputation, Kitchen Flow and Rush Rhythm. The client only displays these; it never spawns or expires orders itself. |
| **LAN Room Discovery** | Host broadcasts a `LAN_BEACON` (room name, host name, port, player count) on UDP `24568` every 1.2 s; the Join screen lists discovered rooms as tappable slots. Direct IP join is also available. |
| **Solo Mode** | One human chef plus an AI partner (`BotController`) controlling the other chef. Toggled in Settings ("MODE"). Not used in 2-player LAN sessions. |
| **Two-Bot QA** | Automated test mode (`TwoBotQAManager`) in which bots control both chefs and play through selected days, writing a report to `qa_reports/`. Desktop/editor only. |

## Kitchen Rules

| Term | Canonical Definition |
| :--- | :--- |
| **Cross-Prep Interdependence** | Core design rule: Linh (West) and Jiwon (East) each own different ingredients and appliances, so most dishes require passing items across the Pass Counter. Ownership is defined per ingredient in `GameData.ingredients` (`sourceZone` / `targetZone`; some items are `shared`). |
| **Center Divider** | Full-height wall (`CenterDivider_Full`) splitting the kitchen; chefs stay in their own half and exchange items via the Pass Counter, Serving Counter, or throwing. |
| **Pass Counter** | Shared 3-slot counter at the kitchen center (`Stations/PassCounter.cs`). Tracks who placed each item and when, to detect coordinated handoffs. |
| **Coordinated Handoff** | Player A places an item needed by an active, unclaimed order on the Pass Counter and Player B (the other chef) retrieves it within `4000 ms` (`ShiftManager.HANDOFF_WINDOW_MS`). Awards **+15 Kitchen Flow** once per order. |
| **Kitchen Flow** | Team momentum meter (`0`–`100`). Increases via handoffs (`+15`) and serving (`+12` standard, `+20` optimal); decreases on expired orders (`-15`). |
| **Rush Rhythm** | Triggered when Kitchen Flow reaches `100`: a `15.0` s window with a `1.5x` score multiplier. |
| **Prep Cadence** | Rhythm input on a `PrepStation` (`IDEAL_CADENCE_SEC = 0.45s`). Strikes graded `PERFECT`, `GOOD`, or `MISS`; misses never block completion. |
| **Cooking State Machine** | Appliance lifecycle: `RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT`. |
| **Holding Counter** | Empty counter (`CounterStation`) where any item can be parked. Rejects a second copy of the item it already holds. |
| **Throw** | `Q` / right-click launches the carried item toward the partner (who catches with empty hands), a holding counter, Pass Counter slot, or trash. |
| **Plates & Bowls** | Unlimited; taken from the quick-select wheel. There is no washing chore — serving never creates dirty plates. |

## Progression & Content

| Term | Canonical Definition |
| :--- | :--- |
| **Day (Campaign Stage)** | One of 5 sequential campaign days. Each day adds one dish to the menu: Day 1 Tteokbokki, Day 2 Bột Chiên, Day 3 Korean BBQ on Rice, Day 4 Vietnamese Broken Rice (Cơm Tấm), Day 5 Bulgogi Bánh Mì (fusion finale, 180 s, Bulgogi Bánh Mì only). Defined in `GameData.dayMenuRecipes` and `GetDayAllowedRecipes`. |
| **Wave** | A batch of orders within a day (`WaveSpec`). Campaign days have 3 waves with rising order count, faster spawns, and lower patience. Completing all waves wins the day. Day 3 BBQ and Day 4 Broken Rice orders get +2 s patience. |
| **Reputation** | Restaurant health bar (`0`–`100`). Each expired order costs `25`; reaching `0` ends the shift in defeat. |
| **Coins (Gold)** | Persistent currency (`ProgressionManager.coins`; UI also calls it "gold"). Earned on victory from remaining reputation (`CalculateGoldReward`); spent on upgrades and part-time crew. |
| **Upgrade Shop** | Permanent kitchen upgrades bought with coins (e.g. `sharp_santoku_knives`, `chef_speed_boots`, `turbo_wok_burners`, `specialist_tier_2/3`, `high_pressure_sink`, `extra_plate_stack`, `insulated_broth_pot`). Saved in `twoladles_save.json`. |
| **Recipe Book** | In-shift overlay (`R`) listing the current day's dishes and their steps. |
| **Endless Rush Mode** | Unlimited mode mixing all five day dishes. Spawn interval shrinks from 14 s to 6.5 s; max concurrent orders grow from 2 to 5 over time. Ends after 3 failed orders; best score/orders/duration are saved. |
| **Tutorial (Kitchen Academy)** | Single-wave guided lesson with one Tteokbokki order and step-guidance HUD. |
| **Story Phone** | Day-start phone chat between Linh and Jiwon (`PhoneStoryUI`, text in `StoryScript`). Shown only for campaign days 1–5; skippable. In LAN play the host's close is synced via `STORY_DONE`. |
| **Day-Night Cycle** | Visual-only lighting change per wave (`DayNightCycleManager`): day → warm sunset → night. |
| **Specialization** | Lobby perk (`PREP MASTER`, `FLAME KEEPER`, `MARKET RUNNER`, `FUSION ARTIST`). Both players may pick the same one. |
| **Execution Specialist** | Linh's progression: speed multipliers 1.0x → 0.92x → 0.85x and a third mechanical arm (none → rustic brass/steel → glowing white-and-blue). |
| **Call in the Crew** | Jiwon's ability (`H`): spends coins (20) to hire a part-time helper (`prep_assistant`) for a 20 s contract, 45 s cooldown, host-validated. |
| **Part-Time Worker** | Hired helper (`PartTimeWorker`, height 1.75) with a 4-state machine `WALKING_IN -> WORKING -> WALKING_OUT -> EXPIRED`, auto-chopping (there is no washing chore). |

## AI & Tooling

| Term | Canonical Definition |
| :--- | :--- |
| **BotController** | Deterministic utility-AI chef used for the solo partner and Two-Bot QA. |
| **KitchenNavGraph** | Waypoint graph with A* pathfinding used by bots to route around counters and the divider. |
| **BotCommunicationHub** | Shared blackboard bots use to coordinate handoffs and avoid duplicate work. |
| **BotLogger** | Writes bot telemetry to `Logs/bot_qa.log` (falls back to `Application.persistentDataPath`). |
| **BotDebugHUD** | `F1` overlay showing each bot's goal, target, held item, and stuck timer. Off by default; desktop only. |
| **Legacy Recipes** | Older recipe data (`pho_bo`, `banh_mi_thit`, bowl `tteokbokki`, `kimchi_bokkeumbap`) still present in `GameData.recipes` and tested, but not on the active 5-day menu. |
