using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class StandingPointForRangedArea : StandingPoint
{
	public float ThrowingValueMultiplier = 5f;

	public float RangedWeaponValueMultiplier = 2f;

	public override Agent.AIScriptedFrameFlags DisableScriptedFrameFlags => Agent.AIScriptedFrameFlags.NoAttack | Agent.AIScriptedFrameFlags.ConsiderRotation;

	protected internal override void OnInit()
	{
		base.OnInit();
		AutoSheathWeapons = false;
		LockUserFrames = false;
		LockUserPositions = true;
		SetScriptComponentToTick(GetTickRequirement());
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		if (primaryWieldedItemIndex != EquipmentIndex.None)
		{
			WeaponComponentData currentUsageItem = agent.Equipment[primaryWieldedItemIndex].CurrentUsageItem;
			if (currentUsageItem != null && currentUsageItem.IsRangedWeapon)
			{
				if (primaryWieldedItemIndex == EquipmentIndex.ExtraWeaponSlot)
				{
					if (!(ThrowingValueMultiplier <= 0f))
					{
						return base.IsDisabledForAgent(agent);
					}
					return true;
				}
				if (!(RangedWeaponValueMultiplier <= 0f))
				{
					return base.IsDisabledForAgent(agent);
				}
				return true;
			}
			return true;
		}
		return true;
	}

	public override float GetUsageScoreForAgent(Agent agent)
	{
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		float num = 0f;
		if (primaryWieldedItemIndex != EquipmentIndex.None && agent.Equipment[primaryWieldedItemIndex].CurrentUsageItem.IsRangedWeapon)
		{
			num = ((primaryWieldedItemIndex == EquipmentIndex.ExtraWeaponSlot) ? ThrowingValueMultiplier : RangedWeaponValueMultiplier);
		}
		return base.GetUsageScoreForAgent(agent) + num;
	}

	public override bool HasAlternative()
	{
		return true;
	}

	public override TickRequirement GetTickRequirement()
	{
		if (base.HasUser)
		{
			return base.GetTickRequirement() | TickRequirement.TickParallel2;
		}
		return base.GetTickRequirement();
	}

	protected internal override void OnTickParallel2(float dt)
	{
		base.OnTickParallel2(dt);
		if (base.HasUser && IsDisabledForAgent(base.UserAgent))
		{
			base.UserAgent.StopUsingGameObjectMT(isSuccessful: false);
		}
	}
}
