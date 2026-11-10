"""
slice_character_sprites.py - Extract character sprites from idea.jpg with outer background removal.
Uses flood fill from outer boundaries so white aprons/shirts inside character outlines stay opaque.
"""

import os
from PIL import Image
import numpy as np
from collections import deque

def extract_sprites():
    src_path = "SPRITES/idea.jpg"
    out_dir = "unity/TwoLadles/Two Ladles/Assets/Resources/Sprites/Characters"
    os.makedirs(out_dir, exist_ok=True)
    
    im = Image.open(src_path).convert("RGB")
    
    # Exact non-overlapping bounding boxes determined by zero-projection gap analysis:
    specs = [
        ("linh_tier1", (195, 16, 420, 520), "Linh Stage 1 (Programmer)"),
        ("linh_tier2", (565, 16, 866, 520), "Linh Stage 2 (1 Robo-Arm)"),
        ("linh_tier3", (910, 16, 1266, 520), "Linh Stage 3 (2 Robo-Arms)"),
        ("jiwon_tier1", (185, 544, 420, 1072), "Jiwon Stage 1 (Burnout Exec)"),
        ("jiwon_tier2", (560, 544, 796, 1072), "Jiwon Stage 2 (Floor Manager)"),
        ("jiwon_tier3", (955, 544, 1225, 1072), "Jiwon Stage 3 (Operator Exec)")
    ]
    
    for file_name, box, desc in specs:
        cropped = im.crop(box).convert("RGBA")
        cw, ch = cropped.size
        carr = np.array(cropped)
        
        # Outer flood fill transparency
        mask = np.zeros((ch, cw), dtype=bool)
        is_bg = (carr[:, :, 0] > 240) & (carr[:, :, 1] > 240) & (carr[:, :, 2] > 240)
        
        queue = deque()
        for x in range(cw):
            if is_bg[0, x]:
                queue.append((0, x))
                mask[0, x] = True
            if is_bg[ch - 1, x]:
                queue.append((ch - 1, x))
                mask[ch - 1, x] = True
        for y in range(ch):
            if is_bg[y, 0]:
                queue.append((y, 0))
                mask[y, 0] = True
            if is_bg[y, cw - 1]:
                queue.append((y, cw - 1))
                mask[y, cw - 1] = True
                
        while queue:
            cy, cx = queue.popleft()
            for ny, nx in ((cy-1, cx), (cy+1, cx), (cy, cx-1), (cy, cx+1)):
                if 0 <= ny < ch and 0 <= nx < cw and not mask[ny, nx] and is_bg[ny, nx]:
                    mask[ny, nx] = True
                    queue.append((ny, nx))
                    
        carr[mask, 3] = 0
        sprite_img = Image.fromarray(carr)
        out_file = os.path.join(out_dir, f"{file_name}.png")
        sprite_img.save(out_file, "PNG")
        print(f"Saved: {out_file} ({cw}x{ch}) - {desc}")

if __name__ == "__main__":
    extract_sprites()
