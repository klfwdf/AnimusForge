using TaleWorlds.Engine;

namespace SandBox.BoardGames.Pawns;

public class PawnMuTorere : PawnBase
{
	public int X { get; set; }

	public override bool IsPlaced => true;

	public PawnMuTorere(GameEntity entity, bool playerOne)
		: base(entity, playerOne)
	{
		X = -1;
	}

	public override void Reset()
	{
		base.Reset();
		X = -1;
	}
}
