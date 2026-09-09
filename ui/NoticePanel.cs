using Godot;

namespace CactusTown;

/// <summary>
/// The Notice Board dialogue. Lists the current notices (tutorial steps early,
/// daily challenges later) with a claim button once each is complete.
/// </summary>
public partial class NoticePanel : Control
{
	private static readonly Color DoneTint = new(1f, 1f, 1f, 0.45f);
	private static readonly Color ReadyColor = new(0.62f, 0.86f, 0.55f);

	private Label _title = null!;
	private Label _subtitle = null!;
	private VBoxContainer _list = null!;

	public override void _Ready()
	{
		_title = GetNode<Label>("%Title");
		_subtitle = GetNode<Label>("%Subtitle");
		_list = GetNode<VBoxContainer>("%List");
		GetNode<Button>("%CloseButton").Pressed += Hide;
		GetNode<Button>("%Backdrop").Pressed += Hide;
		Hide();
	}

	public void Open()
	{
		GameState.Instance.RefreshDailyNotices();
		Rebuild();
		Show();
		MoveToFront();
	}

	private void Rebuild()
	{
		foreach (var child in _list.GetChildren())
			child.QueueFree();

		var gs = GameState.Instance;
		var tutorial = Notices.InTutorialMode(gs);
		_title.Text = tutorial ? "Getting Started" : "Today's Notices";
		_subtitle.Text = tutorial
			? "A few first steps around Cactus Town."
			: $"New challenges each day   ·   {gs.Seeds} seeds";

		foreach (var item in Notices.Current(gs))
			_list.AddChild(BuildRow(item, item.Done(gs), gs.IsNoticeClaimed(item.Id)));
	}

	private Control BuildRow(Notices.Item item, bool done, bool claimed)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 16);
		row.CustomMinimumSize = new Vector2(0, 74);
		if (claimed)
			row.Modulate = DoneTint;

		var glyph = new Label
		{
			Text = claimed ? "✓" : done ? "●" : "○",
			CustomMinimumSize = new Vector2(40, 0),
			VerticalAlignment = VerticalAlignment.Center,
		};
		glyph.AddThemeFontSizeOverride("font_size", 32);
		if (done && !claimed)
			glyph.AddThemeColorOverride("font_color", ReadyColor);
		row.AddChild(glyph);

		var title = new Label
		{
			Text = item.Title,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		title.AddThemeFontSizeOverride("font_size", 30);
		row.AddChild(title);

		var reward = new Label
		{
			Text = RewardText(item),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Right,
			CustomMinimumSize = new Vector2(180, 0),
		};
		reward.AddThemeFontSizeOverride("font_size", 24);
		reward.AddThemeColorOverride("font_color", new Color(0.8f, 0.82f, 0.86f));
		row.AddChild(reward);

		if (done && !claimed)
		{
			var claim = new Button { Text = "Claim", CustomMinimumSize = new Vector2(150, 60) };
			claim.AddThemeFontSizeOverride("font_size", 26);
			var captured = item;
			claim.Pressed += () =>
			{
				GameState.Instance.ClaimNotice(captured.Id, captured.Coins, captured.Seeds);
				Audio.Instance?.PlaySfx("coin");
				Rebuild();
			};
			row.AddChild(claim);
		}
		else
		{
			row.AddChild(new Control { CustomMinimumSize = new Vector2(150, 0) });
		}

		return row;
	}

	private static string RewardText(Notices.Item item)
	{
		if (item.Coins > 0 && item.Seeds > 0)
			return $"+{item.Coins} coins  ·  +{item.Seeds} seed";
		if (item.Seeds > 0)
			return $"+{item.Seeds} seed{(item.Seeds == 1 ? "" : "s")}";
		return $"+{item.Coins} coins";
	}
}
