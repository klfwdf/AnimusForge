using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public interface ITargetable
{
	TargetFlags GetTargetFlags();

	float GetTargetValue(List<Vec3> referencePositions);

	WeakGameEntity GetTargetEntity();

	Vec3 GetTargetingOffset();

	BattleSideEnum GetSide();

	Vec3 GetTargetGlobalVelocity();

	bool IsDestructable();

	WeakGameEntity Entity();

	(Vec3, Vec3) ComputeGlobalPhysicsBoundingBoxMinMax();
}
