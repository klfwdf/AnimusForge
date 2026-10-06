using SandBox.BoardGames.Objects;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Tiles;

public class Tile2D : TileBase
{
	public int X { get; }

	public int Y { get; }

	public Tile2D(GameEntity entity, BoardGameDecal decal, int x, int y)
		: base(entity, decal)
	{
		X = x;
		Y = y;
	}
}
