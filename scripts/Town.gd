extends Node2D

## Town overworld: free-roam with the virtual joystick, walk up to townsfolk
## to talk and take on repair requests. Subsections and regions hook in here later.

const HOUSE_SCENE := "res://scenes/House.tscn"

var _active_npc: Npc = null

@onready var _player: Player = %Player
@onready var _joystick: VirtualJoystick = %VirtualJoystick
@onready var _talk_button: Button = %TalkButton
@onready var _request_panel: Control = %RequestPanel
@onready var _world: Node2D = %World


func _ready() -> void:
	_joystick.direction_changed.connect(_player.set_move_direction)
	_talk_button.pressed.connect(_on_talk_pressed)
	%BackButton.pressed.connect(func() -> void: Router.goto_scene(HOUSE_SCENE))
	_request_panel.closed.connect(_on_request_panel_closed)

	for child in _world.get_children():
		if child is Npc:
			child.player_entered.connect(_on_npc_entered)
			child.player_exited.connect(_on_npc_exited)

	_refresh_talk_button()


func _on_npc_entered(npc: Npc) -> void:
	_active_npc = npc
	_refresh_talk_button()


func _on_npc_exited(npc: Npc) -> void:
	if npc == _active_npc:
		_active_npc = null
	_refresh_talk_button()


func _on_talk_pressed() -> void:
	if _active_npc != null:
		_request_panel.open(_active_npc)
		_refresh_talk_button()


func _on_request_panel_closed() -> void:
	_player.set_move_direction(Vector2.ZERO)
	_refresh_talk_button()


func _refresh_talk_button() -> void:
	var show_it := _active_npc != null and not _request_panel.visible
	_talk_button.visible = show_it
	if show_it:
		_talk_button.text = "Talk to %s" % _active_npc.npc_name
