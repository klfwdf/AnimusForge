namespace Helpers;

public static class BoardGameHelper
{
	public enum AIDifficulty
	{
		Easy,
		Normal,
		Hard,
		NumTypes
	}

	public enum BoardGameState
	{
		None,
		Win,
		Loss,
		Draw
	}
}
