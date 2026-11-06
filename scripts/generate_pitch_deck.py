import os
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.enum.text import PP_ALIGN
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE

def create_deck(output_path="docs/two_ladles_concept.pptx"):
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

    def add_card(slide, left, top, width, height, title, items, accent_color=None):
        card = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
        card.fill.solid()
        card.fill.fore_color.rgb = C_CARD
        card.line.color.rgb = accent_color if accent_color else RGBColor(55, 65, 80)
        card.line.width = Pt(1.5)

        tb = slide.shapes.add_textbox(left + Inches(0.2), top + Inches(0.2), width - Inches(0.4), height - Inches(0.4))
        tf = tb.text_frame
        tf.word_wrap = True

        p0 = tf.paragraphs[0]
        p0.text = title
        p0.font.size = Pt(16)
        p0.font.bold = True
        p0.font.color.rgb = accent_color if accent_color else C_GOLD
        p0.space_after = Pt(10)

        for it in items:
            p = tf.add_paragraph()
            p.text = f"• {it}"
            p.font.size = Pt(13)
            p.font.color.rgb = C_WHITE
            p.space_after = Pt(6)

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
    char_linh = "unity/TwoLadles/Assets/Sprites/Characters/linh_tier2.png"
    char_jiwon = "unity/TwoLadles/Assets/Sprites/Characters/jiwon_tier2.png"
    if os.path.exists(char_linh):
        s1.shapes.add_picture(char_linh, Inches(8.6), Inches(1.6), height=Inches(4.8))
    if os.path.exists(char_jiwon):
        s1.shapes.add_picture(char_jiwon, Inches(10.7), Inches(1.6), height=Inches(4.8))

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
            "Zero AI Teammates / Hot-Seat: Teamwork and communication are mandatory mechanics, not optional add-ons.",
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
            "Clean IP Ownership: Fully handcrafted codebase, custom systems, and original recipes.",
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
            "Playstyle: Process optimization, ingredient cadence mastery, and noodle blanching / broth timing.",
            "Home Base: West Zone (Proteins, Starches, Phở Stockpot, Blancher, Assembly & Serving Window).",
            "Signature Ability: Execution Specialist — unlocks speed multipliers (1.0x -> 0.92x -> 0.85x) with robotic arm evolution."
        ],
        C_TEAL
    )
    if os.path.exists(char_linh):
        s3.shapes.add_picture(char_linh, Inches(5.2), Inches(2.0), height=Inches(4.6))

    add_card(s3, Inches(6.8), Inches(1.8), Inches(4.3), Inches(5.0),
        "Jiwon — The Operations Leader",
        [
            "Nationality: Korean | Former Career: Corporate Business Executive.",
            "Background: Left corporate boardroom exhaustion for a sabbatical in Vietnam; partnered with Linh to build a food venture.",
            "Playstyle: Service pacing, delegation, grill/wok heat management, and dish inventory.",
            "Home Base: East Zone (Vegetables, Slaw, Fish Cakes, Grill/Sear Pan, Gochujang Wok, Washing Sink).",
            "Signature Ability: \"Call in the Crew\" — spends coins to hire temporary helpers (Prep Assistant, Dishwasher)."
        ],
        C_TERRA
    )
    if os.path.exists(char_jiwon):
        s3.shapes.add_picture(char_jiwon, Inches(11.2), Inches(2.0), height=Inches(4.6))

    # ==========================================
    # SLIDE 4: CROSS-PREP KITCHEN INTERDEPENDENCE
    # ==========================================
    s4 = prs.slides.add_slide(blank_layout)
    set_slide_background(s4, C_DARK)
    add_header(s4, "Spatial Topology", "Asymmetric Kitchen Layout: Built-In Mutual Reliance")

    add_card(s4, Inches(0.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "West Zone (Linh)",
        [
            "Raw Meat & Starch Crates (Pork Belly, Bulgogi, Rice Cakes, Beef, Noodles, Baguettes).",
            "West Preparation Counter (discrete 4-strike cadence).",
            "Dedicated Phở Stockpot & Noodle Blancher.",
            "Assembly West & Serving Counter."
        ],
        C_TEAL
    )

    add_card(s4, Inches(4.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Center Pass Counter",
        [
            "3-Slot Shared Handoff Station connecting West and East.",
            "Coordinated Handoff Bonus: +15 Kitchen Flow when partner retrieves item within 4 seconds.",
            "Anti-Exploit Protection: self-retrieval gives 0 Flow; max 1 bonus per order.",
            "Vessel Rack & Return Tray."
        ],
        C_GOLD
    )

    add_card(s4, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "East Zone (Jiwon)",
        [
            "Vegetable, Sauce & Fish Cake Crates (Daikon, Carrot, Kimchi, Gochujang, Eomuk).",
            "East Preparation Counter.",
            "Cast-Iron Grill/Sear Pan & Gochujang Simmer Wok.",
            "Washing Sink & 4-Plate Shared Pool."
        ],
        C_TERRA
    )

    # ==========================================
    # SLIDE 5: AUTHENTIC STARTER MENU
    # ==========================================
    s5 = prs.slides.add_slide(blank_layout)
    set_slide_background(s5, C_DARK)
    add_header(s5, "Culinary Authenticity", "4 Authentic Traditional & Original Fusion Recipes")

    add_card(s5, Inches(0.8), Inches(1.8), Inches(5.6), Inches(2.4),
        "1. Phở Bò (Traditional Vietnamese)",
        [
            "Strict authentic sequence: Rice noodles blanched separately from beef.",
            "Assembly rule: Broth poured last over blanched noodles and sliced beef in a Bowl.",
            "Base Score: 120 pts | Patience: 65s."
        ],
        C_TEAL
    )

    add_card(s5, Inches(6.8), Inches(1.8), Inches(5.6), Inches(2.4),
        "2. Bánh Mì Thịt (Traditional Vietnamese)",
        [
            "Linh portions pork -> passes to Jiwon to grill.",
            "Jiwon preps pickled daikon/carrot -> passes to Linh to assemble on Baguette.",
            "Base Score: 110 pts | Patience: 60s."
        ],
        C_TEAL
    )

    add_card(s5, Inches(0.8), Inches(4.5), Inches(5.6), Inches(2.4),
        "3. Tteokbokki (Traditional Korean)",
        [
            "Linh portions rice cakes -> passes to Jiwon.",
            "Jiwon preps fish cakes & simmers both in Gochujang Wok -> plates in a Bowl.",
            "Base Score: 115 pts | Patience: 60s."
        ],
        C_TERRA
    )

    add_card(s5, Inches(6.8), Inches(4.5), Inches(5.6), Inches(2.4),
        "4. Bulgogi Bánh Mì (Original Fusion Specialty)",
        [
            "Original Vietnam–Korea Fusion: Marinated bulgogi beef seared by Jiwon + seasoned kimchi slaw assembled on Linh's crisp Vietnamese baguette.",
            "High Coordination: Base Score 150 pts | Patience: 70s."
        ],
        C_GOLD
    )

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
            "Assembly Counter: Vessel-first recipe combination (Plate or Bowl required before food).",
            "Rejects invalid pairings with clear sound/visual feedback without destroying items.",
            "Pass Counter: Central 3-slot relay counter.",
            "4-Second Handoff Window: Rapid cross-kitchen coordination rewarded with team Flow."
        ],
        C_GOLD
    )

    add_card(s6, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Serving & Washing",
        [
            "Serving Window: Matches completed dish against active ticket queue.",
            "Patience Tip Bonus: Fast service yields higher restaurant tips.",
            "Finite Vessel Economy: Kitchen begins with pool of 4 clean vessels.",
            "Washing Station: 4-stroke scrub cycle cleans dirty plate and returns it to shared clean stack."
        ],
        C_TERRA
    )

    # ==========================================
    # SLIDE 7: KITCHEN FLOW & RUSH RHYTHM
    # ==========================================
    s7 = prs.slides.add_slide(blank_layout)
    set_slide_background(s7, C_DARK)
    add_header(s7, "Momentum Engine", "Kitchen Flow (0–100) & High-Scoring Rush Rhythm")

    add_card(s7, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.0),
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

    add_card(s7, Inches(6.8), Inches(1.8), Inches(5.6), Inches(5.0),
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
    # SLIDE 8: CHARACTER SIGNATURE ABILITIES
    # ==========================================
    s8 = prs.slides.add_slide(blank_layout)
    set_slide_background(s8, C_DARK)
    add_header(s8, "Character Abilities", "Distinct Thematic Progression Across 3 Evolution Tiers")

    add_card(s8, Inches(0.8), Inches(1.8), Inches(5.6), Inches(2.7),
        "Linh — Execution Specialist",
        [
            "Tier 1 (Ordinary Chef): 1.0x task cadence, no mechanical arm.",
            "Tier 2 (Workshop Engineer): Rustic mechanical arm, 0.92x prep interval.",
            "Tier 3 (Precision Engineer): Sleek dual robotic arms, 0.85x prep interval.",
            "Design Balance: Visual evolution reflecting software automation background."
        ],
        C_TEAL
    )
    # 3 Linh progression stages
    p_l1 = "unity/TwoLadles/Assets/Sprites/Characters/linh_tier1.png"
    p_l2 = "unity/TwoLadles/Assets/Sprites/Characters/linh_tier2.png"
    p_l3 = "unity/TwoLadles/Assets/Sprites/Characters/linh_tier3.png"
    if os.path.exists(p_l1):
        s8.shapes.add_picture(p_l1, Inches(1.1), Inches(4.7), height=Inches(2.1))
    if os.path.exists(p_l2):
        s8.shapes.add_picture(p_l2, Inches(2.7), Inches(4.7), height=Inches(2.1))
    if os.path.exists(p_l3):
        s8.shapes.add_picture(p_l3, Inches(4.4), Inches(4.7), height=Inches(2.1))

    add_card(s8, Inches(6.8), Inches(1.8), Inches(5.6), Inches(2.7),
        "Jiwon — \"Call in the Crew\"",
        [
            "Signature Ability: Spends restaurant currency to hire temporary helpers.",
            "Tier 1 (Burnout Exec): 0 helper slots | Tier 2 (Manager): 1 helper slot.",
            "Tier 3 (Executive Director): 2 simultaneous helper slots unlocked.",
            "Balancing: 20 coins hire cost, 20s contract, 45s agency cooldown."
        ],
        C_TERRA
    )
    # 3 Jiwon progression stages
    p_j1 = "unity/TwoLadles/Assets/Sprites/Characters/jiwon_tier1.png"
    p_j2 = "unity/TwoLadles/Assets/Sprites/Characters/jiwon_tier2.png"
    p_j3 = "unity/TwoLadles/Assets/Sprites/Characters/jiwon_tier3.png"
    if os.path.exists(p_j1):
        s8.shapes.add_picture(p_j1, Inches(7.2), Inches(4.7), height=Inches(2.1))
    if os.path.exists(p_j2):
        s8.shapes.add_picture(p_j2, Inches(8.9), Inches(4.7), height=Inches(2.1))
    if os.path.exists(p_j3):
        s8.shapes.add_picture(p_j3, Inches(10.6), Inches(4.7), height=Inches(2.1))

    # ==========================================
    # SLIDE 9: CAMPAIGN & ENDLESS MODE
    # ==========================================
    s9 = prs.slides.add_slide(blank_layout)
    set_slide_background(s9, C_DARK)
    add_header(s9, "Game Modes", "5 Handcrafted Campaign Levels & Endless Rush Mode")

    add_card(s9, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "5-Level Campaign Progression",
        [
            "Level 1: First Customer (Rural Food Stand) — Phở & Bánh Mì basics (2 orders, 16s pacing).",
            "Level 2: Lunch Rush (Village Market) — introduces dirty plate washing under pressure (3 orders, 13s pacing).",
            "Level 3: Night Market — Korean Tteokbokki added. Completing unlocks Character Tier 2!",
            "Level 4: Fusion Special — Bulgogi Bánh Mì fusion dish added (4 orders, 10s pacing).",
            "Level 5: Grand Opening (Modern Fusion Restaurant) — all 4 dishes; unlocks Character Tier 3!"
        ],
        C_TEAL
    )

    add_card(s9, Inches(6.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "Endless Rush Mode",
        [
            "Continuous Escalation: Unlimited shift duration toggled in lobby.",
            "Dynamic Difficulty: Order spawn interval accelerates from 14s to 6.5s; order concurrency expands from 2 to 4.",
            "Failure Condition: 3 missed customer tickets ends shift.",
            "Personal Best Tracking: Persistent career high score, orders served, and survival duration.",
            "Kitchen Upgrades: Sharp Santoku Knives (+15% prep window), Double-Walled Stockpot (+50% optimal window)."
        ],
        C_GOLD
    )

    # ==========================================
    # SLIDE 10: TECHNICAL ARCHITECTURE & UNITY MIGRATION
    # ==========================================
    s10 = prs.slides.add_slide(blank_layout)
    set_slide_background(s10, C_DARK)
    add_header(s10, "Technology & Architecture", "Modern Unity C# Stack Targeting Android & PC")

    add_card(s10, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "Unity Engine Architecture",
        [
            "Engine: Unity 2022.3 LTS / Unity 6 C#.",
            "Cross-Platform: Landscape Android (1280x720 base viewport) + Windows/Linux PC.",
            "Authoritative Host Topology: Peer 1 governs order timers, cooking transitions, pass counter handoffs, flow, and scoring.",
            "Dual Input Schemes: Responsive Android touch D-pad & action buttons + PC WASD/Arrow & Space/E/F keyboard controls.",
            "Persistent Storage: JSON save/load (Application.persistentDataPath)."
        ],
        C_TEAL
    )

    add_card(s10, Inches(6.8), Inches(1.8), Inches(5.6), Inches(5.0),
        "LAN Multiplayer & Netcode",
        [
            "Multiplayer Transport: Local UDP port 24567.",
            "High-Level Networking: Unity Netcode for GameObjects (NGO) / Unity Transport (UTP).",
            "Character Exclusivity: Mutual exclusion enforced in lobby (Linh vs Jiwon).",
            "Readiness Reset: Changing character or perks resets Ready state to prevent race conditions.",
            "114 Automated Unit Tests: Exhaustive test suite verifying recipes, rhythm, flow, anti-exploit, and abilities."
        ],
        C_TERRA
    )

    # ==========================================
    # SLIDE 11: PRODUCTION ROADMAP
    # ==========================================
    s11 = prs.slides.add_slide(blank_layout)
    set_slide_background(s11, C_DARK)
    add_header(s11, "Production Roadmap", "Current Status & Next Development Milestones")

    add_card(s11, Inches(0.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Milestones 0–3 (Verified)",
        [
            "✔ 4 Authentic Recipes & Cross-Prep Interdependence.",
            "✔ 5 Station Mechanics.",
            "✔ Pass Counter 4s Handoff & Anti-Exploit (+15 Flow).",
            "✔ Rush Rhythm (15s, 1.5x score).",
            "✔ Finite 4-Plate Washing Cycle.",
            "✔ Linh Tiers 1-3 & Jiwon Crew Hiring.",
            "✔ 5 Campaign Levels & Endless Mode.",
            "✔ 114 Automated Tests Passing."
        ],
        C_TEAL
    )

    add_card(s11, Inches(4.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Milestone 4 (Visual & Audio)",
        [
            "• Custom 2D character & food sprites integration.",
            "• Stylized 2.5D visual kitchen rendering.",
            "• Sizzle, blanching, chopping & scrubbing particle effects.",
            "• Vietnamese Dan Bau & Korean Gayageum fusion soundtrack.",
            "• Interactive touch tutorial overlay."
        ],
        C_GOLD
    )

    add_card(s11, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.0),
        "Milestone 5 (Competition)",
        [
            "• Android APK build package for judges.",
            "• Windows Standalone PC executable.",
            "• Gameplay demo video capture.",
            "• Wi-Fi packet jitter stress testing.",
            "• Final submission to Korea Game Week 2026."
        ],
        C_TERRA
    )

    # ==========================================
    # SLIDE 12: SUBMISSION INFORMATION
    # ==========================================
    s12 = prs.slides.add_slide(blank_layout)
    set_slide_background(s12, C_DARK)
    add_header(s12, "Submission Details", "Korea Game Week 2026 Entry Information")

    add_card(s12, Inches(0.8), Inches(1.8), Inches(11.6), Inches(4.8),
        "Official Competition Submission",
        [
            "Game Title: TWO LADLES: A Vietnam–Korea Kitchen Story",
            "Email Recipient: btc.koreagameweek@gmail.com",
            "Subject Line: [Dự thi Lập trình game KGW2026]",
            "Concept Document: docs/two_ladles_concept.pptx (PowerPoint Format - Included)",
            "Game Engine: Unity C# (Android Platform + PC Testable)",
            "Deliverables: Presentation Deck (*.pptx) + Playable APK / PC Build + Demo Video",
            "Copyright Statement: All code, systems, recipes, and presented materials are original works created for Two Ladles and comply fully with KGW2026 copyright regulations."
        ],
        C_GOLD
    )

    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    prs.save(output_path)
    print(f"Presentation saved successfully to {output_path}")

if __name__ == "__main__":
    create_deck()
