using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.Actions
{
	// Token: 0x020004A2 RID: 1186
	public static class ChangeVillageStateAction
	{
		// Token: 0x06004983 RID: 18819 RVA: 0x00172204 File Offset: 0x00170404
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

		// Token: 0x06004984 RID: 18820 RVA: 0x00172246 File Offset: 0x00170446
		public static void ApplyBySettingToNormal(Settlement settlement)
		{
			ChangeVillageStateAction.ApplyInternal(settlement.Village, Village.VillageStates.Normal, null);
		}

		// Token: 0x06004985 RID: 18821 RVA: 0x00172255 File Offset: 0x00170455
		public static void ApplyBySettingToBeingRaided(Settlement settlement, MobileParty raider)
		{
			ChangeVillageStateAction.ApplyInternal(settlement.Village, Village.VillageStates.BeingRaided, raider);
		}

		// Token: 0x06004986 RID: 18822 RVA: 0x00172264 File Offset: 0x00170464
		public static void ApplyBySettingToBeingForcedForSupplies(Settlement settlement, MobileParty raider)
		{
			ChangeVillageStateAction.ApplyInternal(settlement.Village, Village.VillageStates.ForcedForSupplies, raider);
		}

		// Token: 0x06004987 RID: 18823 RVA: 0x00172273 File Offset: 0x00170473
		public static void ApplyBySettingToBeingForcedForVolunteers(Settlement settlement, MobileParty raider)
		{
			ChangeVillageStateAction.ApplyInternal(settlement.Village, Village.VillageStates.ForcedForVolunteers, raider);
		}

		// Token: 0x06004988 RID: 18824 RVA: 0x00172282 File Offset: 0x00170482
		public static void ApplyBySettingToLooted(Settlement settlement, MobileParty raider)
		{
			ChangeVillageStateAction.ApplyInternal(settlement.Village, Village.VillageStates.Looted, raider);
		}
	}
}
