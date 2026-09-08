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

		public string ScenePath => $"res://scenes/arcade/{SceneFile}.tscn";
		public string IconPath => $"res://assets/sprites/arcade/{IconFile}.svg";
	}

	public static readonly Entry[] All =
	{
		new() { Id = "tictactoe", Name = "Tic-Tac-Toe", SceneFile = "TicTacToe", IconFile = "icon_tictactoe" },
		new() { Id = "pong", Name = "Pong", SceneFile = "Pong", IconFile = "icon_pong" },
		new() { Id = "pairs", Name = "Match the Pairs", SceneFile = "MatchPairs", IconFile = "icon_pairs" },
		new() { Id = "minesweeper", Name = "Minesweeper", SceneFile = "Minesweeper", IconFile = "icon_minesweeper", VersusAi = false },
	};
}
