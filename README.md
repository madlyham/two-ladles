# Two Ladles

**Two Ladles** is a two-player cooking game about a Vietnamese chef (**Linh**) and a Korean chef (**Jiwon**) running one kitchen together. Each chef works on their own side of the kitchen, and neither can finish a dish alone — you have to pass ingredients, cook, and serve as a team before customers run out of patience.

It is being built for **Korea Game Week 2026** in **Unity 2022.3.62f3** (C#).

> [!NOTE]
> An early prototype was made in Godot. That prototype has been retired and removed from this repository (it remains only in old git history). Everything here is the Unity game.

---

## How to play

### Playing together on one Wi-Fi network (co-op)
1. Both devices must be on the **same Wi-Fi network**.
2. On the laptop, open `TwoLadles.exe` and choose **HOST A KITCHEN**. Type your chef name and a kitchen name.
3. On the phone (or second computer), open the game and choose **JOIN KITCHEN (LAN)**. Tap your partner's kitchen in the list, or type the host's IP address and press **CONNECT**.
4. In the room, the host picks **Day 1–5** or **Endless Survival**. Both players press **READY**, then the host presses **START**.
5. **LEAVE** returns you to the main menu.

If the phone cannot find the kitchen, see the [LAN crossplay checklist](docs/NETWORKING.md#lan-crossplay-test-checklist) (usually a Windows firewall setting).

### Playing alone (solo)
Open **SETTINGS & OPTIONS**, switch the mode from **LAN CO-OP** to solo, and pick which chef you want to play. The other chef is controlled by the computer.

### Learning the basics
Choose **★ TUTORIAL ★** on the main menu for the Kitchen Academy, a short guided lesson that walks you through one Tteokbokki order step by step.

### Other menu options
- **★ UPGRADES ★** — spend coins earned from shifts on permanent kitchen upgrades (faster knives, speed boots, faster burners, chef tiers, and more).
- **Settings** — language (English, Tiếng Việt, 한국어), solo/co-op, chef choice.
- **2-BOT TEST RUN** (testers only) — a faint button at the bottom of the **Host** popup that lets two computer chefs play automatically. Hidden on phones.

## The menu (5 days)

Each day adds a new dish to the menu:

| Day | New dish |
|---|---|
| 1 | Tteokbokki (Korean spicy rice cakes) |
| 2 | Bột Chiên (Vietnamese fried rice-flour cake with egg) |
| 3 | Korean BBQ on Rice |
| 4 | Vietnamese Broken Rice (Cơm Tấm Sườn Trứng) |
| 5 | Bulgogi Bánh Mì — the Vietnam–Korea fusion finale (3-minute shift) |

Each day has **3 waves** of customers. Late or missed orders cost **reputation**; if reputation reaches zero, the shift is lost. Finishing all waves wins the day and unlocks the next one. **Endless Survival** mixes all five dishes and keeps going until three orders are missed.

Before each day, a short "phone chat" story between Linh and Jiwon introduces the day's dish (you can skip it).

## Controls

| Action | Keyboard / mouse | Phone |
|---|---|---|
| Move | WASD / arrow keys | On-screen joystick |
| Pick up / put down / use station | `E` or `Space` | Interact button |
| Chop (hold-style action) | `F` | Secondary button |
| Throw held item to partner or counter | `Q` or right-click | Throw button |
| Switch chef (solo only) | `C` | — |
| Hire part-time helper (Jiwon) | `H` | — |
| Open/close recipe book | `R` | — |
| Show/hide phone controls on desktop | `F3` | — |
| Bot debug overlay (testing) | `F1` | — |
| Menu / skip story | `Esc` | — |

Phone controls appear automatically on phones and tablets and are hidden on desktop.

## Downloads

After building in Unity (see below), the game files are created here:
- Windows: `unity/TwoLadles/Two Ladles/Builds/Windows/TwoLadles.exe`
- Android: `unity/TwoLadles/Two Ladles/Builds/Android/TwoLadles.apk`

Download copies for the project web page are kept at [`web/downloads/TwoLadles-Windows-x64.zip`](web/downloads/TwoLadles-Windows-x64.zip) and [`web/downloads/TwoLadles.apk`](web/downloads/TwoLadles.apk).

## For developers

### Open the project
Open the folder `unity/TwoLadles/Two Ladles/` in **Unity Hub** with Unity **2022.3.62f3**. The main scene is `Assets/Scenes/MainKitchen.unity`.

### Build and test (Unity menu bar → **Two Ladles**)
- **Build Standalone Windows EXE** → `Builds/Windows/TwoLadles.exe`
- **Build Android Standalone APK** → `Builds/Android/TwoLadles.apk` (needs the Unity Android module)
- **Run Automated Test Suite** → runs all 20 test suites (383 assertions) and prints `RESULTS: X / Y Passed`
- **🤖 Run Two-Bot QA Test** / **QA Test From Day → Day 1..5** → automatic two-bot playthrough

### Project layout
```
two-ladles/
├── CHANGELOG.md
├── docs/                     # Design, architecture, networking, testing, build guides
├── web/                      # Project web page + downloads/ zip mirror
└── unity/TwoLadles/Two Ladles/
    ├── Assets/
    │   ├── Editor/           # BuildScript.cs, TwoLadlesEditorMenu.cs
    │   ├── Resources/        # Sprites, fonts loaded at runtime
    │   ├── Scenes/           # MainKitchen.unity
    │   ├── Scripts/
    │   │   ├── AI/           # Bot partner, A* navigation, 2-bot QA, bot logs/HUD
    │   │   ├── Core/         # GameData, ShiftManager, ProgressionManager, Audio, Localization
    │   │   ├── Data/         # Recipe, campaign/wave and character data
    │   │   ├── IO/           # SaveSystem
    │   │   ├── Kitchen/      # KitchenController, PartTimeWorker
    │   │   ├── Networking/   # NetworkManager (LAN UDP)
    │   │   ├── Player/       # PlayerController
    │   │   ├── Stations/     # Prep, cooking, assembly, pass, serving, crates, trash
    │   │   ├── Story/        # StoryScript (day-start phone chats)
    │   │   ├── UI/           # LobbyUI, KitchenHUD, PhoneStoryUI, QuickSelectWheelUI
    │   │   └── Visuals/      # DayNightCycleManager
    │   ├── Sprites/
    │   └── Tests/            # TwoLadlesTestSuite.cs (20 suites), TwoLadlesTestRunner.cs
    └── Builds/               # Build output (Windows / Android)
```

### Documentation
- [Game Design Document](docs/GAME_DESIGN_DOCUMENT.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Networking & LAN checklist](docs/NETWORKING.md)
- [Testing](docs/TESTING.md)
- [Android build](docs/ANDROID_BUILD.md)
- [Art pipeline](docs/ART_PIPELINE.md)
- [Roadmap](docs/DEVELOPMENT_ROADMAP.md)
- [Changelog](CHANGELOG.md) · [Asset licenses](docs/ASSET_LICENSES.md)
