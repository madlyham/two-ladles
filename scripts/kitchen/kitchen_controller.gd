class_name KitchenController
extends Node3D
## Authoritative 2.5D Kitchen Scene Manager.
## Builds the asymmetric Vietnam-Korea Cross-Prep Kitchen:
## - WEST ZONE (Linh's Home Side): Meat/Starch Crates, Prep Counter West, Stockpot, Noodle Blancher, Assembly West, Serving Window.
## - CENTER: Shared Pass Counter (3 slots, 4s handoff window for +15 Kitchen Flow) & Vessel Rack / Trash.
## - EAST ZONE (Jiwon's Home Side): Veg/Seafood/Sauce Crates, Prep Counter East, Grill/Sear Pan, Simmer Wok, Assembly East, Washing Sink.

const CharacterProgressionData = preload("res://scripts/data/character_progression_data.gd")
const RecipeData = preload("res://scripts/data/recipe_data.gd")
const CounterStation = preload("res://scripts/stations/counter_station.gd")

signal local_prompt_changed(prompt_text: String)
signal status_toast(message: String, color: Color)

var players_by_peer: Dictionary = {} # peer_id (int) -> PlayerController
var stations: Dictionary = {} # station_id (String) -> StationBase
var crate_items: Dictionary = {} # station_id (String) -> String (raw item_id)
var active_workers: Dictionary = {} # worker_id -> PartTimeWorker
var worker_cooldowns: Dictionary = {} # role_id -> float
static var _worker_uid_counter: int = 1

var local_peer_id: int = 1
var virtual_joystick_vec: Vector2 = Vector2.ZERO

func _ready() -> void:
	_build_environment()
	_build_perimeter_walls()
	_build_stations()
	_spawn_players()

func _build_environment() -> void:
	# Fixed 2.5D Orthographic / Isometric-inspired Camera
	var cam: Camera3D = Camera3D.new()
	cam.name = "KitchenCamera3D"
	cam.projection = Camera3D.PROJECTION_ORTHOGONAL
	cam.size = 13.5
	cam.position = Vector3(0.0, 11.5, 9.5)
	cam.look_at_from_position(Vector3(0.0, 11.5, 9.5), Vector3(0.0, 0.0, 0.2), Vector3.UP)
	add_child(cam)

	# Warm Directional Sun Light
	var sun: DirectionalLight3D = DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-52.0, 28.0, 0.0)
	sun.light_color = Color(1.0, 0.94, 0.84)
	sun.light_energy = 1.25
	sun.shadow_enabled = true
	add_child(sun)

	# Kitchen Floor (West = Linh Sage-Teal tile, East = Jiwon Terracotta tile)
	_create_box("FloorWest", Vector3(-4.6, -0.1, 0.0), Vector3(9.2, 0.2, 11.5), Color(0.24, 0.28, 0.27), "floor_tile_light.png")
	_create_box("FloorEast", Vector3(4.6, -0.1, 0.0), Vector3(9.2, 0.2, 11.5), Color(0.30, 0.24, 0.22), "floor_tile_light.png")
	_create_box("CenterDividerLine", Vector3(0.0, -0.08, 0.0), Vector3(0.35, 0.21, 11.5), Color(0.85, 0.72, 0.35))

func _build_perimeter_walls() -> void:
	_create_static_wall("Wall_North", Vector3(0.0, 1.5, -5.8), Vector3(19.2, 3.0, 0.8))
	_create_static_wall("Wall_South", Vector3(0.0, 1.5, 5.8), Vector3(19.2, 3.0, 0.8))
	_create_static_wall("Wall_West", Vector3(-9.4, 1.5, 0.0), Vector3(0.8, 3.0, 12.0))
	_create_static_wall("Wall_East", Vector3(9.4, 1.5, 0.0), Vector3(0.8, 3.0, 12.0))

func _create_static_wall(wall_name: String, pos: Vector3, sz: Vector3) -> StaticBody3D:
	var sb: StaticBody3D = StaticBody3D.new()
	sb.name = wall_name
	sb.position = pos
	var col: CollisionShape3D = CollisionShape3D.new()
	var box: BoxShape3D = BoxShape3D.new()
	box.size = sz
	col.shape = box
	sb.add_child(col)
	add_child(sb)
	return sb

func _load_tile_texture(rel_path: String) -> Texture2D:
	var res_path: String = "res://assets/sprites/kitchen/tiles/" + rel_path
	if ResourceLoader.exists(res_path):
		var res: Resource = load(res_path)
		if res is Texture2D:
			return res
	var global_p: String = ProjectSettings.globalize_path(res_path)
	if FileAccess.file_exists(global_p):
		var img: Image = Image.load_from_file(global_p)
		if img != null and not img.is_empty():
			return ImageTexture.create_from_image(img)
	return null

func _create_station_structure(box_name: String, pos: Vector3, sz: Vector3, col: Color, appliance_tile: String = "") -> StaticBody3D:
	var body: StaticBody3D = StaticBody3D.new()
	body.name = box_name + "_Body"
	body.position = pos

	# Physical Collision Box (prevents character phasing through station)
	var col_shape: CollisionShape3D = CollisionShape3D.new()
	col_shape.name = "Collision"
	var box_shape: BoxShape3D = BoxShape3D.new()
	box_shape.size = sz
	col_shape.shape = box_shape
	body.add_child(col_shape)

	# Clean 3D Industrial Counter Base Mesh
	var mi: MeshInstance3D = MeshInstance3D.new()
	mi.name = box_name + "_Mesh"
	var bm: BoxMesh = BoxMesh.new()
	bm.size = sz
	mi.mesh = bm
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	mat.albedo_color = col
	mat.roughness = 0.4
	mat.metallic = 0.45
	mi.material_override = mat
	body.add_child(mi)

	# 2D Sliced Pixel-Art Appliance Visual
	if appliance_tile != "":
		var spr: Sprite3D = Sprite3D.new()
		spr.name = "ApplianceSprite"
		spr.position = Vector3(0.0, sz.y * 0.5 + 0.42, 0.05)
		spr.pixel_size = 0.022
		spr.billboard = BaseMaterial3D.BILLBOARD_FIXED_Y
		spr.transparent = true
		spr.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
		var tex: Texture2D = _load_tile_texture(appliance_tile)
		if tex != null:
			spr.texture = tex
		body.add_child(spr)

	add_child(body)
	return body

func _create_box(box_name: String, pos: Vector3, sz: Vector3, col: Color, tile_tex: String = "") -> MeshInstance3D:
	var mi: MeshInstance3D = MeshInstance3D.new()
	mi.name = box_name
	var bm: BoxMesh = BoxMesh.new()
	bm.size = sz
	mi.mesh = bm
	mi.position = pos
	var mat: StandardMaterial3D = StandardMaterial3D.new()
	if tile_tex != "":
		var tex: Texture2D = _load_tile_texture(tile_tex)
		if tex != null:
			mat.albedo_texture = tex
			mat.uv1_scale = Vector3(sz.x * 1.5, sz.z * 1.5, 1.0)
			mat.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	mat.albedo_color = col
	mat.roughness = 0.55
	mat.metallic = 0.3
	mi.material_override = mat
	add_child(mi)
	return mi

func _add_station_icon(pos: Vector3, sprite_rel_path: String, scale_size: float = 0.024) -> Sprite3D:
	var spr: Sprite3D = Sprite3D.new()
	spr.position = pos + Vector3(0.0, 1.45, 0.0)
	spr.pixel_size = scale_size
	spr.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	spr.transparent = true
	spr.texture_filter = BaseMaterial3D.TEXTURE_FILTER_NEAREST
	var full_path: String = "res://assets/sprites/items/" + sprite_rel_path
	if ResourceLoader.exists(full_path):
		spr.texture = load(full_path)
	else:
		var global_p: String = ProjectSettings.globalize_path(full_path)
		if FileAccess.file_exists(global_p):
			var img: Image = Image.load_from_file(global_p)
			if img != null and not img.is_empty():
				spr.texture = ImageTexture.create_from_image(img)
	add_child(spr)
	return spr

func _build_stations() -> void:
	# --- CENTER: SHARED PASS COUNTER & TRASH ---
	var pass_st: PassCounter = PassCounter.new()
	pass_st.station_id = "pass_counter"
	pass_st.station_title = "PASS COUNTER"
	pass_st.assigned_zone = "shared"
	pass_st.position = Vector3(0.0, 0.0, -0.4)
	add_child(pass_st)
	_create_station_structure("PassMesh", Vector3(0.0, 0.45, -0.4), Vector3(1.6, 0.9, 3.2), Color(0.92, 0.76, 0.32), "pass_counter.png")
	_add_station_icon(Vector3(0.0, 0.45, -0.4), "dish_plate.png", 0.026)
	stations["pass_counter"] = pass_st

	var trash_st: StationBase = StationBase.new()
	trash_st.station_id = "trash_bin"
	trash_st.station_title = "TRASH BIN\n[Dispose Burnt/Unwanted Item]"
	trash_st.position = Vector3(0.0, 0.0, 3.6)
	add_child(trash_st)
	_create_station_structure("TrashMesh", Vector3(0.0, 0.4, 3.6), Vector3(1.1, 0.8, 1.1), Color(0.35, 0.35, 0.38), "trash_bin.png")
	_add_station_icon(Vector3(0.0, 0.4, 3.6), "dish_brush.png", 0.022)
	stations["trash_bin"] = trash_st

	# --- WEST ZONE (LINH'S SIDE): Preps Proteins/Starches for Jiwon + Cooks Pho Stock/Noodles + Assembly West + Serving ---
	var prep_west: PrepStation = PrepStation.new()
	prep_west.station_id = "prep_west"
	prep_west.station_title = "LINH PREP (West)\n[Pork/Bulgogi/Tteok -> Pass to Jiwon]"
	prep_west.assigned_zone = "linh_west"
	prep_west.position = Vector3(-3.2, 0.0, -2.6)
	add_child(prep_west)
	_create_station_structure("PrepWestMesh", Vector3(-3.2, 0.45, -2.6), Vector3(2.2, 0.9, 1.3), Color(0.25, 0.68, 0.58), "prep_west.png")
	_add_station_icon(Vector3(-3.2, 0.45, -2.6), "prep_counter_icon.png", 0.026)
	stations["prep_west"] = prep_west

	# West Holding Counter
	var counter_west: CounterStation = CounterStation.new()
	counter_west.station_id = "counter_west"
	counter_west.station_title = "HOLDING TABLE (West)"
	counter_west.assigned_zone = "linh_west"
	counter_west.position = Vector3(-3.2, 0.0, -0.6)
	add_child(counter_west)
	_create_station_structure("CounterWestMesh", Vector3(-3.2, 0.45, -0.6), Vector3(2.0, 0.9, 1.1), Color(0.65, 0.70, 0.74), "serving_station.png")
	_add_station_icon(Vector3(-3.2, 0.45, -0.6), "dish_plate.png", 0.024)
	stations["counter_west"] = counter_west

	var stockpot_st: CookingStation = CookingStation.new()
	stockpot_st.station_id = "stockpot"
	stockpot_st.appliance_id = "stockpot"
	stockpot_st.station_title = "Dedicated Phở Stockpot"
	stockpot_st.assigned_zone = "linh_west"
	stockpot_st.position = Vector3(-6.5, 0.0, -3.2)
	add_child(stockpot_st)
	_create_station_structure("StockpotMesh", Vector3(-6.5, 0.45, -3.2), Vector3(1.6, 0.9, 1.4), Color(0.82, 0.58, 0.22), "stockpot.png")
	_add_station_icon(Vector3(-6.5, 0.45, -3.2), "broth_pot.png", 0.026)
	stations["stockpot"] = stockpot_st

	var blancher_st: CookingStation = CookingStation.new()
	blancher_st.station_id = "noodle_blancher"
	blancher_st.appliance_id = "noodle_blancher"
	blancher_st.station_title = "Noodle Blancher"
	blancher_st.assigned_zone = "linh_west"
	blancher_st.position = Vector3(-6.5, 0.0, -1.0)
	add_child(blancher_st)
	_create_station_structure("BlancherMesh", Vector3(-6.5, 0.45, -1.0), Vector3(1.6, 0.9, 1.4), Color(0.75, 0.78, 0.85), "noodle_blancher.png")
	_add_station_icon(Vector3(-6.5, 0.45, -1.0), "dish_bowl.png", 0.026)
	stations["noodle_blancher"] = blancher_st

	var assembly_west: AssemblyStation = AssemblyStation.new()
	assembly_west.station_id = "assembly_west"
	assembly_west.station_title = "ASSEMBLY WEST (Linh)"
	assembly_west.assigned_zone = "linh_west"
	assembly_west.position = Vector3(-3.2, 0.0, 1.4)
	add_child(assembly_west)
	_create_station_structure("AssemblyWestMesh", Vector3(-3.2, 0.45, 1.4), Vector3(2.2, 0.9, 1.3), Color(0.38, 0.58, 0.78), "serving_station.png")
	_add_station_icon(Vector3(-3.2, 0.45, 1.4), "dish_bowl.png", 0.026)
	stations["assembly_west"] = assembly_west

	var serve_st: ServingStation = ServingStation.new()
	serve_st.station_id = "serving_station"
	serve_st.station_title = "SERVING COUNTER"
	serve_st.assigned_zone = "shared"
	serve_st.position = Vector3(-3.2, 0.0, 4.0)
	add_child(serve_st)
	_create_station_structure("ServingMesh", Vector3(-3.2, 0.45, 4.0), Vector3(2.8, 0.9, 1.2), Color(0.95, 0.75, 0.22), "serving_station.png")
	_add_station_icon(Vector3(-3.2, 0.45, 4.0), "baguette_bread.png", 0.026)
	stations["serving_station"] = serve_st

	# West Crates (Linh's Raw Ingredients: Beef, Noodles, Pork, Bulgogi, Tteok, Baguette)
	_add_crate("crate_pork", "CRATE: Raw Pork Belly\n(Prep -> Pass to Jiwon)", "pork_raw", Vector3(-7.6, 0.0, 1.2), Color(0.88, 0.48, 0.45))
	_add_crate("crate_bulgogi", "CRATE: Raw Bulgogi\n(Prep -> Pass to Jiwon)", "bulgogi_raw", Vector3(-7.6, 0.0, 2.6), Color(0.68, 0.22, 0.20))
	_add_crate("crate_tteok", "CRATE: Rice Cakes\n(Prep -> Pass to Jiwon)", "tteok_raw", Vector3(-7.6, 0.0, 4.0), Color(0.95, 0.93, 0.90))
	_add_crate("crate_beef", "CRATE: Raw Beef", "beef_raw", Vector3(-5.4, 0.0, -4.4), Color(0.78, 0.25, 0.25))
	_add_crate("crate_noodles", "CRATE: Dry Rice Noodles", "noodles_raw", Vector3(-3.4, 0.0, -4.4), Color(0.95, 0.92, 0.80))
	_add_crate("crate_baguette", "CRATE: Baguettes", "baguette", Vector3(-1.4, 0.0, -4.4), Color(0.86, 0.66, 0.36))

	# --- EAST ZONE (JIWON'S SIDE): Preps Pickles/Kimchi/Eomuk for Linh + Grills Pork/Bulgogi + Simmers Wok + Washing ---
	var prep_east: PrepStation = PrepStation.new()
	prep_east.station_id = "prep_east"
	prep_east.station_title = "JIWON PREP (East)\n[Pickles/Kimchi/Fish Cake -> Pass to Linh]"
	prep_east.assigned_zone = "jiwon_east"
	prep_east.position = Vector3(3.2, 0.0, -2.6)
	add_child(prep_east)
	_create_station_structure("PrepEastMesh", Vector3(3.2, 0.45, -2.6), Vector3(2.2, 0.9, 1.3), Color(0.88, 0.45, 0.25), "prep_east.png")
	_add_station_icon(Vector3(3.2, 0.45, -2.6), "prep_counter_icon.png", 0.026)
	stations["prep_east"] = prep_east

	# East Holding Counter
	var counter_east: CounterStation = CounterStation.new()
	counter_east.station_id = "counter_east"
	counter_east.station_title = "HOLDING TABLE (East)"
	counter_east.assigned_zone = "jiwon_east"
	counter_east.position = Vector3(3.2, 0.0, -0.6)
	add_child(counter_east)
	_create_station_structure("CounterEastMesh", Vector3(3.2, 0.45, -0.6), Vector3(2.0, 0.9, 1.1), Color(0.65, 0.70, 0.74), "serving_station.png")
	_add_station_icon(Vector3(3.2, 0.45, -0.6), "dish_plate.png", 0.024)
	stations["counter_east"] = counter_east

	var grill_st: CookingStation = CookingStation.new()
	grill_st.station_id = "grill_pan"
	grill_st.appliance_id = "grill_pan"
	grill_st.station_title = "Grill & Sear Pan (Pork / Bulgogi)"
	grill_st.assigned_zone = "jiwon_east"
	grill_st.position = Vector3(6.5, 0.0, -3.0)
	add_child(grill_st)
	_create_station_structure("GrillMesh", Vector3(6.5, 0.45, -3.0), Vector3(1.6, 0.9, 1.4), Color(0.85, 0.35, 0.22), "grill_pan.png")
	_add_station_icon(Vector3(6.5, 0.45, -3.0), "cooking_pan_icon.png", 0.026)
	stations["grill_pan"] = grill_st

	var wok_st: CookingStation = CookingStation.new()
	wok_st.station_id = "simmer_wok"
	wok_st.appliance_id = "simmer_wok"
	wok_st.station_title = "Gochujang Simmer Wok (Tteokbokki)"
	wok_st.assigned_zone = "jiwon_east"
	wok_st.position = Vector3(6.5, 0.0, -0.8)
	add_child(wok_st)
	_create_station_structure("WokMesh", Vector3(6.5, 0.45, -0.8), Vector3(1.6, 0.9, 1.4), Color(0.78, 0.18, 0.16), "simmer_wok.png")
	_add_station_icon(Vector3(6.5, 0.45, -0.8), "cooking_pan_icon.png", 0.026)
	stations["simmer_wok"] = wok_st

	var assembly_east: AssemblyStation = AssemblyStation.new()
	assembly_east.station_id = "assembly_east"
	assembly_east.station_title = "ASSEMBLY EAST (Jiwon)"
	assembly_east.assigned_zone = "jiwon_east"
	assembly_east.position = Vector3(3.2, 0.0, 1.4)
	add_child(assembly_east)
	_create_station_structure("AssemblyEastMesh", Vector3(3.2, 0.45, 1.4), Vector3(2.2, 0.9, 1.3), Color(0.38, 0.58, 0.78), "prep_east.png")
	_add_station_icon(Vector3(3.2, 0.45, 1.4), "dish_plate.png", 0.026)
	stations["assembly_east"] = assembly_east

	var wash_st: WashingStation = WashingStation.new()
	wash_st.station_id = "washing_station"
	wash_st.station_title = "WASHING SINK & CLEAN DISHES"
	wash_st.assigned_zone = "jiwon_east"
	wash_st.position = Vector3(3.2, 0.0, 4.0)
	add_child(wash_st)
	_create_station_structure("WashMesh", Vector3(3.2, 0.45, 4.0), Vector3(2.8, 0.9, 1.2), Color(0.32, 0.75, 0.92), "washing_sink.png")
	_add_station_icon(Vector3(3.2, 0.45, 4.0), "dish_soap.png", 0.026)
	stations["washing_station"] = wash_st

	# East Crates (Jiwon's Raw Ingredients: Daikon/Carrot, Kimchi, Fish Cakes, Gochujang, plus Bowl/Plate Dispenser)
	_add_crate("crate_daikon", "CRATE: Daikon & Carrot\n(Prep -> Pass to Linh)", "daikon_carrot_raw", Vector3(1.6, 0.0, -4.4), Color(0.95, 0.62, 0.24))
	_add_crate("crate_kimchi", "CRATE: Aged Kimchi\n(Prep -> Pass to Linh)", "kimchi_raw", Vector3(3.8, 0.0, -4.4), Color(0.88, 0.26, 0.18))
	_add_crate("crate_eomuk", "CRATE: Fish Cakes", "eomuk_raw", Vector3(6.0, 0.0, -4.4), Color(0.88, 0.76, 0.54))
	_add_crate("crate_gochujang", "CRATE: Gochujang Sauce", "gochujang_sauce", Vector3(7.6, 0.0, 1.4), Color(0.76, 0.12, 0.10))

func _add_crate(st_id: String, title: String, item_id: String, pos: Vector3, col: Color) -> void:
	var st: StationBase = StationBase.new()
	st.station_id = st_id
	st.station_title = title
	st.position = pos
	add_child(st)
	_create_station_structure("%s_Mesh" % st_id, pos + Vector3(0.0, 0.4, 0.0), Vector3(1.4, 0.8, 1.1), col, "crate_cooler.png")
	stations[st_id] = st
	crate_items[st_id] = item_id

	var icon_name: String = "dish_plate.png"
	match item_id:
		"pork_raw": icon_name = "pork_belly_raw.png"
		"bulgogi_raw", "beef_raw": icon_name = "beef_bulgogi_raw.png"
		"tteok_raw": icon_name = "tteok_portioned.png"
		"noodles_raw": icon_name = "dish_bowl.png"
		"baguette": icon_name = "baguette_bread.png"
		"daikon_carrot_raw": icon_name = "pickled_veg.png"
		"kimchi_raw": icon_name = "kimchi_slaw.png"
		"eomuk_raw": icon_name = "fish_cake_raw.png"
		"gochujang_sauce": icon_name = "korean_sauce.png"
	_add_station_icon(pos, icon_name, 0.022)

func _spawn_players() -> void:
	for p_node: Node in players_by_peer.values():
		p_node.queue_free()
	players_by_peer.clear()

	if multiplayer.multiplayer_peer != null and NetworkManager.is_lan_active:
		local_peer_id = multiplayer.get_unique_id()
	else:
		local_peer_id = 1

	var p_dict: Dictionary = NetworkManager.players
	if p_dict.is_empty():
		p_dict = {
			1: {"peer_id": 1, "name": "Player 1", "character": "linh", "specialization": "prep_master"},
			2: {"peer_id": 2, "name": "Player 2", "character": "jiwon", "specialization": "flame_keeper"}
		}

	for pid: int in p_dict.keys():
		var info: Dictionary = p_dict[pid]
		var pc: PlayerController = PlayerController.new()
		pc.name = "Player_%d" % pid
		var char_id: String = str(info.get("character", "linh"))
		var spec_id: String = str(info.get("specialization", "prep_master"))
		var tier: int = ProgressionManager.linh_tier if char_id == "linh" else ProgressionManager.jiwon_tier
		pc.configure_chef(pid, char_id, spec_id, tier)
		pc.position = Vector3(-2.0, 0.0, 0.0) if char_id == "linh" else Vector3(2.0, 0.0, 0.0)
		add_child(pc)
		players_by_peer[pid] = pc

func _physics_process(delta: float) -> void:
	if not ShiftManager.shift_active:
		return

	# Tick worker cooldowns
	for rid: String in worker_cooldowns.keys():
		if worker_cooldowns[rid] > 0.0:
			worker_cooldowns[rid] = maxf(0.0, worker_cooldowns[rid] - delta)

	# Tick cooking stations on authoritative host
	if NetworkManager.is_host():
		for st_id: String in ["stockpot", "noodle_blancher", "grill_pan", "simmer_wok"]:
			if stations.has(st_id):
				var cs: CookingStation = stations[st_id] as CookingStation
				cs.tick_cooking(delta)

	# Process local player movement input
	var move_vec: Vector2 = Input.get_vector("move_left", "move_right", "move_up", "move_down")
	if virtual_joystick_vec.length() > 0.05:
		move_vec = virtual_joystick_vec

	if players_by_peer.has(local_peer_id):
		var local_player: PlayerController = players_by_peer[local_peer_id]
		local_player.apply_movement(move_vec, delta)
		if NetworkManager.is_lan_active and multiplayer.multiplayer_peer != null:
			if NetworkManager.is_host():
				_sync_player_transform.rpc(local_peer_id, local_player.position)
			else:
				_send_client_transform.rpc_id(1, local_player.position)

		var nearest: StationBase = find_nearest_station(local_player.position)
		if nearest != null:
			local_prompt_changed.emit("Near: %s  [Press E / Interact | F / Strike-Bowl]" % nearest.station_id.to_upper())
		else:
			local_prompt_changed.emit("Move near a station to interact (West: Linh Prep/Cook | Center: Pass Counter | East: Jiwon Prep/Cook/Wash)")

	if Input.is_action_just_pressed("interact"):
		trigger_primary_interact()
	if Input.is_action_just_pressed("action_strike"):
		trigger_secondary_action()

func find_nearest_station(pos: Vector3) -> StationBase:
	var best: StationBase = null
	var best_dist: float = StationBase.MAX_INTERACT_DISTANCE
	for st: StationBase in stations.values():
		var d: float = Vector2(pos.x, pos.z).distance_to(Vector2(st.position.x, st.position.z))
		if d <= best_dist:
			best_dist = d
			best = st
	return best

func trigger_primary_interact() -> void:
	if not players_by_peer.has(local_peer_id):
		return
	var p: PlayerController = players_by_peer[local_peer_id]
	var st: StationBase = find_nearest_station(p.position)
	if st == null:
		return
	if NetworkManager.is_host():
		_execute_station_interaction(local_peer_id, st.station_id, false)
	else:
		_request_station_interaction.rpc_id(1, st.station_id, false)

func trigger_secondary_action() -> void:
	if not players_by_peer.has(local_peer_id):
		return
	var p: PlayerController = players_by_peer[local_peer_id]
	var st: StationBase = find_nearest_station(p.position)
	if st == null:
		return
	if NetworkManager.is_host():
		_execute_station_interaction(local_peer_id, st.station_id, true)
	else:
		_request_station_interaction.rpc_id(1, st.station_id, true)

@rpc("any_peer", "reliable")
func _request_station_interaction(station_id: String, is_secondary: bool) -> void:
	if not NetworkManager.is_host():
		return
	var sender_id: int = multiplayer.get_remote_sender_id()
	_execute_station_interaction(sender_id, station_id, is_secondary)

func _execute_station_interaction(peer_id: int, station_id: String, is_secondary: bool) -> void:
	if not players_by_peer.has(peer_id) or not stations.has(station_id):
		return
	var player: PlayerController = players_by_peer[peer_id]
	var st: StationBase = stations[station_id]

	# Host authoritative distance check
	if not st.is_within_range(player.position):
		_emit_feedback_to_peer(peer_id, "Too far from %s!" % st.station_title, Color(0.95, 0.4, 0.4))
		return

	# 1. Crate interaction
	if crate_items.has(station_id):
		if not player.carried_item.is_empty():
			_emit_feedback_to_peer(peer_id, "Hands full! Place or pass your current item first.", Color(0.95, 0.6, 0.3))
			return
		var raw_id: String = crate_items[station_id]
		var spec: RecipeData.IngredientSpec = GameData.ingredients[raw_id]
		player.set_carried_item({
			"uid": "raw_%d" % Time.get_ticks_usec(),
			"item_id": raw_id,
			"display_name": spec.display_name,
			"quality_tier": RecipeData.QualityTier.STANDARD,
		})
		_broadcast_player_item(peer_id)
		return

	# 2. Trash bin
	if station_id == "trash_bin":
		if player.carried_item.is_empty():
			return
		var discarded: Dictionary = player.clear_carried_item()
		ShiftManager.record_waste("Discarded %s (-10 Flow)" % discarded.get("display_name", "item"))
		_broadcast_player_item(peer_id)
		return

	# 3. Prep Station
	if st is PrepStation:
		var prep: PrepStation = st as PrepStation
		var speed_mult: float = player.get_task_speed_multiplier()
		if is_secondary or (player.carried_item.is_empty() and not prep.current_item.is_empty() and GameData.get_prep_output(str(prep.current_item.get("item_id", ""))) != ""):
			var now_sec: float = float(Time.get_ticks_msec()) / 1000.0
			var res: Dictionary = prep.perform_strike(now_sec, player.specialization_id, player.character_id, speed_mult)
			if not res.get("ok", false):
				_emit_feedback_to_peer(peer_id, str(res.get("reason", "")), Color(0.95, 0.4, 0.4))
		elif not player.carried_item.is_empty() and prep.current_item.is_empty():
			var res: Dictionary = prep.place_raw_item(player.carried_item, peer_id)
			if res.get("ok", false):
				player.clear_carried_item()
				_broadcast_player_item(peer_id)
			else:
				_emit_feedback_to_peer(peer_id, str(res.get("reason", "")), Color(0.95, 0.4, 0.4))
		elif player.carried_item.is_empty() and not prep.current_item.is_empty():
			var taken: Dictionary = prep.take_item()
			if not taken.is_empty():
				player.set_carried_item(taken)
				_broadcast_player_item(peer_id)
		_broadcast_station_state(station_id, prep.get_state())
		return

	# 4. Cooking Station
	if st is CookingStation:
		var cook: CookingStation = st as CookingStation
		if not player.carried_item.is_empty():
			var res: Dictionary = cook.add_ingredient(player.carried_item, player.specialization_id)
			if res.get("ok", false):
				player.clear_carried_item()
				_broadcast_player_item(peer_id)
			else:
				_emit_feedback_to_peer(peer_id, str(res.get("reason", "")), Color(0.95, 0.4, 0.4))
		else:
			var res: Dictionary = cook.retrieve_cooked_item(player.character_id)
			if res.get("ok", false):
				player.set_carried_item(res["item"])
				_broadcast_player_item(peer_id)
			else:
				_emit_feedback_to_peer(peer_id, str(res.get("reason", "")), Color(0.95, 0.6, 0.3))
		_broadcast_station_state(station_id, cook.get_state())
		return

	# 5. Pass Counter
	if st is PassCounter:
		var pass_c: PassCounter = st as PassCounter
		if not player.carried_item.is_empty():
			var res: Dictionary = pass_c.place_item(player.carried_item, peer_id, Time.get_ticks_msec())
			if res.get("ok", false):
				player.clear_carried_item()
				_broadcast_player_item(peer_id)
			else:
				_emit_feedback_to_peer(peer_id, str(res.get("reason", "")), Color(0.95, 0.4, 0.4))
		else:
			var res: Dictionary = pass_c.retrieve_item(peer_id, Time.get_ticks_msec())
			if res.get("ok", false):
				player.set_carried_item(res["item"])
				_broadcast_player_item(peer_id)
			else:
				_emit_feedback_to_peer(peer_id, str(res.get("reason", "")), Color(0.95, 0.4, 0.4))
		_broadcast_station_state(station_id, pass_c.get_state())
		return

	# 6. Holding Counter Station (West / East empty holding tables)
	if st is CounterStation:
		var cnt: CounterStation = st as CounterStation
		if not player.carried_item.is_empty() and cnt.held_item.is_empty():
			var placed: bool = cnt.place_item(player.carried_item)
			if placed:
				player.clear_carried_item()
				_broadcast_player_item(peer_id)
		elif player.carried_item.is_empty() and not cnt.held_item.is_empty():
			var item: Dictionary = cnt.retrieve_item()
			if not item.is_empty():
				player.set_carried_item(item)
				_broadcast_player_item(peer_id)
		elif not player.carried_item.is_empty() and not cnt.held_item.is_empty():
			var swapped: Dictionary = cnt.swap_item(player.carried_item)
			player.set_carried_item(swapped)
			_broadcast_player_item(peer_id)
		_broadcast_station_state(station_id, {"held_item": cnt.held_item})
		return

	# 7. Assembly Station
	if st is AssemblyStation:
		var asm: AssemblyStation = st as AssemblyStation
		if not player.carried_item.is_empty():
			var res: Dictionary = asm.add_ingredient(player.carried_item, player.specialization_id)
			if res.get("ok", false):
				player.clear_carried_item()
				_broadcast_player_item(peer_id)
			else:
				_emit_feedback_to_peer(peer_id, str(res.get("reason", "")), Color(0.95, 0.4, 0.4))
		else:
			var taken: Dictionary = asm.take_item()
			if not taken.is_empty():
				player.set_carried_item(taken)
				_broadcast_player_item(peer_id)
		_broadcast_station_state(station_id, asm.get_state())
		return

	# 7. Serving Station
	if st is ServingStation:
		var srv: ServingStation = st as ServingStation
		if not player.carried_item.is_empty():
			var res: Dictionary = srv.submit_dish(player.carried_item, peer_id)
			if res.get("ok", false):
				player.clear_carried_item()
				_broadcast_player_item(peer_id)
			else:
				_emit_feedback_to_peer(peer_id, str(res.get("reason", "")), Color(0.95, 0.4, 0.4))
		return

	# 8. Washing Station & Clean Vessel Pickup
	if st is WashingStation:
		var wash: WashingStation = st as WashingStation
		if is_secondary:
			# Secondary action at Washing Station: scrub dirty plate OR pick up a Clean Bowl
			if wash.dirty_plates_in_sink > 0 or ShiftManager.dirty_plates_at_return > 0:
				wash.perform_scrub_stroke()
			elif player.carried_item.is_empty():
				var res: Dictionary = wash.take_clean_vessel("bowl")
				if res.get("ok", false):
					player.set_carried_item(res["item"])
					_broadcast_player_item(peer_id)
				else:
					_emit_feedback_to_peer(peer_id, str(res.get("reason", "")), Color(0.95, 0.4, 0.4))
		else:
			# Primary action: pick up a Clean Plate (or scrub if no clean plates left)
			if player.carried_item.is_empty():
				if ShiftManager.clean_plates > 0:
					var res: Dictionary = wash.take_clean_vessel("plate")
					if res.get("ok", false):
						player.set_carried_item(res["item"])
						_broadcast_player_item(peer_id)
				else:
					var res: Dictionary = wash.perform_scrub_stroke()
					if not res.get("ok", false):
						_emit_feedback_to_peer(peer_id, str(res.get("reason", "")), Color(0.95, 0.4, 0.4))
		_broadcast_station_state(station_id, wash.get_state())
		return

func _emit_feedback_to_peer(peer_id: int, msg: String, col: Color) -> void:
	if peer_id == 1 or not NetworkManager.is_lan_active:
		status_toast.emit(msg, col)
	elif multiplayer.multiplayer_peer != null:
		_rpc_client_toast.rpc_id(peer_id, msg, col)

@rpc("authority", "call_local", "reliable")
func _rpc_client_toast(msg: String, col: Color) -> void:
	status_toast.emit(msg, col)

func _broadcast_player_item(peer_id: int) -> void:
	if not players_by_peer.has(peer_id):
		return
	var item: Dictionary = players_by_peer[peer_id].carried_item
	if NetworkManager.is_lan_active and multiplayer.multiplayer_peer != null and NetworkManager.is_host():
		_sync_player_item.rpc(peer_id, item)

@rpc("authority", "call_local", "reliable")
func _sync_player_item(peer_id: int, item_data: Dictionary) -> void:
	if players_by_peer.has(peer_id):
		players_by_peer[peer_id].set_carried_item(item_data)

func _broadcast_station_state(station_id: String, state: Dictionary) -> void:
	if NetworkManager.is_lan_active and multiplayer.multiplayer_peer != null and NetworkManager.is_host():
		_sync_station_state.rpc(station_id, state)

@rpc("authority", "call_local", "reliable")
func _sync_station_state(station_id: String, state: Dictionary) -> void:
	if not stations.has(station_id):
		return
	var st: StationBase = stations[station_id]
	if st.has_method("apply_state"):
		st.call("apply_state", state)

@rpc("any_peer", "unreliable_ordered")
func _send_client_transform(client_pos: Vector3) -> void:
	if not NetworkManager.is_host():
		return
	var sender_id: int = multiplayer.get_remote_sender_id()
	if players_by_peer.has(sender_id):
		var p: PlayerController = players_by_peer[sender_id]
		p.position = Vector3(
			clampf(client_pos.x, -PlayerController.KITCHEN_BOUNDS_X, PlayerController.KITCHEN_BOUNDS_X),
			0.0,
			clampf(client_pos.z, -PlayerController.KITCHEN_BOUNDS_Z, PlayerController.KITCHEN_BOUNDS_Z)
		)
		_sync_player_transform.rpc(sender_id, p.position)

@rpc("authority", "unreliable_ordered")
func _sync_player_transform(peer_id: int, synced_pos: Vector3) -> void:
	if peer_id == local_peer_id:
		return
	if players_by_peer.has(peer_id):
		players_by_peer[peer_id].position = synced_pos

## Jiwon's Signature Ability: "CALL IN THE CREW"
## Authoritative host verifies hiring capacity, coin balance, and cooldown.
func request_hire_worker(peer_id: int, role_id: String) -> Dictionary:
	if not NetworkManager.is_host():
		_rpc_request_hire_worker.rpc_id(1, role_id)
		return {"ok": true, "pending_host_approval": true}

	if not players_by_peer.has(peer_id):
		return {"ok": false, "reason": "Unknown chef."}

	var p: PlayerController = players_by_peer[peer_id]
	if p.character_id != "jiwon":
		return {"ok": false, "reason": "Only Jiwon has the delegation authority to hire part-time crew!"}

	var tier_slots: int = 0
	if GameData.jiwon_tiers.has(p.character_tier):
		tier_slots = GameData.jiwon_tiers[p.character_tier].part_timer_slots

	if tier_slots <= 0:
		return {
			"ok": false,
			"reason": "Jiwon is Tier 1 (Burnt-out Executive). Complete Level 3 to unlock Part-Timer hiring!"
		}

	if active_workers.size() >= tier_slots:
		return {
			"ok": false,
			"reason": "Maximum part-timer capacity reached (%d/%d slots active)!" % [active_workers.size(), tier_slots]
		}

	if not GameData.worker_catalog.has(role_id):
		return {"ok": false, "reason": "Unknown worker role: %s" % role_id}

	var spec: CharacterProgressionData.WorkerSpec = GameData.worker_catalog[role_id]
	if worker_cooldowns.get(role_id, 0.0) > 0.0:
		return {
			"ok": false,
			"reason": "%s contract agency is on cooldown (%.0fs left)!" % [spec.display_name, worker_cooldowns[role_id]]
		}

	if ProgressionManager.coins < spec.hire_cost:
		return {
			"ok": false,
			"reason": "Not enough restaurant coins! Need %d coins (Have %d)." % [spec.hire_cost, ProgressionManager.coins]
		}

	# Determine station assignment
	var target_st: StationBase = null
	if role_id == "prep_assistant":
		target_st = stations.get("prep_east", stations.get("prep_west", null))
	elif role_id == "dishwasher":
		target_st = stations.get("washing_station", null)

	if target_st == null:
		return {"ok": false, "reason": "No station available for %s." % spec.display_name}

	# Deduct coins and start cooldown
	ProgressionManager.coins -= spec.hire_cost
	worker_cooldowns[role_id] = spec.cooldown_duration
	ProgressionManager.save_progression()
	ProgressionManager.progression_updated.emit()

	_worker_uid_counter += 1
	var wid: String = "worker_%d" % _worker_uid_counter
	_spawn_worker_local(wid, role_id, target_st.station_id)

	if NetworkManager.is_lan_active and multiplayer.multiplayer_peer != null:
		_rpc_sync_spawn_worker.rpc(wid, role_id, target_st.station_id)

	status_toast.emit("Hired %s for %.0fs! (-%d coins)" % [spec.display_name, spec.contract_duration, spec.hire_cost], Color(0.35, 1.0, 0.6))
	return {"ok": true, "worker_id": wid}

func _spawn_worker_local(worker_id_str: String, role_id: String, station_id_str: String) -> void:
	if not stations.has(station_id_str):
		return
	var st: StationBase = stations[station_id_str]
	var w: PartTimeWorker = PartTimeWorker.new()
	w.name = worker_id_str
	w.initialize_worker(worker_id_str, role_id, st)
	w.worker_expired.connect(func(expired_id: String) -> void:
		active_workers.erase(expired_id)
	)
	add_child(w)
	active_workers[worker_id_str] = w

@rpc("any_peer", "reliable")
func _rpc_request_hire_worker(role_id: String) -> void:
	if not NetworkManager.is_host():
		return
	var sender_id: int = multiplayer.get_remote_sender_id()
	var res: Dictionary = request_hire_worker(sender_id, role_id)
	if not res.get("ok", false):
		_emit_feedback_to_peer(sender_id, res.get("reason", "Hiring rejected."), Color(1.0, 0.4, 0.4))

@rpc("authority", "reliable")
func _rpc_sync_spawn_worker(worker_id_str: String, role_id: String, station_id_str: String) -> void:
	if NetworkManager.is_host():
		return
	_spawn_worker_local(worker_id_str, role_id, station_id_str)
