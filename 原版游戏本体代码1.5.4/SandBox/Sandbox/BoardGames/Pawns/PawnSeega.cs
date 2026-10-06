using TaleWorlds.Engine;

namespace SandBox.BoardGames.Pawns;

public class PawnSeega : PawnBase
{
	public int X;

	public int Y;

	private int _prevX;

	private int _prevY;

	public override bool IsPlaced
	{
		get
		{
			if (X >= 0 && X < BoardGameSeega.BoardWidth && Y >= 0)
			{
				return Y < BoardGameSeega.BoardHeight;
			}
			return false;
		}
	}

	public bool MovedThisTurn { get; private set; }

	public int PrevX
	{
		get
		{
			return _prevX;
		}
		set
		{
			_prevX = value;
			if (value >= 0)
			{
				MovedThisTurn = true;
			}
			else
			{
				MovedThisTurn = false;
			}
		}
	}

	public int PrevY
	{
		get
		{
			return _prevY;
		}
		set
		{
			_prevY = value;
			if (value >= 0)
			{
				MovedThisTurn = true;
			}
			else
			{
				MovedThisTurn = false;
			}
		}
	}

	public PawnSeega(GameEntity entity, bool playerOne)
		: base(entity, playerOne)
	{
		X = -1;
		Y = -1;
		PrevX = -1;
		PrevY = -1;
		MovedThisTurn = false;
	}

	public override void Reset()
	{
		base.Reset();
		X = -1;
		Y = -1;
		PrevX = -1;
		PrevY = -1;
		MovedThisTurn = false;
	}

	public void UpdateMoveBackAvailable()
	{
		if (MovedThisTurn)
		{
			MovedThisTurn = false;
			return;
		}
		PrevX = -1;
		PrevY = -1;
	}

	public void AISetMovedThisTurn(bool moved)
	{
		MovedThisTurn = moved;
	}
}
