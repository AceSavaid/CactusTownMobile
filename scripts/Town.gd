extends Control

## Town — placeholder. This becomes the top-down isometric overworld:
## player movement, NPCs, repair requests, links to subsections and regions.

const HOUSE_SCENE := "res://scenes/House.tscn"


func _ready() -> void:
	%BackButton.pressed.connect(func() -> void: Router.goto_scene(HOUSE_SCENE))
	%DebugCoinsButton.pressed.connect(func() -> void:
		GameState.add_coins(10)
		Router.toast("+10 coins (debug)"))
