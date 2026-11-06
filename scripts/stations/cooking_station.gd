class_name CookingStation
extends StationBase
## Timed Cooking State Machine: RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT.
## Each appliance has independent thresholds and supports Flame Keeper specialization window extension.
## Dedicated Stockpot maintains hot broth independently from individual noodle blanching.

@export var appliance_id: String = "grill_pan"

var cooking_state: int = RecipeData.CookingState.RAW
var loaded_items: Array[Dictionary] = []
var elapsed_cook_time: float = 0.0
var active_flame_keeper: bool = false
var target_output_id: String = ""

static var _cook_uid_counter: int = 500

func _ready() -> void:
	super._ready()
	if appliance_id == "stockpot":
		# Stockpot starts with simmering broth ready to be maintained
		refill_stockpot()
	_refresh_visual()

func get_spec() -> RecipeData.ApplianceSpec:
	if GameData.appliances.has(appliance_id):
		return GameData.appliances[appliance_id]
	return GameData.appliances["grill_pan"]

func refill_stockpot(player_spec: String = "") -> Dictionary:
	if appliance_id != "stockpot":
		return {"ok": false, "reason": "Not a stockpot."}
	loaded_items = [{"item_id": "broth_raw", "display_name": "Phở Stock Base"}]
	cooking_state = RecipeData.CookingState.COOKING
	elapsed_cook_time = 0.0
	active_flame_keeper = (player_spec == "flame_keeper")
	target_output_id = "broth_hot"
	_refresh_visual()
	return {"ok": true}

func add_ingredient(item_data: Dictionary, player_spec: String = "") -> Dictionary:
	var spec: RecipeData.ApplianceSpec = get_spec()
	var item_id: String = str(item_data.get("item_id", ""))

	if appliance_id == "stockpot":
		if cooking_state == RecipeData.CookingState.BURNT or loaded_items.is_empty():
			return refill_stockpot(player_spec)
		return {"ok": false, "reason": "Stockpot only simmers Phở Broth! Blanch rice noodles in the Noodle Blancher."}

	if not (item_id in spec.accepted_inputs):
		return {
			"ok": false,
			"reason": "%s cannot cook %s!" % [spec.display_name, item_data.get("display_name", item_id)]
		}

	if cooking_state == RecipeData.CookingState.BURNT:
		return {"ok": false, "reason": "Appliance has burnt food! Clear/trash burnt waste first."}

	if appliance_id == "simmer_wok":
		# Simmer Wok combines tteok_portioned + eomuk_prepared + gochujang_sauce
		for existing: Dictionary in loaded_items:
			if existing.get("item_id", "") == item_id:
				return {"ok": false, "reason": "%s is already in the Wok." % item_data.get("display_name", item_id)}
		loaded_items.append(item_data.duplicate(true))
		if player_spec == "flame_keeper":
			active_flame_keeper = true
		if _has_all_wok_ingredients():
			cooking_state = RecipeData.CookingState.COOKING
			elapsed_cook_time = 0.0
			target_output_id = "tteokbokki_simmered"
		else:
			cooking_state = RecipeData.CookingState.RAW
		_refresh_visual()
		return {"ok": true, "started_cooking": cooking_state == RecipeData.CookingState.COOKING}
	else:
		# Single-slot appliance (noodle_blancher or grill_pan)
		if not loaded_items.is_empty():
			return {"ok": false, "reason": "%s is already occupied." % spec.display_name}
		loaded_items.append(item_data.duplicate(true))
		cooking_state = RecipeData.CookingState.COOKING
		elapsed_cook_time = 0.0
		active_flame_keeper = (player_spec == "flame_keeper")
		if appliance_id == "noodle_blancher":
			target_output_id = "noodles_blanched"
		elif appliance_id == "grill_pan":
			target_output_id = GameData.get_grill_output(item_id)
		_refresh_visual()
		return {"ok": true, "started_cooking": true}

func _has_all_wok_ingredients() -> bool:
	var ids: Array[String] = []
	for it: Dictionary in loaded_items:
		ids.append(str(it.get("item_id", "")))
	return ("tteok_portioned" in ids) and ("eomuk_prepared" in ids) and ("gochujang_sauce" in ids)

func tick_cooking(delta: float) -> void:
	if cooking_state in [RecipeData.CookingState.RAW, RecipeData.CookingState.BURNT]:
		return
	if loaded_items.is_empty():
		return

	var spec: RecipeData.ApplianceSpec = get_spec()
	var optimal_mult: float = 1.0
	if active_flame_keeper and GameData.specializations.has("flame_keeper"):
		optimal_mult = GameData.specializations["flame_keeper"].optimal_cook_window_multiplier
	if appliance_id == "stockpot" and ProgressionManager.is_upgrade_owned("insulated_broth_pot"):
		optimal_mult *= ProgressionManager.get_upgrade_value("stockpot_window", 1.50)

	var cook_speed_mult: float = ProgressionManager.get_upgrade_value("cook_speed", 1.0) if ProgressionManager.is_upgrade_owned("cast_iron_seasoning") else 1.0
	var t_cook: float = spec.cook_time_to_optimal * cook_speed_mult
	var t_optimal_end: float = t_cook + (spec.optimal_window_duration * optimal_mult)
	var t_overcooked_end: float = t_optimal_end + spec.overcooked_window_duration

	elapsed_cook_time += delta
	var prev_state: int = cooking_state

	if elapsed_cook_time < t_cook:
		cooking_state = RecipeData.CookingState.COOKING
	elif elapsed_cook_time < t_optimal_end:
		cooking_state = RecipeData.CookingState.OPTIMAL
	elif elapsed_cook_time < t_overcooked_end:
		cooking_state = RecipeData.CookingState.OVERCOOKED
	else:
		cooking_state = RecipeData.CookingState.BURNT
		if prev_state != RecipeData.CookingState.BURNT:
			ShiftManager.record_waste("%s burnt! (-10 Flow)" % spec.display_name)

	_refresh_visual()

func get_remaining_phase_time() -> float:
	var spec: RecipeData.ApplianceSpec = get_spec()
	var optimal_mult: float = 1.35 if active_flame_keeper else 1.0
	if appliance_id == "stockpot" and ProgressionManager.is_upgrade_owned("insulated_broth_pot"):
		optimal_mult *= ProgressionManager.get_upgrade_value("stockpot_window", 1.50)
	var cook_speed_mult: float = ProgressionManager.get_upgrade_value("cook_speed", 1.0) if ProgressionManager.is_upgrade_owned("cast_iron_seasoning") else 1.0
	var t_cook: float = spec.cook_time_to_optimal * cook_speed_mult
	var t_optimal_end: float = t_cook + (spec.optimal_window_duration * optimal_mult)
	var t_overcooked_end: float = t_optimal_end + spec.overcooked_window_duration

	if cooking_state == RecipeData.CookingState.COOKING:
		return maxf(0.0, t_cook - elapsed_cook_time)
	elif cooking_state == RecipeData.CookingState.OPTIMAL:
		return maxf(0.0, t_optimal_end - elapsed_cook_time)
	elif cooking_state == RecipeData.CookingState.OVERCOOKED:
		return maxf(0.0, t_overcooked_end - elapsed_cook_time)
	return 0.0

func retrieve_cooked_item(player_character: String = "") -> Dictionary:
	if loaded_items.is_empty():
		return {"ok": false, "reason": "Appliance is empty."}

	if cooking_state == RecipeData.CookingState.RAW or cooking_state == RecipeData.CookingState.COOKING:
		return {"ok": false, "reason": "Still cooking! Wait for OPTIMAL window."}

	_cook_uid_counter += 1

	if cooking_state == RecipeData.CookingState.BURNT:
		loaded_items.clear()
		cooking_state = RecipeData.CookingState.RAW
		elapsed_cook_time = 0.0
		if appliance_id == "stockpot":
			refill_stockpot()
		_refresh_visual()
		return {
			"ok": true,
			"item": {
				"uid": "burnt_%d" % _cook_uid_counter,
				"item_id": "burnt_waste",
				"display_name": "Burnt Food Waste",
				"quality_tier": RecipeData.QualityTier.BURNT_WASTE,
			}
		}

	var quality: int = RecipeData.QualityTier.OPTIMAL if cooking_state == RecipeData.CookingState.OPTIMAL else RecipeData.QualityTier.OVERCOOKED
	if quality == RecipeData.QualityTier.OPTIMAL:
		ShiftManager.stats["optimal_cooks"] += 1

	var out_spec: RecipeData.IngredientSpec = GameData.ingredients[target_output_id]
	var prep_bonus: int = 0
	for it: Dictionary in loaded_items:
		prep_bonus += int(it.get("prep_perfect_bonus", 0))

	var produced: Dictionary = {
		"uid": "cook_%d" % _cook_uid_counter,
		"item_id": target_output_id,
		"display_name": out_spec.display_name,
		"quality_tier": quality,
		"prep_perfect_bonus": prep_bonus,
		"prepared_by_character": player_character,
	}

	if appliance_id == "stockpot":
		# Stockpot stays simmering and resets to fresh simmer cycle when ladled
		elapsed_cook_time = get_spec().cook_time_to_optimal
		cooking_state = RecipeData.CookingState.OPTIMAL
	else:
		loaded_items.clear()
		cooking_state = RecipeData.CookingState.RAW
		elapsed_cook_time = 0.0
		target_output_id = ""

	_refresh_visual()
	return {"ok": true, "item": produced}

func get_state() -> Dictionary:
	return {
		"cooking_state": cooking_state,
		"loaded_items": loaded_items.duplicate(true),
		"elapsed_cook_time": elapsed_cook_time,
		"active_flame_keeper": active_flame_keeper,
		"target_output_id": target_output_id,
	}

func apply_state(state: Dictionary) -> void:
	cooking_state = int(state.get("cooking_state", RecipeData.CookingState.RAW))
	loaded_items.clear()
	for it: Variant in state.get("loaded_items", []):
		if it is Dictionary:
			loaded_items.append(it)
	elapsed_cook_time = float(state.get("elapsed_cook_time", 0.0))
	active_flame_keeper = bool(state.get("active_flame_keeper", false))
	target_output_id = str(state.get("target_output_id", ""))
	_refresh_visual()

func _refresh_visual() -> void:
	var spec: RecipeData.ApplianceSpec = get_spec()
	if loaded_items.is_empty():
		update_label_text("%s\n[Idle - Ready]" % spec.display_name, Color(0.8, 0.85, 0.9))
		return
	var rem: float = get_remaining_phase_time()
	match cooking_state:
		RecipeData.CookingState.RAW:
			var count_str: String = "%d/3 Wok Ingredients" % loaded_items.size() if appliance_id == "simmer_wok" else "Loaded"
			update_label_text("%s\n[%s]" % [spec.display_name, count_str], Color(0.9, 0.9, 0.7))
		RecipeData.CookingState.COOKING:
			update_label_text("%s\nCOOKING... (%.1fs)" % [spec.display_name, rem], Color(1.0, 0.82, 0.3))
		RecipeData.CookingState.OPTIMAL:
			update_label_text("%s\n★ OPTIMAL WINDOW! (%.1fs) ★" % [spec.display_name, rem], Color(0.3, 1.0, 0.45))
		RecipeData.CookingState.OVERCOOKED:
			update_label_text("%s\nWARNING: OVERCOOKED! (%.1fs)" % [spec.display_name, rem], Color(1.0, 0.45, 0.2))
		RecipeData.CookingState.BURNT:
			update_label_text("%s\n✖ BURNT! [Clear Waste]" % spec.display_name, Color(0.95, 0.2, 0.2))
