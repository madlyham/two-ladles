class_name AssemblyStation
extends StationBase
## Recipe-Driven Assembly Counter.
## Requires a clean vessel (plate or bowl) first, then validates recipe ingredients.
## Enforces Pho Bo rule: rice noodles (blanched separately) and sliced beef must be in the bowl BEFORE pouring hot broth.

var vessel_type: String = "" # "plate" or "bowl"
var added_components: Array[String] = []
var component_qualities: Array[int] = []
var total_prep_bonus: int = 0
var has_fusion_artist_touch: bool = false
var completed_dish: Dictionary = {}

static var _dish_uid_counter: int = 900

func _ready() -> void:
	super._ready()
	_refresh_visual()

func place_vessel(v_type: String) -> Dictionary:
	if vessel_type != "" or not completed_dish.is_empty():
		return {"ok": false, "reason": "Assembly counter already has a %s." % (vessel_type if vessel_type != "" else "completed dish")}
	if not (v_type in ["plate", "bowl"]):
		return {"ok": false, "reason": "Only a clean Plate or Bowl can start dish assembly."}
	vessel_type = v_type
	added_components.clear()
	component_qualities.clear()
	total_prep_bonus = 0
	has_fusion_artist_touch = false
	_refresh_visual()
	return {"ok": true}

func add_ingredient(item_data: Dictionary, player_specialization: String = "") -> Dictionary:
	if not completed_dish.is_empty():
		return {"ok": false, "reason": "Dish (%s) is already complete! Pick it up and serve." % completed_dish.get("display_name", "")}

	var item_id: String = str(item_data.get("item_id", ""))
	if item_id in ["plate", "bowl"]:
		return place_vessel(item_id)

	if vessel_type == "":
		return {"ok": false, "reason": "Place a clean Plate or Bowl on the Assembly Counter first!"}

	if item_id == "burnt_waste":
		return {"ok": false, "reason": "Cannot assemble burnt waste! Dispose of it in the Trash."}

	if item_id in added_components:
		return {"ok": false, "reason": "%s is already added to this %s." % [item_data.get("display_name", item_id), vessel_type]}

	# Check if adding this ingredient matches at least one valid recipe for this vessel
	var candidate_recipes: Array[RecipeData.RecipeSpec] = _find_matching_recipes_if_added(item_id)
	if candidate_recipes.is_empty():
		return {
			"ok": false,
			"reason": "%s does not belong with [%s] in a %s!" % [
				item_data.get("display_name", item_id),
				", ".join(added_components),
				vessel_type.to_upper()
			]
		}

	# Check ordered_final_step constraint (e.g. Pho Bo requires noodles_blanched + beef_sliced before broth_hot)
	for rec: RecipeData.RecipeSpec in candidate_recipes:
		if rec.ordered_final_step != "" and item_id == rec.ordered_final_step:
			for req: String in rec.required_components:
				if req != rec.ordered_final_step and not (req in added_components):
					return {
						"ok": false,
						"reason": "For %s, add Blanched Noodles and Sliced Beef to the bowl BEFORE pouring Hot Broth!" % rec.display_name
					}

	added_components.append(item_id)
	component_qualities.append(int(item_data.get("quality_tier", RecipeData.QualityTier.STANDARD)))
	total_prep_bonus += int(item_data.get("prep_perfect_bonus", 0))
	if player_specialization == "fusion_artist":
		has_fusion_artist_touch = true

	# Check if any recipe is now 100% complete
	for rec: RecipeData.RecipeSpec in candidate_recipes:
		if _is_recipe_complete(rec):
			_finalize_dish(rec)
			break

	_refresh_visual()
	return {
		"ok": true,
		"completed": not completed_dish.is_empty(),
		"completed_dish": completed_dish.duplicate(true),
	}

func _find_matching_recipes_if_added(new_item_id: String) -> Array[RecipeData.RecipeSpec]:
	var matches: Array[RecipeData.RecipeSpec] = []
	var test_list: Array[String] = added_components.duplicate()
	test_list.append(new_item_id)

	for r_id: String in GameData.recipes.keys():
		var rec: RecipeData.RecipeSpec = GameData.recipes[r_id]
		if rec.vessel_type != vessel_type:
			continue
		var all_valid: bool = true
		for comp: String in test_list:
			if not (comp in rec.required_components):
				all_valid = false
				break
		if all_valid:
			matches.append(rec)
	return matches

func _is_recipe_complete(rec: RecipeData.RecipeSpec) -> bool:
	if added_components.size() != rec.required_components.size():
		return false
	for req: String in rec.required_components:
		if not (req in added_components):
			return false
	return true

func _finalize_dish(rec: RecipeData.RecipeSpec) -> void:
	_dish_uid_counter += 1
	var overall_quality: int = RecipeData.QualityTier.STANDARD
	if RecipeData.QualityTier.OVERCOOKED in component_qualities:
		overall_quality = RecipeData.QualityTier.OVERCOOKED
	elif RecipeData.QualityTier.OPTIMAL in component_qualities:
		overall_quality = RecipeData.QualityTier.OPTIMAL

	completed_dish = {
		"uid": "dish_%d" % _dish_uid_counter,
		"item_id": "dish_%s" % rec.id,
		"recipe_id": rec.id,
		"display_name": rec.display_name,
		"culinary_origin": rec.culinary_origin,
		"is_fusion": rec.is_fusion,
		"vessel_type": vessel_type,
		"quality_tier": overall_quality,
		"prep_perfect_bonus": total_prep_bonus,
		"has_fusion_artist": has_fusion_artist_touch,
		"completed_dish": true,
	}
	vessel_type = ""
	added_components.clear()
	component_qualities.clear()

func take_item() -> Dictionary:
	if not completed_dish.is_empty():
		var d: Dictionary = completed_dish.duplicate(true)
		completed_dish.clear()
		_refresh_visual()
		return d
	if vessel_type != "" and added_components.is_empty():
		var v: String = vessel_type
		vessel_type = ""
		_refresh_visual()
		return {"item_id": v, "display_name": "Clean %s" % v.capitalize()}
	return {}

func clear_station() -> void:
	vessel_type = ""
	added_components.clear()
	component_qualities.clear()
	total_prep_bonus = 0
	has_fusion_artist_touch = false
	completed_dish.clear()
	_refresh_visual()

func get_state() -> Dictionary:
	return {
		"vessel_type": vessel_type,
		"added_components": added_components.duplicate(),
		"completed_dish": completed_dish.duplicate(true),
	}

func apply_state(state: Dictionary) -> void:
	vessel_type = str(state.get("vessel_type", ""))
	added_components.clear()
	for c: Variant in state.get("added_components", []):
		added_components.append(str(c))
	completed_dish = state.get("completed_dish", {}).duplicate(true)
	_refresh_visual()

func _refresh_visual() -> void:
	if not completed_dish.is_empty():
		update_label_text("%s\n★ COMPLETED: %s ★\n[Pick Up -> Serving Counter]" % [station_title, completed_dish.get("display_name", "")], Color(0.3, 1.0, 0.5))
		return
	if vessel_type == "":
		update_label_text("%s\n[Empty - Place Clean Plate or Bowl]" % station_title, Color(0.85, 0.9, 0.95))
		return
	var comp_names: Array[String] = []
	for cid: String in added_components:
		if GameData.ingredients.has(cid):
			comp_names.append(GameData.ingredients[cid].display_name)
		else:
			comp_names.append(cid)
	var body: String = "Empty %s" % vessel_type.capitalize() if comp_names.is_empty() else " + ".join(comp_names)
	update_label_text("%s [%s]\n%s" % [station_title, vessel_type.to_upper(), body], Color(1.0, 0.88, 0.4))
