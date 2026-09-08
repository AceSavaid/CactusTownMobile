using Godot;

namespace CactusTown;

/// <summary>Modal audio settings: mute toggle, then Master / Music / SFX steppers.</summary>
public partial class AudioSettings : Control
{
	private VolumeStepper _master = null!;
	private VolumeStepper _music = null!;
	private VolumeStepper _sfx = null!;
	private CheckButton _mute = null!;

	public override void _Ready()
	{
		_master = GetNode<VolumeStepper>("%MasterStepper");
		_music = GetNode<VolumeStepper>("%MusicStepper");
		_sfx = GetNode<VolumeStepper>("%SfxStepper");
		_mute = GetNode<CheckButton>("%MuteToggle");

		_master.LevelChanged += level => Audio.Instance?.SetBusVolume("Master", level / (float)VolumeStepper.Steps);
		_music.LevelChanged += level => Audio.Instance?.SetBusVolume("Music", level / (float)VolumeStepper.Steps);
		_sfx.LevelChanged += level =>
		{
			Audio.Instance?.SetBusVolume("SFX", level / (float)VolumeStepper.Steps);
			Audio.Instance?.PlaySfx("confirm");
		};
		_mute.Toggled += muted => Audio.Instance?.SetMuted(muted);
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
			_mute.SetPressedNoSignal(Audio.Instance.Muted);
		}
		Show();
		MoveToFront();
	}

	private static int ToLevel(float linear) => Mathf.RoundToInt(linear * VolumeStepper.Steps);
}
