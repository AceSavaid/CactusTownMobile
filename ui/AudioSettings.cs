using Godot;

namespace CactusTown;

/// <summary>Modal audio settings: Master / Music / SFX sliders and a mute toggle.</summary>
public partial class AudioSettings : Control
{
	private HSlider _master = null!;
	private HSlider _music = null!;
	private HSlider _sfx = null!;
	private CheckButton _mute = null!;

	public override void _Ready()
	{
		_master = GetNode<HSlider>("%MasterSlider");
		_music = GetNode<HSlider>("%MusicSlider");
		_sfx = GetNode<HSlider>("%SfxSlider");
		_mute = GetNode<CheckButton>("%MuteToggle");

		_master.ValueChanged += value => Audio.Instance?.SetBusVolume("Master", (float)value);
		_music.ValueChanged += value => Audio.Instance?.SetBusVolume("Music", (float)value);
		_sfx.ValueChanged += value => Audio.Instance?.SetBusVolume("SFX", (float)value);
		_sfx.DragEnded += _ => Audio.Instance?.PlaySfx("confirm");
		_mute.Toggled += muted => Audio.Instance?.SetMuted(muted);
		GetNode<Button>("%CloseButton").Pressed += Hide;
		GetNode<Button>("%Backdrop").Pressed += Hide;

		Hide();
	}

	public void Open()
	{
		if (Audio.Instance != null)
		{
			_master.SetValueNoSignal(Audio.Instance.GetBusVolume("Master"));
			_music.SetValueNoSignal(Audio.Instance.GetBusVolume("Music"));
			_sfx.SetValueNoSignal(Audio.Instance.GetBusVolume("SFX"));
			_mute.SetPressedNoSignal(Audio.Instance.Muted);
		}
		Show();
		MoveToFront();
	}
}
