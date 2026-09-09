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
		GameState.Instance.SeedsChanged += OnSeedsChanged;
		Refresh();
	}

	public override void _ExitTree()
	{
		GameState.Instance.CoinsChanged -= OnCoinsChanged;
		GameState.Instance.SeedsChanged -= OnSeedsChanged;
	}

	private void OnCoinsChanged(int _) => Refresh();
	private void OnSeedsChanged(int _) => Refresh();

	private void Refresh()
	{
		var coins = GameState.Instance.GetCoins();
		var seeds = GameState.Instance.Seeds;
		_label.Text = seeds > 0 ? $"{coins} coins   ·   {seeds} seeds" : $"{coins} coins";
	}
}
