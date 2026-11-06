# TWO LADLES
### A Vietnam–Korea Kitchen Story

**Two Ladles** is a 2-player local Wi-Fi (LAN) cooperative cooking game built in **Godot Engine 4.7.2-stable** (typed GDScript, 2.5D stylized 3D kitchen, Android-first + Windows/Linux desktop builds).

Two players (**Linh**, a Vietnamese former software programmer, and **Jiwon**, a Korean former business executive) operate a high-pressure Vietnam–Korea kitchen together over a local network.

> **Core Design Philosophy:** *"Easy to play badly. Difficult to play efficiently. Extremely satisfying to master."*

---

## Key Features (Milestones 0–3 Vertical Slice)

- **Mandatory 2-Player Local Network Co-Op:** Built on Godot's `ENetMultiplayerPeer` (UDP port `24567`). One device hosts, the second joins via LAN IPv4 (or `127.0.0.1` for 2-window desktop testing).
- **Character Signature Abilities & Progression Tiers:**
  - **Linh (Execution Specialist):** Tiers 1–3 speed multipliers (1.0x, 0.92x, 0.85x; up to 0.38s cadence) with visual mechanical third arm evolving from rustic brass/steel to futuristic glowing precision white-and-blue.
  - **Jiwon (Multitasking & Delegation Specialist):** "Call in the Crew" ability allowing Jiwon to spend restaurant coins to hire temporary part-time helpers (`prep_assistant`, `dishwasher`) with automated chopping and scrubbing behaviors.
- **5 Handcrafted Campaign Levels:**
  - *First Customer* (Rural Food Stand) → *Lunch Rush* (Village Market) → *Night Market* (Tteokbokki introduced; Tier 2 unlocked) → *Fusion Special* (Bulgogi Bánh Mì introduced) → *Grand Opening* (All 4 recipes; Tier 3 unlocked).
- **Endless Rush Mode:**
  - Dynamic difficulty scaling with shrinking intervals and expanding order concurrency; failure upon 3 missed orders; persistent personal best tracking.
- **Persistent Progression & Kitchen Upgrades:**
  - Restaurant vault coins, 1–3 star ratings, and kitchen appliance upgrades (e.g. Sharp Santoku Knives, Double-Walled Stockpot) saved persistently.
- **Cross-Kitchen Partner Interdependence:**
  - **Linh's Side (West Zone):** Houses raw meats & starches (*Pork Belly*, *Marinated Bulgogi*, *Rice Cakes*, *Beef*, *Dry Rice Noodles*, *Baguettes*), the West Prep Counter, the **Dedicated Phở Stockpot**, the **Noodle Blancher**, Assembly West, and the Serving Window.
  - **Jiwon's Side (East Zone):** Houses vegetables, sauces & fish cakes (*Daikon & Carrot*, *Aged Kimchi*, *Fish Cakes*, *Gochujang*), the East Prep Counter, the **Cast-Iron Grill/Sear Pan**, the **Gochujang Simmer Wok**, Assembly East, and the **Washing Sink & Dish Rack**.
  - **Mutual Reliance:** Linh preps the meats and rice cakes that Jiwon grills and simmers on the East stoves; Jiwon preps the pickled vegetables, kimchi slaw, and clean bowls/plates that Linh needs to assemble dishes on the West side.
- **5 Distinct Station Mechanics (No Generic Progress Bars):**
  1. **Preparation Counter:** Discrete 4-strike rhythm cadence (`PERFECT` / `GOOD` / `MISS` — misses never block completion).
  2. **Cooking Station:** Timed heat-window state machine (`RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT`).
  3. **Assembly Counter:** Recipe-driven vessel combination (`plate` or `bowl` required first; *Phở Bò* enforces adding blanched noodles and sliced beef before pouring hot stockpot broth).
  4. **Serving Counter:** Authoritative order queue matching, patience tip calculation, and dirty plate generation.
  5. **Washing Station:** Finite 4-plate economy with a forgiving 4-stroke scrub cycle.
- **Pass Counter Handoffs, Kitchen Flow & Rush Rhythm:**
  - Passing an active order's ingredient across the central **Pass Counter** to your partner within **4 seconds** grants **+15 Kitchen Flow** (anti-exploit protected: max once per order, no self-handoff).
  - Reaching **100 Kitchen Flow** resets Flow to `0` and triggers **Rush Rhythm** for **15 seconds** (`1.5x` base score multiplier on served orders, Flow generation paused during bonus).
- **Pre-Shift Lobby with Character Exclusivity & Specializations:**
  - Choose **Linh** or **Jiwon** (mutually exclusive) and equip any of the 4 specializations (**PREP MASTER**, **FLAME KEEPER**, **MARKET RUNNER**, **FUSION ARTIST**).

---

## Quick Start — How to Launch & Test

### 1. Run Automated Headless Tests
```powershell
& "$env:LOCALAPPDATA\Programs\two-ladles-tools\godot\Godot_v4.7.2-stable_win64_console.exe" --headless -- --run-tests
```

### 2. Launch Two-Player Localhost Test on One Computer
Open PowerShell in the project folder and launch two independent windows:
```powershell
# Window 1 (Host - Linh):
Start-Process "build\windows\two-ladles.exe" -ArgumentList "--", "--auto-host"

# Window 2 (Client - Jiwon):
Start-Process "build\windows\two-ladles.exe" -ArgumentList "--", "--auto-join=127.0.0.1"
```
In both windows, click **TOGGLE READY**, then click **START SHIFT** on the Host window.

---

## Documentation Index

- [AGENTS.md](AGENTS.md) — Permanent engineering guidelines and constraints.
- [GLOSSARY.md](GLOSSARY.md) — Canonical domain terms.
- [CHANGELOG.md](CHANGELOG.md) — Milestone release history.
- [docs/GAME_DESIGN_DOCUMENT.md](docs/GAME_DESIGN_DOCUMENT.md) — Narrative, recipes, Kitchen Flow, and specialization specs.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — Autoloads, station class hierarchy, and data-driven design.
- [docs/NETWORKING.md](docs/NETWORKING.md) — Authoritative host model, RPC catalog, and anti-exploit validation.
- [docs/TESTING.md](docs/TESTING.md) — Step-by-step non-technical guide for Headless, Same-PC, Desktop+Android Wi-Fi, and Dual-Android testing.
- [docs/ANDROID_BUILD.md](docs/ANDROID_BUILD.md) — Cloud GitHub Actions APK pipeline and local Android SDK setup guide.
- [docs/ART_PIPELINE.md](docs/ART_PIPELINE.md) — 2.5D visual style guide, proportions, palette, and asset license rules.
- [docs/DEVELOPMENT_ROADMAP.md](docs/DEVELOPMENT_ROADMAP.md) — Milestones 0–5 status and next steps.
