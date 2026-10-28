using TaleWorlds.Engine;

namespace SandBox.BoardGames.Pawns;

public class PawnKonane : PawnBase
{
	public int X;

	public int Y;

	public int PrevX;

	public int PrevY;

	public override bool IsPlaced
	{
		get
		{
			if (X >= 0 && X < BoardGameKonane.BoardWidth && Y >= 0)
			{
				return Y < BoardGameKonane.BoardHeight;
			}
			return false;
		}
	}

	public PawnKonane(GameEntity entity, bool playerOne)
		: base(entity, playerOne)
	{
		X = -1;
		Y = -1;
		PrevX = -1;
		PrevY = -1;
	}

	public override void Reset()
	{
		base.Reset();
		X = -1;
		Y = -1;
		PrevX = -1;
		PrevY = -1;
	}
}
