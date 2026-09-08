using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CactusTown;

/// <summary>
/// Solo Minesweeper. Tap to reveal; toggle Flag mode to mark suspected mines.
/// The first tap is always safe. Difficulty sets the board size and mine count.
/// Clear every safe cell to win.
/// </summary>
public partial class Minesweeper : ArcadeGame
{
	private static readonly Color[] NumberColor =
	{
		Colors.White,
		new(0.44f, 0.62f, 0.95f), new(0.40f, 0.76f, 0.46f), new(0.90f, 0.46f, 0.42f),
		new(0.60f, 0.48f, 0.88f), new(0.86f, 0.56f, 0.32f), new(0.36f, 0.76f, 0.80f),
		new(0.85f, 0.85f, 0.88f), new(0.70f, 0.70f, 0.74f),
	};

	private int _cols = 10, _rows = 8, _mineCount = 15;

	private bool[] _mine = System.Array.Empty<bool>();
	private bool[] _revealed = System.Array.Empty<bool>();
	private bool[] _flagged = System.Array.Empty<bool>();
	private int[] _adjacent = System.Array.Empty<int>();
	private Button[] _cells = System.Array.Empty<Button>();

	private bool _firstReveal = true;
	private bool _roundOver;
	private bool _flagMode;
	private int _safeLeft;

	private Texture2D _flagTex = null!, _mineTex = null!;
	private Label _status = null!;
	private Button _flagButton = null!;
	private GridContainer _grid = null!;
	private ArcadeResult _result = null!;

	public override void Configure(int difficulty)
	{
		base.Configure(difficulty);
		(_cols, _rows, _mineCount) = difficulty switch
		{
			1 => (8, 6, 7),
			2 => (10, 8, 15),
			_ => (12, 10, 28),
		};
	}

	public override void _Ready()
	{
		base._Ready();
		_status = GetNode<Label>("%Status");
		_grid = GetNode<GridContainer>("%Grid");
		_flagButton = GetNode<Button>("%FlagButton");
		_result = GetNode<ArcadeResult>("%Result");
		_result.PlayAgain += NewGame;
		_result.Leave += Close;
		GetNode<Button>("%LeaveButton").Pressed += Close;
		_flagButton.Pressed += () => { _flagMode = !_flagMode; RefreshFlagButton(); };

		_flagTex = GD.Load<Texture2D>("res://assets/sprites/arcade/ms_flag.svg");
		_mineTex = GD.Load<Texture2D>("res://assets/sprites/arcade/ms_mine.svg");

		_grid.Columns = _cols;
		var size = Mathf.Clamp(Mathf.Min(980 / _cols, 640 / _rows), 46, 92);
		_cells = new Button[_cols * _rows];
		for (var i = 0; i < _cells.Length; i++)
		{
			var button = new Button { CustomMinimumSize = new Vector2(size, size), ExpandIcon = true };
			button.AddThemeFontSizeOverride("font_size", (int)(size * 0.5f));
			var index = i;
			button.Pressed += () => OnCell(index);
			_grid.AddChild(button);
			_cells[i] = button;
		}
		NewGame();
	}

	private void NewGame()
	{
		var n = _cols * _rows;
		_mine = new bool[n];
		_revealed = new bool[n];
		_flagged = new bool[n];
		_adjacent = new int[n];
		_firstReveal = true;
		_roundOver = false;
		_flagMode = false;
		_safeLeft = n - _mineCount;

		foreach (var cell in _cells)
		{
			cell.Text = "";
			cell.Icon = null;
			cell.Disabled = false;
			cell.Modulate = Colors.White;
		}
		RefreshFlagButton();
		UpdateStatus();
	}

	private void OnCell(int i)
	{
		if (_roundOver)
			return;

		if (_flagMode)
		{
			if (_revealed[i])
				return;
			_flagged[i] = !_flagged[i];
			_cells[i].Icon = _flagged[i] ? _flagTex : null;
			UpdateStatus();
			return;
		}

		if (_revealed[i] || _flagged[i])
			return;

		if (_firstReveal)
		{
			PlaceMines(i);
			_firstReveal = false;
		}

		Reveal(i);
		if (_roundOver)
			return;
		if (_safeLeft <= 0)
			WinGame();
	}

	private void PlaceMines(int safeIndex)
	{
		var forbidden = new HashSet<int>(Neighbours(safeIndex)) { safeIndex };
		var spots = Enumerable.Range(0, _cells.Length).Where(i => !forbidden.Contains(i)).ToList();
		for (var i = spots.Count - 1; i > 0; i--)
		{
			var j = (int)(GD.Randi() % (uint)(i + 1));
			(spots[i], spots[j]) = (spots[j], spots[i]);
		}
		foreach (var s in spots.Take(_mineCount))
			_mine[s] = true;

		for (var i = 0; i < _cells.Length; i++)
			_adjacent[i] = Neighbours(i).Count(n => _mine[n]);
	}

	private void Reveal(int i)
	{
		if (_roundOver || _revealed[i] || _flagged[i])
			return;
		_revealed[i] = true;
		_cells[i].Disabled = true;
		_cells[i].Icon = null;

		if (_mine[i])
		{
			LoseGame();
			return;
		}

		_safeLeft--;
		if (_adjacent[i] == 0)
		{
			foreach (var n in Neighbours(i))
				Reveal(n);
		}
		else
		{
			_cells[i].Text = _adjacent[i].ToString();
			_cells[i].AddThemeColorOverride("font_disabled_color", NumberColor[_adjacent[i]]);
		}
	}

	private void LoseGame()
	{
		_roundOver = true;
		for (var i = 0; i < _cells.Length; i++)
		{
			if (_mine[i])
			{
				_cells[i].Icon = _mineTex;
				_cells[i].Disabled = true;
				_cells[i].Modulate = new Color(1f, 0.6f, 0.55f);
			}
		}
		_status.Text = "Boom!";
		ReportResult(-1);
		_result.ShowResult(-1, Difficulty);
	}

	private void WinGame()
	{
		_roundOver = true;
		_status.Text = "Cleared!";
		ReportResult(1);
		_result.ShowResult(1, Difficulty);
	}

	private void RefreshFlagButton() => _flagButton.Text = _flagMode ? "Flag mode: ON" : "Flag mode: OFF";

	private void UpdateStatus()
	{
		var flags = _flagged.Count(f => f);
		_status.Text = $"Mines left: {_mineCount - flags}";
	}

	private IEnumerable<int> Neighbours(int i)
	{
		var cx = i % _cols;
		var cy = i / _cols;
		for (var dy = -1; dy <= 1; dy++)
		for (var dx = -1; dx <= 1; dx++)
		{
			if (dx == 0 && dy == 0)
				continue;
			var nx = cx + dx;
			var ny = cy + dy;
			if (nx >= 0 && nx < _cols && ny >= 0 && ny < _rows)
				yield return ny * _cols + nx;
		}
	}
}
