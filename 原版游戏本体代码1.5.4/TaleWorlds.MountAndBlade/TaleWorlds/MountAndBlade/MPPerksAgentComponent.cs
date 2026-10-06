using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class MPPerksAgentComponent : AgentComponent
{
	public MPPerksAgentComponent(Agent agent)
		: base(agent)
	{
		Agent.OnAgentHealthChanged += OnHealthChanged;
		if (Agent.HasMount)
		{
			Agent.MountAgent.OnAgentHealthChanged += OnMountHealthChanged;
		}
	}

	public override void OnMount(Agent mount)
	{
		mount.OnAgentHealthChanged += OnMountHealthChanged;
		mount.UpdateAgentProperties();
		MPPerkObject.GetPerkHandler(Agent)?.OnEvent(Agent, MPPerkCondition.PerkEventFlags.MountChange);
	}

	public override void OnDismount(Agent mount)
	{
		mount.OnAgentHealthChanged -= OnMountHealthChanged;
		mount.UpdateAgentProperties();
		MPPerkObject.GetPerkHandler(Agent)?.OnEvent(Agent, MPPerkCondition.PerkEventFlags.MountChange);
	}

	public override void OnItemPickup(SpawnedItemEntity item)
	{
		if (!item.WeaponCopy.IsEmpty && item.WeaponCopy.Item.ItemType == ItemObject.ItemTypeEnum.Banner)
		{
			MPPerkObject.GetPerkHandler(Agent)?.OnEvent(MPPerkCondition.PerkEventFlags.BannerPickUp);
		}
	}

	public override void OnWeaponDrop(MissionWeapon droppedWeapon)
	{
		if (!droppedWeapon.IsEmpty && droppedWeapon.Item.ItemType == ItemObject.ItemTypeEnum.Banner)
		{
			MPPerkObject.GetPerkHandler(Agent)?.OnEvent(MPPerkCondition.PerkEventFlags.BannerDrop);
		}
	}

	public override void OnAgentRemoved()
	{
		if (Agent.HasMount)
		{
			Agent.MountAgent.OnAgentHealthChanged -= OnMountHealthChanged;
			Agent.MountAgent.UpdateAgentProperties();
		}
	}

	private void OnHealthChanged(Agent agent, float oldHealth, float newHealth)
	{
		MPPerkObject.GetPerkHandler(Agent)?.OnEvent(agent, MPPerkCondition.PerkEventFlags.HealthChange);
	}

	private void OnMountHealthChanged(Agent agent, float oldHealth, float newHealth)
	{
		if (Agent.IsActive() && Agent.MountAgent == agent)
		{
			MPPerkObject.GetPerkHandler(Agent)?.OnEvent(Agent, MPPerkCondition.PerkEventFlags.MountHealthChange);
		}
		else
		{
			agent.OnAgentHealthChanged -= OnMountHealthChanged;
		}
	}
}
