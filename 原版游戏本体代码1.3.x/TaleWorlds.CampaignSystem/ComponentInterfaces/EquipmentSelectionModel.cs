using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001F6 RID: 502
	public abstract class EquipmentSelectionModel : MBGameModel<EquipmentSelectionModel>
	{
		// Token: 0x06001F1E RID: 7966
		public abstract MBList<MBEquipmentRoster> GetEquipmentRostersForHeroComeOfAge(Hero hero, bool isCivilian);

		// Token: 0x06001F1F RID: 7967
		public abstract MBList<MBEquipmentRoster> GetEquipmentRostersForHeroReachesTeenAge(Hero hero);

		// Token: 0x06001F20 RID: 7968
		public abstract MBList<MBEquipmentRoster> GetEquipmentRostersForInitialChildrenGeneration(Hero hero);

		// Token: 0x06001F21 RID: 7969
		public abstract MBList<MBEquipmentRoster> GetEquipmentRostersForDeliveredOffspring(Hero hero);

		// Token: 0x06001F22 RID: 7970
		public abstract MBList<MBEquipmentRoster> GetEquipmentRostersForCompanion(Hero companionHero, bool isCivilian);
	}
}
