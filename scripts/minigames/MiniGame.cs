using Godot;

namespace CactusTown;

/// <summary>
/// Base class for gathering mini-games. Pauses the tree while active; call
/// <see cref="Finish"/> with the result and it unpauses and frees itself.
/// A <see cref="ResourceNode"/> calls <see cref="Configure"/> right after
/// instancing (before it enters the tree).
/// </summary>
public partial class MiniGame : Control
{
	[Signal] public delegate void FinishedEventHandler(bool success, int amount);

	protected string TitleText = "Gather";
	protected int Reward = 2;

	/// <summary>Generic difficulty knob — meaning is per mini-game (hits, taps, targets…).</summary>
	protected int Difficulty = 3;

	/// <summary>SFX name played on each successful gathering action (chop / mine / water…).</summary>
	protected string ActionSound = "gather";

	public virtual void Configure(string title, int reward, int difficulty, string actionSound = "gather")
	{
		TitleText = title;
		Reward = Mathf.Max(1, reward);
		Difficulty = Mathf.Max(1, difficulty);
		if (!string.IsNullOrEmpty(actionSound))
			ActionSound = actionSound;
	}

	protected void PlayActionSound() => Audio.Instance?.PlaySfx(ActionSound);

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		GetTree().Paused = true;
	}

	protected void Finish(bool success, int amount)
	{
		GetTree().Paused = false;
		Audio.Instance?.PlaySfx(success ? "gather" : "click");
		EmitSignal(SignalName.Finished, success, amount);
		QueueFree();
	}

	protected int PartialReward(float fraction) =>
		Mathf.Clamp(Mathf.CeilToInt(Reward * fraction), 0, Reward);
}
