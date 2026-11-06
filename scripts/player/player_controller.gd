class_name PlayerController
extends CharacterBody3D
## 2.5D Kitchen Chef Controller supporting both Android virtual joystick + touch buttons
## and Desktop WASD/Arrow + E/Space/F controls.
## Synchronizes position and carried item state through the authoritative host.

const BASE_MOVE_SPEED: float = 6.2
const KITCHEN_BOUNDS_X: float = 8.8
const KITCHEN_BOUNDS_Z: float = 5.2

@export var peer_id: int = 1
@export var character_id: String = "linh"
@export var specialization_id: String = "prep_master"

var carried_item: Dictionary = {}
var external_move_vector: Vector2 = Vector2.ZERO # Set by mobile virtual joystick or client input
var body_mesh: MeshInstance3D = null
var carried_mesh: MeshInstance3D = null
var name_label: Label3D = null

func _ready() -> void:
	_build_visuals()
	_refresh_visuals()

func _build_visuals() -> void:
	var col: CollisionShape3D = CollisionShape3D.new()
	var cap: CapsuleShape3D = CapsuleShape3D.new()
	cap.radius = 0.42
	cap.height = 1.4
	col.shape = cap
	col.position = Vector3(0.0, 0.7, 0.0)
	add_child(col)

	body_mesh = MeshInstance3D.new()
	var cyl: CapsuleMesh = CapsuleMesh.new()
	cyl.radius = 0.42
	cyl.height = 1.4
	body_mesh.mesh = cyl
	body_mesh.position = Vector3(0.0, 0.7, 0.0)
	add_child(body_mesh)

	carried_mesh = MeshInstance3D.new()
	var box: BoxMesh = BoxMesh.new()
	box.size = Vector3(0.45, 0.3, 0.45)
	carried_mesh.mesh = box
	carried_mesh.position = Vector3(0.0, 1.1, 0.55)
	carried_mesh.visible = false
	add_child(carried_mesh)

	name_label = Label3D.new()
	name_label.position = Vector3(0.0, 1.9, 0.0)
	name_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	name_label.font_size = 26
	name_label.outline_size = 8
	add_child(name_label)

func configure_chef(p_peer_id: int, p_char_id: String, p_spec_id: String) -> void:
	peer_id = p_peer_id
	character_id = p_char_id
	specialization_id = p_spec_id
	_refresh_visuals()

func get_effective_move_speed() -> float:
	var mult: float = 1.0
	if specialization_id == "market_runner" and GameData.specializations.has("market_runner"):
		mult = GameData.specializations["market_runner"].move_speed_multiplier
	return BASE_MOVE_SPEED * mult

func apply_movement(input_dir: Vector2, delta: float) -> void:
	var clamped_dir: Vector2 = input_dir
	if clamped_dir.length() > 1.0:
		clamped_dir = clamped_dir.normalized()

	var speed: float = get_effective_move_speed()
	velocity = Vector3(clamped_dir.x * speed, 0.0, clamped_dir.y * speed)
	position += velocity * delta
	position.x = clampf(position.x, -KITCHEN_BOUNDS_X, KITCHEN_BOUNDS_X)
	position.z = clampf(position.z, -KITCHEN_BOUNDS_Z, KITCHEN_BOUNDS_Z)
	position.y = 0.0

func set_carried_item(item_data: Dictionary) -> void:
	carried_item = item_data.duplicate(true)
	_refresh_visuals()

func clear_carried_item() -> Dictionary:
	var prev: Dictionary = carried_item.duplicate(true)
	carried_item.clear()
	_refresh_visuals()
	return prev

func _refresh_visuals() -> void:
	if body_mesh == null:
		return
	var char_info: Dictionary = GameData.characters.get(character_id, GameData.characters["linh"])
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = char_info.get("color", Color(0.2, 0.7, 0.6))
	mat.roughness = 0.45
	body_mesh.material_override = mat

	var spec_name: String = ""
	if GameData.specializations.has(specialization_id):
		spec_name = GameData.specializations[specialization_id].display_name

	var item_str: String = "Hands Empty"
	if not carried_item.is_empty():
		item_str = "Carrying: %s" % carried_item.get("display_name", carried_item.get("item_id", "Item"))
		carried_mesh.visible = true
		var item_mat: StandardMaterial3D = StandardMaterial3D.new()
		var iid: String = str(carried_item.get("item_id", ""))
		if GameData.ingredients.has(iid):
			item_mat.albedo_color = GameData.ingredients[iid].color
		else:
			item_mat.albedo_color = Color(0.95, 0.9, 0.8)
		carried_mesh.material_override = item_mat
	else:
		carried_mesh.visible = false

	if name_label != null:
		name_label.text = "%s [%s]\n%s" % [char_info.get("display_name", "Chef"), spec_name, item_str]
