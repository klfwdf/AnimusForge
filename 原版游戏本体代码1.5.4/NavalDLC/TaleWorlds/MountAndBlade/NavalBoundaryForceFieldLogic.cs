using NavalDLC.Missions;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class NavalBoundaryForceFieldLogic : MissionLogic
{
	private const float SoftStart = 20f;

	private const float HardStop = 0.25f;

	private const float MaxAcceleleration = 6f;

	private const float VRef = 3f;

	private const float SeparationVelocityGain = 4f;

	private const float Damping = 2f;

	private MBList<Vec2> _hardBoundaryPoints;

	private NavalShipsLogic _navalShipsLogic;

	public MBReadOnlyList<Vec2> HardBoundaryPoints => _hardBoundaryPoints;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_hardBoundaryPoints = new MBList<Vec2>();
		_navalShipsLogic = base.Mission.GetMissionBehavior<NavalShipsLogic>();
	}

	public override void OnAfterDeploymentFinished()
	{
		_hardBoundaryPoints = MBSceneUtilities.GetHardBoundaryPoints(Mission.Current.Scene);
	}

	public override void OnFixedMissionTick(float fixedDt)
	{
		if (!base.Mission.IsDeploymentFinished)
		{
			return;
		}
		float num = 0f;
		foreach (MissionShip allShip in _navalShipsLogic.AllShips)
		{
			num = MathF.Max(num, allShip.Physics.PhysicsBoundingBoxWithChildren.radius);
		}
		float num2 = 20f + num;
		float num3 = num2 * num2;
		foreach (MissionShip allShip2 in _navalShipsLogic.AllShips)
		{
			if (!allShip2.IsShipOrderActive || allShip2.ShipOrder.MovementOrderEnum == ShipOrder.ShipMovementOrderEnum.Retreat)
			{
				continue;
			}
			Vec3 origin = allShip2.GameEntity.GetBodyWorldTransform().origin;
			Vec2 position = origin.AsVec2;
			Vec2 closestPoint;
			bool isPositionInsideBoundaries;
			float num4 = MBSceneUtilities.FindClosestPointToBoundariesReturnDistanceSquared(in position, _hardBoundaryPoints, out closestPoint, out isPositionInsideBoundaries);
			Vec3 vec = (position - closestPoint).ToVec3();
			if (!(num4 >= 1E-05f) || !(num4 <= num3))
			{
				continue;
			}
			float num5 = vec.Normalize();
			float radius = allShip2.Physics.PhysicsBoundingBoxWithoutChildren.radius;
			float length = ((origin - vec * radius).AsVec2 - closestPoint).Length;
			float num6 = MathF.Max(19.75f, 0.001f);
			if (!(length <= 20f))
			{
				continue;
			}
			float mass = allShip2.Physics.Mass;
			float num7 = Vec3.DotProduct(allShip2.Physics.LinearVelocity, -vec);
			float num8 = 20f - (length - 0.25f);
			float num9 = MathF.Clamp(num8 / num6, 0f, 1f);
			float num10 = MathF.Clamp(num7 / 3f, 0f, 1f);
			float num11 = num9 * (0.5f + 0.5f * num10);
			if (num8 >= num6)
			{
				if (num7 > 0f)
				{
					Vec3 forceVec = vec * (num7 * mass);
					allShip2.Physics.ApplyForceToDynamicBody(in forceVec, GameEntityPhysicsExtensions.ForceMode.Impulse);
					num7 = 0f;
				}
				float num12 = 4f * (num8 - num6);
				if (num12 > 0f)
				{
					float num13 = num12 - num7;
					if (num13 > 0f)
					{
						Vec3 forceVec2 = vec * (mass * num13);
						allShip2.Physics.ApplyForceToDynamicBody(in forceVec2, GameEntityPhysicsExtensions.ForceMode.Impulse);
					}
				}
			}
			if (num8 > 0f || num5 <= radius + 20f)
			{
				float num14 = 6f * (0.25f + 0.75f * num11);
				Vec3 forceVec3 = vec * (num14 * mass);
				allShip2.Physics.ApplyForceToDynamicBody(in forceVec3);
			}
			if (num7 > 0f)
			{
				allShip2.Physics.ApplyForceToDynamicBody(vec * (2f * num7 * mass));
			}
		}
	}
}
