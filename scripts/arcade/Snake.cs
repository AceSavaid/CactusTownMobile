using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// Solo Snake. Swipe (or arrow keys) to steer; eat pellets to grow. Difficulty
/// sets the speed and the target length — reach it to win, hit a wall or your
/// own tail to lose.
/// </summary>
public partial class Snake : ArcadeGame
{
	private const int Cols = 21, Rows = 15;

	private static readonly Color GridColor = new(0.16f, 0.19f, 0.17f);
	private static readonly Color BodyColor = new(0.42f, 0.75f, 0.45f);
	private static readonly Color HeadColor = new(0.62f, 0.85f, 0.64f);
	private static readonly Color FoodColor = new(0.88f, 0.63f, 0.61f);

	private float _stepInterval = 1f / 6f;
	private int _targetLength = 15;

	private readonly LinkedList<Vector2I> _snake = new();
	private readonly HashSet<Vector2I> _occupied = new();
	private Vector2I _dir = Vector2I.Right;
	private Vector2I _pendingDir = Vector2I.Right;
	private Vector2I _food;
	private double _acc;
	private bool _running;
	private bool _roundOver;

	private Control _board = null!;
	private Label _status = null!;
	private ArcadeResult _result = null!;

	public override void Configure(int difficulty)
	{
		base.Configure(difficulty);
		(_stepInterval, _targetLength) = difficulty switch
		{
			1 => (1f / 6f, 15),
			2 => (1f / 8f, 22),
			_ => (1f / 10f, 30),
		};
	}

	public override void _Ready()
	{
		base._Ready();
		_board = GetNode<Control>("%Board");
		_status = GetNode<Label>("%Status");
		_result = GetNode<ArcadeResult>("%Result");
		_result.PlayAgain += NewGame;
		_result.Leave += Close;
		GetNode<Button>("%LeaveButton").Pressed += Close;
		_board.Draw += DrawBoard;
		NewGame();
	}

	private void NewGame()
	{
		_snake.Clear();
		_occupied.Clear();
		var start = new Vector2I(Cols / 2, Rows / 2);
		for (var i = 2; i >= 0; i--)
		{
			var cell = new Vector2I(start.X - i, start.Y);
			_snake.AddLast(cell);
			_occupied.Add(cell);
		}
		_dir = _pendingDir = Vector2I.Right;
		_acc = 0;
		_running = true;
		_roundOver = false;
		PlaceFood();
		UpdateStatus();
		_board.QueueRedraw();
	}

	public override void _Input(InputEvent @event)
	{
		if (_roundOver)
			return;

		if (@event is InputEventScreenDrag drag)
		{
			var r = drag.Relative;
			if (r.LengthSquared() < 40f)
				return;
			Steer(Mathf.Abs(r.X) > Mathf.Abs(r.Y)
				? new Vector2I(r.X > 0 ? 1 : -1, 0)
				: new Vector2I(0, r.Y > 0 ? 1 : -1));
		}
		else if (@event.IsActionPressed("ui_left")) Steer(Vector2I.Left);
		else if (@event.IsActionPressed("ui_right")) Steer(Vector2I.Right);
		else if (@event.IsActionPressed("ui_up")) Steer(Vector2I.Up);
		else if (@event.IsActionPressed("ui_down")) Steer(Vector2I.Down);
	}

	private void Steer(Vector2I d)
	{
		// No instant 180° reversal.
		if (d + _dir == Vector2I.Zero)
			return;
		_pendingDir = d;
	}

	public override void _Process(double delta)
	{
		if (!_running || _roundOver)
			return;
		_acc += delta;
		if (_acc < _stepInterval)
			return;
		_acc -= _stepInterval;
		Step();
	}

	private void Step()
	{
		_dir = _pendingDir;
		var head = _snake.Last!.Value + _dir;

		var hitWall = head.X < 0 || head.X >= Cols || head.Y < 0 || head.Y >= Rows;
		var tail = _snake.First!.Value;
		var eating = head == _food;
		// The tail cell frees up this step unless we're growing into it.
		var hitSelf = _occupied.Contains(head) && !(head == tail && !eating);
		if (hitWall || hitSelf)
		{
			EndGame(false);
			return;
		}

		_snake.AddLast(head);
		_occupied.Add(head);
		if (eating)
		{
			if (_snake.Count >= _targetLength)
			{
				UpdateStatus();
				_board.QueueRedraw();
				EndGame(true);
				return;
			}
			PlaceFood();
		}
		else
		{
			_occupied.Remove(tail);
			_snake.RemoveFirst();
		}

		UpdateStatus();
		_board.QueueRedraw();
	}

	private void PlaceFood()
	{
		var free = new List<Vector2I>();
		for (var y = 0; y < Rows; y++)
		for (var x = 0; x < Cols; x++)
		{
			var c = new Vector2I(x, y);
			if (!_occupied.Contains(c))
				free.Add(c);
		}
		if (free.Count == 0)
			return;
		_food = free[GD.RandRange(0, free.Count - 1)];
	}

	private void EndGame(bool won)
	{
		_running = false;
		_roundOver = true;
		_status.Text = won ? "Full length!" : "Crashed!";
		ReportResult(won ? 1 : -1);
		_result.ShowResult(won ? 1 : -1, Difficulty);
	}

	private void UpdateStatus() =>
		_status.Text = $"Length {_snake.Count} / {_targetLength}";

	private void DrawBoard()
	{
		var size = _board.Size;
		var cell = Mathf.Floor(Mathf.Min(size.X / Cols, size.Y / Rows));
		if (cell < 4f)
			return;
		var ox = (size.X - cell * Cols) * 0.5f;
		var oy = (size.Y - cell * Rows) * 0.5f;

		_board.DrawRect(new Rect2(ox, oy, cell * Cols, cell * Rows), GridColor);

		Rect2 CellRect(Vector2I c, float pad) =>
			new(ox + c.X * cell + pad, oy + c.Y * cell + pad, cell - pad * 2, cell - pad * 2);

		_board.DrawRect(CellRect(_food, cell * 0.22f), FoodColor);

		var head = _snake.Last!.Value;
		foreach (var seg in _snake)
			_board.DrawRect(CellRect(seg, cell * 0.10f), seg == head ? HeadColor : BodyColor);
	}
}
