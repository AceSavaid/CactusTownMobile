using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// Modal request dialogue. Feed it an <see cref="Npc"/>; it shows the greeting +
/// request and a materials checklist. "Fix it" unlocks once the player has the
/// materials; completing consumes them, pays coins, and shows the thank-you line.
/// Pauses the tree while open.
/// </summary>
public partial class RequestPanel : Control
{
	[Signal] public delegate void ClosedEventHandler();

	private Npc? _npc;

	private Label _name = null!;
	private Label _body = null!;
	private Label _requirements = null!;
	private Button _accept = null!;
	private Button _dismiss = null!;

	public override void _Ready()
	{
		_name = GetNode<Label>("%NpcName");
		_body = GetNode<Label>("%Body");
		_requirements = GetNode<Label>("%Requirements");
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
			_requirements.Text = BuildChecklist(npc);
			_requirements.Visible = npc.RequiredMaterials.Count > 0;
			var ready = npc.CanComplete();
			_accept.Disabled = !ready;
			_accept.Text = ready ? $"Fix it  (+{npc.RewardCoins} coins)" : "Fix it  (need materials)";
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
		_requirements.Hide();
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

	private static string BuildChecklist(Npc npc)
	{
		if (npc.RequiredMaterials.Count == 0)
			return "";

		var lines = new List<string> { "Needs:" };
		foreach (var (id, count) in npc.RequiredMaterials)
		{
			var key = id.AsString();
			var need = count.AsInt32();
			var have = GameState.Instance.GetMaterial(key);
			var mark = have >= need ? "✓" : "•";
			lines.Add($"   {mark}  {Materials.DisplayName(key)}   {have} / {need}");
		}
		return string.Join("\n", lines);
	}
}
