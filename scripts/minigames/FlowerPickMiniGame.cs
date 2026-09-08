using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CactusTown;

/// <summary>
/// Colour-match gathering, in the spirit of the Cave's tap-target game: a patch
/// of flowers appears, and a prompt names the colour to pick. Tap the right
/// colour to score; a wrong pick costs time. Land <c>Difficulty</c> good picks
/// before the timer ends. Used by the Flower Field.
/// </summary>
public partial class FlowerPickMiniGame : MiniGame
{
	private const int FlowerCount = 4;

	private static readonly (string Name, string File, Color Label)[] Kinds =
	{
		("red", "flowers_red", new Color(0.92f, 0.44f, 0.42f)),
		("yellow", "flowers_yellow", new Color(0.95f, 0.80f, 0.36f)),
		("blue", "flowers_blue", new Color(0.50f, 0.66f, 0.95f)),
	};

	[Export] public float TimeLimit = 12f;
	[Export] public float WrongPenalty = 1.2f;

	private readonly Button[] _flowers = new Button[FlowerCount];
	private readonly Texture2D[] _tex = new Texture2D[Kinds.Length];

	private int _picked;
	private int _wantKind;
	private float _timeLeft;
	private bool _resolving;

	private Label _title = null!;
	private Label _prompt = null!;
	private Label _progress = null!;
	private Label _timer = null!;
	private Control _area = null!;

	public override void _Ready()
	{
		base._Ready();
		_title = GetNode<Label>("%Title");
		_prompt = GetNode<Label>("%Prompt");
		_progress = GetNode<Label>("%Progress");
		_timer = GetNode<Label>("%Timer");
		_area = GetNode<Control>("%Area");

		for (var i = 0; i < Kinds.Length; i++)
			_tex[i] = GD.Load<Texture2D>($"res://assets/sprites/{Kinds[i].File}.svg");

		for (var i = 0; i < FlowerCount; i++)
		{
			var button = new Button
			{
				CustomMinimumSize = new Vector2(160, 160),
				Size = new Vector2(160, 160),
				ExpandIcon = true,
				Flat = true,
				IconAlignment = HorizontalAlignment.Center,
			};
			var index = i;
			button.Pressed += () => OnPick(index);
			_area.AddChild(button);
			_flowers[i] = button;
		}

		_timeLeft = TimeLimit;
		_title.Text = TitleText;
		GetNode<Button>("%GiveUpButton").Pressed += () => Resolve(false);
		UpdateProgress();
		Callable.From(Deal).CallDeferred();
	}

	public override void _Process(double delta)
	{
		if (_resolving)
			return;
		_timeLeft -= (float)delta;
		_timer.Text = $"{Mathf.Max(0f, _timeLeft):0.0}s";
		if (_timeLeft <= 0f)
			Resolve(_picked >= Difficulty);
	}

	/// <summary>Lay out a fresh patch: random colours (at least one match) at spaced spots.</summary>
	private void Deal()
	{
		_wantKind = (int)(GD.Randi() % (uint)Kinds.Length);

		var kinds = new int[FlowerCount];
		for (var i = 0; i < FlowerCount; i++)
			kinds[i] = (int)(GD.Randi() % (uint)Kinds.Length);
		kinds[GD.RandRange(0, FlowerCount - 1)] = _wantKind;

		var spots = SpacedSpots();
		for (var i = 0; i < FlowerCount; i++)
		{
			_flowers[i].Icon = _tex[kinds[i]];
			_flowers[i].SetMeta("kind", kinds[i]);
			_flowers[i].Position = spots[i];
			_flowers[i].Modulate = Colors.White;
			_flowers[i].Disabled = false;
		}

		var k = Kinds[_wantKind];
		_prompt.Text = $"Pick the {k.Name} flower";
		_prompt.AddThemeColorOverride("font_color", k.Label);
	}

	private List<Vector2> SpacedSpots()
	{
		var free = _area.Size - new Vector2(160, 160);
		var spots = new List<Vector2>();
		for (var i = 0; i < FlowerCount; i++)
		{
			Vector2 candidate = default;
			for (var attempt = 0; attempt < 24; attempt++)
			{
				candidate = new Vector2(
					(float)GD.RandRange(0.0, Mathf.Max(1f, free.X)),
					(float)GD.RandRange(0.0, Mathf.Max(1f, free.Y)));
				if (spots.All(s => s.DistanceTo(candidate) > 190f))
					break;
			}
			spots.Add(candidate);
		}
		return spots;
	}

	private void OnPick(int index)
	{
		if (_resolving)
			return;

		if (_flowers[index].GetMeta("kind").AsInt32() == _wantKind)
		{
			PlayActionSound();
			_picked++;
			UpdateProgress();
			if (_picked >= Difficulty)
				Resolve(true);
			else
				Deal();
		}
		else
		{
			Audio.Instance?.PlaySfx("cancel");
			_timeLeft = Mathf.Max(0.1f, _timeLeft - WrongPenalty);
			FlashWrong(_flowers[index]);
		}
	}

	private async void FlashWrong(Button flower)
	{
		flower.Modulate = new Color(1.5f, 0.5f, 0.5f);
		await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
		if (IsInstanceValid(flower))
			flower.Modulate = Colors.White;
	}

	private void UpdateProgress() => _progress.Text = $"Picked:  {_picked} / {Difficulty}";

	private void Resolve(bool success)
	{
		_resolving = true;
		Finish(success || _picked > 0, success ? Reward : PartialReward(_picked / (float)Difficulty));
	}
}
