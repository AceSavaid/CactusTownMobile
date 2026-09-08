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

	private Label _plantName = null!;

	public override void _Ready()
	{
		_plantName = GetNode<Label>("%PlantName");

		GetNode<Button>("%CustomisePlantButton").Pressed += () => Router.Instance.Toast("Plant customisation — coming soon");
		GetNode<Button>("%MiniGamesButton").Pressed += () => Router.Instance.GotoScene(ArcadeScene);
		GetNode<Button>("%PlantStatsButton").Pressed += () => Router.Instance.Toast("Plant stats — coming soon");
		GetNode<Button>("%GoToTownButton").Pressed += () => Router.Instance.GotoScene(TownScene);

		GameState.Instance.PlantChanged += RefreshPlant;
		RefreshPlant();
	}

	public override void _ExitTree() => GameState.Instance.PlantChanged -= RefreshPlant;

	private void RefreshPlant()
	{
		var plant = GameState.Instance.GetPlant();
		var name = plant.TryGetValue("name", out var value) ? value.AsString() : "?";
		_plantName.Text = $"Your plant: {name}";
	}
}
