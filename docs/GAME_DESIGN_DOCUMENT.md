# Game Design Document — Two Ladles

> [!NOTE]
> This document describes the **Unity 2022.3.62f3** build at `unity/TwoLadles/Two Ladles/`. The retired Godot prototype is not described here. Numbers below are taken from `Assets/Scripts/Core/GameData.cs`, `CampaignLevelSpec.cs`, and `ShiftManager.cs`.

## 1. Core Identity & Narrative

- **Title:** Two Ladles: A Vietnam–Korea Kitchen Story
- **Genre:** 2-player local Wi-Fi cooperative cooking and time-management game (with a solo mode that uses an AI partner).
- **Core Philosophy:** *"Easy to play badly. Difficult to play efficiently. Extremely satisfying to master."*

### Protagonists
1. **Linh (Vietnamese, former software programmer):** Left tech deadlines to reopen a neglected family stall in rural Vietnam. Brings systems thinking and workflow optimization.
2. **Jiwon (Korean, former corporate executive):** Left corporate burnout for a sabbatical in Vietnam and partnered with Linh to build a Vietnam–Korea culinary venture. Brings operational planning and service management.

Both chefs can perform every kitchen action. The kitchen is split into a **West zone (Linh)** and an **East zone (Jiwon)** by a **center divider wall**; ingredients cross between the zones through the **Pass Counter** or by throwing. Some supplies (flour, eggs, rice, fridges, shelves) are available on both sides.

### Story & Presentation
- **Story phone (`PhoneStoryUI` + `StoryScript`):** Before each campaign day, a chat-style phone conversation between the chefs plays. In co-op, both players must finish the story (`STORY_DONE` sync) before the shift begins.
- **Recipe book:** Opens after the story and can be reopened with `R` during a shift.
- **Day/night cycle (`DayNightCycleManager`):** Lighting shifts over the course of a shift.
- **Audio (`AudioManager`):** Procedurally generated sound effects and music.
- **Languages (`LocalizationManager`):** English, Vietnamese, Korean.

---

## 2. Five Distinct Station Mechanics

| Station | Interaction Mechanism | Failure / Edge Handling |
| :--- | :--- | :--- |
| **1. Preparation Counter** | Discrete 4-strike rhythm cadence (`0.45s` ideal interval). Graded `PERFECT`, `GOOD`, or `MISS`. | `MISS` never blocks completion (4 strikes always complete prep). `PERFECT` cadence upgrades quality and awards bonus points. |
| **2. Cooking Station** | 5-state timed heat machine: `RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT`. | Removing during `OPTIMAL` awards +20% score and extra Flow. `OVERCOOKED` reduces score. `BURNT` creates waste. |
| **3. Assembly Counter** | Combines components into a finished dish. | Rejects invalid combinations with feedback without destroying existing components. Vessel rules (plate vs bowl, Phở broth-last) apply to legacy recipes; the active 5-day dishes are vessel-less (`vessel = "none"`). |
| **4. Serving Counter** | Matches the dish against the active customer order queue. | Awards base score × multipliers + patience tip. |
| **5. No Washing** | Plates and bowls come free from the quick-select wheel. | Washing was cut on purpose to keep the game simple and friendly for new players. |

Additional props: **holding counters** (place any item), **trash bin**, and carried-item **throwing** (`Q` / right-click) toward the partner, a holding counter, a Pass Counter slot, or the trash.

---

## 3. Active 5-Day Menu

The dishes available on each day come from `GameData.GetDayAllowedRecipes`, which overrides the older `allowedRecipes` lists stored in `CampaignLevelSpec`. Each day adds one dish to the previous days, except Day 5, which focuses on the fusion dish only.

| Day | New Dish | Components | Base Points |
| :--- | :--- | :--- | :--- |
| **1** | **Tteokbokki** (Korean) | `tteok_portioned` + `gochujang` | 115 |
| **2** | **Bột Chiên** (Vietnamese fried rice-flour cake) | `cooked_flour` + `egg_raw` (stove) | 125 |
| **3** | **Korean BBQ Rice** | `bbq_beef` + `cooked_rice` (raw meat cut once → `korean_bbq_raw` → grill → `bbq_beef`) | 140 |
| **4** | **Cơm Tấm Sườn Trứng** (Vietnamese broken rice) | `bbq_beef` + `cooked_rice` + `egg_raw` | 155 |
| **5** | **Bulgogi Bánh Mì** — *Original Vietnam–Korea Fusion Specialty* (only dish on Day 5) | Baguette (flour + egg, cooked) + `pickled_veg` (radish/carrot) + `bulgogi_seared` (raw meat cut twice on Jiwon's side, then seared) | 180 |

- **Endless mode** uses all five dishes.
- **Tutorial** serves a single Tteokbokki order with a 600-second timer.
- **Legacy recipes** (`pho_bo`, `banh_mi_thit`, `kimchi_bokkeumbap`, and others) still exist in `GameData.recipes` and are covered by tests, but they are not part of the active day menu.

---

## 4. Kitchen Flow, Pass Counter & Rush Rhythm

- **Pass Counter handoff (+15 Flow):** When one chef places an ingredient needed by an active order on the Pass Counter and the **other** chef picks it up within **4000 ms**, the team earns **+15 Kitchen Flow**.
- **Anti-exploit rules:** self-retrieval awards `0` Flow; each order can grant the handoff bonus at most once; passing the same item back and forth never duplicates the bonus.
- **Other Flow sources:** serving an order +12 (+20 if cooked at optimal); an expired order −15.
- **Rush Rhythm:** reaching `100` Flow triggers **Rush Rhythm** for **15 seconds** with a **1.5x score multiplier**.

---

## 5. Pre-Shift Specializations

- **PREP MASTER:** Widens the Perfect cadence window by +25% and grants +15 bonus points on Perfect-prepped dishes.
- **FLAME KEEPER:** Extends the Optimal cooking window by +35%.
- **MARKET RUNNER:** Increases movement speed by +20%.
- **FUSION ARTIST:** +20% score and +10 Flow on fusion dishes (Bulgogi Bánh Mì).

---

## 6. Character Progression Tiers

### Linh — Execution Specialist
The mechanical third arm is visual and thematic; it does not act on its own.
- **Tier 1 — Ordinary Chef:** 1.0x task speed. No arm.
- **Tier 2 — Workshop Engineer (unlocks after Day 3):** Rustic brass/steel third arm. Prep strike interval ×0.92.
- **Tier 3 — Precision Engineer (unlocks after Day 5):** Glowing white-and-blue precision arm. Prep strike interval ×0.85.

### Jiwon — Delegation Specialist ("Call in the Crew")
- **Tier 1 — Burnt-out Executive:** 0 part-timer slots.
- **Tier 2 — Restaurant Manager (unlocks after Day 3):** 1 part-timer slot.
- **Tier 3 — Business Operator (unlocks after Day 5):** 2 part-timer slots.

#### Part-Time Workers (`PartTimeWorker`)
Hiring costs **20 coins** for a **20-second** contract with a **45-second** cooldown. Part-timers are **Prep Assistants** (automated chopping at the prep counter). One **Part-Timer [H]** button hires every free slot at once (1 slot at Tier 2, 2 slots at Tier 3); there is no washing chore and no separate dishwasher button. Hiring is host-authoritative: the host checks coins, cooldown, and slot capacity before spawning the worker.

---

## 7. Campaign (Days 1–5)

| Day | Title (Setting) | Duration | Waves | Order Patience |
| :--- | :--- | :--- | :--- | :--- |
| **1** | First Customer (Rural Vietnam) | 180 s | 3 waves | max(50, 72 − 3×day) s |
| **2** | Lunch Rush (Village Market) | 180 s | 3 waves | same formula |
| **3** | Night Market | 180 s | 3 waves | same formula |
| **4** | City Bistro (City Food Market) | 180 s | 3 waves | same formula |
| **5** | Grand Opening (Metropolitan Bistro) | 180 s | 3 waves (spawn interval 10 / 8.5 / 7.5 s; max concurrent 3 / 4 / 4) | 75 / 70 / 65 s |

- **Days 1–4 waves:** wave order counts are `2 + (day − 1)`, then +1, then +2; the spawn interval starts at `max(8.5, 16 − 1.4×day)` seconds and drops by 2 s and then 3.5 s in later waves.
- **Unlocks:** finishing Day 3 unlocks Tier 2 for both chefs; finishing Day 5 unlocks Tier 3. Days unlock in order and can be replayed.

### Reputation, Stars & Gold
- Each shift starts at full reputation. Every expired order costs **−25 reputation**; reaching **0** ends the shift in defeat.
- **Stars:** reputation ≥ 75 → 3★, ≥ 40 → 2★, otherwise 1★.
- **Gold reward:** `floor(200 × reputation / 100)` on victory, 0 on failure.

### Upgrade Shop & Saves
- New saves start with **150 coins**. Kitchen upgrades cost 100 / 100 / 150 / 250 / 500 / 150 / 200 / 250 coins.
- `SaveSystem` + `ProgressionManager` persist coins/gold, career score, unlocked days, chef tiers, and purchased upgrades.

---

## 8. Endless Rush Mode

- Selected as **Endless** in the lobby room panel. No time limit.
- **Spawn interval:** `max(6.5, 14 − t/30)` seconds, where `t` is elapsed time.
- **Concurrent orders:** 2, rising to 3 after 30 s, 4 after 60 s, and 5 after 120 s.
- **Failure:** the run ends after **3** failed (expired) orders.
- **High scores:** best score, orders served, and survival time are saved.

---

## 9. Solo Mode & AI

- **Solo mode:** chosen in Settings; the player picks a chef and a **bot partner** (`BotController`) plays the other chef, navigating with an A* path graph (`KitchenNavGraph`) and coordinating via `BotCommunicationHub`.
- **2-Bot QA (developer tool):** a faint (20% opacity) button in the Settings popup (hidden on mobile) runs two bots through the selected day up to Day 5 at 2x speed for automated playtesting (`TwoBotQAManager`). `F1` toggles the bot debug HUD.
