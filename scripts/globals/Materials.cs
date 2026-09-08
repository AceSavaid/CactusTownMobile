using System.Collections.Generic;

namespace CactusTown;

/// <summary>Material ids and their display names. Ids are the keys used in the save file.</summary>
public static class Materials
{
	public const string Wood = "wood";
	public const string Stick = "stick";
	public const string FlowerRed = "flower_red";
	public const string FlowerYellow = "flower_yellow";
	public const string FlowerBlue = "flower_blue";
	public const string Water = "water";
	public const string Stone = "stone";
	public const string IronOre = "iron_ore";
	public const string GoldOre = "gold_ore";

	public static readonly string[] All =
	{
		Wood, Stick, FlowerRed, FlowerYellow, FlowerBlue, Water, Stone, IronOre, GoldOre,
	};

	private static readonly Dictionary<string, string> Names = new()
	{
		{ Wood, "Wood" },
		{ Stick, "Stick" },
		{ FlowerRed, "Red Flower" },
		{ FlowerYellow, "Yellow Flower" },
		{ FlowerBlue, "Blue Flower" },
		{ Water, "Water" },
		{ Stone, "Stone" },
		{ IronOre, "Iron Ore" },
		{ GoldOre, "Gold Ore" },
	};

	public static string DisplayName(string id) => Names.TryGetValue(id, out var name) ? name : id;
}
