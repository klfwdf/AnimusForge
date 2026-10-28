using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeVillageStateAction
{
	private static void ApplyInternal(Village village, Village.VillageStates newState, MobileParty raiderParty)
	{
		Village.VillageStates villageState = village.VillageState;
		if (newState != villageState)
		{
			village.VillageState = newState;
			CampaignEventDispatcher.Instance.OnVillageStateChanged(village, villageState, village.VillageState, raiderParty);
			village.Settlement.Party.SetLevelMaskIsDirty();
		}
	}

	public static void ApplyBySettingToNormal(Settlement settlement)
	{
		ApplyInternal(settlement.Village, Village.VillageStates.Normal, null);
	}

	public static void ApplyBySettingToBeingRaided(Settlement settlement, MobileParty raider)
	{
		ApplyInternal(settlement.Village, Village.VillageStates.BeingRaided, raider);
	}

	public static void ApplyBySettingToBeingForcedForSupplies(Settlement settlement, MobileParty raider)
	{
		ApplyInternal(settlement.Village, Village.VillageStates.ForcedForSupplies, raider);
	}

	public static void ApplyBySettingToBeingForcedForVolunteers(Settlement settlement, MobileParty raider)
	{
		ApplyInternal(settlement.Village, Village.VillageStates.ForcedForVolunteers, raider);
	}

	public static void ApplyBySettingToLooted(Settlement settlement, MobileParty raider)
	{
		ApplyInternal(settlement.Village, Village.VillageStates.Looted, raider);
	}
}
