class_name PassCounter
extends StationBase
## Physical shared Pass Counter dividing Linh's West Zone and Jiwon's East Zone.
## Tracks placed items, peer IDs, and timestamps to award the +15 Kitchen Flow handoff bonus
## when Player B retrieves Player A's ingredient within 4 seconds.

const MAX_SLOTS: int = 3

# Each slot dictionary: { "item": Dictionary, "placed_by_peer": int, "placed_at_ms": int }
var slots: Array[Dictionary] = []

func _ready() -> void:
	super._ready()
	_refresh_visual()

func place_item(item_data: Dictionary, placed_by_peer: int, current_time_ms: int = -1) -> Dictionary:
	if slots.size() >= MAX_SLOTS:
		return {"ok": false, "reason": "Pass Counter is full (3/3 items)!"}
	if current_time_ms < 0:
		current_time_ms = Time.get_ticks_msec()

	var entry: Dictionary = {
		"item": item_data.duplicate(true),
		"placed_by_peer": placed_by_peer,
		"placed_at_ms": current_time_ms,
	}
	slots.append(entry)
	_refresh_visual()
	return {"ok": true, "slot_index": slots.size() - 1}

func retrieve_item(retrieved_by_peer: int, current_time_ms: int = -1, slot_index: int = -1) -> Dictionary:
	if slots.is_empty():
		return {"ok": false, "reason": "Pass Counter is empty."}
	if current_time_ms < 0:
		current_time_ms = Time.get_ticks_msec()

	var target_idx: int = slot_index
	if target_idx < 0 or target_idx >= slots.size():
		# Prefer an item placed by the OTHER player so partner handoff is prioritized naturally
		target_idx = slots.size() - 1
		for i: int in range(slots.size() - 1, -1, -1):
			if int(slots[i]["placed_by_peer"]) != retrieved_by_peer:
				target_idx = i
				break

	var entry: Dictionary = slots[target_idx]
	slots.remove_at(target_idx)

	var placed_by: int = int(entry["placed_by_peer"])
	var placed_at: int = int(entry["placed_at_ms"])
	var elapsed_ms: int = max(0, current_time_ms - placed_at)
	var item_data: Dictionary = entry["item"]

	var handoff_result: Dictionary = ShiftManager.evaluate_pass_counter_handoff(
		item_data,
		placed_by,
		retrieved_by_peer,
		elapsed_ms
	)

	_refresh_visual()
	return {
		"ok": true,
		"item": item_data,
		"handoff": handoff_result,
	}

func get_state() -> Dictionary:
	return {"slots": slots.duplicate(true)}

func apply_state(state: Dictionary) -> void:
	slots.clear()
	for s: Variant in state.get("slots", []):
		if s is Dictionary:
			slots.append(s)
	_refresh_visual()

func _refresh_visual() -> void:
	if slots.is_empty():
		update_label_text("PASS COUNTER (Linh <-> Jiwon)\n[Empty - Pass Prepped/Cooked Items for +15 Flow]", Color(0.65, 0.9, 1.0))
		return
	var names: Array[String] = []
	for s: Dictionary in slots:
		var it: Dictionary = s.get("item", {})
		names.append(str(it.get("display_name", it.get("item_id", "Item"))))
	update_label_text("PASS COUNTER (%d/%d)\n%s" % [slots.size(), MAX_SLOTS, " | ".join(names)], Color(0.35, 1.0, 0.75))
