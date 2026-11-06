# Networking Architecture — Two Ladles

## 1. Transport & Topology

- **API:** Godot 4 `ENetMultiplayerPeer` + High-Level Multiplayer API (`@rpc`).
- **Protocol:** UDP on configurable port `24567` (`NetworkManager.DEFAULT_PORT`).
- **Topology:** 1 Authoritative Host (`peer_id == 1`) + 1 Client (`MAX_CLIENTS = 1`, total 2 human players).

## 2. Host Authority & Client Responsibilities

| System | Authority | Transfer Mode | Notes |
| :--- | :--- | :--- | :--- |
| **Lobby Selection & Ready** | Host (`peer_id == 1`) | `reliable` | Validates character exclusivity (`linh` vs `jiwon`) and resets `ready = false` on any loadout change. |
| **Player Movement** | Client input → Host clamp | `unreliable_ordered` | Client sends position updates; Host clamps to kitchen bounds (`±8.8x, ±5.2z`) and broadcasts. |
| **Station Interactions** | Host (`peer_id == 1`) | `reliable` | Client sends `_request_station_interaction(station_id, is_secondary)`. Host checks distance (`<= 2.6m`) and executes state transition. |
| **Cooking Timers** | Host (`peer_id == 1`) | `reliable` | Host ticks `CookingStation.tick_cooking(delta)` and broadcasts state transitions. |
| **Pass Counter Handoffs** | Host (`peer_id == 1`) | `reliable` | Host checks `placed_by_peer != retrieved_by_peer`, `elapsed_ms <= 4000`, and `claimed_handoff_orders`. |
| **Orders, Score & Flow** | Host (`peer_id == 1`) | `reliable` | Host ticks order patience and Rush Rhythm (`15s`) and syncs every `0.25s` and on state-changing events. |
| **Worker Hiring ("Call in the Crew")** | Host (`peer_id == 1`) | `reliable` | Client requests `_rpc_request_hire_worker(role_id)`. Host validates coins, role cooldown, and Jiwon tier slots, then broadcasts `_rpc_sync_spawn_worker(wid, role_id, st_id)`. |

## 3. Disconnection & Error Recovery

- **Duplicate / Third Peer Join:** Automatically disconnected in `NetworkManager._on_peer_connected` when `players.size() >= 2`.
- **Client Disconnects Mid-Shift:** Host catches `peer_disconnected`, resets lobby readiness, clears the active kitchen instance cleanly, and returns to the lobby screen with a clear notification.
- **Host Disconnects Mid-Shift:** Client catches `server_disconnected`, closes the ENet peer, and returns safely to the main lobby screen.
