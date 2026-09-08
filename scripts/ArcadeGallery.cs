using Godot;

namespace CactusTown;

/// <summary>
/// The House arcade: a scrolling grid of game cards. Pick one, choose a
/// difficulty, and it launches over the gallery. Cards come from
/// <see cref="ArcadeCatalog"/>, so the grid grows automatically.
/// </summary>
public partial class ArcadeGallery : Control
{
	private const string HouseScene = "res://scenes/House.tscn";

	private ArcadeCatalog.Entry? _pending;

	private GridContainer _grid = null!;
	private Control _difficultyPanel = null!;
	private Label _difficultyTitle = null!;
	private CheckButton _modeToggle = null!;
	private Control _gameHost = null!;

	public override void _Ready()
	{
		_grid = GetNode<GridContainer>("%Grid");
		_difficultyPanel = GetNode<Control>("%DifficultyPanel");
		_difficultyTitle = GetNode<Label>("%DifficultyTitle");
		_modeToggle = GetNode<CheckButton>("%ModeToggle");
		_gameHost = GetNode<Control>("%GameHost");
		_difficultyPanel.Hide();

		GetNode<Button>("%BackButton").Pressed += () => Router.Instance.GotoScene(HouseScene);
		GetNode<Button>("%DifficultyCancel").Pressed += () => _difficultyPanel.Hide();
		GetNode<Button>("%EasyButton").Pressed += () => Launch(1);
		GetNode<Button>("%MediumButton").Pressed += () => Launch(2);
		GetNode<Button>("%HardButton").Pressed += () => Launch(3);

		foreach (var entry in ArcadeCatalog.All)
			_grid.AddChild(BuildCard(entry));
	}

	private Control BuildCard(ArcadeCatalog.Entry entry)
	{
		var built = ResourceLoader.Exists(entry.ScenePath);

		var card = new Button { CustomMinimumSize = new Vector2(300, 320) };
		card.Pressed += () => OnCardPressed(entry, built);

		var vbox = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		vbox.SetAnchorsPreset(LayoutPreset.FullRect);
		vbox.OffsetLeft = 18;
		vbox.OffsetTop = 18;
		vbox.OffsetRight = -18;
		vbox.OffsetBottom = -18;
		vbox.AddThemeConstantOverride("separation", 12);
		card.AddChild(vbox);

		var icon = new TextureRect
		{
			Texture = GD.Load<Texture2D>(entry.IconPath),
			SizeFlagsVertical = SizeFlags.ExpandFill,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			MouseFilter = MouseFilterEnum.Ignore,
			Modulate = built ? Colors.White : new Color(1, 1, 1, 0.4f),
		};
		vbox.AddChild(icon);

		var label = new Label
		{
			Text = built ? entry.Name : $"{entry.Name}  (soon)",
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		label.AddThemeFontSizeOverride("font_size", 30);
		vbox.AddChild(label);

		return card;
	}

	private void OnCardPressed(ArcadeCatalog.Entry entry, bool built)
	{
		if (!built)
		{
			Router.Instance.Toast($"{entry.Name} — coming soon");
			return;
		}
		_pending = entry;
		_difficultyTitle.Text = entry.Name;
		_modeToggle.Visible = entry.SupportsSolo;
		_modeToggle.ButtonPressed = false;
		_difficultyPanel.Show();
	}

	private void Launch(int difficulty)
	{
		_difficultyPanel.Hide();
		if (_pending == null)
			return;

		var game = GD.Load<PackedScene>(_pending.ScenePath).Instantiate<ArcadeGame>();
		game.SoloMode = _pending.SupportsSolo && _modeToggle.ButtonPressed;
		game.Configure(difficulty);
		_gameHost.AddChild(game);
	}
}
