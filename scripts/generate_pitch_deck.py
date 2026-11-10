import os
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.enum.text import PP_ALIGN
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE
from PIL import Image


def add_fit(slide, path, left, top, box_w, box_h):
    """Add a picture scaled to fit inside the box (keeps aspect ratio, centered)."""
    with Image.open(path) as im:
        iw, ih = im.size
    scale = min(box_w / iw, box_h / ih)
    w, h = int(iw * scale), int(ih * scale)
    return slide.shapes.add_picture(path, int(left + (box_w - w) / 2), int(top + (box_h - h) / 2), width=w, height=h)


def create_deck(output_path="docs/two_ladles_concept.pptx"):
    # Ensure bot gameplay frames are generated and fresh
    try:
        import sys
        scripts_dir = os.path.dirname(os.path.abspath(__file__))
        if scripts_dir not in sys.path:
            sys.path.insert(0, scripts_dir)
        import generate_bot_gameplay_frames as gbgf
        gbgf.main()
    except Exception as ex:
        print(f"Warning: could not generate bot frames: {ex}")

    prs = Presentation()
    # 16:9 Widescreen layout
    prs.slide_width = Inches(13.333)
    prs.slide_height = Inches(7.5)
    blank_layout = prs.slide_layouts[6]

    # Two Ladles Palette
    C_DARK = RGBColor(26, 32, 38)        # Deep Charcoal
    C_CARD = RGBColor(38, 46, 56)        # Card Background
    C_TEAL = RGBColor(46, 173, 142)      # Linh Emerald Teal
    C_TERRA = RGBColor(224, 98, 56)      # Jiwon Terracotta
    C_GOLD = RGBColor(245, 185, 66)      # Golden Ladle
    C_WHITE = RGBColor(245, 248, 250)    # Crisp Off-White
    C_MUTED = RGBColor(165, 175, 188)    # Secondary Text

    def set_slide_background(slide, color):
        bg = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, prs.slide_height)
        bg.fill.solid()
        bg.fill.fore_color.rgb = color
        bg.line.fill.background()
        return bg

    def add_header(slide, category, title):
        # Category tag
        tb_cat = slide.shapes.add_textbox(Inches(0.8), Inches(0.5), Inches(11.5), Inches(0.4))
        p_cat = tb_cat.text_frame.paragraphs[0]
        p_cat.text = category.upper()
        p_cat.font.size = Pt(11)
        p_cat.font.bold = True
        p_cat.font.color.rgb = C_GOLD

        # Slide Title
        tb_title = slide.shapes.add_textbox(Inches(0.8), Inches(0.85), Inches(11.5), Inches(0.8))
        p_title = tb_title.text_frame.paragraphs[0]
        p_title.text = title
        p_title.font.size = Pt(26)
        p_title.font.bold = True
        p_title.font.color.rgb = C_WHITE

    def add_card(slide, left, top, width, height, title, items, accent_color=None, title_size=16, body_size=13):
        card = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
        card.fill.solid()
        card.fill.fore_color.rgb = C_CARD
        card.line.color.rgb = accent_color if accent_color else RGBColor(55, 65, 80)
        card.line.width = Pt(1.5)

        tb = slide.shapes.add_textbox(left + Inches(0.18), top + Inches(0.18), width - Inches(0.36), height - Inches(0.36))
        tf = tb.text_frame
        tf.word_wrap = True

        p0 = tf.paragraphs[0]
        p0.text = title
        p0.font.size = Pt(title_size)
        p0.font.bold = True
        p0.font.color.rgb = accent_color if accent_color else C_GOLD
        p0.space_after = Pt(6)

        for it in items:
            p = tf.add_paragraph()
            p.text = f"• {it}"
            p.font.size = Pt(body_size)
            p.font.color.rgb = C_WHITE
            p.space_after = Pt(4)

    # ==========================================
    # SLIDE 1: TITLE SLIDE
    # ==========================================
    s1 = prs.slides.add_slide(blank_layout)
    set_slide_background(s1, C_DARK)

    # Gold accent bar
    bar = s1.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.8), Inches(1.8), Inches(0.12), Inches(3.6))
    bar.fill.solid()
    bar.fill.fore_color.rgb = C_GOLD
    bar.line.fill.background()

    # Title and Subtitle text box
    tb = s1.shapes.add_textbox(Inches(1.2), Inches(1.8), Inches(7.2), Inches(3.6))
    tf = tb.text_frame
    p = tf.paragraphs[0]
    p.text = "TWO LADLES"
    p.font.size = Pt(48)
    p.font.bold = True
    p.font.color.rgb = C_WHITE

    p2 = tf.add_paragraph()
    p2.text = "A Vietnam–Korea Kitchen Story"
    p2.font.size = Pt(24)
    p2.font.bold = True
    p2.font.color.rgb = C_TEAL
    p2.space_after = Pt(18)

    p3 = tf.add_paragraph()
    p3.text = "Korea Game Week 2026 (KGW2026) Official Competition Entry"
    p3.font.size = Pt(15)
    p3.font.color.rgb = C_GOLD

    p4 = tf.add_paragraph()
    p4.text = "Genre: 2-Player Local Wi-Fi Co-Op Cooking Simulator | Platform: Unity (Android + PC)"
    p4.font.size = Pt(13)
    p4.font.color.rgb = C_MUTED

    # Title slide character showcase
    char_linh = "docs/deck_art/linh_tier2.jpg"
    if not os.path.exists(char_linh):
        char_linh = "unity/TwoLadles/Two Ladles/Assets/Resources/Sprites/Characters/linh_tier2.png"
    char_jiwon = "docs/deck_art/jiwon_tier2.jpg"
    if not os.path.exists(char_jiwon):
        char_jiwon = "unity/TwoLadles/Two Ladles/Assets/Resources/Sprites/Characters/jiwon_tier2.png"
    if os.path.exists(char_linh):
        add_fit(s1, char_linh, Inches(8.6), Inches(1.6), Inches(2.1), Inches(4.8))
    if os.path.exists(char_jiwon):
        add_fit(s1, char_jiwon, Inches(10.8), Inches(1.6), Inches(2.1), Inches(4.8))

    # ==========================================
    # SLIDE 2: EXECUTIVE SUMMARY & CORE PHILOSOPHY
    # ==========================================
    s2 = prs.slides.add_slide(blank_layout)
    set_slide_background(s2, C_DARK)
    add_header(s2, "Executive Summary", "Core Gameplay & Design Philosophy")

    add_card(s2, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "Design Philosophy",
        [
            "\"Easy to play badly. Difficult to play efficiently. Extremely satisfying to master.\"",
            "True 2-Player Local Co-Op: Two human chefs working together over local Wi-Fi / LAN.",
            "Solo Mode: play alone with an AI partner chef (Assistive / Standard / Challenge difficulty).",
            "Cross-Kitchen Reliance: Linh and Jiwon are dependent on each other to fulfill orders.",
            "Tactile & Satisfying: Rhythm chops, heat timing, and rapid pass counter handoffs."
        ],
        C_TEAL
    )

    add_card(s2, Inches(6.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "Competition Alignment (KGW2026)",
        [
            "Developed in Unity C# targeting Landscape Android & Desktop PC playtesting.",
            "Celebrates Vietnam–Korea Cultural Friendship through authentic cuisine & collaboration.",
            "Original Gameplay: custom code and systems built for this entry; traditional dishes credited as traditional.",
            "Dynamic Shift Tension: High-pressure service shifts, Kitchen Flow meter, and Rush Rhythm bursts.",
            "Broad Appeal: Casual pick-up-and-play mobile touch controls + competitive co-op mastery."
        ],
        C_TERRA
    )

    # ==========================================
    # SLIDE 3: STORY & PROTAGONISTS
    # ==========================================
    s3 = prs.slides.add_slide(blank_layout)
    set_slide_background(s3, C_DARK)
    add_header(s3, "Narrative & Protagonists", "Two Exhausted Professionals, One Shared Culinary Venture")

    add_card(s3, Inches(0.8), Inches(1.8), Inches(4.3), Inches(5.0),
        "Linh — The Systems Optimizer",
        [
            "Nationality: Vietnamese | Former Career: Software Engineer.",
            "Background: Left tech burnout and sprint pressure to return home and reopen her family's food stall in rural Vietnam.",
            "Playstyle: Process optimization, chopping cadence, and stove timing (flour, rice, toboki, eggs).",
            "Home Base: West Zone (Fridge, Shelf, 2 Stoves, 3 Self-Assembly Holding Tables).",
            "Signature Ability: Execution Specialist — one extra 'third arm' speeds her work (+8% at Tier 2, +15% at Tier 3)."
        ],
        C_TEAL
    )
    if os.path.exists(char_linh):
        add_fit(s3, char_linh, Inches(5.15), Inches(2.0), Inches(1.6), Inches(4.6))

    add_card(s3, Inches(6.8), Inches(1.8), Inches(4.3), Inches(5.0),
        "Jiwon — The Operations Leader",
        [
            "Nationality: Korean | Former Career: Corporate Business Executive.",
            "Background: Left corporate boardroom exhaustion for a sabbatical in Vietnam; partnered with Linh to build a food venture.",
            "Playstyle: Service pacing, delegation, and grill/wok heat management.",
            "Home Base: East Zone (Fridge, Shelf, Grill, Wok, 2 Self-Assembly Holding Tables).",
            "Signature Ability: \"Call in the Crew\" — spends coins to hire part-time prep helpers who chop ingredients."
        ],
        C_TERRA
    )
    if os.path.exists(char_jiwon):
        add_fit(s3, char_jiwon, Inches(11.15), Inches(2.0), Inches(2.0), Inches(4.6))

    # ==========================================
    # SLIDE 4: CROSS-PREP KITCHEN INTERDEPENDENCE
    # ==========================================
    s4 = prs.slides.add_slide(blank_layout)
    set_slide_background(s4, C_DARK)
    add_header(s4, "Spatial Topology", "Asymmetric Kitchen Layout: Built-In Mutual Reliance")

    add_card(s4, Inches(0.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "West Zone (Linh)",
        [
            "Fridge (Meat / Radish / Egg / Toboki) & Shelf (Gochujang / Flour / Rice).",
            "Prep Table: chop with [F] in a discrete 4-strike cadence.",
            "2 Stoves: cook flour, rice, toboki & eggs.",
            "3 Self-Assembly Holding Tables: combine finished parts with [E]."
        ],
        C_TEAL
    )

    add_card(s4, Inches(4.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Center Pass Counter",
        [
            "3-Slot Shared Handoff Station connecting West and East.",
            "Coordinated Handoff Bonus: +15 Kitchen Flow when partner retrieves item within 4 seconds.",
            "Anti-Exploit Protection: self-retrieval gives 0 Flow; max 1 bonus per order.",
            "Shared Trash Can for burnt or wrong items."
        ],
        C_GOLD
    )

    add_card(s4, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "East Zone (Jiwon)",
        [
            "Fridge (Raw Meat / Radish / Egg / Toboki) & Shelf (Gochujang / Flour / Rice).",
            "Prep Table: cuts raw meat once (Korean BBQ meat) or twice (bulgogi).",
            "Grill (BBQ beef, bulgogi) & Wok (Tteokbokki, rice, egg).",
            "2 Self-Assembly Holding Tables."
        ],
        C_TERRA
    )

    # ==========================================
    # SLIDE 5: 5-DAY MENU
    # ==========================================
    s5 = prs.slides.add_slide(blank_layout)
    set_slide_background(s5, C_DARK)
    add_header(s5, "The Menu", "5-Day Menu: Traditional Dishes & a Fusion Finale")

    menu_cards = [
        ("Day 1 — Tteokbokki", "Traditional Korean", [
            "Rice cakes (toboki) + gochujang, cooked together.",
            "115 pts | Patience 60s."
        ], C_TERRA),
        ("Day 2 — Bột Chiên", "Traditional Vietnamese", [
            "Saigon fried rice-flour cake: cook flour, add egg, cook again.",
            "125 pts | Patience 65s."
        ], C_TEAL),
        ("Day 3 — Korean BBQ on Rice", "Traditional Korean", [
            "Cut raw meat -> grill BBQ beef -> combine with cooked rice.",
            "140 pts | Patience 65s."
        ], C_TERRA),
        ("Day 4 — Cơm Tấm", "Traditional Vietnamese", [
            "Broken rice with grilled beef & egg, combined in any order.",
            "155 pts | Patience 70s."
        ], C_TEAL),
        ("Day 5 — Bulgogi Bánh Mì", "Two Ladles fusion twist", [
            "Baguette + pickled radish/carrot + bulgogi.",
            "3-minute high-score finale | 180 pts | Patience 75s."
        ], C_GOLD),
    ]
    for i, (mtitle, mtag, mitems, mcol) in enumerate(menu_cards):
        mx = Inches(0.8 + i * 2.35)
        add_card(s5, mx, Inches(1.8), Inches(2.2), Inches(4.2), mtitle, [mtag] + mitems, mcol, title_size=14, body_size=11.5)

    tb_m = s5.shapes.add_textbox(Inches(0.8), Inches(6.2), Inches(11.7), Inches(0.8))
    tb_m.text_frame.word_wrap = True
    p_m = tb_m.text_frame.paragraphs[0]
    p_m.text = "Menus build up day by day (Day 2 = Tteokbokki + Bột Chiên, Day 4 = all four). Days 1–4 are traditional dishes; only Day 5 is our fusion take."
    p_m.font.size = Pt(12)
    p_m.font.color.rgb = C_MUTED

    # ==========================================
    # SLIDE 6: FIVE DISTINCT STATION MECHANICS
    # ==========================================
    s6 = prs.slides.add_slide(blank_layout)
    set_slide_background(s6, C_DARK)
    add_header(s6, "Station Gameplay", "Tactile Mechanics Without Generic Progress Bars")

    add_card(s6, Inches(0.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Prep & Cooking",
        [
            "Preparation Counter: 4-strike discrete rhythm cadence (0.45s ideal interval). Graded PERFECT, GOOD, or MISS.",
            "Miss Non-Blocking: rhythm misses never freeze workflow, preserving game feel.",
            "Cooking Station: 5-state heat machine (RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT).",
            "Optimal timing awards +20% score bonus; overcooking reduces score; burning wastes ingredient."
        ],
        C_TEAL
    )

    add_card(s6, Inches(4.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Assembly & Handoff",
        [
            "Self-Assembly: no plates or bowls — combine finished parts at any holding table with [E], in any order.",
            "Rejects invalid combinations with clear sound/visual feedback without destroying items.",
            "Pass Counter: Central 3-slot relay counter.",
            "4-Second Handoff Window: Rapid cross-kitchen coordination rewarded with team Flow."
        ],
        C_GOLD
    )

    add_card(s6, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Serving & Throwing",
        [
            "Serving: finished dish is matched against the active ticket queue.",
            "Patience Tip Bonus: Fast service yields higher restaurant tips.",
            "Throw (Q / right-click): toss an item to your partner, a free holding table, the pass counter or the trash.",
            "Ingredient Flow: fridge/shelf -> chop -> cook -> combine -> serve. No dishwashing."
        ],
        C_TERRA
    )

    # ==========================================
    # SLIDE 7: LIVE 2-BOT AUTONOMOUS GAMEPLAY SEQUENCE (FRAMES)
    # ==========================================
    s7 = prs.slides.add_slide(blank_layout)
    set_slide_background(s7, C_DARK)
    add_header(s7, "QA Bot Gameplay (Illustrative)", "How the 2-Bot Test Run Plays a Shift: 4-Stage Co-Op Loop")

    frames_meta = [
        ("docs/bot_gameplay_frame1.png", "1. Cadence Prep", [
            "Jiwon [Bot 2]: cuts raw meat twice into bulgogi at the East prep table.",
            "Linh [Bot 1]: cuts radish/carrot for pickled veg.",
            "PERFECT strike timing awards rhythm cadence bonus."
        ], C_TEAL),
        ("docs/bot_gameplay_frame2.png", "2. Pass Counter Relay", [
            "Central 3-slot relay counter connects Linh & Jiwon.",
            "One chef places an item -> the partner picks it up within 4s.",
            "+15 Kitchen Flow awarded (Anti-Exploit protected)."
        ], C_GOLD),
        ("docs/bot_gameplay_frame3.png", "3. Parallel Cooking", [
            "Jiwon sears bulgogi on the East Grill.",
            "Linh cooks flour + egg dough into a baguette on a West Stove.",
            "5-state heat machine: OPTIMAL window gives +20% score."
        ], C_TERRA),
        ("docs/bot_gameplay_frame4.png", "4. Assemble & Serve", [
            "Parts combined at a holding table with [E], any order.",
            "100 Kitchen Flow activates 15s RUSH RHYTHM (1.5x score).",
            "Bots replay any day with all upgrades for QA."
        ], C_GOLD)
    ]

    for i, (fpath, ftitle, fbullets, fcolor) in enumerate(frames_meta):
        fx = Inches(0.8 + i * 2.95)
        if os.path.exists(fpath):
            s7.shapes.add_picture(fpath, fx, Inches(1.65), width=Inches(2.75))
        add_card(s7, fx, Inches(3.6), Inches(2.75), Inches(3.4), ftitle, fbullets, fcolor, title_size=13, body_size=10.5)

    # ==========================================
    # SLIDE 8: IN-GAME ENVIRONMENT & VISUAL TOPOLOGY
    # ==========================================
    s8 = prs.slides.add_slide(blank_layout)
    set_slide_background(s8, C_DARK)
    add_header(s8, "In-Game Environment", "Complete 2.5D Kitchen Environment & Dining Patio View")

    full_kitchen_img = "unity/TwoLadles/Two Ladles/Builds/preview/unity_kitchen_gameplay_view.png"
    if os.path.exists(full_kitchen_img):
        s8.shapes.add_picture(full_kitchen_img, Inches(0.8), Inches(1.65), width=Inches(7.8))

    add_card(s8, Inches(8.8), Inches(1.65), Inches(3.7), Inches(5.2),
        "Live Visual Topology",
        [
            "Flat 2D Floor: the kitchen floor sits flat behind the chefs, removing the old perspective tilt.",
            "Dynamic 2.5D Depth Sorting: Automatic z-index calculates (500 - Z*50) so chefs naturally render in front/behind counters.",
            "Asymmetric West/East Layout: Linh runs the stoves (flour, rice, toboki, egg); Jiwon runs prep, grill and wok.",
            "3-Slot Pass Counter: Central stainless steel relay counter with trash disposal bin.",
            "Exterior Dining Patio: Customer tables with placemats and real-time waiting diners.",
            "2-Bot QA Autoplay: bots can replay any day with all upgrades unlocked."
        ],
        C_GOLD,
        title_size=15,
        body_size=11.5
    )

    # ==========================================
    # SLIDE 9: KITCHEN FLOW & RUSH RHYTHM
    # ==========================================
    s9 = prs.slides.add_slide(blank_layout)
    set_slide_background(s9, C_DARK)
    add_header(s9, "Momentum Engine", "Kitchen Flow (0–100) & High-Scoring Rush Rhythm")

    add_card(s9, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "Kitchen Flow Dynamics",
        [
            "Flow Meter (0 to 100): Represents kitchen rhythm, speed, and harmony.",
            "+15 Flow: Coordinated Pass Counter handoff within 4 seconds.",
            "+20 Flow: Serving an OPTIMAL quality dish.",
            "+12 Flow: Serving a STANDARD quality dish.",
            "-15 Flow: Customer order expires unfulfilled.",
            "-10 Flow: Burnt ingredient / kitchen waste.",
            "Anti-Exploit: Max 1 handoff bonus per order; self-retrieval gives 0 Flow."
        ],
        C_TEAL
    )

    add_card(s9, Inches(6.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "Rush Rhythm Mode",
        [
            "Activation: Reaching 100 Kitchen Flow immediately activates Rush Rhythm.",
            "Meter Reset: Flow resets to 0 upon trigger.",
            "1.5x Multiplier: All customer orders served during the 15-second window earn 1.5x base score.",
            "Paused Generation: Flow accumulation is paused while Rush Rhythm is active.",
            "Dynamic Atmosphere: Screen border glow, lively audio tempo, and celebratory kitchen feedback."
        ],
        C_GOLD
    )

    # ==========================================
    # SLIDE 10: CHARACTER SIGNATURE ABILITIES
    # ==========================================
    s10 = prs.slides.add_slide(blank_layout)
    set_slide_background(s10, C_DARK)
    add_header(s10, "Character Abilities", "Distinct Thematic Progression Across 3 Evolution Tiers")

    add_card(s10, Inches(0.8), Inches(1.8), Inches(5.6), Inches(2.7),
        "Linh — Execution Specialist",
        [
            "Tier 1 (Ordinary Chef): chef uniform, normal speed, no extra arm.",
            "Tier 2 (Workshop Engineer): ONE rustic brass third arm, ~8% faster.",
            "Tier 3 (Precision Engineer): ONE sleek white & blue third arm, ~15% faster.",
            "Unlocks: Tier 2 after Day 3, Tier 3 after Day 5."
        ],
        C_TEAL
    )
    # 3 Linh progression stages (consistent deck illustrations)
    p_l1 = "docs/deck_art/linh_tier1.jpg"
    p_l2 = "docs/deck_art/linh_tier2.jpg"
    p_l3 = "docs/deck_art/linh_tier3.jpg"
    if os.path.exists(p_l1):
        add_fit(s10, p_l1, Inches(1.0), Inches(4.7), Inches(1.6), Inches(2.1))
    if os.path.exists(p_l2):
        add_fit(s10, p_l2, Inches(2.8), Inches(4.7), Inches(1.6), Inches(2.1))
    if os.path.exists(p_l3):
        add_fit(s10, p_l3, Inches(4.6), Inches(4.7), Inches(1.6), Inches(2.1))

    add_card(s10, Inches(6.8), Inches(1.8), Inches(5.6), Inches(2.7),
        "Jiwon — \"Call in the Crew\"",
        [
            "Signature Ability: Spends restaurant coins to hire part-time prep helpers.",
            "Tier 1 (Burnt-out Exec): casual clothes + kitchen apron, 0 helpers.",
            "Tier 2 (Manager): rolled-sleeve shirt, 1 helper | Tier 3 (Business Operator): suit & tie, 2 helpers. One Part-Timer button [H] hires every free slot at once.",
            "Balancing: 20 coins hire cost, 20s contract, 45s agency cooldown."
        ],
        C_TERRA
    )
    # 3 Jiwon progression stages (prefer consistent deck illustrations when available)
    def _pick(deck_name, sprite_name):
        dp = f"docs/deck_art/{deck_name}.jpg"
        return dp if os.path.exists(dp) else f"unity/TwoLadles/Two Ladles/Assets/Resources/Sprites/Characters/{sprite_name}.png"
    p_j1 = _pick("jiwon_tier1", "jiwon_tier1")
    p_j2 = _pick("jiwon_tier2", "jiwon_tier2")
    p_j3 = _pick("jiwon_tier3", "jiwon_tier3")
    if os.path.exists(p_j1):
        add_fit(s10, p_j1, Inches(7.0), Inches(4.7), Inches(1.6), Inches(2.1))
    if os.path.exists(p_j2):
        add_fit(s10, p_j2, Inches(8.8), Inches(4.7), Inches(1.6), Inches(2.1))
    if os.path.exists(p_j3):
        add_fit(s10, p_j3, Inches(10.6), Inches(4.7), Inches(1.6), Inches(2.1))

    # ==========================================
    # SLIDE 11: CAMPAIGN & ENDLESS MODE
    # ==========================================
    s11 = prs.slides.add_slide(blank_layout)
    set_slide_background(s11, C_DARK)
    add_header(s11, "Game Modes", "5-Day Campaign & Endless Rush Mode")

    add_card(s11, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "5-Day Campaign Progression",
        [
            "Day 1: Tteokbokki only — learn chop, cook and serve.",
            "Day 2: adds Bột Chiên (cook flour, add egg, cook again).",
            "Day 3: adds Korean BBQ on Rice. Clearing it unlocks Character Tier 2!",
            "Day 4: adds Vietnamese Cơm Tấm — all four dishes on the menu.",
            "Day 5: Bulgogi Bánh Mì 3-minute high-score finale. Clearing it unlocks Character Tier 3!"
        ],
        C_TEAL
    )

    add_card(s11, Inches(6.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "Endless Rush Mode",
        [
            "Continuous Escalation: Unlimited shift duration toggled in lobby.",
            "Dynamic Difficulty: Order spawn interval accelerates from 14s to 6.5s; order concurrency expands from 2 to 4.",
            "Failure Condition: 3 missed customer tickets ends shift.",
            "Personal Best Tracking: Persistent career high score, orders served, and survival duration.",
            "Kitchen Upgrades: Sharp Santoku Knives, Chef Speed Boots, Turbo Wok Burners."
        ],
        C_GOLD
    )

    # ==========================================
    # SLIDE 12: TECHNICAL ARCHITECTURE & UNITY MIGRATION
    # ==========================================
    s12 = prs.slides.add_slide(blank_layout)
    set_slide_background(s12, C_DARK)
    add_header(s12, "Technology & Architecture", "Modern Unity C# Stack Targeting Android & PC")

    add_card(s12, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "Unity Engine Architecture",
        [
            "Engine: Unity 2022.3.62f3 LTS, statically typed C#. Pixel-art sprites on a flat 2D floor with a 2.5D orthographic camera.",
            "Cross-Platform: Landscape Android APK + Windows PC (Windows host and Android client can play together).",
            "Authoritative Host Topology: Peer 1 governs order timers, cooking transitions, pass counter handoffs, flow, and scoring.",
            "Dual Input Schemes: Responsive Android touch D-pad & action buttons + PC WASD/Arrow & Space/E/F keyboard controls.",
            "Persistent Storage: JSON save/load (Application.persistentDataPath)."
        ],
        C_TEAL
    )

    add_card(s12, Inches(6.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "LAN Multiplayer & Netcode",
        [
            "Transport: raw .NET UdpClient sockets, no third-party networking library.",
            "Ports: UDP 24567 gameplay sync (30Hz movement, 4Hz host state) + UDP 24568 LAN room discovery.",
            "Character Exclusivity: Mutual exclusion enforced in lobby (Linh vs Jiwon).",
            "Readiness Reset: Changing character or perks resets Ready state to prevent race conditions.",
            "Wi-Fi Scan: auto-finds the host on the same Wi-Fi; START needs 2 chefs, both READY.",
            "Unit tests: 19 suites, 261/261 assertions passed (2026-11-10).",
            "Full-gameplay QA (separate): 2-bot autoplay with selectable test day and all upgrades."
        ],
        C_TERRA
    )

    # ==========================================
    # SLIDE 13: PRODUCTION ROADMAP
    # ==========================================
    s13 = prs.slides.add_slide(blank_layout)
    set_slide_background(s13, C_DARK)
    add_header(s13, "Production Roadmap", "Current Status & Next Development Milestones")

    add_card(s13, Inches(0.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Milestones 0–3 (Verified)",
        [
            "✔ 5-Day Menu & Cross-Prep Interdependence.",
            "✔ Self-Assembly Holding Tables & Throwing.",
            "✔ Pass Counter 4s Handoff & Anti-Exploit (+15 Flow).",
            "✔ Rush Rhythm (15s, 1.5x score).",
            "✔ Linh Tiers 1-3 & Jiwon Crew Hiring.",
            "✔ 5-Day Campaign & Endless Mode.",
            "✔ Unit tests: 19 suites, 233/233 passed.",
            "✔ 2-Bot Autoplay QA + Solo AI partner."
        ],
        C_TEAL,
        title_size=15,
        body_size=11.5
    )

    add_card(s13, Inches(4.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Milestone 4 (Visual & Audio)",
        [
            "✔ 2D pixel-art chef & food sprites integrated (beef, noodles, flour added).",
            "✔ Flat 2D kitchen floor, depth-sorted chefs, 15° walk sway.",
            "✔ Lobby, cleaned-up HUD & station prompts.",
            "✔ Story phone chats & day-night lighting.",
            "✔ Upbeat bistro music & kitchen SFX (code-synthesized).",
            "✔ Touch controls for Android."
        ],
        C_GOLD,
        title_size=15,
        body_size=11.5
    )

    add_card(s13, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Milestone 5 (Competition)",
        [
            "✔ Android APK build (TwoLadles.apk).",
            "✔ Windows Standalone PC build (TwoLadles.exe).",
            "✔ Two-Bot Autoplay QA Test Runner & Metrics.",
            "• Gameplay demo video capture.",
            "• Final LAN + Windows/Android cross-play check.",
            "• Final submission to Korea Game Week 2026."
        ],
        C_TERRA,
        title_size=15,
        body_size=11.5
    )

    # ==========================================
    # SLIDE 14: SUBMISSION INFORMATION
    # ==========================================
    s14 = prs.slides.add_slide(blank_layout)
    set_slide_background(s14, C_DARK)
    add_header(s14, "Submission Details", "Korea Game Week 2026 Entry Information")

    add_card(s14, Inches(0.8), Inches(1.8), Inches(11.6), Inches(4.8),
        "Official Competition Submission",
        [
            "Game Title: TWO LADLES: A Vietnam–Korea Kitchen Story",
            "Email Recipient: btc.koreagameweek@gmail.com",
            "Subject Line: [Dự thi Lập trình game KGW2026]",
            "Concept Document: docs/two_ladles_concept.pptx (PowerPoint Format - Included)",
            "Game Engine: Unity C# (Android Platform + PC Testable)",
            "Deliverables: Presentation Deck (*.pptx) + Playable APK / PC Build + Demo Video",
            "Copyright Statement: The game features original gameplay systems and development work. All third-party assets, where applicable, are used in accordance with their respective licenses. See Asset Credits (next slide)."
        ],
        C_GOLD
    )

    # ==========================================
    # SLIDE 15: ASSET CREDITS
    # ==========================================
    s15 = prs.slides.add_slide(blank_layout)
    set_slide_background(s15, C_DARK)
    add_header(s15, "Asset Credits", "Third-Party Assets & Sources")

    add_card(s15, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "Third-Party Art (Licensed)",
        [
            "Professional Kitchen Set (32x32) by 0_mem0ry — 0-mem0ry.itch.io/professional-kitchen-set-free (free, commercial use & modification allowed).",
            "Pixel Mart by ghostpixxells — ghostpixxells.itch.io/pixel-mart (free, commercial use permitted).",
            "Additional food and customer sprites: see docs/ASSET_LICENSES.md for sources and licenses."
        ],
        C_TEAL,
        body_size=12
    )

    add_card(s15, Inches(6.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "Original & Traditional",
        [
            "In-game chef sprites: created for Two Ladles.",
            "Deck character illustrations: AI-assisted concept art made for this presentation.",
            "Gameplay code & systems: written for Two Ladles in Unity C#.",
            "Recipes: Tteokbokki, Bột Chiên, Korean BBQ and Cơm Tấm are traditional dishes; Bulgogi Bánh Mì is a well-known fusion style used as our finale.",
            "Engine: Unity 2022.3 (Unity Personal license)."
        ],
        C_GOLD,
        body_size=12
    )

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    prs.save(output_path)
    print(f"Presentation saved successfully to {output_path} (Total Slides: {len(prs.slides)})")

if __name__ == "__main__":
    create_deck()
