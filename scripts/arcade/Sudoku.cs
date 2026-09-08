using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace CactusTown;

/// <summary>
/// Solo Sudoku. Tap a cell, then a number to fill it. Only correct digits stick;
/// a wrong one costs a mistake and three mistakes end the game. Difficulty sets
/// how many cells start filled. Complete the grid to win.
/// </summary>
public partial class Sudoku : ArcadeGame
{
	private const int MaxMistakes = 3;

	private static readonly Color BoxLight = new(0.24f, 0.26f, 0.31f);
	private static readonly Color BoxDark = new(0.20f, 0.22f, 0.27f);
	private static readonly Color GivenText = new(0.92f, 0.93f, 0.96f);
	private static readonly Color EntryText = new(0.58f, 0.80f, 0.98f);
	private static readonly Color SelectColor = new(0.30f, 0.44f, 0.36f);

	private int _givens = 38;

	private readonly int[] _solution = new int[81];
	private readonly int[] _puzzle = new int[81];
	private readonly int[] _entry = new int[81];
	private readonly Button[] _cells = new Button[81];

	private int _selected = -1;
	private int _mistakes;
	private bool _roundOver;

	private Label _status = null!;
	private GridContainer _grid = null!;
	private GridContainer _pad = null!;
	private ArcadeResult _result = null!;

	public override void Configure(int difficulty)
	{
		base.Configure(difficulty);
		_givens = difficulty switch { 1 => 40, 2 => 33, _ => 27 };
	}

	public override void _Ready()
	{
		base._Ready();
		_status = GetNode<Label>("%Status");
		_grid = GetNode<GridContainer>("%Grid");
		_pad = GetNode<GridContainer>("%Pad");
		_result = GetNode<ArcadeResult>("%Result");
		_result.PlayAgain += NewGame;
		_result.Leave += Close;
		GetNode<Button>("%LeaveButton").Pressed += Close;

		_grid.Columns = 9;
		var size = 62;
		for (var i = 0; i < 81; i++)
		{
			var button = new Button { CustomMinimumSize = new Vector2(size, size) };
			button.AddThemeFontSizeOverride("font_size", (int)(size * 0.56f));
			var index = i;
			button.Pressed += () => OnCell(index);
			_grid.AddChild(button);
			_cells[i] = button;
		}

		_pad.Columns = 5;
		for (var n = 1; n <= 9; n++)
		{
			var value = n;
			var button = new Button
			{
				CustomMinimumSize = new Vector2(96, 96),
				Text = n.ToString(),
			};
			button.AddThemeFontSizeOverride("font_size", 44);
			button.Pressed += () => OnPad(value);
			_pad.AddChild(button);
		}
		var erase = new Button { CustomMinimumSize = new Vector2(96, 96), Text = "⌫" };
		erase.AddThemeFontSizeOverride("font_size", 40);
		erase.Pressed += () => OnPad(0);
		_pad.AddChild(erase);

		NewGame();
	}

	private void NewGame()
	{
		Generate();
		Array.Copy(_puzzle, _entry, 81);
		_selected = -1;
		_mistakes = 0;
		_roundOver = false;
		for (var i = 0; i < 81; i++)
			PaintCell(i);
		UpdateStatus();
	}

	private void OnCell(int i)
	{
		if (_roundOver || _puzzle[i] != 0)
			return;
		_selected = _selected == i ? -1 : i;
		for (var c = 0; c < 81; c++)
			PaintCell(c);
	}

	private void OnPad(int value)
	{
		if (_roundOver || _selected < 0 || _puzzle[_selected] != 0)
			return;

		if (value == 0)
		{
			_entry[_selected] = 0;
			PaintCell(_selected);
			return;
		}

		if (value == _solution[_selected])
		{
			_entry[_selected] = value;
			Audio.Instance?.PlaySfx("confirm");
			PaintCell(_selected);
			CheckWin();
		}
		else
		{
			_mistakes++;
			Audio.Instance?.PlaySfx("cancel");
			FlashWrong(_selected);
			UpdateStatus();
			if (_mistakes >= MaxMistakes)
				EndGame(false);
		}
	}

	private async void FlashWrong(int i)
	{
		var cell = _cells[i];
		cell.Modulate = new Color(1.6f, 0.7f, 0.65f);
		await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
		if (IsInstanceValid(cell))
		{
			cell.Modulate = Colors.White;
			PaintCell(i);
		}
	}

	private void CheckWin()
	{
		for (var i = 0; i < 81; i++)
			if (_entry[i] != _solution[i])
				return;
		EndGame(true);
	}

	private void EndGame(bool won)
	{
		_roundOver = true;
		_selected = -1;
		_status.Text = won ? "Solved!" : "Out of mistakes";
		ReportResult(won ? 1 : -1);
		_result.ShowResult(won ? 1 : -1, Difficulty);
	}

	private void UpdateStatus() => _status.Text = $"Mistakes: {_mistakes} / {MaxMistakes}";

	private void PaintCell(int i)
	{
		var cell = _cells[i];
		var box = i / 27 * 3 + i % 9 / 3;
		var baseColor = box % 2 == 0 ? BoxLight : BoxDark;
		cell.Modulate = Colors.White;

		if (_puzzle[i] != 0)
		{
			cell.Text = _puzzle[i].ToString();
			cell.Disabled = true;
			cell.AddThemeColorOverride("font_disabled_color", GivenText);
			StyleCell(cell, baseColor);
			return;
		}

		cell.Disabled = false;
		cell.Text = _entry[i] == 0 ? "" : _entry[i].ToString();
		cell.AddThemeColorOverride("font_color", EntryText);
		cell.AddThemeColorOverride("font_hover_color", EntryText);
		cell.AddThemeColorOverride("font_pressed_color", EntryText);
		StyleCell(cell, i == _selected ? SelectColor : baseColor);
	}

	private static void StyleCell(Button cell, Color fill)
	{
		var sb = new StyleBoxFlat
		{
			BgColor = fill,
			BorderColor = new Color(0.10f, 0.11f, 0.13f),
		};
		sb.SetBorderWidthAll(2);
		foreach (var state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
			cell.AddThemeStyleboxOverride(state, sb);
	}

	// --- generation -------------------------------------------------------

	private void Generate()
	{
		Array.Clear(_solution, 0, 81);
		FillGrid(_solution);
		Array.Copy(_solution, _puzzle, 81);

		var order = Enumerable.Range(0, 81).ToList();
		Shuffle(order);
		var removed = 0;
		var toRemove = 81 - _givens;
		foreach (var idx in order)
		{
			if (removed >= toRemove)
				break;
			var saved = _puzzle[idx];
			if (saved == 0)
				continue;
			_puzzle[idx] = 0;
			var work = (int[])_puzzle.Clone();
			if (CountSolutions(work, 0, 2) != 1)
				_puzzle[idx] = saved;
			else
				removed++;
		}
	}

	private bool FillGrid(int[] g)
	{
		var cell = Array.IndexOf(g, 0);
		if (cell < 0)
			return true;
		var candidates = Enumerable.Range(1, 9).ToList();
		Shuffle(candidates);
		foreach (var v in candidates)
		{
			if (!Allowed(g, cell, v))
				continue;
			g[cell] = v;
			if (FillGrid(g))
				return true;
			g[cell] = 0;
		}
		return false;
	}

	private int CountSolutions(int[] g, int start, int cap)
	{
		var cell = -1;
		for (var i = start; i < 81; i++)
			if (g[i] == 0) { cell = i; break; }
		if (cell < 0)
			return 1;

		var total = 0;
		for (var v = 1; v <= 9; v++)
		{
			if (!Allowed(g, cell, v))
				continue;
			g[cell] = v;
			total += CountSolutions(g, cell + 1, cap);
			g[cell] = 0;
			if (total >= cap)
				return total;
		}
		return total;
	}

	private static bool Allowed(int[] g, int cell, int v)
	{
		var row = cell / 9;
		var col = cell % 9;
		for (var k = 0; k < 9; k++)
		{
			if (g[row * 9 + k] == v || g[k * 9 + col] == v)
				return false;
		}
		var br = row / 3 * 3;
		var bc = col / 3 * 3;
		for (var r = br; r < br + 3; r++)
		for (var c = bc; c < bc + 3; c++)
			if (g[r * 9 + c] == v)
				return false;
		return true;
	}

	private static void Shuffle<T>(IList<T> list)
	{
		for (var i = list.Count - 1; i > 0; i--)
		{
			var j = (int)(GD.Randi() % (uint)(i + 1));
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}
