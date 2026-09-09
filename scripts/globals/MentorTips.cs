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
					"Head into town and mend the old welcome sign by me. The Notice Board there lists what it needs.",
				"tut:gather" =>
					"Now try a material region from the Town map — chop, mine or pick, and bring some back.",
				"tut:custom" =>
					"You've a few coins. Give your plant a new pot or hat in Customise Plant.",
				"tut:arcade" =>
					"Pop into Mini-Games and win a round — it pays a coin or three.",
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
