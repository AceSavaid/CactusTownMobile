extends CanvasLayer

## Scene navigation with a fade transition, plus a lightweight toast.
## Autoloaded as `Router`. Layer sits above gameplay.

const FADE_TIME := 0.25

var _fade: ColorRect
var _toast: Label
var _busy := false


func _ready() -> void:
	layer = 128
	_fade = ColorRect.new()
	_fade.color = Color.BLACK
	_fade.set_anchors_preset(Control.PRESET_FULL_RECT)
	_fade.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_fade.modulate.a = 0.0
	add_child(_fade)

	_toast = Label.new()
	_toast.set_anchors_preset(Control.PRESET_CENTER_BOTTOM)
	_toast.offset_top = -220.0
	_toast.offset_bottom = -160.0
	_toast.offset_left = -320.0
	_toast.offset_right = 320.0
	_toast.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	_toast.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	_toast.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_toast.modulate.a = 0.0
	add_child(_toast)


func goto_scene(scene_path: String) -> void:
	if _busy:
		return
	_busy = true
	_fade.mouse_filter = Control.MOUSE_FILTER_STOP
	var tween := create_tween()
	tween.tween_property(_fade, "modulate:a", 1.0, FADE_TIME)
	await tween.finished

	var err := get_tree().change_scene_to_file(scene_path)
	if err != OK:
		push_error("Failed to load scene: %s (err %d)" % [scene_path, err])

	var out := create_tween()
	out.tween_property(_fade, "modulate:a", 0.0, FADE_TIME)
	await out.finished
	_fade.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_busy = false


func toast(message: String, duration := 1.6) -> void:
	_toast.text = message
	_toast.modulate.a = 1.0
	var tween := create_tween()
	tween.tween_interval(duration)
	tween.tween_property(_toast, "modulate:a", 0.0, 0.4)
