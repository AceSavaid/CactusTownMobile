using System.Linq;
using Godot;

namespace CactusTown;

/// <summary>
/// Dev-only screenshotter. Runs as a normal scene so autoloads are present.
/// Usage:
///   godot --path . scenes/dev/Screenshot.tscn --resolution 1920x1080 -- &lt;res://scene&gt; &lt;out.png&gt; [frames] [flags...]
/// Flags: grant  complete  demo_repair  demo_customize  mg=&lt;MiniGameScene&gt;
///        card=&lt;n&gt; (open a gallery card's difficulty panel)  walk=&lt;x&gt;,&lt;y&gt; (drive %Player each frame)
/// </summary>
public partial class ScreenshotNode : Node
{
	public override async void _Ready()
	{
		var args = OS.GetCmdlineUserArgs();

		if (args.Length > 0 && args[0] == "bake_audio")
		{
			var dir = args.Length > 1 ? args[1] : "user://audio_preview";
			DirAccess.MakeDirRecursiveAbsolute(dir);
			foreach (var (name, stream) in SfxBank.Build())
				if (stream is AudioStreamWav wav)
					wav.SaveToWav($"{dir}/sfx_{name}.wav");
			foreach (var track in new[] { "home", "town", "region", "arcade" })
				if (MusicBank.Track(track) is AudioStreamWav music)
					music.SaveToWav($"{dir}/music_{track}.wav");
			GD.Print($"baked audio to {dir}");
			GetTree().Quit();
			return;
		}

		if (args.Length > 0 && args[0] == "store_assets")
		{
			DirAccess.MakeDirRecursiveAbsolute("res://store");
			DirAccess.MakeDirRecursiveAbsolute("res://assets/icons");
			var jobs = new (string Src, string Out, float Scale)[]
			{
				("res://assets/store/src/icon.svg", "res://store/icon-512.png", 1f),
				("res://assets/store/src/feature_graphic.svg", "res://store/feature-graphic-1024x500.png", 1f),
				("res://assets/store/src/icon.svg", "res://assets/icons/launcher-192.png", 192f / 512f),
				("res://assets/store/src/icon_foreground.svg", "res://assets/icons/adaptive-foreground-432.png", 1f),
				("res://assets/store/src/icon_background.svg", "res://assets/icons/adaptive-background-432.png", 1f),
			};
			foreach (var (src, dest, scale) in jobs)
			{
				var img = new Image();
				var err = img.LoadSvgFromString(FileAccess.GetFileAsString(src), scale);
				if (err != Error.Ok)
				{
					GD.PushError($"{src}: {err}");
					continue;
				}
				GD.Print($"{dest}  {img.GetSize()}  err={img.SavePng(dest)}");
			}
			GetTree().Quit();
			return;
		}

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

		if (args.Contains("fixall"))
			foreach (var section in TownSections.All)
			{
				foreach (var objectId in section.ObjectIds)
				{
					GameState.Instance.SetObjectState(objectId, "fixed");
					GameState.Instance.BuyVariant(objectId, 1, 0);
				}
				GameState.Instance.SetSectionCompletedOnce(section.Id);
			}

		var scene = GD.Load<PackedScene>(scenePath).Instantiate<Node>();
		var objects = scene.FindChildren("*", recursive: true).OfType<RepairableObject>().ToList();

		if ((args.Contains("complete") || args.Contains("demo_customize")) && objects.Count > 0)
		{
			foreach (var o in objects)
				GameState.Instance.SetObjectState(o.ObjectId, "fixed");
			GameState.Instance.SetSectionCompletedOnce(objects[0].ObjectId.Split('_')[0] == "square" ? "main_square" : "garden");
		}

		AddChild(scene);

		Vector2 walk = Vector2.Zero;
		foreach (var arg in args)
		{
			if (arg.StartsWith("walk="))
			{
				var p = arg["walk=".Length..].Split(',');
				walk = new Vector2(float.Parse(p[0]), float.Parse(p[1]));
			}
			if (arg.StartsWith("tp=") && scene.HasNode("%Player"))
			{
				var p = arg["tp=".Length..].Split(',');
				scene.GetNode<Node2D>("%Player").GlobalPosition = new Vector2(float.Parse(p[0]), float.Parse(p[1]));
			}
		}

		for (var i = 0; i < frames; i++)
		{
			if (walk != Vector2.Zero && scene.HasNode("%Player"))
				scene.GetNode<Player>("%Player").SetMoveDirection(walk);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		if (walk != Vector2.Zero && scene.HasNode("%Player"))
			GD.Print($"player_pos={scene.GetNode<Node2D>("%Player").GlobalPosition}");

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
			var mgTitle = args.FirstOrDefault(a => a.StartsWith("mgtitle="))?["mgtitle=".Length..] ?? "Gather the thing";
			game.Configure(mgTitle, 3, 4);
			scene.GetNode<CanvasLayer>("UI").AddChild(game);
			for (var i = 0; i < 10; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		if (args.Contains("dressup"))
		{
			foreach (var id in new[] { "pot_slate", "plant_aloe", "acc_hat" })
			{
				GameState.Instance.BuyPlantItem(id);
				GameState.Instance.EquipPlantItem(id);
			}
			for (var i = 0; i < 4; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		foreach (var arg in args)
		{
			if (!arg.StartsWith("tab="))
				continue;
			var tab = arg["tab=".Length..] switch
			{
				"plant" => "%PlantTab",
				"accessory" => "%AccessoryTab",
				_ => "%PotTab",
			};
			scene.GetNode<Button>(tab).EmitSignal(BaseButton.SignalName.Pressed);
			for (var i = 0; i < 4; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		if (args.Contains("audio_settings") || args.Contains("settings"))
		{
			scene.GetNode<SettingsMenu>("%SettingsMenu").Open();
			for (var i = 0; i < 4; i++)
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		if (args.Contains("seed_tasks"))
		{
			GameState.Instance.RecordTask("square_fountain", "Plaza Fountain",
				new Godot.Collections.Dictionary { { "stone", 4 }, { "water", 3 } });
			GameState.Instance.RecordTask("garden_pond", "Garden Pond",
				new Godot.Collections.Dictionary { { "water", 4 }, { "stone", 3 } });
			GameState.Instance.RecordTask("square_bench", "Old Bench",
				new Godot.Collections.Dictionary { { "wood", 4 }, { "stick", 2 } });
			scene.GetNode<TasksPanel>("%TasksPanel").Open();
			for (var i = 0; i < 4; i++)
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
			if (!arg.StartsWith("card="))
				continue;
			var grid = scene.GetNode("%Grid");
			var n = int.Parse(arg["card=".Length..]);
			if (n < grid.GetChildCount())
				((Button)grid.GetChild(n)).EmitSignal(BaseButton.SignalName.Pressed);
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

			if (args.Contains("play"))
			{
				var boardName = g is Minesweeper or MatchPairs ? "%Grid" : "%Board";
				var board = g.GetNode(boardName);
				var moves = g switch
				{
					TicTacToe => new[] { 0, 4, 8 },
					MatchPairs => new[] { 0, 1, 4 },
					Minesweeper => new[] { board.GetChildCount() / 2 + 2 },
					_ => System.Array.Empty<int>(),
				};
				foreach (var move in moves)
				{
					if (move < board.GetChildCount())
						((Button)board.GetChild(move)).EmitSignal(BaseButton.SignalName.Pressed);
					for (var i = 0; i < 60; i++)
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
