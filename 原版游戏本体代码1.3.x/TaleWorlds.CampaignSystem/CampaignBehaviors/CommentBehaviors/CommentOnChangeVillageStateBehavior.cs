using System;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x02000457 RID: 1111
	public class CommentOnChangeVillageStateBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004756 RID: 18262 RVA: 0x00165B03 File Offset: 0x00163D03
		public override void RegisterEvents()
		{
			CampaignEvents.VillageStateChanged.AddNonSerializedListener(this, new Action<Village, Village.VillageStates, Village.VillageStates, MobileParty>(this.OnVillageStateChanged));
		}

		// Token: 0x06004757 RID: 18263 RVA: 0x00165B1C File Offset: 0x00163D1C
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004758 RID: 18264 RVA: 0x00165B20 File Offset: 0x00163D20
		private void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
		{
			if (newState != Village.VillageStates.Normal && raiderParty != null && (raiderParty.LeaderHero == Hero.MainHero || village.Owner.Settlement.OwnerClan.Leader == Hero.MainHero || village.Settlement.MapFaction.IsKingdomFaction || raiderParty.MapFaction.IsKingdomFaction))
			{
				LogEntry.AddLogEntry(new VillageStateChangedLogEntry(village, oldState, newState, raiderParty));
			}
		}
	}
}
