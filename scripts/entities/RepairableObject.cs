using Godot;
using Godot.Collections;

namespace CactusTown;

/// <summary>
/// A town fixture that starts worn-down. A cactus stands by it asking for help;
/// the player repairs it (spending materials, earning coins). Once its whole
/// section is restored, walking up lets the player re-style it between
/// <see cref="Variants"/> — index 0 is the default, later ones cost coins to unlock.
/// </summary>
public partial class RepairableObject : Area2D
{
	[Signal] public delegate void PlayerEnteredEventHandler(RepairableObject obj);
	[Signal] public delegate void PlayerExitedEventHandler(RepairableObject obj);

	[Export] public string ObjectId = "";
	[Export] public string DisplayName = "Fixture";

	/// <summary>Look options. [0] = default (free). Add more any time.</summary>
	[Export] public Array<Texture2D> Variants = new();

	/// <summary>Coin cost per variant, parallel to <see cref="Variants"/>. [0] is ignored.</summary>
	[Export] public Array<int> VariantCosts = new() { 0, 50, 100 };

	[Export] public Dictionary RequiredMaterials = new();
	[Export] public int RepairReward = 25;

	/// <summary>Tutorial/decoration fixtures: don't count toward section completion and hide the stock NPC.</summary>
	[Export] public bool ExcludeFromCompletion = false;

	/// <summary>If ≥ 0, snap to this variant the moment the object is repaired (e.g. worn sign → new sign).</summary>
	[Export] public int FixedVariant = -1;
	[Export(PropertyHint.MultilineText)] public string NpcName = "";
	[Export(PropertyHint.MultilineText)] public string RequestText = "This has seen better days.";
	[Export(PropertyHint.MultilineText)] public string ThanksText = "Wonderful — thank you!";
	[Export] public Vector2 SpriteOffset = new(0, -60);

	private bool _customizable;
	private bool _playerNear;

	private Sprite2D _sprite = null!;
	private NpcPlant _npc = null!;
	private Node2D _prompt = null!;
	private Label _promptLabel = null!;

	public bool IsFixed => GameState.Instance.IsObjectFixed(ObjectId);
	public int VariantCount => Variants.Count;
	public int CurrentVariant => GameState.Instance.GetObjectVariant(ObjectId);
	public bool PlayerCanInteract => !IsFixed || (_customizable && IsFixed && !ExcludeFromCompletion);
	public string InteractionLabel => IsFixed ? "Customise" : "Repair";

	public override void _Ready()
	{
		_sprite = GetNode<Sprite2D>("Sprite");
		_npc = GetNode<NpcPlant>("Npc");
		_prompt = GetNode<Node2D>("Prompt");
		_promptLabel = GetNode<Label>("Prompt/Label");
		_sprite.Position = SpriteOffset;
		_npc.Randomize(string.IsNullOrEmpty(ObjectId) ? Name : ObjectId);

		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
		_prompt.Hide();
		RefreshVisual();
	}

	public void SetCustomizable(bool value) => _customizable = value;

	/// <summary>Re-read state from GameState and update sprite + prompt.</summary>
	public void NotifyChanged()
	{
		RefreshVisual();
		UpdatePrompt();
	}

	public void RefreshVisual()
	{
		if (Variants.Count > 0)
		{
			var v = Mathf.Clamp(CurrentVariant, 0, Variants.Count - 1);
			_sprite.Texture = Variants[v];
		}
		var texHeight = _sprite.Texture?.GetHeight() ?? 96;
		_prompt.Position = new Vector2(0, SpriteOffset.Y - texHeight * 0.5f - 18f);
		_sprite.Modulate = IsFixed ? Colors.White : new Color(0.56f, 0.51f, 0.47f);
		_npc.Visible = !IsFixed && !ExcludeFromCompletion;
	}

	private void UpdatePrompt()
	{
		_prompt.Visible = _playerNear && PlayerCanInteract;
		_promptLabel.Text = InteractionLabel;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is not Player)
			return;
		_playerNear = true;
		UpdatePrompt();
		EmitSignal(SignalName.PlayerEntered, this);
	}

	private void OnBodyExited(Node2D body)
	{
		if (body is not Player)
			return;
		_playerNear = false;
		_prompt.Hide();
		EmitSignal(SignalName.PlayerExited, this);
	}

	// --- Repair ---------------------------------------------------------

	public bool CanRepair() => GameState.Instance.HasMaterials(RequiredMaterials);

	public void Repair()
	{
		if (IsFixed || !CanRepair())
			return;
		GameState.Instance.SpendMaterials(RequiredMaterials);
		GameState.Instance.SetObjectState(ObjectId, "fixed");
		GameState.Instance.AddCoins(RepairReward);
		if (FixedVariant >= 0 && FixedVariant < Variants.Count)
			GameState.Instance.SetObjectVariant(ObjectId, FixedVariant);
		RefreshVisual();
	}

	// --- Customisation -------------------------------------------------

	public int VariantCost(int index) =>
		index >= 0 && index < VariantCosts.Count ? VariantCosts[index] : 0;

	public bool OwnsVariant(int index) => GameState.Instance.OwnsVariant(ObjectId, index);

	public bool BuyVariant(int index) =>
		GameState.Instance.BuyVariant(ObjectId, index, VariantCost(index));

	public void SelectVariant(int index)
	{
		GameState.Instance.SetObjectVariant(ObjectId, index);
		RefreshVisual();
	}
}
