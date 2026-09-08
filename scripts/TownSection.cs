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

		RefreshCompletion();
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
