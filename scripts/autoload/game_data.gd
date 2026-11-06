extends Node
## Authoritative data registry for all recipes, ingredients, appliances, specializations, and cross-prep rules.
## Implements the Cross-Kitchen Interdependence rule:
## - Linh's West Zone prepares Jiwon's cooking/assembly ingredients.
## - Jiwon's East Zone prepares Linh's cooking/assembly ingredients.

const RecipeData = preload("res://scripts/data/recipe_data.gd")
const CampaignLevelSpec = preload("res://scripts/data/campaign_level_spec.gd")
const CharacterProgressionData = preload("res://scripts/data/character_progression_data.gd")

var ingredients: Dictionary = {}
var appliances: Dictionary = {}
var recipes: Dictionary = {}
var specializations: Dictionary = {}
var campaign_levels: Dictionary = {}
var linh_tiers: Dictionary = {}
var jiwon_tiers: Dictionary = {}
var worker_catalog: Dictionary = {}
var characters: Dictionary = {
	"linh": {
		"id": "linh",
		"display_name": "Linh",
		"nationality": "Vietnamese",
		"background": "Former Software Programmer",
		"home_zone": "linh_west",
		"partner_zone": "jiwon_east",
		"color": Color(0.22, 0.68, 0.58),
		"bio": "Systems-minded optimizer who left tech burnout to reopen her family food stall."
	},
	"jiwon": {
		"id": "jiwon",
		"display_name": "Jiwon",
		"nationality": "Korean",
		"background": "Former Corporate Executive",
		"home_zone": "jiwon_east",
		"partner_zone": "linh_west",
		"color": Color(0.88, 0.42, 0.24),
		"bio": "Operations leader who traded corporate spreadsheets for high-pressure street kitchen service."
	}
}

func _ready() -> void:
	_init_data()

func _init() -> void:
	_init_data()

func _init_data() -> void:
	if not recipes.is_empty():
		return
	_register_specializations()
	_register_ingredients()
	_register_appliances()
	_register_recipes()
	_register_campaign_levels()
	_register_character_tiers()
	_register_worker_catalog()

func _register_specializations() -> void:
	specializations["prep_master"] = RecipeData.SpecializationSpec.new(
		"prep_master",
		"PREP MASTER",
		"Widens Perfect prep cadence window by +25% and grants +15 bonus points on Perfect-prepped dishes.",
		1.25,
		15,
		1.0,
		1.0,
		1.0
	)
	specializations["flame_keeper"] = RecipeData.SpecializationSpec.new(
		"flame_keeper",
		"FLAME KEEPER",
		"Extends Optimal cooking window duration by +35% across all cooking stations.",
		1.0,
		0,
		1.35,
		1.0,
		1.0
	)
	specializations["market_runner"] = RecipeData.SpecializationSpec.new(
		"market_runner",
		"MARKET RUNNER",
		"Increases kitchen movement speed by +20% for rapid Pass Counter handoffs and delivery.",
		1.0,
		0,
		1.0,
		1.20,
		1.0
	)
	specializations["fusion_artist"] = RecipeData.SpecializationSpec.new(
		"fusion_artist",
		"FUSION ARTIST",
		"Grants +25% bonus score on Fusion recipes and multi-component assemblies.",
		1.0,
		0,
		1.0,
		1.0,
		1.25
	)

func _register_ingredients() -> void:
	# --- LINH'S WEST CRATES & PREP (Prepares proteins/starches that Jiwon cooks or uses, plus Pho noodles) ---
	_add_item("beef_raw", "Raw Beef Brisket", "linh_west", "jiwon_east", "beef_raw", "beef_sliced", "", "", Color(0.75, 0.22, 0.22))
	_add_item("beef_sliced", "Portioned Beef Slices", "linh_west", "jiwon_east", "beef_raw", "beef_sliced", "", "", Color(0.85, 0.32, 0.32))

	_add_item("noodles_raw", "Dry Rice Noodles", "linh_west", "linh_west", "noodles_raw", "noodles_raw", "noodles_blanched", "noodle_blancher", Color(0.95, 0.93, 0.85))
	_add_item("noodles_blanched", "Blanched Rice Noodles", "linh_west", "linh_west", "noodles_raw", "noodles_raw", "noodles_blanched", "noodle_blancher", Color(0.98, 0.96, 0.90))

	_add_item("pork_raw", "Raw Pork Belly", "linh_west", "jiwon_east", "pork_raw", "pork_portioned", "pork_grilled", "grill_pan", Color(0.88, 0.48, 0.45))
	_add_item("pork_portioned", "Portioned Pork Belly (Pass to Jiwon -> Grill)", "linh_west", "jiwon_east", "pork_raw", "pork_portioned", "pork_grilled", "grill_pan", Color(0.92, 0.55, 0.50))
	_add_item("pork_grilled", "Grilled Pork Belly (Pass to Linh -> Assemble)", "jiwon_east", "linh_west", "pork_raw", "pork_portioned", "pork_grilled", "grill_pan", Color(0.68, 0.34, 0.18))

	_add_item("tteok_raw", "Cylinder Rice Cakes", "linh_west", "jiwon_east", "tteok_raw", "tteok_portioned", "tteokbokki_simmered", "simmer_wok", Color(0.96, 0.94, 0.92))
	_add_item("tteok_portioned", "Portioned Rice Cakes (Pass to Jiwon -> Wok)", "linh_west", "jiwon_east", "tteok_raw", "tteok_portioned", "tteokbokki_simmered", "simmer_wok", Color(0.98, 0.95, 0.90))

	_add_item("bulgogi_raw", "Raw Marinated Bulgogi", "linh_west", "jiwon_east", "bulgogi_raw", "bulgogi_portioned", "bulgogi_seared", "grill_pan", Color(0.62, 0.20, 0.18))
	_add_item("bulgogi_portioned", "Portioned Bulgogi Beef (Pass to Jiwon -> Sear)", "linh_west", "jiwon_east", "bulgogi_raw", "bulgogi_portioned", "bulgogi_seared", "grill_pan", Color(0.70, 0.25, 0.20))
	_add_item("bulgogi_seared", "Seared Bulgogi Beef (Pass to Linh -> Assemble)", "jiwon_east", "linh_west", "bulgogi_raw", "bulgogi_portioned", "bulgogi_seared", "grill_pan", Color(0.48, 0.22, 0.14))

	_add_item("baguette", "Crispy Baguette", "linh_west", "linh_west", "baguette", "baguette", "", "", Color(0.86, 0.66, 0.36))
	_add_item("broth_hot", "Simmered Phở Broth", "linh_west", "linh_west", "broth_raw", "broth_raw", "broth_hot", "stockpot", Color(0.82, 0.58, 0.24))

	# --- JIWON'S EAST CRATES & PREP (Prepares aromatics/pickles/slaw/fish cakes for Linh & shared dishes) ---
	_add_item("daikon_carrot_raw", "Raw Daikon & Carrot", "jiwon_east", "linh_west", "daikon_carrot_raw", "pickled_veg", "", "", Color(0.95, 0.58, 0.22))
	_add_item("pickled_veg", "Pickled Daikon & Carrot (Pass to Linh -> Assemble)", "jiwon_east", "linh_west", "daikon_carrot_raw", "pickled_veg", "", "", Color(0.98, 0.72, 0.35))

	_add_item("kimchi_raw", "Aged Napa Kimchi", "jiwon_east", "linh_west", "kimchi_raw", "kimchi_slaw", "", "", Color(0.85, 0.24, 0.16))
	_add_item("kimchi_slaw", "Prepared Kimchi Slaw (Pass to Linh -> Assemble)", "jiwon_east", "linh_west", "kimchi_raw", "kimchi_slaw", "", "", Color(0.92, 0.32, 0.20))

	_add_item("eomuk_raw", "Raw Fish Cake Sheets", "jiwon_east", "linh_west", "eomuk_raw", "eomuk_prepared", "tteokbokki_simmered", "simmer_wok", Color(0.88, 0.76, 0.54))
	_add_item("eomuk_prepared", "Prepared Fish Cakes", "jiwon_east", "linh_west", "eomuk_raw", "eomuk_prepared", "tteokbokki_simmered", "simmer_wok", Color(0.92, 0.80, 0.58))

	_add_item("gochujang_sauce", "Gochujang Chili Paste", "jiwon_east", "jiwon_east", "gochujang_sauce", "gochujang_sauce", "tteokbokki_simmered", "simmer_wok", Color(0.76, 0.12, 0.10))
	_add_item("tteokbokki_simmered", "Simmered Tteokbokki", "jiwon_east", "linh_west", "", "", "tteokbokki_simmered", "simmer_wok", Color(0.88, 0.22, 0.14))

	# --- COMPLETED DISHES & WASTE ---
	_add_item("dish_pho_bo", "Phở Bò", "shared", "shared", "", "", "", "", Color(0.90, 0.72, 0.38))
	_add_item("dish_banh_mi_thit", "Bánh Mì Thịt", "shared", "shared", "", "", "", "", Color(0.88, 0.64, 0.30))
	_add_item("dish_tteokbokki", "Tteokbokki", "shared", "shared", "", "", "", "", Color(0.90, 0.24, 0.16))
	_add_item("dish_bulgogi_banh_mi", "Bulgogi Bánh Mì (Fusion)", "shared", "shared", "", "", "", "", Color(0.78, 0.42, 0.24))
	_add_item("burnt_waste", "Burnt Food Waste", "shared", "shared", "", "", "", "", Color(0.18, 0.16, 0.16))

func _add_item(
	p_id: String,
	p_name: String,
	p_origin: String,
	p_target: String,
	p_raw: String,
	p_prep: String,
	p_cooked: String,
	p_appliance: String,
	p_color: Color
) -> void:
	ingredients[p_id] = RecipeData.IngredientSpec.new(
		p_id, p_name, p_origin, p_target, p_raw, p_prep, p_cooked, p_appliance, p_color
	)

func _register_appliances() -> void:
	# 1. Dedicated Stockpot (Linh's West Zone) - maintains hot broth independently from noodle blanching
	appliances["stockpot"] = RecipeData.ApplianceSpec.new(
		"stockpot",
		"Dedicated Phở Stockpot",
		"linh_west",
		3.0,
		12.0,
		8.0,
		["broth_raw"],
		"broth_hot",
		true
	)
	# 2. Noodle Blancher (Linh's West Zone) - blanches rice noodles separately from stock
	appliances["noodle_blancher"] = RecipeData.ApplianceSpec.new(
		"noodle_blancher",
		"Noodle Blanching Basket",
		"linh_west",
		3.5,
		5.0,
		4.0,
		["noodles_raw"],
		"noodles_blanched",
		false
	)
	# 3. Flat Grill / Sear Pan (Jiwon's East Zone) - grills Linh's portioned pork or sears Linh's bulgogi
	appliances["grill_pan"] = RecipeData.ApplianceSpec.new(
		"grill_pan",
		"Cast-Iron Grill & Sear Pan",
		"jiwon_east",
		4.0,
		5.0,
		4.0,
		["pork_portioned", "bulgogi_portioned"],
		"",
		false
	)
	# 4. Simmer Wok (Jiwon's East Zone) - simmers Linh's portioned rice cakes + Jiwon's fish cakes + gochujang
	appliances["simmer_wok"] = RecipeData.ApplianceSpec.new(
		"simmer_wok",
		"Gochujang Simmer Wok",
		"jiwon_east",
		4.5,
		5.5,
		4.5,
		["tteok_portioned", "eomuk_prepared", "gochujang_sauce"],
		"tteokbokki_simmered",
		false
	)

func _register_recipes() -> void:
	# Recipe 1: PHO BO (Traditional Vietnamese)
	# Sequence: 1. Portion beef slices at Prep Counter. 2. Blanch rice noodles separately.
	# 3. Maintain hot stock broth in dedicated stockpot. 4. Add noodles & beef to bowl. 5. Pour hot broth last.
	recipes["pho_bo"] = RecipeData.RecipeSpec.new(
		"pho_bo",
		"Phở Bò",
		"Traditional Vietnamese",
		false,
		"bowl",
		["noodles_blanched", "beef_sliced", "broth_hot"],
		"broth_hot",
		120,
		65.0,
		2,
		"Jiwon washes/passes Bowl & preps Beef -> Linh blanches Noodles & pours Stockpot Broth last."
	)

	# Recipe 2: BANH MI THIT (Traditional Vietnamese)
	# Sequence: 1. Prepare pickled daikon & carrot (Jiwon). 2. Portion pork belly (Linh).
	# 3. Grill pork (Jiwon). 4. Assemble baguette with pork & pickled vegetables (Linh).
	recipes["banh_mi_thit"] = RecipeData.RecipeSpec.new(
		"banh_mi_thit",
		"Bánh Mì Thịt",
		"Traditional Vietnamese",
		false,
		"plate",
		["baguette", "pork_grilled", "pickled_veg"],
		"",
		110,
		60.0,
		1,
		"Linh portions Pork -> passes to Jiwon to Grill; Jiwon preps Pickled Daikon/Carrot -> passes to Linh to assemble on Baguette."
	)

	# Recipe 3: TTEOKBOKKI (Traditional Korean)
	# Sequence: 1. Portion rice cakes (Linh). 2. Prepare fish cakes (Jiwon).
	# 3. Simmer both with gochujang sauce in Wok (Jiwon). 4. Plate in bowl & serve.
	recipes["tteokbokki"] = RecipeData.RecipeSpec.new(
		"tteokbokki",
		"Tteokbokki",
		"Traditional Korean",
		false,
		"bowl",
		["tteokbokki_simmered"],
		"",
		115,
		60.0,
		2,
		"Linh portions Rice Cakes -> passes to Jiwon; Jiwon preps Fish Cakes & simmers both with Gochujang in Wok -> plates in Bowl."
	)

	# Recipe 4: BULGOGI BANH MI (Original Vietnam-Korea Fusion Specialty)
	# Sequence: 1. Portion marinated bulgogi beef (Linh). 2. Prepare kimchi slaw (Jiwon).
	# 3. Sear bulgogi (Jiwon). 4. Assemble baguette, beef & slaw (Linh).
	recipes["bulgogi_banh_mi"] = RecipeData.RecipeSpec.new(
		"bulgogi_banh_mi",
		"Bulgogi Bánh Mì",
		"Original Vietnam–Korea Fusion Specialty",
		true,
		"plate",
		["baguette", "bulgogi_seared", "kimchi_slaw"],
		"",
		150,
		70.0,
		3,
		"Linh portions Marinated Bulgogi -> passes to Jiwon to Sear; Jiwon preps Kimchi Slaw -> passes to Linh to assemble on Baguette."
	)

func get_prep_output(raw_item_id: String) -> String:
	if not ingredients.has(raw_item_id):
		return ""
	var spec: RecipeData.IngredientSpec = ingredients[raw_item_id]
	if spec.prepared_id != "" and spec.prepared_id != raw_item_id:
		return spec.prepared_id
	return ""

func get_grill_output(prepared_item_id: String) -> String:
	if prepared_item_id == "pork_portioned":
		return "pork_grilled"
	if prepared_item_id == "bulgogi_portioned":
		return "bulgogi_seared"
	return ""

func is_cross_partner_handoff(item_id: String, placer_character: String, retriever_character: String) -> bool:
	if placer_character == "" or retriever_character == "":
		return false
	if placer_character == retriever_character:
		return false
	return ingredients.has(item_id)

func _register_campaign_levels() -> void:
	campaign_levels[1] = CampaignLevelSpec.new(
		1,
		"First Customer",
		"Rural Roadside Food Stand",
		"Rural Vietnam",
		180.0,
		150, 300, 500,
		["banh_mi_thit", "pho_bo"],
		2,
		16.0,
		"Open the family food stall! Master basic movement, cross-prep, and initial service."
	)
	campaign_levels[2] = CampaignLevelSpec.new(
		2,
		"Lunch Rush",
		"Village Market Restaurant",
		"Village Market",
		180.0,
		220, 450, 650,
		["banh_mi_thit", "pho_bo"],
		3,
		13.0,
		"Multiple customer orders arrive together. Manage finite plates and scrub dirty dishes at the sink!"
	)
	campaign_levels[3] = CampaignLevelSpec.new(
		3,
		"Night Market",
		"Busy Vietnam–Korea Street Stall",
		"Night Market",
		180.0,
		300, 550, 800,
		["pho_bo", "banh_mi_thit", "tteokbokki"],
		3,
		11.0,
		"Introduce Korean Tteokbokki! Linh portions rice cakes for Jiwon's Wok. Completing unlocks Tier 2!"
	)
	campaign_levels[4] = CampaignLevelSpec.new(
		4,
		"Fusion Special",
		"Growing Fusion Bistro",
		"City Fringe",
		180.0,
		380, 700, 950,
		["bulgogi_banh_mi", "tteokbokki", "banh_mi_thit"],
		4,
		10.0,
		"Introduce Bulgogi Bánh Mì! Maximize Pass Counter handoffs and trigger Rush Rhythm."
	)
	campaign_levels[5] = CampaignLevelSpec.new(
		5,
		"Grand Opening",
		"Prestigious Urban Restaurant",
		"Downtown Metropolis",
		180.0,
		450, 850, 1200,
		["pho_bo", "banh_mi_thit", "tteokbokki", "bulgogi_banh_mi"],
		4,
		8.5,
		"Full menu under peak customer pressure! Combine all stations and abilities. Unlocks Tier 3!"
	)

func _register_character_tiers() -> void:
	linh_tiers[1] = CharacterProgressionData.LinhTierSpec.new(
		1,
		"Ordinary Chef",
		"Standard chef outfit, no robotic components, normal task speed.",
		1.0,
		"none",
		Color(0.22, 0.68, 0.58)
	)
	linh_tiers[2] = CharacterProgressionData.LinhTierSpec.new(
		2,
		"Workshop Engineer",
		"One mechanical third arm made of rustic brass gears, dark steel, and visible joints (~8% faster actions).",
		0.92,
		"rustic_mechanical",
		Color(0.68, 0.52, 0.28),
		Color(0.85, 0.65, 0.3)
	)
	linh_tiers[3] = CharacterProgressionData.LinhTierSpec.new(
		3,
		"Precision Engineer",
		"One mechanical third arm with clean white exterior and glowing blue lines (~15% faster actions).",
		0.85,
		"precision_white_blue",
		Color(0.92, 0.95, 1.0),
		Color(0.2, 0.6, 1.0)
	)

	jiwon_tiers[1] = CharacterProgressionData.JiwonTierSpec.new(
		1,
		"Burnt-out Executive",
		"Practical casual clothes, kitchen apron, former corporate executive. 0 part-timer slots.",
		0,
		"casual_apron",
		Color(0.88, 0.42, 0.24)
	)
	jiwon_tiers[2] = CharacterProgressionData.JiwonTierSpec.new(
		2,
		"Restaurant Manager",
		"Crisp dress shirt with rolled sleeves, no suit/tie. 1 part-timer slot.",
		1,
		"rolled_dress_shirt",
		Color(0.92, 0.55, 0.32)
	)
	jiwon_tiers[3] = CharacterProgressionData.JiwonTierSpec.new(
		3,
		"Business Operator",
		"Formal executive business suit with tie and briefcase. 2 simultaneous part-timer slots.",
		2,
		"business_suit",
		Color(0.22, 0.28, 0.42)
	)

func _register_worker_catalog() -> void:
	worker_catalog["prep_assistant"] = CharacterProgressionData.WorkerSpec.new(
		"prep_assistant",
		"Prep Assistant",
		"Assists at designated Prep Counter by performing steady chop strikes.",
		20,
		20.0,
		45.0,
		"prep_station",
		1.1,
		Color(0.35, 0.75, 0.45)
	)
	worker_catalog["dishwasher"] = CharacterProgressionData.WorkerSpec.new(
		"dishwasher",
		"Dishwasher",
		"Works at Washing Station to scrub dirty plates into clean dishes.",
		20,
		20.0,
		45.0,
		"washing_station",
		1.0,
		Color(0.28, 0.65, 0.90)
	)

