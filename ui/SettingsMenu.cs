using Godot;

namespace CactusTown;

/// <summary>
/// Modal settings dialog: an <b>Audio</b> tab (Master / Music / SFX steppers) and
/// a <b>Credits</b> tab. Opened from the House "Settings" button.
/// </summary>
public partial class SettingsMenu : Control
{
	private const string Credits =
		"[center][b]Cactus Town[/b]\nby Ace Savaid[/center]\n\n" +
		"[b]Art[/b]\nThe cactus, pots, town props, buildings, region art and store\n" +
		"graphics are original to this project. Some UI icons and mini-game\n" +
		"pieces are from [b]Kenney[/b] (kenney.nl), released under CC0 1.0.\n\n" +
		"[b]Audio[/b]\nSound effects supplied by the developer.\n" +
		"Music is procedurally synthesised.\n\n" +
		"[b]Built with[/b]\nGodot Engine 4.4  ·  .NET / C#\n\n" +
		"[center]Thanks for playing.[/center]";

	private VolumeStepper _master = null!, _music = null!, _sfx = null!;

	public override void _Ready()
	{
		_master = GetNode<VolumeStepper>("%MasterStepper");
		_music = GetNode<VolumeStepper>("%MusicStepper");
		_sfx = GetNode<VolumeStepper>("%SfxStepper");
		GetNode<RichTextLabel>("%CreditsText").Text = Credits;

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
