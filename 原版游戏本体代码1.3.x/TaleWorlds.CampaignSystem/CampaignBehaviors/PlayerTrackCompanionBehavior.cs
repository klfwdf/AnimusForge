using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000430 RID: 1072
	public class PlayerTrackCompanionBehavior : CampaignBehaviorBase
	{
		// Token: 0x06004439 RID: 17465 RVA: 0x0014CB00 File Offset: 0x0014AD00
		public override void RegisterEvents()
		{
			CampaignEvents.CharacterBecameFugitiveEvent.AddNonSerializedListener(this, new Action<Hero, bool>(this.HeroBecameFugitive));
			CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, new Action<Hero, RemoveCompanionAction.RemoveCompanionDetail>(this.CompanionRemoved));
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.SettlementEntered));
			CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, new Action<Hero>(this.CompanionAdded));
			CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, new Action<Hero, PartyBase, IFaction, EndCaptivityDetail, bool>(this.OnHeroPrisonerReleased));
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, new Action(this.OnGameLoadFinished));
			CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, new Action<MobileParty>(this.OnMobilePartyCreated));
			CampaignEvents.OnHeroTeleportationRequestedEvent.AddNonSerializedListener(this, new Action<Hero, Settlement, MobileParty, TeleportHeroAction.TeleportationDetail>(this.OnHeroTeleportationRequested));
		}

		// Token: 0x0600443A RID: 17466 RVA: 0x0014CBC5 File Offset: 0x0014ADC5
		private void OnHeroTeleportationRequested(Hero hero, Settlement settlement, MobileParty party, TeleportHeroAction.TeleportationDetail detail)
		{
			if (hero.IsPlayerCompanion && party == MobileParty.MainParty && detail == TeleportHeroAction.TeleportationDetail.DelayedTeleportToParty && this._scatteredCompanions.ContainsKey(hero))
			{
				this._scatteredCompanions.Remove(hero);
			}
		}

		// Token: 0x0600443B RID: 17467 RVA: 0x0014CBF8 File Offset: 0x0014ADF8
		private void OnGameLoadFinished()
		{
			if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.0", 0)))
			{
				foreach (Hero hero in this._scatteredCompanions.Keys.ToList<Hero>())
				{
					if (hero.PartyBelongedTo != null || hero.GovernorOf != null || Campaign.Current.IssueManager.IssueSolvingCompanionList.Contains(hero))
					{
						this._scatteredCompanions.Remove(hero);
					}
				}
			}
		}

		// Token: 0x0600443C RID: 17468 RVA: 0x0014CCA8 File Offset: 0x0014AEA8
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<Hero, CampaignTime>>("ScatteredCompanions", ref this._scatteredCompanions);
		}

		// Token: 0x0600443D RID: 17469 RVA: 0x0014CCBC File Offset: 0x0014AEBC
		private void AddHeroToScatteredCompanions(Hero hero)
		{
			if (hero.IsPlayerCompanion)
			{
				if (!this._scatteredCompanions.ContainsKey(hero))
				{
					this._scatteredCompanions.Add(hero, CampaignTime.Now);
					return;
				}
				this._scatteredCompanions[hero] = CampaignTime.Now;
			}
		}

		// Token: 0x0600443E RID: 17470 RVA: 0x0014CCF7 File Offset: 0x0014AEF7
		private void HeroBecameFugitive(Hero hero, bool showNotification)
		{
			this.AddHeroToScatteredCompanions(hero);
		}

		// Token: 0x0600443F RID: 17471 RVA: 0x0014CD00 File Offset: 0x0014AF00
		private void OnHeroPrisonerReleased(Hero releasedHero, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
		{
			this.AddHeroToScatteredCompanions(releasedHero);
		}

		// Token: 0x06004440 RID: 17472 RVA: 0x0014CD0C File Offset: 0x0014AF0C
		private void SettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (party == MobileParty.MainParty)
			{
				foreach (Hero hero2 in this._scatteredCompanions.Keys.ToMBList<Hero>())
				{
					if (hero2.CurrentSettlement == settlement)
					{
						TextObject textObject = new TextObject("{=ahpSGaow}You hear that your companion {COMPANION.LINK}, who was separated from you after a battle, is currently in this settlement.", null);
						StringHelpers.SetCharacterProperties("COMPANION", hero2.CharacterObject, textObject, false);
						InformationManager.ShowInquiry(new InquiryData(new TextObject("{=dx0hmeH6}Tracking", null).ToString(), textObject.ToString(), true, false, new TextObject("{=yS7PvrTD}OK", null).ToString(), "", null, null, "", 0f, null, null, null), false, false);
						this._scatteredCompanions.Remove(hero2);
					}
				}
			}
		}

		// Token: 0x06004441 RID: 17473 RVA: 0x0014CDF0 File Offset: 0x0014AFF0
		private void CompanionAdded(Hero companion)
		{
			if (this._scatteredCompanions.ContainsKey(companion))
			{
				this._scatteredCompanions.Remove(companion);
			}
		}

		// Token: 0x06004442 RID: 17474 RVA: 0x0014CE0D File Offset: 0x0014B00D
		private void CompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
		{
			if (this._scatteredCompanions.ContainsKey(companion))
			{
				this._scatteredCompanions.Remove(companion);
			}
		}

		// Token: 0x06004443 RID: 17475 RVA: 0x0014CE2A File Offset: 0x0014B02A
		private void OnMobilePartyCreated(MobileParty mobileParty)
		{
			if (mobileParty.LeaderHero != null && mobileParty.LeaderHero.IsPlayerCompanion && this._scatteredCompanions.ContainsKey(mobileParty.LeaderHero))
			{
				this._scatteredCompanions.Remove(mobileParty.LeaderHero);
			}
		}

		// Token: 0x0400133A RID: 4922
		private Dictionary<Hero, CampaignTime> _scatteredCompanions = new Dictionary<Hero, CampaignTime>();
	}
}
