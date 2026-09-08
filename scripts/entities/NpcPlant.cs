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

	// Applied as Modulate on the base sprites — every NPC gets a visible tint (no
	// pass-through white) so even two same-species cacti read as different people.
	private static readonly Color[] PlantTints =
	{
		new(0.72f, 1.12f, 0.78f), // bright green
		new(0.60f, 1.02f, 0.92f), // teal
		new(1.02f, 1.10f, 0.62f), // chartreuse
		new(0.80f, 0.90f, 1.05f), // blue-green
		new(1.10f, 0.86f, 0.74f), // warm olive
		new(0.78f, 1.06f, 0.64f), // lime
		new(0.58f, 0.86f, 0.66f), // deep pine
		new(1.00f, 0.92f, 0.72f), // golden green
	};

	private static readonly Color[] PotTints =
	{
		new(0.62f, 0.78f, 1.14f), // blue
		new(1.16f, 0.72f, 0.58f), // terracotta+
		new(0.64f, 1.10f, 0.78f), // mint
		new(1.20f, 0.64f, 0.66f), // rose
		new(0.90f, 0.70f, 1.16f), // violet
		new(1.14f, 1.02f, 0.60f), // sand-gold
		new(0.70f, 0.84f, 0.90f), // slate
		new(1.10f, 0.86f, 0.66f), // clay
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
		rng.Randi(); // warm up — PCG's first draw correlates for nearby seeds

		var pots = PlantCatalog.InSlot(PlantCatalog.Slot.Pot).ToArray();
		var plants = PlantCatalog.InSlot(PlantCatalog.Slot.Plant).ToArray();
		SetTexture(_pot, pots[rng.RandiRange(0, pots.Length - 1)]);
		SetTexture(_plant, plants[rng.RandiRange(0, plants.Length - 1)]);
		_pot.Modulate = PotTints[rng.RandiRange(0, PotTints.Length - 1)];
		_plant.Modulate = PlantTints[rng.RandiRange(0, PlantTints.Length - 1)];

		var accessories = PlantCatalog.InSlot(PlantCatalog.Slot.Accessory)
			.Where(a => a.HasTexture && AnchorOffset.ContainsKey(a.Anchor))
			.ToArray();
		if (accessories.Length > 0 && rng.Randf() < 0.33f)
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
		// murmur3 finalizer — avalanche so similar ids ("square_lamp"/"square_bench") diverge
		hash ^= hash >> 33;
		hash *= 0xff51afd7ed558ccdUL;
		hash ^= hash >> 33;
		hash *= 0xc4ceb9fe1a85ec53UL;
		hash ^= hash >> 33;
		return hash;
	}
}
