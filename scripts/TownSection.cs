using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CactusTown;

/// <summary>
/// One town subsection: free-roam with the joystick, repair the worn fixtures
/// (a cactus by each gives the task), and once everything is restored, re-style
/// them. Completing a section for the first time unlocks the next.
/// </summary>
public partial class TownSection : Node2D
{
	private const string TownMapScene = "res://scenes/TownMap.tscn";

	[Export] public string SectionId = "main_square";

	private RepairableObject? _active;
	private bool _complete;

	private Player _player = null!;
	private VirtualJoystick _joystick = null!;
	private Button _actionButton = null!;
	private RepairPanel _repairPanel = null!;
	private CustomizePanel _customizePanel = null!;
	private Node2D _world = null!;

	public override void _Ready()
	{
		_player = GetNode<Player>("%Player");
		_joystick = GetNode<VirtualJoystick>("%VirtualJoystick");
		_actionButton = GetNode<Button>("%ActionButton");
		_repairPanel = GetNode<RepairPanel>("%RepairPanel");
		_customizePanel = GetNode<CustomizePanel>("%CustomizePanel");
		_world = GetNode<Node2D>("%World");

		_joystick.DirectionChanged += _player.SetMoveDirection;
		_actionButton.Pressed += OnActionPressed;
		GetNode<Button>("%BackButton").Pressed += () => Router.Instance.GotoScene(TownMapScene);
		_repairPanel.Closed += OnPanelClosed;
		_customizePanel.Closed += OnPanelClosed;

		GameState.Instance.RunTownDecay();

		foreach (var obj in Objects)
		{
			obj.PlayerEntered += OnObjectEntered;
			obj.PlayerExited += OnObjectExited;
		}

		BuildBuildingColliders();
		RefreshCompletion();
	}

	/// <summary>
	/// Give every backdrop building a solid footprint so the player bumps into it
	/// instead of walking through. A band across the lower part of each sprite —
	/// tall enough to block, short enough that the player can still stand close.
	/// </summary>
	private void BuildBuildingColliders()
	{
		var backdrop = GetNodeOrNull<Node2D>("Backdrop");
		if (backdrop == null)
			return;

		var bodies = new Node2D { Name = "BuildingBodies" };
		AddChild(bodies);

		foreach (var child in backdrop.GetChildren())
		{
			if (child is not Sprite2D sprite || sprite.Texture == null)
				continue;

			var size = sprite.Texture.GetSize() * sprite.Scale.Abs();
			var footHeight = Mathf.Clamp(size.Y * 0.55f, 90f, 260f);
			var footWidth = size.X * 0.88f;
			var baseY = sprite.Position.Y + size.Y * 0.5f;

			var centre = new Vector2(sprite.Position.X, baseY - footHeight * 0.5f - 8f);
			var half = new Vector2(footWidth, footHeight) * 0.5f;

			// A building must never sit on the spawn or an interaction spot.
			if (Mathf.Abs(_player.Position.X - centre.X) < half.X + 40f &&
			    Mathf.Abs(_player.Position.Y - centre.Y) < half.Y + 40f)
				continue;

			var body = new StaticBody2D { Position = centre };
			body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = half * 2f } });
			bodies.AddChild(body);
		}
	}

	private IEnumerable<RepairableObject> Objects => _world.GetChildren().OfType<RepairableObject>();

	private void RefreshCompletion()
	{
		var objects = Objects.ToList();
		_complete = objects.Count > 0 && objects.All(o => o.IsFixed);

		if (_complete && !GameState.Instance.IsSectionCompletedOnce(SectionId))
		{
			GameState.Instance.SetSectionCompletedOnce(SectionId);
			Audio.Instance?.PlaySfx("fanfare");
			Router.Instance.Toast($"{SectionName} restored!");
		}

		foreach (var o in objects)
		{
			o.SetCustomizable(_complete);
			o.NotifyChanged();
		}
		RefreshActionButton();
	}

	private void OnObjectEntered(RepairableObject obj)
	{
		_active = obj;
		RefreshActionButton();
	}

	private void OnObjectExited(RepairableObject obj)
	{
		if (obj == _active)
			_active = null;
		RefreshActionButton();
	}

	private void OnActionPressed()
	{
		if (_active == null)
			return;
		if (_active.IsFixed && _complete)
			_customizePanel.Open(_active);
		else if (!_active.IsFixed)
			_repairPanel.Open(_active);
		RefreshActionButton();
	}

	private void OnPanelClosed()
	{
		_player.SetMoveDirection(Vector2.Zero);
		RefreshCompletion();
	}

	private void RefreshActionButton()
	{
		var show = _active is { PlayerCanInteract: true } && !GetTree().Paused;
		_actionButton.Visible = show;
		if (show)
			_actionButton.Text = _active!.InteractionLabel;
	}

	private string SectionName => TownSections.Find(SectionId)?.Name ?? "The area";
}
