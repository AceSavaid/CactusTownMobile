using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CactusTown;

/// <summary>
/// A townsfolk cactus standing by a <see cref="RepairableObject"/>. Built from the
/// same pot / plant / accessory pieces the player customises with, but each NPC
/// rolls its own look — species, accessory, and extra pot/plant colour tints —
/// deterministically from a seed so it stays stable between visits.
/// </summary>
public partial class NpcPlant : Node2D
{
	private static readonly Dictionary<string, Vector2> AnchorOffset = new()
	{
		{ "hat", new Vector2(0, -196) },
		{ "face", new Vector2(0, -132) },
		{ "neck", new Vector2(0, -34) },
		{ "pot", new Vector2(0, 24) },
	};

	private static readonly Color[] PlantTints =
	{
		new(1.00f, 1.00f, 1.00f), new(0.82f, 1.02f, 0.86f), new(0.72f, 0.96f, 1.00f),
		new(1.05f, 0.98f, 0.72f), new(0.94f, 0.86f, 1.05f), new(1.08f, 0.82f, 0.80f),
		new(0.80f, 1.10f, 0.78f), new(1.02f, 1.00f, 0.90f),
	};

	private static readonly Color[] PotTints =
	{
		new(1.00f, 1.00f, 1.00f), new(0.78f, 0.86f, 1.02f), new(1.08f, 0.90f, 0.72f),
		new(0.80f, 1.04f, 0.86f), new(1.10f, 0.78f, 0.74f), new(0.96f, 0.80f, 1.06f),
		new(1.06f, 1.00f, 0.82f), new(0.86f, 0.94f, 0.98f),
	};

	private Sprite2D _pot = null!;
	private Sprite2D _plant = null!;
	private Sprite2D _accessory = null!;

	public override void _Ready()
	{
		_pot = GetNode<Sprite2D>("Pot");
		_plant = GetNode<Sprite2D>("Plant");
		_accessory = GetNode<Sprite2D>("Accessory");
	}

	/// <summary>Roll this NPC's look from a stable seed (usually the fixture's ObjectId).</summary>
	public void Randomize(string seed)
	{
		if (_pot == null)
		{
			_pot = GetNode<Sprite2D>("Pot");
			_plant = GetNode<Sprite2D>("Plant");
			_accessory = GetNode<Sprite2D>("Accessory");
		}

		var rng = new RandomNumberGenerator { Seed = SeedFrom(seed) };

		var pots = PlantCatalog.InSlot(PlantCatalog.Slot.Pot).ToArray();
		var plants = PlantCatalog.InSlot(PlantCatalog.Slot.Plant).ToArray();
		SetTexture(_pot, pots[rng.RandiRange(0, pots.Length - 1)]);
		SetTexture(_plant, plants[rng.RandiRange(0, plants.Length - 1)]);
		_pot.Modulate = PotTints[rng.RandiRange(0, PotTints.Length - 1)];
		_plant.Modulate = PlantTints[rng.RandiRange(0, PlantTints.Length - 1)];

		var accessories = PlantCatalog.InSlot(PlantCatalog.Slot.Accessory)
			.Where(a => a.HasTexture && AnchorOffset.ContainsKey(a.Anchor))
			.ToArray();
		if (accessories.Length > 0 && rng.Randf() < 0.4f)
		{
			var acc = accessories[rng.RandiRange(0, accessories.Length - 1)];
			_accessory.Texture = GD.Load<Texture2D>(acc.TexturePath);
			_accessory.Position = AnchorOffset[acc.Anchor];
			_accessory.Visible = true;
		}
		else
		{
			_accessory.Visible = false;
		}

		var wobble = rng.RandfRange(0.93f, 1.05f);
		Scale = new Vector2(wobble, wobble) * 0.5f;
	}

	private static void SetTexture(Sprite2D target, PlantCatalog.Item item)
	{
		if (item.HasTexture)
			target.Texture = GD.Load<Texture2D>(item.TexturePath);
	}

	private static ulong SeedFrom(string text)
	{
		var hash = 1469598103934665603UL;
		foreach (var c in text)
		{
			hash ^= c;
			hash *= 1099511628211UL;
		}
		return hash;
	}
}
