using SandBox.BoardGames.Pawns;

namespace SandBox.BoardGames;

public struct TileBaseInformation(ref PawnBase pawnOnTile)
{
	public PawnBase PawnOnTile = pawnOnTile;
}
