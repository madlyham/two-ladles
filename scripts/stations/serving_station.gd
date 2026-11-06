class_name ServingStation
extends StationBase
## Serving Counter: validates completed dish against active customer orders via ShiftManager,
## awards score + bonuses, and generates a dirty plate at the return tray.

func _ready() -> void:
	super._ready()
	_refresh_visual()

func submit_dish(item_data: Dictionary, serving_peer_id: int = 1) -> Dictionary:
	if not bool(item_data.get("completed_dish", false)):
		return {"ok": false, "reason": "Only completed dishes can be served at the Serving Counter!"}
	var result: Dictionary = ShiftManager.serve_completed_dish(item_data, serving_peer_id)
	_refresh_visual()
	return result

func _refresh_visual() -> void:
	update_label_text(
		"SERVING WINDOW\n[Deliver Completed Dishes Here]\nDirty Plates at Return: %d" % ShiftManager.dirty_plates_at_return,
		Color(1.0, 0.85, 0.35)
	)
