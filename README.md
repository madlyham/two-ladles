# TWO LADLES
### A Vietnam–Korea Kitchen Story

> [!IMPORTANT]
> **Engine Migration Notice:** Active development on the Godot prototype has officially **stopped**. The project has fully transitioned to **100% Unity (Unity 2022.3.62f3 C#)** located at [`unity/TwoLadles/`](unity/TwoLadles/). All upcoming gameplay features, physics, sprites, network multiplayer, automated tests, and Korea Game Week 2026 competition deliverables are strictly developed in the Unity codebase.

**Two Ladles** is a 2-player local Wi-Fi (LAN) cooperative cooking game built in **Unity 2022.3.62f3** (C#, 2.5D stylized 3D kitchen, Android APK + Windows desktop builds for Korea Game Week 2026).

Two players (**Linh**, a Vietnamese former software programmer, and **Jiwon**, a Korean former business executive) operate a high-pressure Vietnam–Korea kitchen together over a local network.

> **Core Design Philosophy:** *"Easy to play badly. Difficult to play efficiently. Extremely satisfying to master."*

---

## Competition Deliverables & Engine Architecture

- **Active Engine:** Unity `2022.3.62f3` standard edition (`unity/TwoLadles/`).
- **Target Platforms:**
  - **Windows Desktop Standalone (`Builds/Windows/TwoLadles.exe`):** For desktop testing and laptop co-op host.
  - **Android Standalone APK (`Builds/Android/TwoLadles.apk`):** For mobile touchscreen co-op partner.
- **Godot Status:** Godot `4.7.2` was used as a rapid prototyping sandbox and is permanently archived on the `godot` branch. All active development, physics, assets, and tournament builds are Unity.

---

## Key Gameplay Mechanics

1. **Mandatory 2-Player Local Network Co-Op:** Built on UDP port `24567`. Host runs on laptop/desktop, partner joins via LAN IP from mobile or second machine.
2. **Solid Physical Collisions (Zero Character Phasing):** Every counter, appliance, crate, and perimeter wall has a physical `BoxCollider` static body. Chefs move via `CharacterController` with collision boundaries (`radius = 0.45`, `height = 1.3`), completely preventing characters from phasing through counters or appliances.
3. **Carried Item Visuals & Throw Mechanics:** Carried items hover dynamically above the chef's head with subtle side-to-side tilt (`±10°`) and floating vertical bobbing (`0.08m`). Press `Q` or right-click to throw items directly to your partner (who catches them with empty hands), onto an empty holding counter, into the pass counter, or into the trash.
4. **Asymmetric Vietnam–Korea Cross-Prep Layout:**
   - **Linh's West Zone:** Prepares proteins & starches (*Pork Belly*, *Bulgogi*, *Rice Cakes*, *Dry Rice Noodles*, *Baguettes*), operates the **Dedicated Phở Stockpot** and **Noodle Blancher**, and assembles Vietnamese dishes.
   - **Jiwon's East Zone:** Prepares vegetables & fish cakes (*Daikon & Carrot*, *Aged Kimchi*, *Fish Cakes*, *Gochujang*), operates the **Grill/Sear Pan** and **Gochujang Simmer Wok**, washes dirty dishes, and provides clean vessels.
5. **Authentic 5-Level Campaign Progression:**
   - **Level 1 (First Customer):** *Bánh Mì Thịt* (Traditional Vietnamese — Baguette + grilled pork + pickled veg + cilantro/sauce on plate).
   - **Level 2 (Lunch Rush):** *Phở Bò* (Traditional Vietnamese — Bowl + blanched rice noodles + sliced beef + hot stockpot broth poured last).
   - **Level 3 (Night Market):** *Tteokbokki* (Traditional Korean — Bowl + simmered rice cakes + fish cakes + gochujang sauce. Rejects flat plates!).
   - **Level 4 (Street Kitchen):** *Kimchi Bokkeumbap* (Traditional Korean — Plate + stir-fried rice + aged kimchi + grilled protein).
   - **Level 5 (Grand Opening):** *Bulgogi Bánh Mì* (Original Vietnam–Korea Fusion Specialty — Baguette + grilled bulgogi + kimchi slaw + gochujang glaze).
6. **Pass Counter Handoffs & Rush Rhythm:**
   - Handoff bonus grants `+15` Kitchen Flow when retrieved by the other player within 4.0s.
   - Reaching `100` Flow activates **Rush Rhythm** (`1.5x` score multiplier) for 15.0 seconds.

---

## Project Structure (`unity/TwoLadles`)

```
unity/TwoLadles/
├── Assets/
│   ├── Editor/
│   │   └── BuildScript.cs          # Automated scene generation & Windows/Android build pipeline
│   ├── Resources/
│   │   └── Sprites/               # Pixel-art kitchen tiles, appliances, item icons & characters
│   ├── Scenes/
│   │   └── MainKitchen.unity       # Primary 2.5D game scene
│   ├── Scripts/
│   │   ├── Core/                  # GameData, ShiftManager, ProgressionManager
│   │   ├── Data/                  # RecipeData, CampaignLevelSpec, CharacterProgressionData
│   │   ├── Kitchen/               # KitchenController (layout builder & collision setup)
│   │   ├── Networking/            # NetworkManager (UDP 24567 host/client synchronization)
│   │   ├── Player/                # PlayerController (movement, physics, throw, carried item hover)
│   │   ├── Stations/              # Prep, Cooking, Assembly, Washing, PassCounter, Crate, Trash
│   │   └── UI/                    # LobbyUI & KitchenHUD
│   └── Tests/
│       └── TwoLadlesTestSuite.cs  # 14-suite automated verification runner (692 assertions)
```

---

## How to Open & Build in Unity

1. Open **Unity Hub** (or start Unity `2022.3.62f3`).
2. Add and open the project at `unity/TwoLadles`.
3. In Unity Editor:
   - Run scene `Assets/Scenes/MainKitchen.unity`.
   - Or click **BuildScript -> Build Windows** in the menu or execute `BuildScript.BuildWindows()` to output `Builds/Windows/TwoLadles.exe`.
   - To build Android APK, ensure Android Target Support is installed and select Android platform.
