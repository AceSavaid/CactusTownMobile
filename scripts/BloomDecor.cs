using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CactusTown;

/// <summary>
/// Dresses a town section to match the town-wide Bloom tier: planters and grass
/// near the fixtures (tier 1+), string lights across the buildings (tier 3+), a
/// warm tint and a few butterflies (tier 4). Purely cosmetic; rebuilt on load.
/// </summary>
public static class BloomDecor
{
	private static readonly Texture2D Planter = GD.Load<Texture2D>("res://assets/sprites/bloom/planter_flowers.svg");
	private static readonly Texture2D Grass = GD.Load<Texture2D>("res://assets/sprites/bloom/grass_tuft.svg");
	private static readonly Texture2D Lights = GD.Load<Texture2D>("res://assets/sprites/bloom/hanging_lights.svg");
	private static readonly Texture2D ButterflyTex = GD.Load<Texture2D>("res://assets/sprites/bloom/butterfly.svg");

	public static void Apply(Node2D section, Node2D world, string sectionId, IEnumerable<RepairableObject> fixtures)
	{
		var tier = GameState.Instance.BloomTier;
		if (tier <= 0)
			return;

		var rng = new RandomNumberGenerator { Seed = Hash(sectionId) };

		// ground spots: a couple beside each fixture
		var ground = new List<Vector2>();
		foreach (var f in fixtures)
		{
			ground.Add(f.Position + new Vector2(-96, 40));
			ground.Add(f.Position + new Vector2(104, 26));
		}
		Shuffle(ground, rng);
		var revealed = Mathf.CeilToInt(ground.Count * tier / 4f);

		var layer = new Node2D { Name = "BloomDecor", YSortEnabled = true };
		world.AddChild(layer);

		for (var i = 0; i < revealed && i < ground.Count; i++)
		{
			var useGrass = rng.Randf() < (tier <= 1 ? 0.85f : 0.4f);
			layer.AddChild(new Sprite2D
			{
				Texture = useGrass ? Grass : Planter,
				Position = ground[i],
				TextureFilter = CanvasItem.TextureFilterEnum.Linear,
				Scale = Vector2.One * rng.RandfRange(0.9f, 1.15f),
			});
		}

		// string lights across the front buildings (tier 3+)
		if (tier >= 3 && section.GetNodeOrNull<Node2D>("Backdrop") is { } backdrop)
		{
			var fronts = backdrop.GetChildren().OfType<Sprite2D>()
				.Where(s => s.Position.Y > -260 && s.Texture != null)
				.OrderBy(s => s.Position.X).ToList();
			for (var i = 0; i + 1 < fronts.Count; i += 2)
			{
				var a = fronts[i].Position;
				var b = fronts[i + 1].Position;
				layer.AddChild(new Sprite2D
				{
					Texture = Lights,
					Position = new Vector2((a.X + b.X) / 2f, Mathf.Min(a.Y, b.Y) - 40f),
					Scale = new Vector2(Mathf.Clamp(Mathf.Abs(b.X - a.X) / 360f, 0.8f, 2.2f), 1f),
					ZIndex = 1,
				});
			}
		}

		// flourishing: warm tint + drifting butterflies
		if (tier >= 4)
		{
			section.AddChild(new CanvasModulate { Color = new Color(1.04f, 1.0f, 0.94f) });
			for (var i = 0; i < 4; i++)
				layer.AddChild(new BloomButterfly
				{
					Texture = ButterflyTex,
					Position = new Vector2(rng.RandfRange(-700, 700), rng.RandfRange(-260, 260)),
					Scale = Vector2.One * rng.RandfRange(1.0f, 1.6f),
					ZIndex = 3,
				});
		}
	}

	private static ulong Hash(string s)
	{
		ulong h = 1469598103934665603;
		foreach (var c in s)
			h = (h ^ c) * 1099511628211;
		return h;
	}

	private static void Shuffle<T>(IList<T> list, RandomNumberGenerator rng)
	{
		for (var i = list.Count - 1; i > 0; i--)
		{
			var j = (int)(rng.Randi() % (uint)(i + 1));
			(list[i], list[j]) = (list[j], list[i]);
		}
	}

}
