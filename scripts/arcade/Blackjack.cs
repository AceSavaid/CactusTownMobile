using System.Collections.Generic;
using Godot;

namespace CactusTown;

/// <summary>
/// Blackjack vs the dealer over a short match. Hit or stand; the dealer draws to
/// 17. Win more hands than the dealer across the match to win. Difficulty sets
/// how many hands are played (Easy 3, Medium 5, Hard 7).
/// </summary>
public partial class Blackjack : ArcadeGame
{
	private static readonly string[] Ranks =
		{ "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
	private static readonly string[] Suits = { "♠", "♥", "♦", "♣" };

	private int _handsInMatch = 5;

	private readonly List<int> _deck = new();
	private readonly List<int> _player = new();
	private readonly List<int> _dealer = new();

	private int _handNo;
	private int _playerWins, _dealerWins;
	private bool _dealerHidden = true;
	private bool _handActive;
	private bool _matchOver;

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
		_handsInMatch = difficulty switch { 1 => 3, 2 => 5, _ => 7 };
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

		_result.PlayAgain += NewMatch;
		_result.Leave += Close;
		GetNode<Button>("%LeaveButton").Pressed += Close;
		_hitButton.Pressed += OnHit;
		_standButton.Pressed += OnStand;

		NewMatch();
	}

	private void NewMatch()
	{
		_handNo = 0;
		_playerWins = _dealerWins = 0;
		_matchOver = false;
		DealHand();
	}

	private void DealHand()
	{
		_handNo++;
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
			Settle(playerBj && !dealerBj ? 1 : dealerBj && !playerBj ? -1 : 0,
				playerBj && dealerBj ? "Both blackjack — push"
				: playerBj ? "Blackjack!" : "Dealer blackjack");
			return;
		}

		_message.Text = "Your move";
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
		var outcome = dv > 21 || pv > dv ? 1 : pv < dv ? -1 : 0;
		Settle(outcome,
			dv > 21 ? "Dealer busts — you win"
			: outcome > 0 ? "You win the hand"
			: outcome < 0 ? "Dealer wins the hand"
			: "Push");
	}

	private async void Settle(int outcome, string note)
	{
		_handActive = false;
		SetButtons(false);
		if (outcome > 0) _playerWins++;
		else if (outcome < 0) _dealerWins++;

		_message.Text = note;
		Redraw();
		Audio.Instance?.PlaySfx(outcome > 0 ? "confirm" : "cancel");

		await ToSignal(GetTree().CreateTimer(1.4), SceneTreeTimer.SignalName.Timeout);

		var handsLeft = _handNo < _handsInMatch;
		var decided = _playerWins > _handsInMatch / 2 || _dealerWins > _handsInMatch / 2;
		if (handsLeft && !decided)
		{
			DealHand();
			return;
		}

		_matchOver = true;
		var result = _playerWins > _dealerWins ? 1 : _playerWins < _dealerWins ? -1 : 0;
		UpdateStatus();
		ReportResult(result);
		_result.ShowResult(result, Difficulty);
	}

	private void Redraw()
	{
		_dealerCards.Text = "Dealer:  " + Show(_dealer, _dealerHidden);
		_playerCards.Text = "You:  " + Show(_player, false) + $"   ({HandValue(_player)})";
		UpdateStatus();
	}

	private void UpdateStatus()
	{
		var stage = _matchOver ? "Match over" : $"Hand {_handNo} / {_handsInMatch}";
		_status.Text = $"{stage}      You {_playerWins} – {_dealerWins} Dealer";
	}

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
