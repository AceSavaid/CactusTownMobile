using Godot;

namespace CactusTown;

/// <summary>Hub screen: pick a town subsection (locked ones need a prior section
/// completed) or head out to the material regions.</summary>
public partial class TownMap : Control
{
	private const string HouseScene = "res://scenes/House.tscn";
	private const string RegionSelectScene = "res://scenes/RegionSelect.tscn";

	public override void _Ready()
	{
		GameState.Instance.RunTownDecay();

		var pct = GameState.Instance.BloomPercent;
		GetNode<Label>("%BloomLabel").Text = $"Town Bloom   ·   {pct}%   ·   {GameState.Instance.BloomTierName}";
		GetNode<ProgressBar>("%BloomBar").Value = pct;

		var lockIcon = GD.Load<Texture2D>("res://assets/kenney/icons/icon_lock.png");

		var grid = GetNode<GridContainer>("%Grid");
		foreach (var section in TownSections.All)
		{
			var unlocked = GameState.Instance.IsSectionUnlocked(section.Id);
			var built = ResourceLoader.Exists(section.ScenePath);

			var button = new Button
			{
				CustomMinimumSize = new Vector2(520, 150),
				Text = section.Name,
				Disabled = !unlocked,
				TooltipText = unlocked ? "" : $"Restore {UnlockedByName(section)} first",
			};
			button.AddThemeFontSizeOverride("font_size", 36);
			if (!unlocked)
			{
				button.Icon = lockIcon;
				button.ExpandIcon = false;
				button.AddThemeConstantOverride("h_separation", 18);
				button.AddThemeColorOverride("icon_disabled_color", new Color(0.75f, 0.75f, 0.8f));
			}

			var target = section.ScenePath;
			var name = section.Name;
			button.Pressed += () =>
			{
				if (built)
					Router.Instance.GotoScene(target);
				else
					Router.Instance.Toast($"{name} — coming soon");
			};
			grid.AddChild(button);
		}

		GetNode<Button>("%RegionsButton").Pressed += () => Router.Instance.GotoScene(RegionSelectScene);
		GetNode<Button>("%BackButton").Pressed += () => Router.Instance.GotoScene(HouseScene);
		GetNode<Button>("%TasksButton").Pressed += () => GetNode<TasksPanel>("%TasksPanel").Open();
	}

	private static string UnlockedByName(TownSections.Config section) =>
		TownSections.Find(section.UnlockedBy)?.Name ?? "an earlier area";
}
