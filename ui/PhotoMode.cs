using Godot;

namespace CactusTown;

/// <summary>
/// A clean-view photo mode: hides the gameplay HUD, gives a free pan/zoom camera
/// and a soft frame so the player can line up a shot and use their device's own
/// screenshot. <see cref="Enter"/> from a camera button; <see cref="Exit"/> restores.
/// </summary>
public partial class PhotoMode : CanvasLayer
{
	private static readonly Vector2 ZoomMin = new(0.35f, 0.35f);
	private static readonly Vector2 ZoomMax = new(1.4f, 1.4f);

	private Camera2D _cam = null!;
	private CanvasItem? _hiddenItem;
	private CanvasLayer? _hiddenLayer;
	private Camera2D? _previousCam;
	private bool _active;

	public override void _Ready()
	{
		Layer = 180;
		_cam = GetNode<Camera2D>("Cam");
		GetNode<Button>("%ExitButton").Pressed += Exit;
		GetNode<Button>("%ZoomInButton").Pressed += () => Zoom(0.82f);
		GetNode<Button>("%ZoomOutButton").Pressed += () => Zoom(1.22f);
		Visible = false;
	}

	public void Enter(Node hudToHide, Vector2 startPosition)
	{
		_hiddenItem = hudToHide as CanvasItem;
		_hiddenLayer = hudToHide as CanvasLayer;
		SetHudVisible(false);

		_previousCam = GetViewport().GetCamera2D();
		_cam.GlobalPosition = startPosition;
		_cam.Zoom = new Vector2(0.68f, 0.68f);
		_cam.MakeCurrent();

		_active = true;
		Visible = true;
		Audio.Instance?.PlaySfx("page");
	}

	public void Exit()
	{
		if (!_active)
			return;
		_active = false;
		Visible = false;
		SetHudVisible(true);
		_previousCam?.MakeCurrent();
		Audio.Instance?.PlaySfx("cancel");
	}

	private void SetHudVisible(bool visible)
	{
		if (_hiddenItem != null) _hiddenItem.Visible = visible;
		if (_hiddenLayer != null) _hiddenLayer.Visible = visible;
	}

	private void Zoom(float factor) =>
		_cam.Zoom = (_cam.Zoom * factor).Clamp(ZoomMin, ZoomMax);

	public override void _Input(InputEvent @event)
	{
		if (!_active)
			return;
		if (@event is InputEventScreenDrag drag)
		{
			_cam.GlobalPosition -= drag.Relative / _cam.Zoom;
			GetViewport().SetInputAsHandled();
		}
		else if (@event is InputEventMagnifyGesture pinch)
		{
			Zoom(1f / pinch.Factor);
			GetViewport().SetInputAsHandled();
		}
	}
}
