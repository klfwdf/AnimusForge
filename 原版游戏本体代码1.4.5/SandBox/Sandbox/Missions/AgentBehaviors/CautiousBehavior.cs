using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.AgentBehaviors;

public class CautiousBehavior : AgentBehavior
{
	private readonly Timer _waitTimer;

	public CautiousBehavior(AgentBehaviorGroup behaviorGroup)
		: base(behaviorGroup)
	{
		_waitTimer = new Timer(base.Mission.CurrentTime, 10f);
	}

	public override void Tick(float dt, bool isSimulation)
	{
		bool flag = true;
		if (base.OwnerAgent.IsCautious())
		{
			if (base.OwnerAgent.IsAIAtMoveDestination())
			{
				base.OwnerAgent.SetActionChannel(1, in ActionIndexCache.act_guard_cautious_look_around_1, ignorePriority: false, (AnimFlags)0uL);
			}
			else
			{
				base.OwnerAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: false, AnimFlags.amf_priority_jump);
			}
			if (base.OwnerAgent.GetPrimaryWieldedItemIndex() != EquipmentIndex.None)
			{
				base.Mission.AddTickAction(Mission.MissionTickAction.TryToSheathWeaponInHand, base.OwnerAgent, 0, 1);
			}
			EquipmentIndex offhandWieldedItemIndex = base.OwnerAgent.GetOffhandWieldedItemIndex();
			if (offhandWieldedItemIndex != EquipmentIndex.None && offhandWieldedItemIndex != EquipmentIndex.ExtraWeaponSlot)
			{
				base.Mission.AddTickAction(Mission.MissionTickAction.TryToSheathWeaponInHand, base.OwnerAgent, 1, 1);
			}
		}
		else if (base.OwnerAgent.IsPatrollingCautious())
		{
			bool num = base.OwnerAgent.IsAIAtMoveDestination();
			base.OwnerAgent.SetWeaponGuard(Agent.UsageDirection.AttackRight);
			if (num && base.OwnerAgent.GetAIMoveDestination().AsVec2.DistanceSquared(base.OwnerAgent.GetAILastSuspiciousPosition().AsVec2) < 1f)
			{
				flag = false;
				if (_waitTimer.Check(base.Mission.CurrentTime))
				{
					_waitTimer.Reset(base.Mission.CurrentTime, MBRandom.RandomFloat * 4f + 8f);
					base.OwnerAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: false, AnimFlags.amf_priority_jump);
					WorldPosition worldPosition = base.OwnerAgent.GetWorldPosition();
					Vec2 asVec = worldPosition.AsVec2;
					Vec2 movementDirection = base.OwnerAgent.GetMovementDirection();
					movementDirection.RotateCCW(MBRandom.RandomFloat * (System.MathF.PI * 2f));
					worldPosition.SetVec2(worldPosition.AsVec2 + movementDirection * base.OwnerAgent.Monster.BodyCapsuleRadius * MBRandom.RandomFloatRanged(20f, 35f));
					worldPosition.SetVec2(base.OwnerAgent.FindLongestDirectMoveToPosition(worldPosition.AsVec2, checkBoundaries: true, checkFriendlyAgents: false, out var _));
					float num2 = worldPosition.AsVec2.DistanceSquared(asVec);
					if (num2 > base.OwnerAgent.Monster.BodyCapsuleRadius * base.OwnerAgent.Monster.BodyCapsuleRadius * 10f * 10f)
					{
						worldPosition.SetVec2(asVec + movementDirection * (TaleWorlds.Library.MathF.Sqrt(num2) - base.OwnerAgent.Monster.BodyCapsuleRadius * 10f));
						base.OwnerAgent.SetAILastSuspiciousPosition(worldPosition, checkNavMeshForCorrection: false);
					}
				}
				else
				{
					base.OwnerAgent.SetActionChannel(1, in ActionIndexCache.act_guard_patrolling_cautious_look_around_1, ignorePriority: false, (AnimFlags)0uL);
				}
			}
		}
		if (flag)
		{
			_waitTimer.Reset(base.Mission.CurrentTime);
		}
	}

	public override float GetAvailability(bool isSimulation)
	{
		if (!base.OwnerAgent.IsCautious() && !base.OwnerAgent.IsPatrollingCautious())
		{
			return 0f;
		}
		return 10f;
	}

	protected override void OnDeactivate()
	{
		if (!base.OwnerAgent.IsAlarmed())
		{
			base.OwnerAgent.SetActionChannel(1, in ActionIndexCache.act_none, ignorePriority: false, AnimFlags.amf_priority_jump);
			if (base.OwnerAgent.GetPrimaryWieldedItemIndex() != EquipmentIndex.None)
			{
				base.Mission.AddTickActionMT(Mission.MissionTickAction.TryToSheathWeaponInHand, base.OwnerAgent, 0, 0);
			}
			EquipmentIndex offhandWieldedItemIndex = base.OwnerAgent.GetOffhandWieldedItemIndex();
			if (offhandWieldedItemIndex != EquipmentIndex.None && offhandWieldedItemIndex != EquipmentIndex.ExtraWeaponSlot)
			{
				base.Mission.AddTickActionMT(Mission.MissionTickAction.TryToSheathWeaponInHand, base.OwnerAgent, 1, 0);
			}
		}
	}

	protected override void OnActivate()
	{
		_waitTimer.Reset(base.Mission.CurrentTime);
	}

	public override string GetDebugInfo()
	{
		return string.Empty;
	}
}
