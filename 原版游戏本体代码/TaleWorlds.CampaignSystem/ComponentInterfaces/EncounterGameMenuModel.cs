using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001B8 RID: 440
	public abstract class EncounterGameMenuModel : MBGameModel<EncounterGameMenuModel>
	{
		// Token: 0x06001D6A RID: 7530
		public abstract string GetEncounterMenu(PartyBase attackerParty, PartyBase defenderParty, out bool startBattle, out bool joinBattle);

		// Token: 0x06001D6B RID: 7531
		public abstract string GetRaidCompleteMenu();

		// Token: 0x06001D6C RID: 7532
		public abstract string GetNewPartyJoinMenu(MobileParty newParty);

		// Token: 0x06001D6D RID: 7533
		public abstract string GetGenericStateMenu();

		// Token: 0x06001D6E RID: 7534
		public abstract bool IsPlunderMenu(string menuId);
	}
}
