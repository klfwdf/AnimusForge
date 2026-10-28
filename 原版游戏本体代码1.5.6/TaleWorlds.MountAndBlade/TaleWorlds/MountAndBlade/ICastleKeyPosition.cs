using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface ICastleKeyPosition
{
	IPrimarySiegeWeapon AttackerSiegeWeapon { get; set; }

	TacticalPosition MiddlePosition { get; }

	TacticalPosition WaitPosition { get; }

	WorldFrame MiddleFrame { get; }

	WorldFrame DefenseWaitFrame { get; }

	FormationAI.BehaviorSide DefenseSide { get; }

	Vec3 GetPosition();
}
