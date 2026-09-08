using Godot;

namespace CactusTown;

/// <summary>
/// A harvestable resource in a region (tree, stick, rock, flower, river spot).
/// Player walks up → region offers a "gather" action → runs a mini-game →
/// yields a material. Depletes after <see cref="HarvestsUntilDepleted"/> uses,
/// then respawns.
/// </summary>
public partial class ResourceNode : Area2D
{
	[Signal] public delegate void PlayerEnteredEventHandler(ResourceNode node);
	[Signal] public delegate void PlayerExitedEventHandler(ResourceNode node);

	[Export] public string MaterialId = Materials.Wood;
	[Export] public string ActionLabel = "Chop";
	[Export] public string MiniGameTitle = "Chop the tree";
	[Export] public int Difficulty = 3;
	[Export] public int YieldAmount = 2;
	[Export] public int HarvestsUntilDepleted = 1;
	[Export] public float RespawnSeconds = 25f;
	[Export] public Texture2D? NodeTexture;
	[Export] public Vector2 SpriteOffset = new(0, -87);
	[Export] public string ActionSound = "gather";
	[Export] public PackedScene? MiniGameScene;

	private int _harvestsLeft;
	private Sprite2D _sprite = null!;
	private Node2D _prompt = null!;
	private Timer _respawn = null!;

	public bool IsDepleted => _harvestsLeft <= 0;

	public override void _Ready()
	{
		_harvestsLeft = HarvestsUntilDepleted;
		_sprite = GetNode<Sprite2D>("Sprite");
		_prompt = GetNode<Node2D>("Prompt");
		_respawn = GetNode<Timer>("Respawn");

		if (NodeTexture != null)
			_sprite.Texture = NodeTexture;
		_sprite.Position = SpriteOffset;
		PositionPrompt();

		_respawn.OneShot = true;
		_respawn.Timeout += OnRespawn;
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		_prompt.Hide();
	}

	/// <summary>Sit the "!" prompt just above the sprite's visible top.</summary>
	private void PositionPrompt()
	{
		var texHeight = _sprite.Texture?.GetHeight() ?? 96;
		_prompt.Position = new Vector2(0, SpriteOffset.Y - texHeight * 0.5f - 16f);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player || IsDepleted)
			return;
		_prompt.Show();
		EmitSignal(SignalName.PlayerEntered, this);
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not Player)
			return;
		_prompt.Hide();
		EmitSignal(SignalName.PlayerExited, this);
	}

	public MiniGame? CreateMiniGame()
	{
		if (MiniGameScene == null)
			return null;
		var game = MiniGameScene.Instantiate<MiniGame>();
		game.Configure(MiniGameTitle, YieldAmount, Difficulty, ActionSound);
		return game;
	}

	public void OnHarvested(int amount)
	{
		if (amount > 0)
			GameState.Instance.AddMaterial(MaterialId, amount);

		_harvestsLeft--;
		if (!IsDepleted)
			return;

		_prompt.Hide();
		_sprite.Modulate = new Color(1, 1, 1, 0.2f);
		SetDeferred(Area2D.PropertyName.Monitoring, false);
		_respawn.Start(RespawnSeconds);
	}

	private void OnRespawn()
	{
		_harvestsLeft = HarvestsUntilDepleted;
		_sprite.Modulate = Colors.White;
		SetDeferred(Area2D.PropertyName.Monitoring, true);
	}
}
