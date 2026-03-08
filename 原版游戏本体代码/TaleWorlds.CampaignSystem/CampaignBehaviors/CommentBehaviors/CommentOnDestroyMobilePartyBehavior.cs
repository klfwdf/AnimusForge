using System;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors.CommentBehaviors
{
	// Token: 0x0200045D RID: 1117
	public class CommentOnDestroyMobilePartyBehavior : CampaignBehaviorBase
	{
		// Token: 0x0600476F RID: 18287 RVA: 0x00165E0E File Offset: 0x0016400E
		public override void RegisterEvents()
		{
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnMobilePartyDestroyed));
		}

		// Token: 0x06004770 RID: 18288 RVA: 0x00165E27 File Offset: 0x00164027
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06004771 RID: 18289 RVA: 0x00165E2C File Offset: 0x0016402C
		private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
		{
			Hero hero = ((destroyerParty != null) ? destroyerParty.LeaderHero : null);
			IFaction faction = ((destroyerParty != null) ? destroyerParty.MapFaction : null);
			if (hero == Hero.MainHero || mobileParty.LeaderHero == Hero.MainHero || (faction != null && mobileParty.MapFaction != null && faction.IsKingdomFaction && mobileParty.MapFaction.IsKingdomFaction))
			{
				LogEntry.AddLogEntry(new DestroyMobilePartyLogEntry(mobileParty, destroyerParty));
			}
		}
	}
}
