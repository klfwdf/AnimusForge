using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class CaravanConversationsCampaignBehavior : CampaignBehaviorBase
{
	private int _selectedCaravanType;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	protected void AddDialogs(CampaignGameStarter starter)
	{
		starter.AddPlayerLine("caravan_create_conversation_1", "hero_main_options", "magistrate_form_a_caravan_cost", "{=!}{CARAVAN_BUY_INTENT_TEXT}", conversation_caravan_build_on_condition, null, 100, conversation_caravan_build_clickable_condition);
		starter.AddDialogLine("caravan_create_conversation_2", "magistrate_form_a_caravan_cost", "magistrate_form_a_caravan_player_answer", "{=!}{CARAVAN_FORMING_INFO_1}", conversation_magistrate_form_a_caravan_cost_on_condition, null);
		starter.AddPlayerLine("caravan_create_conversation_3", "magistrate_form_a_caravan_player_answer", "lord_pretalk", "{=otVPaR6T}Actually I do not have a free companion right now.", conversation_magistrate_form_caravan_companion_condition, null);
		starter.AddPlayerLine("caravan_create_conversation_4", "magistrate_form_a_caravan_player_answer", "lord_pretalk", "{=w6WFuDn0}I am sorry, I don't have that much money.", conversation_magistrate_form_caravan_gold_condition, null);
		starter.AddPlayerLine("caravan_create_conversation_5", "magistrate_form_a_caravan_player_answer", "magistrate_form_a_caravan_accepted", "{=!}{FORM_CARAVAN_ACCEPT}", conversation_magistrate_form_a_small_caravan_accept_on_condition, conversation_magistrate_form_a_small_caravan_accept_on_consequence);
		starter.AddPlayerLine("caravan_create_conversation_6", "magistrate_form_a_caravan_player_answer", "magistrate_form_a_caravan_big", "{=!}{LARGE_CARAVAN_OFFER}", conversation_magistrate_form_a_large_caravan_accept_on_condition, null);
		starter.AddPlayerLine("caravan_create_conversation_10", "magistrate_form_a_caravan_player_answer", "lord_pretalk", "{=2mJjDTAZ}That sounds expensive.", conversation_magistrate_form_a_caravan_reject_on_condition, null);
		starter.AddDialogLine("caravan_create_conversation_7", "magistrate_form_a_caravan_big", "magistrate_form_a_caravan_big_player_answer", "{=DaBzJkIz}I can increase quality of troops, but cost will proportionally increase, too. It will cost {AMOUNT}{GOLD_ICON}.", conversation_magistrate_form_a_big_caravan_offer_condition, null);
		starter.AddPlayerLine("caravan_create_conversation_8", "magistrate_form_a_caravan_big_player_answer", "magistrate_form_a_caravan_accepted", "{=!}{CREATE_LARGE_CARAVAN_TEXT}", conversation_magistrate_form_a_big_caravan_accept_on_condition, conversation_magistrate_form_a_big_caravan_accept_on_consequence);
		starter.AddPlayerLine("caravan_create_conversation_9", "magistrate_form_a_caravan_big_player_answer", "lord_pretalk", "{=w6WFuDn0}I am sorry, I don't have that much money.", conversation_magistrate_form_a_big_caravan_gold_condition, null);
		starter.AddPlayerLine("caravan_create_conversation_10_2", "magistrate_form_a_caravan_big_player_answer", "lord_pretalk", "{=2mJjDTAZ}That sounds expensive.", conversation_magistrate_form_a_big_caravan_reject_on_condition, null);
		starter.AddDialogLine("caravan_create_conversation_11", "magistrate_form_a_caravan_accepted", "magistrate_form_a_caravan_accepted_choose_leader", "{=!}{CARAVAN_LEADER_CHOOSE_TEXT}", conversation_magistrate_form_a_caravan_accepted_choose_leader_on_condition, null);
		starter.AddRepeatablePlayerLine("caravan_create_conversation_12", "magistrate_form_a_caravan_accepted_choose_leader", "magistrate_form_a_caravan_accepted_leader_is_chosen", "{=!}{HERO.NAME}", "{=UNFE1BeG}I am thinking of a different person", "magistrate_form_a_caravan_accepted", conversation_magistrate_form_a_caravan_accepted_leader_is_chosen_on_condition, conversation_magistrate_form_a_caravan_accept_on_consequence);
		starter.AddPlayerLine("caravan_create_conversation_13", "magistrate_form_a_caravan_accepted_choose_leader", "lord_pretalk", "{=PznWhAdU}Actually, never mind.", null, null);
		starter.AddDialogLine("caravan_create_conversation_14", "magistrate_form_a_caravan_accepted_leader_is_chosen", "close_window", "{=!}{CARAVAN_NOTABLE_FINAL_TALK}", conversation_magistrate_form_a_caravan_final_conversation_on_condition, null);
	}

	private bool conversation_caravan_build_on_condition()
	{
		int num;
		if (Hero.OneToOneConversationHero != null)
		{
			if (!Hero.OneToOneConversationHero.IsMerchant)
			{
				num = (Hero.OneToOneConversationHero.IsArtisan ? 1 : 0);
				if (num == 0)
				{
					goto IL_004f;
				}
			}
			else
			{
				num = 1;
			}
			if (ShouldCreateConvoy())
			{
				MBTextManager.SetTextVariable("CARAVAN_BUY_INTENT_TEXT", "{=7l4I06Hi}I wish to form a trade convoy in this town.");
				return (byte)num != 0;
			}
			MBTextManager.SetTextVariable("CARAVAN_BUY_INTENT_TEXT", "{=tuz8ZNT6}I wish to form a caravan in this town.");
		}
		else
		{
			num = 0;
		}
		goto IL_004f;
		IL_004f:
		return (byte)num != 0;
	}

	private bool conversation_caravan_build_clickable_condition(out TextObject explanation)
	{
		if (Campaign.Current.IsMainHeroDisguised)
		{
			explanation = new TextObject("{=jcEoUPCB}You are in disguise.");
			return false;
		}
		explanation = null;
		return true;
	}

	private bool conversation_magistrate_form_a_caravan_cost_on_condition()
	{
		if (ShouldCreateConvoy())
		{
			MBTextManager.SetTextVariable("CARAVAN_FORMING_INFO_1", "{=OvtzH0b3}Well.. There are many goods around the town that can bring good money if you trade them. A trade convoy you formed will do this for you. You need to pay at least {AMOUNT}{GOLD_ICON} to hire guards to form a trade convoy and you need one companion to lead the convoy guards.");
		}
		else
		{
			MBTextManager.SetTextVariable("CARAVAN_FORMING_INFO_1", "{=cZptYTYd}Well.. There are many goods around the town that can bring good money if you trade them. A caravan you formed will do this for you. You need to pay at least {AMOUNT}{GOLD_ICON} to hire caravan guards to form a caravan and you need one companion to lead the caravan guards.");
		}
		MBTextManager.SetTextVariable("AMOUNT", GetSmallCaravanGoldCost());
		return true;
	}

	private bool conversation_magistrate_form_caravan_companion_condition()
	{
		return FindSuitableCompanionsToLeadCaravan().Count == 0;
	}

	private bool conversation_magistrate_form_caravan_gold_condition()
	{
		if (FindSuitableCompanionsToLeadCaravan().Count > 0)
		{
			return Hero.MainHero.Gold < GetSmallCaravanGoldCost();
		}
		return false;
	}

	private bool conversation_magistrate_form_a_small_caravan_accept_on_condition()
	{
		if (ShouldCreateConvoy())
		{
			MBTextManager.SetTextVariable("FORM_CARAVAN_ACCEPT", new TextObject("{=JNZwJaJ9}I accept these conditions and I am ready to pay {AMOUNT}{GOLD_ICON} to create a trade convoy."));
			MBTextManager.SetTextVariable("LARGE_CARAVAN_OFFER", new TextObject("{=V8bxlnSl}Is there a way to form a trade convoy that includes better troops?"));
		}
		else
		{
			MBTextManager.SetTextVariable("FORM_CARAVAN_ACCEPT", new TextObject("{=zOp48Fsg}I accept these conditions and I am ready to pay {AMOUNT}{GOLD_ICON} to create a caravan."));
			MBTextManager.SetTextVariable("LARGE_CARAVAN_OFFER", new TextObject("{=4mhOs9Fb}Is there a way to form a caravan that includes better troops?"));
		}
		MBTextManager.SetTextVariable("AMOUNT", GetSmallCaravanGoldCost());
		if (FindSuitableCompanionsToLeadCaravan().Count > 0)
		{
			return Hero.MainHero.Gold >= GetSmallCaravanGoldCost();
		}
		return false;
	}

	private bool conversation_magistrate_form_a_large_caravan_accept_on_condition()
	{
		if (ShouldCreateConvoy())
		{
			MBTextManager.SetTextVariable("LARGE_CARAVAN_OFFER", new TextObject("{=V8bxlnSl}Is there a way to form a trade convoy that includes better troops?"));
		}
		else
		{
			MBTextManager.SetTextVariable("LARGE_CARAVAN_OFFER", new TextObject("{=4mhOs9Fb}Is there a way to form a caravan that includes better troops?"));
		}
		return conversation_magistrate_form_a_small_caravan_accept_on_condition();
	}

	private void conversation_magistrate_form_a_small_caravan_accept_on_consequence()
	{
		_selectedCaravanType = 0;
		conversation_magistrate_form_a_caravan_accepted_on_consequence();
	}

	private void conversation_magistrate_form_a_caravan_accepted_on_consequence()
	{
		ConversationSentence.SetObjectsToRepeatOver(FindSuitableCompanionsToLeadCaravan());
	}

	private bool conversation_magistrate_form_a_caravan_reject_on_condition()
	{
		return Hero.MainHero.Gold >= GetSmallCaravanGoldCost();
	}

	private bool conversation_magistrate_form_a_big_caravan_offer_condition()
	{
		MBTextManager.SetTextVariable("AMOUNT", GetLargeCaravanGoldCost());
		return true;
	}

	private bool conversation_magistrate_form_a_big_caravan_accept_on_condition()
	{
		TextObject empty = TextObject.GetEmpty();
		empty = ((!ShouldCreateConvoy()) ? new TextObject("{=AuMLELpp}Okay then lets go with better troops, I am ready to pay {AMOUNT}{GOLD_ICON} to create a caravan.") : new TextObject("{=XxQzR39f}Okay then lets go with better troops, I am ready to pay {AMOUNT}{GOLD_ICON} to create a trade convoy."));
		MBTextManager.SetTextVariable("AMOUNT", GetLargeCaravanGoldCost());
		MBTextManager.SetTextVariable("CREATE_LARGE_CARAVAN_TEXT", empty);
		if (FindSuitableCompanionsToLeadCaravan().Count > 0)
		{
			return Hero.MainHero.Gold >= GetLargeCaravanGoldCost();
		}
		return false;
	}

	private bool conversation_magistrate_form_a_big_caravan_gold_condition()
	{
		return Hero.MainHero.Gold < GetLargeCaravanGoldCost();
	}

	private void conversation_magistrate_form_a_big_caravan_accept_on_consequence()
	{
		_selectedCaravanType = 1;
		conversation_magistrate_form_a_caravan_accepted_on_consequence();
	}

	private bool conversation_magistrate_form_a_big_caravan_reject_on_condition()
	{
		return Hero.MainHero.Gold >= GetLargeCaravanGoldCost();
	}

	private bool conversation_magistrate_form_a_caravan_accepted_choose_leader_on_condition()
	{
		if (ShouldCreateConvoy())
		{
			MBTextManager.SetTextVariable("CARAVAN_LEADER_CHOOSE_TEXT", new TextObject("{=Ww7vJSb9}Whom do you want to lead the convoy?"));
		}
		else
		{
			MBTextManager.SetTextVariable("CARAVAN_LEADER_CHOOSE_TEXT", new TextObject("{=aeCYFe1g}Whom do you want to lead the caravan?"));
		}
		return true;
	}

	private bool conversation_magistrate_form_a_caravan_final_conversation_on_condition()
	{
		if (ShouldCreateConvoy())
		{
			MBTextManager.SetTextVariable("CARAVAN_NOTABLE_FINAL_TALK", new TextObject("{=2WFPZrFf}Ok then. I will call my men to help you form a trade convoy. I hope it brings you a good profit."));
		}
		else
		{
			MBTextManager.SetTextVariable("CARAVAN_NOTABLE_FINAL_TALK", new TextObject("{=Z2Lq2QLq}Ok then. I will call my men to help you form a caravan. I hope it brings you a good profit."));
		}
		return true;
	}

	private bool conversation_magistrate_form_a_caravan_accepted_leader_is_chosen_on_condition()
	{
		if (ConversationSentence.CurrentProcessedRepeatObject is CharacterObject character)
		{
			StringHelpers.SetRepeatableCharacterProperties("HERO", character);
			return true;
		}
		return false;
	}

	private void conversation_magistrate_form_a_caravan_accept_on_consequence()
	{
		CharacterObject characterObject = ConversationSentence.SelectedRepeatObject as CharacterObject;
		FadeOutSelectedCaravanCompanionInMission(characterObject);
		LeaveSettlementAction.ApplyForCharacterOnly(characterObject.HeroObject);
		bool flag = _selectedCaravanType == 1;
		PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(Settlement.CurrentSettlement.Culture, flag, !ShouldCreateConvoy());
		CaravanPartyComponent.CreateCaravanParty(Hero.MainHero, Settlement.CurrentSettlement, randomCaravanTemplate, isInitialSpawn: false, characterObject.HeroObject, null, flag);
		GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, (!flag) ? GetSmallCaravanGoldCost() : GetLargeCaravanGoldCost());
		TextObject textObject = ((!ShouldCreateConvoy()) ? new TextObject("{=RmtTsqcx}A new caravan is created for {HERO.NAME}.") : new TextObject("{=c7VOPmSb}A new trade convoy is created for {HERO.NAME}."));
		StringHelpers.SetCharacterProperties("HERO", Hero.MainHero.CharacterObject, textObject);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
	}

	private void FadeOutSelectedCaravanCompanionInMission(CharacterObject caravanLeader)
	{
		CampaignMission.Current?.FadeOutCharacter(caravanLeader);
	}

	private List<CharacterObject> FindSuitableCompanionsToLeadCaravan()
	{
		List<CharacterObject> list = new List<CharacterObject>();
		foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			Hero heroObject = item.Character.HeroObject;
			if (heroObject != null && heroObject != Hero.MainHero && heroObject.Clan == Clan.PlayerClan && heroObject.GovernorOf == null && heroObject.CanLeadParty())
			{
				list.Add(item.Character);
			}
		}
		return list;
	}

	private bool ShouldCreateConvoy()
	{
		if (Settlement.CurrentSettlement == null)
		{
			Debug.FailedAssert("Current settlement is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CaravanConversationsCampaignBehavior.cs", "ShouldCreateConvoy", 302);
			return false;
		}
		return Settlement.CurrentSettlement.HasPort;
	}

	private int GetLargeCaravanGoldCost()
	{
		if (!ShouldCreateConvoy())
		{
			return Campaign.Current.Models.CaravanModel.GetCaravanFormingCost(eliteCaravan: true, navalCaravan: false);
		}
		return Campaign.Current.Models.CaravanModel.GetCaravanFormingCost(eliteCaravan: true, navalCaravan: true);
	}

	private int GetSmallCaravanGoldCost()
	{
		if (!ShouldCreateConvoy())
		{
			return Campaign.Current.Models.CaravanModel.GetCaravanFormingCost(eliteCaravan: false, navalCaravan: false);
		}
		return Campaign.Current.Models.CaravanModel.GetCaravanFormingCost(eliteCaravan: false, navalCaravan: true);
	}
}
