using Godot;

namespace CactusTown;

/// <summary>
/// Sage's ambient tip on the House hub — a small always-visible card (no dimmer,
/// no modal) that re-reads <see cref="MentorTips"/> whenever progress changes.
/// </summary>
public partial class HubTip : PanelContainer
{
	private Label _body = null!;

	public override void _Ready()
	{
		_body = GetNode<Label>("%Body");
		Refresh();
		GameState.Instance.ProgressChanged += Refresh;
		GameState.Instance.PlantChanged += Refresh;
		GameState.Instance.ObjectsChanged += Refresh;
	}

	public override void _ExitTree()
	{
		GameState.Instance.ProgressChanged -= Refresh;
		GameState.Instance.PlantChanged -= Refresh;
		GameState.Instance.ObjectsChanged -= Refresh;
	}

	private void Refresh() => _body.Text = MentorTips.Line(GameState.Instance);
}
