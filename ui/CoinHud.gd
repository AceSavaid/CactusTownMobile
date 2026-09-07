extends PanelContainer

## Persistent coin counter. Drop into any screen; it self-syncs with GameState.

@onready var _label: Label = $Margin/Label


func _ready() -> void:
	GameState.coins_changed.connect(_on_coins_changed)
	_on_coins_changed(GameState.get_coins())


func _on_coins_changed(total: int) -> void:
	_label.text = "%d coins" % total
