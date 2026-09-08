using Godot;

namespace CactusTown;

/// <summary>
/// Free-moving overworld character — the player's customised plant. Movement
/// direction is fed in from outside (the virtual joystick), so this stays
/// input-source agnostic.
/// </summary>
public partial class Player : CharacterBody2D
{
	[Export] public float MaxSpeed = 430f;
	[Export] public float Acceleration = 2800f;
	[Export] public float Friction = 3200f;

	private Vector2 _moveDirection = Vector2.Zero;
	private Node2D _avatar = null!;
	private float _avatarScale = 0.62f;
	private int _facing = 1;

	public override void _Ready()
	{
		_avatar = GetNode<Node2D>("Avatar");
		_avatarScale = _avatar.Scale.X;
	}

	public override void _PhysicsProcess(double delta)
	{
		var target = _moveDirection.LimitLength(1f) * MaxSpeed;
		if (target.Length() > 1f)
		{
			Velocity = Velocity.MoveToward(target, Acceleration * (float)delta);
			if (target.X < -5f)
				_facing = -1;
			else if (target.X > 5f)
				_facing = 1;
			_avatar.Scale = new Vector2(_facing * _avatarScale, _avatarScale);
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, Friction * (float)delta);
		}

		MoveAndSlide();
	}

	public void SetMoveDirection(Vector2 direction) => _moveDirection = direction;
}
