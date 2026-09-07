extends Node2D

## Entry point for Cactus Town Mobile.
## Replace the placeholder label with real gameplay as scenes come online.


func _ready() -> void:
	print("Cactus Town Mobile — booted at %d x %d" % [
		get_viewport_rect().size.x,
		get_viewport_rect().size.y,
	])
