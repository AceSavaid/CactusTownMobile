using Godot;

namespace CactusTown;

/// <summary>Menu of gathering regions, reached from the town.</summary>
public partial class RegionSelect : Control
{
	private const string TownScene = "res://scenes/Town.tscn";

	public override void _Ready()
	{
		GetNode<Button>("%ForestButton").Pressed += () => Router.Instance.GotoScene("res://scenes/regions/Forest.tscn");
		GetNode<Button>("%FlowerFieldButton").Pressed += () => Router.Instance.Toast("Flower Field — coming soon");
		GetNode<Button>("%RiverButton").Pressed += () => Router.Instance.Toast("River — coming soon");
		GetNode<Button>("%CaveButton").Pressed += () => Router.Instance.Toast("Cave — coming soon");
		GetNode<Button>("%BackButton").Pressed += () => Router.Instance.GotoScene(TownScene);
	}
}
