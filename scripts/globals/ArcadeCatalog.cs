namespace CactusTown;

/// <summary>
/// Static list of House arcade games shown in the gallery. Add an entry here and
/// a matching scene (an <see cref="ArcadeGame"/>) + icon to add a game — the
/// gallery grid grows on its own.
/// </summary>
public static class ArcadeCatalog
{
	public sealed class Entry
	{
		public required string Id;
		public required string Name;
		public required string SceneFile;
		public required string IconFile;

		/// <summary>False for solo games with no AI opponent (still difficulty-scaled).</summary>
		public bool VersusAi = true;

		/// <summary>
		/// True when the game offers a "single player" alternative to its default
		/// mode. The gallery shows a Solo / vs AI toggle on the difficulty screen
		/// and sets <c>ArcadeGame.SoloMode</c> before launch.
		/// </summary>
		public bool SupportsSolo;

		public string ScenePath => $"res://scenes/arcade/{SceneFile}.tscn";
		public string IconPath => $"res://assets/sprites/arcade/{IconFile}.svg";
	}

	public static readonly Entry[] All =
	{
		new() { Id = "tictactoe", Name = "Tic-Tac-Toe", SceneFile = "TicTacToe", IconFile = "icon_tictactoe" },
		new() { Id = "pong", Name = "Pong", SceneFile = "Pong", IconFile = "icon_pong" },
		new() { Id = "pairs", Name = "Match the Pairs", SceneFile = "MatchPairs", IconFile = "icon_pairs", SupportsSolo = true },
		new() { Id = "minesweeper", Name = "Minesweeper", SceneFile = "Minesweeper", IconFile = "icon_minesweeper", VersusAi = false },
		new() { Id = "snake", Name = "Snake", SceneFile = "Snake", IconFile = "icon_snake", VersusAi = false },
		new() { Id = "sudoku", Name = "Sudoku", SceneFile = "Sudoku", IconFile = "icon_sudoku", VersusAi = false },
		new() { Id = "blackjack", Name = "Blackjack", SceneFile = "Blackjack", IconFile = "icon_blackjack" },
	};
}
