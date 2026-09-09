using Godot;

namespace CactusTown;

/// <summary>
/// The house — hub and main menu. Customise the plant, play mini-games,
/// check plant stats, or head out to the town. The menu opens up progressively:
/// a first-time player sees only "Go to Town", "Settings", the Notices icon and
/// Sage's tip; the three menu plaques appear once they've made their first repair.
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
		GetNode<Button>("%NoticesButton").Pressed += () => GetNode<NoticePanel>("%NoticePanel").Open();
		GetNode<HubQuest>("%HubQuest").Panel = GetNode<NoticePanel>("%NoticePanel");

		ApplyProgressiveMenu();

		GameState.Instance.PlantChanged += RefreshPlant;
		RefreshPlant();
	}

	public override void _ExitTree() => GameState.Instance.PlantChanged -= RefreshPlant;

	private void ApplyProgressiveMenu()
	{
		var unlocked = GameState.Instance.FirstRepairDone;
		GetNode<Button>("%CustomisePlantButton").Visible = unlocked;
		GetNode<Button>("%MiniGamesButton").Visible = unlocked;
		GetNode<Button>("%PlantStatsButton").Visible = unlocked;
	}

	private void RefreshPlant() => _plantName.Text = GameState.Instance.PlantName;
}
