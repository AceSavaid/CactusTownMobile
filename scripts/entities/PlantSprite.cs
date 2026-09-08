using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// The player's plant composited from pot + species + accessory, for use as the
/// in-world character. Reads <see cref="GameState"/> and updates on
/// <see cref="GameState.PlantChanged"/> so customisation carries between scenes.
/// </summary>
public partial class PlantSprite : Node2D
{
	private static readonly Dictionary<string, Vector2> AnchorOffset = new()
	{
		{ "hat", new Vector2(0, -196) },
		{ "face", new Vector2(0, -132) },
		{ "neck", new Vector2(0, -34) },
		{ "pot", new Vector2(0, 24) },
	};

	private Sprite2D _pot = null!;
	private Sprite2D _plant = null!;
	private Sprite2D _accessory = null!;

	public override void _Ready()
	{
		_pot = GetNode<Sprite2D>("Pot");
		_plant = GetNode<Sprite2D>("Plant");
		_accessory = GetNode<Sprite2D>("Accessory");
		Refresh();
		GameState.Instance.PlantChanged += Refresh;
	}

	public override void _ExitTree()
	{
		GameState.Instance.PlantChanged -= Refresh;
	}

	private void Refresh()
	{
		var gs = GameState.Instance;
		SetTexture(_pot, gs.PlantPot);
		SetTexture(_plant, gs.PlantSpecies);

		var accessory = PlantCatalog.Find(gs.PlantAccessory);
		if (accessory is { HasTexture: true } && accessory.Anchor != "aura")
		{
			_accessory.Texture = GD.Load<Texture2D>(accessory.TexturePath);
			_accessory.Position = AnchorOffset.GetValueOrDefault(accessory.Anchor, new Vector2(0, -60));
			_accessory.Visible = true;
		}
		else
		{
			_accessory.Visible = false;
		}
	}

	private static void SetTexture(Sprite2D target, string itemId)
	{
		var item = PlantCatalog.Find(itemId);
		if (item is { HasTexture: true })
			target.Texture = GD.Load<Texture2D>(item.TexturePath);
	}
}
