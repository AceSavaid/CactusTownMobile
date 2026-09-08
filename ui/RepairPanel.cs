using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// Modal repair dialogue for a <see cref="RepairableObject"/>. Shows the cactus's
/// request and a materials checklist; "Repair" unlocks once the player has the
/// materials, consumes them, pays coins, then shows the thank-you line.
/// </summary>
public partial class RepairPanel : Control
{
	[Signal] public delegate void ClosedEventHandler();

	private RepairableObject? _obj;

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

	public void Open(RepairableObject obj)
	{
		_obj = obj;
		_name.Text = obj.DisplayName;

		if (obj.IsFixed)
		{
			ShowThanks();
		}
		else
		{
			_body.Text = string.IsNullOrEmpty(obj.NpcName)
				? obj.RequestText
				: $"{obj.NpcName}: {obj.RequestText}";
			_requirements.Text = BuildChecklist(obj);
			_requirements.Visible = obj.RequiredMaterials.Count > 0;
			var ready = obj.CanRepair();
			_accept.Disabled = !ready;
			_accept.Text = ready ? $"Repair  (+{obj.RepairReward} coins)" : "Repair  (need materials)";
			_accept.Show();
			_dismiss.Text = "Not now";
		}

		Show();
		GetTree().Paused = true;
	}

	private void OnAccept()
	{
		if (_obj == null)
			return;
		_obj.Repair();
		Audio.Instance?.PlaySfx("repair");
		Router.Instance.Toast($"Repaired!  +{_obj.RepairReward} coins");
		ShowThanks();
	}

	private void ShowThanks()
	{
		_body.Text = _obj!.ThanksText;
		_requirements.Hide();
		_accept.Hide();
		_dismiss.Text = "Close";
	}

	private void Close()
	{
		Hide();
		_obj = null;
		GetTree().Paused = false;
		EmitSignal(SignalName.Closed);
	}

	private static string BuildChecklist(RepairableObject obj)
	{
		if (obj.RequiredMaterials.Count == 0)
			return "";

		var lines = new List<string> { "Needs:" };
		foreach (var (id, count) in obj.RequiredMaterials)
		{
			var key = id.AsString();
			var need = count.AsInt32();
			var have = GameState.Instance.GetMaterial(key);
			lines.Add($"   {(have >= need ? "✓" : "•")}  {Materials.DisplayName(key)}   {have} / {need}");
		}
		return string.Join("\n", lines);
	}
}
