extends Node

## Dev-only screenshotter. Runs as a normal scene so autoloads are present.
## Usage:
##   godot --path . scenes/dev/Screenshot.tscn --resolution 1080x1920 -- <res://scene> <out.png> [frames]

func _ready() -> void:
	var args := OS.get_cmdline_user_args()
	var scene_path: String = args[0] if args.size() > 0 else "res://scenes/House.tscn"
	var out_path: String = args[1] if args.size() > 1 else "user://shot.png"
	var frames: int = int(args[2]) if args.size() > 2 else 20

	var scene: Node = (load(scene_path) as PackedScene).instantiate()
	add_child(scene)

	for _i in frames:
		await get_tree().process_frame

	if "demo_talk" in args:
		var npc := scene.get_node("World/Npc")
		var player := scene.get_node("World/Player")
		player.global_position = npc.global_position + Vector2(0.0, 70.0)
		for _i in 15:
			await get_tree().process_frame
		scene._on_talk_pressed()
		for _i in 5:
			await get_tree().process_frame

	var img := get_viewport().get_texture().get_image()
	var err := img.save_png(out_path)
	print("screenshot %s -> %s (%s) err=%d" % [scene_path, out_path, img.get_size(), err])
	get_tree().quit()
