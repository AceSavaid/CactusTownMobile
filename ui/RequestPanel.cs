using Godot;

namespace CactusTown;

/// <summary>
/// Modal request dialogue. Feed it an <see cref="Npc"/>; it shows the greeting +
/// request, lets the player help out (completing the request and paying coins),
/// then shows the thank-you line. Pauses the tree while open.
/// </summary>
public partial class RequestPanel : Control
{
	[Signal] public delegate void ClosedEventHandler();

	private Npc? _npc;

	private Label _name = null!;
	private Label _body = null!;
	private Button _accept = null!;
	private Button _dismiss = null!;

	public override void _Ready()
	{
		_name = GetNode<Label>("%NpcName");
		_body = GetNode<Label>("%Body");
		_accept = GetNode<Button>("%AcceptButton");
		_dismiss = GetNode<Button>("%DismissButton");

		Hide();
		_accept.Pressed += OnAccept;
		_dismiss.Pressed += Close;
	}

	public void Open(Npc npc)
	{
		_npc = npc;
		_name.Text = npc.NpcName;

		if (npc.IsDone())
		{
			ShowThanks();
		}
		else
		{
			_body.Text = $"{npc.Greeting}\n\n{npc.RequestText}";
			_accept.Text = $"Help out  (+{npc.RewardCoins} coins)";
			_accept.Show();
			_dismiss.Text = "Not now";
		}

		Show();
		GetTree().Paused = true;
	}

	private void OnAccept()
	{
		if (_npc == null)
			return;
		_npc.CompleteRequest();
		Router.Instance.Toast($"Request complete!  +{_npc.RewardCoins} coins");
		ShowThanks();
	}

	private void ShowThanks()
	{
		_body.Text = _npc!.ThanksText;
		_accept.Hide();
		_dismiss.Text = "Close";
	}

	private void Close()
	{
		Hide();
		_npc = null;
		GetTree().Paused = false;
		EmitSignal(SignalName.Closed);
	}
}
