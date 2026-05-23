using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class VassalAndMercenaryOfferCampaignBehavior : CampaignBehaviorBase, IVassalAndMercenaryOfferCampaignBehavior
{
	private const float MercenaryOfferCreationChance = 0.02f;

	private const float VassalOfferCreationChance = 0.01f;

	private const int MercenaryOfferCancelTimeInDays = 2;

	private static readonly TextObject MercenaryOfferDecisionPopUpExplanationText = new TextObject("{=TENbJKpP}The {OFFERED_KINGDOM_NAME} is offering you work as a mercenary, paying {GOLD_AMOUNT}{GOLD_ICON} per influence point that you would gain from fighting on their behalf. Do you accept?");

	private static readonly TextObject MercenaryOfferPanelNotificationText = new TextObject("{=FA2QZc7Q}A courier arrives, bearing a message from {OFFERED_KINGDOM_LEADER.NAME}. {?OFFERED_KINGDOM_LEADER.GENDER}She{?}He{\\?} is offering you a contract as a mercenary.");

	private static readonly TextObject VassalOfferPanelNotificationText = new TextObject("{=7ouzFASf}A courier arrives, bearing a message from {OFFERED_KINGDOM_LEADER.NAME}. {?OFFERED_KINGDOM_LEADER.GENDER}She{?}He{\\?} remarks on your growing reputation, and asks if you would consider pledging yourself as a vassal of the {OFFERED_KINGDOM_NAME}. You should speak in person if you are interested.");

	private Tuple<Kingdom, CampaignTime> _currentMercenaryOffer;

	private Dictionary<Kingdom, CampaignTime> _vassalOffers = new Dictionary<Kingdom, CampaignTime>();

	private bool _stopOffers;

	private static TextObject DecisionPopUpTitleText => new TextObject("{=ho5EndaV}Decision");

	private static TextObject DecisionPopUpAffirmativeText => new TextObject("{=Y94H6XnK}Accept");

	private static TextObject DecisionPopUpNegativeText => new TextObject("{=cOgmdp9e}Decline");

	public override void RegisterEvents()
	{
		if (!_stopOffers)
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
			CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
			CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
			CampaignEvents.OnVassalOrMercenaryServiceOfferedToPlayerEvent.AddNonSerializedListener(this, OnVassalOrMercenaryServiceOfferedToPlayer);
			CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
			CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, OnHeroRelationChanged);
			CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
			CampaignEvents.OnPlayerCharacterChangedEvent.AddNonSerializedListener(this, OnPlayerCharacterChanged);
		}
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_currentMercenaryOffer", ref _currentMercenaryOffer);
		dataStore.SyncData("_vassalOffers", ref _vassalOffers);
		dataStore.SyncData("_stopOffers", ref _stopOffers);
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddVassalDialogues(campaignGameStarter);
	}

	private void DailyTick()
	{
		if (_stopOffers || Clan.PlayerClan.Tier <= Campaign.Current.Models.ClanTierModel.MinClanTier)
		{
			return;
		}
		if (_currentMercenaryOffer != null)
		{
			if (_currentMercenaryOffer.Item2.ElapsedDaysUntilNow >= 2f || !MercenaryKingdomSelectionConditionsHold(_currentMercenaryOffer.Item1))
			{
				CancelVassalOrMercenaryServiceOffer(_currentMercenaryOffer.Item1);
			}
		}
		else
		{
			if (Hero.MainHero.IsPrisoner || MobileParty.MainParty.IsInRaftState)
			{
				return;
			}
			float randomFloat = MBRandom.RandomFloat;
			if (randomFloat <= 0.02f && CanPlayerClanReceiveMercenaryOffer())
			{
				Kingdom randomElementWithPredicate = Kingdom.All.GetRandomElementWithPredicate(MercenaryKingdomSelectionConditionsHold);
				if (randomElementWithPredicate != null)
				{
					CreateMercenaryOffer(randomElementWithPredicate);
				}
			}
			else if (randomFloat <= 0.01f && CanPlayerClanReceiveVassalOffer())
			{
				Kingdom randomElementWithPredicate2 = Kingdom.All.GetRandomElementWithPredicate(VassalKingdomSelectionConditionsHold);
				if (randomElementWithPredicate2 != null)
				{
					CreateVassalOffer(randomElementWithPredicate2);
				}
			}
		}
	}

	private bool VassalKingdomSelectionConditionsHold(Kingdom kingdom)
	{
		if (!_vassalOffers.ContainsKey(kingdom) && FactionHelper.CanPlayerOfferVassalage(kingdom, out var _, out var _) && !kingdom.Leader.IsPrisoner)
		{
			return !kingdom.Leader.IsFugitive;
		}
		return false;
	}

	private bool MercenaryKingdomSelectionConditionsHold(Kingdom kingdom)
	{
		if (!kingdom.IsEliminated && FactionHelper.CanPlayerOfferMercenaryService(kingdom, out var _, out var _) && !kingdom.Leader.IsPrisoner)
		{
			return !kingdom.Leader.IsFugitive;
		}
		return false;
	}

	private void OnHeroPrisonerTaken(PartyBase captor, Hero prisoner)
	{
		if (prisoner == Hero.MainHero && _currentMercenaryOffer != null)
		{
			CancelVassalOrMercenaryServiceOffer(_currentMercenaryOffer.Item1);
			{
				foreach (Kingdom item in _vassalOffers.Keys.ToList())
				{
					CancelVassalOrMercenaryServiceOffer(item);
				}
				return;
			}
		}
		if (prisoner.IsKingdomLeader)
		{
			CancelVassalOrMercenaryServiceOffer(prisoner.MapFaction as Kingdom);
		}
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		if (clan != Clan.PlayerClan || newKingdom == null)
		{
			return;
		}
		if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary && _currentMercenaryOffer != null && _currentMercenaryOffer.Item1 != newKingdom)
		{
			CancelVassalOrMercenaryServiceOffer(_currentMercenaryOffer.Item1);
		}
		else
		{
			if (detail != ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom && detail != ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom)
			{
				return;
			}
			_stopOffers = true;
			if (_currentMercenaryOffer != null)
			{
				CancelVassalOrMercenaryServiceOffer(_currentMercenaryOffer.Item1);
			}
			foreach (KeyValuePair<Kingdom, CampaignTime> item in _vassalOffers.ToDictionary((KeyValuePair<Kingdom, CampaignTime> x) => x.Key, (KeyValuePair<Kingdom, CampaignTime> x) => x.Value))
			{
				CancelVassalOrMercenaryServiceOffer(item.Key);
			}
		}
	}

	private void OnVassalOrMercenaryServiceOfferedToPlayer(Kingdom kingdom)
	{
		if (_currentMercenaryOffer != null && _currentMercenaryOffer.Item1 == kingdom)
		{
			CreateMercenaryOfferDecisionPopUp(kingdom);
		}
	}

	public void CancelVassalOrMercenaryServiceOffer(Kingdom kingdom)
	{
		ClearKingdomOffer(kingdom);
		CampaignEventDispatcher.Instance.OnVassalOrMercenaryServiceOfferCanceled(kingdom);
	}

	private void OnWarDeclared(IFaction faction1, IFaction faction2, DeclareWarAction.DeclareWarDetail detail)
	{
		if ((faction1 == Clan.PlayerClan || faction2 == Clan.PlayerClan) && _currentMercenaryOffer != null && !MercenaryKingdomSelectionConditionsHold(_currentMercenaryOffer.Item1))
		{
			CancelVassalOrMercenaryServiceOffer(_currentMercenaryOffer.Item1);
		}
	}

	private void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
	{
		if ((effectiveHero == Hero.MainHero || effectiveHeroGainedRelationWith == Hero.MainHero) && _currentMercenaryOffer != null && !MercenaryKingdomSelectionConditionsHold(_currentMercenaryOffer.Item1))
		{
			CancelVassalOrMercenaryServiceOffer(_currentMercenaryOffer.Item1);
		}
	}

	private void OnKingdomDestroyed(Kingdom destroyedKingdom)
	{
		if ((_currentMercenaryOffer != null && _currentMercenaryOffer.Item1 == destroyedKingdom) || _vassalOffers.ContainsKey(destroyedKingdom))
		{
			CancelVassalOrMercenaryServiceOffer(destroyedKingdom);
		}
	}

	private void OnPlayerCharacterChanged(Hero oldPlayer, Hero newPlayer, MobileParty newMainParty, bool isMainPartyChanged)
	{
		if (_currentMercenaryOffer != null)
		{
			CancelVassalOrMercenaryServiceOffer(_currentMercenaryOffer.Item1);
		}
		if (_vassalOffers.IsEmpty())
		{
			return;
		}
		foreach (Kingdom item in Kingdom.All)
		{
			if (_vassalOffers.ContainsKey(item))
			{
				CancelVassalOrMercenaryServiceOffer(item);
			}
		}
	}

	private void ClearKingdomOffer(Kingdom kingdom)
	{
		if (_currentMercenaryOffer != null && _currentMercenaryOffer.Item1 == kingdom)
		{
			_currentMercenaryOffer = null;
		}
		_vassalOffers.Remove(kingdom);
	}

	private bool CanPlayerClanReceiveMercenaryOffer()
	{
		if (Clan.PlayerClan.Kingdom == null)
		{
			return Clan.PlayerClan.Tier == Campaign.Current.Models.ClanTierModel.MercenaryEligibleTier;
		}
		return false;
	}

	public void CreateMercenaryOffer(Kingdom kingdom)
	{
		_currentMercenaryOffer = new Tuple<Kingdom, CampaignTime>(kingdom, CampaignTime.Now);
		MercenaryOfferPanelNotificationText.SetCharacterProperties("OFFERED_KINGDOM_LEADER", kingdom.Leader.CharacterObject);
		Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new MercenaryOfferMapNotification(kingdom, MercenaryOfferPanelNotificationText));
	}

	private void CreateMercenaryOfferDecisionPopUp(Kingdom kingdom)
	{
		Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
		int mercenaryAwardFactorToJoinKingdom = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(Clan.PlayerClan, kingdom, neededAmountForClanToJoinCalculation: true);
		MercenaryOfferDecisionPopUpExplanationText.SetTextVariable("OFFERED_KINGDOM_NAME", kingdom.Name);
		MercenaryOfferDecisionPopUpExplanationText.SetTextVariable("GOLD_AMOUNT", mercenaryAwardFactorToJoinKingdom);
		InformationManager.ShowInquiry(new InquiryData(DecisionPopUpTitleText.ToString(), MercenaryOfferDecisionPopUpExplanationText.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, DecisionPopUpAffirmativeText.ToString(), DecisionPopUpNegativeText.ToString(), MercenaryOfferAccepted, MercenaryOfferDeclined));
	}

	private void MercenaryOfferAccepted()
	{
		Kingdom item = _currentMercenaryOffer.Item1;
		ClearKingdomOffer(_currentMercenaryOffer.Item1);
		int mercenaryAwardFactorToJoinKingdom = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(Clan.PlayerClan, item, neededAmountForClanToJoinCalculation: true);
		ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Clan.PlayerClan, item, default(CampaignTime), mercenaryAwardFactorToJoinKingdom);
	}

	private void MercenaryOfferDeclined()
	{
		ClearKingdomOffer(_currentMercenaryOffer.Item1);
	}

	private bool CanPlayerClanReceiveVassalOffer()
	{
		if (Clan.PlayerClan.Kingdom == null || Clan.PlayerClan.IsUnderMercenaryService)
		{
			return Clan.PlayerClan.Tier >= Campaign.Current.Models.ClanTierModel.VassalEligibleTier;
		}
		return false;
	}

	public void CreateVassalOffer(Kingdom kingdom)
	{
		_vassalOffers.Add(kingdom, CampaignTime.Now);
		VassalOfferPanelNotificationText.SetTextVariable("OFFERED_KINGDOM_NAME", kingdom.Name);
		VassalOfferPanelNotificationText.SetCharacterProperties("OFFERED_KINGDOM_LEADER", kingdom.Leader.CharacterObject);
		Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new VassalOfferMapNotification(kingdom, VassalOfferPanelNotificationText));
	}

	private void AddVassalDialogues(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddDialogLine("valid_vassal_offer_start", "start", "valid_vassal_offer_player_response", "{=aDABE6Md}Greetings, {PLAYER.NAME}. I am glad that you received my message. Are you interested in my offer?", valid_vassal_offer_start_condition, null, int.MaxValue);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_accepts_response", "valid_vassal_offer_player_response", "lord_give_oath_2", "{=IHXqZSnt}Yes, I am ready to accept your offer.", null, null);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_declines_response", "valid_vassal_offer_player_response", "vassal_offer_king_response_to_decline", "{=FAuoq2gT}While I am honored, I must decline your offer.", null, vassal_conversation_end_consequence);
		campaignGameStarter.AddDialogLine("vassal_offer_king_response_to_accept_continue", "vassal_offer_start_oath", "vassal_offer_king_response_to_accept_start_oath_1_response", "{=54PbMkNw}Good. Then repeat the words of the oath with me: {OATH_LINE_1}", conversation_set_oath_phrases_on_condition, null);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_1", "vassal_offer_king_response_to_accept_start_oath_1_response", "vassal_offer_king_response_to_accept_start_oath_2", "{=!}{OATH_LINE_1}", null, null);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_1_decline", "vassal_offer_king_response_to_accept_start_oath_1_response", "vassal_offer_king_response_to_accept_start_oath_decline", "{=8bLwh9yy}Excuse me, {?CONVERSATION_NPC.GENDER}my lady{?}sir{\\?}. But I feel I need to think about this.", null, null);
		campaignGameStarter.AddDialogLine("vassal_offer_lord_oath_2", "vassal_offer_king_response_to_accept_start_oath_2", "vassal_offer_king_response_to_accept_start_oath_2_response", "{=!}{OATH_LINE_2}", null, null);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_2", "vassal_offer_king_response_to_accept_start_oath_2_response", "vassal_offer_king_response_to_accept_start_oath_3", "{=!}{OATH_LINE_2}", null, null);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_2_decline", "vassal_offer_king_response_to_accept_start_oath_2_response", "vassal_offer_king_response_to_accept_start_oath_decline", "{=LKdrCaTO}{?CONVERSATION_NPC.GENDER}My lady{?}Sir{\\?}, may I ask for some time to think about this?", null, null);
		campaignGameStarter.AddDialogLine("vassal_offer_lord_oath_3", "vassal_offer_king_response_to_accept_start_oath_3", "vassal_offer_king_response_to_accept_start_oath_3_response", "{=!}{OATH_LINE_3}", null, null);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_3", "vassal_offer_king_response_to_accept_start_oath_3_response", "vassal_offer_king_response_to_accept_start_oath_4", "{=!}{OATH_LINE_3}", null, null);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_3_decline", "vassal_offer_king_response_to_accept_start_oath_3_response", "vassal_offer_king_response_to_accept_start_oath_decline", "{=aa5F4vP5}My {?CONVERSATION_NPC.GENDER}lady{?}lord{\\?}, please give me more time to think about this.", null, null);
		campaignGameStarter.AddDialogLine("vassal_offer_lord_oath_4", "vassal_offer_king_response_to_accept_start_oath_4", "vassal_offer_king_response_to_accept_start_oath_4_response", "{=!}{OATH_LINE_4}", null, null);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_4", "vassal_offer_king_response_to_accept_start_oath_4_response", "lord_give_oath_10", "{=!}{OATH_LINE_4}", null, null);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_oath_4_decline", "vassal_offer_king_response_to_accept_start_oath_4_response", "vassal_offer_king_response_to_accept_start_oath_decline", "{=aupbQveh}{?CONVERSATION_NPC.GENDER}Madame{?}Sir{\\?}, I must have more time to consider this.", null, null);
		campaignGameStarter.AddDialogLine("vassal_offer_king_response_to_decline_during_oath", "vassal_offer_king_response_to_accept_start_oath_decline", "lord_start", "{=vueZBBYB}Indeed. I am not sure why you didn't make up your mind before coming to speak with me.", null, vassal_conversation_end_consequence);
		campaignGameStarter.AddDialogLine("vassal_offer_king_response_to_decline_continue", "vassal_offer_king_response_to_decline", "lord_start", "{=Lo2kJuhK}I am sorry to hear that.", null, null);
		campaignGameStarter.AddDialogLine("invalid_vassal_offer_start", "start", "invalid_vassal_offer_player_response", "{=!}{INVALID_REASON}[if:idle_angry][ib:closed]", invalid_vassal_offer_start_condition, null, int.MaxValue);
		campaignGameStarter.AddPlayerLine("vassal_offer_player_accepts_response_2", "invalid_vassal_offer_player_response", "lord_start", "{=AmBEgOyq}I see...", null, vassal_conversation_end_consequence);
	}

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
				KeyValuePair<Kingdom, CampaignTime> keyValuePair = _vassalOffers.FirstOrDefault((KeyValuePair<Kingdom, CampaignTime> o) => o.Key == Hero.OneToOneConversationHero?.MapFaction);
				List<IFaction> playerWars;
				List<IFaction> warsOfFactionToJoin;
				bool flag = Hero.OneToOneConversationHero != null && keyValuePair.Key != null && Hero.OneToOneConversationHero == keyValuePair.Key.Leader && FactionHelper.CanPlayerOfferVassalage((Kingdom)Hero.OneToOneConversationHero.MapFaction, out playerWars, out warsOfFactionToJoin);
				if (flag)
				{
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
					Hero.OneToOneConversationHero.SetHasMet();
					float scoreOfKingdomToGetClan = Campaign.Current.Models.DiplomacyModel.GetScoreOfKingdomToGetClan((Kingdom)Hero.OneToOneConversationHero.MapFaction, Clan.PlayerClan);
					flag = flag && scoreOfKingdomToGetClan > 0f;
				}
				return flag;
			}
		}
		return false;
	}

	private bool conversation_set_oath_phrases_on_condition()
	{
		Hero leader = Hero.OneToOneConversationHero.MapFaction.Leader;
		string stringId = Hero.OneToOneConversationHero.Culture.StringId;
		MBTextManager.SetTextVariable("FACTION_TITLE", leader.IsFemale ? Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_liege_title_female", leader.CharacterObject) : Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_liege_title", leader.CharacterObject));
		StringHelpers.SetCharacterProperties("LORD", CharacterObject.OneToOneConversationCharacter);
		if (stringId == "empire")
		{
			MBTextManager.SetTextVariable("OATH_LINE_1", "{=ya8VF98X}I swear by my ancestors that you are lawful {FACTION_TITLE}.");
		}
		else if (stringId == "khuzait")
		{
			MBTextManager.SetTextVariable("OATH_LINE_1", "{=PP8VeNiC}I swear that you are my {?LORD.GENDER}khatun{?}khan{\\?}, my {?LORD.GENDER}mother{?}father{\\?}, my protector...");
		}
		else
		{
			MBTextManager.SetTextVariable("OATH_LINE_1", "{=MqIg6Mh2}I swear homage to you as lawful {FACTION_TITLE}.");
		}
		switch (stringId)
		{
		case "empire":
			MBTextManager.SetTextVariable("OATH_LINE_2", "{=vuEyisBW}I affirm that you are executor of the will of the Senate and people...");
			break;
		case "khuzait":
			MBTextManager.SetTextVariable("OATH_LINE_2", "{=QSPMKz2R}You are the chosen of the Sky, and I shall follow your banner as long as my breath remains...");
			break;
		case "battania":
			MBTextManager.SetTextVariable("OATH_LINE_2", "{=OHJYAaW5}The powers of Heaven and of the Earth have entrusted to you the guardianship of this sacred land...");
			break;
		case "aserai":
			MBTextManager.SetTextVariable("OATH_LINE_2", "{=kc3tLqGy}You command the sons of Asera in war and govern them in peace...");
			break;
		case "sturgia":
			MBTextManager.SetTextVariable("OATH_LINE_2", "{=Qs7qs3b0}You are the shield of our people against the wolves of the forest, the steppe and the sea.");
			break;
		default:
			MBTextManager.SetTextVariable("OATH_LINE_2", "{=PypPEj5Z}I will be your loyal {?PLAYER.GENDER}follower{?}man{\\?} as long as my breath remains...");
			break;
		}
		switch (stringId)
		{
		case "empire":
			MBTextManager.SetTextVariable("OATH_LINE_3", "{=LWFDXeQc}Furthermore, I accept induction into the army of Calradia, at the rank of archon.");
			break;
		case "khuzait":
			MBTextManager.SetTextVariable("OATH_LINE_3", "{=8lOCOcXw}Your word shall direct the strike of my sword and the flight of my arrow...");
			break;
		case "aserai":
			MBTextManager.SetTextVariable("OATH_LINE_3", "{=bue9AShm}I swear to fight your enemies and give shelter and water to your friends...");
			break;
		case "sturgia":
			MBTextManager.SetTextVariable("OATH_LINE_3", "{=U3u2D6Ze}I give you my word and bond, to stand by your banner in battle so long as my breath remains...");
			break;
		case "battania":
			MBTextManager.SetTextVariable("OATH_LINE_3", "{=UwbhGhGw}I shall stand by your side and not foresake you, and fight until my life leaves my body...");
			break;
		default:
			MBTextManager.SetTextVariable("OATH_LINE_3", "{=2o7U1bNV}..and I will be at your side to fight your enemies should you need my sword.");
			break;
		}
		switch (stringId)
		{
		case "empire":
			MBTextManager.SetTextVariable("OATH_LINE_4", "{=EsF8sEaQ}And as such, that you are my commander, and I shall follow you wherever you lead.");
			break;
		case "battania":
			MBTextManager.SetTextVariable("OATH_LINE_4", "{=6KbDn1HS}I shall heed your judgements and pay you the tribute that is your due, so that this land may have a strong protector.");
			break;
		case "khuzait":
			MBTextManager.SetTextVariable("OATH_LINE_4", "{=xDzxaYed}Your word shall divide the spoils of victory and the bounties of peace.");
			break;
		case "aserai":
			MBTextManager.SetTextVariable("OATH_LINE_4", "{=qObicX7y}I swear to heed your judgements according to the laws of the Aserai, and ensure that my kinfolk heed them as well...");
			break;
		case "sturgia":
			MBTextManager.SetTextVariable("OATH_LINE_4", "{=HpWYfcgw}..and to uphold your rights under the laws of the Sturgians, and the rights of your kin, and to avenge their blood as thought it were my own.");
			break;
		default:
			MBTextManager.SetTextVariable("OATH_LINE_4", "{=waoSd6tj}.. and I shall defend your rights and the rights of your legitimate heirs.");
			break;
		}
		StringHelpers.SetCharacterProperties("CONVERSATION_NPC", CharacterObject.OneToOneConversationCharacter);
		return true;
	}

	private bool invalid_vassal_offer_start_condition()
	{
		if (Hero.OneToOneConversationHero != null)
		{
			IFaction mapFaction = Hero.OneToOneConversationHero.MapFaction;
			if ((mapFaction == null || mapFaction.IsKingdomFaction) && (PlayerEncounter.Current == null || (PlayerEncounter.Current.EncounterState != PlayerEncounterState.FreeHeroes && PlayerEncounter.Current.EncounterState != PlayerEncounterState.CaptureHeroes)))
			{
				Kingdom offerKingdom = (Kingdom)Hero.OneToOneConversationHero.MapFaction;
				KeyValuePair<Kingdom, CampaignTime> keyValuePair = _vassalOffers.FirstOrDefault((KeyValuePair<Kingdom, CampaignTime> o) => o.Key == offerKingdom);
				List<IFaction> playerWars = new List<IFaction>();
				List<IFaction> warsOfFactionToJoin = new List<IFaction>();
				bool flag = Hero.OneToOneConversationHero != null && keyValuePair.Key != null && Hero.OneToOneConversationHero == keyValuePair.Key.Leader && !FactionHelper.CanPlayerOfferVassalage(offerKingdom, out playerWars, out warsOfFactionToJoin);
				if (flag)
				{
					Hero.OneToOneConversationHero.SetHasMet();
					TextObject textObject;
					if (offerKingdom.Leader.GetRelationWithPlayer() < (float)Campaign.Current.Models.DiplomacyModel.MinimumRelationWithConversationCharacterToJoinKingdom)
					{
						textObject = new TextObject("{=niWfuEeh}Well, {PLAYER.NAME}. Are you here about that offer I made? Seeing as what's happened between then and now, surely you realize that that offer no longer stands?");
					}
					else if (playerWars.Contains(offerKingdom))
					{
						textObject = new TextObject("{=RACyH7N5}Greetings, {PLAYER.NAME}. I suppose that you're here because of that message I sent you. But we are at war now. I can no longer make that offer to you.");
					}
					else if (warsOfFactionToJoin.Intersect(playerWars).Count() != playerWars.Count)
					{
						textObject = new TextObject("{=lynev8Lk}Greetings, {PLAYER.NAME}. I suppose that you're here because of that message I sent you. But the diplomatic situation has changed. You are at war with {WAR_KINGDOMS}, and we are at peace with them. Until that changes, I can no longer accept your fealty.");
						List<TextObject> list = new List<TextObject>();
						foreach (IFaction item in playerWars)
						{
							if (!warsOfFactionToJoin.Contains(item))
							{
								list.Add(item.Name);
							}
						}
						textObject.SetTextVariable("WAR_KINGDOMS", GameTexts.GameTextHelper.MergeTextObjectsWithComma(list, includeAnd: true));
					}
					else
					{
						textObject = TextObject.GetEmpty();
					}
					textObject.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
					MBTextManager.SetTextVariable("INVALID_REASON", textObject);
				}
				return flag;
			}
		}
		return false;
	}

	private void vassal_conversation_end_consequence()
	{
		CancelVassalOrMercenaryServiceOffer((Kingdom)Hero.OneToOneConversationHero.MapFaction);
	}
}
