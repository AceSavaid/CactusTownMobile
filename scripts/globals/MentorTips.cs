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
		// Onboarding: mirror the Notice Board's current getting-started step so the
		// two never disagree.
		var step = Notices.CurrentTutorialStep(g);
		if (step != null)
		{
			return step.Id switch
			{
				"tut:sign" =>
					"Welcome to Cactus Town! Head out through Go to Town — the Notice Board " +
					"in the square shows your first task: mend the old welcome sign by me.",
				"tut:gather" =>
					"Nice work on the sign. Now visit a material region — Forest, Cave, River " +
					"or Flower Field, reached from the Town map — and gather a few times.",
				"tut:custom" =>
					"You've earned some coins. Open Customise Plant and treat yourself to a " +
					"new pot or a little hat.",
				"tut:arcade" =>
					"There are games in the house. Open Mini-Games and win a round — it pays coins.",
				"tut:section" =>
					RestoreLine(g),
				_ => "Keep at it — the town's looking brighter already.",
			};
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

	private static string RestoreLine(GameState g)
	{
		var next = TownSections.All.FirstOrDefault(s =>
			g.IsSectionUnlocked(s.Id) && !g.IsSectionCompletedOnce(s.Id));
		return next == null
			? "Keep mending — the town's brighter already."
			: $"Fix every fixture in the {next.Name} and the next part of town opens up.";
	}
}
