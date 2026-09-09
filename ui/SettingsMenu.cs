using Godot;

namespace CactusTown;

/// <summary>
/// Modal settings dialog: an <b>Audio</b> tab (Master / Music / SFX steppers) and
/// a <b>Credits</b> tab. Opened from the House "Settings" button.
/// </summary>
public partial class SettingsMenu : Control
{
	private static readonly (string Heading, string Body)[] Credits =
	{
		("Cactus Town", "A game by Ace Savaid.\nBased on the RPG Maker game jam original."),
		("Art", "Cactus, pots, town props, buildings, regions and store graphics are " +
		        "original to this project. Some UI icons and mini-game pieces are from " +
		        "Kenney (kenney.nl), released under CC0 1.0."),
		("Audio", "Sound effects supplied by the developer.\nMusic is procedurally synthesised."),
		("Built with", "Godot Engine 4.4  ·  .NET / C#"),
		("", "Thanks for playing."),
	};

	private VolumeStepper _master = null!, _music = null!, _sfx = null!;

	public override void _Ready()
	{
		_master = GetNode<VolumeStepper>("%MasterStepper");
		_music = GetNode<VolumeStepper>("%MusicStepper");
		_sfx = GetNode<VolumeStepper>("%SfxStepper");
		BuildCredits(GetNode<VBoxContainer>("%CreditsText"));

		_master.LevelChanged += level => Audio.Instance?.SetBusVolume("Master", level / (float)VolumeStepper.Steps);
		_music.LevelChanged += level => Audio.Instance?.SetBusVolume("Music", level / (float)VolumeStepper.Steps);
		_sfx.LevelChanged += level =>
		{
			Audio.Instance?.SetBusVolume("SFX", level / (float)VolumeStepper.Steps);
			Audio.Instance?.PlaySfx("confirm");
		};
		GetNode<Button>("%CloseButton").Pressed += Hide;
		GetNode<Button>("%Backdrop").Pressed += Hide;

		Hide();
	}

	private static void BuildCredits(VBoxContainer box)
	{
		foreach (var (heading, body) in Credits)
		{
			if (heading.Length > 0)
			{
				var h = new Label { Text = heading };
				h.AddThemeFontSizeOverride("font_size", 30);
				h.AddThemeColorOverride("font_color", new Color(0.72f, 0.86f, 0.6f));
				box.AddChild(h);
			}

			var p = new Label
			{
				Text = body,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				HorizontalAlignment = heading.Length > 0 ? HorizontalAlignment.Left : HorizontalAlignment.Center,
			};
			p.AddThemeFontSizeOverride("font_size", 25);
			p.AddThemeConstantOverride("line_spacing", 8);
			box.AddChild(p);
		}
	}

	public void Open()
	{
		if (Audio.Instance != null)
		{
			_master.SetLevelSilent(ToLevel(Audio.Instance.GetBusVolume("Master")));
			_music.SetLevelSilent(ToLevel(Audio.Instance.GetBusVolume("Music")));
			_sfx.SetLevelSilent(ToLevel(Audio.Instance.GetBusVolume("SFX")));
		}
		Show();
		MoveToFront();
	}

	private static int ToLevel(float linear) => Mathf.RoundToInt(linear * VolumeStepper.Steps);
}
