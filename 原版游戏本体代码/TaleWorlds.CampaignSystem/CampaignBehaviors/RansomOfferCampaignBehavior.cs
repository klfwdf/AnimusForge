using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000437 RID: 1079
	public class RansomOfferCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E26 RID: 3622
		// (get) Token: 0x06004481 RID: 17537 RVA: 0x0014EBA0 File Offset: 0x0014CDA0
		private static TextObject RansomPanelTitleText
		{
			get
			{
				return new TextObject("{=ho5EndaV}Decision", null);
			}
		}

		// Token: 0x17000E27 RID: 3623
		// (get) Token: 0x06004482 RID: 17538 RVA: 0x0014EBAD File Offset: 0x0014CDAD
		private static TextObject RansomPanelAffirmativeText
		{
			get
			{
				return new TextObject("{=Y94H6XnK}Accept", null);
			}
		}

		// Token: 0x17000E28 RID: 3624
		// (get) Token: 0x06004483 RID: 17539 RVA: 0x0014EBBA File Offset: 0x0014CDBA
		private static TextObject RansomPanelNegativeText
		{
			get
			{
				return new TextObject("{=cOgmdp9e}Decline", null);
			}
		}

		// Token: 0x06004484 RID: 17540 RVA: 0x0014EBC8 File Offset: 0x0014CDC8
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, new Action<Hero>(this.DailyTickHero));
			CampaignEvents.OnRansomOfferedToPlayerEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnRansomOffered));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
			CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, new Action<Hero, PartyBase, IFaction, EndCaptivityDetail, bool>(this.OnHeroPrisonerReleased));
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.HourlyTick));
			CampaignEvents.PrisonersChangeInSettlement.AddNonSerializedListener(this, new Action<Settlement, FlattenedTroopRoster, Hero, bool>(this.OnPrisonersChangeInSettlement));
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(this.OnHeroPrisonerTaken));
		}

		// Token: 0x06004485 RID: 17541 RVA: 0x0014EC76 File Offset: 0x0014CE76
		private void OnHeroPrisonerTaken(PartyBase party, Hero hero)
		{
			this.HandleDeclineRansomOffer(hero);
		}

		// Token: 0x06004486 RID: 17542 RVA: 0x0014EC80 File Offset: 0x0014CE80
		private void DailyTickHero(Hero hero)
		{
			if (hero.IsPrisoner && hero.Clan != null && hero.PartyBelongedToAsPrisoner != null && hero.PartyBelongedToAsPrisoner.MapFaction != null && !hero.PartyBelongedToAsPrisoner.MapFaction.IsBanditFaction && hero != Hero.MainHero && hero.Clan.AliveLords.Count > 1 && hero.MapFaction != null)
			{
				this.ConsiderRansomPrisoner(hero);
			}
		}

		// Token: 0x06004487 RID: 17543 RVA: 0x0014ECF0 File Offset: 0x0014CEF0
		private void ConsiderRansomPrisoner(Hero hero)
		{
			Clan captorClanOfPrisoner = this.GetCaptorClanOfPrisoner(hero);
			if (captorClanOfPrisoner != null)
			{
				Hero hero2 = ((hero.Clan.Leader != hero) ? hero.Clan.Leader : (from t in hero.Clan.AliveLords
					where t != hero.Clan.Leader
					select t).GetRandomElementInefficiently<Hero>());
				if (hero2 != Hero.MainHero || !hero2.IsPrisoner)
				{
					if (captorClanOfPrisoner == Clan.PlayerClan || hero.Clan == Clan.PlayerClan)
					{
						if (this._currentRansomHero == null && !MobileParty.MainParty.IsInRaftState)
						{
							float num = ((!this._heroesWithDeclinedRansomOffers.Contains(hero)) ? 0.2f : 0.12f);
							if (MBRandom.RandomFloat < num)
							{
								float num2 = (float)new SetPrisonerFreeBarterable(hero, captorClanOfPrisoner.Leader, hero.PartyBelongedToAsPrisoner, hero2).GetUnitValueForFaction(hero.Clan) * 1.1f;
								if (num2 > 1E-05f && (float)(hero2.Gold + 1000) >= num2)
								{
									this.SetCurrentRansomHero(hero, hero2);
									StringHelpers.SetCharacterProperties("CAPTIVE_HERO", hero.CharacterObject, RansomOfferCampaignBehavior.RansomOfferDescriptionText, false);
									Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new RansomOfferMapNotification(hero, RansomOfferCampaignBehavior.RansomOfferDescriptionText));
									return;
								}
							}
						}
					}
					else if (MBRandom.RandomFloat < 0.1f)
					{
						SetPrisonerFreeBarterable setPrisonerFreeBarterable = new SetPrisonerFreeBarterable(hero, captorClanOfPrisoner.Leader, hero.PartyBelongedToAsPrisoner, hero2);
						if (setPrisonerFreeBarterable.GetValueForFaction(captorClanOfPrisoner) + setPrisonerFreeBarterable.GetValueForFaction(hero.Clan) > 0)
						{
							Campaign.Current.BarterManager.ExecuteAiBarter(captorClanOfPrisoner, hero.Clan, captorClanOfPrisoner.Leader, hero2, setPrisonerFreeBarterable);
						}
					}
				}
			}
		}

		// Token: 0x06004488 RID: 17544 RVA: 0x0014EEF0 File Offset: 0x0014D0F0
		private Clan GetCaptorClanOfPrisoner(Hero hero)
		{
			Clan result;
			if (hero.PartyBelongedToAsPrisoner.IsMobile)
			{
				if ((hero.PartyBelongedToAsPrisoner.MobileParty.IsMilitia || hero.PartyBelongedToAsPrisoner.MobileParty.IsGarrison || hero.PartyBelongedToAsPrisoner.MobileParty.IsCaravan || hero.PartyBelongedToAsPrisoner.MobileParty.IsVillager) && hero.PartyBelongedToAsPrisoner.Owner != null)
				{
					if (hero.PartyBelongedToAsPrisoner.Owner.IsNotable)
					{
						result = hero.PartyBelongedToAsPrisoner.Owner.CurrentSettlement.OwnerClan;
					}
					else
					{
						result = hero.PartyBelongedToAsPrisoner.Owner.Clan;
					}
				}
				else if (hero.PartyBelongedToAsPrisoner.MobileParty.IsPatrolParty)
				{
					result = hero.PartyBelongedToAsPrisoner.MobileParty.HomeSettlement.OwnerClan;
				}
				else
				{
					result = hero.PartyBelongedToAsPrisoner.MobileParty.ActualClan;
				}
			}
			else
			{
				result = hero.PartyBelongedToAsPrisoner.Settlement.OwnerClan;
			}
			return result;
		}

		// Token: 0x06004489 RID: 17545 RVA: 0x0014EFF0 File Offset: 0x0014D1F0
		public void SetCurrentRansomHero(Hero hero, Hero ransomPayer = null)
		{
			this._currentRansomHero = hero;
			this._currentRansomPayer = ransomPayer;
			this._currentRansomOfferDate = ((hero != null) ? CampaignTime.Now : CampaignTime.Never);
		}

		// Token: 0x0600448A RID: 17546 RVA: 0x0014F018 File Offset: 0x0014D218
		private void OnRansomOffered(Hero captiveHero)
		{
			Clan captorClanOfPrisoner = this.GetCaptorClanOfPrisoner(captiveHero);
			Clan clan = ((captiveHero.Clan == Clan.PlayerClan) ? captorClanOfPrisoner : captiveHero.Clan);
			Hero ransomPayer = ((captiveHero.Clan.Leader != captiveHero) ? captiveHero.Clan.Leader : (from t in captiveHero.Clan.AliveLords
				where t != captiveHero.Clan.Leader
				select t).GetRandomElementInefficiently<Hero>());
			int ransomPrice = (int)((float)new SetPrisonerFreeBarterable(captiveHero, captorClanOfPrisoner.Leader, captiveHero.PartyBelongedToAsPrisoner, ransomPayer).GetUnitValueForFaction(captiveHero.Clan) * 1.1f);
			TextObject textObject = ((captorClanOfPrisoner == Clan.PlayerClan) ? RansomOfferCampaignBehavior.RansomPanelDescriptionPlayerHeldPrisonerText : RansomOfferCampaignBehavior.RansomPanelDescriptionNpcHeldPrisonerText);
			textObject.SetTextVariable("CLAN_NAME", clan.Name);
			textObject.SetTextVariable("GOLD_AMOUNT", ransomPrice);
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			StringHelpers.SetCharacterProperties("CAPTIVE_HERO", captiveHero.CharacterObject, textObject, false);
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			InformationManager.ShowInquiry(new InquiryData(RansomOfferCampaignBehavior.RansomPanelTitleText.ToString(), textObject.ToString(), true, true, RansomOfferCampaignBehavior.RansomPanelAffirmativeText.ToString(), RansomOfferCampaignBehavior.RansomPanelNegativeText.ToString(), delegate()
			{
				this.AcceptRansomOffer(ransomPrice);
			}, new Action(this.DeclineRansomOffer), "", 0f, null, () => this.IsAffirmativeOptionEnabled(ransomPayer, ransomPrice), null), true, false);
		}

		// Token: 0x0600448B RID: 17547 RVA: 0x0014F1CC File Offset: 0x0014D3CC
		private ValueTuple<bool, string> IsAffirmativeOptionEnabled(Hero ransomPayer, int ransomPrice)
		{
			if (ransomPayer == Hero.MainHero && ransomPayer.Gold < ransomPrice)
			{
				return new ValueTuple<bool, string>(false, "{=d0kbtGYn}You don't have enough gold.");
			}
			return new ValueTuple<bool, string>(true, string.Empty);
		}

		// Token: 0x0600448C RID: 17548 RVA: 0x0014F1F8 File Offset: 0x0014D3F8
		private void AcceptRansomOffer(int ransomPrice)
		{
			if (this._heroesWithDeclinedRansomOffers.Contains(this._currentRansomHero))
			{
				this._heroesWithDeclinedRansomOffers.Remove(this._currentRansomHero);
			}
			if (this._currentRansomPayer.Gold < ransomPrice + 1000 && this._currentRansomPayer != Hero.MainHero)
			{
				this._currentRansomPayer.Gold = ransomPrice + 1000;
			}
			GiveGoldAction.ApplyBetweenCharacters(this._currentRansomPayer, this.GetCaptorClanOfPrisoner(this._currentRansomHero).Leader, ransomPrice, false);
			EndCaptivityAction.ApplyByRansom(this._currentRansomHero, this._currentRansomHero.Clan.Leader);
			IStatisticsCampaignBehavior behavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<IStatisticsCampaignBehavior>();
			if (behavior != null)
			{
				behavior.OnPlayerAcceptedRansomOffer(ransomPrice);
			}
		}

		// Token: 0x0600448D RID: 17549 RVA: 0x0014F2B0 File Offset: 0x0014D4B0
		private void DeclineRansomOffer()
		{
			if (this._currentRansomHero.IsPrisoner && this._currentRansomHero.IsAlive && !this._heroesWithDeclinedRansomOffers.Contains(this._currentRansomHero))
			{
				this._heroesWithDeclinedRansomOffers.Add(this._currentRansomHero);
			}
			this.SetCurrentRansomHero(null, null);
		}

		// Token: 0x0600448E RID: 17550 RVA: 0x0014F303 File Offset: 0x0014D503
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			this.HandleDeclineRansomOffer(victim);
		}

		// Token: 0x0600448F RID: 17551 RVA: 0x0014F30C File Offset: 0x0014D50C
		private void HandleDeclineRansomOffer(Hero victim)
		{
			if (this._currentRansomHero != null && (victim == this._currentRansomHero || victim == Hero.MainHero))
			{
				CampaignEventDispatcher.Instance.OnRansomOfferCancelled(this._currentRansomHero);
				this.DeclineRansomOffer();
			}
		}

		// Token: 0x06004490 RID: 17552 RVA: 0x0014F340 File Offset: 0x0014D540
		private void OnPrisonersChangeInSettlement(Settlement settlement, FlattenedTroopRoster roster, Hero prisoner, bool takenFromDungeon)
		{
			if (!takenFromDungeon && this._currentRansomHero != null)
			{
				if (prisoner == this._currentRansomHero)
				{
					CampaignEventDispatcher.Instance.OnRansomOfferCancelled(this._currentRansomHero);
					this.DeclineRansomOffer();
					return;
				}
				if (roster != null)
				{
					foreach (FlattenedTroopRosterElement flattenedTroopRosterElement in roster)
					{
						if (flattenedTroopRosterElement.Troop.IsHero && flattenedTroopRosterElement.Troop.HeroObject == this._currentRansomHero)
						{
							CampaignEventDispatcher.Instance.OnRansomOfferCancelled(this._currentRansomHero);
							this.DeclineRansomOffer();
							break;
						}
					}
				}
			}
		}

		// Token: 0x06004491 RID: 17553 RVA: 0x0014F3F0 File Offset: 0x0014D5F0
		private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
		{
			this.HandleDeclineRansomOffer(prisoner);
		}

		// Token: 0x06004492 RID: 17554 RVA: 0x0014F3F9 File Offset: 0x0014D5F9
		private void HourlyTick()
		{
			if (this._currentRansomHero != null && this._currentRansomOfferDate.ElapsedDaysUntilNow >= 2f)
			{
				CampaignEventDispatcher.Instance.OnRansomOfferCancelled(this._currentRansomHero);
				this.DeclineRansomOffer();
			}
		}

		// Token: 0x06004493 RID: 17555 RVA: 0x0014F42C File Offset: 0x0014D62C
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<List<Hero>>("_heroesWithDeclinedRansomOffers", ref this._heroesWithDeclinedRansomOffers);
			dataStore.SyncData<Hero>("_currentRansomHero", ref this._currentRansomHero);
			dataStore.SyncData<Hero>("_currentRansomPayer", ref this._currentRansomPayer);
			dataStore.SyncData<CampaignTime>("_currentRansomOfferDate", ref this._currentRansomOfferDate);
		}

		// Token: 0x0400133F RID: 4927
		private const float RansomOfferInitialChance = 0.2f;

		// Token: 0x04001340 RID: 4928
		private const float RansomOfferChanceAfterRefusal = 0.12f;

		// Token: 0x04001341 RID: 4929
		private const float RansomOfferChanceForPrisonersKeptByAI = 0.1f;

		// Token: 0x04001342 RID: 4930
		private const float MapNotificationAutoDeclineDurationInDays = 2f;

		// Token: 0x04001343 RID: 4931
		private const int AmountOfGoldLeftAfterRansom = 1000;

		// Token: 0x04001344 RID: 4932
		private static TextObject RansomOfferDescriptionText = new TextObject("{=ZqJ92UN4}A courier with a ransom offer for the freedom of {CAPTIVE_HERO.NAME} has arrived.", null);

		// Token: 0x04001345 RID: 4933
		private static TextObject RansomPanelDescriptionNpcHeldPrisonerText = new TextObject("{=4fXpOe4N}A courier arrives from the {CLAN_NAME}. They hold {CAPTIVE_HERO.NAME} and are demanding {GOLD_AMOUNT}{GOLD_ICON} in ransom.", null);

		// Token: 0x04001346 RID: 4934
		private static TextObject RansomPanelDescriptionPlayerHeldPrisonerText = new TextObject("{=PutoRsWp}A courier arrives from the {CLAN_NAME}. They offer you {GOLD_AMOUNT}{GOLD_ICON} in ransom if you will free {CAPTIVE_HERO.NAME}.", null);

		// Token: 0x04001347 RID: 4935
		private List<Hero> _heroesWithDeclinedRansomOffers = new List<Hero>();

		// Token: 0x04001348 RID: 4936
		private Hero _currentRansomHero;

		// Token: 0x04001349 RID: 4937
		private Hero _currentRansomPayer;

		// Token: 0x0400134A RID: 4938
		private CampaignTime _currentRansomOfferDate;
	}
}
