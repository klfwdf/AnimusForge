using System;
using System.Collections.Generic;
using NavalDLC.Missions.MissionLogics;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects;

public class MissionShipRam : MissionObject
{
	private struct RamCollisionData
	{
		public MissionShip TargetShip;

		public CapsuleData CapsuleData;

		public bool RamWillBeHandled;

		public Vec3 SelectedIntersectionPoint;

		public Vec3 AverageIntersectionPoint;

		public Vec3 RamDirection;

		public float PenetrationLength;

		public bool HasPoint;

		public float CalculatedDamage;

		public Vec3 PointVelocityOnOwner;

		public Vec3 PointVelocityOnTarget;

		public bool IsValid => TargetShip != null;
	}

	private const float SpeedFactorOnMagnitude = 0.03f;

	private const string ShipDebrisAndParticlePrefabName = "decal_ship_damaged_b_heap";

	private const string ShipBodyPhysicsEntityTag = "body_mesh";

	private const float RamHitDirectionThresholdPercentage = 0.3f;

	private const float RamStickThresholdPercentage = 0.33f;

	private const string PhysicsMaterialName = "wood_ship";

	private static readonly int RamCollisionSoundEffectSoundId = SoundManager.GetEventGlobalIndex("event:/physics/vessel/ship_ramming");

	private const BodyFlags RamRaycastExcludeFlags = BodyFlags.Disabled | BodyFlags.AILimiter | BodyFlags.Barrier | BodyFlags.Barrier3D | BodyFlags.Ragdoll | BodyFlags.RagdollLimiter | BodyFlags.FloatingDebris;

	private static (float, float, float, float, bool)[] _ramQualityThresholds = new(float, float, float, float, bool)[5]
	{
		(10f, 70f, 0.2f, 5f, true),
		(8f, 60f, 0.3f, 4f, true),
		(6f, 45f, 0.45f, 2.5f, false),
		(5f, 30f, 0.65f, 1.5f, false),
		(3f, 0f, 0.9f, 0.5f, false)
	};

	private Intersection[] _intersectionsCache = new Intersection[128];

	private WeakGameEntity[] _entitiesCache = new WeakGameEntity[128];

	private UIntPtr[] _entityPointersCache = new UIntPtr[128];

	private Intersection[] _selectedIntersectionsCache = new Intersection[128];

	private MissionShip _ownerShip;

	private MissionShip _ramStuckTargetShip;

	private bool _ramCollisionBeingHandled;

	private RamCollisionData _ramDamageData;

	private RamCollisionData _ramCollisionData;

	private Scene _ownScene;

	private int _lastRamHitQuality;

	[EditableScriptComponentVariable(true, "")]
	private float _ramLength = 5f;

	[EditableScriptComponentVariable(true, "")]
	private float _ramRadius = 0.5f;

	[EditableScriptComponentVariable(true, "")]
	private Vec3 _ramAttachmentPointOffset = Vec3.Zero;

	[EditableScriptComponentVariable(true, "")]
	private float _ramTierDamageMultiplier = 1f;

	private float _scaledRamRadius = -1f;

	private float _scaledRamLength = -1f;

	private static float ForwardSpeedThresholdToDamage => _ramQualityThresholds[_ramQualityThresholds.Length - 1].Item1;

	private static float DistanceToShipCenterThresholdToDamage => _ramQualityThresholds[_ramQualityThresholds.Length - 1].Item3;

	public float RamLength => _ramLength;

	private CapsuleData GetRamCapsuleData(float fixedDt, bool getDataForNextFrame)
	{
		MatrixFrame matrixFrame = base.GameEntity.ComputePreciseGlobalFrameForFixedTickSlow();
		Vec3 f = matrixFrame.rotation.f;
		Vec3 vec = matrixFrame.TransformToParent(in _ramAttachmentPointOffset);
		Vec3 vec2 = vec + f * _ramLength;
		float radius = _ramRadius * Math.Max(matrixFrame.rotation.u.Length, matrixFrame.rotation.s.Length);
		if (getDataForNextFrame)
		{
			vec += _ownerShip.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(vec) * fixedDt;
			vec2 += _ownerShip.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(vec2) * fixedDt;
		}
		return new CapsuleData(radius, vec, vec2);
	}

	protected override void OnTick(float dt)
	{
		if (!_ramDamageData.IsValid)
		{
			return;
		}
		float calculatedDamage = _ramDamageData.CalculatedDamage;
		if (calculatedDamage > 0f)
		{
			_ramDamageData.TargetShip.DealCollisionDamage(_ownerShip, isRamDamage: true, _ramDamageData.SelectedIntersectionPoint, calculatedDamage);
			_ownerShip.UpdateDamageCooldown(_ramDamageData.TargetShip);
			Vec3 averageIntersectionPoint = _ramDamageData.AverageIntersectionPoint;
			foreach (DestructableComponent allDestructableComponent in _ramDamageData.TargetShip.AllDestructableComponents)
			{
				if (!allDestructableComponent.IsDestroyed && allDestructableComponent.GameEntity.GlobalPosition.DistanceSquared(averageIntersectionPoint) < 25f)
				{
					allDestructableComponent.DestroyOnAnyHit = true;
					allDestructableComponent.TriggerOnHit(null, 1, averageIntersectionPoint, _ramDamageData.RamDirection, in MissionWeapon.Invalid, -1, this);
				}
			}
			Agent agent = _ownerShip.Captain;
			if (agent == null || !agent.IsMainAgent)
			{
				Agent agent2 = null;
				if (_ownerShip.ShipControllerMachine.PilotAgent != null && (agent2 == null || _ownerShip.ShipControllerMachine.PilotAgent.IsMainAgent))
				{
					agent2 = _ownerShip.ShipControllerMachine.PilotAgent;
				}
				if (agent == null || (agent2 != null && agent2.IsMainAgent))
				{
					agent = agent2;
				}
			}
			for (int num = Mission.Current.Agents.Count - 1; num >= 0; num--)
			{
				Agent agent3 = Mission.Current.Agents[num];
				Vec3 position = agent3.Position;
				if (agent3.IsActive() && position.AsVec2.DistanceSquared(averageIntersectionPoint.AsVec2) < 4f)
				{
					Blow b = new Blow(agent?.Index ?? agent3.Index);
					b.DamageType = DamageTypes.Blunt;
					b.BaseMagnitude = 200f;
					b.InflictedDamage = 200;
					b.GlobalPosition = position;
					b.DamagedPercentage = 1f;
					agent3.Die(b);
				}
			}
		}
		TriggerRamCollisionParticleAndSoundEffect(_ramDamageData.TargetShip.Index, _ramDamageData.TargetShip.GameEntity, _ramDamageData.CapsuleData, calculatedDamage);
		_ramDamageData = default(RamCollisionData);
	}

	protected override void OnInit()
	{
		base.OnInit();
		_ownerShip = base.GameEntity.Root.GetFirstScriptOfTypeInFamily<MissionShip>();
		CapsuleData ramCapsuleData = GetRamCapsuleData(0f, getDataForNextFrame: false);
		_scaledRamRadius = ramCapsuleData.Radius;
		_scaledRamLength = (ramCapsuleData.P2 - ramCapsuleData.P1).Length + _scaledRamRadius;
		MatrixFrame globalFrame = _ownerShip.GameEntity.GetGlobalFrame();
		_ownerShip.GameEntity.PushCapsuleShapeToEntityBody(globalFrame.TransformToLocal(ramCapsuleData.P1), globalFrame.TransformToLocal(ramCapsuleData.P2), ramCapsuleData.Radius, "wood_ship");
		_ownScene = base.GameEntity.Scene;
	}

	protected override void OnFixedTick(float fixedDt)
	{
		RamCollisionHandleFixedTick(fixedDt);
	}

	protected override void OnParallelFixedTick(float fixedDt)
	{
		RamCollisionCheckTick(fixedDt);
	}

	public override TickRequirement GetTickRequirement()
	{
		return TickRequirement.Tick | TickRequirement.FixedTick | TickRequirement.FixedParallelTick;
	}

	private void TriggerRamCollisionParticleAndSoundEffect(int targetShipIndex, WeakGameEntity targetEntity, CapsuleData shipRamCapsule, float damage)
	{
		List<WeakGameEntity> list = targetEntity.CollectChildrenEntitiesWithTag("body_mesh");
		if (list.Count == 0)
		{
			return;
		}
		WeakGameEntity weakGameEntity = list[0];
		Vec3 vec = shipRamCapsule.P2 - shipRamCapsule.P1;
		vec.Normalize();
		Vec3 p = shipRamCapsule.P1;
		float num = _scaledRamLength * 3f;
		Vec3 position = p;
		float resultLength = num;
		Vec3 resultNormal = Vec3.Zero;
		if (weakGameEntity.RayHitEntityWithNormal(p, vec, num, ref resultNormal, ref resultLength))
		{
			MatrixFrame identity = MatrixFrame.Identity;
			identity.origin = p + vec * resultLength;
			identity.rotation.u = resultNormal;
			identity.rotation.f = Vec3.Up;
			identity.rotation.s = Vec3.CrossProduct(identity.rotation.f, identity.rotation.u);
			identity.rotation.s.Normalize();
			identity.rotation.f = Vec3.CrossProduct(identity.rotation.u, identity.rotation.s);
			GameEntity gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, "decal_ship_damaged_b_heap", identity);
			targetEntity.AddChild(gameEntity.WeakEntity, autoLocalizeFrame: true);
			position = identity.origin;
			Color color = Colors.White;
			ColorAssigner firstScriptOfType = base.GameEntity.Root.GetFirstScriptOfType<ColorAssigner>();
			if (firstScriptOfType != null)
			{
				color = firstScriptOfType.RamDebrisColor;
			}
			foreach (GameEntity child in gameEntity.GetChildren())
			{
				if (child.HasTag("plank"))
				{
					MatrixFrame frame = child.GetGlobalFrame();
					Vec3 vec2 = frame.origin + resultNormal * 2f;
					float resultLength2 = 0f;
					Vec3 vec3 = frame.origin;
					bool flag = weakGameEntity.RayHitEntity(vec2, -resultNormal, 2.5f, ref resultLength2);
					if (flag)
					{
						vec3 = vec2 - resultNormal * resultLength2;
						Vec3 boundingBoxMax = child.GetBoundingBoxMax();
						Vec3 boundingBoxMin = child.GetBoundingBoxMin();
						_ = vec3 + boundingBoxMax.z * frame.rotation.u + boundingBoxMax.x * frame.rotation.s;
						Vec3 rayOrigin = vec3 + resultNormal;
						flag = weakGameEntity.RayHitEntity(rayOrigin, -resultNormal, 1.5f, ref resultLength2);
						if (flag)
						{
							_ = vec3 + boundingBoxMin.z * frame.rotation.u + boundingBoxMax.x * frame.rotation.s;
							Vec3 rayOrigin2 = vec3 + resultNormal;
							flag = weakGameEntity.RayHitEntity(rayOrigin2, -resultNormal, 1.5f, ref resultLength2);
						}
						if (flag)
						{
							_ = vec3 + boundingBoxMin.z * frame.rotation.u + boundingBoxMin.x * frame.rotation.s;
							Vec3 rayOrigin3 = vec3 + resultNormal;
							flag = weakGameEntity.RayHitEntity(rayOrigin3, -resultNormal, 1.5f, ref resultLength2);
						}
						if (flag)
						{
							_ = vec3 + boundingBoxMax.z * frame.rotation.u + boundingBoxMin.x * frame.rotation.s;
							Vec3 rayOrigin4 = vec3 + resultNormal;
							flag = weakGameEntity.RayHitEntity(rayOrigin4, -resultNormal, 1.5f, ref resultLength2);
						}
					}
					if (flag)
					{
						frame.origin = vec3;
						child.SetGlobalFrame(in frame);
						child.SetFactorColor(color.ToUnsignedInteger());
					}
					else
					{
						child.SetVisibilityExcludeParents(visible: false);
					}
				}
				else if (child.HasTag("decal"))
				{
					child.SetFactorColor(color.ToUnsignedInteger());
				}
			}
		}
		else
		{
			MBDebug.Print("Could not hit body\n");
		}
		SoundEventParameter parameter = new SoundEventParameter("Force", TaleWorlds.Library.MathF.Min(damage * 0.01f, 1f));
		MBSoundEvent.PlaySound(RamCollisionSoundEffectSoundId, ref parameter, position);
	}

	private void RamCollisionHandleFixedTick(float fixedDt)
	{
		MatrixFrame bodyWorldTransform = _ownerShip.GameEntity.GetBodyWorldTransform();
		Vec3 vec = bodyWorldTransform.rotation.f.NormalizedCopy();
		MissionShip targetShip = _ramCollisionData.TargetShip;
		CapsuleData capsuleData = _ramCollisionData.CapsuleData;
		bool flag = _ramCollisionData.RamWillBeHandled;
		if (_ramCollisionData.HasPoint)
		{
			Vec3 v = _ramCollisionData.AverageIntersectionPoint;
			WeakGameEntity gameEntity = targetShip.GameEntity;
			MatrixFrame bodyWorldTransform2 = gameEntity.GetBodyWorldTransform();
			Vec3 v2 = bodyWorldTransform2.rotation.f.NormalizedCopy();
			if (_ramStuckTargetShip == null && _lastRamHitQuality > 0 && _ramQualityThresholds[_ramQualityThresholds.Length - _lastRamHitQuality].Item5 && _ramCollisionData.PenetrationLength >= _scaledRamLength * 0.33f && targetShip.HitPoints > 0f)
			{
				_ramStuckTargetShip = targetShip;
			}
			if (_ramStuckTargetShip != null)
			{
				if (_ramStuckTargetShip.HitPoints <= 0f)
				{
					_ramStuckTargetShip = null;
				}
				flag = true;
			}
			Vec3 pointVelocityOnOwner = _ramCollisionData.PointVelocityOnOwner;
			Vec3 pointVelocityOnTarget = _ramCollisionData.PointVelocityOnTarget;
			Vec3 vec2 = pointVelocityOnOwner - pointVelocityOnTarget;
			float num = vec2.Normalize();
			bool flag2 = true;
			float num2 = num * 0.03f;
			if (_ramStuckTargetShip == null)
			{
				flag = true;
				float num3 = 1f / _ownerShip.GameEntity.Mass + 1f / targetShip.GameEntity.Mass;
				Vec3 globalForce = -vec2 * num2 / num3;
				Vec3 vec3 = vec2 * num2 / num3;
				float num4 = Vec3.DotProduct(vec3, -vec);
				if (num4 > 0f)
				{
					Vec3 vec4 = -vec * num4;
					vec3 -= vec4;
				}
				gameEntity.ApplyGlobalForceAtLocalPosToDynamicBody(bodyWorldTransform2.TransformToLocal(in v), vec3, GameEntityPhysicsExtensions.ForceMode.Impulse);
				_ownerShip.GameEntity.ApplyGlobalForceAtLocalPosToDynamicBody(bodyWorldTransform.TransformToLocal(in v), globalForce, GameEntityPhysicsExtensions.ForceMode.Impulse);
				float num5 = TaleWorlds.Library.MathF.Abs(Vec3.DotProduct(pointVelocityOnOwner - pointVelocityOnTarget, vec));
				BoundingBox localPhysicsBoundingBox = gameEntity.GetLocalPhysicsBoundingBox(includeChildren: false);
				Vec3 v3 = localPhysicsBoundingBox.center;
				v3 = bodyWorldTransform2.TransformToParent(in v3);
				v3.z = _ramCollisionData.SelectedIntersectionPoint.z;
				float num6 = TaleWorlds.Library.MathF.Abs(Vec3.DotProduct(_ramCollisionData.SelectedIntersectionPoint - v3, v2));
				float num7 = localPhysicsBoundingBox.max.y - localPhysicsBoundingBox.min.y;
				int num8 = 1;
				float num9 = TaleWorlds.Library.MathF.Acos(TaleWorlds.Library.MathF.Abs(Vec3.DotProduct(vec, v2))) * (180f / System.MathF.PI);
				float item = _ramQualityThresholds[_ramQualityThresholds.Length - 1].Item4;
				for (int i = 0; i < _ramQualityThresholds.Length; i++)
				{
					(float, float, float, float, bool) tuple = _ramQualityThresholds[i];
					if (num5 >= tuple.Item1 && num9 >= tuple.Item2 && num6 * 2f <= num7 * tuple.Item3)
					{
						if (tuple.Item5)
						{
							flag2 = true;
						}
						item = tuple.Item4;
						num8 = _ramQualityThresholds.Length - i;
						break;
					}
				}
				float num10 = 12f * (float)Math.Sqrt(_ownerShip.Physics.Mass / 500f) * _ramTierDamageMultiplier * item * num5;
				bool flag3 = !_ramCollisionBeingHandled && flag;
				if (flag3 && _ownerShip.CanDealDamage(targetShip))
				{
					_lastRamHitQuality = num8;
					if (!_ramDamageData.IsValid)
					{
						_ramDamageData = new RamCollisionData
						{
							TargetShip = _ramCollisionData.TargetShip,
							CapsuleData = _ramCollisionData.CapsuleData,
							RamWillBeHandled = _ramCollisionData.RamWillBeHandled,
							SelectedIntersectionPoint = _ramCollisionData.SelectedIntersectionPoint,
							AverageIntersectionPoint = _ramCollisionData.AverageIntersectionPoint,
							RamDirection = _ramCollisionData.RamDirection,
							PenetrationLength = _ramCollisionData.PenetrationLength,
							HasPoint = _ramCollisionData.HasPoint,
							CalculatedDamage = num10
						};
					}
				}
				_ownerShip.ShipsLogic.OnShipRamming(_ownerShip, targetShip, num10 / targetShip.HitPoints, flag3, capsuleData, num8);
			}
			if (flag && _ramStuckTargetShip != null)
			{
				if (1f - Math.Abs(Vec3.DotProduct(vec, v2)) < 0.3f)
				{
					_ramStuckTargetShip = null;
				}
				else if (flag2)
				{
					Vec3 vec5 = pointVelocityOnOwner - pointVelocityOnTarget;
					float num11 = 1f / _ownerShip.GameEntity.Mass + 1f / targetShip.GameEntity.Mass;
					Vec3 globalForce2 = -0.1f * vec5 / num11;
					Vec3 vec6 = 0.1f * vec5 / num11;
					float num12 = Vec3.DotProduct(vec6, -vec);
					if (num12 > 0f)
					{
						Vec3 vec7 = -vec.NormalizedCopy() * num12;
						vec6 -= vec7;
					}
					_ownerShip.GameEntity.ApplyGlobalForceAtLocalPosToDynamicBody(bodyWorldTransform.TransformToLocal(in v), globalForce2, GameEntityPhysicsExtensions.ForceMode.Impulse);
					targetShip.GameEntity.ApplyGlobalForceAtLocalPosToDynamicBody(bodyWorldTransform2.TransformToLocal(in v), vec6, GameEntityPhysicsExtensions.ForceMode.Impulse);
				}
			}
		}
		else if (_ramStuckTargetShip != null)
		{
			_ramStuckTargetShip = null;
		}
		if (_ramCollisionBeingHandled != flag)
		{
			if (flag)
			{
				_ownerShip.GameEntity.PopCapsuleShapeFromEntityBody();
			}
			else
			{
				_lastRamHitQuality = 0;
				_ownerShip.GameEntity.PushCapsuleShapeToEntityBody(bodyWorldTransform.TransformToLocal(capsuleData.P1), bodyWorldTransform.TransformToLocal(capsuleData.P2), capsuleData.Radius, "wood_ship");
				Mission.Current.GetMissionBehavior<ShipCollisionOutcomeLogic>()?.ActivateCooldownForShip(_ownerShip, 0.2f);
			}
			_ramCollisionBeingHandled = flag;
		}
	}

	private void RamCollisionCheckTick(float fixedDt)
	{
		bool flag = false;
		int num = -1;
		MissionShip missionShip = null;
		WeakGameEntity root = base.GameEntity.Root;
		Vec3 vec = _ownerShip.GameEntity.GetBodyWorldTransform().rotation.f.NormalizedCopy();
		CapsuleData capsule = GetRamCapsuleData(fixedDt, !_ramCollisionBeingHandled);
		Vec3 vec2 = capsule.P2 - capsule.P1;
		Vec3 intersectionPoint = Vec3.Invalid;
		WeakGameEntity collidedEntity = WeakGameEntity.Invalid;
		float collisionDistance = -1f;
		BodyFlags bodyFlag = _ownerShip.GameEntity.BodyFlag;
		if (_ownScene.RayCastForRamming(capsule.P1, capsule.P1 + vec2 * 2f, _ownerShip.GameEntity, _scaledRamRadius, out collisionDistance, out intersectionPoint, out collidedEntity, BodyFlags.Disabled | BodyFlags.AILimiter | BodyFlags.Barrier | BodyFlags.Barrier3D | BodyFlags.Ragdoll | BodyFlags.RagdollLimiter | BodyFlags.FloatingDebris, bodyFlag))
		{
			float num2 = -1f;
			missionShip = collidedEntity.GetFirstScriptWithNameHash(MissionShip.MissionShipScriptNameHash) as MissionShip;
			if (missionShip != null)
			{
				float num3 = 0f;
				int num4 = 0;
				int num5 = 0;
				num5 = _ownScene.GenerateContactsWithCapsule(ref capsule, BodyFlags.OnlyCollideWithRaycast | BodyFlags.AILimiter | BodyFlags.Barrier, isFixedTick: true, _intersectionsCache, _entitiesCache, _entityPointersCache);
				for (int i = 0; i < num5; i++)
				{
					WeakGameEntity weakGameEntity = _entitiesCache[i];
					if (weakGameEntity == null)
					{
						continue;
					}
					WeakGameEntity root2 = weakGameEntity.Root;
					if (root == root2)
					{
						continue;
					}
					MissionShip firstScriptOfType = root2.GetFirstScriptOfType<MissionShip>();
					if (firstScriptOfType != null && firstScriptOfType != _ownerShip && firstScriptOfType == missionShip)
					{
						if (_ramCollisionBeingHandled && weakGameEntity.BodyFlag.HasAnyFlag(BodyFlags.DynamicConvexHull))
						{
							flag = true;
							continue;
						}
						_selectedIntersectionsCache[num4] = _intersectionsCache[i];
						num4++;
						num3 += Vec3.DotProduct(_intersectionsCache[i].IntersectionPoint - capsule.P1, vec);
					}
				}
				if (num4 > 0)
				{
					num2 = num3 / (float)num4;
				}
				float num6 = float.MaxValue;
				for (int j = 0; j < num4; j++)
				{
					if (missionShip != null && missionShip != _ownerShip)
					{
						float num7 = Math.Abs(_selectedIntersectionsCache[j].IntersectionPoint.DistanceSquared(capsule.P1) - num2 * num2);
						if (num7 < num6)
						{
							num6 = num7;
							num = j;
						}
					}
				}
			}
			int num8 = -1;
			Vec3 vec3 = Vec3.Invalid;
			Vec3 selectedIntersectionPoint = Vec3.Invalid;
			Vec3 vec4 = Vec3.Invalid;
			Vec3 vec5 = Vec3.Invalid;
			if (num >= 0)
			{
				vec3 = capsule.P1 + vec * num2;
				vec4 = _ownerShip.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(vec3);
				vec5 = missionShip.GameEntity.GetLinearVelocityAtGlobalPointForEntityWithDynamicBody(vec3);
				if ((Vec3.DotProduct(vec4 - vec5, vec) * vec).Length > ForwardSpeedThresholdToDamage)
				{
					Vec3 intersectionPoint2 = _selectedIntersectionsCache[num].IntersectionPoint;
					WeakGameEntity gameEntity = missionShip.GameEntity;
					MatrixFrame bodyWorldTransform = gameEntity.GetBodyWorldTransform();
					Vec3 v = bodyWorldTransform.rotation.f.NormalizedCopy();
					BoundingBox localPhysicsBoundingBox = gameEntity.GetLocalPhysicsBoundingBox(includeChildren: false);
					Vec3 v2 = localPhysicsBoundingBox.center;
					v2 = bodyWorldTransform.TransformToParent(in v2);
					v2.z = intersectionPoint2.z;
					float num9 = localPhysicsBoundingBox.max.y - localPhysicsBoundingBox.min.y;
					if (TaleWorlds.Library.MathF.Abs(Vec3.DotProduct(intersectionPoint2 - v2, v)) * 2f <= num9 * DistanceToShipCenterThresholdToDamage)
					{
						selectedIntersectionPoint = intersectionPoint2;
						num8 = num;
					}
				}
			}
			if (missionShip != null && !flag && !_ramCollisionBeingHandled)
			{
				float speedInRamDirection = vec.AsVec2.DotProduct(_ownerShip.Physics.LinearVelocity.AsVec2);
				_ownerShip.ShipsLogic.OnShipAboutToBeRammed(_ownerShip, missionShip, collisionDistance, speedInRamDirection);
			}
			_ramCollisionData = new RamCollisionData
			{
				TargetShip = missionShip,
				CapsuleData = capsule,
				RamWillBeHandled = flag,
				SelectedIntersectionPoint = selectedIntersectionPoint,
				AverageIntersectionPoint = vec3,
				RamDirection = vec,
				PenetrationLength = TaleWorlds.Library.MathF.Max(0f, _scaledRamLength - collisionDistance),
				HasPoint = (num8 >= 0),
				PointVelocityOnOwner = vec4,
				PointVelocityOnTarget = vec5
			};
		}
		else
		{
			_ramCollisionData = new RamCollisionData
			{
				CapsuleData = capsule
			};
		}
	}

	protected override bool CanPhysicsCollideBetweenTwoEntities(WeakGameEntity myEntity, BodyFlags myEntityBodyFlags, WeakGameEntity otherEntity, BodyFlags otherEntityBodyFlags)
	{
		if (myEntity != base.GameEntity)
		{
			return true;
		}
		if (!otherEntity.IsValid)
		{
			return true;
		}
		if (otherEntity.Root.HasScriptOfType<MissionShip>())
		{
			return false;
		}
		return true;
	}
}
