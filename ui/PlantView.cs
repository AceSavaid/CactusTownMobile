using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// Composites the player's plant from its pot / species / accessory. Self-updates
/// on <see cref="GameState.PlantChanged"/>. Drop it into any screen.
/// </summary>
public partial class PlantView : Control
{
	private static readonly Dictionary<string, Vector2> AnchorCentre = new()
	{
		{ "hat", new Vector2(140, 96) },
		{ "face", new Vector2(140, 150) },
		{ "neck", new Vector2(140, 250) },
		{ "pot", new Vector2(140, 306) },
	};

	private TextureRect _pot = null!;
	private TextureRect _plant = null!;
	private TextureRect _accessory = null!;

	public override void _Ready()
	{
		_pot = GetNode<TextureRect>("Pot");
		_plant = GetNode<TextureRect>("Plant");
		_accessory = GetNode<TextureRect>("Accessory");
		Refresh();
		GameState.Instance.PlantChanged += Refresh;
	}

	public override void _ExitTree()
	{
		GameState.Instance.PlantChanged -= Refresh;
	}

	public void Refresh()
	{
		SetTexture(_pot, GameState.Instance.PlantPot);
		SetTexture(_plant, GameState.Instance.PlantSpecies);

		var accessory = PlantCatalog.Find(GameState.Instance.PlantAccessory);
		if (accessory is not { HasTexture: true })
		{
			_accessory.Visible = false;
			return;
		}

		_accessory.Texture = GD.Load<Texture2D>(accessory.TexturePath);
		_accessory.Visible = true;

		if (accessory.Anchor == "aura")
		{
			_accessory.Position = Vector2.Zero;
			_accessory.Size = Size;
		}
		else
		{
			var size = _accessory.Texture.GetSize();
			var centre = AnchorCentre.GetValueOrDefault(accessory.Anchor, new Vector2(140, 120));
			_accessory.Size = size;
			_accessory.Position = centre - size * 0.5f;
		}
	}

	private static void SetTexture(TextureRect target, string itemId)
	{
		var item = PlantCatalog.Find(itemId);
		if (item is { HasTexture: true })
			target.Texture = GD.Load<Texture2D>(item.TexturePath);
	}
}
