using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001BD RID: 445
	public abstract class PartyDesertionModel : MBGameModel<PartyDesertionModel>
	{
		// Token: 0x06001D88 RID: 7560
		public abstract TroopRoster GetTroopsToDesert(MobileParty mobileParty);

		// Token: 0x06001D89 RID: 7561
		public abstract float GetDesertionChanceForTroop(MobileParty mobileParty, in TroopRosterElement troopRosterElement);

		// Token: 0x06001D8A RID: 7562
		public abstract int GetMoraleThresholdForTroopDesertion();
	}
}
