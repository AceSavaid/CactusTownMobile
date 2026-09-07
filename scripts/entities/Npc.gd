class_name Npc
extends Area2D

## A townsperson with a repair request. Detects the player entering its radius
## and announces it so the town UI can offer a "Talk" action.

signal player_entered(npc: Npc)
signal player_exited(npc: Npc)

@export var npc_name := "Rosa"
## Id of the repairable object this request targets (persisted in GameState).
@export var object_id := "plaza_bench"
@export_multiline var greeting := "Morning! Lovely day for it."
@export_multiline var request_text := "The old plaza bench is falling apart. Fancy giving it a fix?"
@export_multiline var thanks_text := "You fixed it up beautifully. The whole plaza feels warmer."
@export var reward_coins := 25

@onready var _prompt: Node2D = $Prompt


func _ready() -> void:
	body_entered.connect(_on_body_entered)
	body_exited.connect(_on_body_exited)
	_prompt.hide()


func _on_body_entered(body: Node2D) -> void:
	if body is Player:
		_prompt.visible = not is_done()
		player_entered.emit(self)


func _on_body_exited(body: Node2D) -> void:
	if body is Player:
		_prompt.hide()
		player_exited.emit(self)


func is_done() -> bool:
	return GameState.is_object_fixed(object_id)


func complete_request() -> void:
	if is_done():
		return
	GameState.set_object_state(object_id, "fixed")
	GameState.add_coins(reward_coins)
	_prompt.hide()
