using SandBox.BoardGames.Objects;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Tiles;

public class TileMuTorere : Tile1D
{
	public int XLeftTile { get; }

	public int XRightTile { get; }

	public TileMuTorere(GameEntity entity, BoardGameDecal decal, int x, int xLeft, int xRight)
		: base(entity, decal, x)
	{
		XLeftTile = xLeft;
		XRightTile = xRight;
	}
}
