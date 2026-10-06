using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.NavalPhysics;

public class NavalPhysicsAgentCollisionSolver : ScriptComponentBehavior
{
	private const float CutoffDistance = 0.6f;

	private const float CollisionAcceleration = 2f;

	private NavalPhysics _floatableEntityNavalPhysicsScript;

	private MBList<Agent> _nearbyAgentsCache;

	private Vec3[] _floatableMeshBoundingBoxGlobalVertices;

	private Vec3 _forceToBeAppliedOnFixedTick;

	private Vec3 _torqueToBeAppliedOnFixedTick;

	protected override void OnInit()
	{
		_nearbyAgentsCache = new MBList<Agent>(5);
		_floatableEntityNavalPhysicsScript = base.GameEntity.GetFirstScriptOfType<NavalPhysics>();
		_floatableMeshBoundingBoxGlobalVertices = new Vec3[8];
		_forceToBeAppliedOnFixedTick = Vec3.Zero;
		_torqueToBeAppliedOnFixedTick = Vec3.Zero;
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.FixedTick | TickRequirement.FixedParallelTick | base.GetTickRequirement();
	}

	private bool IsPointInsideLocalBoundingBox(MatrixFrame globalFrame, Vec3 point, float margin)
	{
		Vec3 vec = globalFrame.TransformToLocal(in point);
		BoundingBox physicsBoundingBoxWithChildren = _floatableEntityNavalPhysicsScript.PhysicsBoundingBoxWithChildren;
		if (vec.x > physicsBoundingBoxWithChildren.min.x - margin && vec.y > physicsBoundingBoxWithChildren.min.y - margin && vec.z > physicsBoundingBoxWithChildren.min.z - margin && vec.x - margin < physicsBoundingBoxWithChildren.max.x && vec.y - margin < physicsBoundingBoxWithChildren.max.y && vec.z - margin < physicsBoundingBoxWithChildren.max.z)
		{
			return true;
		}
		return false;
	}

	private void UpdateFloatableMeshBoundingBoxGlobalVertices(MatrixFrame globalFrame)
	{
		BoundingBox physicsBoundingBoxWithChildren = _floatableEntityNavalPhysicsScript.PhysicsBoundingBoxWithChildren;
		_floatableMeshBoundingBoxGlobalVertices[0] = globalFrame.TransformToParent(new Vec3(physicsBoundingBoxWithChildren.min.x, physicsBoundingBoxWithChildren.min.y, physicsBoundingBoxWithChildren.min.z));
		_floatableMeshBoundingBoxGlobalVertices[1] = globalFrame.TransformToParent(new Vec3(physicsBoundingBoxWithChildren.min.x, physicsBoundingBoxWithChildren.max.y, physicsBoundingBoxWithChildren.min.z));
		_floatableMeshBoundingBoxGlobalVertices[2] = globalFrame.TransformToParent(new Vec3(physicsBoundingBoxWithChildren.max.x, physicsBoundingBoxWithChildren.max.y, physicsBoundingBoxWithChildren.min.z));
		_floatableMeshBoundingBoxGlobalVertices[3] = globalFrame.TransformToParent(new Vec3(physicsBoundingBoxWithChildren.max.x, physicsBoundingBoxWithChildren.min.y, physicsBoundingBoxWithChildren.min.z));
		_floatableMeshBoundingBoxGlobalVertices[4] = globalFrame.TransformToParent(new Vec3(physicsBoundingBoxWithChildren.min.x, physicsBoundingBoxWithChildren.min.y, physicsBoundingBoxWithChildren.max.z));
		_floatableMeshBoundingBoxGlobalVertices[5] = globalFrame.TransformToParent(new Vec3(physicsBoundingBoxWithChildren.min.x, physicsBoundingBoxWithChildren.max.y, physicsBoundingBoxWithChildren.max.z));
		_floatableMeshBoundingBoxGlobalVertices[6] = globalFrame.TransformToParent(new Vec3(physicsBoundingBoxWithChildren.max.x, physicsBoundingBoxWithChildren.max.y, physicsBoundingBoxWithChildren.max.z));
		_floatableMeshBoundingBoxGlobalVertices[7] = globalFrame.TransformToParent(new Vec3(physicsBoundingBoxWithChildren.max.x, physicsBoundingBoxWithChildren.min.y, physicsBoundingBoxWithChildren.max.z));
	}

	protected override void OnFixedTick(float fixedDt)
	{
		if (_forceToBeAppliedOnFixedTick.LengthSquared > 0f)
		{
			_floatableEntityNavalPhysicsScript.ApplyForceToDynamicBody(in _forceToBeAppliedOnFixedTick);
			_floatableEntityNavalPhysicsScript.ApplyTorque(in _torqueToBeAppliedOnFixedTick);
		}
	}

	protected override void OnParallelFixedTick(float fixedDt)
	{
		_forceToBeAppliedOnFixedTick = Vec3.Zero;
		_torqueToBeAppliedOnFixedTick = Vec3.Zero;
		BoundingBox physicsBoundingBoxWithChildren = _floatableEntityNavalPhysicsScript.PhysicsBoundingBoxWithChildren;
		MatrixFrame globalMassFrame = _floatableEntityNavalPhysicsScript.GetGlobalMassFrame();
		MatrixFrame bodyWorldTransform = base.GameEntity.GetBodyWorldTransform();
		UpdateFloatableMeshBoundingBoxGlobalVertices(bodyWorldTransform);
		Vec3 vec = Vec3.Vec3Max(physicsBoundingBoxWithChildren.min, physicsBoundingBoxWithChildren.max);
		Mission.Current.GetNearbyAgents(bodyWorldTransform.origin.AsVec2, vec.Length + 0.6f, _nearbyAgentsCache);
		foreach (Agent item in _nearbyAgentsCache)
		{
			if (!item.IsInWater())
			{
				continue;
			}
			Vec3 point = item.GetEyeGlobalPosition();
			Vec3 vec2 = Vec3.Invalid;
			float num = float.MaxValue;
			if (IsPointInsideLocalBoundingBox(bodyWorldTransform, point, -0.05f))
			{
				Vec3 vec3 = -item.Frame.rotation.f;
				float num2 = MathF.Min(_floatableEntityNavalPhysicsScript.Mass, item.GetTotalMass());
				Vec3 vec4 = vec3 * num2 * 2f * 5f;
				_forceToBeAppliedOnFixedTick += vec4;
				continue;
			}
			for (int i = 0; i < 4; i++)
			{
				Vec3 lineSegmentBegin = _floatableMeshBoundingBoxGlobalVertices[i];
				Vec3 lineSegmentEnd = _floatableMeshBoundingBoxGlobalVertices[(i + 1) % 4];
				Vec3 closestPointOnLineSegmentToPoint = MBMath.GetClosestPointOnLineSegmentToPoint(in lineSegmentBegin, in lineSegmentEnd, in point);
				float num3 = closestPointOnLineSegmentToPoint.DistanceSquared(point);
				if (num3 < num)
				{
					num = num3;
					vec2 = closestPointOnLineSegmentToPoint;
				}
				Vec3 lineSegmentBegin2 = _floatableMeshBoundingBoxGlobalVertices[i + 4];
				Vec3 lineSegmentEnd2 = _floatableMeshBoundingBoxGlobalVertices[(i + 1) % 4 + 4];
				Vec3 closestPointOnLineSegmentToPoint2 = MBMath.GetClosestPointOnLineSegmentToPoint(in lineSegmentBegin2, in lineSegmentEnd2, in point);
				float num4 = closestPointOnLineSegmentToPoint2.DistanceSquared(point);
				if (num4 < num)
				{
					num = num4;
					vec2 = closestPointOnLineSegmentToPoint2;
				}
				Vec3 closestPointOnLineSegmentToPoint3 = MBMath.GetClosestPointOnLineSegmentToPoint(in lineSegmentBegin, in lineSegmentBegin2, in point);
				float num5 = closestPointOnLineSegmentToPoint3.DistanceSquared(point);
				if (num5 < num)
				{
					num = num5;
					vec2 = closestPointOnLineSegmentToPoint3;
				}
			}
			if (num < 0.36f)
			{
				Vec3 vec5 = vec2 - point;
				float num6 = vec5.Normalize();
				float b = 0.6f - num6;
				float num7 = MathF.Min(_floatableEntityNavalPhysicsScript.Mass, item.GetTotalMass());
				Vec3 vec6 = vec5 * num7 * 2f / MathF.Max(0.25f, b);
				Vec3 vec7 = Vec3.CrossProduct(vec2 - globalMassFrame.origin, vec6);
				_forceToBeAppliedOnFixedTick += vec6;
				_torqueToBeAppliedOnFixedTick += vec7;
			}
		}
	}
}
