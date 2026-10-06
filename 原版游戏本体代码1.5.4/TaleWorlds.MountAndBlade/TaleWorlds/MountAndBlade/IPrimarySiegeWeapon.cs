using System.Collections.Generic;

namespace TaleWorlds.MountAndBlade;

public interface IPrimarySiegeWeapon
{
	float SiegeWeaponPriority { get; }

	int OverTheWallNavMeshID { get; }

	bool HoldLadders { get; }

	bool SendLadders { get; }

	MissionObject TargetCastlePosition { get; }

	FormationAI.BehaviorSide WeaponSide { get; }

	bool HasCompletedAction();

	bool GetNavmeshFaceIds(out List<int> navmeshFaceIds);
}
