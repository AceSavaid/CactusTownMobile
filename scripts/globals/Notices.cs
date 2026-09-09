using System;
using System.Collections.Generic;
using System.Linq;

namespace CactusTown;

/// <summary>
/// The Notice Board content. Runs in two modes:
/// <list type="bullet">
///   <item><b>Tutorial</b> — a fixed one-time getting-started list. The board
///   stays in this mode until every tutorial item is claimed.</item>
///   <item><b>Daily</b> — three challenges chosen deterministically from the
///   day's date, refreshed each EST day (see <see cref="GameState.RefreshDailyNotices"/>).</item>
/// </list>
/// Completion is a predicate over <see cref="GameState"/>; rewards are granted by
/// <see cref="GameState.ClaimNotice"/>.
/// </summary>
public static class Notices
{
	public sealed class Item
	{
		public required string Id;
		public required string Title;
		public int Coins;
		public int Seeds;
		public required Func<GameState, bool> Done;
	}

	// ---- tutorial (fixed order, one-time) ---------------------------

	public static readonly Item[] Tutorial =
	{
		new() { Id = "tut:sign",    Title = "Fix the welcome sign",         Coins = 15, Done = g => g.IsObjectFixed("intro_sign") },
		new() { Id = "tut:gather",  Title = "Gather in a material region",  Coins = 15, Done = g => g.HasFlag("gathered") },
		new() { Id = "tut:custom",  Title = "Give your plant a new look",   Coins = 15, Done = g => g.HasFlag("customised") },
		new() { Id = "tut:arcade",  Title = "Win a House mini-game",        Coins = 15, Done = g => g.HasFlag("arcade_win") },
		new() { Id = "tut:section", Title = "Restore a whole town section", Coins = 25, Seeds = 1, Done = g => g.FirstSectionDone },
	};

	public static bool TutorialComplete(GameState g) => Tutorial.All(t => g.IsNoticeClaimed(t.Id));

	// ---- daily challenge templates --------------------------------
	// Each builds an Item scored against the day-start counter snapshot.

	private static readonly Func<GameState, Item>[] DailyTemplates =
	{
		g => Delta(g, "repairs", 2, "Repair 2 fixtures today", 25, 1),
		g => Delta(g, "arcade_wins", 1, "Win a House mini-game today", 20, 1),
		g => Delta(g, "gathers", 3, "Gather three times today", 20, 1),
		g => Delta(g, "restyles", 1, "Re-style a restored fixture", 20, 1),
		g => Delta(g, "coins_earned", 60, "Earn 60 coins today", 0, 2),
		_ => new Item
		{
			Id = "daily:tidy",
			Title = "Keep every restored section tidy",
			Coins = 30, Seeds = 1,
			Done = g => TownSections.All
				.Where(s => g.IsSectionCompletedOnce(s.Id))
				.All(s => s.ObjectIds.All(g.IsObjectFixed)),
		},
	};

	private static Item Delta(GameState g, string stat, int need, string title, int coins, int seeds) => new()
	{
		Id = $"daily:{stat}",
		Title = title,
		Coins = coins,
		Seeds = seeds,
		Done = gs => gs.GetStat(stat) - gs.NoticeSnapshot(stat) >= need,
	};

	public static Item[] DailyFor(GameState g, string estDay)
	{
		// deterministic shuffle of template indices seeded by the date
		var order = Enumerable.Range(0, DailyTemplates.Length).ToList();
		var seed = estDay.Aggregate(2166136261u, (h, c) => (h ^ c) * 16777619u);
		for (var i = order.Count - 1; i > 0; i--)
		{
			seed = seed * 1664525u + 1013904223u;
			var j = (int)(seed % (uint)(i + 1));
			(order[i], order[j]) = (order[j], order[i]);
		}
		return order.Take(3).Select(idx => DailyTemplates[idx](g)).ToArray();
	}

	// ---- what the board shows right now ---------------------------

	public static IReadOnlyList<Item> Current(GameState g) =>
		TutorialComplete(g) ? DailyFor(g, GameState.EstToday()) : Tutorial;

	public static bool InTutorialMode(GameState g) => !TutorialComplete(g);
}
