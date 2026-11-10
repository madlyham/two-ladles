# Testing Guide — Two Ladles (`docs/TESTING.md`)

> [!IMPORTANT]
> All active testing targets the **Unity 2022.3.62f3** project at `unity/TwoLadles/Two Ladles/`. The retired Godot prototype and its headless test runner have been removed from the repository.

## 1. Automated C# Test Suite

File: [`Assets/Tests/TwoLadlesTestSuite.cs`](../unity/TwoLadles/Two%20Ladles/Assets/Tests/TwoLadlesTestSuite.cs)

### What it covers (19 suites, 303 assertions as of 2026-11-10)
1. Recipes & Cross-Prep Architecture
2. Prep Station Rhythm Cadence (0.45 s sweet spot)
3. Cooking State Machine & Phở Separation
4. Assembly Station Rules & Vessel Rejections
5. Pass Counter Handoff & Anti-Exploit Window (4000 ms)
6. Kitchen Flow & Rush Rhythm (15 s, 1.5x)
7. No Washing Chore (unlimited plates; serving never makes dirty plates)
8. Lobby Exclusivity & Ready Resets
9. Permanent Career Progression & Kitchen Upgrades
10. Linh Execution Specialist Tiers
11. Jiwon Delegation Specialist Tiers (Part-Timer Crew)
12. 5-Level Campaign Progression & Unlock Gates
13. Endless Rush Survival Mode & High Scores
14. Holding Counters, Hover Tilt Animation & Throw Mechanics
15. Wave Progression, Reputation, Gold Economy, Shop & Level Replay
16. Solo Mode & Deterministic AI Partner
17. Two-Bot QA Autoplay Manager & Reporting
18. Story Phone Script & Day-Night Lighting
19. LAN Order Sync, Recipe Timing & Bot Safety

> [!IMPORTANT]
> **Test immutability:** never weaken, skip, comment out, or delete assertions to make a build pass. Fix the code instead. Tests change only when requirements change and the change is approved.

### Run from the Unity Editor
1. Open `unity/TwoLadles/Two Ladles` in Unity Hub (Unity 2022.3.62f3).
2. Use one of these menu items:
   - **Two Ladles → Run Automated Test Suite**
   - **Two Ladles → Run All Tests**
3. Check the Console. The summary line looks like `RESULTS: X / Y Passed`. Any failure is logged as an error.

### Run from the command line (batch mode)
Open PowerShell in the repository folder. This only reads the project and writes logs; it does not change game files.
```powershell
& "C:\Users\madly\AppData\Local\Programs\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -nographics -projectPath "unity/TwoLadles/Two Ladles" -executeMethod BuildScript.RunTestSuite -logFile test.log
```
(Adjust the Unity path if your editor is installed elsewhere.) `BuildScript.RunTestSuite` exits with code `0` when every test passes and `1` when any test fails. Read `test.log` for `ALL N TESTS PASSED CLEANLY!` or `TEST SUITE FAILED`.

## 2. Two-Bot Autoplay QA

`TwoBotQAManager` lets two AI chefs play the campaign by themselves to catch stuck states, softlocks, and balance problems.

### From the Editor
- **Two Ladles → 🤖 Run Two-Bot QA Test** — run the bot QA from the default day.
- **Two Ladles → QA Test From Day → Day 1…Day 5** — choose the starting day (stored in EditorPrefs key `TwoLadles_QADay`).
- In a desktop build, the **🤖 2-BOT TEST RUN** button sits at the bottom of the **Settings** popup (20% opacity, hidden on mobile). It runs at 2x speed with a random seed. Two buttons next to it set up the run:
  - **TEST DAY: ALL (1-5)** — click to cycle All → Day 1 → … → Day 5. A single day tests only that day.
  - **ALL UPGRADES: ON/OFF** (default ON) — gives both bots every kitchen upgrade and Tier 3, so part-timers and the third arm are tested together.
  - The run uses a temporary sandbox; your real save (coins, upgrades, unlocked days) is restored when the run ends and is never overwritten.
- Bot rule: a wrong or unneeded baguette / bánh mì goes in the trash; other unneeded food is parked on a holding table.

### From the command line
```powershell
& "C:\Users\madly\AppData\Local\Programs\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "unity/TwoLadles/Two Ladles" -executeMethod BuildScript.RunAutoplayQABatch -autoplayQA -seed 42 -days 1-5 -timescale 4
```
Arguments (parsed in `TwoBotQAManager.ParseCommandLineArguments`):

| Argument | Meaning |
| --- | --- |
| `-autoplayQA` | Turn on QA mode (exits automatically when run in batch mode). |
| `-seed <N>` | Random seed so runs can be repeated. |
| `-days <N>` or `-days <A>-<B>` | `N` = Days 1 to N; `A-B` = a range. |
| `-timescale <x>` | Simulation speed, clamped to 0.5–10. |

### Output
- QA reports: `unity/qa_reports/` (path resolved as `Application.dataPath/../../../qa_reports`).
- Bot telemetry log: `Logs/bot_qa.log` (written by `BotLogger`).
- In-game: press **F1** to toggle the bot debug / TAS HUD (off by default).

> [!NOTE]
> The batch QA path has not been re-verified as part of this documentation update. If a batch run never finishes, run QA from the Editor menu instead and report the log.

## 3. Manual Multiplayer Tests

### Same computer (two windows)
1. Build the Windows player (**Two Ladles → Build Standalone Windows EXE**) → `Builds/Windows/TwoLadles.exe`.
2. Start it twice. In window 1 choose **Host** (enter a name and room name). In window 2 choose **Join** and pick the room from the LAN list, or type `127.0.0.1`.
3. Pick a day in the room panel, press **Ready** in both windows, then **Start** on the host.

### Laptop + Android phone over Wi-Fi
Follow the [LAN crossplay test checklist](NETWORKING.md#lan-crossplay-test-checklist).

### Desktop-only helpers
- **F3** toggles on-screen mobile controls on desktop (they show automatically on phones).
- **F9** opens the story debug panel.

