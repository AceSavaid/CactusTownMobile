using Godot;

namespace CactusTown;

/// <summary>Small speech modal for Sage's tips.</summary>
public partial class MentorPanel : Control
{
	private Label _body = null!;

	public override void _Ready()
	{
		_body = GetNode<Label>("%Body");
		GetNode<Button>("%CloseButton").Pressed += Hide;
		GetNode<Button>("%Backdrop").Pressed += Hide;
		Hide();
	}

	public void Open(string line)
	{
		_body.Text = line;
		Show();
		MoveToFront();
	}

	public void OpenTip() => Open(MentorTips.Line(GameState.Instance));
}
