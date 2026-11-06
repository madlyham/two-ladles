extends SceneTree
## Entrypoint wrapper so both `godot --headless -s res://tests/test_runner.gd`
## and `godot --headless -- --run-tests` execute the full automated test suite.

func _initialize() -> void:
	change_scene_to_file("res://scenes/main.tscn")
	var suite_script: Script = load("res://tests/test_suite_node.gd")
	var suite_node: Node = suite_script.new()
	root.add_child(suite_node)
