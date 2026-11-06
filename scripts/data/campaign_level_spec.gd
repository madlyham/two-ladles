class_name CampaignLevelSpec
extends RefCounted
## Handcrafted Campaign Level Specification for Two Ladles.

var level_id: int
var title: String
var subtitle: String
var setting_name: String
var shift_duration: float
var target_score_star1: int
var target_score_star2: int
var target_score_star3: int
var allowed_recipes: Array[String]
var max_concurrent_orders: int
var order_spawn_interval: float
var intro_objective: String

func _init(
	p_id: int,
	p_title: String,
	p_subtitle: String,
	p_setting: String,
	p_duration: float,
	p_star1: int,
	p_star2: int,
	p_star3: int,
	p_recipes: Array[String],
	p_concurrent: int,
	p_interval: float,
	p_intro: String
) -> void:
	level_id = p_id
	title = p_title
	subtitle = p_subtitle
	setting_name = p_setting
	shift_duration = p_duration
	target_score_star1 = p_star1
	target_score_star2 = p_star2
	target_score_star3 = p_star3
	allowed_recipes = p_recipes
	max_concurrent_orders = p_concurrent
	order_spawn_interval = p_interval
	intro_objective = p_intro

func evaluate_stars(final_score: int) -> int:
	if final_score >= target_score_star3:
		return 3
	elif final_score >= target_score_star2:
		return 2
	elif final_score >= target_score_star1:
		return 1
	return 0
