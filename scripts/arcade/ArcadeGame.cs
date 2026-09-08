using Godot;

namespace CactusTown;

/// <summary>
/// Base class for the House arcade games. The gallery instances one, calls
/// <see cref="Configure"/> with the chosen difficulty, and listens for
/// <see cref="Finished"/>. A win pays <c>Difficulty</c> coins (1 / 2 / 3).
/// Games call <see cref="ReportResult"/> when a round ends and <see cref="Close"/>
/// when the player leaves.
/// </summary>
public partial class ArcadeGame : Control
{
	[Signal] public delegate void FinishedEventHandler();

	/// <summary>1 = easy, 2 = medium, 3 = hard. Also the coin reward for a win.</summary>
	public int Difficulty { get; private set; } = 1;

	/// <summary>
	/// Set by the gallery before <see cref="Configure"/> for games that offer a
	/// solo variant (see <c>ArcadeCatalog.Entry.SupportsSolo</c>). Games that
	/// ignore it always play their default mode.
	/// </summary>
	public bool SoloMode { get; set; }

	protected int LastResult { get; private set; }

	public virtual void Configure(int difficulty)
	{
		Difficulty = Mathf.Clamp(difficulty, 1, 3);
	}

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
	}

	/// <summary>Record a finished round: 1 win, 0 draw, -1 loss. A win pays coins now.</summary>
	protected void ReportResult(int result)
	{
		LastResult = result;
		if (result > 0)
			GameState.Instance.AddCoins(Difficulty);
		Audio.Instance?.PlaySfx(result > 0 ? "win" : result < 0 ? "lose" : "confirm");
	}

	protected void Close()
	{
		EmitSignal(SignalName.Finished);
		QueueFree();
	}
}
