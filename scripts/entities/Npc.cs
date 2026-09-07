using Godot;

namespace CactusTown;

/// <summary>
/// A townsperson (also a cactus in a pot) with a repair request. Detects the
/// player entering its radius and announces it so the town UI can offer a
/// "Talk" action.
/// </summary>
public partial class Npc : Area2D
{
	[Signal] public delegate void PlayerEnteredEventHandler(Npc npc);
	[Signal] public delegate void PlayerExitedEventHandler(Npc npc);

	[Export] public string NpcName = "Rosa";

	/// <summary>Id of the repairable object this request targets (persisted in GameState).</summary>
	[Export] public string ObjectId = "plaza_bench";

	[Export(PropertyHint.MultilineText)] public string Greeting = "Morning! Lovely day for it.";
	[Export(PropertyHint.MultilineText)] public string RequestText = "The old plaza bench is falling apart. Fancy giving it a fix?";
	[Export(PropertyHint.MultilineText)] public string ThanksText = "You fixed it up beautifully. The whole plaza feels warmer.";
	[Export] public int RewardCoins = 25;

	private Node2D _prompt = null!;

	public override void _Ready()
	{
		_prompt = GetNode<Node2D>("Prompt");
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		_prompt.Hide();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player)
			return;
		_prompt.Visible = !IsDone();
		EmitSignal(SignalName.PlayerEntered, this);
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not Player)
			return;
		_prompt.Hide();
		EmitSignal(SignalName.PlayerExited, this);
	}

	public bool IsDone() => GameState.Instance.IsObjectFixed(ObjectId);

	public void CompleteRequest()
	{
		if (IsDone())
			return;
		GameState.Instance.SetObjectState(ObjectId, "fixed");
		GameState.Instance.AddCoins(RewardCoins);
		_prompt.Hide();
	}
}
