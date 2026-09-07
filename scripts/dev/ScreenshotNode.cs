using System;
using System.Linq;
using Godot;

namespace CactusTown;

/// <summary>
/// Dev-only screenshotter. Runs as a normal scene so autoloads are present.
/// Usage:
///   godot --path . scenes/dev/Screenshot.tscn --resolution 1920x1080 -- &lt;res://scene&gt; &lt;out.png&gt; [frames] [demo_talk]
/// </summary>
public partial class ScreenshotNode : Node
{
	public override async void _Ready()
	{
		var args = OS.GetCmdlineUserArgs();
		var scenePath = args.Length > 0 ? args[0] : "res://scenes/House.tscn";
		var outPath = args.Length > 1 ? args[1] : "user://shot.png";
		var frames = args.Length > 2 ? int.Parse(args[2]) : 20;

		var scene = GD.Load<PackedScene>(scenePath).Instantiate<Node>();
		AddChild(scene);

		for (var i = 0; i < frames; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		if (args.Contains("grant"))
		{
			GameState.Instance.AddMaterial(Materials.Wood, 5);
			GameState.Instance.AddMaterial(Materials.Stick, 5);
		}

		if (args.Contains("demo_talk"))
		{
			var npc = scene.GetNode<Npc>("World/Npc");
			var player = scene.GetNode<Node2D>("World/Player");
			player.GlobalPosition = npc.GlobalPosition + new Vector2(0f, 70f);
			for (var i = 0; i < 15; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			scene.GetNode<RequestPanel>("UI/RequestPanel").Open(npc);
			for (var i = 0; i < 5; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		if (args.Contains("demo_chop"))
		{
			var game = GD.Load<PackedScene>("res://scenes/minigames/TimingBarMiniGame.tscn").Instantiate<TimingBarMiniGame>();
			game.Setup("Chop the tree", 3, 2);
			scene.GetNode<CanvasLayer>("UI").AddChild(game);
			for (var i = 0; i < 8; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		var image = GetViewport().GetTexture().GetImage();
		var error = image.SavePng(outPath);
		GD.Print($"screenshot {scenePath} -> {outPath} ({image.GetSize()}) err={error}");
		GetTree().Quit();
	}
}
