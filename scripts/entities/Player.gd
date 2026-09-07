class_name Player
extends CharacterBody2D

## Free-moving overworld character. Movement direction is fed in from outside
## (the virtual joystick), so this stays input-source agnostic.

@export var max_speed := 430.0
@export var acceleration := 2800.0
@export var friction := 3200.0

var move_direction := Vector2.ZERO

@onready var _sprite: Sprite2D = $Sprite


func _physics_process(delta: float) -> void:
	var target := move_direction.limit_length(1.0) * max_speed
	if target.length() > 1.0:
		velocity = velocity.move_toward(target, acceleration * delta)
		if target.x < -5.0:
			_sprite.flip_h = true
		elif target.x > 5.0:
			_sprite.flip_h = false
	else:
		velocity = velocity.move_toward(Vector2.ZERO, friction * delta)
	move_and_slide()


func set_move_direction(direction: Vector2) -> void:
	move_direction = direction
