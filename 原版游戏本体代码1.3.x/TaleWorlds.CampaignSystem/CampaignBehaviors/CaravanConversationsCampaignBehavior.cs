using System;
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

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003D8 RID: 984
	public class CaravanConversationsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x06003A7E RID: 14974 RVA: 0x000F1FC4 File Offset: 0x000F01C4
		public override void RegisterEvents()
		{
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x06003A7F RID: 14975 RVA: 0x000F1FDD File Offset: 0x000F01DD
		public override void SyncData(IDataStore dataStore)
		{
		}

		// Token: 0x06003A80 RID: 14976 RVA: 0x000F1FDF File Offset: 0x000F01DF
		private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x06003A81 RID: 14977 RVA: 0x000F1FE8 File Offset: 0x000F01E8
		protected void AddDialogs(CampaignGameStarter starter)
		{
			starter.AddPlayerLine("caravan_create_conversation_1", "hero_main_options", "magistrate_form_a_caravan_cost", "{=!}{CARAVAN_BUY_INTENT_TEXT}", new ConversationSentence.OnConditionDelegate(this.conversation_caravan_build_on_condition), null, 100, new ConversationSentence.OnClickableConditionDelegate(this.conversation_caravan_build_clickable_condition), null);
			starter.AddDialogLine("caravan_create_conversation_2", "magistrate_form_a_caravan_cost", "magistrate_form_a_caravan_player_answer", "{=!}{CARAVAN_FORMING_INFO_1}", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_caravan_cost_on_condition), null, 100, null);
			starter.AddPlayerLine("caravan_create_conversation_3", "magistrate_form_a_caravan_player_answer", "lord_pretalk", "{=otVPaR6T}Actually I do not have a free companion right now.", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_caravan_companion_condition), null, 100, null, null);
			starter.AddPlayerLine("caravan_create_conversation_4", "magistrate_form_a_caravan_player_answer", "lord_pretalk", "{=w6WFuDn0}I am sorry, I don't have that much money.", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_caravan_gold_condition), null, 100, null, null);
			starter.AddPlayerLine("caravan_create_conversation_5", "magistrate_form_a_caravan_player_answer", "magistrate_form_a_caravan_accepted", "{=!}{FORM_CARAVAN_ACCEPT}", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_small_caravan_accept_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_magistrate_form_a_small_caravan_accept_on_consequence), 100, null, null);
			starter.AddPlayerLine("caravan_create_conversation_6", "magistrate_form_a_caravan_player_answer", "magistrate_form_a_caravan_big", "{=!}{LARGE_CARAVAN_OFFER}", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_large_caravan_accept_on_condition), null, 100, null, null);
			starter.AddPlayerLine("caravan_create_conversation_10", "magistrate_form_a_caravan_player_answer", "lord_pretalk", "{=2mJjDTAZ}That sounds expensive.", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_caravan_reject_on_condition), null, 100, null, null);
			starter.AddDialogLine("caravan_create_conversation_7", "magistrate_form_a_caravan_big", "magistrate_form_a_caravan_big_player_answer", "{=DaBzJkIz}I can increase quality of troops, but cost will proportionally increase, too. It will cost {AMOUNT}{GOLD_ICON}.", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_big_caravan_offer_condition), null, 100, null);
			starter.AddPlayerLine("caravan_create_conversation_8", "magistrate_form_a_caravan_big_player_answer", "magistrate_form_a_caravan_accepted", "{=!}{CREATE_LARGE_CARAVAN_TEXT}", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_big_caravan_accept_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_magistrate_form_a_big_caravan_accept_on_consequence), 100, null, null);
			starter.AddPlayerLine("caravan_create_conversation_9", "magistrate_form_a_caravan_big_player_answer", "lord_pretalk", "{=w6WFuDn0}I am sorry, I don't have that much money.", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_big_caravan_gold_condition), null, 100, null, null);
			starter.AddPlayerLine("caravan_create_conversation_10_2", "magistrate_form_a_caravan_big_player_answer", "lord_pretalk", "{=2mJjDTAZ}That sounds expensive.", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_big_caravan_reject_on_condition), null, 100, null, null);
			starter.AddDialogLine("caravan_create_conversation_11", "magistrate_form_a_caravan_accepted", "magistrate_form_a_caravan_accepted_choose_leader", "{=!}{CARAVAN_LEADER_CHOOSE_TEXT}", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_caravan_accepted_choose_leader_on_condition), null, 100, null);
			starter.AddRepeatablePlayerLine("caravan_create_conversation_12", "magistrate_form_a_caravan_accepted_choose_leader", "magistrate_form_a_caravan_accepted_leader_is_chosen", "{=!}{HERO.NAME}", "{=UNFE1BeG}I am thinking of a different person", "magistrate_form_a_caravan_accepted", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_caravan_accepted_leader_is_chosen_on_condition), new ConversationSentence.OnConsequenceDelegate(this.conversation_magistrate_form_a_caravan_accept_on_consequence), 100, null);
			starter.AddPlayerLine("caravan_create_conversation_13", "magistrate_form_a_caravan_accepted_choose_leader", "lord_pretalk", "{=PznWhAdU}Actually, never mind.", null, null, 100, null, null);
			starter.AddDialogLine("caravan_create_conversation_14", "magistrate_form_a_caravan_accepted_leader_is_chosen", "close_window", "{=!}{CARAVAN_NOTABLE_FINAL_TALK}", new ConversationSentence.OnConditionDelegate(this.conversation_magistrate_form_a_caravan_final_conversation_on_condition), null, 100, null);
		}

		// Token: 0x06003A82 RID: 14978 RVA: 0x000F22B0 File Offset: 0x000F04B0
		private bool conversation_caravan_build_on_condition()
		{
			bool flag = Hero.OneToOneConversationHero != null && (Hero.OneToOneConversationHero.IsMerchant || Hero.OneToOneConversationHero.IsArtisan);
			if (flag)
			{
				if (this.ShouldCreateConvoy())
				{
					MBTextManager.SetTextVariable("CARAVAN_BUY_INTENT_TEXT", "{=7l4I06Hi}I wish to form a trade convoy in this town.", false);
					return flag;
				}
				MBTextManager.SetTextVariable("CARAVAN_BUY_INTENT_TEXT", "{=tuz8ZNT6}I wish to form a caravan in this town.", false);
			}
			return flag;
		}

		// Token: 0x06003A83 RID: 14979 RVA: 0x000F230C File Offset: 0x000F050C
		private bool conversation_caravan_build_clickable_condition(out TextObject explanation)
		{
			if (Campaign.Current.IsMainHeroDisguised)
			{
				explanation = new TextObject("{=jcEoUPCB}You are in disguise.", null);
				return false;
			}
			explanation = null;
			return true;
		}

		// Token: 0x06003A84 RID: 14980 RVA: 0x000F232D File Offset: 0x000F052D
		private bool conversation_magistrate_form_a_caravan_cost_on_condition()
		{
			if (this.ShouldCreateConvoy())
			{
				MBTextManager.SetTextVariable("CARAVAN_FORMING_INFO_1", "{=OvtzH0b3}Well.. There are many goods around the town that can bring good money if you trade them. A trade convoy you formed will do this for you. You need to pay at least {AMOUNT}{GOLD_ICON} to hire guards to form a trade convoy and you need one companion to lead the convoy guards.", false);
			}
			else
			{
				MBTextManager.SetTextVariable("CARAVAN_FORMING_INFO_1", "{=cZptYTYd}Well.. There are many goods around the town that can bring good money if you trade them. A caravan you formed will do this for you. You need to pay at least {AMOUNT}{GOLD_ICON} to hire caravan guards to form a caravan and you need one companion to lead the caravan guards.", false);
			}
			MBTextManager.SetTextVariable("AMOUNT", this.GetSmallCaravanGoldCost());
			return true;
		}

		// Token: 0x06003A85 RID: 14981 RVA: 0x000F236A File Offset: 0x000F056A
		private bool conversation_magistrate_form_caravan_companion_condition()
		{
			return this.FindSuitableCompanionsToLeadCaravan().Count == 0;
		}

		// Token: 0x06003A86 RID: 14982 RVA: 0x000F237A File Offset: 0x000F057A
		private bool conversation_magistrate_form_caravan_gold_condition()
		{
			return this.FindSuitableCompanionsToLeadCaravan().Count > 0 && Hero.MainHero.Gold < this.GetSmallCaravanGoldCost();
		}

		// Token: 0x06003A87 RID: 14983 RVA: 0x000F23A0 File Offset: 0x000F05A0
		private bool conversation_magistrate_form_a_small_caravan_accept_on_condition()
		{
			if (this.ShouldCreateConvoy())
			{
				MBTextManager.SetTextVariable("FORM_CARAVAN_ACCEPT", new TextObject("{=JNZwJaJ9}I accept these conditions and I am ready to pay {AMOUNT}{GOLD_ICON} to create a trade convoy.", null), false);
				MBTextManager.SetTextVariable("LARGE_CARAVAN_OFFER", new TextObject("{=V8bxlnSl}Is there a way to form a trade convoy that includes better troops?", null), false);
			}
			else
			{
				MBTextManager.SetTextVariable("FORM_CARAVAN_ACCEPT", new TextObject("{=zOp48Fsg}I accept these conditions and I am ready to pay {AMOUNT}{GOLD_ICON} to create a caravan.", null), false);
				MBTextManager.SetTextVariable("LARGE_CARAVAN_OFFER", new TextObject("{=4mhOs9Fb}Is there a way to form a caravan that includes better troops?", null), false);
			}
			MBTextManager.SetTextVariable("AMOUNT", this.GetSmallCaravanGoldCost());
			return this.FindSuitableCompanionsToLeadCaravan().Count > 0 && Hero.MainHero.Gold >= this.GetSmallCaravanGoldCost();
		}

		// Token: 0x06003A88 RID: 14984 RVA: 0x000F2444 File Offset: 0x000F0644
		private bool conversation_magistrate_form_a_large_caravan_accept_on_condition()
		{
			if (this.ShouldCreateConvoy())
			{
				MBTextManager.SetTextVariable("LARGE_CARAVAN_OFFER", new TextObject("{=V8bxlnSl}Is there a way to form a trade convoy that includes better troops?", null), false);
			}
			else
			{
				MBTextManager.SetTextVariable("LARGE_CARAVAN_OFFER", new TextObject("{=4mhOs9Fb}Is there a way to form a caravan that includes better troops?", null), false);
			}
			return this.conversation_magistrate_form_a_small_caravan_accept_on_condition();
		}

		// Token: 0x06003A89 RID: 14985 RVA: 0x000F2482 File Offset: 0x000F0682
		private void conversation_magistrate_form_a_small_caravan_accept_on_consequence()
		{
			this._selectedCaravanType = 0;
			this.conversation_magistrate_form_a_caravan_accepted_on_consequence();
		}

		// Token: 0x06003A8A RID: 14986 RVA: 0x000F2491 File Offset: 0x000F0691
		private void conversation_magistrate_form_a_caravan_accepted_on_consequence()
		{
			ConversationSentence.SetObjectsToRepeatOver(this.FindSuitableCompanionsToLeadCaravan(), 5);
		}

		// Token: 0x06003A8B RID: 14987 RVA: 0x000F249F File Offset: 0x000F069F
		private bool conversation_magistrate_form_a_caravan_reject_on_condition()
		{
			return Hero.MainHero.Gold >= this.GetSmallCaravanGoldCost();
		}

		// Token: 0x06003A8C RID: 14988 RVA: 0x000F24B6 File Offset: 0x000F06B6
		private bool conversation_magistrate_form_a_big_caravan_offer_condition()
		{
			MBTextManager.SetTextVariable("AMOUNT", this.GetLargeCaravanGoldCost());
			return true;
		}

		// Token: 0x06003A8D RID: 14989 RVA: 0x000F24CC File Offset: 0x000F06CC
		private bool conversation_magistrate_form_a_big_caravan_accept_on_condition()
		{
			TextObject text = TextObject.GetEmpty();
			if (this.ShouldCreateConvoy())
			{
				text = new TextObject("{=XxQzR39f}Okay then lets go with better troops, I am ready to pay {AMOUNT}{GOLD_ICON} to create a trade convoy.", null);
			}
			else
			{
				text = new TextObject("{=AuMLELpp}Okay then lets go with better troops, I am ready to pay {AMOUNT}{GOLD_ICON} to create a caravan.", null);
			}
			MBTextManager.SetTextVariable("AMOUNT", this.GetLargeCaravanGoldCost());
			MBTextManager.SetTextVariable("CREATE_LARGE_CARAVAN_TEXT", text, false);
			return this.FindSuitableCompanionsToLeadCaravan().Count > 0 && Hero.MainHero.Gold >= this.GetLargeCaravanGoldCost();
		}

		// Token: 0x06003A8E RID: 14990 RVA: 0x000F2542 File Offset: 0x000F0742
		private bool conversation_magistrate_form_a_big_caravan_gold_condition()
		{
			return Hero.MainHero.Gold < this.GetLargeCaravanGoldCost();
		}

		// Token: 0x06003A8F RID: 14991 RVA: 0x000F2556 File Offset: 0x000F0756
		private void conversation_magistrate_form_a_big_caravan_accept_on_consequence()
		{
			this._selectedCaravanType = 1;
			this.conversation_magistrate_form_a_caravan_accepted_on_consequence();
		}

		// Token: 0x06003A90 RID: 14992 RVA: 0x000F2565 File Offset: 0x000F0765
		private bool conversation_magistrate_form_a_big_caravan_reject_on_condition()
		{
			return Hero.MainHero.Gold >= this.GetLargeCaravanGoldCost();
		}

		// Token: 0x06003A91 RID: 14993 RVA: 0x000F257C File Offset: 0x000F077C
		private bool conversation_magistrate_form_a_caravan_accepted_choose_leader_on_condition()
		{
			if (this.ShouldCreateConvoy())
			{
				MBTextManager.SetTextVariable("CARAVAN_LEADER_CHOOSE_TEXT", new TextObject("{=Ww7vJSb9}Whom do you want to lead the convoy?", null), false);
			}
			else
			{
				MBTextManager.SetTextVariable("CARAVAN_LEADER_CHOOSE_TEXT", new TextObject("{=aeCYFe1g}Whom do you want to lead the caravan?", null), false);
			}
			return true;
		}

		// Token: 0x06003A92 RID: 14994 RVA: 0x000F25B5 File Offset: 0x000F07B5
		private bool conversation_magistrate_form_a_caravan_final_conversation_on_condition()
		{
			if (this.ShouldCreateConvoy())
			{
				MBTextManager.SetTextVariable("CARAVAN_NOTABLE_FINAL_TALK", new TextObject("{=2WFPZrFf}Ok then. I will call my men to help you form a trade convoy. I hope it brings you a good profit.", null), false);
			}
			else
			{
				MBTextManager.SetTextVariable("CARAVAN_NOTABLE_FINAL_TALK", new TextObject("{=Z2Lq2QLq}Ok then. I will call my men to help you form a caravan. I hope it brings you a good profit.", null), false);
			}
			return true;
		}

		// Token: 0x06003A93 RID: 14995 RVA: 0x000F25F0 File Offset: 0x000F07F0
		private bool conversation_magistrate_form_a_caravan_accepted_leader_is_chosen_on_condition()
		{
			CharacterObject characterObject = ConversationSentence.CurrentProcessedRepeatObject as CharacterObject;
			if (characterObject != null)
			{
				StringHelpers.SetRepeatableCharacterProperties("HERO", characterObject, false);
				return true;
			}
			return false;
		}

		// Token: 0x06003A94 RID: 14996 RVA: 0x000F261C File Offset: 0x000F081C
		private void conversation_magistrate_form_a_caravan_accept_on_consequence()
		{
			CharacterObject characterObject = ConversationSentence.SelectedRepeatObject as CharacterObject;
			this.FadeOutSelectedCaravanCompanionInMission(characterObject);
			LeaveSettlementAction.ApplyForCharacterOnly(characterObject.HeroObject);
			bool flag = this._selectedCaravanType == 1;
			PartyTemplateObject randomCaravanTemplate = CaravanHelper.GetRandomCaravanTemplate(Settlement.CurrentSettlement.Culture, flag, !this.ShouldCreateConvoy());
			CaravanPartyComponent.CreateCaravanParty(Hero.MainHero, Settlement.CurrentSettlement, randomCaravanTemplate, false, characterObject.HeroObject, null, flag);
			GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, (!flag) ? this.GetSmallCaravanGoldCost() : this.GetLargeCaravanGoldCost(), false);
			TextObject textObject;
			if (this.ShouldCreateConvoy())
			{
				textObject = new TextObject("{=c7VOPmSb}A new trade convoy is created for {HERO.NAME}.", null);
			}
			else
			{
				textObject = new TextObject("{=RmtTsqcx}A new caravan is created for {HERO.NAME}.", null);
			}
			StringHelpers.SetCharacterProperties("HERO", Hero.MainHero.CharacterObject, textObject, false);
			InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
		}

		// Token: 0x06003A95 RID: 14997 RVA: 0x000F26EE File Offset: 0x000F08EE
		private void FadeOutSelectedCaravanCompanionInMission(CharacterObject caravanLeader)
		{
			ICampaignMission campaignMission = CampaignMission.Current;
			if (campaignMission == null)
			{
				return;
			}
			campaignMission.FadeOutCharacter(caravanLeader);
		}

		// Token: 0x06003A96 RID: 14998 RVA: 0x000F2700 File Offset: 0x000F0900
		private List<CharacterObject> FindSuitableCompanionsToLeadCaravan()
		{
			List<CharacterObject> list = new List<CharacterObject>();
			foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				Hero heroObject = troopRosterElement.Character.HeroObject;
				if (heroObject != null && heroObject != Hero.MainHero && heroObject.Clan == Clan.PlayerClan && heroObject.GovernorOf == null && heroObject.CanLeadParty())
				{
					list.Add(troopRosterElement.Character);
				}
			}
			return list;
		}

		// Token: 0x06003A97 RID: 14999 RVA: 0x000F279C File Offset: 0x000F099C
		private bool ShouldCreateConvoy()
		{
			if (Settlement.CurrentSettlement == null)
			{
				Debug.FailedAssert("Current settlement is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\CampaignBehaviors\\CaravanConversationsCampaignBehavior.cs", "ShouldCreateConvoy", 302);
				return false;
			}
			return Settlement.CurrentSettlement.HasPort;
		}

		// Token: 0x06003A98 RID: 15000 RVA: 0x000F27CA File Offset: 0x000F09CA
		private int GetLargeCaravanGoldCost()
		{
			if (!this.ShouldCreateConvoy())
			{
				return Campaign.Current.Models.CaravanModel.GetCaravanFormingCost(true, false);
			}
			return Campaign.Current.Models.CaravanModel.GetCaravanFormingCost(true, true);
		}

		// Token: 0x06003A99 RID: 15001 RVA: 0x000F2801 File Offset: 0x000F0A01
		private int GetSmallCaravanGoldCost()
		{
			if (!this.ShouldCreateConvoy())
			{
				return Campaign.Current.Models.CaravanModel.GetCaravanFormingCost(false, false);
			}
			return Campaign.Current.Models.CaravanModel.GetCaravanFormingCost(false, true);
		}

		// Token: 0x04001214 RID: 4628
		private int _selectedCaravanType;
	}
}
