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
	/// <summary>Save file location. A field, not a const, so the test harness can redirect it.</summary>
	public static string SavePath = "user://cactus_town_save.json";
	public const int SaveVersion = 1;

	[Signal] public delegate void CoinsChangedEventHandler(int total);
	[Signal] public delegate void MaterialsChangedEventHandler();
	[Signal] public delegate void PlantChangedEventHandler();
	[Signal] public delegate void ObjectsChangedEventHandler();
	[Signal] public delegate void SeedsChangedEventHandler(int total);
	[Signal] public delegate void ProgressChangedEventHandler();

	public static GameState Instance { get; private set; } = null!;

	private Dictionary _data = new();

	public override void _Ready()
	{
		Instance = this;
		LoadGame();
		EvaluateStreak();   // judge yesterday's tidiness *before* today's decay
		RunTownDecay();
		RefreshDailyNotices();
	}

	// --- Wallet -----------------------------------------------------------

	public int GetCoins() => _data["coins"].AsInt32();

	public void AddCoins(int amount)
	{
		_data["coins"] = GetCoins() + amount;
		if (amount > 0)
		{
			var stats = _data.TryGetValue("stats", out var s) ? s.AsGodotDictionary() : new Dictionary();
			stats["coins_earned"] = (stats.TryGetValue("coins_earned", out var v) ? v.AsInt32() : 0) + amount;
			_data["stats"] = stats;
		}
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

	public string PlantName => PlantField("name", "Spike");
	public string PlantPot => PlantField("pot", "pot_clay");
	public string PlantSpecies => PlantField("plant", "plant_cactus");
	public string PlantAccessory => PlantField("accessory", "acc_none");

	public string PlantSlotItem(PlantCatalog.Slot slot) => slot switch
	{
		PlantCatalog.Slot.Pot => PlantPot,
		PlantCatalog.Slot.Plant => PlantSpecies,
		_ => PlantAccessory,
	};

	private string PlantField(string key, string fallback)
	{
		var plant = _data["plant"].AsGodotDictionary();
		return plant.TryGetValue(key, out var value) ? value.AsString() : fallback;
	}

	private void SetPlantField(string key, string value)
	{
		var plant = _data["plant"].AsGodotDictionary();
		plant[key] = value;
		_data["plant"] = plant;
		SaveGame();
		EmitSignal(SignalName.PlantChanged);
	}

	public void SetPlantName(string name)
	{
		var trimmed = name.Trim();
		if (trimmed.Length > 16)
			trimmed = trimmed[..16];
		SetPlantField("name", trimmed.Length > 0 ? trimmed : "Spike");
	}

	public void EquipPlantItem(string itemId)
	{
		var item = PlantCatalog.Find(itemId);
		if (item == null)
			return;
		SetPlantField(PlantCatalog.SlotKey(item.Slot), itemId);
		if (item.Cost > 0 || item.Id is not ("pot_clay" or "plant_cactus" or "acc_none"))
			SetFlag("customised");
	}

	private Array PlantOwned()
	{
		var plant = _data["plant"].AsGodotDictionary();
		if (plant.TryGetValue("owned", out var owned))
			return owned.AsGodotArray();
		var fresh = new Array();
		plant["owned"] = fresh;
		_data["plant"] = plant;
		return fresh;
	}

	public bool OwnsPlantItem(string itemId)
	{
		if (PlantCatalog.Find(itemId) is { Cost: <= 0 })
			return true;
		return PlantOwned().Any(x => x.AsString() == itemId);
	}

	public bool BuyPlantItem(string itemId)
	{
		var item = PlantCatalog.Find(itemId);
		if (item == null || OwnsPlantItem(itemId))
			return item != null;
		if (!SpendCoins(item.Cost))
			return false;
		var owned = PlantOwned();
		owned.Add(itemId);
		var plant = _data["plant"].AsGodotDictionary();
		plant["owned"] = owned;
		_data["plant"] = plant;
		SaveGame();
		EmitSignal(SignalName.PlantChanged);
		return true;
	}

	public int PlantCustomizationsUnlocked => PlantOwned().Count;

	// --- Stats --------------------------------------------------------

	public int DaysOnApp
	{
		get
		{
			var first = _data.TryGetValue("first_day", out var f) ? f.AsString() : "";
			if (string.IsNullOrEmpty(first))
				return 1;
			var a = Time.GetUnixTimeFromDatetimeString(first + "T00:00:00");
			var b = Time.GetUnixTimeFromDatetimeString(EstToday() + "T00:00:00");
			return Mathf.Max(1, (int)((b - a) / 86400.0 + 0.5) + 1);
		}
	}

	public (int Fixed, int Total) SectionProgress(string sectionId)
	{
		var cfg = TownSections.Find(sectionId);
		return cfg == null ? (0, 0) : (cfg.ObjectIds.Count(IsObjectFixed), cfg.ObjectIds.Length);
	}

	public int TownCustomizationsUnlocked
	{
		get
		{
			var total = 0;
			foreach (var (_, entry) in _data["objects"].AsGodotDictionary())
				if (entry.AsGodotDictionary().TryGetValue("owned", out var owned))
					total += owned.AsGodotArray().Count;
			return total;
		}
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
		var wasFixed = entry.TryGetValue("state", out var s) && s.AsString() == "fixed";
		entry["state"] = state;
		objects[objectId] = entry;
		_data["objects"] = objects;

		if (state == "fixed" && !wasFixed)
		{
			SetFlag("first_repair");
			BumpStat("repairs");
		}
		SaveGame();
	}

	public bool IsObjectFixed(string objectId) => GetObjectState(objectId) == "fixed";

	// --- Task log --------------------------------------------------

	/// <summary>Remember that the player has taken on this repair task (talked to the cactus).</summary>
	public void RecordTask(string objectId, string displayName, Dictionary requiredMaterials)
	{
		var entry = ObjectEntry(objectId);
		entry["seen"] = true;
		entry["name"] = displayName;
		entry["needs"] = requiredMaterials.Duplicate(true);
		SaveGame();
	}

	/// <summary>Started-but-unfinished repair tasks: {id, name, section, needs}.</summary>
	public Array<Dictionary> OpenTasks()
	{
		var result = new Array<Dictionary>();
		var objects = _data["objects"].AsGodotDictionary();
		foreach (var section in TownSections.All)
		{
			foreach (var id in section.ObjectIds)
			{
				if (!objects.TryGetValue(id, out var raw))
					continue;
				var entry = raw.AsGodotDictionary();
				if (!(entry.TryGetValue("seen", out var seen) && seen.AsBool()) || IsObjectFixed(id))
					continue;
				result.Add(new Dictionary
				{
					{ "id", id },
					{ "name", entry.TryGetValue("name", out var nm) ? nm : id },
					{ "section", section.Name },
					{ "needs", entry.TryGetValue("needs", out var nd) ? nd : new Dictionary() },
				});
			}
		}
		return result;
	}

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
		var changed = (entry.TryGetValue("variant", out var v) ? v.AsInt32() : 0) != index;
		entry["variant"] = index;
		SaveGame();
		if (changed && index > 0)
			BumpStat("restyles");
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

	// --- Onboarding & progression -----------------------------------

	/// <summary>Seeds — the premium currency, earned from notices and streak milestones.</summary>
	public int Seeds => _data.TryGetValue("seeds", out var s) ? s.AsInt32() : 0;

	public void AddSeeds(int amount)
	{
		if (amount == 0)
			return;
		_data["seeds"] = Seeds + amount;
		EmitSignal(SignalName.SeedsChanged, Seeds);
		SaveGame();
	}

	private Array Flags => _data.TryGetValue("flags", out var f) ? f.AsGodotArray() : new Array();

	public bool HasFlag(string flag) => Flags.Any(x => x.AsString() == flag);

	/// <summary>Set a one-way onboarding flag (idempotent).</summary>
	public void SetFlag(string flag)
	{
		var flags = Flags;
		if (flags.Any(x => x.AsString() == flag))
			return;
		flags.Add(flag);
		_data["flags"] = flags;
		SaveGame();
		EmitSignal(SignalName.ProgressChanged);
	}

	public int GetStat(string key)
	{
		var stats = _data.TryGetValue("stats", out var s) ? s.AsGodotDictionary() : new Dictionary();
		return stats.TryGetValue(key, out var v) ? v.AsInt32() : 0;
	}

	public void BumpStat(string key, int by = 1)
	{
		var stats = _data.TryGetValue("stats", out var s) ? s.AsGodotDictionary() : new Dictionary();
		stats[key] = (stats.TryGetValue(key, out var v) ? v.AsInt32() : 0) + by;
		_data["stats"] = stats;
		SaveGame();
		EmitSignal(SignalName.ProgressChanged);
	}

	/// <summary>Called from a region when a gather mini-game pays out.</summary>
	public void RecordGather()
	{
		SetFlag("gathered");
		BumpStat("gathers");
	}

	/// <summary>Called from the arcade when the player wins a round.</summary>
	public void RecordArcadeWin()
	{
		SetFlag("arcade_win");
		BumpStat("arcade_wins");
	}

	public bool FirstRepairDone => HasFlag("first_repair");
	public bool FirstSectionDone => TownSections.All.Any(s => IsSectionCompletedOnce(s.Id));

	// --- Bloom Score ------------------------------------------------

	private static readonly string[] BloomTierNames = { "Dusty", "Sprouting", "Growing", "Blooming", "Flourishing" };

	/// <summary>
	/// 0..1 measure of how restored the whole town is: half from sections
	/// completed, a third from re-styled fixtures, the rest from keeping it tidy.
	/// </summary>
	public float BloomScore
	{
		get
		{
			var sections = TownSections.All;
			var completed = sections.Count(s => IsSectionCompletedOnce(s.Id));
			var sectionScore = completed / (float)sections.Length;

			var totalObjects = sections.Sum(s => s.ObjectIds.Length);
			var maxVariants = totalObjects * 2;
			var variantScore = maxVariants == 0
				? 0f
				: Mathf.Min(1f, TownCustomizationsUnlocked / (float)maxVariants);

			var tidyScore = completed > 0 && TownIsTidy ? 1f : 0f;

			return sectionScore * 0.5f + variantScore * 0.35f + tidyScore * 0.15f;
		}
	}

	public int BloomPercent => Mathf.RoundToInt(BloomScore * 100f);

	/// <summary>0 (Dusty) … 4 (Flourishing), crossing at 20 / 45 / 70 / 100%.</summary>
	public int BloomTier => BloomPercent >= 100 ? 4
		: BloomPercent >= 70 ? 3
		: BloomPercent >= 45 ? 2
		: BloomPercent >= 20 ? 1
		: 0;

	public string BloomTierName => BloomTierNames[BloomTier];

	// --- Notice board ----------------------------------------------

	private Dictionary NoticeData => _data.TryGetValue("notices", out var n)
		? n.AsGodotDictionary()
		: new Dictionary { { "day", "" }, { "claimed", new Array() }, { "snapshot", new Dictionary() } };

	public bool IsNoticeClaimed(string id) =>
		NoticeData["claimed"].AsGodotArray().Any(x => x.AsString() == id);

	/// <summary>Grant a completed notice's reward once.</summary>
	public void ClaimNotice(string id, int coins, int seeds)
	{
		var notices = NoticeData;
		var claimed = notices["claimed"].AsGodotArray();
		if (claimed.Any(x => x.AsString() == id))
			return;
		claimed.Add(id);
		notices["claimed"] = claimed;
		_data["notices"] = notices;
		SaveGame();
		if (coins > 0) AddCoins(coins);
		if (seeds > 0) AddSeeds(seeds);
		EmitSignal(SignalName.ProgressChanged);
	}

	/// <summary>Day-start counter snapshot used to score "…today" daily notices.</summary>
	public int NoticeSnapshot(string key)
	{
		var snap = NoticeData["snapshot"].AsGodotDictionary();
		return snap.TryGetValue(key, out var v) ? v.AsInt32() : 0;
	}

	/// <summary>Roll the daily notice set if the EST day changed (no-op in tutorial mode).</summary>
	public void RefreshDailyNotices()
	{
		if (!Notices.TutorialComplete(this))
			return;
		var notices = NoticeData;
		var today = EstToday();
		if (notices["day"].AsString() == today)
			return;

		notices["day"] = today;
		notices["snapshot"] = new Dictionary
		{
			{ "repairs", GetStat("repairs") },
			{ "arcade_wins", GetStat("arcade_wins") },
			{ "gathers", GetStat("gathers") },
			{ "restyles", GetStat("restyles") },
			{ "coins_earned", GetStat("coins_earned") },
		};
		// drop yesterday's daily claims; keep the tutorial ones
		var kept = new Array();
		foreach (var c in notices["claimed"].AsGodotArray())
			if (!c.AsString().StartsWith("daily:"))
				kept.Add(c);
		notices["claimed"] = kept;

		_data["notices"] = notices;
		SaveGame();
		EmitSignal(SignalName.ProgressChanged);
	}

	// --- Days Tended streak --------------------------------------

	/// <summary>Seed rewards for reaching a streak length. Granted once each.</summary>
	private static readonly (int Days, int Seeds)[] StreakMilestones =
		{ (3, 2), (7, 4), (14, 6), (30, 12), (60, 20), (100, 40) };

	private Dictionary StreakData => _data.TryGetValue("streak", out var s)
		? s.AsGodotDictionary()
		: new Dictionary { { "count", 0 }, { "best", 0 }, { "day", "" }, { "grace", false }, { "milestones", new Array() } };

	public int StreakCount => StreakData["count"].AsInt32();
	public int StreakBest => StreakData["best"].AsInt32();

	/// <summary>True when every restored section is currently fully fixed (nothing decayed outstanding).</summary>
	public bool TownIsTidy => TownSections.All
		.Where(s => IsSectionCompletedOnce(s.Id))
		.SelectMany(s => s.ObjectIds)
		.All(IsObjectFixed);

	/// <summary>
	/// Once per EST day: if the town was tidy, the streak grows (with milestone
	/// rewards); if it wasn't, one missed day is forgiven, a second resets it.
	/// Call before <see cref="RunTownDecay"/> so today's fresh breaks don't count
	/// against yesterday.
	/// </summary>
	public void EvaluateStreak(string? todayOverride = null)
	{
		var streak = StreakData;
		var today = todayOverride ?? EstToday();
		if (streak["day"].AsString() == today)
			return;

		var everCompleted = TownSections.All.Any(s => IsSectionCompletedOnce(s.Id));
		var firstEval = string.IsNullOrEmpty(streak["day"].AsString());

		if (!everCompleted)
		{
			streak["day"] = today;   // nothing to tend yet
			_data["streak"] = streak;
			SaveGame();
			return;
		}

		if (TownIsTidy)
		{
			var count = streak["count"].AsInt32() + 1;
			streak["count"] = count;
			streak["grace"] = false;
			streak["best"] = Mathf.Max(streak["best"].AsInt32(), count);
			GrantStreakMilestones(streak, count);
		}
		else if (!firstEval)
		{
			if (!streak["grace"].AsBool())
				streak["grace"] = true;              // one forgiven day
			else
			{
				streak["count"] = 0;
				streak["grace"] = false;
			}
		}

		streak["day"] = today;
		_data["streak"] = streak;
		SaveGame();
		EmitSignal(SignalName.ProgressChanged);
	}

	private void GrantStreakMilestones(Dictionary streak, int count)
	{
		var claimed = streak["milestones"].AsGodotArray();
		var seeds = 0;
		foreach (var (days, reward) in StreakMilestones)
		{
			if (count < days || claimed.Any(x => x.AsInt32() == days))
				continue;
			claimed.Add(days);
			seeds += reward;
		}
		streak["milestones"] = claimed;
		if (seeds > 0)
		{
			AddSeeds(seeds);
			Router.Instance?.Toast($"{count}-day streak!  +{seeds} seeds");
		}
	}

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
		{ "first_day", EstToday() },
		{
			"plant", new Dictionary
			{
				{ "name", "Spike" },
				{ "pot", "pot_clay" },
				{ "plant", "plant_cactus" },
				{ "accessory", "acc_none" },
				{ "owned", new Array() },
			}
		},
		{ "materials", new Dictionary() },
		{ "areas", new Dictionary() },
		{ "objects", new Dictionary() },
		{ "regions", new Dictionary() },
		{ "town", new Dictionary() },
		{ "seeds", 0 },
		{ "flags", new Array() },
		{ "stats", new Dictionary() },
		{ "notices", new Dictionary { { "day", "" }, { "claimed", new Array() }, { "snapshot", new Dictionary() } } },
		{ "streak", new Dictionary { { "count", 0 }, { "best", 0 }, { "day", "" }, { "grace", false }, { "milestones", new Array() } } },
	};
}
