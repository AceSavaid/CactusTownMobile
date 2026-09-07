class_name VirtualJoystick
extends Control

## On-screen thumbstick for touch movement. Dynamic: the base jumps to wherever
## the player first presses inside the activation zone, then tracks the drag.
## Emits a normalised direction (length 0..1).

signal direction_changed(direction: Vector2)

## Activation zone as viewport ratios (x, y, w, h). Default: lower-left quadrant.
@export var activation_zone := Rect2(0.0, 0.35, 0.5, 0.65)
@export var handle_range := 96.0

var _touch_index := -1
var _home_position := Vector2.ZERO
var _direction := Vector2.ZERO

@onready var _base: TextureRect = $Base
@onready var _handle: TextureRect = $Base/Handle


func _ready() -> void:
	_home_position = _base.position
	_centre_handle()


func _input(event: InputEvent) -> void:
	if event is InputEventScreenTouch:
		if event.pressed and _touch_index == -1 and _in_activation_zone(event.position):
			_touch_index = event.index
			_base.global_position = event.position - _base.size * 0.5
			_drag_to(event.position)
		elif not event.pressed and event.index == _touch_index:
			_release()
	elif event is InputEventScreenDrag and event.index == _touch_index:
		_drag_to(event.position)


func _in_activation_zone(pos: Vector2) -> bool:
	var vp := get_viewport_rect().size
	var zone := Rect2(activation_zone.position * vp, activation_zone.size * vp)
	return zone.has_point(pos)


func _drag_to(pos: Vector2) -> void:
	var offset := (pos - _base.global_position - _base.size * 0.5).limit_length(handle_range)
	_handle.position = _base.size * 0.5 + offset - _handle.size * 0.5
	_direction = offset / handle_range
	direction_changed.emit(_direction)


func _release() -> void:
	_touch_index = -1
	_base.position = _home_position
	_centre_handle()
	_direction = Vector2.ZERO
	direction_changed.emit(_direction)


func _centre_handle() -> void:
	_handle.position = (_base.size - _handle.size) * 0.5
