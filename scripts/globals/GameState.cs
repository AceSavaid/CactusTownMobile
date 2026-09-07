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

	public static GameState Instance { get; private set; } = null!;

	private Dictionary _data = new();

	public override void _Ready()
	{
		Instance = this;
		LoadGame();
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
	};
}
