using TaleWorlds.Engine;

namespace SandBox.BoardGames.Pawns;

public class PawnTablut : PawnBase
{
	public int X;

	public int Y;

	public override bool IsPlaced
	{
		get
		{
			if (X >= 0 && X < 9 && Y >= 0)
			{
				return Y < 9;
			}
			return false;
		}
	}

	public PawnTablut(GameEntity entity, bool playerOne)
		: base(entity, playerOne)
	{
		X = -1;
		Y = -1;
	}

	public override void Reset()
	{
		base.Reset();
		X = -1;
		Y = -1;
	}
}
