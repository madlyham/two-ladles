class_name PlayerController
extends CharacterBody3D
## 2.5D Kitchen Chef Controller supporting both Android virtual joystick + touch buttons
## and Desktop WASD/Arrow + E/Space/F controls.
## Synchronizes position and carried item state through the authoritative host.

const CharacterProgressionData = preload("res://scripts/data/character_progression_data.gd")
const RecipeData = preload("res://scripts/data/recipe_data.gd")

const BASE_MOVE_SPEED: float = 6.2
const KITCHEN_BOUNDS_X: float = 8.8
const KITCHEN_BOUNDS_Z: float = 5.2

@export var peer_id: int = 1
@export var character_id: String = "linh"
@export var specialization_id: String = "prep_master"
@export var character_tier: int = 1

var carried_item: Dictionary = {}
var external_move_vector: Vector2 = Vector2.ZERO # Set by mobile virtual joystick or client input
var body_mesh: MeshInstance3D = null
var carried_mesh: MeshInstance3D = null
var third_arm_mesh: MeshInstance3D = null
var third_arm_glow_mesh: MeshInstance3D = null
var character_sprite: Sprite3D = null
var carried_sprite: Sprite3D = null
var name_label: Label3D = null
var walk_anim_time: float = 0.0

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

	# Linh's Visual 3rd Arm (Evolves from Rustic Mechanical at Tier 2 to Precision White-and-Blue at Tier 3)
	third_arm_mesh = MeshInstance3D.new()
	var arm_box: BoxMesh = BoxMesh.new()
	arm_box.size = Vector3(0.18, 0.55, 0.18)
	third_arm_mesh.mesh = arm_box
	third_arm_mesh.position = Vector3(0.38, 1.15, -0.22)
	third_arm_mesh.rotation_degrees = Vector3(18.0, 0.0, -24.0)
	third_arm_mesh.visible = false
	add_child(third_arm_mesh)

	third_arm_glow_mesh = MeshInstance3D.new()
	var glow_box: BoxMesh = BoxMesh.new()
	glow_box.size = Vector3(0.06, 0.48, 0.06)
	third_arm_glow_mesh.mesh = glow_box
	third_arm_glow_mesh.position = Vector3(0.42, 1.15, -0.22)
	third_arm_glow_mesh.visible = false
	add_child(third_arm_glow_mesh)

	# 2D Character Billboard Sprite
	character_sprite = Sprite3D.new()
	character_sprite.name = "CharacterSprite"
	character_sprite.position = Vector3(0.0, 0.95, 0.05)
	character_sprite.pixel_size = 0.0036
	character_sprite.billboard = BaseMaterial3D.BILLBOARD_FIXED_Y
	character_sprite.transparent = true
	character_sprite.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	add_child(character_sprite)

	# 2D Carried Item Sprite Icon
	carried_sprite = Sprite3D.new()
	carried_sprite.name = "CarriedSprite"
	carried_sprite.position = Vector3(0.0, 1.45, 0.45)
	carried_sprite.pixel_size = 0.022
	carried_sprite.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	carried_sprite.transparent = true
	carried_sprite.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	carried_sprite.visible = false
	add_child(carried_sprite)

	name_label = Label3D.new()
	name_label.position = Vector3(0.0, 1.9, 0.0)
	name_label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	name_label.font_size = 26
	name_label.outline_size = 8
	add_child(name_label)

func configure_chef(p_peer_id: int, p_char_id: String, p_spec_id: String, p_tier: int = 1) -> void:
	peer_id = p_peer_id
	character_id = p_char_id
	specialization_id = p_spec_id
	character_tier = p_tier
	_refresh_visuals()

func get_task_speed_multiplier() -> float:
	if character_id == "linh" and GameData.linh_tiers.has(character_tier):
		return float(GameData.linh_tiers[character_tier].task_speed_multiplier)
	return 1.0

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

	# 30% Tilt movement animation (waddle while walking)
	if clamped_dir.length() > 0.08:
		walk_anim_time += delta * 14.0
		# Oscillate tilt up to ~17.5 degrees (approx 30% arc)
		var tilt_rad: float = sin(walk_anim_time) * deg_to_rad(17.5)
		if character_sprite != null:
			character_sprite.rotation.z = lerp_angle(character_sprite.rotation.z, tilt_rad, delta * 18.0)
			if clamped_dir.x < -0.05:
				character_sprite.flip_h = true
			elif clamped_dir.x > 0.05:
				character_sprite.flip_h = false
		if third_arm_mesh != null and third_arm_mesh.visible:
			third_arm_mesh.rotation.z = lerp_angle(third_arm_mesh.rotation.z, tilt_rad - 0.4, delta * 18.0)
	else:
		walk_anim_time = 0.0
		if character_sprite != null:
			character_sprite.rotation.z = lerp_angle(character_sprite.rotation.z, 0.0, delta * 14.0)

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

	var char_color: Color = Color(0.2, 0.7, 0.6)
	if character_id == "linh":
		char_color = GameData.characters["linh"]["color"]
		# Linh's Visual 3rd Arm Evolution
		if character_tier >= 2 and GameData.linh_tiers.has(character_tier):
			var tier_info: CharacterProgressionData.LinhTierSpec = GameData.linh_tiers[character_tier]
			third_arm_mesh.visible = true
			var arm_mat: StandardMaterial3D = StandardMaterial3D.new()
			arm_mat.albedo_color = tier_info.arm_color_primary
			arm_mat.metallic = 0.8 if character_tier == 2 else 0.2
			arm_mat.roughness = 0.5 if character_tier == 2 else 0.1
			third_arm_mesh.material_override = arm_mat

			if character_tier == 3:
				third_arm_glow_mesh.visible = true
				var glow_mat: StandardMaterial3D = StandardMaterial3D.new()
				glow_mat.albedo_color = tier_info.arm_color_glow
				glow_mat.emission_enabled = true
				glow_mat.emission = tier_info.arm_color_glow
				glow_mat.emission_energy_multiplier = 2.5
				third_arm_glow_mesh.material_override = glow_mat
			else:
				third_arm_glow_mesh.visible = false
		else:
			if third_arm_mesh != null:
				third_arm_mesh.visible = false
			if third_arm_glow_mesh != null:
				third_arm_glow_mesh.visible = false
	elif character_id == "jiwon":
		if GameData.jiwon_tiers.has(character_tier):
			char_color = GameData.jiwon_tiers[character_tier].outfit_color
		else:
			char_color = GameData.characters["jiwon"]["color"]
		if third_arm_mesh != null:
			third_arm_mesh.visible = false
		if third_arm_glow_mesh != null:
			third_arm_glow_mesh.visible = false

	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = char_color
	mat.roughness = 0.45
	body_mesh.material_override = mat

	# Update 2D character billboard sprite
	if character_sprite != null:
		var sprite_path: String = "res://assets/sprites/characters/%s_tier%d.png" % [character_id, character_tier]
		if ResourceLoader.exists(sprite_path):
			character_sprite.texture = load(sprite_path)
			character_sprite.visible = true
			body_mesh.visible = false
		else:
			character_sprite.visible = false
			body_mesh.visible = true

	var spec_name: String = ""
	if GameData.specializations.has(specialization_id):
		spec_name = GameData.specializations[specialization_id].display_name

	var tier_label: String = "Tier %d" % character_tier
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

		if carried_sprite != null:
			var icon_path: String = _get_item_sprite_path(iid)
			if ResourceLoader.exists(icon_path):
				carried_sprite.texture = load(icon_path)
				carried_sprite.visible = true
				carried_mesh.visible = false
			else:
				carried_sprite.visible = false
	else:
		carried_mesh.visible = false
		if carried_sprite != null:
			carried_sprite.visible = false

	if name_label != null:
		var display_cname: String = "Linh" if character_id == "linh" else "Jiwon"
		name_label.text = "%s [%s - %s]\n%s" % [display_cname, spec_name, tier_label, item_str]

func _get_item_sprite_path(item_id: String) -> String:
	match item_id:
		"pork_belly": return "res://assets/sprites/items/pork_belly_raw.png"
		"pork_portioned": return "res://assets/sprites/items/pork_portioned.png"
		"marinated_bulgogi", "beef_bulgogi": return "res://assets/sprites/items/beef_bulgogi_raw.png"
		"bulgogi_portioned", "seared_bulgogi": return "res://assets/sprites/items/bulgogi_portioned.png"
		"baguette": return "res://assets/sprites/items/baguette_bread.png"
		"cabbage": return "res://assets/sprites/items/cabbage_fresh.png"
		"kimchi_slaw": return "res://assets/sprites/items/kimchi_slaw.png"
		"pickled_daikon_carrot", "pickled_veg": return "res://assets/sprites/items/pickled_veg.png"
		"tteok_raw", "tteok_portioned": return "res://assets/sprites/items/tteok_portioned.png"
		"fish_cake_raw": return "res://assets/sprites/items/fish_cake_raw.png"
		"eomuk_prepared": return "res://assets/sprites/items/eomuk_prepared.png"
		"bowl", "pho_bo", "tteokbokki": return "res://assets/sprites/items/dish_bowl.png"
		"plate", "banh_mi_thit", "bulgogi_banh_mi": return "res://assets/sprites/items/dish_plate.png"
		"noodles_raw", "noodles_blanched": return "res://assets/sprites/items/dish_bowl.png"
		"broth_hot": return "res://assets/sprites/items/broth_pot.png"
		"burnt_waste": return "res://assets/sprites/items/cooking_pan_icon.png"
		_: return "res://assets/sprites/items/dish_plate.png"
