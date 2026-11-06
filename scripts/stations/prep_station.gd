class_name PrepStation
extends StationBase
## Preparation Counter implementing Q3 discrete 4-strike rhythm-based input.
## Evaluates cadence (PERFECT, GOOD, MISS). Misses never block completion.
## Enforces Cross-Prep workflow tags so Linh preps Jiwon's cooking items and Jiwon preps Linh's assembly items.

const REQUIRED_STRIKES: int = 4
const IDEAL_CADENCE_SEC: float = 0.45
const BASE_PERFECT_DELTA: float = 0.12
const BASE_GOOD_DELTA: float = 0.28

var current_item: Dictionary = {}
var strikes_completed: int = 0
var last_strike_time_sec: float = -1.0
var perfect_strikes: int = 0
var good_strikes: int = 0
var miss_strikes: int = 0
var last_cadence_grade: int = RecipeData.CadenceGrade.GOOD

static var _uid_counter: int = 100

func place_raw_item(item_data: Dictionary, _peer_id: int = 1) -> Dictionary:
	if not current_item.is_empty():
		return {"ok": false, "reason": "Prep counter already holds %s." % current_item.get("display_name", "item")}
	var item_id: String = str(item_data.get("item_id", ""))
	var output_id: String = GameData.get_prep_output(item_id)
	if output_id == "":
		return {"ok": false, "reason": "%s does not require chopping/portioning." % item_data.get("display_name", item_id)}

	current_item = item_data.duplicate(true)
	strikes_completed = 0
	last_strike_time_sec = -1.0
	perfect_strikes = 0
	good_strikes = 0
	miss_strikes = 0
	_refresh_visual()
	return {"ok": true, "state": get_state()}

## Performs one discrete strike at timestamp_sec.
## Cadence is evaluated relative to the previous strike (first strike is always GOOD or PERFECT start).
func perform_strike(timestamp_sec: float, player_specialization: String = "", player_character: String = "linh") -> Dictionary:
	if current_item.is_empty():
		return {"ok": false, "reason": "No raw ingredient on Prep Counter."}

	var item_id: String = str(current_item.get("item_id", ""))
	var output_id: String = GameData.get_prep_output(item_id)
	if output_id == "":
		return {"ok": false, "reason": "Item is already prepared."}

	var grade: int = RecipeData.CadenceGrade.GOOD
	var window_mult: float = 1.0
	if player_specialization == "prep_master" and GameData.specializations.has("prep_master"):
		window_mult = GameData.specializations["prep_master"].prep_window_multiplier

	if last_strike_time_sec < 0.0:
		# First strike starts the rhythm beat cleanly
		grade = RecipeData.CadenceGrade.PERFECT
		perfect_strikes += 1
	else:
		var interval: float = timestamp_sec - last_strike_time_sec
		var diff: float = absf(interval - IDEAL_CADENCE_SEC)
		if diff <= BASE_PERFECT_DELTA * window_mult:
			grade = RecipeData.CadenceGrade.PERFECT
			perfect_strikes += 1
		elif diff <= BASE_GOOD_DELTA * window_mult:
			grade = RecipeData.CadenceGrade.GOOD
			good_strikes += 1
		else:
			grade = RecipeData.CadenceGrade.MISS
			miss_strikes += 1

	last_strike_time_sec = timestamp_sec
	last_cadence_grade = grade
	strikes_completed += 1

	var completed: bool = false
	if strikes_completed >= REQUIRED_STRIKES:
		completed = true
		var out_spec: RecipeData.IngredientSpec = GameData.ingredients[output_id]
		var quality: int = RecipeData.QualityTier.STANDARD
		var prep_bonus: int = 0
		if perfect_strikes >= 3:
			quality = RecipeData.QualityTier.OPTIMAL
			prep_bonus = 10
			if player_specialization == "prep_master":
				prep_bonus += 15
			ShiftManager.stats["perfect_preps"] += 1

		_uid_counter += 1
		current_item = {
			"uid": "prep_%d" % _uid_counter,
			"item_id": output_id,
			"display_name": out_spec.display_name,
			"quality_tier": quality,
			"prep_perfect_bonus": prep_bonus,
			"prepared_by_character": player_character,
			"partner_target_zone": out_spec.partner_target_zone,
			"prepared": true,
		}

	_refresh_visual()
	return {
		"ok": true,
		"grade": grade,
		"strikes_completed": strikes_completed,
		"completed": completed,
		"item": current_item.duplicate(true),
	}

func take_item() -> Dictionary:
	if current_item.is_empty():
		return {}
	if strikes_completed > 0 and strikes_completed < REQUIRED_STRIKES and GameData.get_prep_output(str(current_item.get("item_id", ""))) != "":
		# Still mid-prep
		return {}
	var taken: Dictionary = current_item.duplicate(true)
	current_item.clear()
	strikes_completed = 0
	last_strike_time_sec = -1.0
	_refresh_visual()
	return taken

func get_state() -> Dictionary:
	return {
		"current_item": current_item.duplicate(true),
		"strikes_completed": strikes_completed,
		"perfect_strikes": perfect_strikes,
		"good_strikes": good_strikes,
		"miss_strikes": miss_strikes,
		"last_cadence_grade": last_cadence_grade,
	}

func apply_state(state: Dictionary) -> void:
	current_item = state.get("current_item", {}).duplicate(true)
	strikes_completed = int(state.get("strikes_completed", 0))
	perfect_strikes = int(state.get("perfect_strikes", 0))
	good_strikes = int(state.get("good_strikes", 0))
	miss_strikes = int(state.get("miss_strikes", 0))
	last_cadence_grade = int(state.get("last_cadence_grade", RecipeData.CadenceGrade.GOOD))
	_refresh_visual()

func _refresh_visual() -> void:
	if current_item.is_empty():
		update_label_text("%s\n[Empty - Place Raw Ingredient]" % station_title, Color(0.85, 0.9, 0.95))
		return
	var iname: String = str(current_item.get("display_name", current_item.get("item_id", "")))
	if GameData.get_prep_output(str(current_item.get("item_id", ""))) != "":
		var grade_str: String = "PERFECT!" if last_cadence_grade == RecipeData.CadenceGrade.PERFECT else ("GOOD" if last_cadence_grade == RecipeData.CadenceGrade.GOOD else "MISS (Keep Going!)")
		update_label_text("%s\n%s [%d/%d Strikes] %s" % [station_title, iname, strikes_completed, REQUIRED_STRIKES, grade_str], Color(1.0, 0.88, 0.35))
	else:
		update_label_text("%s\nREADY: %s\n[Pick Up -> Pass Counter]" % [station_title, iname], Color(0.35, 0.95, 0.55))
