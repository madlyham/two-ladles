# Two Ladles: Full Project Report

**Game:** *Two Ladles: A Vietnam–Korea Kitchen Story*. It is a co-op cooking game for 2 players, built for **Korea Game Week 2026 (KGW2026)**.
**Current version:** **v1.4.10** (Android build code 150). It is live at https://madlyham.github.io/two-ladles/
**Engine:** Unity 2022.3.62f3, written in C#. Godot was used for the early prototype; it is now archived on the `godot` branch.
**Repository:** `two-ladles`, branch `main`. There are 67 commits from 2026-11-06 to 2026-11-11. The latest is `5be2a06`.

---

## 1. Numbers at a glance

| Item | Value |
|---|---|
| Game scripts | 37 C# files, about 17,600 lines |
| Test suite | 1 file (`TwoLadlesTestSuite.cs`, 1,527 lines), 20 suites, **375 / 375 assertions passing** |
| Platforms | Windows (`TwoLadles.exe`, zip) and Android (`TwoLadles.apk`) |
| Languages | English, Vietnamese, Korean |
| Campaign | Tutorial plus 5 story days, then Endless Rush mode |
| Network | Raw UDP. Port 24567 = gameplay, port 24568 = room discovery |
| Third-party art | 2 free itch.io packs. Everything else is original |

---

## 2. The game itself

### Story and characters
- **Linh** (Vietnamese chef, *Execution Specialist*) works the **West** side of the kitchen.
- **Jiwon** (Korean chef, *Delegation / Business Operator*) works the **East** side.
- Each chef has 3 art tiers (tier 1 to 3) that unlock through upgrades.
- **Story phone:** a dating-app-style phone screen tells the story between days (`PhoneStoryUI`, `StoryScript`). It shows Jiwon's portrait in a square frame. On v1.4.7 the head was made smaller and raised on mobile.
- **Day–night lighting** changes over each shift (`DayNightCycleManager`).

### 5-day menu
| Day | Dish | Vessel | Notes |
|---|---|---|---|
| 0 | Kitchen Academy tutorial | – | Step-by-step guide, chef switching |
| 1 | Korean **Tteokbokki** | Bowl only (plates rejected) | Rice cake + gochujang wok sauce |
| 2 | Saigon **Bột Chiên** | Plate | Fried rice-flour cake + egg |
| 3 | **Korean BBQ on Rice** | Plate | +2.0 s customer patience |
| 4 | Vietnamese **Cơm Tấm** (broken rice) | Plate | BBQ beef + egg, +2.0 s patience |
| 5 | **Bulgogi Bánh Mì** (fusion) | Plate | Baguette + seared bulgogi + pickled veg / kimchi slaw |

Other dishes exist in the code: Phở Bò (bowl, broth poured last), Bánh Mì Thịt and Kimchi Bokkeumbap.

### Core mechanics
- **Cross-prep:** each chef preps ingredients the *other* chef needs, so you must cooperate.
- **Pass Counter:** if the other chef picks up a handoff within 4 seconds, the team gets +15 **Kitchen Flow**. Bonus farming is blocked.
- **Rush Rhythm:** at 100 Flow, scores are worth 1.5x for 15 seconds.
- **Stations:** prep boards, crates, fridges and shelves, grill or sear pan, simmer wok, stockpot, noodle blancher, assembly tables, holding counters, pass counter, serving window, trash.
- **Storage (v1.4.5 and v1.4.6):** both fridges and both shelves hold 2 ingredients each.
- **Quick-select radial wheel:** pick an ingredient from a fridge or shelf. It closes on its own when you walk away (v1.4.6).
- **Carrying and throwing:** the held item floats above your head and gently tilts and bobs. It was raised in v1.4.7 so faces stay visible. Throw with `Q` or right-click to your partner, a counter, the pass counter or the trash.
- **No dish-washing:** plates and bowls are unlimited, to keep the pace fast.
- **Collisions:** chefs cannot walk through counters or walls.
- **Serving door:** the double door swings open for 1 second on each serve (v1.4.8).

### Progression and economy
- Waves, a reputation bar, gold, an upgrade shop, a recipe book with a recipe-discovery fade-in, and level replay.
- **Linh's tiers:** "third arm" visuals and faster speed.
- **Jiwon's tiers:** hire **part-timers** (one button or the `H` key, costs 20 score). Part-timers chop at the East prep counter.
- In LAN play each chef has their own upgrades (v1.3.0 and later).
- Career progress is saved to disk (`SaveSystem`, `ProgressionManager`).
- **Endless Rush** survival mode keeps high scores.

### Modes
1. **LAN co-op:** 2 devices on the same Wi-Fi. The host's device makes all the game decisions. Wi-Fi rooms are found automatically, typing an IP is a last resort, and both players must press ready.
2. **Solo with an AI partner:** 3 difficulty levels (Assistive, Standard, Challenge).
3. **Two-bot QA autoplay:** 2 bots play the game alone and write a report. Since v1.4.9, the bots are cleared when you leave this mode.

---

## 3. How it is built (architecture)

| Folder | Files | What it does |
|---|---|---|
| `Scripts/Core` | GameData, ShiftManager, ProgressionManager, AudioManager, LocalizationManager, AutoBootstrap | Recipes, shift rules, scoring, saves, procedural music and sound, 3 languages, startup fixes |
| `Scripts/Networking` | NetworkManager | UDP host and client: 30 Hz movement, 4 Hz game-state sync, LAN beacon every 1.2 s, rooms dropped after 5 s silent |
| `Scripts/Kitchen` | KitchenController, PartTimeWorker | Builds the kitchen, yard, trees and doors; part-timer crew |
| `Scripts/Stations` | 10 station types | Each station's rules. The host checks interact range (2.6 m) |
| `Scripts/Player` | PlayerController | Movement (CharacterController), tilt, carrying, throwing |
| `Scripts/AI` | BotController, KitchenNavGraph, BotCommunicationHub, TwoBotQAManager, BotLogger, BotDebugHUD | AI partner: A* waypoint paths, bot-to-bot cross-prep messages, QA reports, logs |
| `Scripts/UI` | KitchenHUD, LobbyUI, PhoneStoryUI, QuickSelectWheelUI, StoryDebugPanel, UIScaleSettings | HUD, joystick, pause and settings, lobby, story phone, wheel, UI scale |
| `Scripts/Story`, `Visuals`, `Data`, `IO` | | Dialogue, lighting, level specs, save file |

Scene is built automatically: the kitchen is baked into the scene when the editor starts.

---

## 4. Testing

- `Assets/Tests/TwoLadlesTestSuite.cs` has 20 suites and 375 assertions. The latest run passed all 375.
- The suites cover: recipes, prep rhythm, cooking states, assembly and vessel rules, pass counter anti-exploit, Flow and Rush, no-washing supply, lobby, progression, Linh and Jiwon tiers, campaign gates, endless mode, throwing and hover, waves, economy and shop, solo AI, QA bots, story and lighting, LAN sync and bot safety, pause and the LAN day cycle.
- Rule: tests are never weakened to make a build pass.
- Extra tooling: the two-bot QA run, the BotLogger log on disk, and the on-screen bot debug overlay.

---

## 5. Version history

### Phase A: Prototype and foundations (Nov 6–7)
- First LAN co-op slice in Godot: cross-prep kitchen, 4 recipes, CI, 62 tests.
- Added Linh and Jiwon tiers, 5 campaign levels and endless mode.
- **Moved to Unity C#.** 114 assertions were passing at that point.
- Imported the itch.io kitchen and food art. Added 30% movement tilt and 2D character sprites.
- Android SDK and JDK setup script.
- **Web download portal** on GitHub Pages, and the crossed-ladles crest icon and favicon.
- Kimchi Bokkeumbap, holding counters, throwing, and the hovering carried item.
- Fixed the camera, sprite scale, HUD, shaders and the duplicate EventSystem; first standalone build.

### Phase B: Kitchen and content (Nov 8)
- Tiled floor, radial wheel, grounded appliances, central divider wall.
- **Kitchen Academy tutorial.**
- Procedural AudioManager.
- LAN room discovery, reputation, waves, gold, shop, recipe book, mobile joystick, restaurant exterior and grass.

### Phase C: Menu, languages and AI (Nov 9)
- New 5-day menu and kitchen layout, plus a recipe codex on the web page.
- **3 languages** (EN, VI, KO).
- **Solo AI partner**, then the **two-bot QA runner** with an in-game button and report window.
- Many AI fixes: A* navigation, stuck loops, wok reset loop, serving deadlock, egg pipeline for Bột Chiên, counter overflow freeze, freezing at shift end.

### Phase D: Polish and hand-in (Nov 10)
- Simpler lobby for non-technical players. Upgrades are cached. Mobile controls are hidden on desktop.
- LAN order sync. Washing removed. Day 5 dialogue. Single part-timer hire button. Android JDK 11 build fix.
- Hand-in cleanup: fixed the pitch deck, removed Godot leftovers, fixed the broken Android download link.
- **v1.3.0:** per-chef LAN upgrades, score-based hiring, part-timer sync, Wi-Fi auto-scan first.
- **v1.4.0:** opening order, card placement, UI scale, placeholder chef, updated translations.
- **v1.4.1:** joystick placement, 2-bot QA test on mobile.
- **v1.4.2:** shop and story menu flow, square Jiwon portrait.
- **v1.4.3:** the LAN guest sees the match scene; Jiwon sprite raised.
- **v1.4.4:** pause menu, LAN day report, shop and ready flow, partner wobble, clear door.
- **v1.4.5:** balanced fridge and shelf storage.

### Phase E: Final polish (Nov 11)
- **v1.4.6:**
  - Settings tab in the pause menu with the music on/off switch.
  - More visible button colour.
  - 2 ingredients per fridge and shelf.
  - The wheel closes when you walk away.
- **v1.4.7:**
  - Carried item raised.
  - Jiwon's head smaller on both platforms.
  - Mobile portrait raised.
- **v1.4.8:** the door swings open for 1 second on each serve.
- **v1.4.9:**
  - QA bots no longer stay behind in other modes.
  - Text **PAUSE** button, moved inward so phones with rounded corners can reach it.
  - Resume and Leave are plain words.
  - All emojis removed from the code.
  - Camera zoomed in more on mobile.
  - Trees and decorations on the grass.
- **v1.4.10:**
  - Forgiving joystick: a 260 px touch zone, it keeps following your finger outside the ring, and it has a dead zone (a small centre area that ignores tiny movements).
  - WASD no longer moves through menus.
  - The live site was redeployed (`5be2a06`) after a temporary GitHub glitch.

---

## 6. Website and downloads
- `web/index.html` holds the themed portal, recipe codex, character art and download buttons.
- The downloads are kept in `web/downloads/`: the Windows zip and `TwoLadles.apk`.
- It deploys on its own through `.github/workflows/pages.yml` whenever `web/**` changes.

---

## 7. Docs and presentation
- Docs in `docs/`: `GAME_DESIGN_DOCUMENT.md`, `ARCHITECTURE.md`, `NETWORKING.md`, `TESTING.md`, `ANDROID_BUILD.md`, `ART_PIPELINE.md`, `DEVELOPMENT_ROADMAP.md`, `ASSET_LICENSES.md`, and `designs/`.
- Media: `demo_game_play.mp4` and 4 bot gameplay frames.
- **Pitch deck** `TwoLadles_Proposal.pptx`: slide 12 now holds the **Copyright & Asset Credits**. It is not committed to git. A backup is in the scratch folder.

### Open-source and third-party items used
| Item | By | License |
|---|---|---|
| Professional Kitchen Set (tiles) | 0_mem0ry, itch.io | Free, commercial use OK |
| Pixel Mart (food and item icons) | ghostpixxells, itch.io | Free, commercial use OK |
| Unity 2022.3 LTS and its packages | Unity | Unity Companion License |
| Networking | .NET built-in `UdpClient` | Part of .NET (no extra library) |
| Web fonts: Cinzel, Plus Jakarta Sans, JetBrains Mono | Google Fonts | SIL Open Font License |
| Characters, code, music and sound effects | Team (original) | Own work |

---

## 8. Open items and unknowns
- **Not checked:** how slide 12 looks visually. PowerPoint could not be opened from here, so please look at it yourself.
- **Unknown:** where `PixelFont.ttf` comes from (its internal name is "TwoLadlesPixel"). Please confirm before it is credited.
- **Possible tweak:** if the 260 px joystick zone catches nearby taps, it can be made smaller.
- **Not recorded here:** final playtest results on real phones for v1.4.10. Only the automated tests and the builds were confirmed.
