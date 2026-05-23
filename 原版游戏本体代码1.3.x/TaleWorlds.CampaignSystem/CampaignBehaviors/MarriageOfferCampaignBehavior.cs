using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x02000417 RID: 1047
	public class MarriageOfferCampaignBehavior : CampaignBehaviorBase, IMarriageOfferCampaignBehavior, ICampaignBehavior
	{
		// Token: 0x17000E1F RID: 3615
		// (get) Token: 0x0600426D RID: 17005 RVA: 0x0013F77D File Offset: 0x0013D97D
		internal bool IsThereActiveMarriageOffer
		{
			get
			{
				return this._currentOfferedPlayerClanHero != null && this._currentOfferedOtherClanHero != null;
			}
		}

		// Token: 0x0600426E RID: 17006 RVA: 0x0013F794 File Offset: 0x0013D994
		public override void RegisterEvents()
		{
			CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.DailyTickClan));
			CampaignEvents.OnMarriageOfferedToPlayerEvent.AddNonSerializedListener(this, new Action<Hero, Hero>(this.OnMarriageOfferedToPlayer));
			CampaignEvents.OnMarriageOfferCanceledEvent.AddNonSerializedListener(this, new Action<Hero, Hero>(this.OnMarriageOfferCanceled));
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.HourlyTick));
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(this.OnHeroPrisonerTaken));
			CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, new Action<Hero, Hero, bool>(this.OnHeroesMarried));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
			CampaignEvents.ArmyCreated.AddNonSerializedListener(this, new Action<Army>(this.OnArmyCreated));
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
			CampaignEvents.CharacterBecameFugitiveEvent.AddNonSerializedListener(this, new Action<Hero, bool>(this.CharacterBecameFugitive));
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
			CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, new Action<Hero, Hero, int, bool, ChangeRelationAction.ChangeRelationDetail, Hero, Hero>(this.OnHeroRelationChanged));
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
		}

		// Token: 0x0600426F RID: 17007 RVA: 0x0013F8D0 File Offset: 0x0013DAD0
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Hero>("_currentOfferedPlayerClanHero", ref this._currentOfferedPlayerClanHero);
			dataStore.SyncData<Hero>("_currentOfferedOtherClanHero", ref this._currentOfferedOtherClanHero);
			dataStore.SyncData<CampaignTime>("_lastMarriageOfferTime", ref this._lastMarriageOfferTime);
			dataStore.SyncData<Dictionary<Hero, Hero>>("_acceptedMarriageOffersThatWaitingForAvailability", ref this._acceptedMarriageOffersThatWaitingForAvailability);
		}

		// Token: 0x06004270 RID: 17008 RVA: 0x0013F928 File Offset: 0x0013DB28
		public void CreateMarriageOffer(Hero currentOfferedPlayerClanHero, Hero currentOfferedOtherClanHero)
		{
			this._currentOfferedPlayerClanHero = currentOfferedPlayerClanHero;
			this._currentOfferedOtherClanHero = currentOfferedOtherClanHero;
			this._lastMarriageOfferTime = CampaignTime.Now;
			this.MarriageOfferPanelExplanationText.SetCharacterProperties("CLAN_MEMBER", this._currentOfferedPlayerClanHero.CharacterObject, false);
			this.MarriageOfferPanelExplanationText.SetTextVariable("OFFERING_CLAN_NAME", this._currentOfferedOtherClanHero.Clan.Name);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new MarriageOfferMapNotification(this._currentOfferedPlayerClanHero, this._currentOfferedOtherClanHero, this.MarriageOfferPanelExplanationText));
		}

		// Token: 0x06004271 RID: 17009 RVA: 0x0013F9B4 File Offset: 0x0013DBB4
		public MBBindingList<TextObject> GetMarriageAcceptedConsequences()
		{
			MBBindingList<TextObject> mbbindingList = new MBBindingList<TextObject>();
			TextObject textObject = GameTexts.FindText("str_marriage_consequence_hero_join_clan", null);
			if (Campaign.Current.Models.MarriageModel.GetClanAfterMarriage(this._currentOfferedPlayerClanHero, this._currentOfferedOtherClanHero) == this._currentOfferedPlayerClanHero.Clan)
			{
				textObject.SetCharacterProperties("HERO", this._currentOfferedOtherClanHero.CharacterObject, false);
				textObject.SetTextVariable("CLAN_NAME", this._currentOfferedPlayerClanHero.Clan.Name);
			}
			else
			{
				textObject.SetCharacterProperties("HERO", this._currentOfferedPlayerClanHero.CharacterObject, false);
				textObject.SetTextVariable("CLAN_NAME", this._currentOfferedOtherClanHero.Clan.Name);
			}
			mbbindingList.Add(textObject);
			TextObject textObject2 = GameTexts.FindText("str_marriage_consequence_clan_relation", null);
			textObject2.SetTextVariable("CLAN_NAME", this._currentOfferedOtherClanHero.Clan.Name);
			textObject2.SetTextVariable("AMOUNT", 10.ToString("+0;-#"));
			mbbindingList.Add(textObject2);
			return mbbindingList;
		}

		// Token: 0x06004272 RID: 17010 RVA: 0x0013FAB8 File Offset: 0x0013DCB8
		private void DailyTickClan(Clan consideringClan)
		{
			if (this.CanOfferMarriageForClan(consideringClan))
			{
				MobileParty.NavigationType navigationType = (consideringClan.HasNavalNavigationCapability ? MobileParty.NavigationType.All : MobileParty.NavigationType.Default);
				float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(Clan.PlayerClan.FactionMidSettlement, consideringClan.FactionMidSettlement, false, false, navigationType);
				if (MBRandom.RandomFloat >= distance / Campaign.Current.Models.MapDistanceModel.GetMaximumDistanceBetweenTwoConnectedSettlements(navigationType) - 0.5f)
				{
					foreach (Hero hero in Clan.PlayerClan.Heroes)
					{
						if (hero != Hero.MainHero && hero.CanMarry() && !this._acceptedMarriageOffersThatWaitingForAvailability.ContainsKey(hero) && this.ConsiderMarriageForPlayerClanMember(hero, consideringClan))
						{
							break;
						}
					}
				}
			}
		}

		// Token: 0x06004273 RID: 17011 RVA: 0x0013FB98 File Offset: 0x0013DD98
		private void MarryHeroesViaOffer(Hero playerClanHero, Hero otherClanHero, bool showSceneNotification)
		{
			if (playerClanHero != Hero.MainHero && showSceneNotification)
			{
				Hero groomHero = (playerClanHero.IsFemale ? otherClanHero : playerClanHero);
				Hero brideHero = (playerClanHero.IsFemale ? playerClanHero : otherClanHero);
				MBInformationManager.ShowSceneNotification(new MarriageSceneNotificationItem(groomHero, brideHero, CampaignTime.Now, SceneNotificationData.RelevantContextType.Any));
			}
			ChangeRelationAction.ApplyPlayerRelation(otherClanHero.Clan.Leader, 10, true, true);
			MarriageAction.Apply(playerClanHero, otherClanHero, true);
		}

		// Token: 0x06004274 RID: 17012 RVA: 0x0013FBFC File Offset: 0x0013DDFC
		public void OnMarriageOfferAcceptedOnPopUp()
		{
			if (Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(this._currentOfferedPlayerClanHero, this._currentOfferedOtherClanHero))
			{
				this.MarryHeroesViaOffer(this._currentOfferedPlayerClanHero, this._currentOfferedOtherClanHero, true);
			}
			else
			{
				this._acceptedMarriageOffersThatWaitingForAvailability.Add(this._currentOfferedPlayerClanHero, this._currentOfferedOtherClanHero);
				InformationManager.ShowInquiry(new InquiryData(string.Empty, this.MarriagePreparationStartedText.ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), string.Empty, null, null, "", 0f, null, null, null), false, false);
			}
			this.FinalizeMarriageOffer();
		}

		// Token: 0x06004275 RID: 17013 RVA: 0x0013FC9F File Offset: 0x0013DE9F
		public void OnMarriageOfferedToPlayer(Hero suitor, Hero maiden)
		{
		}

		// Token: 0x06004276 RID: 17014 RVA: 0x0013FCA4 File Offset: 0x0013DEA4
		public void OnMarriageOfferDeclinedOnPopUp()
		{
			CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
		}

		// Token: 0x06004277 RID: 17015 RVA: 0x0013FCF1 File Offset: 0x0013DEF1
		public void OnMarriageOfferCanceled(Hero suitor, Hero maiden)
		{
			this.FinalizeMarriageOffer();
		}

		// Token: 0x06004278 RID: 17016 RVA: 0x0013FCFC File Offset: 0x0013DEFC
		public bool IsHeroEngaged(Hero hero)
		{
			foreach (KeyValuePair<Hero, Hero> keyValuePair in this._acceptedMarriageOffersThatWaitingForAvailability)
			{
				if (keyValuePair.Key == hero || keyValuePair.Value == hero)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06004279 RID: 17017 RVA: 0x0013FD64 File Offset: 0x0013DF64
		private void HourlyTick()
		{
			if (this.IsThereActiveMarriageOffer && this._lastMarriageOfferTime.ElapsedDaysUntilNow >= 2f)
			{
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
			}
			MarriageModel marriageModel = Campaign.Current.Models.MarriageModel;
			List<Hero> list = new List<Hero>();
			foreach (KeyValuePair<Hero, Hero> keyValuePair in this._acceptedMarriageOffersThatWaitingForAvailability)
			{
				Hero key = keyValuePair.Key;
				Hero value = keyValuePair.Value;
				if (marriageModel.IsCoupleSuitableForMarriage(key, value))
				{
					this.MarryHeroesViaOffer(key, value, false);
					list.Add(key);
				}
				else if (!marriageModel.ShouldNpcMarriageBetweenClansBeAllowed(Clan.PlayerClan, value.Clan))
				{
					list.Add(key);
				}
			}
			foreach (Hero key2 in list)
			{
				this._acceptedMarriageOffersThatWaitingForAvailability.Remove(key2);
			}
		}

		// Token: 0x0600427A RID: 17018 RVA: 0x0013FEB8 File Offset: 0x0013E0B8
		private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
		{
			if (this.IsThereActiveMarriageOffer && (prisoner == Hero.MainHero || prisoner == this._currentOfferedPlayerClanHero || prisoner == this._currentOfferedOtherClanHero))
			{
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
			}
		}

		// Token: 0x0600427B RID: 17019 RVA: 0x0013FF28 File Offset: 0x0013E128
		private void OnHeroesMarried(Hero hero1, Hero hero2, bool showNotification = true)
		{
			if (this.IsThereActiveMarriageOffer && ((hero1 == this._currentOfferedPlayerClanHero && hero2 == this._currentOfferedOtherClanHero) || (hero1 == this._currentOfferedOtherClanHero && hero2 == this._currentOfferedPlayerClanHero)))
			{
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
			}
		}

		// Token: 0x0600427C RID: 17020 RVA: 0x0013FFA4 File Offset: 0x0013E1A4
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			if (this.IsThereActiveMarriageOffer && (victim == Hero.MainHero || victim == this._currentOfferedPlayerClanHero || victim == this._currentOfferedOtherClanHero))
			{
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
			}
			Hero hero = null;
			foreach (KeyValuePair<Hero, Hero> keyValuePair in this._acceptedMarriageOffersThatWaitingForAvailability)
			{
				if (keyValuePair.Key == victim || keyValuePair.Value == victim)
				{
					hero = keyValuePair.Key;
				}
			}
			if (hero != null)
			{
				Hero hero2 = null;
				Hero hero3 = null;
				if (hero.IsDead)
				{
					hero2 = hero;
					hero3 = this._acceptedMarriageOffersThatWaitingForAvailability[hero];
				}
				else if (this._acceptedMarriageOffersThatWaitingForAvailability[hero].IsDead)
				{
					hero2 = this._acceptedMarriageOffersThatWaitingForAvailability[hero];
					hero3 = hero;
				}
				this.MarriagePreparationCanceledText.SetCharacterProperties("DEAD_HERO", hero2.CharacterObject, false);
				this.MarriagePreparationCanceledText.SetTextVariable("OTHER_HERO", hero3.Name);
				InformationManager.ShowInquiry(new InquiryData(string.Empty, this.MarriagePreparationCanceledText.ToString(), true, false, GameTexts.FindText("str_ok", null).ToString(), string.Empty, null, null, "", 0f, null, null, null), false, false);
				this._acceptedMarriageOffersThatWaitingForAvailability.Remove(hero);
			}
		}

		// Token: 0x0600427D RID: 17021 RVA: 0x00140138 File Offset: 0x0013E338
		private void OnArmyCreated(Army army)
		{
			if (this.IsThereActiveMarriageOffer)
			{
				MobileParty partyBelongedTo = this._currentOfferedPlayerClanHero.PartyBelongedTo;
				if (((partyBelongedTo != null) ? partyBelongedTo.Army : null) == null)
				{
					MobileParty partyBelongedTo2 = this._currentOfferedOtherClanHero.PartyBelongedTo;
					if (((partyBelongedTo2 != null) ? partyBelongedTo2.Army : null) == null)
					{
						return;
					}
				}
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
			}
		}

		// Token: 0x0600427E RID: 17022 RVA: 0x001401C0 File Offset: 0x0013E3C0
		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (this.IsThereActiveMarriageOffer)
			{
				MobileParty partyBelongedTo = this._currentOfferedPlayerClanHero.PartyBelongedTo;
				if (((partyBelongedTo != null) ? partyBelongedTo.MapEvent : null) == null)
				{
					MobileParty partyBelongedTo2 = this._currentOfferedOtherClanHero.PartyBelongedTo;
					if (((partyBelongedTo2 != null) ? partyBelongedTo2.MapEvent : null) == null)
					{
						return;
					}
				}
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
			}
		}

		// Token: 0x0600427F RID: 17023 RVA: 0x00140248 File Offset: 0x0013E448
		private void CharacterBecameFugitive(Hero hero, bool showNotification)
		{
			if (this.IsThereActiveMarriageOffer && (!this._currentOfferedPlayerClanHero.IsActive || !this._currentOfferedOtherClanHero.IsActive))
			{
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
			}
		}

		// Token: 0x06004280 RID: 17024 RVA: 0x001402B8 File Offset: 0x0013E4B8
		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail declareWarDetail)
		{
			if (this.IsThereActiveMarriageOffer && (!Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(this._currentOfferedPlayerClanHero, this._currentOfferedOtherClanHero) || !Campaign.Current.Models.MarriageModel.ShouldNpcMarriageBetweenClansBeAllowed(Clan.PlayerClan, this._currentOfferedOtherClanHero.Clan)))
			{
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
			}
		}

		// Token: 0x06004281 RID: 17025 RVA: 0x00140358 File Offset: 0x0013E558
		private void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
		{
			if (this.IsThereActiveMarriageOffer && (effectiveHero.Clan == this._currentOfferedPlayerClanHero.Clan || effectiveHero.Clan == this._currentOfferedOtherClanHero.Clan) && (effectiveHeroGainedRelationWith.Clan == this._currentOfferedPlayerClanHero.Clan || effectiveHeroGainedRelationWith.Clan == this._currentOfferedOtherClanHero.Clan) && !Campaign.Current.Models.MarriageModel.ShouldNpcMarriageBetweenClansBeAllowed(this._currentOfferedPlayerClanHero.Clan, this._currentOfferedOtherClanHero.Clan))
			{
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
			}
		}

		// Token: 0x06004282 RID: 17026 RVA: 0x0014042C File Offset: 0x0013E62C
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (this.IsThereActiveMarriageOffer && (this._currentOfferedPlayerClanHero.Clan == clan || this._currentOfferedOtherClanHero.Clan == clan) && !Campaign.Current.Models.MarriageModel.ShouldNpcMarriageBetweenClansBeAllowed(this._currentOfferedPlayerClanHero.Clan, this._currentOfferedOtherClanHero.Clan))
			{
				CampaignEventDispatcher.Instance.OnMarriageOfferCanceled(this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedOtherClanHero : this._currentOfferedPlayerClanHero, this._currentOfferedPlayerClanHero.IsFemale ? this._currentOfferedPlayerClanHero : this._currentOfferedOtherClanHero);
			}
		}

		// Token: 0x06004283 RID: 17027 RVA: 0x001404CC File Offset: 0x0013E6CC
		private bool CanOfferMarriageForClan(Clan consideringClan)
		{
			return !this.IsThereActiveMarriageOffer && this._lastMarriageOfferTime.ElapsedDaysUntilNow >= 7f && !Hero.MainHero.IsPrisoner && !MobileParty.MainParty.IsInRaftState && consideringClan != Clan.PlayerClan && Campaign.Current.Models.MarriageModel.IsClanSuitableForMarriage(consideringClan) && Campaign.Current.Models.MarriageModel.ShouldNpcMarriageBetweenClansBeAllowed(Clan.PlayerClan, consideringClan);
		}

		// Token: 0x06004284 RID: 17028 RVA: 0x00140548 File Offset: 0x0013E748
		private bool ConsiderMarriageForPlayerClanMember(Hero playerClanHero, Clan consideringClan)
		{
			MarriageModel marriageModel = Campaign.Current.Models.MarriageModel;
			foreach (Hero hero in consideringClan.Heroes)
			{
				float num = marriageModel.NpcCoupleMarriageChance(playerClanHero, hero);
				if (num > 0f && MBRandom.RandomFloat < num)
				{
					foreach (Romance.RomanticState romanticState in Romance.RomanticStateList)
					{
						if (romanticState.Level >= Romance.RomanceLevelEnum.MatchMadeByFamily && (romanticState.Person1 == playerClanHero || romanticState.Person2 == playerClanHero || romanticState.Person1 == hero || romanticState.Person2 == hero))
						{
							return false;
						}
					}
					this.CreateMarriageOffer(playerClanHero, hero);
					return true;
				}
			}
			return false;
		}

		// Token: 0x06004285 RID: 17029 RVA: 0x00140648 File Offset: 0x0013E848
		private void FinalizeMarriageOffer()
		{
			this._currentOfferedPlayerClanHero = null;
			this._currentOfferedOtherClanHero = null;
		}

		// Token: 0x040012FA RID: 4858
		private const int MarriageOfferCooldownDurationAsDays = 7;

		// Token: 0x040012FB RID: 4859
		private const int OfferRelationGainAmountWithTheMarriageClan = 10;

		// Token: 0x040012FC RID: 4860
		private const float MapNotificationAutoDeclineDurationInDays = 2f;

		// Token: 0x040012FD RID: 4861
		private readonly TextObject MarriageOfferPanelExplanationText = new TextObject("{=CZwrlJMJ}A courier with a marriage offer for {CLAN_MEMBER.NAME} from {OFFERING_CLAN_NAME} has arrived.", null);

		// Token: 0x040012FE RID: 4862
		private readonly TextObject MarriagePreparationStartedText = new TextObject("{=yz78jqbx}Ceremony is being prepared and will be conducted as soon as possible.", null);

		// Token: 0x040012FF RID: 4863
		private readonly TextObject MarriagePreparationCanceledText = new TextObject("{=dp044PNk}Due to the untimely death of {DEAD_HERO}, {?DEAD_HERO.GENDER}her{?}his{\\\\?} marriage with {OTHER_HERO} has been cancelled.", null);

		// Token: 0x04001300 RID: 4864
		private Hero _currentOfferedPlayerClanHero;

		// Token: 0x04001301 RID: 4865
		private Hero _currentOfferedOtherClanHero;

		// Token: 0x04001302 RID: 4866
		private CampaignTime _lastMarriageOfferTime;

		// Token: 0x04001303 RID: 4867
		private Dictionary<Hero, Hero> _acceptedMarriageOffersThatWaitingForAvailability = new Dictionary<Hero, Hero>();
	}
}
