using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// Central audio: Master / Music / SFX buses, a small play API, and settings
/// persistence. Placeholder sounds are synthesised (see <see cref="SfxBank"/> /
/// <see cref="MusicBank"/>); dropping a real file in <c>assets/audio/sfx</c> or
/// <c>assets/audio/music</c> overrides the matching name. Autoloaded as <c>Audio</c>.
/// </summary>
public partial class Audio : Node
{
	public static Audio? Instance { get; private set; }

	private const string SettingsPath = "user://audio.cfg";
	private const int SfxVoices = 10;

	private AudioStreamPlayer _musicA = null!;
	private AudioStreamPlayer _musicB = null!;
	private bool _usingA = true;
	private string _currentTrack = "";

	private readonly List<AudioStreamPlayer> _sfxPool = new();
	private int _sfxNext;
	private Dictionary<string, AudioStream> _sfx = new();
	private readonly Dictionary<string, AudioStream> _musicCache = new();

	private int _lastCoins;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;

		_musicA = NewMusicPlayer();
		_musicB = NewMusicPlayer();
		for (var i = 0; i < SfxVoices; i++)
		{
			var voice = new AudioStreamPlayer { Bus = "SFX" };
			AddChild(voice);
			_sfxPool.Add(voice);
		}

		_sfx = SfxBank.Build();
		LoadSettings();

		GetTree().NodeAdded += OnNodeAdded;
		GameState.Instance.CoinsChanged += OnCoinsChanged;
		_lastCoins = GameState.Instance.GetCoins();

		Callable.From(StartMusicForCurrentScene).CallDeferred();
	}

	private AudioStreamPlayer NewMusicPlayer()
	{
		var player = new AudioStreamPlayer { Bus = "Music" };
		AddChild(player);
		return player;
	}

	// --- SFX ---------------------------------------------------------

	public void PlaySfx(string name, float pitchJitter = 0.04f)
	{
		var stream = ResolveSfx(name);
		if (stream == null)
			return;
		var voice = _sfxPool[_sfxNext];
		_sfxNext = (_sfxNext + 1) % _sfxPool.Count;
		voice.Stream = stream;
		voice.PitchScale = 1f + (float)GD.RandRange(-pitchJitter, pitchJitter);
		voice.Play();
	}

	private AudioStream? ResolveSfx(string name)
	{
		foreach (var ext in new[] { "wav", "ogg", "mp3" })
		{
			var path = $"res://assets/audio/sfx/{name}.{ext}";
			if (ResourceLoader.Exists(path))
				return GD.Load<AudioStream>(path);
		}
		return _sfx.GetValueOrDefault(name);
	}

	// --- Music -----------------------------------------------------

	public void PlayMusic(string name)
	{
		if (name == _currentTrack)
			return;
		_currentTrack = name;

		var from = _usingA ? _musicA : _musicB;
		var to = _usingA ? _musicB : _musicA;
		_usingA = !_usingA;

		to.Stream = ResolveMusic(name);
		to.VolumeDb = -30f;
		if (to.Stream != null)
			to.Play();

		var tween = CreateTween();
		tween.TweenProperty(to, "volume_db", 0f, 0.6f);
		tween.Parallel().TweenProperty(from, "volume_db", -30f, 0.6f);
		tween.TweenCallback(Callable.From(from.Stop));
	}

	public void PlayMusicForScene(string scenePath) => PlayMusic(TrackForScene(scenePath));

	private AudioStream? ResolveMusic(string name)
	{
		if (name.Length == 0)
			return null;
		foreach (var ext in new[] { "ogg", "mp3", "wav" })
		{
			var path = $"res://assets/audio/music/{name}.{ext}";
			if (ResourceLoader.Exists(path))
				return GD.Load<AudioStream>(path);
		}
		if (!_musicCache.TryGetValue(name, out var cached))
		{
			cached = MusicBank.Track(name);
			if (cached != null)
				_musicCache[name] = cached;
		}
		return cached;
	}

	private static string TrackForScene(string path)
	{
		if (path.Contains("/arcade/") || path.EndsWith("ArcadeGallery.tscn"))
			return "arcade";
		if (path.Contains("/regions/") || path.EndsWith("RegionSelect.tscn"))
			return "region";
		if (path.Contains("/town/") || path.EndsWith("TownMap.tscn"))
			return "town";
		return "home";
	}

	private void StartMusicForCurrentScene()
	{
		var scene = GetTree().CurrentScene;
		if (scene != null)
			PlayMusicForScene(scene.SceneFilePath);
	}

	// --- Automatic hooks ------------------------------------------

	private void OnNodeAdded(Node node)
	{
		if (node is BaseButton button)
			button.Pressed += () => PlaySfx(node is CheckButton or CheckBox ? "toggle" : "click");
	}

	private void OnCoinsChanged(int total)
	{
		if (total > _lastCoins)
			PlaySfx("coin");
		_lastCoins = total;
	}

	// --- Settings ------------------------------------------------

	public void SetBusVolume(string bus, float linear)
	{
		var index = AudioServer.GetBusIndex(bus);
		if (index < 0)
			return;
		AudioServer.SetBusVolumeDb(index, ToDb(linear));
		SaveSettings();
	}

	public float GetBusVolume(string bus)
	{
		var index = AudioServer.GetBusIndex(bus);
		return index < 0 ? 1f : ToLinear(AudioServer.GetBusVolumeDb(index));
	}

	public void SetMuted(bool muted)
	{
		AudioServer.SetBusMute(0, muted);
		SaveSettings();
	}

	public bool Muted => AudioServer.IsBusMute(0);

	private static float ToDb(float linear) => linear <= 0.001f ? -60f : Mathf.LinearToDb(linear);
	private static float ToLinear(float db) => db <= -59f ? 0f : Mathf.DbToLinear(db);

	private void SaveSettings()
	{
		var cfg = new ConfigFile();
		cfg.SetValue("audio", "master", GetBusVolume("Master"));
		cfg.SetValue("audio", "music", GetBusVolume("Music"));
		cfg.SetValue("audio", "sfx", GetBusVolume("SFX"));
		cfg.SetValue("audio", "muted", Muted);
		cfg.Save(SettingsPath);
	}

	private void LoadSettings()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(SettingsPath) != Error.Ok)
			return;
		ApplyBus("Master", cfg.GetValue("audio", "master", 0.9f).AsSingle());
		ApplyBus("Music", cfg.GetValue("audio", "music", 0.7f).AsSingle());
		ApplyBus("SFX", cfg.GetValue("audio", "sfx", 0.85f).AsSingle());
		AudioServer.SetBusMute(0, cfg.GetValue("audio", "muted", false).AsBool());
	}

	private static void ApplyBus(string bus, float linear)
	{
		var index = AudioServer.GetBusIndex(bus);
		if (index >= 0)
			AudioServer.SetBusVolumeDb(index, ToDb(linear));
	}
}
