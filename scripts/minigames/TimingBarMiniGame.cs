using Godot;

namespace CactusTown;

/// <summary>
/// Timing-bar gathering mini-game: a marker sweeps a track; tap CHOP when it is
/// over the moving target zone. Land <c>HitsNeeded</c> good hits to succeed.
/// No fail state — giving up early pays out proportionally. Used by the Forest.
/// </summary>
public partial class TimingBarMiniGame : MiniGame
{
	[Export] public float SweepSpeed = 1.15f;
	[Export] public float ZoneWidth = 130f;

	private const float TrackWidth = 900f;

	private string _titleText = "Gather";
	private int _hitsNeeded = 3;
	private int _reward = 2;

	private int _hits;
	private float _t;
	private float _zoneCentre;
	private bool _resolving;

	private Label _title = null!;
	private Label _progress = null!;
	private ColorRect _zone = null!;
	private ColorRect _marker = null!;
	private ColorRect _flash = null!;

	/// <summary>Called by the caller right after instancing, before AddChild.</summary>
	public void Setup(string title, int hitsNeeded, int reward)
	{
		_titleText = title;
		_hitsNeeded = Mathf.Max(1, hitsNeeded);
		_reward = Mathf.Max(1, reward);
	}

	public override void _Ready()
	{
		base._Ready();
		_title = GetNode<Label>("%Title");
		_progress = GetNode<Label>("%Progress");
		_zone = GetNode<ColorRect>("%Zone");
		_marker = GetNode<ColorRect>("%Marker");
		_flash = GetNode<ColorRect>("%Flash");

		GetNode<Button>("%ChopButton").Pressed += OnChop;
		GetNode<Button>("%GiveUpButton").Pressed += () => Finish(_hits > 0, PartialReward());

		_title.Text = _titleText;
		_zone.Size = new Vector2(ZoneWidth, _zone.Size.Y);
		_flash.Modulate = new Color(1, 1, 1, 0);
		MoveZone();
		UpdateProgress();
	}

	public override void _Process(double delta)
	{
		if (_resolving)
			return;
		_t += (float)delta * SweepSpeed;
		var x = Mathf.PingPong(_t, 1f) * TrackWidth;
		_marker.Position = new Vector2(x - _marker.Size.X * 0.5f, _marker.Position.Y);
	}

	private void OnChop()
	{
		if (_resolving)
			return;
		var markerCentre = _marker.Position.X + _marker.Size.X * 0.5f;
		var hit = Mathf.Abs(markerCentre - _zoneCentre) <= ZoneWidth * 0.5f;
		FlashFeedback(hit);

		if (hit)
		{
			_hits++;
			UpdateProgress();
			if (_hits >= _hitsNeeded)
			{
				_resolving = true;
				Finish(true, _reward);
				return;
			}
			MoveZone();
		}
	}

	private void MoveZone()
	{
		_zoneCentre = (float)GD.RandRange(TrackWidth * 0.15, TrackWidth * 0.85);
		_zone.Position = new Vector2(_zoneCentre - ZoneWidth * 0.5f, _zone.Position.Y);
	}

	private void UpdateProgress() => _progress.Text = $"Chops:  {_hits} / {_hitsNeeded}";

	private int PartialReward() => Mathf.CeilToInt(_reward * (_hits / (float)_hitsNeeded));

	private void FlashFeedback(bool good)
	{
		_flash.Color = good ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.9f, 0.4f, 0.4f);
		_flash.Modulate = new Color(1, 1, 1, 0.5f);
		var tween = CreateTween();
		tween.TweenProperty(_flash, "modulate:a", 0f, 0.25f);
	}
}
