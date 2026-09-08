using Godot;

namespace CactusTown;

/// <summary>
/// The Plant Stats screen: rename the plant, and see town restoration progress,
/// coins, days played, materials collected, and customisations unlocked.
/// </summary>
public partial class PlantStats : Control
{
	private const string HouseScene = "res://scenes/House.tscn";

	private VBoxContainer _list = null!;
	private LineEdit _nameEdit = null!;

	public override void _Ready()
	{
		_list = GetNode<VBoxContainer>("%List");
		_nameEdit = GetNode<LineEdit>("%NameEdit");
		_nameEdit.Text = GameState.Instance.PlantName;

		GetNode<Button>("%SaveNameButton").Pressed += SaveName;
		_nameEdit.TextSubmitted += _ => SaveName();
		GetNode<Button>("%BackButton").Pressed += () => Router.Instance.GotoScene(HouseScene);

		GameState.Instance.PlantChanged += BuildRows;
		BuildRows();
	}

	public override void _ExitTree()
	{
		GameState.Instance.PlantChanged -= BuildRows;
	}

	private void SaveName()
	{
		GameState.Instance.SetPlantName(_nameEdit.Text);
		_nameEdit.Text = GameState.Instance.PlantName;
		_nameEdit.ReleaseFocus();
		Router.Instance.Toast($"Renamed to {GameState.Instance.PlantName}");
	}

	private void BuildRows()
	{
		foreach (var child in _list.GetChildren())
			child.QueueFree();

		var gs = GameState.Instance;

		Header("Town restoration");
		var totalFixed = 0;
		var totalObjects = 0;
		foreach (var section in TownSections.All)
		{
			var (fixedCount, total) = gs.SectionProgress(section.Id);
			totalFixed += fixedCount;
			totalObjects += total;
			var done = total > 0 && fixedCount == total ? "   done" : "";
			Row(section.Name, $"{fixedCount} / {total}{done}");
		}
		Row("Overall", $"{totalFixed} / {totalObjects}");

		Header("Wallet & time");
		Row("Coins", gs.GetCoins().ToString());
		Row("Days on the app", gs.DaysOnApp.ToString());

		Header("Customisations unlocked");
		Row("Town styles", gs.TownCustomizationsUnlocked.ToString());
		Row("Plant styles", $"{gs.PlantCustomizationsUnlocked} / {PlantCatalog.PurchasableCount}");

		Header("Materials collected");
		foreach (var id in Materials.All)
			Row(Materials.DisplayName(id), gs.GetMaterial(id).ToString());
	}

	private void Header(string text)
	{
		var label = new Label { Text = text };
		label.AddThemeFontSizeOverride("font_size", 34);
		label.AddThemeColorOverride("font_color", new Color(0.25f, 0.32f, 0.24f));
		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_top", _list.GetChildCount() == 0 ? 0 : 22);
		margin.AddChild(label);
		_list.AddChild(margin);
	}

	private void Row(string name, string value)
	{
		var row = new HBoxContainer();
		var left = new Label { Text = name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		left.AddThemeFontSizeOverride("font_size", 30);
		var right = new Label { Text = value, HorizontalAlignment = HorizontalAlignment.Right };
		right.AddThemeFontSizeOverride("font_size", 30);
		row.AddChild(left);
		row.AddChild(right);
		_list.AddChild(row);
	}
}
