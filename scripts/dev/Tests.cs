using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace CactusTown;

/// <summary>
/// Lightweight headless test runner — no third-party framework. Add a
/// <c>[Test] void SomeCheck()</c> method and it runs. Failures are collected;
/// the process exits non-zero if any fail.
///
///   godot --headless --path . scenes/dev/Tests.tscn
///
/// GameState is redirected to a throwaway save so the real one is untouched.
/// </summary>
public partial class Tests : Node
{
	[AttributeUsage(AttributeTargets.Method)]
	private sealed class TestAttribute : Attribute { }

	private const string TestSave = "user://__tests_save.json";

	private readonly List<string> _failures = new();
	private int _passed;
	private string _current = "";

	public override async void _Ready()
	{
		GD.Print("── Cactus Town tests ──");

		GameState.SavePath = TestSave;
		DeleteTestSave();
		ResetState();

		// let the scene-tree settle before instancing scenes
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		foreach (var method in GetType()
			         .GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
			         .Where(m => m.GetCustomAttributes(typeof(TestAttribute), false).Length > 0)
			         .OrderBy(m => m.Name))
		{
			_current = method.Name;
			var before = _failures.Count;
			try
			{
				ResetState();
				method.Invoke(this, null);
			}
			catch (Exception e)
			{
				Fail($"threw {(e.InnerException ?? e).GetType().Name}: {(e.InnerException ?? e).Message}");
			}

			if (_failures.Count == before)
			{
				_passed++;
				GD.Print($"  PASS  {method.Name}");
			}
		}

		DeleteTestSave();

		GD.Print($"── {_passed} passed, {_failures.Count} failed ──");
		foreach (var f in _failures)
			GD.PrintErr("  FAIL  " + f);

		GetTree().Quit(_failures.Count == 0 ? 0 : 1);
	}

	// ---- assertions ----------------------------------------------------

	private void Ok(bool condition, string message)
	{
		if (!condition)
			Fail(message);
	}

	private void Eq(object actual, object expected, string message)
	{
		if (!Equals(actual, expected))
			Fail($"{message} — expected <{expected}>, got <{actual}>");
	}

	private void Fail(string message) => _failures.Add($"{_current}: {message}");

	// ---- helpers -----------------------------------------------------

	private static void DeleteTestSave()
	{
		if (FileAccess.FileExists(TestSave))
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(TestSave));
	}

	private static void ResetState() => GameState.Instance.NewGame();

	private static IEnumerable<string> AllSceneFiles()
	{
		var found = new List<string>();
		Walk("res://scenes");
		Walk("res://ui");
		return found;

		void Walk(string dir)
		{
			using var d = DirAccess.Open(dir);
			if (d == null)
				return;
			foreach (var sub in d.GetDirectories())
				Walk($"{dir}/{sub}");
			foreach (var file in d.GetFiles())
				if (file.EndsWith(".tscn"))
					found.Add($"{dir}/{file}");
		}
	}

	// ================================================================
	//  Catalog integrity
	// ================================================================

	[Test]
	private void PlantCatalog_ids_unique_and_textures_exist()
	{
		var ids = PlantCatalog.All.Select(i => i.Id).ToList();
		Eq(ids.Count, ids.Distinct().Count(), "duplicate plant item id");

		foreach (var item in PlantCatalog.All.Where(i => i.HasTexture))
			Ok(ResourceLoader.Exists(item.TexturePath), $"missing texture {item.TexturePath}");

		foreach (var slot in Enum.GetValues<PlantCatalog.Slot>())
			Ok(PlantCatalog.InSlot(slot).Any(), $"no items in slot {slot}");

		foreach (var id in new[] { "pot_clay", "plant_cactus", "acc_none" })
			Ok(PlantCatalog.Find(id) is { Cost: <= 0 }, $"default item {id} missing or not free");
	}

	[Test]
	private void ArcadeCatalog_scenes_and_icons_exist()
	{
		var ids = ArcadeCatalog.All.Select(e => e.Id).ToList();
		Eq(ids.Count, ids.Distinct().Count(), "duplicate arcade id");

		foreach (var entry in ArcadeCatalog.All)
		{
			Ok(ResourceLoader.Exists(entry.ScenePath), $"missing arcade scene {entry.ScenePath}");
			Ok(ResourceLoader.Exists(entry.IconPath), $"missing arcade icon {entry.IconPath}");
		}
	}

	[Test]
	private void TownSections_config_is_consistent()
	{
		Eq(TownSections.All.Length, 6, "expected 6 town sections");
		var ids = TownSections.All.Select(s => s.Id).ToHashSet();

		foreach (var section in TownSections.All)
		{
			Ok(ResourceLoader.Exists(section.ScenePath), $"missing section scene {section.ScenePath}");
			Ok(section.ObjectIds.Length is >= 3 and <= 6, $"{section.Id} has {section.ObjectIds.Length} objects");
			Ok(section.ObjectIds.Distinct().Count() == section.ObjectIds.Length, $"{section.Id} has duplicate object ids");
			Ok(string.IsNullOrEmpty(section.UnlockedBy) || ids.Contains(section.UnlockedBy),
				$"{section.Id}.UnlockedBy '{section.UnlockedBy}' is not a section");
		}

		Ok(TownSections.All.Count(s => string.IsNullOrEmpty(s.UnlockedBy)) >= 1, "no starting section");

		// unlock graph has no cycles
		foreach (var section in TownSections.All)
		{
			var seen = new HashSet<string>();
			var cur = section;
			while (cur != null && !string.IsNullOrEmpty(cur.UnlockedBy))
			{
				Ok(seen.Add(cur.Id), $"unlock cycle through {cur.Id}");
				cur = TownSections.Find(cur.UnlockedBy);
			}
		}

		var allObjectIds = TownSections.All.SelectMany(s => s.ObjectIds).ToList();
		Eq(allObjectIds.Count, allObjectIds.Distinct().Count(), "object id reused across sections");
	}

	[Test]
	private void TownSection_scenes_contain_their_configured_objects()
	{
		foreach (var section in TownSections.All)
		{
			var scene = GD.Load<PackedScene>(section.ScenePath).Instantiate();
			var inScene = scene.FindChildren("*", recursive: true)
				.OfType<RepairableObject>()
				.Where(o => !o.ExcludeFromCompletion)
				.Select(o => o.ObjectId)
				.ToHashSet();
			scene.QueueFree();

			foreach (var id in section.ObjectIds)
				Ok(inScene.Contains(id), $"{section.Id}: object '{id}' in config but not in scene");
			Eq(inScene.Count, section.ObjectIds.Length, $"{section.Id}: scene object count");
		}
	}

	[Test]
	private void Materials_ids_unique()
	{
		Eq(Materials.All.Length, Materials.All.Distinct().Count(), "duplicate material id");
		foreach (var id in Materials.All)
			Ok(Materials.DisplayName(id) != id, $"material {id} has no display name");
	}

	// ================================================================
	//  GameState — wallet & materials
	// ================================================================

	[Test]
	private void Wallet_add_and_spend()
	{
		var gs = GameState.Instance;
		Eq(gs.GetCoins(), 0, "fresh save starts at 0 coins");

		gs.AddCoins(100);
		Eq(gs.GetCoins(), 100, "after AddCoins(100)");

		Ok(gs.SpendCoins(30), "SpendCoins(30) succeeds");
		Eq(gs.GetCoins(), 70, "balance after spend");

		Ok(!gs.SpendCoins(999), "SpendCoins beyond balance fails");
		Eq(gs.GetCoins(), 70, "failed spend does not change balance");
	}

	[Test]
	private void Materials_add_has_and_spend()
	{
		var gs = GameState.Instance;
		gs.AddMaterial(Materials.Wood, 5);
		gs.AddMaterial(Materials.Stone, 2);
		Eq(gs.GetMaterial(Materials.Wood), 5, "wood count");

		var need = new Dictionary { { Materials.Wood, 4 }, { Materials.Stone, 2 } };
		Ok(gs.HasMaterials(need), "has enough for the recipe");
		Ok(gs.SpendMaterials(need), "spend the recipe");
		Eq(gs.GetMaterial(Materials.Wood), 1, "wood left after spend");
		Ok(!gs.HasMaterials(need), "no longer has enough");
	}

	// ================================================================
	//  GameState — plant
	// ================================================================

	[Test]
	private void Plant_buy_equip_and_no_double_charge()
	{
		var gs = GameState.Instance;
		gs.AddCoins(200);

		var hat = PlantCatalog.Find("acc_hat")!;
		Ok(!gs.OwnsPlantItem("acc_hat"), "does not own the hat yet");
		Ok(gs.BuyPlantItem("acc_hat"), "buy the hat");
		Eq(gs.GetCoins(), 200 - hat.Cost, "hat cost deducted");
		Ok(gs.OwnsPlantItem("acc_hat"), "owns the hat now");

		var balance = gs.GetCoins();
		Ok(gs.BuyPlantItem("acc_hat"), "buying an owned item is a no-op success");
		Eq(gs.GetCoins(), balance, "no second charge");

		gs.EquipPlantItem("acc_hat");
		Eq(gs.PlantAccessory, "acc_hat", "accessory equipped");

		gs.SetPlantName("   Prickles the Great and Powerful   ");
		Ok(gs.PlantName.Length <= 16, "plant name is clamped to 16 chars");
	}

	// ================================================================
	//  GameState — town progress
	// ================================================================

	[Test]
	private void Section_unlock_chain()
	{
		var gs = GameState.Instance;
		Ok(gs.IsSectionUnlocked("main_square"), "main square open from the start");
		Ok(!gs.IsSectionUnlocked("garden"), "garden locked until main square done");

		gs.SetSectionCompletedOnce("main_square");
		Ok(gs.IsSectionCompletedOnce("main_square"), "main square recorded complete");
		Ok(gs.IsSectionUnlocked("garden"), "garden unlocks");
		Ok(gs.IsSectionUnlocked("shopping"), "shopping also gated on main square");
		Ok(!gs.IsSectionUnlocked("business"), "business still locked");
	}

	[Test]
	private void Object_state_and_section_progress()
	{
		var gs = GameState.Instance;
		var section = TownSections.Find("garden")!;

		Eq(gs.GetObjectState(section.ObjectIds[0]), "broken", "objects start broken");
		Ok(!gs.IsObjectFixed(section.ObjectIds[0]), "not fixed");

		foreach (var id in section.ObjectIds)
			gs.SetObjectState(id, "fixed");

		var (done, total) = gs.SectionProgress("garden");
		Eq(done, total, "all garden objects fixed");
		Eq(total, section.ObjectIds.Length, "progress total matches config");
	}

	[Test]
	private void Variant_purchase_gates_on_coins()
	{
		var gs = GameState.Instance;
		Ok(gs.OwnsVariant("square_bench", 0), "base variant always owned");
		Ok(!gs.OwnsVariant("square_bench", 1), "variant 1 not owned yet");

		Ok(!gs.BuyVariant("square_bench", 1, 50), "cannot buy without coins");
		gs.AddCoins(50);
		Ok(gs.BuyVariant("square_bench", 1, 50), "buy variant 1");
		Eq(gs.GetCoins(), 0, "coins spent");
		Ok(gs.OwnsVariant("square_bench", 1), "owns variant 1");
	}

	// ================================================================
	//  GameState — persistence & decay
	// ================================================================

	[Test]
	private void Save_round_trips()
	{
		var gs = GameState.Instance;
		gs.AddCoins(123);
		gs.AddMaterial(Materials.IronOre, 7);
		gs.EquipPlantItem("plant_barrel");
		gs.SetObjectState("square_fountain", "fixed");
		gs.SetSectionCompletedOnce("main_square");
		gs.SaveGame();

		gs.LoadGame();
		Eq(gs.GetCoins(), 123, "coins survived reload");
		Eq(gs.GetMaterial(Materials.IronOre), 7, "materials survived reload");
		Eq(gs.PlantSpecies, "plant_barrel", "plant survived reload");
		Ok(gs.IsObjectFixed("square_fountain"), "object state survived reload");
		Ok(gs.IsSectionCompletedOnce("main_square"), "section completion survived reload");
	}

	[Test]
	private void Decay_is_idempotent_within_a_day()
	{
		var gs = GameState.Instance;
		var section = TownSections.Find("main_square")!;
		foreach (var id in section.ObjectIds)
			gs.SetObjectState(id, "fixed");
		gs.SetSectionCompletedOnce("main_square");   // stamps last_decay_day = today

		gs.RunTownDecay();
		var fixedAfterFirst = section.ObjectIds.Count(gs.IsObjectFixed);
		gs.RunTownDecay();
		var fixedAfterSecond = section.ObjectIds.Count(gs.IsObjectFixed);

		Eq(fixedAfterSecond, fixedAfterFirst, "second decay on the same day changes nothing");
	}

	[Test]
	private void Decay_leaves_incomplete_sections_alone()
	{
		var gs = GameState.Instance;
		// nothing completed
		foreach (var id in TownSections.Find("garden")!.ObjectIds)
			gs.SetObjectState(id, "fixed");

		gs.RunTownDecay();

		foreach (var id in TownSections.Find("garden")!.ObjectIds)
			Ok(gs.IsObjectFixed(id), "no decay without a first completion");
	}

	// ================================================================
	//  Onboarding & notices
	// ================================================================

	[Test]
	private void FirstRepair_flag_and_progressive_gates()
	{
		var gs = GameState.Instance;
		Ok(!gs.FirstRepairDone, "no repairs on a fresh save");
		Ok(!gs.FirstSectionDone, "no sections done");

		gs.SetObjectState("square_lamp", "fixed");
		Ok(gs.FirstRepairDone, "first_repair flag set on first fix");
		Eq(gs.GetStat("repairs"), 1, "repair counter bumped");

		gs.SetObjectState("square_lamp", "fixed");
		Eq(gs.GetStat("repairs"), 1, "re-fixing the same object doesn't double-count");
	}

	[Test]
	private void Seeds_add_and_signal_value()
	{
		var gs = GameState.Instance;
		Eq(gs.Seeds, 0, "fresh save has no seeds");
		gs.AddSeeds(3);
		Eq(gs.Seeds, 3, "seeds added");
	}

	[Test]
	private void Notices_start_in_tutorial_then_switch_to_daily()
	{
		var gs = GameState.Instance;
		Ok(Notices.InTutorialMode(gs), "fresh save shows the tutorial list");
		Eq(Notices.Current(gs).Count, Notices.Tutorial.Length, "tutorial list length");

		foreach (var t in Notices.Tutorial)
			gs.ClaimNotice(t.Id, 0, 0);   // claim without needing the predicate for this test

		Ok(Notices.TutorialComplete(gs), "tutorial complete once all claimed");
		Ok(!Notices.InTutorialMode(gs), "board switched to daily mode");
		Eq(Notices.DailyFor(gs, "2026-09-09").Length, 3, "three daily notices");
	}

	[Test]
	private void Notice_claim_pays_once()
	{
		var gs = GameState.Instance;
		Ok(!gs.IsNoticeClaimed("tut:sign"), "not claimed yet");
		gs.ClaimNotice("tut:sign", 15, 0);
		Eq(gs.GetCoins(), 15, "reward paid");
		Ok(gs.IsNoticeClaimed("tut:sign"), "marked claimed");

		gs.ClaimNotice("tut:sign", 15, 0);
		Eq(gs.GetCoins(), 15, "no second payout");
	}

	[Test]
	private void Daily_notices_are_deterministic_per_day()
	{
		var gs = GameState.Instance;
		var a1 = Notices.DailyFor(gs, "2026-09-09").Select(n => n.Id).ToArray();
		var a2 = Notices.DailyFor(gs, "2026-09-09").Select(n => n.Id).ToArray();
		var b = Notices.DailyFor(gs, "2026-09-10").Select(n => n.Id).ToArray();
		Ok(a1.SequenceEqual(a2), "same day → same challenges");
		Ok(!a1.SequenceEqual(b), "different day → different challenges");
		Eq(a1.Distinct().Count(), a1.Length, "no duplicate challenges in a day");
	}

	// ================================================================
	//  Scenes
	// ================================================================

	[Test]
	private void Every_scene_instantiates()
	{
		foreach (var path in AllSceneFiles())
		{
			if (path.Contains("/dev/"))
				continue;
			var packed = GD.Load<PackedScene>(path);
			Ok(packed != null, $"could not load {path}");
			if (packed == null)
				continue;
			try
			{
				var node = packed.Instantiate();
				node.QueueFree();
			}
			catch (Exception e)
			{
				Fail($"{path} — instantiate threw {e.GetType().Name}: {e.Message}");
			}
		}
	}
}
