using Godot;

namespace CactusTown;

/// <summary>
/// Base class for gathering mini-games. Pauses the tree while active; call
/// <see cref="Finish"/> with the result and it unpauses and frees itself.
/// </summary>
public partial class MiniGame : Control
{
	[Signal] public delegate void FinishedEventHandler(bool success, int amount);

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		GetTree().Paused = true;
	}

	protected void Finish(bool success, int amount)
	{
		GetTree().Paused = false;
		EmitSignal(SignalName.Finished, success, amount);
		QueueFree();
	}
}
