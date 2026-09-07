using Godot;

namespace CactusTown;

/// <summary>
/// Scene navigation with a fade transition, plus a lightweight toast.
/// Autoloaded as <c>Router</c>. Layer sits above gameplay and always processes.
/// </summary>
public partial class Router : CanvasLayer
{
	private const float FadeTime = 0.25f;

	public static Router Instance { get; private set; } = null!;

	private ColorRect _fade = null!;
	private Label _toast = null!;
	private bool _busy;

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always;
		Layer = 128;

		_fade = new ColorRect
		{
			Color = Colors.Black,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = new Color(1, 1, 1, 0),
		};
		_fade.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(_fade);

		_toast = new Label
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = new Color(1, 1, 1, 0),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AnchorLeft = 0.5f,
			AnchorRight = 0.5f,
			AnchorTop = 1.0f,
			AnchorBottom = 1.0f,
			OffsetLeft = -400,
			OffsetRight = 400,
			OffsetTop = -150,
			OffsetBottom = -90,
		};
		AddChild(_toast);
	}

	public async void GotoScene(string scenePath)
	{
		if (_busy)
			return;
		_busy = true;
		_fade.MouseFilter = Control.MouseFilterEnum.Stop;

		var tween = CreateTween();
		tween.TweenProperty(_fade, "modulate:a", 1.0, FadeTime);
		await ToSignal(tween, Tween.SignalName.Finished);

		var error = GetTree().ChangeSceneToFile(scenePath);
		if (error != Error.Ok)
			GD.PushError($"Failed to load scene: {scenePath} ({error})");

		var outTween = CreateTween();
		outTween.TweenProperty(_fade, "modulate:a", 0.0, FadeTime);
		await ToSignal(outTween, Tween.SignalName.Finished);

		_fade.MouseFilter = Control.MouseFilterEnum.Ignore;
		_busy = false;
	}

	public void Toast(string message, float duration = 1.6f)
	{
		_toast.Text = message;
		_toast.Modulate = Colors.White;
		var tween = CreateTween();
		tween.TweenInterval(duration);
		tween.TweenProperty(_toast, "modulate:a", 0.0, 0.4);
	}
}
