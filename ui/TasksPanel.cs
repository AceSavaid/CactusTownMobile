using Godot;

namespace CactusTown;

/// <summary>
/// Modal list of repair tasks the player has taken on but not finished (including
/// ones that have since decayed). Shows the materials each still needs.
/// </summary>
public partial class TasksPanel : Control
{
	private VBoxContainer _list = null!;
	private Label _empty = null!;

	public override void _Ready()
	{
		_list = GetNode<VBoxContainer>("%List");
		_empty = GetNode<Label>("%Empty");
		GetNode<Button>("%CloseButton").Pressed += Hide;
		GetNode<Button>("%Backdrop").Pressed += Hide;
		Hide();
	}

	public void Open()
	{
		Rebuild();
		Show();
		MoveToFront();
	}

	private void Rebuild()
	{
		foreach (var child in _list.GetChildren())
			child.QueueFree();

		var tasks = GameState.Instance.OpenTasks();
		_empty.Visible = tasks.Count == 0;

		foreach (var task in tasks)
		{
			var card = new VBoxContainer();
			card.AddThemeConstantOverride("separation", 4);

			var heading = new Label { Text = $"{task["section"].AsString()}  —  {task["name"].AsString()}" };
			heading.AddThemeFontSizeOverride("font_size", 32);
			card.AddChild(heading);

			foreach (var (id, count) in task["needs"].AsGodotDictionary())
			{
				var key = id.AsString();
				var need = count.AsInt32();
				var have = GameState.Instance.GetMaterial(key);
				var line = new Label
				{
					Text = $"     {(have >= need ? "✓" : "•")}  {Materials.DisplayName(key)}   {have} / {need}",
				};
				line.AddThemeFontSizeOverride("font_size", 26);
				card.AddChild(line);
			}

			var spacer = new Control { CustomMinimumSize = new Vector2(0, 14) };
			card.AddChild(spacer);
			_list.AddChild(card);
		}
	}
}
