# Game Design Document — Two Ladles

## 1. Core Identity & Narrative

- **Title:** Two Ladles: A Vietnam–Korea Kitchen Story
- **Genre:** 2-Player Local Wi-Fi Cooperative Cooking & Time-Management Game
- **Core Philosophy:** *"Easy to play badly. Difficult to play efficiently. Extremely satisfying to master."*

### Protagonists
1. **Linh (Vietnamese, Former Software Programmer):** Left high-pressure tech deadlines to reopen a neglected family stall in rural Vietnam. Brings systems thinking and workflow optimization.
2. **Jiwon (Korean, Former Corporate Executive):** Left corporate burnout for a sabbatical in Vietnam and partnered with Linh to build a Vietnam–Korea culinary venture. Brings operational planning and service management.

Both characters can perform every kitchen action, while the kitchen's spatial topology creates natural teamwork: **Linh's West Zone prepares proteins and starches for Jiwon's East cooking stations, and Jiwon's East Zone prepares vegetables, slaw, fish cakes, and washed bowls/plates for Linh's West assembly stations.**

---

## 2. Five Distinct Station Mechanics

| Station | Interaction Mechanism | Failure / Edge Handling |
| :--- | :--- | :--- |
| **1. Preparation Counter** | Discrete 4-strike rhythm cadence (`0.45s` ideal interval). Graded `PERFECT`, `GOOD`, or `MISS`. | `MISS` grades never block completion (4 strikes always complete prep). `PERFECT` cadence upgrades quality and awards bonus points. |
| **2. Cooking Station** | 5-state timed heat machine: `RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT`. | Removing during `OPTIMAL` awards +20% score & +20 Flow. `OVERCOOKED` reduces score (-25%). `BURNT` creates waste (-10 Flow). |
| **3. Assembly Counter** | Vessel-first (`plate` or `bowl`) recipe combination. | Rejects invalid pairings with clear feedback without destroying existing components. Enforces *Phở Bò* broth-last rule. |
| **4. Serving Counter** | Matches dish against active customer order queue. | Awards base score × multipliers + patience tip, and spawns 1 dirty plate at the Return Tray. |
| **5. Washing Station** | Finite pool of 4 shared vessels; 4-stroke scrub cycle. | Prevents unlimited disposable plates while keeping scrubbing fast and forgiving. |

---

## 3. Authentic Starter Menu & Cross-Prep Chains

1. **Phở Bò (Traditional Vietnamese):**
   - Prep `beef_raw` → `beef_sliced`.
   - Blanch `noodles_raw` → `noodles_blanched` in the **Noodle Blancher** (separate from stockpot).
   - Maintain hot broth (`broth_hot`) in the **Dedicated Stockpot**.
   - Place clean `bowl` on Assembly Counter → add `noodles_blanched` and `beef_sliced` → pour `broth_hot` last.
2. **Bánh Mì Thịt (Traditional Vietnamese):**
   - Linh portions `pork_raw` → `pork_portioned` on West Prep Counter → passes via **Pass Counter** to Jiwon.
   - Jiwon grills `pork_portioned` → `pork_grilled` on East Grill Pan and preps `daikon_carrot_raw` → `pickled_veg` on East Prep Counter → passes to Linh.
   - Assemble on clean `plate` with `baguette` + `pork_grilled` + `pickled_veg`.
3. **Tteokbokki (Traditional Korean):**
   - Linh portions `tteok_raw` → `tteok_portioned` on West Prep Counter → passes via **Pass Counter** to Jiwon.
   - Jiwon preps `eomuk_raw` → `eomuk_prepared` on East Prep Counter and simmers `tteok_portioned` + `eomuk_prepared` + `gochujang_sauce` in the **Simmer Wok** → `tteokbokki_simmered`.
   - Plate in clean `bowl`.
4. **Bulgogi Bánh Mì (Original Vietnam–Korea Fusion Specialty):**
   - Explicitly labeled as an **Original Vietnam–Korea Fusion Specialty**.
   - Linh portions `bulgogi_raw` → `bulgogi_portioned` on West Prep Counter → passes via **Pass Counter** to Jiwon.
   - Jiwon sears `bulgogi_portioned` → `bulgogi_seared` on East Grill Pan and preps `kimchi_raw` → `kimchi_slaw` on East Prep Counter → passes to Linh.
   - Assemble on clean `plate` with `baguette` + `bulgogi_seared` + `kimchi_slaw`.

---

## 4. Kitchen Flow, Pass Counter & Rush Rhythm

- **Pass Counter Handoff (+15 Flow):** When Player A places an ingredient needed by an active order onto the central Pass Counter and Player B retrieves it within **4 seconds**, the team earns **+15 Kitchen Flow**.
- **Anti-Exploit Rules:**
  - Self-retrieval (Player A placing and retrieving their own item) awards `0` Flow.
  - Each customer order can grant the handoff bonus at most once.
  - Passing the same item back and forth never generates duplicate bonuses.
- **Rush Rhythm:**
  - Reaching `100` Flow resets the Flow meter to `0` and triggers **Rush Rhythm** for **15 seconds**.
  - All orders served during Rush Rhythm receive a **1.5x base score multiplier**.
  - Flow generation is paused while Rush Rhythm is active and resumes automatically afterward.

---

## 5. Pre-Shift Specializations

- **PREP MASTER:** Widens Perfect cadence window by +25% and grants +15 bonus points on Perfect-prepped dishes.
- **FLAME KEEPER:** Extends Optimal cooking window duration by +35% across all appliances.
- **MARKET RUNNER:** Increases movement speed by +20%.

---

## 6. Character Signature Abilities & Progression Tiers

Rather than interchangeable cosmetic skins, Linh and Jiwon possess distinct thematic progression identities:

### Linh — Execution Specialist (Automation & Precision Engineering)
Linh optimizes kitchen throughput through mechanical cadence mastery and unlockable assistant arms. The arm is purely visual and thematic (no autonomous bot AI):
- **Tier 1 — Ordinary Chef:** Standard movement and task cadence (1.0x task speed multiplier). No robotic arm visible.
- **Tier 2 — Workshop Engineer (Unlocks after Level 3):** Unlocks a visible rustic brass/steel mechanical third arm mounted to her apron/back. Reduces rhythm prep strike intervals to 0.92x (~8% faster cadence).
- **Tier 3 — Precision Engineer (Unlocks after Level 5):** Replaces the rustic arm with a sleek, white-and-blue glowing precision robotic arm with pulsing emissive lines. Further reduces prep strike intervals to 0.85x (~15% faster cadence; 0.38s ideal intervals).

### Jiwon — Delegation & Multitasking Specialist ("Call in the Crew")
Jiwon leverages executive delegation to hire temporary, predictable part-time contract workers using restaurant coins.
- **Tier 1 — Burnt-out Executive:** 0 part-timer slots available. Casual apron attire. Must work stations by hand.
- **Tier 2 — Restaurant Manager (Unlocks after Level 3):** 1 simultaneous part-timer slot. Crisp rolled-sleeve dress shirt. Can spend 20 coins to hire a worker for 20 seconds (45s agency cooldown).
- **Tier 3 — Business Operator (Unlocks after Level 5):** 2 simultaneous part-timer slots. Formal business suit with executive styling. Can maintain 2 active workers concurrently.

#### Part-Time Worker Roles & State Machine
1. **Prep Assistant:** Walks into the kitchen, navigates to the East preparation counter, performs automated rhythm chopping strikes at 1.1s intervals, and departs when the 20s contract expires.
2. **Dishwasher:** Walks into the kitchen, navigates to the washing station, performs automated plate scrubbing strokes at 1.2s intervals, and departs when the 20s contract expires.

All hiring requests are host-authoritative: client requests validate coin balance, role cooldowns, and active capacity before deducting funds and spawning the worker over LAN.

---

## 7. Handcrafted 5-Level Campaign Progression

The campaign guides players from a humble rural stall through urban expansion:

| Level | Title & Setting | Shift Duration | Target 1★ / 2★ / 3★ | Allowed Recipes | Concurrent / Interval | Unlocks & Narrative Context |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | **First Customer** (Rural Food Stand) | 180s | 150 / 300 / 500 | Phở Bò, Bánh Mì Thịt | 2 orders / 16.0s | Family stall reopening. Core movement and cross-prep basics. |
| **2** | **Lunch Rush** (Village Market) | 180s | 220 / 450 / 650 | Phở Bò, Bánh Mì Thịt | 3 orders / 13.0s | Higher ticket volume. Finite plate stack and sink washing introduced. |
| **3** | **Night Market** (Night Market Stall) | 180s | 300 / 600 / 850 | Phở, Bánh Mì, Tteokbokki | 3 orders / 11.0s | Introduces Korean Tteokbokki. **Unlocks Tier 2 for Linh & Jiwon!** |
| **4** | **Fusion Special** (Street Food Hub) | 180s | 400 / 750 / 1100 | All 4 Recipes | 4 orders / 10.0s | Introduces Bulgogi Bánh Mì fusion dish. Intense cross-kitchen handoffs. |
| **5** | **Grand Opening** (Modern Fusion Restaurant) | 180s | 550 / 950 / 1400 | All 4 Recipes | 4 orders / 8.5s | Culmination of the journey. **Unlocks Tier 3 for Linh & Jiwon!** |

---

## 8. Endless Rush Mode

- **Continuous Escalation:** Players toggle Endless Mode in the multiplayer lobby. Shift duration is unlimited.
- **Dynamic Difficulty:** Order spawn interval scales downward from 14s to 6.5s as shift duration increases; concurrent order capacity scales from 2 to 4.
- **Failure Condition:** Missing or letting 3 customer orders expire triggers an immediate shift conclusion.
- **High Score Tracking:** Authoritatively records personal best score, orders served, and survival duration in persistent storage.
- **FUSION ARTIST:** Grants +25% bonus score on Fusion recipes and 3+ component assemblies.
