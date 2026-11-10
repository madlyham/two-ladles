# Development Roadmap — Two Ladles (`docs/DEVELOPMENT_ROADMAP.md`)

> [!IMPORTANT]
> All active work is in the **Unity 2022.3.62f3 C#** project at `unity/TwoLadles/Two Ladles/`. The Godot prototype has been retired and removed.

## Milestone Status

| Milestone | Scope | Engine | Status |
| :--- | :--- | :--- | :--- |
| **M0: Project Foundation** | Game design, recipes, documentation | Godot (retired) | Complete |
| **M1: Prototype Verification** | Mechanics and anti-exploit math prototyping | Godot (retired) | Complete |
| **M2: Unity Port** (2026-11-07) | C# port of stations, recipes, Flow, progression, automated test suite | Unity | Complete |
| **M3: Visuals, Economy & LAN Discovery** (2026-11-08) | 2.5D orthographic kitchen, colliders, hover/throw, tutorial, coins/upgrade shop, reputation & waves, LAN room discovery | Unity | Complete |
| **M4: Content, Localization & AI** (2026-11-09) | 5-day menu, EN/VI/KO localization, solo AI partner (A* navigation), Two-Bot QA autoplay, part-time workers | Unity | Complete |
| **M5: Lobby & Presentation** (2026-11-10) | Lobby revamp (Host/Join popups, room panel), story phone, day/night cycle, procedural audio, Leave-button fix | Unity | Complete (not yet playtested on real devices) |
| **M6: Device Playtesting** | Laptop host + Android phone over Wi-Fi; Android build verification | Unity | **Active** |
| **M7: Competition Submission** | Final `TwoLadles.exe` and `TwoLadles.apk` for Korea Game Week 2026 | Unity | Planned |

## Next Steps (in order)
1. Run the automated test suite (**Two Ladles → Run Automated Test Suite**) and confirm all 19 suites pass.
2. Build the Android APK and install it on a real phone ([ANDROID_BUILD.md](ANDROID_BUILD.md)).
3. Do a real laptop + phone LAN session using the [LAN crossplay test checklist](NETWORKING.md#lan-crossplay-test-checklist).
4. Close the known networking gap: the host does not yet send order, score, or station state to the client during a shift (see [NETWORKING.md](NETWORKING.md)).
5. Build final Windows and Android packages and refresh the web download zip (`web/downloads/TwoLadles-Windows-x64.zip`).

## Starting a New Session
1. Read [`README.md`](../README.md) and [`ARCHITECTURE.md`](ARCHITECTURE.md).
2. Open `unity/TwoLadles/Two Ladles` in Unity `2022.3.62f3`.
3. Run the test suite before changing code ([TESTING.md](TESTING.md)).
