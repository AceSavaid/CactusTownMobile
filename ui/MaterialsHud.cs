using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>Compact read-out of the materials the player is carrying.</summary>
public partial class MaterialsHud : PanelContainer
{
	private Label _label = null!;

	public override void _Ready()
	{
		_label = GetNode<Label>("Margin/Label");
		GameState.Instance.MaterialsChanged += Refresh;
		Refresh();
	}

	public override void _ExitTree()
	{
		GameState.Instance.MaterialsChanged -= Refresh;
	}

	private void Refresh()
	{
		var parts = new List<string>();
		foreach (var (id, count) in GameState.Instance.GetMaterials())
		{
			if (count.AsInt32() > 0)
				parts.Add($"{Materials.DisplayName(id.AsString())} {count.AsInt32()}");
		}
		_label.Text = parts.Count > 0 ? string.Join("     ", parts) : "No materials yet";
	}
}
