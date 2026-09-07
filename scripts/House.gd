extends Control

## The house — hub and main menu. Customise the plant, play mini-games,
## check plant stats, or head out to the town.

const TOWN_SCENE := "res://scenes/Town.tscn"

@onready var _plant_name: Label = %PlantName


func _ready() -> void:
	%CustomisePlantButton.pressed.connect(_coming_soon.bind("Plant customisation"))
	%MiniGamesButton.pressed.connect(_coming_soon.bind("Mini-games"))
	%PlantStatsButton.pressed.connect(_coming_soon.bind("Plant stats"))
	%GoToTownButton.pressed.connect(func() -> void: Router.goto_scene(TOWN_SCENE))

	GameState.plant_changed.connect(_refresh_plant)
	_refresh_plant()


func _refresh_plant() -> void:
	_plant_name.text = "Your plant: %s" % GameState.get_plant().get("name", "?")


func _coming_soon(feature: String) -> void:
	Router.toast("%s — coming soon" % feature)
