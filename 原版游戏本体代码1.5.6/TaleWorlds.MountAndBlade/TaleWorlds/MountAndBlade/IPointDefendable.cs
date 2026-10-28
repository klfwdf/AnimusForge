using System.Collections.Generic;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public interface IPointDefendable
{
	IEnumerable<DefencePoint> DefencePoints { get; }

	FormationAI.BehaviorSide DefenseSide { get; }

	WorldFrame MiddleFrame { get; }

	WorldFrame DefenseWaitFrame { get; }
}
