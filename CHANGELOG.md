# Changelog

All notable changes to **Two Ladles** are documented in this file.

## [0.2.0] - 2026-11-06 (Milestones 0, 1 & 2 Vertical Slice)

### Added
- **Milestone 0 (Foundation & CI/CD):**
  - Configured Godot `4.7.2-stable` project (`project.godot`), `mobile` renderer, landscape `1280x720` layout, and export presets (`export_presets.cfg`) for Windows, Linux, and Android (`INTERNET` and Wi-Fi permissions enabled).
  - Created `.github/workflows/ci.yml` (automated headless import, 62-assertion test suite, and Windows/Linux build artifacts on push/PR) and `.github/workflows/android-build.yml` (manual + `v*` tag cloud APK build).
  - Added full documentation suite (`README.md`, `AGENTS.md`, `GLOSSARY.md`, and `docs/*.md`).
- **Milestone 1 (LAN Multiplayer & Pre-Shift Lobby):**
  - Implemented authoritative `ENetMultiplayerPeer` host/client networking on UDP port `24567` (`scripts/autoload/network_manager.gd`).
  - Added pre-shift lobby supporting character exclusivity (`Linh` vs `Jiwon`), 4 selectable specializations (`PREP MASTER`, `FLAME KEEPER`, `MARKET RUNNER`, `FUSION ARTIST`), automatic readiness reset on loadout change, and safe disconnect handling.
  - Built 2.5D orthographic kitchen scene (`scripts/kitchen/kitchen_controller.gd`) with simultaneous dual-chef movement, desktop keyboard controls (`WASD`/Arrows + `E`/`Space`/`F`), and Android touch D-pad + context buttons.
- **Milestone 2 (Complete Gameplay Loop & Cross-Prep Interdependence):**
  - Implemented 5 distinct station mechanics: discrete 4-strike rhythm `PrepStation`, 5-state timed `CookingStation` (`RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT`), vessel-validated `AssemblyStation`, authoritative `ServingStation`, and 4-stroke `WashingStation` with finite 4-plate pool.
  - Implemented 4 data-driven starter recipes (*Phở Bò*, *Bánh Mì Thịt*, *Tteokbokki*, *Bulgogi Bánh Mì*) with asymmetric West/East **Cross-Prep Interdependence** between Linh and Jiwon across the shared `PassCounter`.
  - Implemented anti-exploit Pass Counter handoff detection (`+15` Kitchen Flow within 4s, max once per order) and **Rush Rhythm** (`100` Flow resets to `0`, activates `15s` `1.5x` base score multiplier with paused Flow generation).
  - Added 62-assertion headless automated test suite (`tests/test_suite_node.gd`) and 2-instance localhost UDP smoke test (`--lan-smoke-host` / `--lan-smoke-client`).
