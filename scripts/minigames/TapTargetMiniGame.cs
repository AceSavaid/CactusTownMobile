using Godot;

namespace CactusTown;

/// <summary>
/// Aim gathering: a spot appears on the rock face; tap it, it jumps elsewhere.
/// Land <c>Difficulty</c> hits before the timer ends. Used by the Cave.
/// </summary>
public partial class TapTargetMiniGame : MiniGame
{
	[Export] public float TimeLimit = 9f;

	private int _hits;
	private float _timeLeft;
	private bool _resolving;

	private Label _title = null!;
	private Label _progress = null!;
	private Label _timer = null!;
	private Control _area = null!;
	private Button _target = null!;

	public override void _Ready()
	{
		base._Ready();
		_title = GetNode<Label>("%Title");
		_progress = GetNode<Label>("%Progress");
		_timer = GetNode<Label>("%Timer");
		_area = GetNode<Control>("%Area");
		_target = GetNode<Button>("%Target");

		_timeLeft = TimeLimit;
		_target.Pressed += OnHit;
		GetNode<Button>("%GiveUpButton").Pressed += () => Resolve(false);

		_title.Text = TitleText;
		UpdateProgress();
		Callable.From(MoveTarget).CallDeferred();
	}

	public override void _Process(double delta)
	{
		if (_resolving)
			return;
		_timeLeft -= (float)delta;
		_timer.Text = $"{Mathf.Max(0f, _timeLeft):0.0}s";
		if (_timeLeft <= 0f)
			Resolve(_hits >= Difficulty);
	}

	private void OnHit()
	{
		if (_resolving)
			return;
		PlayActionSound();
		_hits++;
		UpdateProgress();
		if (_hits >= Difficulty)
			Resolve(true);
		else
			MoveTarget();
	}

	private void MoveTarget()
	{
		var free = _area.Size - _target.Size;
		_target.Position = new Vector2(
			(float)GD.RandRange(0.0, Mathf.Max(1f, free.X)),
			(float)GD.RandRange(0.0, Mathf.Max(1f, free.Y)));
	}

	private void UpdateProgress() => _progress.Text = $"Ore:  {_hits} / {Difficulty}";

	private void Resolve(bool success)
	{
		_resolving = true;
		Finish(success || _hits > 0, success ? Reward : PartialReward(_hits / (float)Difficulty));
	}
}
