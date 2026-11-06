class_name WashingStation
extends StationBase
## Washing Station implementing Q3 4-stroke forgiving scrub sequence.
## Restores dirty plates from the Return Tray back into the shared usable clean plate stack.

const REQUIRED_SCRUB_STROKES: int = 4

var dirty_plates_in_sink: int = 0
var current_scrub_strokes: int = 0

func _ready() -> void:
	super._ready()
	_refresh_visual()

func load_dirty_plate_from_return() -> Dictionary:
	if not ShiftManager.take_dirty_plate_from_return():
		return {"ok": false, "reason": "No dirty plates waiting at the Return Tray."}
	dirty_plates_in_sink += 1
	_refresh_visual()
	return {"ok": true, "dirty_plates_in_sink": dirty_plates_in_sink}

func load_carried_dirty_plate() -> Dictionary:
	dirty_plates_in_sink += 1
	_refresh_visual()
	return {"ok": true, "dirty_plates_in_sink": dirty_plates_in_sink}

func perform_scrub_stroke() -> Dictionary:
	if dirty_plates_in_sink <= 0:
		# Auto-pull from return tray if available for forgiving UX
		if ShiftManager.take_dirty_plate_from_return():
			dirty_plates_in_sink += 1
		else:
			return {"ok": false, "reason": "No dirty plates in sink or return tray."}

	current_scrub_strokes += 1
	var plate_cleaned: bool = false
	if current_scrub_strokes >= REQUIRED_SCRUB_STROKES:
		current_scrub_strokes = 0
		dirty_plates_in_sink -= 1
		plate_cleaned = true
		ShiftManager.return_washed_plate()

	_refresh_visual()
	return {
		"ok": true,
		"strokes": current_scrub_strokes,
		"plate_cleaned": plate_cleaned,
		"clean_plates_available": ShiftManager.clean_plates,
		"dirty_plates_in_sink": dirty_plates_in_sink,
	}

func take_clean_vessel(preferred_vessel: String = "plate") -> Dictionary:
	if not ShiftManager.take_clean_plate():
		return {"ok": false, "reason": "No clean plates/bowls left! Wash dirty plates at the Sink."}
	var v: String = preferred_vessel if preferred_vessel in ["plate", "bowl"] else "plate"
	_refresh_visual()
	return {
		"ok": true,
		"item": {
			"uid": "vessel_%d" % Time.get_ticks_usec(),
			"item_id": v,
			"display_name": "Clean %s" % v.capitalize(),
		}
	}

func get_state() -> Dictionary:
	return {
		"dirty_plates_in_sink": dirty_plates_in_sink,
		"current_scrub_strokes": current_scrub_strokes,
		"clean_plates": ShiftManager.clean_plates,
		"dirty_return": ShiftManager.dirty_plates_at_return,
	}

func apply_state(state: Dictionary) -> void:
	dirty_plates_in_sink = int(state.get("dirty_plates_in_sink", 0))
	current_scrub_strokes = int(state.get("current_scrub_strokes", 0))
	_refresh_visual()

func _refresh_visual() -> void:
	update_label_text(
		"WASHING SINK & DISH RACK\nClean Stack: %d | Dirty in Sink: %d (Return: %d)\nScrub Progress: [%d/%d]" % [
			ShiftManager.clean_plates,
			dirty_plates_in_sink,
			ShiftManager.dirty_plates_at_return,
			current_scrub_strokes,
			REQUIRED_SCRUB_STROKES
		],
		Color(0.45, 0.88, 1.0)
	)
