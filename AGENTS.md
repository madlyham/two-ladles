# AGENTS.md — Two Ladles Engineering Guidelines & Constraints

This file is the permanent engineering contract for all AI agents and human developers working on **Two Ladles**. The repository is the single source of truth.

## 1. Non-Negotiable Technical Constraints

- **Engine:** Godot Engine `4.7.2-stable` standard edition (GDScript with static typing). Do NOT switch to C# or experimental Godot betas without explicit owner approval.
- **Renderer:** `mobile` renderer targeting landscape Android (`1280x720` base viewport, `canvas_items` stretch) plus Windows/Linux desktop builds for development and testing.
- **Zero Unnecessary Plugins:** Do not add third-party addons or external networking backends. Use Godot's built-in `ENetMultiplayerPeer` and high-level multiplayer API.
- **No Single-Player AI / Hot-Seat Replacement:** Two human players cooperating simultaneously over a local network (UDP default port `24567`) is mandatory.

## 2. Core Gameplay & Architecture Rules

1. **Authoritative Host Model (`scripts/autoload/network_manager.gd` & `shift_manager.gd`):**
   - Peer ID `1` is authoritative over customer orders, timers, cooking state transitions, Pass Counter handoffs, Kitchen Flow, Rush Rhythm, plate count, and scoring.
   - Clients send movement vectors (`unreliable_ordered`) and station interaction requests (`reliable`).
   - Host validates interaction range (`StationBase.MAX_INTERACT_DISTANCE = 2.6`) and lobby rules before mutating state.
2. **Cross-Prep Interdependence (`scripts/autoload/game_data.gd` & `kitchen_controller.gd`):**
   - Preserve the asymmetric West/East kitchen layout: **Linh's West Zone** prepares proteins/starches (`pork_portioned`, `bulgogi_portioned`, `tteok_portioned`) for **Jiwon's East Zone** cooking appliances, while **Jiwon's East Zone** prepares vegetables/slaw/fish cakes (`pickled_veg`, `kimchi_slaw`, `eomuk_prepared`) and clean dishes for **Linh's West Zone** assembly.
3. **Authentic Culinary Sequences:**
   - Never present invented fusion dishes as traditional recipes (*Bulgogi Bánh Mì* is explicitly labeled `"Original Vietnam–Korea Fusion Specialty"`).
   - For *Phở Bò*, rice noodles (`noodles_raw` → `noodles_blanched`) are blanched in the `noodle_blancher` separately from the `stockpot` (`broth_hot`), and hot broth must be poured into the bowl **after** blanched noodles and sliced beef.
4. **Anti-Exploit Pass Counter & Rush Rhythm:**
   - A Pass Counter handoff grants `+15` Kitchen Flow only when retrieved by the **other** player within `4000 ms` for an active order that has not yet claimed a handoff bonus.
   - Reaching `100` Flow resets Flow to `0`, activates **Rush Rhythm** (`1.5x` base score) for `15.0` seconds, and pauses Flow generation until Rush Rhythm ends.

## 3. Testing & CI Discipline

- **Run Automated Tests Before Every Commit:**
  ```powershell
  & "$env:LOCALAPPDATA\Programs\two-ladles-tools\godot\Godot_v4.7.2-stable_win64_console.exe" --headless -- --run-tests
  ```
- **Test Immutability:** Never weaken, skip, or delete assertions in `tests/test_suite_node.gd` to make a build pass.
- **Honest Verification Reporting:** Always distinguish between:
  1. Headless automated logic tests (`--run-tests`)
  2. Two-instance localhost UDP tests (`127.0.0.1`)
  3. Cross-device physical Wi-Fi LAN tests (Desktop + Android or 2 Android devices)
- **Secrets & Security:** Never commit `.keystore`, `.jks`, passwords, tokens, or local build outputs.
