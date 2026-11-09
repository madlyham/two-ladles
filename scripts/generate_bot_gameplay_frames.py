import os
from PIL import Image, ImageDraw, ImageFont

def get_font(size, bold=False):
    font_names = ["segoeui.ttf", "arial.ttf", "tahoma.ttf"]
    if bold:
        font_names = ["segoeuib.ttf", "arialbd.ttf", "tahomabd.ttf"]
    for fn in font_names:
        p = os.path.join(r"C:\Windows\Fonts", fn)
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                pass
    return ImageFont.load_default()

def get_item_sprite(name, size=(28, 28)):
    path = os.path.join("unity", "TwoLadles", "Two Ladles", "Assets", "Resources", "Sprites", "Items", f"{name}.png")
    if os.path.exists(path):
        return Image.open(path).convert("RGBA").resize(size, Image.Resampling.NEAREST)
    return None

def create_gameplay_frame(base_crop, title_tag, title_color, sub_text, hud_badge, badge_color, item_icon_names=None):
    card_w, card_h = 640, 420
    card = Image.new("RGBA", (card_w, card_h), (26, 32, 38, 255))
    
    view_w, view_h = 636, 330
    resized_crop = base_crop.resize((view_w, view_h), Image.Resampling.LANCZOS)
    card.paste(resized_crop, (2, 2))
    
    draw = ImageDraw.Draw(card)
    
    # Outer border
    draw.rectangle([0, 0, card_w - 1, card_h - 1], outline=title_color, width=2)
    # Split line
    draw.line([0, view_h + 2, card_w, view_h + 2], fill=title_color, width=2)
    
    # Header banner
    draw.rectangle([10, 10, card_w - 10, 42], fill=(20, 24, 30, 230))
    font_title = get_font(14, bold=True)
    draw.text((20, 16), title_tag.upper(), fill=title_color, font=font_title)
    
    # Header badge
    font_badge = get_font(12, bold=True)
    badge_bbox = font_badge.getbbox(hud_badge)
    badge_w = badge_bbox[2] - badge_bbox[0] + 16
    draw.rounded_rectangle([card_w - badge_w - 20, 12, card_w - 20, 40], radius=4, fill=(35, 42, 52, 245), outline=badge_color, width=1)
    draw.text((card_w - badge_w - 12, 17), hud_badge, fill=badge_color, font=font_badge)
    
    # Bottom caption
    font_sub = get_font(13, bold=False)
    draw.text((15, view_h + 12), sub_text, fill=(245, 248, 250), font=font_sub)
    
    # Bottom right item icons
    if item_icon_names:
        icon_x = card_w - 15
        for iname in reversed(item_icon_names):
            icon_img = get_item_sprite(iname, (30, 30))
            if icon_img:
                icon_x -= 38
                draw.rounded_rectangle([icon_x - 3, view_h - 38, icon_x + 35, view_h], radius=4, fill=(20, 24, 30, 210), outline=(80, 95, 115), width=1)
                card.paste(icon_img, (icon_x, view_h - 34), icon_img)

    return card

def main():
    repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    os.chdir(repo_root)
    
    src_path = os.path.join(repo_root, "unity", "TwoLadles", "Two Ladles", "Builds", "preview", "unity_kitchen_gameplay_view.png")
    if not os.path.exists(src_path):
        print(f"Error: {src_path} not found!")
        return
        
    src = Image.open(src_path)
    docs_dir = os.path.join(repo_root, "docs")
    os.makedirs(docs_dir, exist_ok=True)
    
    # Load character sprites for compositing
    linh_raw = Image.open("unity/TwoLadles/Two Ladles/Assets/Resources/Sprites/Characters/linh_tier1.png").convert("RGBA")
    jiwon_raw = Image.open("unity/TwoLadles/Two Ladles/Assets/Resources/Sprites/Characters/jiwon_tier1.png").convert("RGBA")
    
    C_TEAL = (46, 173, 142)
    C_TERRA = (224, 98, 56)
    C_GOLD = (245, 185, 66)
    C_GREEN = (60, 210, 120)
    
    # --------------------------------------------------------------------------
    # FRAME 1: Synchronized Dual-Zone Preparation
    # --------------------------------------------------------------------------
    crop1 = src.crop((390, 210, 890, 560)).copy()
    pork_ico = get_item_sprite("pork_belly_raw", (22, 22))
    daikon_ico = get_item_sprite("pickled_veg", (22, 22))
    d1 = ImageDraw.Draw(crop1)
    if pork_ico:
        d1.ellipse([140, 130, 170, 160], fill=(20, 24, 30, 230), outline=(46, 173, 142), width=1)
        crop1.paste(pork_ico, (144, 134), pork_ico)
    if daikon_ico:
        d1.ellipse([310, 145, 340, 175], fill=(20, 24, 30, 230), outline=(224, 98, 56), width=1)
        crop1.paste(daikon_ico, (314, 149), daikon_ico)
        
    f1 = create_gameplay_frame(
        crop1,
        "Frame 1: Synchronized Dual-Zone Preparation",
        C_TEAL,
        "Linh [Bot 1] chops raw pork in 4-strike cadence | Jiwon [Bot 2] preps daikon & kimchi slaw",
        "CADENCE: PERFECT",
        C_GREEN,
        ["pork_portioned", "pickled_veg", "kimchi_slaw"]
    )
    p1 = os.path.join(docs_dir, "bot_gameplay_frame1.png")
    f1.save(p1)
    print(f"Saved {p1}")

    # --------------------------------------------------------------------------
    # FRAME 2: Pass Counter Relay (+15 Flow Handoff)
    # --------------------------------------------------------------------------
    crop2 = src.crop((470, 270, 810, 570)).copy()
    d2 = ImageDraw.Draw(crop2)
    meat_ico = get_item_sprite("pork_portioned", (24, 24))
    if meat_ico:
        d2.rectangle([155, 125, 185, 155], fill=(245, 185, 66, 180), outline=(255, 255, 255), width=1)
        crop2.paste(meat_ico, (158, 128), meat_ico)
        d2.rounded_rectangle([130, 95, 210, 120], radius=4, fill=(245, 185, 66), outline=(20, 24, 30), width=1)
        font_sm = get_font(11, bold=True)
        d2.text((136, 100), "+15 FLOW!", fill=(20, 24, 30), font=font_sm)

    f2 = create_gameplay_frame(
        crop2,
        "Frame 2: Cross-Kitchen Pass Counter Relay",
        C_GOLD,
        "Linh places portioned meat -> Jiwon retrieves in 2.8s (+15 Flow Anti-Exploit Bonus)",
        "+15 KITCHEN FLOW",
        C_GOLD,
        ["pork_portioned", "beef_bulgogi_raw"]
    )
    p2 = os.path.join(docs_dir, "bot_gameplay_frame2.png")
    f2.save(p2)
    print(f"Saved {p2}")

    # --------------------------------------------------------------------------
    # FRAME 3: Cooking Appliances & Heat Timing
    # --------------------------------------------------------------------------
    w_stove = src.crop((280, 220, 560, 500)).copy()
    e_grill = src.crop((720, 220, 1000, 500)).copy()
    
    linh_stove = linh_raw.resize((42, 94), Image.Resampling.LANCZOS)
    w_stove.paste(linh_stove, (90, 80), linh_stove)
    
    jiwon_grill = jiwon_raw.resize((42, 94), Image.Resampling.LANCZOS)
    e_grill.paste(jiwon_grill, (170, 75), jiwon_grill)
    
    crop3 = Image.new("RGBA", (560, 280))
    crop3.paste(w_stove, (0, 0))
    crop3.paste(e_grill, (280, 0))
    d3 = ImageDraw.Draw(crop3)
    d3.line([279, 0, 279, 280], fill=(245, 185, 66), width=2)
    
    f3 = create_gameplay_frame(
        crop3,
        "Frame 3: Parallel Heat Cooking",
        C_TERRA,
        "Linh cooks flour + egg on West Stove | Jiwon sears bulgogi on East Grill",
        "HEAT: OPTIMAL (+20%)",
        C_GREEN,
        ["cooked_flour", "bulgogi"]
    )
    p3 = os.path.join(docs_dir, "bot_gameplay_frame3.png")
    f3.save(p3)
    print(f"Saved {p3}")

    # --------------------------------------------------------------------------
    # FRAME 4: Self-Assembly, Service & Rush Rhythm
    # --------------------------------------------------------------------------
    crop4 = src.crop((280, 40, 1000, 320)).copy()
    f4 = create_gameplay_frame(
        crop4,
        "Frame 4: Assemble & Serve",
        C_GOLD,
        "Parts combined at holding table | served to diners | Flow 100 = 1.5x Rush",
        "RUSH RHYTHM (1.5x)",
        C_GOLD,
        ["baguette", "bulgogi", "bulgogi_banh_mi"]
    )
    p4 = os.path.join(docs_dir, "bot_gameplay_frame4.png")
    f4.save(p4)
    print(f"Saved {p4}")

    print("All 4 bot gameplay frames generated successfully with custom overlays!")

if __name__ == "__main__":
    main()
