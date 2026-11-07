# Development Roadmap — Two Ladles (`docs/DEVELOPMENT_ROADMAP.md`)

> [!IMPORTANT]
> **Engine Transition Notice:** Active development on the Godot prototype has officially **stopped**. Engineering is now 100% focused on the **Unity 2022.3.62f3 C#** codebase (`unity/TwoLadles/`). All milestone progression, art pipelines, builds, and tournament submissions are executed exclusively in Unity.

## Milestone Status Overview

| Milestone | Scope | Engine | Status |
| :--- | :--- | :--- | :--- |
| **Milestone 0: Project Foundation** | Initial game design, authentic recipes, and full documentation suite | Godot (Archived) | **COMPLETE & ARCHIVED** |
| **Milestone 1: Prototype Verification** | Initial mechanics validation & anti-exploit math prototyping | Godot (Archived) | **COMPLETE & ARCHIVED** |
| **Milestone 2: Unity Engine Port & Architecture** | Complete C# port of all stations, recipes, flow, character progression, and 14-suite test runner in Unity 2022.3.62f3 | Unity C# | **COMPLETE & VERIFIED** |
| **Milestone 3: Visuals, Collisions & Controls** | 2.5D kitchen builder, physical BoxColliders (zero phasing), hover tilt animation, throw mechanics, pixel-art tiles & appliances | Unity C# | **COMPLETE & VERIFIED** |
| **Milestone 4: Android Build & Network Playtesting** | Touchscreen virtual controls, Windows desktop host + Android client LAN playtesting, audio integration | Unity C# | **ACTIVE MILESTONE** |
| **Milestone 5: Competition Submission Build** | Final Android APK (`TwoLadles.apk`) & Windows executable (`TwoLadles.exe`) for Korea Game Week 2026 | Unity C# | Planned |

---

## How to Continue in the Next Antigravity Session

1. Read [`AGENTS.md`](../AGENTS.md) and [`README.md`](../README.md).
2. Open the official Unity project at `unity/TwoLadles/` in Unity `2022.3.62f3`.
3. Verify the automated C# test suite in `Assets/Tests/TwoLadlesTestSuite.cs`.
4. Proceed to **Milestone 4**: Touchscreen controls, Android APK compilation via `BuildScript.cs`, and LAN network verification.
