using Godot;

namespace CactusTown;

/// <summary>Reusable end-of-round overlay for arcade games.</summary>
public partial class ArcadeResult : Control
{
	[Signal] public delegate void PlayAgainEventHandler();
	[Signal] public delegate void LeaveEventHandler();

	private Label _label = null!;
	private CpuParticles2D _burst = null!;

	public override void _Ready()
	{
		_label = GetNode<Label>("%Label");
		_burst = GetNode<CpuParticles2D>("%Burst");
		GetNode<Button>("%PlayAgainButton").Pressed += () => { Hide(); EmitSignal(SignalName.PlayAgain); };
		GetNode<Button>("%LeaveButton").Pressed += () => EmitSignal(SignalName.Leave);
		Hide();
	}

	/// <summary>result: 1 win, 0 draw, -1 loss. coins = amount already awarded for a win.</summary>
	public void ShowResult(int result, int coins)
	{
		_label.Text = result switch
		{
			> 0 => $"You win!\n+{coins} coins",
			0 => "It's a draw",
			_ => "You lose",
		};
		Show();
		MoveToFront();

		if (result > 0)
		{
			_burst.Restart();
			_burst.Emitting = true;
		}
	}
}
