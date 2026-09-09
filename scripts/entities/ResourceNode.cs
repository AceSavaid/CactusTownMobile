using System.Linq;
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

	/// <summary>Pause after a harvest that did NOT deplete the node, before it can be gathered again.</summary>
	[Export] public float CooldownSeconds = 0f;
	[Export] public Texture2D? NodeTexture;
	[Export] public Vector2 SpriteOffset = new(0, -87);
	[Export] public string ActionSound = "gather";
	[Export] public PackedScene? MiniGameScene;

	private int _harvestsLeft;
	private bool _onCooldown;
	private Sprite2D _sprite = null!;
	private Node2D _prompt = null!;
	private Timer _respawn = null!;
	private Timer _cooldown = null!;

	public bool IsDepleted => _harvestsLeft <= 0;

	/// <summary>Freshly harvested and still cooling down — can't be gathered yet.</summary>
	public bool IsOnCooldown => _onCooldown;

	public override void _Ready()
	{
		_harvestsLeft = HarvestsUntilDepleted;
		_sprite = GetNode<Sprite2D>("Sprite");
		_prompt = GetNode<Node2D>("Prompt");
		_respawn = GetNode<Timer>("Respawn");
		_cooldown = GetNode<Timer>("Cooldown");

		if (NodeTexture != null)
			_sprite.Texture = NodeTexture;
		_sprite.Position = SpriteOffset;
		PositionPrompt();

		_respawn.OneShot = true;
		_respawn.Timeout += OnRespawn;
		_cooldown.OneShot = true;
		_cooldown.Timeout += OnCooldownDone;
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
		if (body is not Player || IsDepleted || _onCooldown)
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
		{
			GameState.Instance.AddMaterial(MaterialId, amount);
			GameState.Instance.RecordGather();
		}

		_harvestsLeft--;
		if (IsDepleted)
		{
			_prompt.Hide();
			_sprite.Modulate = new Color(1, 1, 1, 0.2f);
			SetDeferred(Area2D.PropertyName.Monitoring, false);
			_respawn.Start(RespawnSeconds);
			return;
		}

		if (CooldownSeconds > 0f)
			StartCooldown();
	}

	private void StartCooldown()
	{
		// Monitoring stays on so walk-away still fires PlayerExited; the flag gates gathering.
		_onCooldown = true;
		_prompt.Hide();
		_sprite.Modulate = new Color(1, 1, 1, 0.45f);
		_cooldown.Start(CooldownSeconds);
	}

	private void OnCooldownDone()
	{
		_onCooldown = false;
		_sprite.Modulate = Colors.White;
		// Still standing here? Re-offer the node.
		if (GetOverlappingBodies().Any(b => b is Player))
		{
			_prompt.Show();
			EmitSignal(SignalName.PlayerEntered, this);
		}
	}

	private void OnRespawn()
	{
		_harvestsLeft = HarvestsUntilDepleted;
		_sprite.Modulate = Colors.White;
		SetDeferred(Area2D.PropertyName.Monitoring, true);
	}
}
