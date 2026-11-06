extends Node
## Authoritative Shift, Order Queue, Scoring, Kitchen Flow (0-100), Rush Rhythm (15s, 1.5x), and Plate Pool Manager.

const CampaignLevelSpec = preload("res://scripts/data/campaign_level_spec.gd")
const RecipeData = preload("res://scripts/data/recipe_data.gd")

signal shift_state_updated(state: Dictionary)
signal order_queue_updated(orders: Array)
signal flow_updated(flow_value: float, rush_active: bool, rush_remaining: float)
signal rush_rhythm_changed(is_active: bool)
signal feedback_banner(message: String, color: Color)
signal shift_ended(summary: Dictionary)

const MAX_FLOW: float = 100.0
const HANDOFF_FLOW_BONUS: float = 15.0
const RUSH_RHYTHM_DURATION: float = 15.0
const RUSH_RHYTHM_MULTIPLIER: float = 1.5
const HANDOFF_WINDOW_MS: int = 4000
const INITIAL_CLEAN_PLATES: int = 4
const DEFAULT_SHIFT_DURATION: float = 180.0
const MAX_ACTIVE_ORDERS: int = 4

var shift_active: bool = false
var shift_time_remaining: float = DEFAULT_SHIFT_DURATION
var total_score: int = 0
var kitchen_flow: float = 0.0
var rush_rhythm_active: bool = false
var rush_rhythm_remaining: float = 0.0

var clean_plates: int = INITIAL_CLEAN_PLATES
var dirty_plates_at_return: int = 0

var active_orders: Array[Dictionary] = []
var next_order_id: int = 1
var order_spawn_timer: float = 0.0
var order_spawn_interval: float = 14.0

# Anti-exploit tracking: each order_id can claim the +15 Pass Counter handoff bonus at most once
var claimed_handoff_orders: Dictionary = {} # order_id (int) -> bool
# Track item provenance so repeated back-and-forth passing of the same item instance cannot farm Flow
var claimed_item_instances: Dictionary = {} # instance_uid (String) -> bool

# Shift statistics
var stats: Dictionary = {
	"orders_completed": 0,
	"orders_failed": 0,
	"handoffs_completed": 0,
	"cross_prep_transfers": 0,
	"rush_rhythms_triggered": 0,
	"ingredients_wasted": 0,
	"perfect_preps": 0,
	"optimal_cooks": 0,
}

enum GameMode {
	CAMPAIGN = 0,
	ENDLESS_RUSH = 1,
}

var game_mode: int = GameMode.CAMPAIGN
var current_stage_id: int = 1
var endless_elapsed_sec: float = 0.0
var endless_allowed_failures: int = 3
var dynamic_max_orders: int = MAX_ACTIVE_ORDERS

func start_new_shift(duration: float = DEFAULT_SHIFT_DURATION, stage_id: int = 1, p_mode: int = GameMode.CAMPAIGN) -> void:
	shift_active = true
	game_mode = p_mode
	current_stage_id = stage_id
	endless_elapsed_sec = 0.0
	dynamic_max_orders = MAX_ACTIVE_ORDERS

	if game_mode == GameMode.CAMPAIGN and GameData.campaign_levels.has(stage_id):
		var lvl: CampaignLevelSpec = GameData.campaign_levels[stage_id]
		shift_time_remaining = lvl.shift_duration
		order_spawn_interval = lvl.order_spawn_interval
		dynamic_max_orders = lvl.max_concurrent_orders
	elif game_mode == GameMode.ENDLESS_RUSH:
		shift_time_remaining = 99999.0 # Effectively unlimited, ends on failures
		order_spawn_interval = 14.0
		dynamic_max_orders = 2
	else:
		shift_time_remaining = duration
	shift_time_remaining = duration
	total_score = 0
	kitchen_flow = 0.0
	rush_rhythm_active = false
	rush_rhythm_remaining = 0.0
	clean_plates = INITIAL_CLEAN_PLATES
	dirty_plates_at_return = 0
	active_orders.clear()
	claimed_handoff_orders.clear()
	claimed_item_instances.clear()
	next_order_id = 1
	order_spawn_timer = 0.0
	stats = {
		"orders_completed": 0,
		"orders_failed": 0,
		"handoffs_completed": 0,
		"cross_prep_transfers": 0,
		"rush_rhythms_triggered": 0,
		"ingredients_wasted": 0,
		"perfect_preps": 0,
		"optimal_cooks": 0,
	}
	# Spawn 2 starter orders immediately so both Linh and Jiwon have simultaneous cross-prep work
	spawn_order("banh_mi_thit")
	spawn_order("pho_bo")
	_broadcast_all()

func _process(delta: float) -> void:
	if not shift_active:
		return
	if not NetworkManager.is_host():
		return
	tick_shift(delta)

func tick_shift(delta: float) -> void:
	if not shift_active:
		return

	shift_time_remaining = maxf(0.0, shift_time_remaining - delta)

	# Tick Rush Rhythm timer
	if rush_rhythm_active:
		rush_rhythm_remaining -= delta
		if rush_rhythm_remaining <= 0.0:
			rush_rhythm_active = false
			rush_rhythm_remaining = 0.0
			rush_rhythm_changed.emit(false)
			feedback_banner.emit("Rush Rhythm Ended — Flow Generation Resumed", Color(0.9, 0.8, 0.4))
			_sync_flow.rpc(kitchen_flow, rush_rhythm_active, rush_rhythm_remaining)

	# Tick active orders patience
	var expired_indices: Array[int] = []
	for i: int in range(active_orders.size()):
		active_orders[i]["remaining_patience"] -= delta
		if active_orders[i]["remaining_patience"] <= 0.0:
			expired_indices.append(i)

	# Remove expired orders in reverse order
	for idx: int in range(expired_indices.size() - 1, -1, -1):
		var expired_order: Dictionary = active_orders[expired_indices[idx]]
		active_orders.remove_at(expired_indices[idx])
		stats["orders_failed"] += 1
		modify_flow(-15.0, "Order Expired: %s (-15 Flow)" % expired_order["display_name"])

	# Endless Mode failure condition check
	if game_mode == GameMode.ENDLESS_RUSH:
		endless_elapsed_sec += delta
		# Dynamic difficulty scaling: faster spawns & more concurrent tickets over time
		order_spawn_interval = maxf(6.5, 14.0 - (endless_elapsed_sec / 30.0))
		if endless_elapsed_sec > 120.0:
			dynamic_max_orders = 5
		elif endless_elapsed_sec > 60.0:
			dynamic_max_orders = 4
		elif endless_elapsed_sec > 30.0:
			dynamic_max_orders = 3

		if stats["orders_failed"] >= endless_allowed_failures:
			feedback_banner.emit("Kitchen Overwhelmed! 3 Customer Orders Lost!", Color(1.0, 0.25, 0.25))
			end_shift()
			return

	# Spawn new orders periodically
	order_spawn_timer += delta
	if order_spawn_timer >= order_spawn_interval and active_orders.size() < dynamic_max_orders:
		order_spawn_timer = 0.0
		var allowed: Array[String] = ["pho_bo", "banh_mi_thit", "tteokbokki", "bulgogi_banh_mi"]
		if game_mode == GameMode.CAMPAIGN and GameData.campaign_levels.has(current_stage_id):
			allowed = GameData.campaign_levels[current_stage_id].allowed_recipes
		var chosen_recipe: String = allowed[(next_order_id - 1) % allowed.size()]
		spawn_order(chosen_recipe)

	if shift_time_remaining <= 0.0:
		end_shift()
		return

	_broadcast_state_throttled(delta)

var _sync_accum: float = 0.0
func _broadcast_state_throttled(delta: float) -> void:
	_sync_accum += delta
	if _sync_accum >= 0.25:
		_sync_accum = 0.0
		_broadcast_all()

func spawn_order(recipe_id: String) -> Dictionary:
	if not GameData.recipes.has(recipe_id):
		return {}
	var spec: RecipeData.RecipeSpec = GameData.recipes[recipe_id]
	var order: Dictionary = {
		"order_id": next_order_id,
		"recipe_id": spec.id,
		"display_name": spec.display_name,
		"culinary_origin": spec.culinary_origin,
		"is_fusion": spec.is_fusion,
		"vessel_type": spec.vessel_type,
		"required_components": spec.required_components.duplicate(),
		"base_score": spec.base_score,
		"max_patience": spec.patience_seconds,
		"remaining_patience": spec.patience_seconds,
		"handoff_claimed": false,
	}
	next_order_id += 1
	active_orders.append(order)
	_broadcast_orders()
	return order

## Evaluates Pass Counter Handoff with Q4 Anti-Exploit Rules:
## 1. Player A places an ingredient associated with an active order on the Pass Counter.
## 2. Player B retrieves that ingredient within 4 seconds (4000 ms).
## 3. Verifies transfer is between two different players.
## 4. Grants +15 Kitchen Flow points on 0-100 meter.
## 5. A particular order can reward the coordinated handoff bonus only once.
## 6. Repeatedly passing the same item back and forth never grants extra bonuses.
func evaluate_pass_counter_handoff(
	item_data: Dictionary,
	placed_by_peer: int,
	retrieved_by_peer: int,
	elapsed_ms: int
) -> Dictionary:
	if placed_by_peer == retrieved_by_peer:
		return {"granted": false, "reason": "Same player retrieved item."}
	if elapsed_ms > HANDOFF_WINDOW_MS:
		return {"granted": false, "reason": "Handoff window (4s) expired."}

	var item_uid: String = str(item_data.get("uid", ""))
	if item_uid != "" and claimed_item_instances.has(item_uid):
		return {"granted": false, "reason": "Item already used for a handoff."}

	var item_id: String = str(item_data.get("item_id", ""))
	var matching_order_id: int = _find_unclaimed_order_for_item(item_id, int(item_data.get("order_id", -1)))
	if matching_order_id == -1:
		return {"granted": false, "reason": "No active unclaimed order requires this ingredient."}

	claimed_handoff_orders[matching_order_id] = true
	if item_uid != "":
		claimed_item_instances[item_uid] = true

	for i: int in range(active_orders.size()):
		if active_orders[i]["order_id"] == matching_order_id:
			active_orders[i]["handoff_claimed"] = true
			break

	stats["handoffs_completed"] += 1
	var placer_char: String = str(item_data.get("prepared_by_character", ""))
	var retriever_char: String = _get_peer_character(retrieved_by_peer)
	if placer_char != "" and retriever_char != "" and placer_char != retriever_char:
		stats["cross_prep_transfers"] += 1

	var flow_awarded: float = modify_flow(
		HANDOFF_FLOW_BONUS,
		"Coordinated Pass Counter Handoff! (+15 Kitchen Flow)"
	)
	_broadcast_orders()
	return {
		"granted": true,
		"order_id": matching_order_id,
		"flow_awarded": flow_awarded,
		"reason": "Coordinated handoff bonus granted!"
	}

func _find_unclaimed_order_for_item(item_id: String, hinted_order_id: int = -1) -> int:
	# First check hinted order_id if valid and unclaimed
	if hinted_order_id > 0 and not claimed_handoff_orders.get(hinted_order_id, false):
		for order: Dictionary in active_orders:
			if order["order_id"] == hinted_order_id and _order_uses_ingredient(order["recipe_id"], item_id):
				return hinted_order_id
	# Otherwise check oldest active unclaimed order that needs this ingredient
	for order: Dictionary in active_orders:
		var oid: int = order["order_id"]
		if not claimed_handoff_orders.get(oid, false) and _order_uses_ingredient(order["recipe_id"], item_id):
			return oid
	return -1

func _order_uses_ingredient(recipe_id: String, item_id: String) -> bool:
	if item_id in ["plate", "bowl"]:
		return true
	var map: Dictionary = {
		"pho_bo": ["beef_raw", "beef_sliced", "noodles_raw", "noodles_blanched", "broth_hot"],
		"banh_mi_thit": ["pork_raw", "pork_portioned", "pork_grilled", "daikon_carrot_raw", "pickled_veg", "baguette"],
		"tteokbokki": ["tteok_raw", "tteok_portioned", "eomuk_raw", "eomuk_prepared", "gochujang_sauce", "tteokbokki_simmered"],
		"bulgogi_banh_mi": ["bulgogi_raw", "bulgogi_portioned", "bulgogi_seared", "kimchi_raw", "kimchi_slaw", "baguette"],
	}
	return item_id in map.get(recipe_id, [])

## Modifies Kitchen Flow (0..100). Pauses positive Flow gain while Rush Rhythm is active.
## Reaching 100 resets Flow to 0 and activates Rush Rhythm for 15 seconds (1.5x base score multiplier).
func modify_flow(delta_flow: float, reason: String = "") -> float:
	if delta_flow > 0.0 and rush_rhythm_active:
		# Flow generation is paused during Rush Rhythm (no stacking/extending)
		return 0.0

	var previous: float = kitchen_flow
	kitchen_flow = clampf(kitchen_flow + delta_flow, 0.0, MAX_FLOW)
	var applied: float = kitchen_flow - previous

	if kitchen_flow >= MAX_FLOW and not rush_rhythm_active:
		kitchen_flow = 0.0
		rush_rhythm_active = true
		rush_rhythm_remaining = RUSH_RHYTHM_DURATION
		stats["rush_rhythms_triggered"] += 1
		rush_rhythm_changed.emit(true)
		feedback_banner.emit("RUSH RHYTHM ACTIVATED! 1.5x Base Score for 15s!", Color(1.0, 0.78, 0.15))
	elif reason != "":
		var col: Color = Color(0.35, 0.92, 0.55) if delta_flow >= 0.0 else Color(0.95, 0.35, 0.35)
		feedback_banner.emit(reason, col)

	_sync_flow.rpc(kitchen_flow, rush_rhythm_active, rush_rhythm_remaining)
	return applied

## Validates and serves a completed dish at the Serving Counter.
func serve_completed_dish(dish_data: Dictionary, serving_peer_id: int = 1) -> Dictionary:
	var recipe_id: String = str(dish_data.get("recipe_id", ""))
	if recipe_id == "":
		return {"ok": false, "reason": "Dish is incomplete or invalid."}

	var match_idx: int = -1
	for i: int in range(active_orders.size()):
		if active_orders[i]["recipe_id"] == recipe_id:
			match_idx = i
			break

	if match_idx == -1:
		modify_flow(-10.0, "Served unrequested dish! (-10 Flow)")
		return {"ok": false, "reason": "No customer is currently ordering %s!" % recipe_id}

	var order: Dictionary = active_orders[match_idx]
	active_orders.remove_at(match_idx)

	var base_score: float = float(order["base_score"])
	if rush_rhythm_active:
		base_score *= RUSH_RHYTHM_MULTIPLIER

	# Quality modifier from cooking/prep
	var quality_tier: int = int(dish_data.get("quality_tier", RecipeData.QualityTier.STANDARD))
	var quality_mult: float = 1.0
	if quality_tier == RecipeData.QualityTier.OPTIMAL:
		quality_mult = 1.20
	elif quality_tier == RecipeData.QualityTier.OVERCOOKED:
		quality_mult = 0.75

	# Specialization modifiers (Fusion Artist / Prep Master)
	var spec_bonus: int = int(dish_data.get("prep_perfect_bonus", 0))
	var server_spec: String = _get_peer_specialization(serving_peer_id)
	if server_spec == "fusion_artist" or bool(dish_data.get("has_fusion_artist", false)):
		if bool(order.get("is_fusion", false)) or order["required_components"].size() >= 3:
			base_score *= 1.25

	# Patience speed bonus (up to +25 pts)
	var patience_ratio: float = clampf(
		float(order["remaining_patience"]) / maxf(1.0, float(order["max_patience"])),
		0.0,
		1.0
	)
	var tip_score: int = int(round(patience_ratio * 25.0))
	var final_points: int = int(round(base_score * quality_mult)) + tip_score + spec_bonus

	total_score += final_points
	stats["orders_completed"] += 1
	dirty_plates_at_return += 1

	var flow_gain: float = 20.0 if quality_tier == RecipeData.QualityTier.OPTIMAL else 12.0
	modify_flow(flow_gain, "Served %s! +%d pts" % [order["display_name"], final_points])

	_broadcast_all()
	return {
		"ok": true,
		"score_awarded": final_points,
		"total_score": total_score,
		"order_id": order["order_id"],
		"dirty_plates_at_return": dirty_plates_at_return,
	}

func record_waste(reason: String = "Ingredient burnt/wasted! (-10 Flow)") -> void:
	stats["ingredients_wasted"] += 1
	modify_flow(-10.0, reason)
	_broadcast_all()

func take_clean_plate() -> bool:
	if clean_plates <= 0:
		return false
	clean_plates -= 1
	_broadcast_all()
	return true

func take_dirty_plate_from_return() -> bool:
	if dirty_plates_at_return <= 0:
		return false
	dirty_plates_at_return -= 1
	_broadcast_all()
	return true

func return_washed_plate() -> void:
	clean_plates += 1
	_broadcast_all()

func end_shift() -> Dictionary:
	shift_active = false
	var stars: int = 1
	if game_mode == GameMode.CAMPAIGN and GameData.campaign_levels.has(current_stage_id):
		stars = GameData.campaign_levels[current_stage_id].evaluate_stars(total_score)
	else:
		if total_score >= 650:
			stars = 3
		elif total_score >= 350:
			stars = 2

	var summary: Dictionary = {
		"total_score": total_score,
		"stars": stars,
		"stage_id": current_stage_id,
		"game_mode": game_mode,
		"endless_elapsed_sec": endless_elapsed_sec,
		"orders_completed": stats["orders_completed"],
		"orders_failed": stats["orders_failed"],
		"handoffs_completed": stats["handoffs_completed"],
		"cross_prep_transfers": stats["cross_prep_transfers"],
		"rush_rhythms_triggered": stats["rush_rhythms_triggered"],
		"ingredients_wasted": stats["ingredients_wasted"],
	}

	if game_mode == GameMode.ENDLESS_RUSH:
		var end_res: Dictionary = ProgressionManager.record_endless_results(
			total_score,
			stats["orders_completed"],
			endless_elapsed_sec
		)
		summary["earned_coins"] = end_res.get("earned_coins", 0)
		summary["new_balance"] = end_res.get("total_coins", 0)
		summary["new_high_score"] = end_res.get("new_high_score", false)
		summary["best_score"] = end_res.get("best_score", total_score)
	else:
		var prog_res: Dictionary = ProgressionManager.record_shift_results(current_stage_id, summary)
		summary["earned_coins"] = prog_res.get("earned_coins", 0)
		summary["new_balance"] = prog_res.get("new_balance", 0)
		summary["newly_unlocked"] = prog_res.get("newly_unlocked", false)
		summary["tier_upgraded"] = prog_res.get("tier_upgraded", false)
		summary["linh_tier"] = prog_res.get("linh_tier", 1)
		summary["jiwon_tier"] = prog_res.get("jiwon_tier", 1)
		summary["highest_unlocked_stage"] = ProgressionManager.highest_unlocked_stage

	_sync_shift_ended.rpc(summary)
	return summary

@rpc("any_peer", "call_local", "reliable")
func request_restart_shift() -> void:
	if not NetworkManager.is_host():
		return
	start_new_shift(DEFAULT_SHIFT_DURATION, current_stage_id, game_mode)

func _get_peer_character(peer_id: int) -> String:
	if NetworkManager.players.has(peer_id):
		return str(NetworkManager.players[peer_id].get("character", ""))
	return ""

func _get_peer_specialization(peer_id: int) -> String:
	if NetworkManager.players.has(peer_id):
		return str(NetworkManager.players[peer_id].get("specialization", ""))
	return ""

func _broadcast_all() -> void:
	var state_payload: Dictionary = {
		"shift_active": shift_active,
		"shift_time_remaining": shift_time_remaining,
		"total_score": total_score,
		"clean_plates": clean_plates,
		"dirty_plates_at_return": dirty_plates_at_return,
		"stats": stats,
	}
	if NetworkManager.is_lan_active and multiplayer.multiplayer_peer != null and NetworkManager.is_host():
		_sync_shift_state.rpc(state_payload)
		_sync_orders.rpc(active_orders.duplicate(true))
		_sync_flow.rpc(kitchen_flow, rush_rhythm_active, rush_rhythm_remaining)
	else:
		shift_state_updated.emit(state_payload)
		order_queue_updated.emit(active_orders)
		flow_updated.emit(kitchen_flow, rush_rhythm_active, rush_rhythm_remaining)

func _broadcast_orders() -> void:
	if NetworkManager.is_lan_active and multiplayer.multiplayer_peer != null and NetworkManager.is_host():
		_sync_orders.rpc(active_orders.duplicate(true))
	else:
		order_queue_updated.emit(active_orders)

@rpc("authority", "call_local", "reliable")
func _sync_shift_state(state_payload: Dictionary) -> void:
	shift_active = bool(state_payload.get("shift_active", false))
	shift_time_remaining = float(state_payload.get("shift_time_remaining", 0.0))
	total_score = int(state_payload.get("total_score", 0))
	clean_plates = int(state_payload.get("clean_plates", INITIAL_CLEAN_PLATES))
	dirty_plates_at_return = int(state_payload.get("dirty_plates_at_return", 0))
	stats = state_payload.get("stats", stats).duplicate(true)
	shift_state_updated.emit(state_payload)

@rpc("authority", "call_local", "reliable")
func _sync_orders(orders_payload: Array) -> void:
	var rebuilt: Array[Dictionary] = []
	for o: Variant in orders_payload:
		if o is Dictionary:
			rebuilt.append(o)
	active_orders = rebuilt
	order_queue_updated.emit(active_orders)

@rpc("authority", "call_local", "reliable")
func _sync_flow(flow_val: float, rush_active: bool, rush_rem: float) -> void:
	kitchen_flow = flow_val
	rush_rhythm_active = rush_active
	rush_rhythm_remaining = rush_rem
	flow_updated.emit(kitchen_flow, rush_rhythm_active, rush_rhythm_remaining)

@rpc("authority", "call_local", "reliable")
func _sync_shift_ended(summary: Dictionary) -> void:
	shift_active = false
	shift_ended.emit(summary)
