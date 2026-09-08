using Godot;

namespace CactusTown;

/// <summary>Menu of gathering regions, reached from the town.</summary>
public partial class RegionSelect : Control
{
	private const string TownScene = "res://scenes/TownMap.tscn";

	public override void _Ready()
	{
		GetNode<Button>("%ForestButton").Pressed += () => Router.Instance.GotoScene("res://scenes/regions/Forest.tscn");
		GetNode<Button>("%FlowerFieldButton").Pressed += () => Router.Instance.GotoScene("res://scenes/regions/FlowerField.tscn");
		GetNode<Button>("%RiverButton").Pressed += () => Router.Instance.GotoScene("res://scenes/regions/River.tscn");
		GetNode<Button>("%CaveButton").Pressed += () => Router.Instance.GotoScene("res://scenes/regions/Cave.tscn");
		GetNode<Button>("%BackButton").Pressed += () => Router.Instance.GotoScene(TownScene);
		GetNode<Button>("%TasksButton").Pressed += () => GetNode<TasksPanel>("%TasksPanel").Open();
	}
}
