extends Node
## Root Coordinator for Two Ladles.
## Manages transitions between the LAN Multiplayer Lobby, the 2.5D Kitchen Shift, HUD (Touch + Desktop), and End-of-Shift Summary.

const CampaignLevelSpec = preload("res://scripts/data/campaign_level_spec.gd")
const RecipeData = preload("res://scripts/data/recipe_data.gd")

var lobby_panel: PanelContainer = null
var hud_layer: CanvasLayer = null
var kitchen_instance: KitchenController = null

# Lobby UI controls
var ip_input: LineEdit = null
var port_input: LineEdit = null
var name_input: LineEdit = null
var status_label: Label = null
var players_list_label: Label = null
var char_select_btn: OptionButton = null
var spec_select_btn: OptionButton = null
var ready_btn: Button = null
var start_shift_btn: Button = null
var stage_select_btn: OptionButton = null
var progression_header_lbl: Label = null
var shop_container: HBoxContainer = null
var endless_mode_toggle: CheckButton = null
var is_endless_selected: bool = false
var jiwon_crew_bar: HBoxContainer = null
var hire_prep_btn: Button = null
var hire_dish_btn: Button = null

# HUD controls
var hud_score_label: Label = null
var hud_timer_label: Label = null
var hud_flow_bar: ProgressBar = null
var hud_flow_label: Label = null
var hud_plates_label: Label = null
var hud_orders_label: Label = null
var hud_prompt_label: Label = null
var hud_toast_label: Label = null
var end_shift_modal: PanelContainer = null
var end_shift_details: Label = null

func _ready() -> void:
	_build_ui()
	NetworkManager.lobby_updated.connect(_on_lobby_updated)
	NetworkManager.connection_status_changed.connect(_on_connection_status_changed)
	NetworkManager.shift_start_requested.connect(_on_shift_start_requested)
	NetworkManager.returned_to_menu.connect(_on_returned_to_menu)

	ShiftManager.shift_state_updated.connect(_on_shift_state_updated)
	ShiftManager.order_queue_updated.connect(_on_order_queue_updated)
	ShiftManager.flow_updated.connect(_on_flow_updated)
	ShiftManager.feedback_banner.connect(_on_feedback_banner)
	ShiftManager.shift_ended.connect(_on_shift_ended)
	ProgressionManager.progression_updated.connect(_on_progression_updated)

	# Support command-line auto-host / auto-join / test-suite execution
	var args: PackedStringArray = OS.get_cmdline_user_args()
	for arg: String in args:
		if arg == "--run-tests":
			var runner_script: Script = load("res://tests/test_suite_node.gd")
			var runner: Node = runner_script.new()
			add_child(runner)
			return
		elif arg == "--lan-smoke-host":
			_run_lan_smoke_host()
			return
		elif arg == "--lan-smoke-client":
			_run_lan_smoke_client()
			return
		elif arg == "--screenshot-kitchen":
			_take_kitchen_screenshot()
			return
		elif arg == "--auto-host":
			NetworkManager.host_game(NetworkManager.DEFAULT_PORT, "HostChef")
		elif arg.begins_with("--auto-join="):
			var target_ip: String = arg.trim_prefix("--auto-join=")
			NetworkManager.join_game(target_ip, NetworkManager.DEFAULT_PORT, "ClientChef")

func _run_lan_smoke_host() -> void:
	print("[LAN-SMOKE-HOST] Starting ENet Host on UDP port %d..." % NetworkManager.DEFAULT_PORT)
	var err: Error = NetworkManager.host_game(NetworkManager.DEFAULT_PORT, "HostLinh")
	if err != OK:
		printerr("[LAN-SMOKE-HOST] Failed to bind UDP port!")
		get_tree().quit(1)
		return
	NetworkManager.set_player_ready(1, true)
	var timeout: float = 8.0
	while timeout > 0.0:
		await get_tree().create_timer(0.2).timeout
		timeout -= 0.2
		if NetworkManager.players.size() == 2 and NetworkManager.can_start_shift(true).get("ok", false):
			print("[LAN-SMOKE-HOST] Partner connected & Ready! Starting authoritative 2-player shift...")
			NetworkManager.request_start_shift(true)
			await get_tree().create_timer(0.2).timeout
			print("[LAN-SMOKE-HOST] Shift active=%s | Active Orders=%d | Players Spawned=2 -> PASS" % [
				str(ShiftManager.shift_active),
				ShiftManager.active_orders.size()
			])
			await get_tree().create_timer(0.5).timeout
			get_tree().quit(0)
			return
	printerr("[LAN-SMOKE-HOST] Timed out waiting for client handshake.")
	get_tree().quit(1)

func _run_lan_smoke_client() -> void:
	await get_tree().create_timer(0.4).timeout
	print("[LAN-SMOKE-CLIENT] Joining 127.0.0.1:%d..." % NetworkManager.DEFAULT_PORT)
	var err: Error = NetworkManager.join_game("127.0.0.1", NetworkManager.DEFAULT_PORT, "ClientJiwon")
	if err != OK:
		printerr("[LAN-SMOKE-CLIENT] Failed to create ENet client!")
		get_tree().quit(1)
		return
	var timeout: float = 8.0
	var sent_ready: bool = false
	while timeout > 0.0:
		await get_tree().create_timer(0.2).timeout
		timeout -= 0.2
		if multiplayer.multiplayer_peer != null and multiplayer.get_unique_id() != 1 and not sent_ready:
			var my_id: int = multiplayer.get_unique_id()
			if NetworkManager.players.has(my_id):
				sent_ready = true
				print("[LAN-SMOKE-CLIENT] Lobby synced (My Peer=%d, Char=%s). Sending Ready RPC..." % [
					my_id,
					str(NetworkManager.players[my_id].get("character", ""))
				])
				NetworkManager.request_ready_toggle.rpc_id(1, true)
		if ShiftManager.shift_active and ShiftManager.active_orders.size() >= 2:
			print("[LAN-SMOKE-CLIENT] Received authoritative Shift Start & %d Orders from Host -> PASS" % ShiftManager.active_orders.size())
			get_tree().quit(0)
			return
	printerr("[LAN-SMOKE-CLIENT] Timed out waiting for shift start sync.")
	get_tree().quit(1)

func _take_kitchen_screenshot() -> void:
	print("[SCREENSHOT] Initializing kitchen shift for offscreen frame capture...")
	_on_shift_start_requested()
	await get_tree().create_timer(0.4).timeout
	var vp: Viewport = get_viewport()
	var tex: ViewportTexture = vp.get_texture()
	var img: Image = tex.get_image()
	DirAccess.make_dir_recursive_absolute("build/preview")
	var err: Error = img.save_png("build/preview/kitchen_view.png")
	if err == OK:
		print("[SCREENSHOT_SAVED] build/preview/kitchen_view.png (Size: %dx%d)" % [img.get_width(), img.get_height()])
	else:
		printerr("[SCREENSHOT_ERROR] Failed to save png: %d" % err)
	get_tree().quit(0 if err == OK else 1)

func _build_ui() -> void:
	hud_layer = CanvasLayer.new()
	hud_layer.name = "MainUILayer"
	add_child(hud_layer)

	# --- 1. LOBBY PANEL ---
	lobby_panel = PanelContainer.new()
	lobby_panel.set_anchors_preset(Control.PRESET_FULL_RECT)
	hud_layer.add_child(lobby_panel)

	var margin: MarginContainer = MarginContainer.new()
	margin.add_theme_constant_override("margin_left", 48)
	margin.add_theme_constant_override("margin_right", 48)
	margin.add_theme_constant_override("margin_top", 28)
	margin.add_theme_constant_override("margin_bottom", 28)
	lobby_panel.add_child(margin)

	var vbox: VBoxContainer = VBoxContainer.new()
	vbox.add_theme_constant_override("separation", 12)
	margin.add_child(vbox)

	var title: Label = Label.new()
	title.text = "TWO LADLES — A Vietnam–Korea Kitchen Story (LAN Co-Op)"
	title.add_theme_font_size_override("font_size", 28)
	title.modulate = Color(1.0, 0.86, 0.45)
	vbox.add_child(title)

	var subtitle: Label = Label.new()
	subtitle.text = "Cross-Kitchen Interdependence: Linh (West) preps Jiwon's ingredients | Jiwon (East) preps Linh's ingredients across the Pass Counter!"
	subtitle.modulate = Color(0.8, 0.92, 0.88)
	vbox.add_child(subtitle)

	# Connection Row
	var conn_row: HBoxContainer = HBoxContainer.new()
	conn_row.add_theme_constant_override("separation", 10)
	vbox.add_child(conn_row)

	name_input = LineEdit.new()
	name_input.placeholder_text = "Chef Name"
	name_input.text = "Chef"
	name_input.custom_minimum_size = Vector2(150, 40)
	conn_row.add_child(name_input)

	ip_input = LineEdit.new()
	ip_input.placeholder_text = "Host IPv4 (e.g. 127.0.0.1 or 192.168.1.x)"
	ip_input.text = "127.0.0.1"
	ip_input.custom_minimum_size = Vector2(240, 40)
	conn_row.add_child(ip_input)

	port_input = LineEdit.new()
	port_input.text = str(NetworkManager.DEFAULT_PORT)
	port_input.custom_minimum_size = Vector2(95, 40)
	conn_row.add_child(port_input)

	var host_btn: Button = Button.new()
	host_btn.text = "HOST GAME (UDP)"
	host_btn.custom_minimum_size = Vector2(160, 42)
	host_btn.pressed.connect(func() -> void:
		NetworkManager.host_game(int(port_input.text), name_input.text)
	)
	conn_row.add_child(host_btn)

	var join_btn: Button = Button.new()
	join_btn.text = "JOIN GAME"
	join_btn.custom_minimum_size = Vector2(140, 42)
	join_btn.pressed.connect(func() -> void:
		NetworkManager.join_game(ip_input.text, int(port_input.text), name_input.text)
	)
	conn_row.add_child(join_btn)

	var leave_btn: Button = Button.new()
	leave_btn.text = "DISCONNECT / RESET"
	leave_btn.pressed.connect(func() -> void:
		NetworkManager.close_connection()
		_on_connection_status_changed("Disconnected.", false)
	)
	conn_row.add_child(leave_btn)

	status_label = Label.new()
	status_label.text = "Status: Not connected. Host a game or enter Host IPv4 to join over LAN Wi-Fi."
	status_label.modulate = Color(0.85, 0.9, 1.0)
	vbox.add_child(status_label)

	# Character & Specialization Selection Row
	var loadout_row: HBoxContainer = HBoxContainer.new()
	loadout_row.add_theme_constant_override("separation", 14)
	vbox.add_child(loadout_row)

	var char_lbl: Label = Label.new()
	char_lbl.text = "Character (Exclusive):"
	loadout_row.add_child(char_lbl)

	char_select_btn = OptionButton.new()
	char_select_btn.add_item("Linh (Vietnamese Programmer - West Home)", 0)
	char_select_btn.set_item_metadata(0, "linh")
	char_select_btn.add_item("Jiwon (Korean Executive - East Home)", 1)
	char_select_btn.set_item_metadata(1, "jiwon")
	char_select_btn.item_selected.connect(func(_idx: int) -> void: _submit_lobby_loadout())
	loadout_row.add_child(char_select_btn)

	var spec_lbl: Label = Label.new()
	spec_lbl.text = "Specialization:"
	loadout_row.add_child(spec_lbl)

	spec_select_btn = OptionButton.new()
	var s_idx: int = 0
	for s_id: String in ["prep_master", "flame_keeper", "market_runner", "fusion_artist"]:
		var spec_obj: RecipeData.SpecializationSpec = GameData.specializations[s_id]
		spec_select_btn.add_item(spec_obj.display_name, s_idx)
		spec_select_btn.set_item_metadata(s_idx, s_id)
		s_idx += 1
	spec_select_btn.item_selected.connect(func(_idx: int) -> void: _submit_lobby_loadout())
	loadout_row.add_child(spec_select_btn)

	ready_btn = Button.new()
	ready_btn.text = "TOGGLE READY"
	ready_btn.custom_minimum_size = Vector2(160, 42)
	ready_btn.pressed.connect(func() -> void:
		var my_id: int = multiplayer.get_unique_id() if (NetworkManager.is_lan_active and multiplayer.multiplayer_peer != null) else 1
		var cur_ready: bool = bool(NetworkManager.players.get(my_id, {}).get("ready", false))
		NetworkManager.request_ready_toggle.rpc_id(1, not cur_ready)
	)
	loadout_row.add_child(ready_btn)

	start_shift_btn = Button.new()
	start_shift_btn.text = "START SHIFT (Host - Requires 2 Ready Players)"
	start_shift_btn.custom_minimum_size = Vector2(310, 42)
	start_shift_btn.pressed.connect(func() -> void:
		NetworkManager.request_start_shift(true)
	)
	loadout_row.add_child(start_shift_btn)

	# Progression & Stage Selection Row
	var prog_row: HBoxContainer = HBoxContainer.new()
	prog_row.add_theme_constant_override("separation", 14)
	vbox.add_child(prog_row)

	progression_header_lbl = Label.new()
	prog_row.add_child(progression_header_lbl)

	var stage_lbl: Label = Label.new()
	stage_lbl.text = "Select Stage:"
	prog_row.add_child(stage_lbl)

	stage_select_btn = OptionButton.new()
	stage_select_btn.item_selected.connect(func(idx: int) -> void:
		var st_id: int = int(stage_select_btn.get_item_metadata(idx))
		ProgressionManager.selected_stage = st_id
	)
	prog_row.add_child(stage_select_btn)

	endless_mode_toggle = CheckButton.new()
	endless_mode_toggle.text = "ENDLESS RUSH MODE (Survival)"
	endless_mode_toggle.toggled.connect(func(toggled_on: bool) -> void:
		is_endless_selected = toggled_on
		stage_select_btn.disabled = toggled_on
	)
	prog_row.add_child(endless_mode_toggle)

	# Kitchen Upgrades Row
	var shop_vbox: VBoxContainer = VBoxContainer.new()
	shop_vbox.add_theme_constant_override("separation", 4)
	vbox.add_child(shop_vbox)

	var shop_title: Label = Label.new()
	shop_title.text = "KITCHEN UPGRADE WORKSHOP (Persistent Restaurant Improvements):"
	shop_title.modulate = Color(1.0, 0.85, 0.4)
	shop_vbox.add_child(shop_title)

	shop_container = HBoxContainer.new()
	shop_container.add_theme_constant_override("separation", 10)
	shop_vbox.add_child(shop_container)

	_refresh_progression_ui()

	players_list_label = Label.new()
	players_list_label.text = "Lobby Roster:\n  (Host or Join a LAN session to populate players)"
	players_list_label.modulate = Color(0.95, 0.96, 0.85)
	vbox.add_child(players_list_label)

	var recipe_guide: Label = Label.new()
	recipe_guide.text = (
		"STARTER MENU & CROSS-PREP WORKFLOW:\n" +
		"• Phở Bò (VN): Jiwon washes Bowl & preps Beef -> Pass to Linh | Linh blanches Noodles & pours Hot Stockpot Broth last.\n" +
		"• Bánh Mì Thịt (VN): Linh portions Pork -> Pass to Jiwon to Grill | Jiwon preps Pickled Daikon/Carrot -> Pass to Linh to assemble on Baguette.\n" +
		"• Tteokbokki (KR): Linh portions Rice Cakes -> Pass to Jiwon | Jiwon preps Fish Cakes & simmers both with Gochujang in Wok -> Bowl.\n" +
		"• Bulgogi Bánh Mì (VN-KR Fusion): Linh portions Bulgogi -> Pass to Jiwon to Sear | Jiwon preps Kimchi Slaw -> Pass to Linh to assemble on Baguette."
	)
	recipe_guide.modulate = Color(0.75, 0.88, 0.95)
	vbox.add_child(recipe_guide)

	# --- 2. IN-GAME HUD & TOUCH CONTROLS ---
	var top_bar: PanelContainer = PanelContainer.new()
	top_bar.name = "TopHUDBar"
	top_bar.visible = false
	top_bar.set_anchors_preset(Control.PRESET_TOP_WIDE)
	hud_layer.add_child(top_bar)

	var top_hbox: HBoxContainer = HBoxContainer.new()
	top_hbox.add_theme_constant_override("separation", 22)
	top_bar.add_child(top_hbox)

	hud_score_label = Label.new()
	hud_score_label.text = "SCORE: 0"
	hud_score_label.add_theme_font_size_override("font_size", 22)
	top_hbox.add_child(hud_score_label)

	hud_timer_label = Label.new()
	hud_timer_label.text = "TIME: 180s"
	hud_timer_label.add_theme_font_size_override("font_size", 22)
	top_hbox.add_child(hud_timer_label)

	hud_flow_label = Label.new()
	hud_flow_label.text = "KITCHEN FLOW: 0 / 100"
	hud_flow_label.add_theme_font_size_override("font_size", 20)
	top_hbox.add_child(hud_flow_label)

	hud_flow_bar = ProgressBar.new()
	hud_flow_bar.max_value = 100.0
	hud_flow_bar.value = 0.0
	hud_flow_bar.custom_minimum_size = Vector2(180, 28)
	top_hbox.add_child(hud_flow_bar)

	hud_plates_label = Label.new()
	hud_plates_label.text = "Clean Plates: 4 | Dirty Return: 0"
	top_hbox.add_child(hud_plates_label)

	var menu_return_btn: Button = Button.new()
	menu_return_btn.text = "Return to Lobby"
	menu_return_btn.pressed.connect(func() -> void:
		NetworkManager.close_connection()
		_return_to_lobby_ui("Returned to Lobby.")
	)
	top_hbox.add_child(menu_return_btn)

	hud_orders_label = Label.new()
	hud_orders_label.name = "OrdersQueueLabel"
	hud_orders_label.visible = false
	hud_orders_label.position = Vector2(16, 52)
	hud_orders_label.modulate = Color(1.0, 0.95, 0.78)
	hud_layer.add_child(hud_orders_label)

	hud_toast_label = Label.new()
	hud_toast_label.name = "ToastLabel"
	hud_toast_label.visible = false
	hud_toast_label.position = Vector2(360, 96)
	hud_toast_label.add_theme_font_size_override("font_size", 22)
	hud_layer.add_child(hud_toast_label)

	hud_prompt_label = Label.new()
	hud_prompt_label.name = "BottomPromptLabel"
	hud_prompt_label.visible = false
	hud_prompt_label.position = Vector2(220, 675)
	hud_prompt_label.modulate = Color(0.9, 0.95, 1.0)
	hud_layer.add_child(hud_prompt_label)

	# Jiwon's Signature Delegation Ability Bar: "CALL IN THE CREW"
	jiwon_crew_bar = HBoxContainer.new()
	jiwon_crew_bar.name = "JiwonCrewBar"
	jiwon_crew_bar.visible = false
	jiwon_crew_bar.position = Vector2(370, 620)
	jiwon_crew_bar.add_theme_constant_override("separation", 10)
	hud_layer.add_child(jiwon_crew_bar)

	var crew_lbl: Label = Label.new()
	crew_lbl.text = "CALL CREW:"
	crew_lbl.modulate = Color(1.0, 0.85, 0.35)
	jiwon_crew_bar.add_child(crew_lbl)

	hire_prep_btn = Button.new()
	hire_prep_btn.text = "Hire Prep Assistant (20c)"
	hire_prep_btn.pressed.connect(func() -> void:
		if kitchen_instance != null:
			kitchen_instance.request_hire_worker(kitchen_instance.local_peer_id, "prep_assistant")
	)
	jiwon_crew_bar.add_child(hire_prep_btn)

	hire_dish_btn = Button.new()
	hire_dish_btn.text = "Hire Dishwasher (20c)"
	hire_dish_btn.pressed.connect(func() -> void:
		if kitchen_instance != null:
			kitchen_instance.request_hire_worker(kitchen_instance.local_peer_id, "dishwasher")
	)
	jiwon_crew_bar.add_child(hire_dish_btn)

	# Mobile Touch Controls (Left D-Pad / Virtual Joystick + Right Action Buttons)
	var touch_container: Control = Control.new()
	touch_container.name = "MobileTouchControls"
	touch_container.visible = false
	touch_container.set_anchors_preset(Control.PRESET_FULL_RECT)
	touch_container.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hud_layer.add_child(touch_container)

	_create_dpad_button(touch_container, "▲", Vector2(96, 520), Vector2(0, -1))
	_create_dpad_button(touch_container, "▼", Vector2(96, 630), Vector2(0, 1))
	_create_dpad_button(touch_container, "◀", Vector2(32, 575), Vector2(-1, 0))
	_create_dpad_button(touch_container, "▶", Vector2(160, 575), Vector2(1, 0))

	var touch_interact: Button = Button.new()
	touch_interact.text = "INTERACT / PASS\n(E / Space)"
	touch_interact.position = Vector2(1060, 535)
	touch_interact.custom_minimum_size = Vector2(190, 68)
	touch_interact.pressed.connect(func() -> void:
		if kitchen_instance != null:
			kitchen_instance.trigger_primary_interact()
	)
	touch_container.add_child(touch_interact)

	var touch_strike: Button = Button.new()
	touch_strike.text = "STRIKE / SCRUB / BOWL\n(F Key)"
	touch_strike.position = Vector2(1060, 615)
	touch_strike.custom_minimum_size = Vector2(190, 68)
	touch_strike.pressed.connect(func() -> void:
		if kitchen_instance != null:
			kitchen_instance.trigger_secondary_action()
	)
	touch_container.add_child(touch_strike)

	# --- 3. END OF SHIFT MODAL ---
	end_shift_modal = PanelContainer.new()
	end_shift_modal.visible = false
	end_shift_modal.custom_minimum_size = Vector2(540, 360)
	end_shift_modal.position = Vector2(370, 170)
	hud_layer.add_child(end_shift_modal)

	var end_vbox: VBoxContainer = VBoxContainer.new()
	end_vbox.add_theme_constant_override("separation", 14)
	end_shift_modal.add_child(end_vbox)

	var end_title: Label = Label.new()
	end_title.text = "SHIFT COMPLETE!"
	end_title.add_theme_font_size_override("font_size", 28)
	end_vbox.add_child(end_title)

	end_shift_details = Label.new()
	end_vbox.add_child(end_shift_details)

	var retry_btn: Button = Button.new()
	retry_btn.text = "RETRY SHIFT"
	retry_btn.custom_minimum_size = Vector2(200, 46)
	retry_btn.pressed.connect(func() -> void:
		end_shift_modal.visible = false
		ShiftManager.request_restart_shift.rpc_id(1)
	)
	end_vbox.add_child(retry_btn)

func _create_dpad_button(parent: Control, label: String, pos: Vector2, dir: Vector2) -> void:
	var btn: Button = Button.new()
	btn.text = label
	btn.position = pos
	btn.custom_minimum_size = Vector2(58, 52)
	btn.button_down.connect(func() -> void:
		if kitchen_instance != null:
			kitchen_instance.virtual_joystick_vec = dir
	)
	btn.button_up.connect(func() -> void:
		if kitchen_instance != null:
			kitchen_instance.virtual_joystick_vec = Vector2.ZERO
	)
	parent.add_child(btn)

func _on_progression_updated() -> void:
	_refresh_progression_ui()

func _refresh_progression_ui() -> void:
	if progression_header_lbl != null:
		var linh_desc: String = GameData.linh_tiers[ProgressionManager.linh_tier].title
		var jiwon_desc: String = GameData.jiwon_tiers[ProgressionManager.jiwon_tier].title
		var endless_txt: String = " | Endless Best: %d pts (%d orders)" % [ProgressionManager.endless_best_score, ProgressionManager.endless_best_orders] if ProgressionManager.endless_best_score > 0 else ""
		progression_header_lbl.text = "Vault: %d Coins | Linh: %s (Tier %d) | Jiwon: %s (Tier %d)%s" % [
			ProgressionManager.coins,
			linh_desc,
			ProgressionManager.linh_tier,
			jiwon_desc,
			ProgressionManager.jiwon_tier,
			endless_txt
		]
		progression_header_lbl.modulate = Color(1.0, 0.88, 0.35)

	if stage_select_btn != null:
		stage_select_btn.clear()
		for lvl_id: int in range(1, 6):
			var lvl: CampaignLevelSpec = GameData.campaign_levels.get(lvl_id, null)
			var lvl_title: String = lvl.title if lvl != null else "Level %d" % lvl_id
			var setting: String = lvl.setting_name if lvl != null else ""
			var rec: Dictionary = ProgressionManager.stage_records.get(lvl_id, {})
			var stars_txt: String = " (" + "★".repeat(int(rec.get("stars", 0))) + ")" if int(rec.get("stars", 0)) > 0 else ""
			var locked_tag: String = " [LOCKED - Complete Level %d]" % (lvl_id - 1) if lvl_id > ProgressionManager.highest_unlocked_stage else ""
			var label: String = "Level %d: %s — %s%s%s" % [lvl_id, lvl_title, setting, stars_txt, locked_tag]
			stage_select_btn.add_item(label, lvl_id - 1)
			stage_select_btn.set_item_metadata(lvl_id - 1, lvl_id)
			stage_select_btn.set_item_disabled(lvl_id - 1, lvl_id > ProgressionManager.highest_unlocked_stage)
		var current_sel: int = clamp(ProgressionManager.selected_stage, 1, 5)
		stage_select_btn.select(current_sel - 1)

	if shop_container != null:
		for ch: Node in shop_container.get_children():
			ch.queue_free()
		for up_id: String in ProgressionManager.catalog_upgrades.keys():
			var up: Dictionary = ProgressionManager.catalog_upgrades[up_id]
			var up_btn: Button = Button.new()
			var owned: bool = bool(up.get("purchased", false))
			if owned:
				up_btn.text = "✓ %s\n(OWNED)" % up.get("display_name", "")
				up_btn.disabled = true
			else:
				up_btn.text = "%s\n(Buy: %d Coins)" % [up.get("display_name", ""), int(up.get("cost", 0))]
				up_btn.disabled = (ProgressionManager.coins < int(up.get("cost", 0)))
			up_btn.custom_minimum_size = Vector2(240, 52)
			var cur_uid: String = up_id
			up_btn.pressed.connect(func() -> void:
				var res: Dictionary = ProgressionManager.purchase_upgrade(cur_uid)
				if not res.get("ok", false):
					_on_feedback_banner(res.get("reason", "Cannot buy upgrade."), Color(1.0, 0.4, 0.4))
				else:
					_on_feedback_banner("Purchased %s!" % up.get("display_name", ""), Color(0.3, 1.0, 0.5))
			)
			shop_container.add_child(up_btn)

func _submit_lobby_loadout() -> void:
	var c_id: String = str(char_select_btn.get_selected_metadata())
	var s_id: String = str(spec_select_btn.get_selected_metadata())
	if NetworkManager.is_lan_active and multiplayer.multiplayer_peer != null:
		NetworkManager.request_selection.rpc_id(1, c_id, s_id)

func _on_lobby_updated(players_dict: Dictionary) -> void:
	if players_dict.is_empty():
		players_list_label.text = "Lobby Roster:\n  (Not connected)"
		return
	var lines: Array[String] = ["Lobby Roster (%d/2 Connected):" % players_dict.size()]
	for pid: int in players_dict.keys():
		var p: Dictionary = players_dict[pid]
		var c_id: String = str(p.get("character", "linh"))
		var s_id: String = str(p.get("specialization", "prep_master"))
		var r_str: String = "★ READY" if bool(p.get("ready", false)) else "NOT READY"
		lines.append("  • [Peer %d] %s — Chef: %s | Spec: %s | %s" % [
			pid,
			str(p.get("name", "Chef")),
			c_id.to_upper(),
			s_id.to_upper(),
			r_str
		])
	players_list_label.text = "\n".join(lines)

func _on_connection_status_changed(status_text: String, is_error: bool) -> void:
	status_label.text = "Status: %s" % status_text
	status_label.modulate = Color(1.0, 0.42, 0.42) if is_error else Color(0.45, 0.98, 0.62)

func _on_shift_start_requested() -> void:
	lobby_panel.visible = false
	end_shift_modal.visible = false
	(hud_layer.get_node("TopHUDBar") as Control).visible = true
	hud_orders_label.visible = true
	hud_prompt_label.visible = true
	hud_toast_label.visible = true
	(hud_layer.get_node("MobileTouchControls") as Control).visible = true

	var my_peer: int = multiplayer.get_unique_id() if (NetworkManager.is_lan_active and multiplayer.multiplayer_peer != null) else 1
	var my_char: String = str(NetworkManager.players.get(my_peer, {}).get("character", "linh"))
	if jiwon_crew_bar != null:
		jiwon_crew_bar.visible = (my_char == "jiwon")

	if kitchen_instance != null:
		kitchen_instance.queue_free()
	kitchen_instance = KitchenController.new()
	kitchen_instance.name = "KitchenController"
	kitchen_instance.local_prompt_changed.connect(func(txt: String) -> void:
		hud_prompt_label.text = txt
	)
	kitchen_instance.status_toast.connect(_on_feedback_banner)
	add_child(kitchen_instance)

	if NetworkManager.is_host():
		var mode: int = ShiftManager.GameMode.ENDLESS_RUSH if is_endless_selected else ShiftManager.GameMode.CAMPAIGN
		ShiftManager.start_new_shift(ShiftManager.DEFAULT_SHIFT_DURATION, ProgressionManager.selected_stage, mode)

func _on_returned_to_menu(reason: String) -> void:
	_return_to_lobby_ui(reason)

func _return_to_lobby_ui(reason: String) -> void:
	ShiftManager.shift_active = false
	if kitchen_instance != null:
		kitchen_instance.queue_free()
		kitchen_instance = null
	lobby_panel.visible = true
	end_shift_modal.visible = false
	(hud_layer.get_node("TopHUDBar") as Control).visible = false
	hud_orders_label.visible = false
	hud_prompt_label.visible = false
	hud_toast_label.visible = false
	if jiwon_crew_bar != null:
		jiwon_crew_bar.visible = false
	(hud_layer.get_node("MobileTouchControls") as Control).visible = false
	_on_connection_status_changed(reason, true)

func _on_shift_state_updated(state: Dictionary) -> void:
	hud_score_label.text = "SCORE: %d" % int(state.get("total_score", 0))
	hud_timer_label.text = "TIME: %ds" % int(ceil(float(state.get("shift_time_remaining", 0.0))))
	hud_plates_label.text = "Clean Vessels: %d | Dirty Return: %d" % [
		int(state.get("clean_plates", 4)),
		int(state.get("dirty_plates_at_return", 0))
	]

func _on_order_queue_updated(orders: Array) -> void:
	var lines: Array[String] = ["ACTIVE CUSTOMER ORDERS:"]
	for o: Variant in orders:
		if o is Dictionary:
			var od: Dictionary = o
			var handoff_tag: String = " [Handoff Bonus Claimed]" if bool(od.get("handoff_claimed", false)) else " [Pass Counter Handoff +15 Flow Available]"
			lines.append("  #%d %s (%s) — %ds left | %d pts%s" % [
				int(od.get("order_id", 0)),
				str(od.get("display_name", "")),
				str(od.get("culinary_origin", "")),
				int(ceil(float(od.get("remaining_patience", 0.0)))),
				int(od.get("base_score", 100)),
				handoff_tag
			])
	hud_orders_label.text = "\n".join(lines)

func _on_flow_updated(flow_val: float, rush_active: bool, rush_rem: float) -> void:
	hud_flow_bar.value = 100.0 if rush_active else flow_val
	if rush_active:
		hud_flow_label.text = "★ RUSH RHYTHM 1.5x! (%.1fs) ★" % rush_rem
		hud_flow_label.modulate = Color(1.0, 0.82, 0.18)
	else:
		hud_flow_label.text = "KITCHEN FLOW: %d / 100" % int(round(flow_val))
		hud_flow_label.modulate = Color(0.55, 0.95, 1.0)

func _on_feedback_banner(message: String, color: Color) -> void:
	hud_toast_label.text = message
	hud_toast_label.modulate = color

func _on_shift_ended(summary: Dictionary) -> void:
	end_shift_modal.visible = true
	var mode: int = int(summary.get("game_mode", ShiftManager.GameMode.CAMPAIGN))

	if mode == ShiftManager.GameMode.ENDLESS_RUSH:
		var new_high_tag: String = " 🎉 NEW PERSONAL BEST!" if bool(summary.get("new_high_score", false)) else ""
		end_shift_details.text = (
			"MODE: ENDLESS RUSH (Survival)%s\n" +
			"Time Survived: %.1fs\n" +
			"Final Score: %d (All-Time Best: %d)\n" +
			"Coins Earned: +%d (Balance: %d Coins)\n" +
			"Orders Served: %d | Orders Lost: %d\n" +
			"Coordinated Pass Counter Handoffs: %d\n" +
			"Rush Rhythms Triggered: %d"
		) % [
			new_high_tag,
			float(summary.get("endless_elapsed_sec", 0.0)),
			int(summary.get("total_score", 0)),
			int(summary.get("best_score", 0)),
			int(summary.get("earned_coins", 0)),
			int(summary.get("new_balance", ProgressionManager.coins)),
			int(summary.get("orders_completed", 0)),
			int(summary.get("orders_failed", 0)),
			int(summary.get("handoffs_completed", 0)),
			int(summary.get("rush_rhythms_triggered", 0)),
		]
	else:
		var stars_str: String = "★".repeat(int(summary.get("stars", 1)))
		var lvl: CampaignLevelSpec = GameData.campaign_levels.get(int(summary.get("stage_id", 1)), null)
		var stage_name: String = lvl.title if lvl != null else "Level %d" % int(summary.get("stage_id", 1))
		var unlock_msg: String = "\n🎉 NEXT LEVEL UNLOCKED!" if bool(summary.get("newly_unlocked", false)) else ""
		var tier_msg: String = "\n⭐ CHARACTER TIER UPGRADED! (Linh Tier %d | Jiwon Tier %d)" % [
			int(summary.get("linh_tier", 1)),
			int(summary.get("jiwon_tier", 1))
		] if bool(summary.get("tier_upgraded", false)) else ""
		end_shift_details.text = (
			"Level: %s\n" +
			"Rating: %s (%d Stars)\n" +
			"Final Score: %d\n" +
			"Coins Earned: +%d (Balance: %d Coins)%s%s\n" +
			"Orders Served: %d | Orders Expired: %d\n" +
			"Coordinated Pass Counter Handoffs: %d\n" +
			"Linh <-> Jiwon Cross-Prep Transfers: %d\n" +
			"Rush Rhythms Triggered: %d\n" +
			"Ingredients Wasted: %d"
		) % [
			stage_name,
			stars_str,
			int(summary.get("stars", 1)),
			int(summary.get("total_score", 0)),
			int(summary.get("earned_coins", 0)),
			int(summary.get("new_balance", ProgressionManager.coins)),
			unlock_msg,
			tier_msg,
			int(summary.get("orders_completed", 0)),
			int(summary.get("orders_failed", 0)),
			int(summary.get("handoffs_completed", 0)),
			int(summary.get("cross_prep_transfers", 0)),
			int(summary.get("rush_rhythms_triggered", 0)),
			int(summary.get("ingredients_wasted", 0)),
		]
