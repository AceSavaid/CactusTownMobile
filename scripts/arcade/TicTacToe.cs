using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// Tic-Tac-Toe vs the AI. Player is X and moves first. Easy = random AI,
/// Medium = mostly optimal, Hard = unbeatable minimax.
/// </summary>
public partial class TicTacToe : ArcadeGame
{
	private const int Empty = 0, Human = 1, Ai = 2;

	private static readonly int[][] Lines =
	{
		new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
		new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
		new[] { 0, 4, 8 }, new[] { 2, 4, 6 },
	};

	private readonly int[] _board = new int[9];
	private readonly Button[] _cells = new Button[9];
	private bool _humanTurn = true;
	private bool _roundOver;

	private Label _status = null!;
	private ArcadeResult _result = null!;

	public override void _Ready()
	{
		base._Ready();
		_status = GetNode<Label>("%Status");
		_result = GetNode<ArcadeResult>("%Result");
		_result.PlayAgain += ResetRound;
		_result.Leave += Close;
		GetNode<Button>("%LeaveButton").Pressed += Close;

		var grid = GetNode<GridContainer>("%Board");
		for (var i = 0; i < 9; i++)
		{
			var button = new Button { CustomMinimumSize = new Vector2(190, 190) };
			button.AddThemeFontSizeOverride("font_size", 110);
			var index = i;
			button.Pressed += () => OnCell(index);
			grid.AddChild(button);
			_cells[i] = button;
		}
		ResetRound();
	}

	private void ResetRound()
	{
		for (var i = 0; i < 9; i++)
		{
			_board[i] = Empty;
			_cells[i].Text = "";
			_cells[i].Disabled = false;
		}
		_roundOver = false;
		_humanTurn = true;
		_status.Text = "Your move  (X)";
	}

	private void OnCell(int i)
	{
		if (_roundOver || !_humanTurn || _board[i] != Empty)
			return;
		Place(i, Human);
		if (CheckEnd())
			return;
		_humanTurn = false;
		_status.Text = "Thinking…";
		GetTree().CreateTimer(0.35).Timeout += AiMove;
	}

	private void AiMove()
	{
		if (_roundOver)
			return;
		Place(ChooseAiMove(), Ai);
		if (CheckEnd())
			return;
		_humanTurn = true;
		_status.Text = "Your move  (X)";
	}

	private void Place(int i, int who)
	{
		_board[i] = who;
		_cells[i].Text = who == Human ? "X" : "O";
		_cells[i].Disabled = true;
		_cells[i].AddThemeColorOverride("font_disabled_color",
			who == Human ? new Color(0.90f, 0.55f, 0.45f) : new Color(0.50f, 0.78f, 0.92f));
	}

	private int ChooseAiMove()
	{
		var empties = Empties(_board);
		if (Difficulty == 1)
			return empties[GD.RandRange(0, empties.Count - 1)];
		if (Difficulty == 2 && GD.Randf() > 0.6f)
			return empties[GD.RandRange(0, empties.Count - 1)];
		return BestMove();
	}

	private int BestMove()
	{
		var best = -1;
		var bestScore = int.MinValue;
		foreach (var i in Empties(_board))
		{
			_board[i] = Ai;
			var score = Minimax(_board, false, 0);
			_board[i] = Empty;
			if (score > bestScore)
			{
				bestScore = score;
				best = i;
			}
		}
		return best;
	}

	private static int Minimax(int[] board, bool aiToMove, int depth)
	{
		var winner = Winner(board);
		if (winner == Ai)
			return 10 - depth;
		if (winner == Human)
			return depth - 10;
		var empties = Empties(board);
		if (empties.Count == 0)
			return 0;

		var best = aiToMove ? int.MinValue : int.MaxValue;
		foreach (var i in empties)
		{
			board[i] = aiToMove ? Ai : Human;
			var score = Minimax(board, !aiToMove, depth + 1);
			board[i] = Empty;
			best = aiToMove ? Mathf.Max(best, score) : Mathf.Min(best, score);
		}
		return best;
	}

	private bool CheckEnd()
	{
		var winner = Winner(_board);
		if (winner == Ai)
			return EndRound(-1);
		if (winner == Human)
			return EndRound(1);
		if (Empties(_board).Count == 0)
			return EndRound(0);
		return false;
	}

	private bool EndRound(int result)
	{
		_roundOver = true;
		foreach (var cell in _cells)
			cell.Disabled = true;
		_status.Text = result > 0 ? "You win!" : result < 0 ? "AI wins" : "Draw";
		ReportResult(result);
		_result.ShowResult(result, Difficulty);
		return true;
	}

	private static List<int> Empties(int[] board)
	{
		var list = new List<int>();
		for (var i = 0; i < 9; i++)
			if (board[i] == Empty)
				list.Add(i);
		return list;
	}

	private static int Winner(int[] board)
	{
		foreach (var line in Lines)
		{
			var a = board[line[0]];
			if (a != Empty && a == board[line[1]] && a == board[line[2]])
				return a;
		}
		return Empty;
	}
}
