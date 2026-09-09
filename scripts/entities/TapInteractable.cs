using Godot;

namespace CactusTown;

/// <summary>
/// A world object the player walks up to and taps — the Notice Board, the
/// Mentor. Shows a floating prompt when the player is in range; a tap on or near
/// it while in range emits <see cref="Interacted"/>.
/// </summary>
public partial class TapInteractable : Area2D
{
	[Signal] public delegate void InteractedEventHandler();

	[Export] public string PromptText = "Look";
	[Export] public Vector2 PromptOffset = new(0, -150);

	/// <summary>World-space radius around this node that counts as a tap on it.</summary>
	[Export] public float TapRadius = 150f;

	private bool _near;
	private Label _prompt = null!;

	public override void _Ready()
	{
		_prompt = new Label
		{
			Text = PromptText,
			HorizontalAlignment = HorizontalAlignment.Center,
			Visible = false,
			Size = new Vector2(240, 40),
			Position = PromptOffset - new Vector2(120, 0),
		};
		_prompt.AddThemeFontSizeOverride("font_size", 30);
		AddChild(_prompt);

		BodyEntered += body => OnBody(body, true);
		BodyExited += body => OnBody(body, false);

		OnReady();
	}

	/// <summary>Subclass setup after the prompt exists.</summary>
	protected virtual void OnReady() { }

	public void SetPrompt(string text)
	{
		PromptText = text;
		_prompt.Text = text;
	}

	private void OnBody(Node2D body, bool entering)
	{
		if (body is not Player)
			return;
		_near = entering;
		_prompt.Visible = entering;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_near || GetTree().Paused)
			return;

		Vector2 screenPos;
		if (@event is InputEventScreenTouch { Pressed: true } touch)
			screenPos = touch.Position;
		else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
			screenPos = click.Position;
		else
			return;

		var world = GetCanvasTransform().AffineInverse() * screenPos;
		if (world.DistanceTo(GlobalPosition) <= TapRadius)
		{
			GetViewport().SetInputAsHandled();
			EmitSignal(SignalName.Interacted);
		}
	}
}
