using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CactusTown;

/// <summary>
/// Memory / match-the-pairs. Default mode is vs the AI: players alternate
/// flipping two cards, a match scores a point and an extra go, and the AI
/// remembers cards it has seen up to a difficulty-scaled cap (Hard = perfect
/// recall). Most pairs wins.
///
/// In <see cref="ArcadeGame.SoloMode"/> there is no AI — clear the whole board
/// before running out of your (difficulty-scaled) mismatch allowance.
/// </summary>
public partial class MatchPairs : ArcadeGame
{
	private static readonly string[] FaceFiles =
	{
		"element_blue_square", "element_green_square", "element_red_square", "element_yellow_square",
		"element_purple_square", "element_grey_square", "element_red_diamond", "element_blue_diamond",
	};

	private int _cols = 4, _rows = 4, _pairs = 8, _memoryCap = 8;
	private int _missBudget = 8, _missesLeft;

	private int[] _face = System.Array.Empty<int>();
	private bool[] _matched = System.Array.Empty<bool>();
	private Button[] _cards = System.Array.Empty<Button>();
	private Texture2D _back = null!;
	private Texture2D[] _faceTex = System.Array.Empty<Texture2D>();

	private readonly Dictionary<int, List<int>> _mem = new();
	private readonly List<int> _memOrder = new();

	private int _first = -1;
	private bool _busy;
	private bool _playerTurn = true;
	private bool _roundOver;
	private int _playerScore, _aiScore;

	private Label _status = null!;
	private GridContainer _grid = null!;
	private ArcadeResult _result = null!;

	public override void Configure(int difficulty)
	{
		base.Configure(difficulty);
		(_cols, _rows, _memoryCap) = difficulty switch
		{
			1 => (4, 3, 3),
			2 => (4, 4, 8),
			_ => (4, 4, 999),
		};
		_pairs = _cols * _rows / 2;
		// Solo: a mismatch allowance that tightens with difficulty.
		_missBudget = difficulty switch { 1 => _pairs + 4, 2 => _pairs, _ => Mathf.Max(3, _pairs - 3) };
	}

	public override void _Ready()
	{
		base._Ready();
		_status = GetNode<Label>("%Status");
		_grid = GetNode<GridContainer>("%Grid");
		_result = GetNode<ArcadeResult>("%Result");
		_result.PlayAgain += NewGame;
		_result.Leave += Close;
		GetNode<Button>("%LeaveButton").Pressed += Close;

		_back = GD.Load<Texture2D>("res://assets/sprites/arcade/card_back.svg");
		_faceTex = FaceFiles.Select(f => GD.Load<Texture2D>($"res://assets/kenney/puzzle/{f}.png")).ToArray();

		_grid.Columns = _cols;
		var count = _cols * _rows;
		_cards = new Button[count];
		for (var i = 0; i < count; i++)
		{
			var button = new Button
			{
				CustomMinimumSize = new Vector2(158, 172),
				ExpandIcon = true,
				IconAlignment = HorizontalAlignment.Center,
			};
			var index = i;
			button.Pressed += () => OnCard(index);
			_grid.AddChild(button);
			_cards[i] = button;
		}
		NewGame();
	}

	private void NewGame()
	{
		var count = _cols * _rows;
		_face = new int[count];
		_matched = new bool[count];

		var pool = new List<int>();
		for (var p = 0; p < _pairs; p++)
		{
			pool.Add(p);
			pool.Add(p);
		}
		for (var i = pool.Count - 1; i > 0; i--)
		{
			var j = (int)(GD.Randi() % (uint)(i + 1));
			(pool[i], pool[j]) = (pool[j], pool[i]);
		}
		for (var i = 0; i < count; i++)
			_face[i] = pool[i];

		_mem.Clear();
		_memOrder.Clear();
		_first = -1;
		_busy = false;
		_roundOver = false;
		_playerTurn = true;
		_playerScore = _aiScore = 0;
		_missesLeft = _missBudget;

		for (var i = 0; i < count; i++)
		{
			_cards[i].Icon = _back;
			_cards[i].Disabled = false;
			_cards[i].Modulate = Colors.White;
		}
		UpdateStatus();
	}

	private void OnCard(int i)
	{
		if (_busy || _roundOver || !_playerTurn || _matched[i] || i == _first)
			return;

		Reveal(i);
		Remember(i);

		if (_first == -1)
		{
			_first = i;
			return;
		}

		var a = _first;
		_first = -1;
		_busy = true;
		Evaluate(a, i, true);
	}

	private async void Evaluate(int a, int b, bool byPlayer)
	{
		await ToSignal(GetTree().CreateTimer(0.75), SceneTreeTimer.SignalName.Timeout);

		var isMatch = _face[a] == _face[b];
		if (isMatch)
		{
			_matched[a] = _matched[b] = true;
			_cards[a].Disabled = _cards[b].Disabled = true;
			_cards[a].Modulate = _cards[b].Modulate = new Color(1, 1, 1, 0.4f);
			Forget(a);
			Forget(b);
			if (byPlayer)
				_playerScore++;
			else
				_aiScore++;
		}
		else
		{
			HideCard(a);
			HideCard(b);
			if (SoloMode && byPlayer)
				_missesLeft--;
		}

		_busy = false;

		if (SoloMode)
		{
			if (_playerScore >= _pairs)
				EndSolo(true);
			else if (_missesLeft <= 0)
				EndSolo(false);
			else
				UpdateStatus();
			return;
		}

		if (_playerScore + _aiScore >= _pairs)
		{
			EndRound();
			return;
		}

		// Match keeps the turn; a miss passes it.
		_playerTurn = isMatch ? byPlayer : !byPlayer;
		if (_playerTurn)
			UpdateStatus();
		else
			AiTurn();
	}

	private async void AiTurn()
	{
		if (_roundOver)
			return;
		_busy = true;
		UpdateStatus();

		await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
		var a = AiFirstPick();
		Reveal(a);
		Remember(a);

		await ToSignal(GetTree().CreateTimer(0.65), SceneTreeTimer.SignalName.Timeout);
		var b = AiSecondPick(a);
		Reveal(b);
		Remember(b);

		Evaluate(a, b, false);
	}

	private int AiFirstPick()
	{
		foreach (var (_, known) in _mem)
		{
			var live = known.Where(idx => !_matched[idx]).Distinct().ToList();
			if (live.Count >= 2)
				return live[0];
		}
		return RandomCard(-1, preferUnknown: true);
	}

	private int AiSecondPick(int a)
	{
		if (_mem.TryGetValue(_face[a], out var known))
		{
			var partner = known.FirstOrDefault(idx => idx != a && !_matched[idx], -1);
			if (partner >= 0)
				return partner;
		}
		return RandomCard(a, preferUnknown: true);
	}

	private int RandomCard(int exclude, bool preferUnknown)
	{
		var available = Enumerable.Range(0, _cards.Length)
			.Where(i => !_matched[i] && i != exclude && i != _first)
			.ToList();
		var pool = preferUnknown ? available.Where(i => !_memOrder.Contains(i)).ToList() : available;
		if (pool.Count == 0)
			pool = available;
		return pool[GD.RandRange(0, pool.Count - 1)];
	}

	private void Remember(int i)
	{
		var f = _face[i];
		if (!_mem.TryGetValue(f, out var list))
		{
			list = new List<int>();
			_mem[f] = list;
		}
		if (list.Contains(i))
			return;
		list.Add(i);
		_memOrder.Add(i);
		while (_memOrder.Count > _memoryCap)
		{
			var old = _memOrder[0];
			_memOrder.RemoveAt(0);
			if (_mem.TryGetValue(_face[old], out var l))
			{
				l.Remove(old);
				if (l.Count == 0)
					_mem.Remove(_face[old]);
			}
		}
	}

	private void Forget(int i)
	{
		_memOrder.Remove(i);
		if (_mem.TryGetValue(_face[i], out var l))
		{
			l.Remove(i);
			if (l.Count == 0)
				_mem.Remove(_face[i]);
		}
	}

	private void Reveal(int i) => _cards[i].Icon = _faceTex[_face[i]];
	private void HideCard(int i) => _cards[i].Icon = _back;

	private void UpdateStatus()
	{
		if (SoloMode)
		{
			_status.Text = $"Pairs {_playerScore} / {_pairs}      Misses left: {Mathf.Max(0, _missesLeft)}";
			return;
		}
		var who = _roundOver ? "" : _playerTurn ? "Your turn" : "AI's turn";
		_status.Text = $"{who}      You {_playerScore} : {_aiScore} AI";
	}

	private void EndRound()
	{
		_roundOver = true;
		var result = _playerScore > _aiScore ? 1 : _playerScore < _aiScore ? -1 : 0;
		UpdateStatus();
		ReportResult(result);
		_result.ShowResult(result, Difficulty);
	}

	private void EndSolo(bool cleared)
	{
		_roundOver = true;
		var result = cleared ? 1 : -1;
		UpdateStatus();
		ReportResult(result);
		_result.ShowResult(result, Difficulty);
	}
}
