class_name PartTimeWorker
extends Node3D
## Part-Time Worker hired by Jiwon ("Call in the Crew").
## Implements a simple, reliable state machine:
## ENTRANCE -> TRAVEL_TO_STATION -> WORK (Prep or Scrub) -> TRAVEL_TO_EXIT -> DESPAWN.
## Synchronized host-authoritatively over LAN.

const CharacterProgressionData = preload("res://scripts/data/character_progression_data.gd")
const RecipeData = preload("res://scripts/data/recipe_data.gd")

enum WorkerState {
	WALKING_IN = 0,
	WORKING = 1,
	WALKING_OUT = 2,
	EXPIRED = 3,
}

signal worker_expired(worker_id: String)

const ENTRANCE_POS: Vector3 = Vector3(8.5, 0.0, 4.8)
const WORKER_WALK_SPEED: float = 3.8

@export var worker_id: String = ""
@export var role_id: String = "prep_assistant" # "prep_assistant" or "dishwasher"

var current_state: int = WorkerState.WALKING_IN
var contract_remaining_sec: float = 20.0
var task_tick_accum: float = 0.0
var task_interval_sec: float = 1.1

var target_station: StationBase = null
var target_station_pos: Vector3 = Vector3.ZERO

var body_mesh: MeshInstance3D = null
var worker_sprite: Sprite3D = null
var label_3d: Label3D = null
var walk_anim_time: float = 0.0

func _ready() -> void:
	position = ENTRANCE_POS
	_build_visuals()

func initialize_worker(p_worker_id: String, p_role_id: String, p_station: StationBase) -> void:
	worker_id = p_worker_id
	role_id = p_role_id
	target_station = p_station
	if target_station != null:
		target_station_pos = target_station.position + Vector3(0.0, 0.0, 0.9)
	if GameData.worker_catalog.has(role_id):
		var spec: CharacterProgressionData.WorkerSpec = GameData.worker_catalog[role_id]
		contract_remaining_sec = spec.contract_duration
		task_interval_sec = spec.task_interval
	_refresh_visuals()

func _build_visuals() -> void:
	body_mesh = MeshInstance3D.new()
	var cap: CapsuleMesh = CapsuleMesh.new()
	cap.radius = 0.35
	cap.height = 1.25
	body_mesh.mesh = cap
	body_mesh.position = Vector3(0.0, 0.62, 0.0)
	add_child(body_mesh)

	worker_sprite = Sprite3D.new()
	worker_sprite.position = Vector3(0.0, 0.85, 0.05)
	worker_sprite.pixel_size = 0.024
	worker_sprite.billboard = BaseMaterial3D.BILLBOARD_FIXED_Y
	worker_sprite.transparent = true
	worker_sprite.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	add_child(worker_sprite)

	label_3d = Label3D.new()
	label_3d.position = Vector3(0.0, 1.6, 0.0)
	label_3d.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	label_3d.font_size = 22
	label_3d.outline_size = 6
	add_child(label_3d)

func _process(delta: float) -> void:
	if current_state == WorkerState.EXPIRED:
		return

	# Host is authoritative over movement and task ticks
	if NetworkManager.is_host():
		_tick_host_logic(delta)
	_refresh_visuals()

func _tick_host_logic(delta: float) -> void:
	contract_remaining_sec -= delta

	match current_state:
		WorkerState.WALKING_IN:
			var flat_cur: Vector2 = Vector2(position.x, position.z)
			var flat_tar: Vector2 = Vector2(target_station_pos.x, target_station_pos.z)
			if flat_cur.distance_to(flat_tar) <= 0.35:
				current_state = WorkerState.WORKING
				task_tick_accum = 0.0
				if worker_sprite != null:
					worker_sprite.rotation.z = 0.0
			else:
				var move_dir: Vector2 = (flat_tar - flat_cur).normalized()
				position.x += move_dir.x * WORKER_WALK_SPEED * delta
				position.z += move_dir.y * WORKER_WALK_SPEED * delta
				walk_anim_time += delta * 12.0
				var tilt_rad: float = sin(walk_anim_time) * deg_to_rad(17.5)
				if worker_sprite != null:
					worker_sprite.rotation.z = lerp_angle(worker_sprite.rotation.z, tilt_rad, delta * 18.0)
					if move_dir.x < -0.05:
						worker_sprite.flip_h = true
					elif move_dir.x > 0.05:
						worker_sprite.flip_h = false

		WorkerState.WORKING:
			if contract_remaining_sec <= 0.0:
				current_state = WorkerState.WALKING_OUT
				return

			task_tick_accum += delta
			if task_tick_accum >= task_interval_sec:
				task_tick_accum = 0.0
				_execute_worker_task()

		WorkerState.WALKING_OUT:
			var flat_cur_out: Vector2 = Vector2(position.x, position.z)
			var flat_exit: Vector2 = Vector2(ENTRANCE_POS.x, ENTRANCE_POS.z)
			if flat_cur_out.distance_to(flat_exit) <= 0.4:
				current_state = WorkerState.EXPIRED
				worker_expired.emit(worker_id)
				queue_free()
			else:
				var move_dir_out: Vector2 = (flat_exit - flat_cur_out).normalized()
				position.x += move_dir_out.x * WORKER_WALK_SPEED * delta
				position.z += move_dir_out.y * WORKER_WALK_SPEED * delta
				walk_anim_time += delta * 12.0
				var tilt_rad_out: float = sin(walk_anim_time) * deg_to_rad(17.5)
				if worker_sprite != null:
					worker_sprite.rotation.z = lerp_angle(worker_sprite.rotation.z, tilt_rad_out, delta * 18.0)
					if move_dir_out.x < -0.05:
						worker_sprite.flip_h = true
					elif move_dir_out.x > 0.05:
						worker_sprite.flip_h = false

func _execute_worker_task() -> void:
	if target_station == null or not is_instance_valid(target_station):
		return

	if role_id == "prep_assistant" and target_station is PrepStation:
		var prep: PrepStation = target_station as PrepStation
		if not prep.current_item.is_empty():
			var iid: String = str(prep.current_item.get("item_id", ""))
			if GameData.get_prep_output(iid) != "":
				var now_sec: float = float(Time.get_ticks_msec()) / 1000.0
				prep.perform_strike(now_sec, "", "prep_assistant", 1.0)
	elif role_id == "dishwasher" and target_station is WashingStation:
		var wash: WashingStation = target_station as WashingStation
		if wash.dirty_plates_in_sink > 0 or ShiftManager.dirty_plates_at_return > 0:
			wash.perform_scrub_stroke()

func _refresh_visuals() -> void:
	if label_3d == null or body_mesh == null:
		return
	var spec: CharacterProgressionData.WorkerSpec = GameData.worker_catalog.get(role_id, null)
	var col: Color = spec.uniform_color if spec != null else Color(0.4, 0.7, 0.4)
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = col
	body_mesh.material_override = mat

	if worker_sprite != null:
		var icon_name: String = "prep_counter_icon.png" if role_id == "prep_assistant" else "cleaning_gloves.png"
		var full_path: String = "res://assets/sprites/items/" + icon_name
		if ResourceLoader.exists(full_path):
			worker_sprite.texture = load(full_path)
			worker_sprite.visible = true

	var state_text: String = "Arriving..."
	if current_state == WorkerState.WORKING:
		state_text = "Working (%.0fs left)" % maxf(0.0, contract_remaining_sec)
	elif current_state == WorkerState.WALKING_OUT:
		state_text = "Contract Ended"
	label_3d.text = "PART-TIMER: %s\n[%s]" % [spec.display_name if spec != null else role_id, state_text]
	label_3d.modulate = Color(1.0, 0.9, 0.4)
