using System.Linq;
using Godot;

namespace CactusTown;

/// <summary>
/// Dev-only screenshotter. Runs as a normal scene so autoloads are present.
/// Usage:
///   godot --path . scenes/dev/Screenshot.tscn --resolution 1920x1080 -- &lt;res://scene&gt; &lt;out.png&gt; [frames] [flags...]
/// Flags: grant  complete  demo_repair  demo_customize  mg=&lt;MiniGameScene&gt;
/// </summary>
public partial class ScreenshotNode : Node
{
	public override async void _Ready()
	{
		var args = OS.GetCmdlineUserArgs();
		var scenePath = args.Length > 0 ? args[0] : "res://scenes/House.tscn";
		var outPath = args.Length > 1 ? args[1] : "user://shot.png";
		var frames = args.Length > 2 ? int.Parse(args[2]) : 20;

		if (args.Contains("grant"))
		{
			GameState.Instance.AddCoins(400);
			foreach (var m in new[] { Materials.Wood, Materials.Stick, Materials.Stone, Materials.Water, Materials.FlowerRed, Materials.FlowerYellow, Materials.IronOre })
				GameState.Instance.AddMaterial(m, 20);
		}

		foreach (var arg in args)
			if (arg.StartsWith("unlock="))
				GameState.Instance.SetSectionCompletedOnce(arg["unlock=".Length..]);

		var scene = GD.Load<PackedScene>(scenePath).Instantiate<Node>();
		var objects = scene.FindChildren("*", recursive: true).OfType<RepairableObject>().ToList();

		if ((args.Contains("complete") || args.Contains("demo_customize")) && objects.Count > 0)
		{
			foreach (var o in objects)
				GameState.Instance.SetObjectState(o.ObjectId, "fixed");
			GameState.Instance.SetSectionCompletedOnce(objects[0].ObjectId.Split('_')[0] == "square" ? "main_square" : "garden");
		}

		AddChild(scene);
		for (var i = 0; i < frames; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		if ((args.Contains("demo_repair") || args.Contains("demo_customize")) && objects.Count > 0)
		{
			var obj = objects[0];
			var player = scene.GetNode<Node2D>("%Player");
			player.GlobalPosition = obj.GlobalPosition + new Vector2(0f, 95f);
			for (var i = 0; i < 15; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (args.Contains("demo_customize"))
				scene.GetNode<CustomizePanel>("%CustomizePanel").Open(obj);
			else
				scene.GetNode<RepairPanel>("%RepairPanel").Open(obj);
			for (var i = 0; i < 6; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		foreach (var arg in args)
		{
			if (!arg.StartsWith("mg="))
				continue;
			var game = GD.Load<PackedScene>($"res://scenes/minigames/{arg[3..]}.tscn").Instantiate<MiniGame>();
			game.Configure("Gather the thing", 3, 4);
			scene.GetNode<CanvasLayer>("UI").AddChild(game);
			for (var i = 0; i < 10; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		if (args.Contains("showdiff"))
		{
			scene.GetNode<Label>("%DifficultyTitle").Text = "Tic-Tac-Toe";
			scene.GetNode<Control>("%DifficultyPanel").Show();
			for (var i = 0; i < 4; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		foreach (var arg in args)
		{
			if (!arg.StartsWith("arcade="))
				continue;
			var diff = 2;
			foreach (var a in args)
				if (a.StartsWith("diff="))
					diff = int.Parse(a["diff=".Length..]);
			var g = GD.Load<PackedScene>($"res://scenes/arcade/{arg["arcade=".Length..]}.tscn").Instantiate<ArcadeGame>();
			g.Configure(diff);
			scene.GetNode<Control>("%GameHost").AddChild(g);
			for (var i = 0; i < 12; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

			if (args.Contains("play") && g is TicTacToe)
			{
				var board = g.GetNode("%Board");
				foreach (var move in new[] { 0, 4, 8 })
				{
					((Button)board.GetChild(move)).EmitSignal(BaseButton.SignalName.Pressed);
					for (var i = 0; i < 40; i++)
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				}
			}
		}

		var image = GetViewport().GetTexture().GetImage();
		var error = image.SavePng(outPath);
		GD.Print($"screenshot {scenePath} -> {outPath} ({image.GetSize()}) err={error}");
		GetTree().Quit();
	}
}
