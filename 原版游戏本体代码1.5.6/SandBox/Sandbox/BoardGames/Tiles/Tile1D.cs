using SandBox.BoardGames.Objects;
using TaleWorlds.Engine;

namespace SandBox.BoardGames.Tiles;

public class Tile1D : TileBase
{
	public int X { get; }

	public Tile1D(GameEntity entity, BoardGameDecal decal, int x)
		: base(entity, decal)
	{
		X = x;
	}
}
