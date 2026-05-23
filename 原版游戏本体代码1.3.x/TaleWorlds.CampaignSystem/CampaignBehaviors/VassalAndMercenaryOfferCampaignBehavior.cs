using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200044A RID: 1098
	public class VassalAndMercenaryOfferCampaignBehavior : CampaignBehaviorBase, IVassalAndMercenaryOfferCampaignBehavior
	{
		// Token: 0x17000E2D RID: 3629
		// (get) Token: 0x060045E7 RID: 17895 RVA: 0x0015BDC7 File Offset: 0x00159FC7
		private static TextObject DecisionPopUpTitleText
		{
			get
			{
				return new TextObject("{=ho5EndaV}Decision", null);
			}
		}

		// Token: 0x17000E2E RID: 3630
		// (get) Token: 0x060045E8 RID: 17896 RVA: 0x0015BDD4 File Offset: 0x00159FD4
		private static TextObject DecisionPopUpAffirmativeText
		{
			get
			{
				return new TextObject("{=Y94H6XnK}Accept", null);
			}
		}

		// Token: 0x17000E2F RID: 3631
		// (get) Token: 0x060045E9 RID: 17897 RVA: 0x0015BDE1 File Offset: 0x00159FE1
		private static TextObject DecisionPopUpNegativeText
		{
			get
			{
				return new TextObject("{=cOgmdp9e}Decline", null);
			}
		}

		// Token: 0x060045EA RID: 17898 RVA: 0x0015BDF0 File Offset: 0x00159FF0
		public override void RegisterEvents()
		{
			if (!this._stopOffers)
			{
				CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
				CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.DailyTick));
				CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(this.OnHeroPrisonerTaken));
				CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
				CampaignEvents.OnVassalOrMercenaryServiceOfferedToPlayerEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnVassalOrMercenaryServiceOfferedToPlayer));
				CampaignEvents.WarDeclared.AddNonSerializedListener(this, new Action<IFaction, IFaction, DeclareWarAction.DeclareWarDetail>(this.OnWarDeclared));
				CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, new Action<Hero, Hero, int, bool, ChangeRelationAction.ChangeRelationDetail, Hero, Hero>(this.OnHeroRelationChanged));
				CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, new Action<Kingdom>(this.OnKingdomDestroyed));
				CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, new Action<Hero, Hero, MobileParty, bool>(this.OnPlayerCharacterChanged));
			}
		}

		// Token: 0x060045EB RID: 17899 RVA: 0x0015BED7 File Offset: 0x0015A0D7
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Tuple<Kingdom, CampaignTime>>("_currentMercenaryOffer", ref this._currentMercenaryOffer);
			dataStore.SyncData<Dictionary<Kingdom, CampaignTime>>("_vassalOffers", ref this._vassalOffers);
			dataStore.SyncData<bool>("_stopOffers", ref this._stopOffers);
		}

		// Token: 0x060045EC RID: 17900 RVA: 0x0015BF0F File Offset: 0x0015A10F
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddVassalDialogues(campaignGameStarter);
		}

		// Token: 0x060045ED RID: 17901 RVA: 0x0015BF18 File Offset: 0x0015A118
		private void DailyTick()
		{
			if (!this._stopOffers && Clan.PlayerClan.Tier > Campaign.Current.Models.ClanTierModel.MinClanTier)
			{
				if (this._currentMercenaryOffer != null)
				{
					if (this._currentMercenaryOffer.Item2.ElapsedDaysUntilNow >= 2f || !this.MercenaryKingdomSelectionConditionsHold(this._currentMercenaryOffer.Item1))
					{
						this.CancelVassalOrMercenaryServiceOffer(this._currentMercenaryOffer.Item1);
						return;
					}
				}
				else if (!Hero.MainHero.IsPrisoner && !MobileParty.MainParty.IsInRaftState)
				{
					float randomFloat = MBRandom.RandomFloat;
					if (randomFloat <= 0.02f && this.CanPlayerClanReceiveMercenaryOffer())
					{
						Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate(new Func<Kingdom, bool>(this.MercenaryKingdomSelectionConditionsHold));
						if (randomElementWithPredicate != null)
						{
							this.CreateMercenaryOffer(randomElementWithPredicate);
							return;
						}
					}
					else if (randomFloat <= 0.01f && this.CanPlayerClanReceiveVassalOffer())
					{
						Kingdom randomElementWithPredicate2 = Kingdom.All.GetRandomElementWithPredicate(new Func<Kingdom, bool>(this.VassalKingdomSelectionConditionsHold));
						if (randomElementWithPredicate2 != null)
						{
							this.CreateVassalOffer(randomElementWithPredicate2);
						}
					}
				}
			}
		}

		// Token: 0x060045EE RID: 17902 RVA: 0x0015C020 File Offset: 0x0015A220
		private bool VassalKingdomSelectionConditionsHold(Kingdom kingdom)
		{
			List<IFaction> list;
			List<IFaction> list2;
			return !this._vassalOffers.ContainsKey(kingdom) && FactionHelper.CanPlayerOfferVassalage(kingdom, out list, out list2) && !kingdom.Leader.IsPrisoner && !kingdom.Leader.IsFugitive;
		}

		// Token: 0x060045EF RID: 17903 RVA: 0x0015C064 File Offset: 0x0015A264
		private bool MercenaryKingdomSelectionConditionsHold(Kingdom kingdom)
		{
			List<IFaction> list;
			List<IFaction> list2;
			return !kingdom.IsEliminated && FactionHelper.CanPlayerOfferMercenaryService(kingdom, out list, out list2) && !kingdom.Leader.IsPrisoner && !kingdom.Leader.IsFugitive;
		}

		// Token: 0x060045F0 RID: 17904 RVA: 0x0015C0A4 File Offset: 0x0015A2A4
		private void OnHeroPrisonerTaken(PartyBase captor, Hero prisoner)
		{
			if (prisoner == Hero.MainHero && this._currentMercenaryOffer != null)
			{
				this.CancelVassalOrMercenaryServiceOffer(this._currentMercenaryOffer.Item1);
				using (List<Kingdom>.Enumerator enumerator = this._vassalOffers.Keys.ToList<Kingdom>().GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Kingdom kingdom = enumerator.Current;
						this.CancelVassalOrMercenaryServiceOffer(kingdom);
					}
					return;
				}
			}
			if (prisoner.IsKingdomLeader)
			{
				this.CancelVassalOrMercenaryServiceOffer(prisoner.MapFaction as Kingdom);
			}
		}

		// Token: 0x060045F1 RID: 17905 RVA: 0x0015C13C File Offset: 0x0015A33C
		private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
		{
			if (clan == Clan.PlayerClan && newKingdom != null)
			{
				if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary && this._currentMercenaryOffer != null && this._currentMercenaryOffer.Item1 != newKingdom)
				{
					this.CancelVassalOrMercenaryServiceOffer(this._currentMercenaryOffer.Item1);
					return;
				}
				if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom || detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom)
				{
					this._stopOffers = true;
					if (this._currentMercenaryOffer != null)
					{
						this.CancelVassalOrMercenaryServiceOffer(this._currentMercenaryOffer.Item1);
					}
					foreach (KeyValuePair<Kingdom, CampaignTime> keyValuePair in this._vassalOffers.ToDictionary((KeyValuePair<Kingdom, CampaignTime> x) => x.Key, (KeyValuePair<Kingdom, CampaignTime> x) => x.Value))
					{
						this.CancelVassalOrMercenaryServiceOffer(keyValuePair.Key);
					}
				}
			}
		}

		// Token: 0x060045F2 RID: 17906 RVA: 0x0015C244 File Offset: 0x0015A444
		private void OnVassalOrMercenaryServiceOfferedToPlayer(Kingdom kingdom)
		{
			if (this._currentMercenaryOffer != null && this._currentMercenaryOffer.Item1 == kingdom)
			{
				this.CreateMercenaryOfferDecisionPopUp(kingdom);
			}
		}

		// Token: 0x060045F3 RID: 17907 RVA: 0x0015C263 File Offset: 0x0015A463
		public void CancelVassalOrMercenaryServiceOffer(Kingdom kingdom)
		{
			this.ClearKingdomOffer(kingdom);
			CampaignEventDispatcher.Instance.OnVassalOrMercenaryServiceOfferCanceled(kingdom);
		}

		// Token: 0x060045F4 RID: 17908 RVA: 0x0015C277 File Offset: 0x0015A477
		private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
		{
			if ((faction1 == Clan.PlayerClan || faction2 == Clan.PlayerClan) && this._currentMercenaryOffer != null && !this.MercenaryKingdomSelectionConditionsHold(this._currentMercenaryOffer.Item1))
			{
				this.CancelVassalOrMercenaryServiceOffer(this._currentMercenaryOffer.Item1);
			}
		}

		// Token: 0x060045F5 RID: 17909 RVA: 0x0015C2B5 File Offset: 0x0015A4B5
		private void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
		{
			if ((effectiveHero == Hero.MainHero || effectiveHeroGainedRelationWith == Hero.MainHero) && this._currentMercenaryOffer != null && !this.MercenaryKingdomSelectionConditionsHold(this._currentMercenaryOffer.Item1))
			{
				this.CancelVassalOrMercenaryServiceOffer(this._currentMercenaryOffer.Item1);
			}
		}

		// Token: 0x060045F6 RID: 17910 RVA: 0x0015C2F3 File Offset: 0x0015A4F3
		private void OnKingdomDestroyed(Kingdom destroyedKingdom)
		{
			if ((this._currentMercenaryOffer != null && this._currentMercenaryOffer.Item1 == destroyedKingdom) || this._vassalOffers.ContainsKey(destroyedKingdom))
			{
				this.CancelVassalOrMercenaryServiceOffer(destroyedKingdom);
			}
		}

		// Token: 0x060045F7 RID: 17911 RVA: 0x0015C320 File Offset: 0x0015A520
		private void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
		{
			if (this._currentMercenaryOffer != null)
			{
				this.CancelVassalOrMercenaryServiceOffer(this._currentMercenaryOffer.Item1);
			}
			if (!this._vassalOffers.IsEmpty<KeyValuePair<Kingdom, CampaignTime>>())
			{
				foreach (Kingdom kingdom in Kingdom.All)
				{
					if (this._vassalOffers.ContainsKey(kingdom))
					{
						this.CancelVassalOrMercenaryServiceOffer(kingdom);
					}
				}
			}
		}

		// Token: 0x060045F8 RID: 17912 RVA: 0x0015C3A8 File Offset: 0x0015A5A8
		private void ClearKingdomOffer(Kingdom kingdom)
		{
			if (this._currentMercenaryOffer != null && this._currentMercenaryOffer.Item1 == kingdom)
			{
				this._currentMercenaryOffer = null;
			}
			this._vassalOffers.Remove(kingdom);
		}

		// Token: 0x060045F9 RID: 17913 RVA: 0x0015C3D4 File Offset: 0x0015A5D4
		private bool CanPlayerClanReceiveMercenaryOffer()
		{
			return Clan.PlayerClan.Kingdom == null && Clan.PlayerClan.Tier == Campaign.Current.Models.ClanTierModel.MercenaryEligibleTier;
		}

		// Token: 0x060045FA RID: 17914 RVA: 0x0015C404 File Offset: 0x0015A604
		public void CreateMercenaryOffer(Kingdom kingdom)
		{
			this._currentMercenaryOffer = new Tuple<Kingdom, CampaignTime>(kingdom, CampaignTime.Now);
			VassalAndMercenaryOfferCampaignBehavior.MercenaryOfferPanelNotificationText.SetCharacterProperties("OFFERED_KINGDOM_LEADER", kingdom.Leader.CharacterObject, false);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new MercenaryOfferMapNotification(kingdom, VassalAndMercenaryOfferCampaignBehavior.MercenaryOfferPanelNotificationText));
		}

		// Token: 0x060045FB RID: 17915 RVA: 0x0015C458 File Offset: 0x0015A658
		private void CreateMercenaryOfferDecisionPopUp(Kingdom kingdom)
		{
			Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
			int mercenaryAwardFactorToJoinKingdom = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(Clan.PlayerClan, kingdom, true);
			VassalAndMercenaryOfferCampaignBehavior.MercenaryOfferDecisionPopUpExplanationText.SetTextVariable("OFFERED_KINGDOM_NAME", kingdom.Name);
			VassalAndMercenaryOfferCampaignBehavior.MercenaryOfferDecisionPopUpExplanationText.SetTextVariable("GOLD_AMOUNT", mercenaryAwardFactorToJoinKingdom);
			InformationManager.ShowInquiry(new InquiryData(VassalAndMercenaryOfferCampaignBehavior.DecisionPopUpTitleText.ToString(), VassalAndMercenaryOfferCampaignBehavior.MercenaryOfferDecisionPopUpExplanationText.ToString(), true, true, VassalAndMercenaryOfferCampaignBehavior.DecisionPopUpAffirmativeText.ToString(), VassalAndMercenaryOfferCampaignBehavior.DecisionPopUpNegativeText.ToString(), new Action(this.MercenaryOfferAccepted), new Action(this.MercenaryOfferDeclined), "", 0f, null, null, null), false, false);
		}

		// Token: 0x060045FC RID: 17916 RVA: 0x0015C510 File Offset: 0x0015A710
		private void MercenaryOfferAccepted()
		{
			Kingdom item = this._currentMercenaryOffer.Item1;
			this.ClearKingdomOffer(this._currentMercenaryOffer.Item1);
			int mercenaryAwardFactorToJoinKingdom = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(Clan.PlayerClan, item, true);
			ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Clan.PlayerClan, item, default(CampaignTime), mercenaryAwardFactorToJoinKingdom, true);
		}

		// Token: 0x060045FD RID: 17917 RVA: 0x0015C56C File Offset: 0x0015A76C
		private void MercenaryOfferDeclined()
		{
			this.ClearKingdomOffer(this._currentMercenaryOffer.Item1);
		}

		// Token: 0x060045FE RID: 17918 RVA: 0x0015C57F File Offset: 0x0015A77F
		private bool CanPlayerClanReceiveVassalOffer()
		{
			return (Clan.PlayerClan.Kingdom == null || Clan.PlayerClan.IsUnderMercenaryService) && Clan.PlayerClan.Tier >= Campaign.Current.Models.ClanTierModel.VassalEligibleTier;
		}

		// Token: 0x060045FF RID: 17919 RVA: 0x0015C5C0 File Offset: 0x0015A7C0
		public void CreateVassalOffer(Kingdom kingdom)
		{
			this._vassalOffers.Add(kingdom, CampaignTime.Now);
			VassalAndMercenaryOfferCampaignBehavior.VassalOfferPanelNotificationText.SetTextVariable("OFFERED_KINGDOM_NAME", kingdom.Name);
			VassalAndMercenaryOfferCampaignBehavior.VassalOfferPanelNotificationText.SetCharacterProperties("OFFERED_KINGDOM_LEADER", kingdom.Leader.CharacterObject, false);
			Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new VassalOfferMapNotification(kingdom, VassalAndMercenaryOfferCampaignBehavior.VassalOfferPanelNotificationText));
		}

		// Token: 0x06004600 RID: 17920 RVA: 0x0015C62C File Offset: 0x0015A82C
		private void AddVassalDialogues(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddDialogLine("valid_vassal_offer_start", "start", "valid_vassal_offer_player_response", "{=aDABE6Md}Greetings, {PLAYER.NAME}. I am glad that you received my message. Are you interested in my offer?", new ConversationSentence.OnConditionDelegate(this.valid_vassal_offer_start_condition), null, int.MaxValue, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_accepts_response", "valid_vassal_offer_player_response", "lord_give_oath_2", "{=IHXqZSnt}Yes, I am ready to accept your offer.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_declines_response", "valid_vassal_offer_player_response", "vassal_offer_king_response_to_decline", "{=FAuoq2gT}While I am honored, I must decline your offer.", null, new ConversationSentence.OnConsequenceDelegate(this.vassal_conversation_end_consequence), 100, null, null);
			campaignGameStarter.AddDialogLine("vassal_offer_king_response_to_accept_continue", "vassal_offer_start_oath", "vassal_offer_king_response_to_accept_start_oath_1_response", "{=54PbMkNw}Good. Then repeat the words of the oath with me: {OATH_LINE_1}", new ConversationSentence.OnConditionDelegate(this.conversation_set_oath_phrases_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_1", "vassal_offer_king_response_to_accept_start_oath_1_response", "vassal_offer_king_response_to_accept_start_oath_2", "{=!}{OATH_LINE_1}", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_1_decline", "vassal_offer_king_response_to_accept_start_oath_1_response", "vassal_offer_king_response_to_accept_start_oath_decline", "{=8bLwh9yy}Excuse me, {?CONVERSATION_NPC.GENDER}my lady{?}sir{\\?}. But I feel I need to think about this.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("vassal_offer_lord_oath_2", "vassal_offer_king_response_to_accept_start_oath_2", "vassal_offer_king_response_to_accept_start_oath_2_response", "{=!}{OATH_LINE_2}", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_2", "vassal_offer_king_response_to_accept_start_oath_2_response", "vassal_offer_king_response_to_accept_start_oath_3", "{=!}{OATH_LINE_2}", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_2_decline", "vassal_offer_king_response_to_accept_start_oath_2_response", "vassal_offer_king_response_to_accept_start_oath_decline", "{=LKdrCaTO}{?CONVERSATION_NPC.GENDER}My lady{?}Sir{\\?}, may I ask for some time to think about this?", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("vassal_offer_lord_oath_3", "vassal_offer_king_response_to_accept_start_oath_3", "vassal_offer_king_response_to_accept_start_oath_3_response", "{=!}{OATH_LINE_3}", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_3", "vassal_offer_king_response_to_accept_start_oath_3_response", "vassal_offer_king_response_to_accept_start_oath_4", "{=!}{OATH_LINE_3}", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_3_decline", "vassal_offer_king_response_to_accept_start_oath_3_response", "vassal_offer_king_response_to_accept_start_oath_decline", "{=aa5F4vP5}My {?CONVERSATION_NPC.GENDER}lady{?}lord{\\?}, please give me more time to think about this.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("vassal_offer_lord_oath_4", "vassal_offer_king_response_to_accept_start_oath_4", "vassal_offer_king_response_to_accept_start_oath_4_response", "{=!}{OATH_LINE_4}", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_4", "vassal_offer_king_response_to_accept_start_oath_4_response", "lord_give_oath_10", "{=!}{OATH_LINE_4}", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_4_decline", "vassal_offer_king_response_to_accept_start_oath_4_response", "vassal_offer_king_response_to_accept_start_oath_decline", "{=aupbQveh}{?CONVERSATION_NPC.GENDER}Madame{?}Sir{\\?}, I must have more time to consider this.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("vassal_offer_king_response_to_decline_during_oath", "vassal_offer_king_response_to_accept_start_oath_decline", "lord_start", "{=vueZBBYB}Indeed. I am not sure why you didn't make up your mind before coming to speak with me.", null, new ConversationSentence.OnConsequenceDelegate(this.vassal_conversation_end_consequence), 100, null);
			campaignGameStarter.AddDialogLine("vassal_offer_king_response_to_decline_continue", "vassal_offer_king_response_to_decline", "lord_start", "{=Lo2kJuhK}I am sorry to hear that.", null, null, 100, null);
			campaignGameStarter.AddDialogLine("invalid_vassal_offer_start", "start", "invalid_vassal_offer_player_response", "{=!}{INVALID_REASON}[if:idle_angry][ib:closed]", new ConversationSentence.OnConditionDelegate(this.invalid_vassal_offer_start_condition), null, int.MaxValue, null);
			campaignGameStarter.AddPlayerLine("vassal_offer_player_accepts_response_2", "invalid_vassal_offer_player_response", "lord_start", "{=AmBEgOyq}I see...", null, new ConversationSentence.OnConsequenceDelegate(this.vassal_conversation_end_consequence), 100, null, null);
		}

		// Token: 0x06004601 RID: 17921 RVA: 0x0015C8EC File Offset: 0x0015AAEC
		private bool valid_vassal_offer_start_condition()
		{
			if (PlayerEncounter.Current != null && PlayerEncounter.Current.IsJoinedBattle)
			{
				return false;
			}
			if (Hero.OneToOneConversationHero != null)
			{
				IFaction mapFaction = Hero.OneToOneConversationHero.MapFaction;
				if ((mapFaction == null || mapFaction.IsKingdomFaction) && !Hero.OneToOneConversationHero.IsPrisoner)
				{
					KeyValuePair<Kingdom, CampaignTime> keyValuePair = this._vassalOffers.FirstOrDefault(delegate(KeyValuePair<Kingdom, CampaignTime> o)
					{
						IFaction key = o.Key;
						Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
						return key == ((oneToOneConversationHero != null) ? oneToOneConversationHero.MapFaction : null);
					});
					List<IFaction> list;
					List<IFaction> list2;
					bool flag = Hero.OneToOneConversationHero != null && keyValuePair.Key != null && Hero.OneToOneConversationHero == keyValuePair.Key.Leader && FactionHelper.CanPlayerOfferVassalage((Kingdom)Hero.OneToOneConversationHero.MapFaction, out list, out list2);
					if (flag)
					{
						StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, null, false);
						Hero.OneToOneConversationHero.SetHasMet();
						float scoreOfKingdomToGetClan = Campaign.Current.Models.DiplomacyModel.GetScoreOfKingdomToGetClan((Kingdom)Hero.OneToOneConversationHero.MapFaction, Clan.PlayerClan);
						flag &= scoreOfKingdomToGetClan > 0f;
					}
					return flag;
				}
			}
			return false;
		}

		// Token: 0x06004602 RID: 17922 RVA: 0x0015CA00 File Offset: 0x0015AC00
		private bool conversation_set_oath_phrases_on_condition()
		{
			Hero leader = Hero.OneToOneConversationHero.MapFaction.Leader;
			string stringId = Hero.OneToOneConversationHero.Culture.StringId;
			MBTextManager.SetTextVariable("FACTION_TITLE", leader.IsFemale ? Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_liege_title_female", leader.CharacterObject) : Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_liege_title", leader.CharacterObject), false);
			StringHelpers.SetCharacterProperties("LORD", CharacterObject.OneToOneConversationCharacter, null, false);
			if (stringId == "empire")
			{
				MBTextManager.SetTextVariable("OATH_LINE_1", "{=ya8VF98X}I swear by my ancestors that you are lawful {FACTION_TITLE}.", false);
			}
			else if (stringId == "khuzait")
			{
				MBTextManager.SetTextVariable("OATH_LINE_1", "{=PP8VeNiC}I swear that you are my {?LORD.GENDER}khatun{?}khan{\\?}, my {?LORD.GENDER}mother{?}father{\\?}, my protector...", false);
			}
			else
			{
				MBTextManager.SetTextVariable("OATH_LINE_1", "{=MqIg6Mh2}I swear homage to you as lawful {FACTION_TITLE}.", false);
			}
			if (stringId == "empire")
			{
				MBTextManager.SetTextVariable("OATH_LINE_2", "{=vuEyisBW}I affirm that you are executor of the will of the Senate and people...", false);
			}
			else if (stringId == "khuzait")
			{
				MBTextManager.SetTextVariable("OATH_LINE_2", "{=QSPMKz2R}You are the chosen of the Sky, and I shall follow your banner as long as my breath remains...", false);
			}
			else if (stringId == "battania")
			{
				MBTextManager.SetTextVariable("OATH_LINE_2", "{=OHJYAaW5}The powers of Heaven and of the Earth have entrusted to you the guardianship of this sacred land...", false);
			}
			else if (stringId == "aserai")
			{
				MBTextManager.SetTextVariable("OATH_LINE_2", "{=kc3tLqGy}You command the sons of Asera in war and govern them in peace...", false);
			}
			else if (stringId == "sturgia")
			{
				MBTextManager.SetTextVariable("OATH_LINE_2", "{=Qs7qs3b0}You are the shield of our people against the wolves of the forest, the steppe and the sea.", false);
			}
			else
			{
				MBTextManager.SetTextVariable("OATH_LINE_2", "{=PypPEj5Z}I will be your loyal {?PLAYER.GENDER}follower{?}man{\\?} as long as my breath remains...", false);
			}
			if (stringId == "empire")
			{
				MBTextManager.SetTextVariable("OATH_LINE_3", "{=LWFDXeQc}Furthermore, I accept induction into the army of Calradia, at the rank of archon.", false);
			}
			else if (stringId == "khuzait")
			{
				MBTextManager.SetTextVariable("OATH_LINE_3", "{=8lOCOcXw}Your word shall direct the strike of my sword and the flight of my arrow...", false);
			}
			else if (stringId == "aserai")
			{
				MBTextManager.SetTextVariable("OATH_LINE_3", "{=bue9AShm}I swear to fight your enemies and give shelter and water to your friends...", false);
			}
			else if (stringId == "sturgia")
			{
				MBTextManager.SetTextVariable("OATH_LINE_3", "{=U3u2D6Ze}I give you my word and bond, to stand by your banner in battle so long as my breath remains...", false);
			}
			else if (stringId == "battania")
			{
				MBTextManager.SetTextVariable("OATH_LINE_3", "{=UwbhGhGw}I shall stand by your side and not foresake you, and fight until my life leaves my body...", false);
			}
			else
			{
				MBTextManager.SetTextVariable("OATH_LINE_3", "{=2o7U1bNV}..and I will be at your side to fight your enemies should you need my sword.", false);
			}
			if (stringId == "empire")
			{
				MBTextManager.SetTextVariable("OATH_LINE_4", "{=EsF8sEaQ}And as such, that you are my commander, and I shall follow you wherever you lead.", false);
			}
			else if (stringId == "battania")
			{
				MBTextManager.SetTextVariable("OATH_LINE_4", "{=6KbDn1HS}I shall heed your judgements and pay you the tribute that is your due, so that this land may have a strong protector.", false);
			}
			else if (stringId == "khuzait")
			{
				MBTextManager.SetTextVariable("OATH_LINE_4", "{=xDzxaYed}Your word shall divide the spoils of victory and the bounties of peace.", false);
			}
			else if (stringId == "aserai")
			{
				MBTextManager.SetTextVariable("OATH_LINE_4", "{=qObicX7y}I swear to heed your judgements according to the laws of the Aserai, and ensure that my kinfolk heed them as well...", false);
			}
			else if (stringId == "sturgia")
			{
				MBTextManager.SetTextVariable("OATH_LINE_4", "{=HpWYfcgw}..and to uphold your rights under the laws of the Sturgians, and the rights of your kin, and to avenge their blood as thought it were my own.", false);
			}
			else
			{
				MBTextManager.SetTextVariable("OATH_LINE_4", "{=waoSd6tj}.. and I shall defend your rights and the rights of your legitimate heirs.", false);
			}
			StringHelpers.SetCharacterProperties("CONVERSATION_NPC", CharacterObject.OneToOneConversationCharacter, null, false);
			return true;
		}

		// Token: 0x06004603 RID: 17923 RVA: 0x0015CCF4 File Offset: 0x0015AEF4
		private bool invalid_vassal_offer_start_condition()
		{
			if (Hero.OneToOneConversationHero != null)
			{
				IFaction mapFaction = Hero.OneToOneConversationHero.MapFaction;
				if ((mapFaction == null || mapFaction.IsKingdomFaction) && (PlayerEncounter.Current == null || (PlayerEncounter.Current.EncounterState != PlayerEncounterState.FreeHeroes && PlayerEncounter.Current.EncounterState != PlayerEncounterState.CaptureHeroes)))
				{
					Kingdom offerKingdom = (Kingdom)Hero.OneToOneConversationHero.MapFaction;
					KeyValuePair<Kingdom, CampaignTime> keyValuePair = this._vassalOffers.FirstOrDefault((KeyValuePair<Kingdom, CampaignTime> o) => o.Key == offerKingdom);
					List<IFaction> list = new List<IFaction>();
					List<IFaction> list2 = new List<IFaction>();
					bool flag = Hero.OneToOneConversationHero != null && keyValuePair.Key != null && Hero.OneToOneConversationHero == keyValuePair.Key.Leader && !FactionHelper.CanPlayerOfferVassalage(offerKingdom, out list, out list2);
					if (flag)
					{
						Hero.OneToOneConversationHero.SetHasMet();
						TextObject textObject;
						if (offerKingdom.Leader.GetRelationWithPlayer() < (float)Campaign.Current.Models.DiplomacyModel.MinimumRelationWithConversationCharacterToJoinKingdom)
						{
							textObject = new TextObject("{=niWfuEeh}Well, {PLAYER.NAME}. Are you here about that offer I made? Seeing as what's happened between then and now, surely you realize that that offer no longer stands?", null);
						}
						else if (list.Contains(offerKingdom))
						{
							textObject = new TextObject("{=RACyH7N5}Greetings, {PLAYER.NAME}. I suppose that you're here because of that message I sent you. But we are at war now. I can no longer make that offer to you.", null);
						}
						else if (list2.Intersect(list).Count<IFaction>() != list.Count)
						{
							textObject = new TextObject("{=lynev8Lk}Greetings, {PLAYER.NAME}. I suppose that you're here because of that message I sent you. But the diplomatic situation has changed. You are at war with {WAR_KINGDOMS}, and we are at peace with them. Until that changes, I can no longer accept your fealty.", null);
							List<TextObject> list3 = new List<TextObject>();
							foreach (IFaction faction in list)
							{
								if (!list2.Contains(faction))
								{
									list3.Add(faction.Name);
								}
							}
							textObject.SetTextVariable("WAR_KINGDOMS", GameTexts.GameTextHelper.MergeTextObjectsWithComma(list3, true));
						}
						else
						{
							textObject = TextObject.GetEmpty();
						}
						textObject.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, false);
						MBTextManager.SetTextVariable("INVALID_REASON", textObject, false);
					}
					return flag;
				}
			}
			return false;
		}

		// Token: 0x06004604 RID: 17924 RVA: 0x0015CEE4 File Offset: 0x0015B0E4
		private void vassal_conversation_end_consequence()
		{
			this.CancelVassalOrMercenaryServiceOffer((Kingdom)Hero.OneToOneConversationHero.MapFaction);
		}

		// Token: 0x04001386 RID: 4998
		private const float MercenaryOfferCreationChance = 0.02f;

		// Token: 0x04001387 RID: 4999
		private const float VassalOfferCreationChance = 0.01f;

		// Token: 0x04001388 RID: 5000
		private const int MercenaryOfferCancelTimeInDays = 2;

		// Token: 0x04001389 RID: 5001
		private static readonly TextObject MercenaryOfferDecisionPopUpExplanationText = new TextObject("{=TENbJKpP}The {OFFERED_KINGDOM_NAME} is offering you work as a mercenary, paying {GOLD_AMOUNT}{GOLD_ICON} per influence point that you would gain from fighting on their behalf. Do you accept?", null);

		// Token: 0x0400138A RID: 5002
		private static readonly TextObject MercenaryOfferPanelNotificationText = new TextObject("{=FA2QZc7Q}A courier arrives, bearing a message from {OFFERED_KINGDOM_LEADER.NAME}. {?OFFERED_KINGDOM_LEADER.GENDER}She{?}He{\\?} is offering you a contract as a mercenary.", null);

		// Token: 0x0400138B RID: 5003
		private static readonly TextObject VassalOfferPanelNotificationText = new TextObject("{=7ouzFASf}A courier arrives, bearing a message from {OFFERED_KINGDOM_LEADER.NAME}. {?OFFERED_KINGDOM_LEADER.GENDER}She{?}He{\\?} remarks on your growing reputation, and asks if you would consider pledging yourself as a vassal of the {OFFERED_KINGDOM_NAME}. You should speak in person if you are interested.", null);

		// Token: 0x0400138C RID: 5004
		private Tuple<Kingdom, CampaignTime> _currentMercenaryOffer;

		// Token: 0x0400138D RID: 5005
		private Dictionary<Kingdom, CampaignTime> _vassalOffers = new Dictionary<Kingdom, CampaignTime>();

		// Token: 0x0400138E RID: 5006
		private bool _stopOffers;
	}
}
