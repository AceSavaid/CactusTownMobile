using Godot;

namespace CactusTown;

/// <summary>
/// Town overworld: free-roam with the virtual joystick, walk up to townsfolk
/// to talk and take on repair requests. Subsections and regions hook in here later.
/// </summary>
public partial class Town : Node2D
{
	private const string HouseScene = "res://scenes/House.tscn";

	private Npc? _activeNpc;

	private Player _player = null!;
	private VirtualJoystick _joystick = null!;
	private Button _talkButton = null!;
	private RequestPanel _requestPanel = null!;
	private Node2D _world = null!;

	public override void _Ready()
	{
		_player = GetNode<Player>("%Player");
		_joystick = GetNode<VirtualJoystick>("%VirtualJoystick");
		_talkButton = GetNode<Button>("%TalkButton");
		_requestPanel = GetNode<RequestPanel>("%RequestPanel");
		_world = GetNode<Node2D>("%World");

		_joystick.DirectionChanged += _player.SetMoveDirection;
		_talkButton.Pressed += OnTalkPressed;
		GetNode<Button>("%BackButton").Pressed += () => Router.Instance.GotoScene(HouseScene);
		_requestPanel.Closed += OnRequestPanelClosed;

		foreach (var child in _world.GetChildren())
		{
			if (child is Npc npc)
			{
				npc.PlayerEntered += OnNpcEntered;
				npc.PlayerExited += OnNpcExited;
			}
		}

		RefreshTalkButton();
	}

	private void OnNpcEntered(Npc npc)
	{
		_activeNpc = npc;
		RefreshTalkButton();
	}

	private void OnNpcExited(Npc npc)
	{
		if (npc == _activeNpc)
			_activeNpc = null;
		RefreshTalkButton();
	}

	private void OnTalkPressed()
	{
		if (_activeNpc == null)
			return;
		_requestPanel.Open(_activeNpc);
		RefreshTalkButton();
	}

	private void OnRequestPanelClosed()
	{
		_player.SetMoveDirection(Vector2.Zero);
		RefreshTalkButton();
	}

	private void RefreshTalkButton()
	{
		var showIt = _activeNpc != null && !_requestPanel.Visible;
		_talkButton.Visible = showIt;
		if (showIt)
			_talkButton.Text = $"Talk to {_activeNpc!.NpcName}";
	}
}
