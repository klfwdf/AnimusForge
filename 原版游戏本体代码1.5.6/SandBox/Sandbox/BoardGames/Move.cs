using SandBox.BoardGames.Pawns;
using SandBox.BoardGames.Tiles;

namespace SandBox.BoardGames;

public struct Move
{
	public static readonly Move Invalid = new Move
	{
		Unit = null,
		GoalTile = null
	};

	public PawnBase Unit;

	public TileBase GoalTile;

	public bool IsValid
	{
		get
		{
			if (Unit != null)
			{
				return GoalTile != null;
			}
			return false;
		}
	}

	public Move(PawnBase unit, TileBase goalTile)
	{
		Unit = unit;
		GoalTile = goalTile;
	}
}
