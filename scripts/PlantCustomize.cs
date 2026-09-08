using Godot;

namespace CactusTown;

/// <summary>
/// Full-screen plant styling: pick a pot, a plant species, and an accessory.
/// Default items are free; the rest cost coins and unlock on purchase. The live
/// preview updates as you go.
/// </summary>
public partial class PlantCustomize : Control
{
	private const string HouseScene = "res://scenes/House.tscn";

	private PlantCatalog.Slot _slot = PlantCatalog.Slot.Pot;

	private HBoxContainer _options = null!;
	private Button _potTab = null!, _plantTab = null!, _accessoryTab = null!;

	public override void _Ready()
	{
		_options = GetNode<HBoxContainer>("%Options");
		_potTab = GetNode<Button>("%PotTab");
		_plantTab = GetNode<Button>("%PlantTab");
		_accessoryTab = GetNode<Button>("%AccessoryTab");

		GetNode<Button>("%BackButton").Pressed += () => Router.Instance.GotoScene(HouseScene);
		_potTab.Pressed += () => SelectSlot(PlantCatalog.Slot.Pot);
		_plantTab.Pressed += () => SelectSlot(PlantCatalog.Slot.Plant);
		_accessoryTab.Pressed += () => SelectSlot(PlantCatalog.Slot.Accessory);

		SelectSlot(PlantCatalog.Slot.Pot);
	}

	private void SelectSlot(PlantCatalog.Slot slot)
	{
		_slot = slot;
		_potTab.ButtonPressed = slot == PlantCatalog.Slot.Pot;
		_plantTab.ButtonPressed = slot == PlantCatalog.Slot.Plant;
		_accessoryTab.ButtonPressed = slot == PlantCatalog.Slot.Accessory;
		Rebuild();
	}

	private void Rebuild()
	{
		foreach (var child in _options.GetChildren())
			child.QueueFree();

		var equipped = GameState.Instance.PlantSlotItem(_slot);
		foreach (var item in PlantCatalog.InSlot(_slot))
			_options.AddChild(BuildCard(item, item.Id == equipped));
	}

	private Control BuildCard(PlantCatalog.Item item, bool equipped)
	{
		var card = new VBoxContainer { CustomMinimumSize = new Vector2(214, 0) };
		card.AddThemeConstantOverride("separation", 12);

		var preview = new TextureRect
		{
			CustomMinimumSize = new Vector2(190, 190),
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = item.HasTexture ? GD.Load<Texture2D>(item.TexturePath) : null,
		};
		card.AddChild(preview);

		card.AddChild(new Label { Text = item.Name, HorizontalAlignment = HorizontalAlignment.Center });

		var button = new Button { CustomMinimumSize = new Vector2(0, 86) };
		button.AddThemeFontSizeOverride("font_size", 26);

		if (equipped)
		{
			button.Text = "Selected";
			button.Disabled = true;
		}
		else if (GameState.Instance.OwnsPlantItem(item.Id))
		{
			button.Text = "Select";
			button.Pressed += () =>
			{
				GameState.Instance.EquipPlantItem(item.Id);
				Rebuild();
			};
		}
		else
		{
			button.Text = $"{item.Cost} coins";
			button.Disabled = GameState.Instance.GetCoins() < item.Cost;
			button.Pressed += () =>
			{
				if (GameState.Instance.BuyPlantItem(item.Id))
					GameState.Instance.EquipPlantItem(item.Id);
				Rebuild();
			};
		}

		card.AddChild(button);
		return card;
	}
}
