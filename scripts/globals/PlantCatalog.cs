using System.Collections.Generic;
using System.Linq;

namespace CactusTown;

/// <summary>
/// Every plant customisation item: pots, plant species, and accessories.
/// Accessories render at a fixed <see cref="Item.Anchor"/> on the plant or pot.
/// Add an entry + a sprite in <c>assets/sprites/plant/</c> to add an option.
/// </summary>
public static class PlantCatalog
{
	public enum Slot { Pot, Plant, Accessory }

	public sealed class Item
	{
		public required string Id;
		public required Slot Slot;
		public required string Name;
		public required string TextureFile;
		public int Cost;

		/// <summary>Accessories only: "hat" | "face" | "neck" | "pot" | "aura". Empty = no art.</summary>
		public string Anchor = "";

		public bool HasTexture => TextureFile.Length > 0;
		public string TexturePath => $"res://assets/sprites/plant/{TextureFile}.svg";
	}

	public static readonly Item[] All =
	{
		new() { Id = "pot_clay",  Slot = Slot.Pot, Name = "Clay Pot",  TextureFile = "pot_clay" },
		new() { Id = "pot_slate", Slot = Slot.Pot, Name = "Slate Pot", TextureFile = "pot_slate", Cost = 40 },
		new() { Id = "pot_cream", Slot = Slot.Pot, Name = "Cream Pot", TextureFile = "pot_cream", Cost = 60 },
		new() { Id = "pot_moss",  Slot = Slot.Pot, Name = "Moss Pot",  TextureFile = "pot_moss",  Cost = 80 },

		new() { Id = "plant_cactus", Slot = Slot.Plant, Name = "Cactus",             TextureFile = "plant_cactus" },
		new() { Id = "plant_barrel", Slot = Slot.Plant, Name = "Barrel Cactus",      TextureFile = "plant_barrel", Cost = 50 },
		new() { Id = "plant_aloe",   Slot = Slot.Plant, Name = "Aloe",               TextureFile = "plant_aloe",   Cost = 75 },
		new() { Id = "plant_bloom",  Slot = Slot.Plant, Name = "Blooming Succulent", TextureFile = "plant_bloom",  Cost = 100 },

		new() { Id = "acc_none",    Slot = Slot.Accessory, Name = "None",     TextureFile = "" },
		new() { Id = "acc_hat",     Slot = Slot.Accessory, Name = "Top Hat",  TextureFile = "acc_hat",     Cost = 40, Anchor = "hat" },
		new() { Id = "acc_shades",  Slot = Slot.Accessory, Name = "Shades",   TextureFile = "acc_shades",  Cost = 60, Anchor = "face" },
		new() { Id = "acc_bowtie",  Slot = Slot.Accessory, Name = "Bow Tie",  TextureFile = "acc_bowtie",  Cost = 50, Anchor = "neck" },
		new() { Id = "acc_sparkle", Slot = Slot.Accessory, Name = "Sparkles", TextureFile = "acc_sparkle", Cost = 90, Anchor = "aura" },
	};

	public static Item? Find(string id) => All.FirstOrDefault(i => i.Id == id);

	public static IEnumerable<Item> InSlot(Slot slot) => All.Where(i => i.Slot == slot);

	public static string SlotKey(Slot slot) => slot switch
	{
		Slot.Pot => "pot",
		Slot.Plant => "plant",
		_ => "accessory",
	};

	public static int PurchasableCount => All.Count(i => i.Cost > 0);
}
