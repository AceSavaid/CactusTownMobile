using Godot;

namespace CactusTown;

/// <summary>
/// Modal styling picker for a restored <see cref="RepairableObject"/>. One card
/// per variant: the current one is marked, owned ones can be selected, locked
/// ones show their coin cost and unlock on purchase.
/// </summary>
public partial class CustomizePanel : Control
{
	[Signal] public delegate void ClosedEventHandler();

	private RepairableObject? _obj;

	private Label _title = null!;
	private HBoxContainer _options = null!;

	public override void _Ready()
	{
		_title = GetNode<Label>("%Title");
		_options = GetNode<HBoxContainer>("%Options");
		Hide();
		GetNode<Button>("%CloseButton").Pressed += Close;
	}

	public void Open(RepairableObject obj)
	{
		_obj = obj;
		_title.Text = $"Customise — {obj.DisplayName}";
		Rebuild();
		Show();
		GetTree().Paused = true;
	}

	private void Rebuild()
	{
		if (_obj == null)
			return;

		foreach (var child in _options.GetChildren())
			child.QueueFree();

		for (var i = 0; i < _obj.VariantCount; i++)
			_options.AddChild(BuildCard(i));
	}

	private Control BuildCard(int index)
	{
		var card = new VBoxContainer { CustomMinimumSize = new Vector2(260, 0) };
		card.AddThemeConstantOverride("separation", 14);

		var preview = new TextureRect
		{
			Texture = _obj!.Variants[index],
			CustomMinimumSize = new Vector2(240, 200),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
		};
		card.AddChild(preview);

		var label = new Label
		{
			Text = index == 0 ? "Original" : $"Style {index}",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		card.AddChild(label);

		var button = new Button { CustomMinimumSize = new Vector2(0, 88) };
		button.AddThemeFontSizeOverride("font_size", 28);

		if (_obj.CurrentVariant == index)
		{
			button.Text = "Selected";
			button.Disabled = true;
		}
		else if (_obj.OwnsVariant(index))
		{
			button.Text = "Select";
			var i = index;
			button.Pressed += () =>
			{
				_obj!.SelectVariant(i);
				Rebuild();
			};
		}
		else
		{
			var cost = _obj.VariantCost(index);
			button.Text = $"{cost} coins";
			button.Disabled = GameState.Instance.GetCoins() < cost;
			var i = index;
			button.Pressed += () =>
			{
				if (_obj!.BuyVariant(i))
					_obj.SelectVariant(i);
				Rebuild();
			};
		}

		card.AddChild(button);
		return card;
	}

	private void Close()
	{
		Hide();
		_obj = null;
		GetTree().Paused = false;
		EmitSignal(SignalName.Closed);
	}
}
