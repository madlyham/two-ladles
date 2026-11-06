# Development Roadmap — Two Ladles (`docs/DEVELOPMENT_ROADMAP.md`)

## Milestone Status Overview

| Milestone | Scope | Status |
| :--- | :--- | :--- |
| **Milestone 0: Project Foundation** | Godot 4.7.2 toolchain, export templates, Git repo, `.github/workflows/ci.yml` & `android-build.yml`, full documentation suite | **COMPLETE & VERIFIED** |
| **Milestone 1: Multiplayer Kitchen Prototype** | Authoritative ENet UDP `:24567` LAN host/client, pre-shift lobby with character exclusivity & specializations, 2.5D kitchen, 5 station types | **COMPLETE & VERIFIED** |
| **Milestone 2: Complete Gameplay Loop** | 4 authentic recipes, Linh ↔ Jiwon Cross-Prep layout, Pass Counter 4s handoffs (+15 Flow, anti-exploit), Rush Rhythm (15s, 1.5x), finite plate washing, end-of-shift summary & retry | **COMPLETE & VERIFIED** |
| **Milestone 3: Progression & 8 Culinary Stages** | Persistent reputation, currency, appliance upgrades, additional specializations, and stage layouts 1–8 | **NEXT MILESTONE** |
| **Milestone 4: Visual & Audio Polish** | Custom low-poly rigged meshes for Linh & Jiwon, food models, sizzle/chop particles, dynamic Rush Rhythm soundtrack, interactive tutorial | Planned |
| **Milestone 5: Competition Build** | Mid-range Android profiling, LAN packet-loss stress testing, balancing, final release APK packaging | Planned |

---

## How to Continue in the Next Antigravity Session

1. Read `AGENTS.md` and `GLOSSARY.md`.
2. Run the headless automated test suite first to confirm baseline health:
   ```powershell
   & "$env:LOCALAPPDATA\Programs\two-ladles-tools\godot\Godot_v4.7.2-stable_win64_console.exe" --headless -- --run-tests
   ```
3. Proceed to **Milestone 3**: add persistent save data (`user://progression.json`), stage selection across the 8 restaurant tiers, and appliance upgrade modifiers hooked into `GameData.appliances`.
