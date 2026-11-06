class_name CharacterProgressionData
extends RefCounted
## Data structures for Linh's Execution Specialist Tiers,
## Jiwon's Multitasking / Part-Time Worker Hiring System, and Worker specs.

class LinhTierSpec extends RefCounted:
	var tier: int
	var title: String
	var description: String
	var task_speed_multiplier: float # < 1.0 means faster execution cadence
	var arm_style: String # "none", "rustic_mechanical", "precision_white_blue"
	var arm_color_primary: Color
	var arm_color_glow: Color

	func _init(
		p_tier: int,
		p_title: String,
		p_desc: String,
		p_speed_mult: float,
		p_style: String,
		p_col: Color,
		p_glow: Color = Color.TRANSPARENT
	) -> void:
		tier = p_tier
		title = p_title
		description = p_desc
		task_speed_multiplier = p_speed_mult
		arm_style = p_style
		arm_color_primary = p_col
		arm_color_glow = p_glow

class JiwonTierSpec extends RefCounted:
	var tier: int
	var title: String
	var description: String
	var part_timer_slots: int
	var outfit_name: String
	var outfit_color: Color

	func _init(
		p_tier: int,
		p_title: String,
		p_desc: String,
		p_slots: int,
		p_outfit: String,
		p_col: Color
	) -> void:
		tier = p_tier
		title = p_title
		description = p_desc
		part_timer_slots = p_slots
		outfit_name = p_outfit
		outfit_color = p_col

class WorkerSpec extends RefCounted:
	var role_id: String
	var display_name: String
	var description: String
	var hire_cost: int
	var contract_duration: float
	var cooldown_duration: float
	var target_station_type: String # "prep_station" or "washing_station"
	var task_interval: float
	var uniform_color: Color

	func _init(
		p_id: String,
		p_name: String,
		p_desc: String,
		p_cost: int,
		p_duration: float,
		p_cooldown: float,
		p_station_type: String,
		p_interval: float,
		p_col: Color
	) -> void:
		role_id = p_id
		display_name = p_name
		description = p_desc
		hire_cost = p_cost
		contract_duration = p_duration
		cooldown_duration = p_cooldown
		target_station_type = p_station_type
		task_interval = p_interval
		uniform_color = p_col
