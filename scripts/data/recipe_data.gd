class_name RecipeData
extends RefCounted
## Core typed data structures for Ingredients, Appliances, Recipes, Specializations, and Kitchen Items.

enum CookingState {
	RAW = 0,
	COOKING = 1,
	OPTIMAL = 2,
	OVERCOOKED = 3,
	BURNT = 4,
}

enum CadenceGrade {
	PERFECT = 0,
	GOOD = 1,
	MISS = 2,
}

enum QualityTier {
	BURNT_WASTE = 0,
	OVERCOOKED = 1,
	STANDARD = 2,
	OPTIMAL = 3,
}

class IngredientSpec extends RefCounted:
	var id: String
	var display_name: String
	var origin_zone: String # "linh_west" or "jiwon_east" or "shared"
	var partner_target_zone: String # "jiwon_east" or "linh_west" or "shared"
	var raw_id: String
	var prepared_id: String
	var cooked_id: String
	var required_prep_strikes: int = 4
	var required_appliance: String = ""
	var color: Color = Color.WHITE

	func _init(
		p_id: String,
		p_display_name: String,
		p_origin_zone: String,
		p_partner_target_zone: String,
		p_raw_id: String = "",
		p_prepared_id: String = "",
		p_cooked_id: String = "",
		p_required_appliance: String = "",
		p_color: Color = Color.WHITE
	) -> void:
		id = p_id
		display_name = p_display_name
		origin_zone = p_origin_zone
		partner_target_zone = p_partner_target_zone
		raw_id = p_raw_id
		prepared_id = p_prepared_id
		cooked_id = p_cooked_id
		required_appliance = p_required_appliance
		color = p_color

class ApplianceSpec extends RefCounted:
	var id: String
	var display_name: String
	var zone: String # "linh_west" or "jiwon_east"
	var cook_time_to_optimal: float
	var optimal_window_duration: float
	var overcooked_window_duration: float
	var accepted_inputs: Array[String]
	var output_item_id: String
	var continuous_stockpot: bool = false

	func _init(
		p_id: String,
		p_display_name: String,
		p_zone: String,
		p_cook_time: float,
		p_optimal_window: float,
		p_overcooked_window: float,
		p_accepted_inputs: Array[String],
		p_output_item_id: String,
		p_continuous_stockpot: bool = false
	) -> void:
		id = p_id
		display_name = p_display_name
		zone = p_zone
		cook_time_to_optimal = p_cook_time
		optimal_window_duration = p_optimal_window
		overcooked_window_duration = p_overcooked_window
		accepted_inputs = p_accepted_inputs
		output_item_id = p_output_item_id
		continuous_stockpot = p_continuous_stockpot

class RecipeSpec extends RefCounted:
	var id: String
	var display_name: String
	var culinary_origin: String # "Traditional Vietnamese", "Traditional Korean", "Original Vietnam-Korea Fusion Specialty"
	var is_fusion: bool
	var vessel_type: String # "bowl" or "plate"
	var required_components: Array[String] # All components needed in final vessel
	var ordered_final_step: String # e.g., "broth_hot" for Pho Bo (must be added after noodles & beef)
	var base_score: int
	var patience_seconds: float
	var difficulty: int
	var workflow_summary: String

	func _init(
		p_id: String,
		p_display_name: String,
		p_culinary_origin: String,
		p_is_fusion: bool,
		p_vessel_type: String,
		p_required_components: Array[String],
		p_ordered_final_step: String,
		p_base_score: int,
		p_patience_seconds: float,
		p_difficulty: int,
		p_workflow_summary: String
	) -> void:
		id = p_id
		display_name = p_display_name
		culinary_origin = p_culinary_origin
		is_fusion = p_is_fusion
		vessel_type = p_vessel_type
		required_components = p_required_components
		ordered_final_step = p_ordered_final_step
		base_score = p_base_score
		patience_seconds = p_patience_seconds
		difficulty = p_difficulty
		workflow_summary = p_workflow_summary

class SpecializationSpec extends RefCounted:
	var id: String
	var display_name: String
	var description: String
	var prep_window_multiplier: float = 1.0
	var prep_perfect_bonus_score: int = 0
	var optimal_cook_window_multiplier: float = 1.0
	var move_speed_multiplier: float = 1.0
	var fusion_score_multiplier: float = 1.0

	func _init(
		p_id: String,
		p_display_name: String,
		p_description: String,
		p_prep_mult: float = 1.0,
		p_prep_bonus: int = 0,
		p_cook_mult: float = 1.0,
		p_move_mult: float = 1.0,
		p_fusion_mult: float = 1.0
	) -> void:
		id = p_id
		display_name = p_display_name
		description = p_description
		prep_window_multiplier = p_prep_mult
		prep_perfect_bonus_score = p_prep_bonus
		optimal_cook_window_multiplier = p_cook_mult
		move_speed_multiplier = p_move_mult
		fusion_score_multiplier = p_fusion_mult
