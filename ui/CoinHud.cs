using Godot;

namespace CactusTown;

/// <summary>Persistent coin counter. Drop into any screen; it self-syncs with GameState.</summary>
public partial class CoinHud : PanelContainer
{
	private Label _label = null!;

	public override void _Ready()
	{
		_label = GetNode<Label>("Margin/Label");
		GameState.Instance.CoinsChanged += OnCoinsChanged;
		OnCoinsChanged(GameState.Instance.GetCoins());
	}

	public override void _ExitTree()
	{
		GameState.Instance.CoinsChanged -= OnCoinsChanged;
	}

	private void OnCoinsChanged(int total) => _label.Text = $"{total} coins";
}
