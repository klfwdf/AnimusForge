using SandBox.BoardGames.Pawns;

namespace SandBox.BoardGames;

public struct TileBaseInformation
{
	public PawnBase PawnOnTile;

	public TileBaseInformation(ref PawnBase pawnOnTile)
	{
		PawnOnTile = pawnOnTile;
	}
}
