using System.Linq;

namespace CactusTown;

/// <summary>
/// Sage's lines. During onboarding they follow the getting-started sequence;
/// afterwards they're progress-aware nudges toward whatever's next.
/// </summary>
public static class MentorTips
{
	public static string Line(GameState g)
	{
		if (!g.IsObjectFixed("intro_sign"))
			return "Welcome to Cactus Town! The old sign beside me needs mending. " +
			       "Read the Notice Board, then gather what it asks for out in the regions.";

		if (!g.HasFlag("gathered"))
			return "Wood, stone, water, flowers, ore — it all comes from the four regions. " +
			       "Head out through the Town map and bring some back.";

		if (!g.HasFlag("customised"))
			return "You've a few coins now. Spend them on your own plant back home — " +
			       "a new pot, or a little hat.";

		if (!g.HasFlag("arcade_win"))
			return "There are games to play in your house. Win one and it pays a coin or three.";

		if (!g.FirstSectionDone)
		{
			var next = TownSections.All.FirstOrDefault(s =>
				g.IsSectionUnlocked(s.Id) && !g.IsSectionCompletedOnce(s.Id));
			return next == null
				? "Keep mending — the town's brighter already."
				: $"Fix every fixture in the {next.Name} and the next part of town opens up.";
		}

		// --- post-tutorial ---
		var locked = TownSections.All.FirstOrDefault(s =>
			!g.IsSectionCompletedOnce(s.Id) && g.IsSectionUnlocked(s.Id));
		if (locked != null)
		{
			var (done, total) = g.SectionProgress(locked.Id);
			return $"The {locked.Name} is {done} of {total} restored. Keep at it.";
		}

		var decayed = TownSections.All
			.Where(s => g.IsSectionCompletedOnce(s.Id))
			.SelectMany(s => s.ObjectIds)
			.Any(id => !g.IsObjectFixed(id));
		if (decayed)
			return "Something's worn down again overnight. Check the Tasks list and set it right — " +
			       "a tidy town keeps your streak going.";

		return "The whole town's flourishing. Check the Notice Board for today's challenges — " +
		       "they pay in seeds for the fancier decorations.";
	}
}
