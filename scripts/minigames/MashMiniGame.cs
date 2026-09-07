using Godot;

namespace CactusTown;

/// <summary>
/// Button-mash gathering: tap PICK repeatedly to fill the basket before time
/// runs out. The fill decays, so keep tapping. Used by the Flower Field.
/// </summary>
public partial class MashMiniGame : MiniGame
{
	[Export] public float TimeLimit = 6f;
	[Export] public float DecayPerSecond = 10f;

	private float _fill;
	private float _timeLeft;
	private float _gainPerTap = 12f;
	private bool _resolving;

	private Label _title = null!;
	private Label _timer = null!;
	private ProgressBar _bar = null!;

	public override void _Ready()
	{
		base._Ready();
		_title = GetNode<Label>("%Title");
		_timer = GetNode<Label>("%Timer");
		_bar = GetNode<ProgressBar>("%Bar");

		_timeLeft = TimeLimit;
		_gainPerTap = 130f / Difficulty;
		_bar.MinValue = 0;
		_bar.MaxValue = 100;

		GetNode<Button>("%PickButton").Pressed += OnPick;
		GetNode<Button>("%GiveUpButton").Pressed += () => Resolve(false);
		_title.Text = TitleText;
	}

	public override void _Process(double delta)
	{
		if (_resolving)
			return;
		_fill = Mathf.Max(0f, _fill - DecayPerSecond * (float)delta);
		_timeLeft -= (float)delta;
		_bar.Value = _fill;
		_timer.Text = $"{Mathf.Max(0f, _timeLeft):0.0}s";

		if (_fill >= 100f)
			Resolve(true);
		else if (_timeLeft <= 0f)
			Resolve(_fill >= 60f);
	}

	private void OnPick()
	{
		if (!_resolving)
			_fill = Mathf.Min(100f, _fill + _gainPerTap);
	}

	private void Resolve(bool success)
	{
		_resolving = true;
		Finish(success, success ? Reward : PartialReward(_fill / 100f));
	}
}
