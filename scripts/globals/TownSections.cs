using System.Linq;

namespace CactusTown;

/// <summary>
/// Static definition of the town's subsections: display name, unlock chain, the
/// objects each contains, and its scene. Decay and the town map read this without
/// loading section scenes.
/// </summary>
public static class TownSections
{
	public sealed class Config
	{
		public required string Id;
		public required string Name;
		/// <summary>Section that must be completed once before this one unlocks ("" = open from start).</summary>
		public string UnlockedBy = "";
		public required string SceneFile;
		public required string[] ObjectIds;

		public string ScenePath => $"res://scenes/town/{SceneFile}.tscn";
	}

	public static readonly Config[] All =
	{
		new()
		{
			Id = "main_square", Name = "Main Square", SceneFile = "MainSquare",
			ObjectIds = new[] { "square_fountain", "square_statue", "square_lamp", "square_bench" },
		},
		new()
		{
			Id = "garden", Name = "Garden", UnlockedBy = "main_square", SceneFile = "Garden",
			ObjectIds = new[] { "garden_flowerbed", "garden_trellis", "garden_pond" },
		},
		new()
		{
			Id = "park", Name = "Park", UnlockedBy = "garden", SceneFile = "Park",
			ObjectIds = new[] { "park_pavilion", "park_swings", "park_pond", "park_path", "park_sign" },
		},
		new()
		{
			Id = "shopping", Name = "Shopping District", UnlockedBy = "main_square", SceneFile = "Shopping",
			ObjectIds = new[] { "shop_awning", "shop_sign", "shop_window", "shop_crates", "shop_lamp" },
		},
		new()
		{
			Id = "housing", Name = "Housing", UnlockedBy = "shopping", SceneFile = "Housing",
			ObjectIds = new[] { "house_roof", "house_fence", "house_mailbox", "house_garden", "house_door" },
		},
		new()
		{
			Id = "business", Name = "Business Quarter", UnlockedBy = "housing", SceneFile = "Business",
			ObjectIds = new[] { "biz_doors", "biz_sign", "biz_planters", "biz_bench", "biz_clock" },
		},
	};

	public static Config? Find(string id) => All.FirstOrDefault(c => c.Id == id);
}
