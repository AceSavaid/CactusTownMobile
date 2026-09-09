using Godot;

namespace CactusTown;

/// <summary>
/// The hub's inline quest strip — sits under Sage's tip and shows the Notice
/// Board's current line (the getting-started step, or the day's challenges).
/// Tapping it opens the full <see cref="NoticePanel"/>.
/// </summary>
public partial class HubQuest : Button
{
	private Label _heading = null!;
	private Label _body = null!;
	private Label _reward = null!;

	public NoticePanel? Panel;

	public override void _Ready()
	{
		_heading = GetNode<Label>("%QHeading");
		_body = GetNode<Label>("%QBody");
		_reward = GetNode<Label>("%QReward");

		Pressed += () => Panel?.Open();
		GameState.Instance.ProgressChanged += Refresh;
		GameState.Instance.SeedsChanged += _ => Refresh();
		Refresh();
	}

	public override void _ExitTree()
	{
		GameState.Instance.ProgressChanged -= Refresh;
	}

	private void Refresh()
	{
		var g = GameState.Instance;

		if (Notices.InTutorialMode(g))
		{
			var (step, total) = Notices.TutorialProgress(g);
			var item = Notices.CurrentTutorialStep(g);
			_heading.Text = $"Getting Started  ·  step {step} of {total}";
			_body.Text = item?.Title ?? "";
			_reward.Text = item == null ? "" : Reward(item);
			return;
		}

		var current = Notices.Current(g);
		var claimable = 0;
		foreach (var n in current)
			if (n.Done(g) && !g.IsNoticeClaimed(n.Id))
				claimable++;

		_heading.Text = "Today's Notices";
		_body.Text = claimable > 0
			? $"{claimable} reward{(claimable == 1 ? "" : "s")} ready to claim"
			: "Tap to see today's challenges";
		_reward.Text = $"{g.Seeds} seeds";
	}

	private static string Reward(Notices.Item item) =>
		item.Seeds > 0 ? $"+{item.Coins} coins, +{item.Seeds} seed" : $"+{item.Coins} coins";
}
