using Godot;

namespace CactusTown;

/// <summary>
/// Pong vs an AI paddle. Drag anywhere to move your paddle (left). First to five.
/// Easy / Medium / Hard change the AI paddle's speed; Hard also predicts bounces.
/// </summary>
public partial class Pong : ArcadeGame
{
	private const int WinScore = 5;
	private const float PaddleH = 170f, PaddleW = 22f, BallSize = 26f, Inset = 44f;

	private float _aiSpeed = 300f;
	private bool _aiPredicts;

	private Vector2 _fieldSize;
	private bool _matchStarted;
	private bool _running;
	private bool _roundOver;

	private float _playerY, _aiY;
	private Vector2 _ballPos, _ballVel;
	private float _ballSpeed;
	private int _playerScore, _aiScore;

	private Control _field = null!;
	private ColorRect _playerPaddle = null!, _aiPaddle = null!, _ball = null!;
	private Label _score = null!;
	private ArcadeResult _result = null!;

	public override void Configure(int difficulty)
	{
		base.Configure(difficulty);
		_aiSpeed = difficulty switch { 1 => 300f, 2 => 480f, _ => 720f };
		_aiPredicts = difficulty == 3;
	}

	public override void _Ready()
	{
		base._Ready();
		_field = GetNode<Control>("%Field");
		_playerPaddle = GetNode<ColorRect>("%PlayerPaddle");
		_aiPaddle = GetNode<ColorRect>("%AiPaddle");
		_ball = GetNode<ColorRect>("%Ball");
		_score = GetNode<Label>("%Score");
		_result = GetNode<ArcadeResult>("%Result");

		_result.PlayAgain += StartMatch;
		_result.Leave += Close;
		GetNode<Button>("%LeaveButton").Pressed += Close;

		_playerPaddle.Size = new Vector2(PaddleW, PaddleH);
		_aiPaddle.Size = new Vector2(PaddleW, PaddleH);
		_ball.Size = new Vector2(BallSize, BallSize);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventScreenDrag drag)
			MovePlayer(drag.Position.Y);
		else if (@event is InputEventScreenTouch { Pressed: true } touch)
			MovePlayer(touch.Position.Y);
	}

	private void MovePlayer(float viewportY)
	{
		var localY = viewportY - _field.GlobalPosition.Y;
		_playerY = Mathf.Clamp(localY, PaddleH * 0.5f, _fieldSize.Y - PaddleH * 0.5f);
	}

	public override void _Process(double delta)
	{
		_fieldSize = _field.Size;
		if (_fieldSize.X < 1f)
			return;
		if (!_matchStarted)
		{
			_matchStarted = true;
			StartMatch();
		}
		if (!_running || _roundOver)
		{
			LayOut();
			return;
		}

		var dt = (float)delta;

		var target = _aiPredicts && _ballVel.X > 0f ? PredictBallY() : _ballPos.Y;
		target = Mathf.Clamp(target, PaddleH * 0.5f, _fieldSize.Y - PaddleH * 0.5f);
		_aiY = Mathf.MoveToward(_aiY, target, _aiSpeed * dt);

		_ballPos += _ballVel * _ballSpeed * dt;

		if (_ballPos.Y < BallSize * 0.5f)
		{
			_ballPos.Y = BallSize * 0.5f;
			_ballVel.Y = Mathf.Abs(_ballVel.Y);
		}
		else if (_ballPos.Y > _fieldSize.Y - BallSize * 0.5f)
		{
			_ballPos.Y = _fieldSize.Y - BallSize * 0.5f;
			_ballVel.Y = -Mathf.Abs(_ballVel.Y);
		}

		if (_ballVel.X < 0f && _ballPos.X - BallSize * 0.5f <= Inset + PaddleW && _ballPos.X > Inset
			&& Mathf.Abs(_ballPos.Y - _playerY) < PaddleH * 0.5f + BallSize * 0.4f)
			Bounce(_playerY, 1f);

		var aiX = _fieldSize.X - Inset;
		if (_ballVel.X > 0f && _ballPos.X + BallSize * 0.5f >= aiX - PaddleW && _ballPos.X < aiX
			&& Mathf.Abs(_ballPos.Y - _aiY) < PaddleH * 0.5f + BallSize * 0.4f)
			Bounce(_aiY, -1f);

		if (_ballPos.X < -60f)
			Point(false);
		else if (_ballPos.X > _fieldSize.X + 60f)
			Point(true);

		LayOut();
	}

	private void StartMatch()
	{
		_playerScore = _aiScore = 0;
		_roundOver = false;
		_playerY = _aiY = _fieldSize.Y * 0.5f;
		UpdateScore();
		ResetBall(GD.Randf() < 0.5f ? -1f : 1f);
	}

	private void ResetBall(float dir)
	{
		_ballPos = _fieldSize * 0.5f;
		_ballSpeed = 560f;
		var angle = (float)GD.RandRange(-0.45, 0.45);
		_ballVel = new Vector2(dir * Mathf.Cos(angle), Mathf.Sin(angle)).Normalized();
		_running = true;
	}

	private void Bounce(float paddleY, float dir)
	{
		var offset = Mathf.Clamp((_ballPos.Y - paddleY) / (PaddleH * 0.5f), -1f, 1f);
		_ballVel = new Vector2(dir * 0.86f, offset * 0.92f).Normalized();
		_ballSpeed = Mathf.Min(_ballSpeed + 42f, 1020f);
	}

	private float PredictBallY()
	{
		var dist = _fieldSize.X - Inset - _ballPos.X;
		if (_ballVel.X <= 0f)
			return _ballPos.Y;
		var t = dist / (_ballVel.X * _ballSpeed);
		var y = _ballPos.Y + _ballVel.Y * _ballSpeed * t;
		var span = _fieldSize.Y;
		y = Mathf.PosMod(y, 2f * span);
		return y > span ? 2f * span - y : y;
	}

	private void Point(bool playerScored)
	{
		if (playerScored)
			_playerScore++;
		else
			_aiScore++;
		UpdateScore();

		if (_playerScore >= WinScore)
			EndMatch(1);
		else if (_aiScore >= WinScore)
			EndMatch(-1);
		else
			ResetBall(playerScored ? -1f : 1f);
	}

	private void EndMatch(int result)
	{
		_running = false;
		_roundOver = true;
		ReportResult(result);
		_result.ShowResult(result, Difficulty);
	}

	private void UpdateScore() => _score.Text = $"{_playerScore}    —    {_aiScore}";

	private void LayOut()
	{
		_playerPaddle.Position = new Vector2(Inset - PaddleW * 0.5f, _playerY - PaddleH * 0.5f);
		_aiPaddle.Position = new Vector2(_fieldSize.X - Inset - PaddleW * 0.5f, _aiY - PaddleH * 0.5f);
		_ball.Position = _ballPos - _ball.Size * 0.5f;
	}
}
