# TWO LADLES — Domain Glossary (`GLOSSARY.md`)

Canonical terms used across codebase, design documents, and automated tests.

| Term | Canonical Definition |
| :--- | :--- |
| **Authoritative Host** | Peer ID `1` running the `ENetMultiplayerPeer` server on UDP port `24567`. Owns all order timers, cooking states, Pass Counter handoff validations, Kitchen Flow, Rush Rhythm, and scoring. |
| **Client** | Peer ID `!= 1` connected to the Authoritative Host over local Wi-Fi (or `127.0.0.1` in dual-instance desktop testing). Sends movement vectors and interaction requests via RPC. |
| **Cross-Prep Interdependence** | Core spatial design rule: **Linh's West Zone** contains raw proteins/starches (`pork_raw`, `bulgogi_raw`, `tteok_raw`) that must be prepped and passed across the **Pass Counter** for **Jiwon's East Zone** appliances (`grill_pan`, `simmer_wok`); conversely, **Jiwon's East Zone** contains vegetables/aromatics/seafood (`daikon_carrot_raw`, `kimchi_raw`, `eomuk_raw`) and washed vessels that must be prepped and passed to **Linh's West Zone** for assembly. |
| **Pass Counter** | Physical 3-slot shared counter at the center of the kitchen (`x = 0.0`). Tracks `placed_by_peer` and `placed_at_ms` to detect coordinated partner handoffs. |
| **Coordinated Handoff** | Occurs when Player A places an ingredient required by an active unclaimed order onto the **Pass Counter** and Player B (`peer_id != placed_by_peer`) retrieves it within `4000 ms` (4 seconds). Awards **+15 Kitchen Flow** once per order. |
| **Kitchen Flow** | Authoritative team momentum meter (`0.0` to `100.0`). Increases via Coordinated Handoffs (`+15`) and serving dishes (`+12` Standard, `+20` Optimal); decreases on expired orders (`-15`) or burnt/wasted food (`-10`). |
| **Rush Rhythm** | Triggered immediately when **Kitchen Flow** reaches `100`. Resets Flow to `0`, activates a `15.0`-second bonus window with a `1.5x` multiplier on base order scores, and pauses all Flow generation until the 15s period expires. |
| **Prep Cadence** | Discrete 4-strike rhythm input on a `PrepStation` (`IDEAL_CADENCE_SEC = 0.45s`). Graded per strike as `PERFECT`, `GOOD`, or `MISS`. Misses never block completion. |
| **Cooking State Machine** | 5-phase appliance lifecycle: `RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT`. |
| **Specialization** | Pre-shift passive perk (`PREP MASTER`, `FLAME KEEPER`, `MARKET RUNNER`, `FUSION ARTIST`) selectable by either character in the lobby. Both players may equip the same specialization. |
