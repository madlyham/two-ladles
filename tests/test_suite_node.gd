extends Node
## Automated Test Suite Node for Two Ladles (Milestones 0-2).
## Runs inside the active SceneTree with full Autoload access (`GameData`, `NetworkManager`, `ShiftManager`).
## Run via: godot --headless -- --run-tests

const CampaignLevelSpec = preload("res://scripts/data/campaign_level_spec.gd")
const CharacterProgressionData = preload("res://scripts/data/character_progression_data.gd")
const RecipeData = preload("res://scripts/data/recipe_data.gd")

var tests_run: int = 0
var tests_passed: int = 0
var tests_failed: int = 0
var failure_messages: Array[String] = []

func _ready() -> void:
	print("============================================================")
	print("  TWO LADLES — AUTOMATED HEADLESS TEST SUITE (Godot 4.7.2)")
	print("============================================================")

	_test_recipes_and_cross_prep_architecture()
	_test_prep_station_rhythm_cadence()
	_test_cooking_station_state_machine_and_pho_separation()
	_test_assembly_station_rules()
	_test_pass_counter_handoff_and_anti_exploit()
	_test_kitchen_flow_and_rush_rhythm()
	_test_washing_station_plate_lifecycle()
	_test_lobby_exclusivity_and_ready_reset()
	_test_progression_system()
	_test_linh_execution_specialist_tiers()
	_test_jiwon_delegation_part_time_crew()
	_test_campaign_levels_and_progression_unlocks()
	_test_endless_rush_mode()

	print("------------------------------------------------------------")
	print("  RESULTS: %d / %d Passed | %d Failed" % [tests_passed, tests_run, tests_failed])
	print("------------------------------------------------------------")
	if tests_failed > 0:
		for msg: String in failure_messages:
			printerr("  [FAIL] %s" % msg)
		get_tree().quit(1)
	else:
		print("  ALL AUTOMATED TESTS PASSED CLEANLY.")
		get_tree().quit(0)

func _assert(condition: bool, description: String) -> void:
	tests_run += 1
	if condition:
		tests_passed += 1
		print("  [PASS] %s" % description)
	else:
		tests_failed += 1
		failure_messages.append(description)
		printerr("  [FAIL] %s" % description)

func _test_recipes_and_cross_prep_architecture() -> void:
	print("\n[Suite 1] Data-Driven Recipes & Cross-Prep Interdependence")
	_assert(GameData.recipes.size() == 4, "Exactly 4 starter recipes registered")
	_assert(GameData.recipes.has("pho_bo") and not GameData.recipes["pho_bo"].is_fusion, "Pho Bo registered as Traditional Vietnamese dish")
	_assert(GameData.recipes.has("banh_mi_thit") and not GameData.recipes["banh_mi_thit"].is_fusion, "Banh Mi Thit registered as Traditional Vietnamese dish")
	_assert(GameData.recipes.has("tteokbokki") and not GameData.recipes["tteokbokki"].is_fusion, "Tteokbokki registered as Traditional Korean dish")
	_assert(
		GameData.recipes.has("bulgogi_banh_mi") and GameData.recipes["bulgogi_banh_mi"].is_fusion and GameData.recipes["bulgogi_banh_mi"].culinary_origin.find("Fusion") != -1,
		"Bulgogi Banh Mi explicitly marked as Original Vietnam-Korea Fusion Specialty"
	)

	var pork_spec: RecipeData.IngredientSpec = GameData.ingredients["pork_raw"]
	var bulgogi_spec: RecipeData.IngredientSpec = GameData.ingredients["bulgogi_raw"]
	var daikon_spec: RecipeData.IngredientSpec = GameData.ingredients["daikon_carrot_raw"]
	var kimchi_spec: RecipeData.IngredientSpec = GameData.ingredients["kimchi_raw"]
	_assert(pork_spec.origin_zone == "linh_west" and pork_spec.partner_target_zone == "jiwon_east", "Linh preps Pork Belly on West side for Jiwon's East Grill")
	_assert(bulgogi_spec.origin_zone == "linh_west" and bulgogi_spec.partner_target_zone == "jiwon_east", "Linh preps Marinated Bulgogi on West side for Jiwon's East Sear Pan")
	_assert(daikon_spec.origin_zone == "jiwon_east" and daikon_spec.partner_target_zone == "linh_west", "Jiwon preps Pickled Daikon & Carrot on East side for Linh's West Assembly")
	_assert(kimchi_spec.origin_zone == "jiwon_east" and kimchi_spec.partner_target_zone == "linh_west", "Jiwon preps Kimchi Slaw on East side for Linh's West Assembly")

func _test_prep_station_rhythm_cadence() -> void:
	print("\n[Suite 2] Prep Counter 4-Strike Rhythm Cadence & Miss Non-Blocking")
	var prep: PrepStation = PrepStation.new()

	var place_res: Dictionary = prep.place_raw_item({"item_id": "pork_raw", "display_name": "Raw Pork Belly"})
	_assert(bool(place_res.get("ok", false)), "Prep Station accepts raw pork belly")

	var s1: Dictionary = prep.perform_strike(1.00, "prep_master", "linh")
	var _s2: Dictionary = prep.perform_strike(1.45, "prep_master", "linh")
	var _s3: Dictionary = prep.perform_strike(1.90, "prep_master", "linh")
	var s4: Dictionary = prep.perform_strike(2.35, "prep_master", "linh")
	_assert(s1["grade"] == RecipeData.CadenceGrade.PERFECT and s4["grade"] == RecipeData.CadenceGrade.PERFECT, "Ideal 0.45s strike intervals graded as PERFECT")
	_assert(bool(s4["completed"]), "4th strike completes preparation")

	var prepped_pork: Dictionary = prep.take_item()
	_assert(prepped_pork.get("item_id", "") == "pork_portioned", "Raw pork transforms into pork_portioned after 4 strikes")
	_assert(int(prepped_pork.get("quality_tier", 0)) == RecipeData.QualityTier.OPTIMAL, "Perfect cadence grants OPTIMAL quality tier")
	_assert(int(prepped_pork.get("prep_perfect_bonus", 0)) == 25, "Prep Master specialization adds +15 bonus on top of +10 Perfect cadence bonus")

	prep.place_raw_item({"item_id": "kimchi_raw", "display_name": "Aged Kimchi"})
	prep.perform_strike(10.0, "", "jiwon")
	var m2: Dictionary = prep.perform_strike(12.5, "", "jiwon")
	var _m3: Dictionary = prep.perform_strike(15.0, "", "jiwon")
	var m4: Dictionary = prep.perform_strike(18.0, "", "jiwon")
	_assert(m2["grade"] == RecipeData.CadenceGrade.MISS, "Off-beat strike graded as MISS")
	_assert(bool(m4["completed"]), "Misses never permanently prevent task completion (4 strikes still complete)")
	var prepped_kimchi: Dictionary = prep.take_item()
	_assert(prepped_kimchi.get("item_id", "") == "kimchi_slaw", "Kimchi slaw produced even with missed rhythm cadence")
	prep.free()

func _test_cooking_station_state_machine_and_pho_separation() -> void:
	print("\n[Suite 3] Cooking State Machine (RAW -> COOKING -> OPTIMAL -> OVERCOOKED -> BURNT) & Pho Separation")
	var stockpot: CookingStation = CookingStation.new()
	stockpot.appliance_id = "stockpot"
	stockpot.refill_stockpot()

	var blancher: CookingStation = CookingStation.new()
	blancher.appliance_id = "noodle_blancher"

	var bad_noodle_in_stock: Dictionary = stockpot.add_ingredient({"item_id": "noodles_raw", "display_name": "Dry Rice Noodles"})
	_assert(not bool(bad_noodle_in_stock.get("ok", false)), "Dedicated Stockpot rejects raw rice noodles (enforces separate noodle blanching)")

	var good_noodle_blanch: Dictionary = blancher.add_ingredient({"item_id": "noodles_raw", "display_name": "Dry Rice Noodles"})
	_assert(bool(good_noodle_blanch.get("ok", false)), "Noodle Blancher accepts raw rice noodles")

	var grill: CookingStation = CookingStation.new()
	grill.appliance_id = "grill_pan"
	grill.add_ingredient({"item_id": "bulgogi_portioned", "display_name": "Portioned Bulgogi"})
	_assert(grill.cooking_state == RecipeData.CookingState.COOKING, "Grill starts in COOKING state when ingredient added")

	grill.tick_cooking(2.0)
	_assert(grill.cooking_state == RecipeData.CookingState.COOKING, "At 2.0s Grill remains in COOKING state")

	grill.tick_cooking(2.5)
	_assert(grill.cooking_state == RecipeData.CookingState.OPTIMAL, "At 4.5s Grill enters OPTIMAL state")

	grill.tick_cooking(5.0)
	_assert(grill.cooking_state == RecipeData.CookingState.OVERCOOKED, "At 9.5s Grill transitions to OVERCOOKED state")

	grill.tick_cooking(4.0)
	_assert(grill.cooking_state == RecipeData.CookingState.BURNT, "At 13.5s Grill transitions to BURNT state")

	var burnt_res: Dictionary = grill.retrieve_cooked_item("jiwon")
	_assert(burnt_res.get("item", {}).get("item_id", "") == "burnt_waste", "Retrieving BURNT food produces burnt_waste")

	grill.add_ingredient({"item_id": "pork_portioned", "display_name": "Portioned Pork"}, "flame_keeper")
	grill.tick_cooking(9.8)
	_assert(grill.cooking_state == RecipeData.CookingState.OPTIMAL, "Flame Keeper specialization extends OPTIMAL cooking window (+35%)")

	stockpot.free()
	blancher.free()
	grill.free()

func _test_assembly_station_rules() -> void:
	print("\n[Suite 4] Recipe-Driven Assembly & Pho Bo Ordered Broth Constraint")
	var asm: AssemblyStation = AssemblyStation.new()

	var no_vessel_res: Dictionary = asm.add_ingredient({"item_id": "noodles_blanched", "display_name": "Blanched Noodles"})
	_assert(not bool(no_vessel_res.get("ok", false)), "Assembly rejects ingredients before a clean Plate or Bowl is placed")

	asm.place_vessel("bowl")
	var early_broth_res: Dictionary = asm.add_ingredient({"item_id": "broth_hot", "display_name": "Hot Broth"})
	_assert(not bool(early_broth_res.get("ok", false)), "Pho Bo rejects pouring hot broth before noodles and sliced beef are in the bowl")

	var add_beef: Dictionary = asm.add_ingredient({"item_id": "beef_sliced", "display_name": "Sliced Beef"})
	var add_noodles: Dictionary = asm.add_ingredient({"item_id": "noodles_blanched", "display_name": "Blanched Noodles"})
	var add_broth: Dictionary = asm.add_ingredient({"item_id": "broth_hot", "display_name": "Hot Broth"})
	_assert(bool(add_beef.get("ok", false)) and bool(add_noodles.get("ok", false)) and bool(add_broth.get("ok", false)), "Valid Pho Bo sequence accepted")
	_assert(bool(add_broth.get("completed", false)), "Pho Bo completes once noodles, beef, and hot broth are assembled")
	var pho_dish: Dictionary = asm.take_item()
	_assert(pho_dish.get("recipe_id", "") == "pho_bo", "Completed dish has recipe_id == pho_bo")

	asm.place_vessel("plate")
	var invalid_mix: Dictionary = asm.add_ingredient({"item_id": "tteokbokki_simmered", "display_name": "Simmered Tteokbokki"})
	_assert(not bool(invalid_mix.get("ok", false)), "Assembly rejects Tteokbokki on a flat Plate")

	asm.add_ingredient({"item_id": "baguette", "display_name": "Baguette"})
	asm.add_ingredient({"item_id": "kimchi_slaw", "display_name": "Kimchi Slaw"})
	var finish_fusion: Dictionary = asm.add_ingredient({"item_id": "bulgogi_seared", "display_name": "Seared Bulgogi"}, "fusion_artist")
	_assert(bool(finish_fusion.get("completed", false)), "Bulgogi Banh Mi completes when Baguette, Kimchi Slaw, and Seared Bulgogi are combined")
	asm.free()

func _test_pass_counter_handoff_and_anti_exploit() -> void:
	print("\n[Suite 5] Pass Counter Handoff (+15 Flow) & Anti-Exploit Rules")
	ShiftManager.start_new_shift(180.0)
	var pass_c: PassCounter = PassCounter.new()

	var item_pork: Dictionary = {
		"uid": "item_pork_1",
		"item_id": "pork_portioned",
		"display_name": "Portioned Pork Belly",
		"prepared_by_character": "linh",
	}

	pass_c.place_item(item_pork, 1, 1000)
	var self_pickup: Dictionary = pass_c.retrieve_item(1, 2000)
	_assert(not bool(self_pickup["handoff"].get("granted", false)), "Anti-Exploit: Self-retrieval by same player grants 0 Flow")
	_assert(is_equal_approx(ShiftManager.kitchen_flow, 0.0), "Kitchen Flow remains 0 after self-retrieval")

	pass_c.place_item(item_pork, 1, 1000)
	var late_pickup: Dictionary = pass_c.retrieve_item(2, 6500)
	_assert(not bool(late_pickup["handoff"].get("granted", false)), "Anti-Exploit: Retrieval after 4-second window grants 0 Flow")

	pass_c.place_item(item_pork, 1, 10000)
	var valid_handoff: Dictionary = pass_c.retrieve_item(2, 12500)
	_assert(bool(valid_handoff["handoff"].get("granted", false)), "Valid handoff between Player 1 and Player 2 within 2.5s grants handoff bonus")
	_assert(is_equal_approx(ShiftManager.kitchen_flow, 15.0), "Kitchen Flow increases by exactly +15 points")

	var item_pickles: Dictionary = {
		"uid": "item_pickles_2",
		"item_id": "pickled_veg",
		"display_name": "Pickled Veg",
		"order_id": valid_handoff["handoff"]["order_id"],
	}
	pass_c.place_item(item_pickles, 2, 13000)
	var duplicate_order_handoff: Dictionary = pass_c.retrieve_item(1, 14000)
	_assert(not bool(duplicate_order_handoff["handoff"].get("granted", false)), "Anti-Exploit: An order can claim the coordinated handoff bonus only once")
	_assert(is_equal_approx(ShiftManager.kitchen_flow, 15.0), "Kitchen Flow unchanged at 15.0 on duplicate order handoff attempt")
	pass_c.free()

func _test_kitchen_flow_and_rush_rhythm() -> void:
	print("\n[Suite 6] Kitchen Flow (0-100) & Rush Rhythm (15s, 1.5x Multiplier, Paused Flow Gain)")
	ShiftManager.start_new_shift(180.0)

	ShiftManager.modify_flow(90.0, "Test Flow Boost")
	_assert(is_equal_approx(ShiftManager.kitchen_flow, 90.0) and not ShiftManager.rush_rhythm_active, "Flow at 90 does not trigger Rush Rhythm yet")

	ShiftManager.modify_flow(15.0, "Trigger Rush Rhythm")
	_assert(ShiftManager.rush_rhythm_active, "Reaching 100 Flow activates Rush Rhythm")
	_assert(is_equal_approx(ShiftManager.kitchen_flow, 0.0), "Activating Rush Rhythm resets Kitchen Flow meter to 0")
	_assert(is_equal_approx(ShiftManager.rush_rhythm_remaining, 15.0), "Rush Rhythm duration is initialized to 15.0 seconds")

	var gained_during_rush: float = ShiftManager.modify_flow(25.0, "Attempt Flow Gain During Rush")
	_assert(is_equal_approx(gained_during_rush, 0.0) and is_equal_approx(ShiftManager.kitchen_flow, 0.0), "Flow generation is paused while Rush Rhythm is active")

	var serve_res: Dictionary = ShiftManager.serve_completed_dish({
		"recipe_id": "banh_mi_thit",
		"quality_tier": RecipeData.QualityTier.STANDARD,
		"prep_perfect_bonus": 0,
		"completed_dish": true,
	}, 1)
	_assert(bool(serve_res.get("ok", false)) and int(serve_res.get("score_awarded", 0)) == 190, "Rush Rhythm applies 1.5x multiplier to base order score (165 + 25 tip = 190)")

	ShiftManager.tick_shift(15.1)
	_assert(not ShiftManager.rush_rhythm_active, "Rush Rhythm deactivates after 15 seconds")
	ShiftManager.modify_flow(20.0, "Post-Rush Flow")
	_assert(is_equal_approx(ShiftManager.kitchen_flow, 20.0), "Flow generation resumes cleanly after Rush Rhythm ends")

func _test_washing_station_plate_lifecycle() -> void:
	print("\n[Suite 7] Washing Station & Finite Synchronized Plate Stack")
	ShiftManager.start_new_shift(180.0)
	var wash: WashingStation = WashingStation.new()

	_assert(ShiftManager.clean_plates == 4, "Kitchen starts with finite pool of 4 clean plates/bowls")
	for _i: int in range(4):
		var take_res: Dictionary = wash.take_clean_vessel("plate")
		_assert(bool(take_res.get("ok", false)), "Took clean vessel from stack")

	var fifth_try: Dictionary = wash.take_clean_vessel("plate")
	_assert(not bool(fifth_try.get("ok", false)), "5th vessel pickup rejected when all 4 plates are in use (no unlimited disposable plates)")

	ShiftManager.serve_completed_dish({
		"recipe_id": "pho_bo",
		"quality_tier": RecipeData.QualityTier.STANDARD,
		"completed_dish": true,
	}, 1)
	_assert(ShiftManager.dirty_plates_at_return == 1, "Serving a dish generates 1 dirty plate at the Return Tray")

	wash.perform_scrub_stroke()
	wash.perform_scrub_stroke()
	wash.perform_scrub_stroke()
	var stroke4: Dictionary = wash.perform_scrub_stroke()
	_assert(bool(stroke4.get("plate_cleaned", false)) and ShiftManager.clean_plates == 1, "4 scrub strokes clean the dirty plate and return it to the shared clean stack")
	wash.free()

func _test_lobby_exclusivity_and_ready_reset() -> void:
	print("\n[Suite 8] Multiplayer Lobby Character Exclusivity & Readiness Reset")
	NetworkManager.players = {
		1: {"peer_id": 1, "name": "Host", "character": "linh", "specialization": "prep_master", "ready": true},
		2: {"peer_id": 2, "name": "Client", "character": "jiwon", "specialization": "flame_keeper", "ready": true},
	}

	var start_ok: Dictionary = NetworkManager.can_start_shift(true)
	_assert(bool(start_ok.get("ok", false)), "Host can start shift when both Linh and Jiwon are connected and Ready")

	var dup_char: Dictionary = NetworkManager.apply_player_selection(2, "linh", "flame_keeper")
	_assert(not bool(dup_char.get("ok", false)), "Lobby rejects selecting Linh when partner already selected Linh")

	var same_spec: Dictionary = NetworkManager.apply_player_selection(2, "jiwon", "prep_master")
	_assert(bool(same_spec.get("ok", false)), "Lobby allows both players to equip the same specialization (PREP MASTER)")
	_assert(not bool(NetworkManager.players[2]["ready"]), "Changing specialization automatically resets player Ready status to false")

	var start_blocked: Dictionary = NetworkManager.can_start_shift(true)
	_assert(not bool(start_blocked.get("ok", false)), "Host cannot start shift after a player's Ready status was reset")

func _test_progression_system() -> void:
	print("\n[Suite 9] Milestone 3 Progression, Stage Unlocks & Kitchen Upgrades")
	ProgressionManager.reset_progression_for_testing()

	_assert(ProgressionManager.coins == 150, "Player starts with 150 initial seed coins")
	_assert(ProgressionManager.highest_unlocked_stage == 1, "Only Stage 1 is unlocked initially")

	# Shift 1: 1 Star result (score 250) -> does not unlock Stage 2
	var s1_res: Dictionary = ProgressionManager.record_shift_results(1, {
		"total_score": 250,
		"stars": 1,
		"handoffs_completed": 2,
		"rush_rhythms_triggered": 0
	})
	_assert(int(s1_res.get("earned_coins", 0)) == 41, "Shift 1 earned 41 coins (250/8 + 2*5)")
	_assert(ProgressionManager.highest_unlocked_stage == 1, "Stage 2 remains locked after 1-star shift")

	# Shift 2: 2 Stars result (score 400) -> unlocks Stage 2!
	var s2_res: Dictionary = ProgressionManager.record_shift_results(1, {
		"total_score": 400,
		"stars": 2,
		"handoffs_completed": 3,
		"rush_rhythms_triggered": 1
	})
	_assert(bool(s2_res.get("newly_unlocked", false)), "Achieving 2 stars unlocks the next stage")
	_assert(ProgressionManager.highest_unlocked_stage == 2, "Highest unlocked stage is now Stage 2 (Village Market Restaurant)")

	# Upgrades: purchase sharp_santoku_knives (cost 100)
	var prev_balance: int = ProgressionManager.coins
	var buy_ok: Dictionary = ProgressionManager.purchase_upgrade("sharp_santoku_knives")
	_assert(bool(buy_ok.get("ok", false)), "Successfully purchased Sharp Santoku Knife Set")
	_assert(ProgressionManager.is_upgrade_owned("sharp_santoku_knives"), "Sharp knives upgrade marked as owned")
	_assert(ProgressionManager.coins == prev_balance - 100, "100 coins deducted for knife upgrade")

	# Duplicate purchase rejected
	var dup_buy: Dictionary = ProgressionManager.purchase_upgrade("sharp_santoku_knives")
	_assert(not bool(dup_buy.get("ok", false)), "Duplicate upgrade purchase is rejected")

	# Test knife upgrade effect on Prep Station rhythm window
	var prep: PrepStation = PrepStation.new()
	prep.place_raw_item({"item_id": "pork_raw", "display_name": "Raw Pork Belly"})
	prep.perform_strike(0.0, "", "linh")
	# Ideal interval is 0.45s. Base good window is 0.28s. With +15% from knife upgrade, window is 0.322s.
	# An interval of 0.75s has delta = 0.30s (> 0.28s, but <= 0.322s)!
	var upgraded_strike: Dictionary = prep.perform_strike(0.75, "", "linh")
	_assert(upgraded_strike["grade"] == RecipeData.CadenceGrade.GOOD, "Purchased knife upgrade widened cadence tolerance window")
	prep.free()

	# Test JSON persistence save and reload
	ProgressionManager.save_progression()
	var reload_ok: bool = ProgressionManager.load_progression()
	_assert(reload_ok, "Progression data saved and reloaded from JSON file successfully")
	_assert(ProgressionManager.highest_unlocked_stage == 2, "Reloaded progression preserves unlocked stages")
	_assert(ProgressionManager.is_upgrade_owned("sharp_santoku_knives"), "Reloaded progression preserves owned upgrades")

func _test_linh_execution_specialist_tiers() -> void:
	print("\n[Suite 10] Linh Execution Specialist: Tiers 1-3 Speed Multipliers & Visual Third Arm")
	var player_linh: PlayerController = PlayerController.new()
	add_child(player_linh)

	# Tier 1: Ordinary Chef
	player_linh.configure_chef(1, "linh", "prep_master", 1)
	_assert(is_equal_approx(player_linh.get_task_speed_multiplier(), 1.0), "Linh Tier 1 task speed multiplier is 1.0x (Ordinary Chef)")
	_assert(not player_linh.third_arm_mesh.visible, "Linh Tier 1 has no mechanical third arm visible")

	# Tier 2: Workshop Engineer (~8% faster, rustic mechanical third arm)
	player_linh.configure_chef(1, "linh", "prep_master", 2)
	_assert(is_equal_approx(player_linh.get_task_speed_multiplier(), 0.92), "Linh Tier 2 task speed multiplier is 0.92x (~8% faster)")
	_assert(player_linh.third_arm_mesh.visible, "Linh Tier 2 has rustic mechanical third arm visible")
	_assert(not player_linh.third_arm_glow_mesh.visible, "Linh Tier 2 has no neon glow on rustic third arm")

	# Tier 3: Precision Engineer (~15% faster, sleek precision white-and-blue glowing arm)
	player_linh.configure_chef(1, "linh", "prep_master", 3)
	_assert(is_equal_approx(player_linh.get_task_speed_multiplier(), 0.85), "Linh Tier 3 task speed multiplier is 0.85x (~15% faster)")
	_assert(player_linh.third_arm_mesh.visible and player_linh.third_arm_glow_mesh.visible, "Linh Tier 3 has futuristic white-and-blue glowing precision arm visible")

	# Verify strike execution timing with Linh Tier 3
	var prep: PrepStation = PrepStation.new()
	prep.place_raw_item({"item_id": "pork_raw", "display_name": "Raw Pork Belly"})
	prep.perform_strike(0.0, "", "linh", 0.85) # First strike
	# Ideal interval for Tier 3 is 0.45s * 0.85 = 0.3825s!
	var fast_strike: Dictionary = prep.perform_strike(0.385, "", "linh", 0.85)
	_assert(fast_strike["grade"] == RecipeData.CadenceGrade.PERFECT, "Linh Tier 3 executes perfect cadence strikes at faster 0.38s intervals")

	remove_child(player_linh)
	player_linh.free()
	prep.free()

func _test_jiwon_delegation_part_time_crew() -> void:
	print("\n[Suite 11] Jiwon Multitasking & Delegation: Part-Time Worker Hiring & State Machine")
	ProgressionManager.coins = 100
	ProgressionManager.jiwon_tier = 1

	var kitchen: KitchenController = KitchenController.new()
	add_child(kitchen)

	var p2: PlayerController = kitchen.players_by_peer[2]
	p2.character_tier = 1

	# 1. Tier 1 (Burnt-out Executive): 0 part-timer slots -> hiring rejected
	var t1_hire: Dictionary = kitchen.request_hire_worker(2, "prep_assistant")
	_assert(not bool(t1_hire.get("ok", false)), "Jiwon Tier 1 cannot hire part-time crew (0 slots available)")

	# 2. Tier 2 (Restaurant Manager): 1 part-timer slot -> can hire 1 worker for 20 coins
	ProgressionManager.jiwon_tier = 2
	p2.character_tier = 2

	var t2_hire: Dictionary = kitchen.request_hire_worker(2, "prep_assistant")
	_assert(bool(t2_hire.get("ok", false)), "Jiwon Tier 2 successfully hires Prep Assistant for 20 coins")
	_assert(ProgressionManager.coins == 80, "20 coins deducted from restaurant vault (Balance: 80)")
	_assert(kitchen.active_workers.size() == 1, "Exactly 1 active part-time worker in kitchen")

	# 3. Attempting to hire second worker at Tier 2 is rejected (slot limit = 1)
	var t2_second_hire: Dictionary = kitchen.request_hire_worker(2, "dishwasher")
	_assert(not bool(t2_second_hire.get("ok", false)), "Hiring second worker rejected under Tier 2 (slot limit = 1)")

	# 4. Verify worker state machine and automated prep strike
	var worker_id: String = t2_hire["worker_id"]
	var worker: PartTimeWorker = kitchen.active_workers[worker_id]
	_assert(worker.current_state == PartTimeWorker.WorkerState.WALKING_IN, "Hired worker enters in WALKING_IN state")

	# Place an unchopped item on East prep counter
	var prep_east: PrepStation = kitchen.stations["prep_east"] as PrepStation
	prep_east.place_raw_item({"item_id": "daikon_carrot_raw", "display_name": "Raw Daikon & Carrot"})

	# Simulate worker reaching station and working
	worker.position = prep_east.position + Vector3(0.0, 0.0, 0.9)
	worker._process(0.1) # Triggers state switch to WORKING
	_assert(worker.current_state == PartTimeWorker.WorkerState.WORKING, "Worker transitions to WORKING state upon reaching station")

	# Tick worker task execution
	worker._process(1.2) # Interval is 1.1s -> triggers strike
	_assert(prep_east.strikes_completed >= 1, "Prep Assistant automated strike advanced prep progress on station")

	# Contract expiration -> walking out
	worker.contract_remaining_sec = 0.0
	worker._process(0.1)
	_assert(worker.current_state == PartTimeWorker.WorkerState.WALKING_OUT, "Worker transitions to WALKING_OUT when contract expires")

	# Cooldown check: hiring prep_assistant again before 45s cooldown is rejected
	var cd_hire: Dictionary = kitchen.request_hire_worker(2, "prep_assistant")
	_assert(not bool(cd_hire.get("ok", false)), "Hiring worker on active agency cooldown is rejected")

	# 5. Tier 3 (Business Operator): 2 simultaneous part-timer slots
	ProgressionManager.jiwon_tier = 3
	p2.character_tier = 3
	kitchen.active_workers.clear()
	kitchen.worker_cooldowns.clear()
	var hire1: Dictionary = kitchen.request_hire_worker(2, "prep_assistant")
	var hire2: Dictionary = kitchen.request_hire_worker(2, "dishwasher")
	_assert(bool(hire1.get("ok", false)) and bool(hire2.get("ok", false)), "Jiwon Tier 3 can hire 2 simultaneous part-timers")
	_assert(kitchen.active_workers.size() == 2, "Kitchen manages 2 simultaneous part-time workers at Tier 3")

	kitchen.queue_free()

func _test_campaign_levels_and_progression_unlocks() -> void:
	print("\n[Suite 12] 5 Handcrafted Campaign Levels & Milestone 3 Character Tier Unlocks")
	_assert(GameData.campaign_levels.size() == 5, "Exactly 5 handcrafted campaign levels registered")

	var lvl1: CampaignLevelSpec = GameData.campaign_levels[1]
	var lvl3: CampaignLevelSpec = GameData.campaign_levels[3]
	var lvl5: CampaignLevelSpec = GameData.campaign_levels[5]

	_assert(lvl1.allowed_recipes.size() == 2, "Level 1 starts with 2 starter recipes (First Customer)")
	_assert("tteokbokki" in lvl3.allowed_recipes, "Level 3 introduces Korean Tteokbokki (Night Market)")
	_assert(lvl5.allowed_recipes.size() == 4, "Level 5 combines all 4 authentic recipes (Grand Opening)")

	# Verify Character Tier Unlocks based on campaign progress:
	ProgressionManager.reset_progression_for_testing()
	_assert(ProgressionManager.linh_tier == 1 and ProgressionManager.jiwon_tier == 1, "Chefs start at Tier 1")

	# Complete Level 3 with 2 stars -> upgrades Linh & Jiwon to Tier 2!
	var l3_res: Dictionary = ProgressionManager.record_shift_results(3, {
		"total_score": 600,
		"stars": 2,
		"handoffs_completed": 4,
		"rush_rhythms_triggered": 1
	})
	_assert(bool(l3_res.get("tier_upgraded", false)), "Completing Level 3 triggers character tier upgrade")
	_assert(ProgressionManager.linh_tier == 2 and ProgressionManager.jiwon_tier == 2, "Linh & Jiwon upgraded to Tier 2 after Level 3")

	# Complete Level 5 with 2 stars -> upgrades Linh & Jiwon to Tier 3!
	var l5_res: Dictionary = ProgressionManager.record_shift_results(5, {
		"total_score": 900,
		"stars": 2,
		"handoffs_completed": 6,
		"rush_rhythms_triggered": 2
	})
	_assert(bool(l5_res.get("tier_upgraded", false)), "Completing Level 5 triggers Tier 3 upgrade")
	_assert(ProgressionManager.linh_tier == 3 and ProgressionManager.jiwon_tier == 3, "Linh & Jiwon upgraded to Tier 3 (Precision Engineer & Business Operator)")

func _test_endless_rush_mode() -> void:
	print("\n[Suite 13] Endless Rush Mode: Dynamic Difficulty Scaling & Personal Best Tracking")
	ProgressionManager.endless_best_score = 0
	ProgressionManager.endless_best_orders = 0

	ShiftManager.start_new_shift(9999.0, 1, ShiftManager.GameMode.ENDLESS_RUSH)
	_assert(ShiftManager.game_mode == ShiftManager.GameMode.ENDLESS_RUSH, "ShiftManager initialized in ENDLESS_RUSH mode")
	_assert(ShiftManager.dynamic_max_orders == 2, "Endless mode begins with 2 concurrent orders")

	# Tick 65 seconds -> dynamic scaling increases pressure
	ShiftManager.tick_shift(65.0)
	_assert(ShiftManager.dynamic_max_orders >= 4, "Endless mode dynamically scales concurrent order pressure over time")
	_assert(ShiftManager.order_spawn_interval < 12.0, "Endless mode dynamically accelerates order spawn tempo")

	# Simulate 3 failed orders -> triggers Endless failure condition
	ShiftManager.stats["orders_failed"] = 3
	ShiftManager.tick_shift(1.0)
	_assert(not ShiftManager.shift_active, "3 failed customer orders triggers Endless Mode shift end")

	# Record endless results and personal best
	var end_summary: Dictionary = ProgressionManager.record_endless_results(850, 7, 72.5)
	_assert(bool(end_summary.get("new_high_score", false)), "First endless run recorded as new high score")
	_assert(ProgressionManager.endless_best_score == 850, "Endless personal best score updated to 850")

	# Lower score run does not overwrite personal best
	var lower_summary: Dictionary = ProgressionManager.record_endless_results(500, 4, 45.0)
	_assert(not bool(lower_summary.get("new_high_score", false)), "Lower score run does not overwrite personal best")
	_assert(ProgressionManager.endless_best_score == 850, "Endless personal best preserved at 850")
