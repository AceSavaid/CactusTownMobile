using Godot;

namespace CactusTown;

/// <summary>A Bloom-tier decoration butterfly that bobs along a gentle path.</summary>
public partial class BloomButterfly : Sprite2D
{
	private Vector2 _home;
	private float _t;

	public override void _Ready()
	{
		_home = Position;
		TextureFilter = TextureFilterEnum.Linear;
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		Position = _home + new Vector2(Mathf.Sin(_t * 0.7f) * 60f, Mathf.Cos(_t * 1.1f) * 34f);
		FlipH = Mathf.Cos(_t * 0.7f) < 0f;
	}
}
