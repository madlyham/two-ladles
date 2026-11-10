import base64
import os
import cv2
import numpy as np
from PIL import Image

PROJECT_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC_IMAGE = os.path.join(PROJECT_DIR, "TWO_LADLES.jpg")
PNG_TRANSPARENT = os.path.join(PROJECT_DIR, "web", "assets", "icon_transparent.png")
ICON_PNG = os.path.join(PROJECT_DIR, "icon.png")

WEB_ICON_SVG = os.path.join(PROJECT_DIR, "web", "assets", "icon.svg")
WEB_FAVICON_SVG = os.path.join(PROJECT_DIR, "web", "favicon.svg")
WEB_FAVICON_ICO = os.path.join(PROJECT_DIR, "web", "favicon.ico")

def generate():
    img = cv2.imread(SRC_IMAGE)
    h, w, _ = img.shape

    # Floodfill from edges to find external background
    mask = np.zeros((h+2, w+2), np.uint8)
    floodflags = 4 | (255 << 8) | cv2.FLOODFILL_MASK_ONLY
    cv2.floodFill(img, mask, (10, 10), 0, (25, 25, 25), (25, 25, 25), floodflags)
    cv2.floodFill(img, mask, (w-10, 10), 0, (25, 25, 25), (25, 25, 25), floodflags)
    cv2.floodFill(img, mask, (10, h-10), 0, (25, 25, 25), (25, 25, 25), floodflags)
    cv2.floodFill(img, mask, (w-10, h-10), 0, (25, 25, 25), (25, 25, 25), floodflags)

    bg_mask = mask[1:h+1, 1:w+1] == 255
    # Remove corner sparkle watermark
    bg_mask[1800:, 1800:] = True

    alpha = np.where(bg_mask, 0, 255).astype(np.uint8)

    # Find mascot bounds
    ys, xs = np.where(alpha > 0)
    min_x, max_x = xs.min(), xs.max()
    min_y, max_y = ys.min(), ys.max()

    pad = 40
    crop_x1 = max(0, min_x - pad)
    crop_x2 = min(w, max_x + pad)
    crop_y1 = max(0, min_y - pad)
    crop_y2 = min(h, max_y + pad)

    mascot_w = crop_x2 - crop_x1
    mascot_h = crop_y2 - crop_y1
    side = max(mascot_w, mascot_h)

    # Center in a clean square
    canvas = np.zeros((side, side, 4), dtype=np.uint8)
    ox = (side - mascot_w) // 2
    oy = (side - mascot_h) // 2

    b, g, r = cv2.split(img)
    alpha_smooth = cv2.GaussianBlur(alpha, (3, 3), 0)

    canvas[oy:oy+mascot_h, ox:ox+mascot_w, 0] = b[crop_y1:crop_y2, crop_x1:crop_x2]
    canvas[oy:oy+mascot_h, ox:ox+mascot_w, 1] = g[crop_y1:crop_y2, crop_x1:crop_x2]
    canvas[oy:oy+mascot_h, ox:ox+mascot_w, 2] = r[crop_y1:crop_y2, crop_x1:crop_x2]
    canvas[oy:oy+mascot_h, ox:ox+mascot_w, 3] = alpha_smooth[crop_y1:crop_y2, crop_x1:crop_x2]

    # Save high-res 1024x1024 transparent PNG
    resized_1024 = cv2.resize(canvas, (1024, 1024), interpolation=cv2.INTER_AREA)
    cv2.imwrite(PNG_TRANSPARENT, resized_1024)
    cv2.imwrite(ICON_PNG, resized_1024)

    # Save multi-size ICO
    pil_img = Image.fromarray(cv2.cvtColor(resized_1024, cv2.COLOR_BGRA2RGBA))
    pil_img.save(WEB_FAVICON_ICO, format="ICO", sizes=[(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])

    # Encode PNG to base64 for SVG
    with open(PNG_TRANSPARENT, "rb") as f:
        b64_data = base64.b64encode(f.read()).decode("utf-8")

    svg_content = f'''<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 1024 1024" width="1024" height="1024">
  <defs>
    <filter id="icon-shadow" x="-10%" y="-10%" width="120%" height="120%">
      <feDropShadow dx="0" dy="16" stdDeviation="12" flood-color="#000000" flood-opacity="0.35"/>
    </filter>
  </defs>
  <image width="1024" height="1024" filter="url(#icon-shadow)" xlink:href="data:image/png;base64,{b64_data}" href="data:image/png;base64,{b64_data}"/>
</svg>'''

    for path in [WEB_ICON_SVG, WEB_FAVICON_SVG]:
        with open(path, "w", encoding="utf-8") as f:
            f.write(svg_content)

    print("Successfully generated all SVG, PNG, and ICO icon assets!")

if __name__ == "__main__":
    generate()
