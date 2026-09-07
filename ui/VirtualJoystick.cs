using Godot;

namespace CactusTown;

/// <summary>
/// On-screen thumbstick for touch movement. Dynamic: the base jumps to wherever
/// the player first presses inside the activation zone, then tracks the drag.
/// Emits a normalised direction (length 0..1).
/// </summary>
public partial class VirtualJoystick : Control
{
	[Signal] public delegate void DirectionChangedEventHandler(Vector2 direction);

	/// <summary>Activation zone as viewport ratios (x, y, w, h). Default: lower-left quadrant.</summary>
	[Export] public Rect2 ActivationZone = new(0f, 0.35f, 0.5f, 0.65f);
	[Export] public float HandleRange = 96f;

	private int _touchIndex = -1;
	private Vector2 _homePosition;

	private TextureRect _base = null!;
	private TextureRect _handle = null!;

	public override void _Ready()
	{
		_base = GetNode<TextureRect>("Base");
		_handle = GetNode<TextureRect>("Base/Handle");
		_homePosition = _base.Position;
		CentreHandle();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventScreenTouch touch)
		{
			if (touch.Pressed && _touchIndex == -1 && InActivationZone(touch.Position))
			{
				_touchIndex = (int)touch.Index;
				_base.GlobalPosition = touch.Position - _base.Size * 0.5f;
				DragTo(touch.Position);
			}
			else if (!touch.Pressed && (int)touch.Index == _touchIndex)
			{
				Release();
			}
		}
		else if (@event is InputEventScreenDrag drag && (int)drag.Index == _touchIndex)
		{
			DragTo(drag.Position);
		}
	}

	private bool InActivationZone(Vector2 pos)
	{
		var viewport = GetViewportRect().Size;
		var zone = new Rect2(ActivationZone.Position * viewport, ActivationZone.Size * viewport);
		return zone.HasPoint(pos);
	}

	private void DragTo(Vector2 pos)
	{
		var offset = (pos - _base.GlobalPosition - _base.Size * 0.5f).LimitLength(HandleRange);
		_handle.Position = _base.Size * 0.5f + offset - _handle.Size * 0.5f;
		EmitSignal(SignalName.DirectionChanged, offset / HandleRange);
	}

	private void Release()
	{
		_touchIndex = -1;
		_base.Position = _homePosition;
		CentreHandle();
		EmitSignal(SignalName.DirectionChanged, Vector2.Zero);
	}

	private void CentreHandle() => _handle.Position = (_base.Size - _handle.Size) * 0.5f;
}
