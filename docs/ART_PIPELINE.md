# Art Pipeline & 2.5D Visual Style Guide (`docs/ART_PIPELINE.md`)

## 1. Visual Direction: Readable Stylized 2.5D

**Two Ladles** uses a 3D kitchen rendered through a **fixed orthographic camera** (`Camera3D.PROJECTION_ORTHOGONAL`, `size = 13.5`, positioned at `(0, 11.5, 9.5)` looking at `(0, 0, 0.2)`).

### Proportions & Readability Rules
- **Characters (`Linh` & `Jiwon`):** Stylized low-poly capsules/meshes (`radius = 0.42m`, `height = 1.4m`) with distinct silhouette colors (`Linh` = Vietnamese Jade Teal `#38ADA9`; `Jiwon` = Korean Gochujang Terracotta `#E06B3D`).
- **Food Proportions:** Oversized by ~1.35x relative to real-world scale so players on 6-inch phone screens can immediately identify whether a partner is carrying raw pork, grilled pork, pickled daikon, or a completed bowl of *Phở Bò*.
- **Zone Floor Color Coding:**
  - **West Zone (Linh's Home Side):** Cool slate-teal wood floor (`#3D4745`).
  - **Center Divider & Pass Counter:** Warm brass/amber highlight (`#EBBA52`).
  - **East Zone (Jiwon's Home Side):** Warm terracotta tile floor (`#4D3D38`).

## 2. Asset Vetting & License Compliance Protocol

Before introducing any external 3D model, texture, font, or audio file in Milestone 4:
1. Inspect polygon count (< 1,500 tris for props, < 3,500 tris for characters) and texture dimensions (<= 1024×1024 compressed `ETC2/ASTC`).
2. Verify explicit commercial-use license (CC0, MIT, or documented paid license).
3. Record the asset source, author, and license in `docs/ASSET_ATTRIBUTIONS.md`.
4. Never use unverified, mismatched asset packs or raw AI-generated 2D sprites in the 3D scene.
