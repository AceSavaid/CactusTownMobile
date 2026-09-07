using Godot;

namespace CactusTown;

/// <summary>
/// Free-moving overworld character. Movement direction is fed in from outside
/// (the virtual joystick), so this stays input-source agnostic.
/// </summary>
public partial class Player : CharacterBody2D
{
	[Export] public float MaxSpeed = 430f;
	[Export] public float Acceleration = 2800f;
	[Export] public float Friction = 3200f;

	private Vector2 _moveDirection = Vector2.Zero;
	private Sprite2D _sprite = null!;

	public override void _Ready() => _sprite = GetNode<Sprite2D>("Sprite");

	public override void _PhysicsProcess(double delta)
	{
		var target = _moveDirection.LimitLength(1f) * MaxSpeed;
		if (target.Length() > 1f)
		{
			Velocity = Velocity.MoveToward(target, Acceleration * (float)delta);
			if (target.X < -5f)
				_sprite.FlipH = true;
			else if (target.X > 5f)
				_sprite.FlipH = false;
		}
		else
		{
			Velocity = Velocity.MoveToward(Vector2.Zero, Friction * (float)delta);
		}

		MoveAndSlide();
	}

	public void SetMoveDirection(Vector2 direction) => _moveDirection = direction;
}
