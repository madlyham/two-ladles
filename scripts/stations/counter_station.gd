class_name CounterStation
extends StationBase
## An empty holding counter / prep table station.
## Allows chefs to place any carried ingredient, dish, plate, or bowl down
## to free their hands, and pick it back up or swap items.

var held_item: Dictionary = {}

func place_item(p_item: Dictionary) -> bool:
	if not held_item.is_empty():
		return false
	held_item = p_item.duplicate(true)
	_update_counter_label()
	return true

func retrieve_item() -> Dictionary:
	if held_item.is_empty():
		return {}
	var prev: Dictionary = held_item.duplicate(true)
	held_item.clear()
	_update_counter_label()
	return prev

func swap_item(p_item: Dictionary) -> Dictionary:
	var prev: Dictionary = held_item.duplicate(true)
	held_item = p_item.duplicate(true)
	_update_counter_label()
	return prev

func _update_counter_label() -> void:
	if held_item.is_empty():
		update_label_text(station_title, Color.WHITE)
	else:
		var item_name: String = held_item.get("display_name", held_item.get("id", "Item"))
		update_label_text("%s\n[%s]" % [station_title, item_name], Color(0.95, 0.85, 0.45))

func apply_state(state: Dictionary) -> void:
	if state.has("held_item"):
		held_item = state["held_item"].duplicate(true)
	else:
		held_item = state.duplicate(true)
	_update_counter_label()

func get_prompt_text(carried_item: Dictionary) -> String:
	if held_item.is_empty():
		if carried_item.is_empty():
			return "%s: Empty Counter" % station_title
		return "%s: [Press E to Place Item]" % station_title
	else:
		var item_name: String = held_item.get("display_name", held_item.get("id", "Item"))
		if carried_item.is_empty():
			return "%s: [Press E to Pick Up %s]" % [station_title, item_name]
		return "%s: [Press E to Swap with %s]" % [station_title, item_name]
