using Godot;

namespace CactusTown;

/// <summary>
/// Hold-and-release gathering: hold to fill the bucket, release while the level
/// is inside the green band. Too little spills less water; overfilling spills
/// most of it. Used by the River.
/// </summary>
public partial class HoldGaugeMiniGame : MiniGame
{
	[Export] public float RiseSpeed = 40f;
	[Export] public float BandLow = 70f;
	[Export] public float BandHigh = 90f;

	private float _gauge;
	private bool _holding;
	private bool _resolving;

	private Label _title = null!;
	private Label _hint = null!;
	private ProgressBar _bar = null!;

	public override void _Ready()
	{
		base._Ready();
		_title = GetNode<Label>("%Title");
		_hint = GetNode<Label>("%Hint");
		_bar = GetNode<ProgressBar>("%Bar");
		_bar.MinValue = 0;
		_bar.MaxValue = 100;

		var band = GetNode<Control>("%Band");
		band.AnchorLeft = BandLow / 100f;
		band.AnchorRight = BandHigh / 100f;

		var hold = GetNode<Button>("%HoldButton");
		hold.ButtonDown += () => _holding = true;
		hold.ButtonUp += OnRelease;
		GetNode<Button>("%GiveUpButton").Pressed += () =>
		{
			if (_resolving)
				return;
			_resolving = true;
			Finish(false, 0);
		};

		_title.Text = TitleText;
		_hint.Text = "Hold — release in the green";
	}

	public override void _Process(double delta)
	{
		if (_resolving || !_holding)
			return;
		_gauge += RiseSpeed * (float)delta;
		_bar.Value = _gauge;
		if (_gauge >= 100f)
		{
			_hint.Text = "Overflowed!";
			_resolving = true;
			Finish(true, PartialReward(0.35f));
		}
	}

	private void OnRelease()
	{
		_holding = false;
		if (_resolving || _gauge <= 1f)
			return;
		_resolving = true;
		var good = _gauge >= BandLow && _gauge <= BandHigh;
		_hint.Text = good ? "Perfect!" : "A bit short";
		Finish(true, good ? Reward : PartialReward(Mathf.Clamp(_gauge / BandHigh, 0.2f, 0.85f)));
	}
}
