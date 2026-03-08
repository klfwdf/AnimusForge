using System;
using TaleWorlds.CampaignSystem.Party;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003DB RID: 987
	public class CharacterDevelopmentCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003C37 RID: 15415 RVA: 0x0010052B File Offset: 0x000FE72B
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.DailyTickHero));
		}

		// Token: 0x06003C38 RID: 15416 RVA: 0x00100544 File Offset: 0x000FE744
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003C39 RID: 15417 RVA: 0x00100548 File Offset: 0x000FE748
		private void DailyTickHero(Hero hero)
		{
			if (!hero.IsChild && hero.IsAlive && (hero.Clan != Clan.PlayerClan || (hero != Hero.MainHero && CampaignOptions.AutoAllocateClanMemberPerks)))
			{
				MobileParty partyBelongedTo = hero.PartyBelongedTo;
				if (((partyBelongedTo != null) ? partyBelongedTo.MapEvent : null) == null)
				{
					hero.HeroDeveloper.DevelopCharacterStats();
				}
			}
		}
	}
}
