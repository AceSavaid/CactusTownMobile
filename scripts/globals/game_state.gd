extends Node

## Global game state: wallet, plant, materials, town progress.
## Persists to a single JSON save slot. Autoloaded as `GameState`.

const SAVE_PATH := "user://cactus_town_save.json"
const SAVE_VERSION := 1

signal coins_changed(total: int)
signal materials_changed
signal plant_changed

var data: Dictionary = {}


func _ready() -> void:
	load_game()


# --- Wallet ------------------------------------------------------------------

func get_coins() -> int:
	return int(data.get("coins", 0))


func add_coins(amount: int) -> void:
	data["coins"] = get_coins() + amount
	coins_changed.emit(get_coins())
	save_game()


func spend_coins(amount: int) -> bool:
	if get_coins() < amount:
		return false
	data["coins"] = get_coins() - amount
	coins_changed.emit(get_coins())
	save_game()
	return true


# --- Materials -------------------------------------------------------------

func get_material(material_id: String) -> int:
	return int(data.get("materials", {}).get(material_id, 0))


func add_material(material_id: String, amount: int) -> void:
	var materials: Dictionary = data.get("materials", {})
	materials[material_id] = int(materials.get(material_id, 0)) + amount
	data["materials"] = materials
	materials_changed.emit()
	save_game()


# --- Plant ---------------------------------------------------------------

func get_plant() -> Dictionary:
	return data.get("plant", {})


func set_plant_customisation(slot: String, variant: String) -> void:
	var plant: Dictionary = data.get("plant", {})
	var custom: Dictionary = plant.get("customisation", {})
	custom[slot] = variant
	plant["customisation"] = custom
	data["plant"] = plant
	plant_changed.emit()
	save_game()


# --- Persistence -------------------------------------------------------------

func new_game() -> void:
	data = _default_data()
	save_game()


func load_game() -> void:
	if not FileAccess.file_exists(SAVE_PATH):
		new_game()
		return
	var file := FileAccess.open(SAVE_PATH, FileAccess.READ)
	if file == null:
		push_warning("Save unreadable, starting fresh.")
		new_game()
		return
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if typeof(parsed) != TYPE_DICTIONARY:
		push_warning("Save corrupt, starting fresh.")
		new_game()
		return
	data = _migrate(parsed)


func save_game() -> void:
	var file := FileAccess.open(SAVE_PATH, FileAccess.WRITE)
	if file == null:
		push_error("Could not open save file for writing: %s" % SAVE_PATH)
		return
	file.store_string(JSON.stringify(data, "\t"))


func _migrate(loaded: Dictionary) -> Dictionary:
	# Fill any keys added since the save was written.
	var base := _default_data()
	base.merge(loaded, true)
	base["version"] = SAVE_VERSION
	return base


func _default_data() -> Dictionary:
	return {
		"version": SAVE_VERSION,
		"coins": 0,
		"plant": {
			"name": "Spike",
			"customisation": {"pot": "clay", "species": "cactus", "hat": "none"},
		},
		"materials": {},
		"owned_customisations": [],
		"areas": {},
		"objects": {},
		"regions": {},
	}
