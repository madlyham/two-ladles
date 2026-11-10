# Art Pipeline & 2.5D Visual Style Guide

> [!NOTE]
> Applies to the Unity 2022.3.62f3 project at `unity/TwoLadles/Two Ladles/`. The earlier Godot 3D-capsule prototype has been retired.

## 1. Visual Direction: Readable Pixel-Art 2.5D

**Two Ladles** renders a 3D kitchen layout through a **fixed orthographic camera** ("2.5D Orthographic", `orthographicSize = 10.6`). Characters, food, and props are 2D pixel-art sprites placed upright in the 3D scene.

### Readability Rules
- **Chefs (Linh & Jiwon):** Upright `SpriteRenderer` sprites with distinct silhouettes and colors so each player can spot their chef instantly.
- **Carried items:** Shown floating above the chef's head with a gentle ±10° tilt and 0.08 m bob, large enough to read on a phone screen.
- **Zones:** West (Linh) and East (Jiwon) sides are separated by a center divider wall with the Pass Counter between them. Floor and grass tiles are generated procedurally.

## 2. Sprite Import Settings

| Setting | Value |
| :--- | :--- |
| Pixels Per Unit | `32` |
| Filter Mode | `Point` (no blurring) |
| Folders | `Assets/Resources/Sprites` (loaded at runtime), `Assets/Sprites` |
| Fonts | `Assets/Resources/Fonts` (lobby and story-phone UI) |

## 3. Preview Captures

`BuildScript.CaptureKitchenPreview` renders a kitchen screenshot to `Builds/preview` for quick visual checks without playing a shift.

## 4. Asset Vetting & License Compliance

Before adding any external sprite, font, or audio file:
1. Verify an explicit commercial-use license (CC0, MIT, or a documented paid license).
2. Record the source, author, and license in [ASSET_LICENSES.md](ASSET_LICENSES.md).
3. Keep sprite sizes and import settings consistent with the table above.
4. Never add unverified or license-unclear asset packs.
