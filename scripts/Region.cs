using Godot;

namespace CactusTown;

/// <summary>
/// A gathering region (Forest, Flower Field, River, Cave). Free-roam like the
/// town; walk up to resource nodes and run their mini-game to collect materials.
/// </summary>
public partial class Region : Node2D
{
	[Export] public string RegionId = "forest";
	[Export] public string ReturnScene = "res://scenes/RegionSelect.tscn";

	private ResourceNode? _activeNode;

	private Player _player = null!;
	private VirtualJoystick _joystick = null!;
	private Button _gatherButton = null!;
	private Node2D _world = null!;
	private CanvasLayer _ui = null!;

	public override void _Ready()
	{
		_player = GetNode<Player>("%Player");
		_joystick = GetNode<VirtualJoystick>("%VirtualJoystick");
		_gatherButton = GetNode<Button>("%GatherButton");
		_world = GetNode<Node2D>("%World");
		_ui = GetNode<CanvasLayer>("UI");

		_joystick.DirectionChanged += _player.SetMoveDirection;
		_gatherButton.Pressed += OnGatherPressed;
		GetNode<Button>("%BackButton").Pressed += () => Router.Instance.GotoScene(ReturnScene);

		GameState.Instance.MarkRegionVisited(RegionId);

		foreach (var child in _world.GetChildren())
		{
			if (child is ResourceNode node)
			{
				node.PlayerEntered += OnNodeEntered;
				node.PlayerExited += OnNodeExited;
			}
		}

		RefreshGatherButton();
	}

	private void OnNodeEntered(ResourceNode node)
	{
		_activeNode = node;
		RefreshGatherButton();
	}

	private void OnNodeExited(ResourceNode node)
	{
		if (node == _activeNode)
			_activeNode = null;
		RefreshGatherButton();
	}

	private void OnGatherPressed()
	{
		if (_activeNode == null)
			return;

		var game = _activeNode.CreateMiniGame();
		if (game == null)
			return;

		var node = _activeNode;
		game.Finished += (success, amount) =>
		{
			node.OnHarvested(amount);
			_player.SetMoveDirection(Vector2.Zero);
			RefreshGatherButton();
		};
		_ui.AddChild(game);
		RefreshGatherButton();
	}

	private void RefreshGatherButton()
	{
		var showIt = _activeNode is { IsDepleted: false } && !GetTree().Paused;
		_gatherButton.Visible = showIt;
		if (showIt)
			_gatherButton.Text = _activeNode!.ActionLabel;
	}
}
