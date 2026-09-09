using Godot;

namespace CactusTown;

/// <summary>
/// The house — hub and main menu. Customise the plant, play mini-games,
/// check plant stats, or head out to the town.
/// </summary>
public partial class House : Control
{
	private const string TownScene = "res://scenes/TownMap.tscn";
	private const string ArcadeScene = "res://scenes/ArcadeGallery.tscn";
	private const string PlantCustomizeScene = "res://scenes/PlantCustomize.tscn";
	private const string PlantStatsScene = "res://scenes/PlantStats.tscn";

	private Label _plantName = null!;

	public override void _Ready()
	{
		_plantName = GetNode<Label>("%PlantName");

		GetNode<Button>("%CustomisePlantButton").Pressed += () => Router.Instance.GotoScene(PlantCustomizeScene);
		GetNode<Button>("%MiniGamesButton").Pressed += () => Router.Instance.GotoScene(ArcadeScene);
		GetNode<Button>("%PlantStatsButton").Pressed += () => Router.Instance.GotoScene(PlantStatsScene);
		GetNode<Button>("%GoToTownButton").Pressed += () => Router.Instance.GotoScene(TownScene);
		GetNode<Button>("%SettingsButton").Pressed += () => GetNode<SettingsMenu>("%SettingsMenu").Open();

		GameState.Instance.PlantChanged += RefreshPlant;
		RefreshPlant();
	}

	public override void _ExitTree() => GameState.Instance.PlantChanged -= RefreshPlant;

	private void RefreshPlant() => _plantName.Text = GameState.Instance.PlantName;
}
