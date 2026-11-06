extends Node
## Progression & Upgrade Manager for Two Ladles.
## Manages persistent restaurant reputation, coins, stage unlocks (1 to 8),
## appliance upgrades, and specialization talent upgrades.
## Saves and loads from `user://progression.json`.

signal progression_updated()
signal upgrade_purchased(upgrade_id: String)
signal stage_unlocked(stage_id: int)

var save_path: String = "user://progression.json"

func _init() -> void:
	_detect_save_path()

func _detect_save_path() -> void:
	var test_file: FileAccess = FileAccess.open("user://.write_test", FileAccess.WRITE)
	if test_file != null:
		test_file.close()
		DirAccess.remove_absolute(ProjectSettings.globalize_path("user://.write_test"))
		save_path = "user://progression.json"
	else:
		save_path = ProjectSettings.globalize_path("res://.user/progression.json")

# Fictional prestigious culinary rating system (IP-safe)
const RATING_TIER_NAMES: Dictionary = {
	1: "Rural Food Stand",
	2: "Village Market Restaurant",
	3: "Night Market Stall",
	4: "Street Food Market",
	5: "Modern Fusion Restaurant",
	6: "Upscale City Bistro",
	7: "Fine-Dining Restaurant",
	8: "Golden Ladle Three-Star Prestige"
}

var coins: int = 150 # Starting seed currency
var total_career_score: int = 0
var highest_unlocked_stage: int = 1
var selected_stage: int = 1

# Character Tiers (1 to 3)
var linh_tier: int = 1
var jiwon_tier: int = 1

# Endless Mode Records
var endless_best_score: int = 0
var endless_best_orders: int = 0
var endless_best_duration: float = 0.0

# Best scores and stars per stage (stage_id: int -> Dictionary)
var stage_records: Dictionary = {
	1: {"stars": 0, "best_score": 0, "completed": false},
	2: {"stars": 0, "best_score": 0, "completed": false},
	3: {"stars": 0, "best_score": 0, "completed": false},
	4: {"stars": 0, "best_score": 0, "completed": false},
	5: {"stars": 0, "best_score": 0, "completed": false},
	6: {"stars": 0, "best_score": 0, "completed": false},
	7: {"stars": 0, "best_score": 0, "completed": false},
	8: {"stars": 0, "best_score": 0, "completed": false},
}

# Purchasable Kitchen Upgrades (data-driven)
var catalog_upgrades: Dictionary = {
	"sharp_santoku_knives": {
		"id": "sharp_santoku_knives",
		"display_name": "Sharp Santoku Knife Set",
		"cost": 100,
		"description": "Widens Prep Counter cadence sweet-spot by +15% for both chefs.",
		"purchased": false,
		"effect_type": "prep_window",
		"value": 1.15
	},
	"cast_iron_seasoning": {
		"id": "cast_iron_seasoning",
		"display_name": "Pre-Seasoned Cast Iron Stoves",
		"cost": 150,
		"description": "Reduces cooking heat-up time by 15% across all appliances.",
		"purchased": false,
		"effect_type": "cook_speed",
		"value": 0.85
	},
	"extended_pass_counter": {
		"id": "extended_pass_counter",
		"display_name": "Widened Brass Pass Counter",
		"cost": 200,
		"description": "Increases Pass Counter capacity from 3 to 4 slots.",
		"purchased": false,
		"effect_type": "pass_slots",
		"value": 4
	},
	"insulated_broth_pot": {
		"id": "insulated_broth_pot",
		"display_name": "Double-Walled Phở Stockpot",
		"cost": 250,
		"description": "Stockpot remains in OPTIMAL window for +50% longer duration.",
		"purchased": false,
		"effect_type": "stockpot_window",
		"value": 1.50
	}
}

func _ready() -> void:
	_detect_save_path()
	load_progression()

func record_shift_results(stage_id: int, summary: Dictionary) -> Dictionary:
	var score: int = int(summary.get("total_score", 0))
	var stars: int = int(summary.get("stars", 1))
	var handoffs: int = int(summary.get("handoffs_completed", 0))
	var rush_triggers: int = int(summary.get("rush_rhythms_triggered", 0))

	# Earnings formula: base score / 8 + tip bonuses for team coordination
	var earned_coins: int = int(floor(float(score) / 8.0)) + (handoffs * 5) + (rush_triggers * 20)
	coins += earned_coins
	total_career_score += score

	if not stage_records.has(stage_id):
		stage_records[stage_id] = {"stars": 0, "best_score": 0, "completed": false}

	var rec: Dictionary = stage_records[stage_id]
	rec["completed"] = true
	if score > int(rec.get("best_score", 0)):
		rec["best_score"] = score
	if stars > int(rec.get("stars", 0)):
		rec["stars"] = stars
	stage_records[stage_id] = rec

	# Unlock next stage if 2 or more stars achieved and next stage exists
	var newly_unlocked: bool = false
	if stars >= 2 and stage_id < 8 and stage_id >= highest_unlocked_stage:
		highest_unlocked_stage = stage_id + 1
		newly_unlocked = true
		stage_unlocked.emit(highest_unlocked_stage)

	# Character Progression Unlocks based on campaign progress:
	# - Tier 2: Unlock after completing Level 3
	# - Tier 3: Unlock after completing Level 5
	var tier_upgraded: bool = false
	if stage_id >= 3 and stars >= 1 and linh_tier < 2:
		linh_tier = 2
		jiwon_tier = 2
		tier_upgraded = true
	if stage_id >= 5 and stars >= 1 and linh_tier < 3:
		linh_tier = 3
		jiwon_tier = 3
		tier_upgraded = true

	save_progression()
	progression_updated.emit()

	return {
		"earned_coins": earned_coins,
		"new_balance": coins,
		"stage_stars": rec["stars"],
		"best_score": rec["best_score"],
		"newly_unlocked": newly_unlocked,
		"next_stage": highest_unlocked_stage,
		"tier_upgraded": tier_upgraded,
		"linh_tier": linh_tier,
		"jiwon_tier": jiwon_tier,
	}

func record_endless_results(score: int, orders_served: int, duration_sec: float) -> Dictionary:
	var new_high: bool = (score > endless_best_score)
	if new_high:
		endless_best_score = score
		endless_best_orders = orders_served
		endless_best_duration = duration_sec

	var earned_coins: int = int(floor(float(score) / 7.0))
	coins += earned_coins
	total_career_score += score
	save_progression()
	progression_updated.emit()

	return {
		"score": score,
		"orders_served": orders_served,
		"duration_sec": duration_sec,
		"new_high_score": new_high,
		"best_score": endless_best_score,
		"earned_coins": earned_coins,
		"total_coins": coins,
	}

func purchase_upgrade(upgrade_id: String) -> Dictionary:
	if not catalog_upgrades.has(upgrade_id):
		return {"ok": false, "reason": "Unknown upgrade ID."}

	var up: Dictionary = catalog_upgrades[upgrade_id]
	if bool(up.get("purchased", false)):
		return {"ok": false, "reason": "%s is already owned!" % up.get("display_name", "")}

	var cost: int = int(up.get("cost", 0))
	if coins < cost:
		return {
			"ok": false,
			"reason": "Not enough coins! Need %d coins (You have %d)." % [cost, coins]
		}

	coins -= cost
	up["purchased"] = true
	catalog_upgrades[upgrade_id] = up

	save_progression()
	upgrade_purchased.emit(upgrade_id)
	progression_updated.emit()

	return {"ok": true, "remaining_coins": coins, "upgrade": up}

func is_upgrade_owned(upgrade_id: String) -> bool:
	if catalog_upgrades.has(upgrade_id):
		return bool(catalog_upgrades[upgrade_id].get("purchased", false))
	return false

func get_upgrade_value(effect_type: String, default_value: float = 1.0) -> float:
	for up: Dictionary in catalog_upgrades.values():
		if bool(up.get("purchased", false)) and up.get("effect_type", "") == effect_type:
			return float(up.get("value", default_value))
	return default_value

func save_progression() -> bool:
	var abs_dir: String = ProjectSettings.globalize_path(save_path.get_base_dir())
	DirAccess.make_dir_recursive_absolute(abs_dir)
	var file: FileAccess = FileAccess.open(save_path, FileAccess.WRITE)
	if file == null:
		# If user:// fails, try absolute fallback
		var fallback_path: String = ProjectSettings.globalize_path("res://.user/progression.json")
		if save_path != fallback_path:
			save_path = fallback_path
			file = FileAccess.open(save_path, FileAccess.WRITE)
		if file == null:
			printerr("[ProgressionManager] Save failed! Open error: %d on %s" % [FileAccess.get_open_error(), save_path])
			return false
	var payload: Dictionary = {
		"coins": coins,
		"total_career_score": total_career_score,
		"highest_unlocked_stage": highest_unlocked_stage,
		"selected_stage": selected_stage,
		"linh_tier": linh_tier,
		"jiwon_tier": jiwon_tier,
		"endless_best_score": endless_best_score,
		"endless_best_orders": endless_best_orders,
		"endless_best_duration": endless_best_duration,
		"stage_records": stage_records,
		"purchased_upgrades": []
	}
	for uid: String in catalog_upgrades.keys():
		if bool(catalog_upgrades[uid].get("purchased", false)):
			payload["purchased_upgrades"].append(uid)
	file.store_string(JSON.stringify(payload, "\t"))
	file.close()
	return true

func load_progression() -> bool:
	var load_target: String = save_path
	if not FileAccess.file_exists(load_target):
		var fallback_path: String = ProjectSettings.globalize_path("res://.user/progression.json")
		if FileAccess.file_exists(fallback_path):
			load_target = fallback_path
		else:
			printerr("[ProgressionManager] Load failed! File does not exist: %s" % load_target)
			return false
	var file: FileAccess = FileAccess.open(load_target, FileAccess.READ)
	if file == null:
		printerr("[ProgressionManager] Load failed! Open error: %d on %s" % [FileAccess.get_open_error(), load_target])
		return false
	var content: String = file.get_as_text()
	file.close()

	var test_json: JSON = JSON.new()
	var err: Error = test_json.parse(content)
	if err != OK or not (test_json.data is Dictionary):
		printerr("[ProgressionManager] Load failed! JSON parse error: %d" % err)
		return false

	var d: Dictionary = test_json.data
	coins = int(d.get("coins", 150))
	total_career_score = int(d.get("total_career_score", 0))
	highest_unlocked_stage = int(d.get("highest_unlocked_stage", 1))
	selected_stage = int(d.get("selected_stage", 1))
	linh_tier = int(d.get("linh_tier", 1))
	jiwon_tier = int(d.get("jiwon_tier", 1))
	endless_best_score = int(d.get("endless_best_score", 0))
	endless_best_orders = int(d.get("endless_best_orders", 0))
	endless_best_duration = float(d.get("endless_best_duration", 0.0))

	var records: Dictionary = d.get("stage_records", {})
	for k: Variant in records.keys():
		var ik: int = int(k)
		if stage_records.has(ik) and records[k] is Dictionary:
			stage_records[ik] = records[k]

	var owned: Array = d.get("purchased_upgrades", [])
	for uid: Variant in owned:
		var suid: String = str(uid)
		if catalog_upgrades.has(suid):
			catalog_upgrades[suid]["purchased"] = true

	progression_updated.emit()
	return true

func reset_progression_for_testing() -> void:
	coins = 150
	total_career_score = 0
	highest_unlocked_stage = 1
	selected_stage = 1
	linh_tier = 1
	jiwon_tier = 1
	endless_best_score = 0
	endless_best_orders = 0
	endless_best_duration = 0.0
	for k: int in stage_records.keys():
		stage_records[k] = {"stars": 0, "best_score": 0, "completed": false}
	for uid: String in catalog_upgrades.keys():
		catalog_upgrades[uid]["purchased"] = false
	save_progression()
	progression_updated.emit()
