using System.Linq;
using Godot;
using Godot.Collections;

namespace CactusTown;

/// <summary>
/// Global game state: wallet, plant, materials, town progress.
/// Persists to a single JSON save slot. Autoloaded as <c>GameState</c>.
/// </summary>
public partial class GameState : Node
{
	public const string SavePath = "user://cactus_town_save.json";
	public const int SaveVersion = 1;

	[Signal] public delegate void CoinsChangedEventHandler(int total);
	[Signal] public delegate void MaterialsChangedEventHandler();
	[Signal] public delegate void PlantChangedEventHandler();
	[Signal] public delegate void ObjectsChangedEventHandler();

	public static GameState Instance { get; private set; } = null!;

	private Dictionary _data = new();

	public override void _Ready()
	{
		Instance = this;
		LoadGame();
		RunTownDecay();
	}

	// --- Wallet -----------------------------------------------------------

	public int GetCoins() => _data["coins"].AsInt32();

	public void AddCoins(int amount)
	{
		_data["coins"] = GetCoins() + amount;
		EmitSignal(SignalName.CoinsChanged, GetCoins());
		SaveGame();
	}

	public bool SpendCoins(int amount)
	{
		if (GetCoins() < amount)
			return false;
		_data["coins"] = GetCoins() - amount;
		EmitSignal(SignalName.CoinsChanged, GetCoins());
		SaveGame();
		return true;
	}

	// --- Materials -------------------------------------------------------

	public int GetMaterial(string materialId)
	{
		var materials = _data["materials"].AsGodotDictionary();
		return materials.TryGetValue(materialId, out var value) ? value.AsInt32() : 0;
	}

	public void AddMaterial(string materialId, int amount)
	{
		var materials = _data["materials"].AsGodotDictionary();
		materials[materialId] = GetMaterial(materialId) + amount;
		_data["materials"] = materials;
		EmitSignal(SignalName.MaterialsChanged);
		SaveGame();
	}

	/// <summary>All owned materials as id -> count (only positive entries).</summary>
	public Dictionary GetMaterials() => _data["materials"].AsGodotDictionary();

	public bool HasMaterials(Dictionary required)
	{
		foreach (var (id, count) in required)
		{
			if (GetMaterial(id.AsString()) < count.AsInt32())
				return false;
		}
		return true;
	}

	public bool SpendMaterials(Dictionary required)
	{
		if (!HasMaterials(required))
			return false;
		var materials = _data["materials"].AsGodotDictionary();
		foreach (var (id, count) in required)
			materials[id] = materials[id].AsInt32() - count.AsInt32();
		_data["materials"] = materials;
		EmitSignal(SignalName.MaterialsChanged);
		SaveGame();
		return true;
	}

	// --- Plant ----------------------------------------------------------

	public Dictionary GetPlant() => _data["plant"].AsGodotDictionary();

	public void SetPlantCustomisation(string slot, string variant)
	{
		var plant = GetPlant();
		var custom = plant["customisation"].AsGodotDictionary();
		custom[slot] = variant;
		plant["customisation"] = custom;
		_data["plant"] = plant;
		EmitSignal(SignalName.PlantChanged);
		SaveGame();
	}

	// --- Repairable objects -------------------------------------------

	public string GetObjectState(string objectId)
	{
		var objects = _data["objects"].AsGodotDictionary();
		if (!objects.TryGetValue(objectId, out var entry))
			return "broken";
		var dict = entry.AsGodotDictionary();
		return dict.TryGetValue("state", out var state) ? state.AsString() : "broken";
	}

	public void SetObjectState(string objectId, string state)
	{
		var objects = _data["objects"].AsGodotDictionary();
		var entry = objects.TryGetValue(objectId, out var existing)
			? existing.AsGodotDictionary()
			: new Dictionary();
		entry["state"] = state;
		objects[objectId] = entry;
		_data["objects"] = objects;
		SaveGame();
	}

	public bool IsObjectFixed(string objectId) => GetObjectState(objectId) == "fixed";

	private Dictionary ObjectEntry(string objectId)
	{
		var objects = _data["objects"].AsGodotDictionary();
		var entry = objects.TryGetValue(objectId, out var existing)
			? existing.AsGodotDictionary()
			: new Dictionary();
		objects[objectId] = entry;
		_data["objects"] = objects;
		return entry;
	}

	// --- Object customisation -----------------------------------------

	public int GetObjectVariant(string objectId)
	{
		var objects = _data["objects"].AsGodotDictionary();
		if (!objects.TryGetValue(objectId, out var e))
			return 0;
		return e.AsGodotDictionary().TryGetValue("variant", out var v) ? v.AsInt32() : 0;
	}

	public void SetObjectVariant(string objectId, int index)
	{
		var entry = ObjectEntry(objectId);
		entry["variant"] = index;
		SaveGame();
		EmitSignal(SignalName.ObjectsChanged);
	}

	public bool OwnsVariant(string objectId, int index)
	{
		if (index <= 0)
			return true;
		var objects = _data["objects"].AsGodotDictionary();
		if (!objects.TryGetValue(objectId, out var e) ||
			!e.AsGodotDictionary().TryGetValue("owned", out var owned))
			return false;
		return owned.AsGodotArray().Any(o => o.AsInt32() == index);
	}

	/// <summary>Unlock a variant by paying its cost. Returns false if already owned-check passes but coins fall short.</summary>
	public bool BuyVariant(string objectId, int index, int cost)
	{
		if (OwnsVariant(objectId, index))
			return true;
		if (!SpendCoins(cost))
			return false;
		var entry = ObjectEntry(objectId);
		var owned = entry.TryGetValue("owned", out var o) ? o.AsGodotArray() : new Array();
		owned.Add(index);
		entry["owned"] = owned;
		SaveGame();
		EmitSignal(SignalName.ObjectsChanged);
		return true;
	}

	// --- Town subsections --------------------------------------------

	public bool IsSectionUnlocked(string sectionId)
	{
		var cfg = TownSections.Find(sectionId);
		if (cfg == null || string.IsNullOrEmpty(cfg.UnlockedBy))
			return true;
		return IsSectionCompletedOnce(cfg.UnlockedBy);
	}

	public bool IsSectionCompletedOnce(string sectionId)
	{
		var town = _data["town"].AsGodotDictionary();
		return town.TryGetValue(sectionId, out var e)
			&& e.AsGodotDictionary().TryGetValue("completed_once", out var c) && c.AsBool();
	}

	public void SetSectionCompletedOnce(string sectionId)
	{
		var town = _data["town"].AsGodotDictionary();
		var entry = town.TryGetValue(sectionId, out var ex) ? ex.AsGodotDictionary() : new Dictionary();
		entry["completed_once"] = true;
		if (!entry.ContainsKey("last_decay_day"))
			entry["last_decay_day"] = EstToday();
		town[sectionId] = entry;
		_data["town"] = town;
		SaveGame();
	}

	/// <summary>Today's date in the US Eastern zone (fixed UTC-5), as "YYYY-MM-DD".</summary>
	public static string EstToday()
	{
		var estUnix = Time.GetUnixTimeFromSystem() - 5 * 3600.0;
		var dt = Time.GetDatetimeDictFromUnixTime((long)estUnix);
		return $"{dt["year"].AsInt32():0000}-{dt["month"].AsInt32():00}-{dt["day"].AsInt32():00}";
	}

	/// <summary>
	/// For each fully-restored section, if a new EST day has started, break one
	/// object (25% chance two, 5% chance three). One catch-up event regardless of
	/// how many days passed.
	/// </summary>
	public void RunTownDecay()
	{
		var today = EstToday();
		var town = _data["town"].AsGodotDictionary();
		var changed = false;

		foreach (var section in TownSections.All)
		{
			if (!IsSectionCompletedOnce(section.Id))
				continue;
			var entry = town[section.Id].AsGodotDictionary();
			if ((entry.TryGetValue("last_decay_day", out var last) ? last.AsString() : "") == today)
				continue;

			var fixedIds = section.ObjectIds.Where(IsObjectFixed).ToList();
			if (fixedIds.Count > 0)
			{
				var roll = GD.Randf();
				var count = roll < 0.05f ? 3 : roll < 0.30f ? 2 : 1;
				count = Mathf.Min(count, fixedIds.Count);
				for (var i = 0; i < count; i++)
				{
					var pick = (int)(GD.Randi() % (uint)fixedIds.Count);
					SetObjectState(fixedIds[pick], "broken");
					fixedIds.RemoveAt(pick);
				}
				changed = true;
			}

			entry["last_decay_day"] = today;
			town[section.Id] = entry;
		}

		_data["town"] = town;
		SaveGame();
		if (changed)
			EmitSignal(SignalName.ObjectsChanged);
	}

	// --- Regions -------------------------------------------------------

	public void MarkRegionVisited(string regionId)
	{
		var regions = _data["regions"].AsGodotDictionary();
		if (regions.ContainsKey(regionId))
			return;
		regions[regionId] = new Dictionary { { "visited", true } };
		_data["regions"] = regions;
		SaveGame();
	}

	public bool IsRegionVisited(string regionId) =>
		_data["regions"].AsGodotDictionary().ContainsKey(regionId);

	// --- Persistence --------------------------------------------------

	public void NewGame()
	{
		_data = DefaultData();
		SaveGame();
	}

	public void LoadGame()
	{
		if (!FileAccess.FileExists(SavePath))
		{
			NewGame();
			return;
		}

		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushWarning("Save unreadable, starting fresh.");
			NewGame();
			return;
		}

		var parsed = Json.ParseString(file.GetAsText());
		if (parsed.VariantType != Variant.Type.Dictionary)
		{
			GD.PushWarning("Save corrupt, starting fresh.");
			NewGame();
			return;
		}

		_data = Migrate(parsed.AsGodotDictionary());
	}

	public void SaveGame()
	{
		using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PushError($"Could not open save file for writing: {SavePath}");
			return;
		}

		file.StoreString(Json.Stringify(_data, "\t"));
	}

	private static Dictionary Migrate(Dictionary loaded)
	{
		// Fill any keys added since the save was written.
		var merged = DefaultData();
		merged.Merge(loaded, overwrite: true);
		merged["version"] = SaveVersion;
		return merged;
	}

	private static Dictionary DefaultData() => new()
	{
		{ "version", SaveVersion },
		{ "coins", 0 },
		{
			"plant", new Dictionary
			{
				{ "name", "Spike" },
				{
					"customisation", new Dictionary
					{
						{ "pot", "clay" },
						{ "species", "cactus" },
						{ "hat", "none" },
					}
				},
			}
		},
		{ "materials", new Dictionary() },
		{ "owned_customisations", new Array() },
		{ "areas", new Dictionary() },
		{ "objects", new Dictionary() },
		{ "regions", new Dictionary() },
		{ "town", new Dictionary() },
	};
}
