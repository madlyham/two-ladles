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
- **FUSION ARTIST:** Grants +25% bonus score on Fusion recipes and 3+ component assemblies.
