class_name StationBase
extends Node3D
## Base class for all interactive kitchen stations in the 2.5D kitchen.
## Enforces host distance validation and state synchronization.

signal station_state_changed(state: Dictionary)

const MAX_INTERACT_DISTANCE: float = 2.6

@export var station_id: String = "station"
@export var station_title: String = "Station"
@export var assigned_zone: String = "shared" # "linh_west", "jiwon_east", or "shared"

var label_3d: Label3D = null

func _ready() -> void:
	_setup_visual_label()

func _setup_visual_label() -> void:
	if has_node("StatusLabel3D"):
		label_3d = get_node("StatusLabel3D") as Label3D
	else:
		label_3d = Label3D.new()
		label_3d.name = "StatusLabel3D"
		label_3d.position = Vector3(0.0, 1.55, 0.0)
		label_3d.billboard = BaseMaterial3D.BILLBOARD_ENABLED
		label_3d.font_size = 28
		label_3d.outline_size = 8
		label_3d.text = station_title
		add_child(label_3d)

func is_within_range(player_pos: Vector3) -> bool:
	var flat_a: Vector2 = Vector2(global_position.x, global_position.z)
	var flat_b: Vector2 = Vector2(player_pos.x, player_pos.z)
	return flat_a.distance_to(flat_b) <= MAX_INTERACT_DISTANCE

func update_label_text(text: String, modulate_color: Color = Color.WHITE) -> void:
	if label_3d != null:
		label_3d.text = text
		label_3d.modulate = modulate_color

func get_prompt_text(_carried_item: Dictionary) -> String:
	return station_title
