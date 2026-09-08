using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// Blackjack vs the dealer. Get closer to 21 than the dealer without going over;
/// the dealer draws to 17. Every hand stakes one chip: win to grow your stack,
/// bust or lose and it shrinks. You start on 5 — reach the target stack to win
/// the game, hit zero and it's over. Difficulty sets the target.
/// </summary>
public partial class Blackjack : ArcadeGame
{
	private static readonly string[] Ranks =
		{ "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
	private static readonly string[] Suits = { "♠", "♥", "♦", "♣" };

	private const int StartChips = 5;

	private int _target = 9;
	private int _chips;

	private readonly List<int> _deck = new();
	private readonly List<int> _player = new();
	private readonly List<int> _dealer = new();

	private bool _dealerHidden = true;
	private bool _handActive;
	private bool _gameOver;

	private Label _status = null!;
	private Label _message = null!;
	private Label _dealerCards = null!;
	private Label _playerCards = null!;
	private Button _hitButton = null!;
	private Button _standButton = null!;
	private ArcadeResult _result = null!;

	public override void Configure(int difficulty)
	{
		base.Configure(difficulty);
		_target = difficulty switch { 1 => 7, 2 => 9, _ => 11 };
	}

	public override void _Ready()
	{
		base._Ready();
		_status = GetNode<Label>("%Status");
		_message = GetNode<Label>("%Message");
		_dealerCards = GetNode<Label>("%DealerCards");
		_playerCards = GetNode<Label>("%PlayerCards");
		_hitButton = GetNode<Button>("%HitButton");
		_standButton = GetNode<Button>("%StandButton");
		_result = GetNode<ArcadeResult>("%Result");

		_result.PlayAgain += NewGame;
		_result.Leave += Close;
		GetNode<Button>("%LeaveButton").Pressed += Close;
		_hitButton.Pressed += OnHit;
		_standButton.Pressed += OnStand;

		NewGame();
	}

	private void NewGame()
	{
		_chips = StartChips;
		_gameOver = false;
		DealHand();
	}

	private void DealHand()
	{
		_player.Clear();
		_dealer.Clear();
		BuildDeck();
		_player.Add(DrawCard());
		_dealer.Add(DrawCard());
		_player.Add(DrawCard());
		_dealer.Add(DrawCard());
		_dealerHidden = true;
		_handActive = true;
		SetButtons(true);

		var playerBj = HandValue(_player) == 21;
		var dealerBj = HandValue(_dealer) == 21;
		if (playerBj || dealerBj)
		{
			_dealerHidden = false;
			if (playerBj && dealerBj)
				Settle(0, "Both blackjack — push");
			else if (playerBj)
				Settle(2, "Blackjack!");
			else
				Settle(-1, "Dealer blackjack");
			return;
		}

		_message.Text = "Hit or stand?";
		Redraw();
	}

	private void OnHit()
	{
		if (!_handActive)
			return;
		_player.Add(DrawCard());
		if (HandValue(_player) > 21)
		{
			_dealerHidden = false;
			Settle(-1, "Bust!");
		}
		else
		{
			Redraw();
		}
	}

	private async void OnStand()
	{
		if (!_handActive)
			return;
		_handActive = false;
		SetButtons(false);
		_dealerHidden = false;
		Redraw();

		while (HandValue(_dealer) < 17)
		{
			await ToSignal(GetTree().CreateTimer(0.55), SceneTreeTimer.SignalName.Timeout);
			_dealer.Add(DrawCard());
			Redraw();
		}

		var pv = HandValue(_player);
		var dv = HandValue(_dealer);
		if (dv > 21)
			Settle(1, "Dealer busts — you win");
		else if (pv > dv)
			Settle(1, "Closer to 21 — you win");
		else if (pv < dv)
			Settle(-1, "Dealer wins the hand");
		else
			Settle(0, "Push");
	}

	/// <summary>chipDelta: +2 natural blackjack, +1 win, 0 push, -1 loss.</summary>
	private async void Settle(int chipDelta, string note)
	{
		_handActive = false;
		SetButtons(false);
		_chips += chipDelta;
		_message.Text = note;
		Redraw();
		Audio.Instance?.PlaySfx(chipDelta > 0 ? "confirm" : chipDelta < 0 ? "cancel" : "click");

		await ToSignal(GetTree().CreateTimer(1.4), SceneTreeTimer.SignalName.Timeout);

		if (_chips <= 0)
			EndGame(false, "Out of chips");
		else if (_chips >= _target)
			EndGame(true, "Target reached!");
		else
			DealHand();
	}

	private void EndGame(bool won, string note)
	{
		_gameOver = true;
		_message.Text = note;
		Redraw();
		ReportResult(won ? 1 : -1);
		_result.ShowResult(won ? 1 : -1, Difficulty);
	}

	private void Redraw()
	{
		_dealerCards.Text = "Dealer:  " + Show(_dealer, _dealerHidden);
		_playerCards.Text = "You:  " + Show(_player, false) + $"   ({HandValue(_player)})";
		UpdateStatus();
	}

	private void UpdateStatus() =>
		_status.Text = _gameOver
			? $"Chips: {Mathf.Max(0, _chips)}"
			: $"Chips: {_chips}  ·  reach {_target} to win";

	private void SetButtons(bool on)
	{
		_hitButton.Disabled = !on;
		_standButton.Disabled = !on;
	}

	private string Show(List<int> hand, bool hideSecond)
	{
		var parts = new List<string>();
		for (var i = 0; i < hand.Count; i++)
			parts.Add(i == 1 && hideSecond ? "??" : CardName(hand[i]));
		return string.Join("  ", parts);
	}

	private static string CardName(int card) => Ranks[card % 13] + Suits[card / 13];

	private static int CardValue(int card)
	{
		var r = card % 13;
		if (r == 0) return 11;
		return r >= 9 ? 10 : r + 1;
	}

	private static int HandValue(List<int> hand)
	{
		var total = 0;
		var aces = 0;
		foreach (var c in hand)
		{
			total += CardValue(c);
			if (c % 13 == 0)
				aces++;
		}
		while (total > 21 && aces > 0)
		{
			total -= 10;
			aces--;
		}
		return total;
	}

	private void BuildDeck()
	{
		_deck.Clear();
		for (var i = 0; i < 52; i++)
			_deck.Add(i);
		for (var i = _deck.Count - 1; i > 0; i--)
		{
			var j = (int)(GD.Randi() % (uint)(i + 1));
			(_deck[i], _deck[j]) = (_deck[j], _deck[i]);
		}
	}

	private int DrawCard()
	{
		if (_deck.Count == 0)
			BuildDeck();
		var card = _deck[^1];
		_deck.RemoveAt(_deck.Count - 1);
		return card;
	}
}
