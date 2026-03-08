using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003DF RID: 991
	public class CompanionRolesCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x17000E08 RID: 3592
		// (get) Token: 0x06003C73 RID: 15475 RVA: 0x00103AED File Offset: 0x00101CED
		private static CompanionRolesCampaignBehavior CurrentBehavior
		{
			get
			{
				return Campaign.Current.GetCampaignBehavior<CompanionRolesCampaignBehavior>();
			}
		}

		// Token: 0x06003C74 RID: 15476 RVA: 0x00103AFC File Offset: 0x00101CFC
		public override void RegisterEvents()
		{
			CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, new Action<Hero, RemoveCompanionAction.RemoveCompanionDetail>(this.OnCompanionRemoved));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
			CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, new Action<Hero, Hero, int, bool, ChangeRelationAction.ChangeRelationDetail, Hero, Hero>(this.OnHeroRelationChanged));
		}

		// Token: 0x06003C75 RID: 15477 RVA: 0x00103B4E File Offset: 0x00101D4E
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<List<int>>("_alreadyUsedIconIdsForNewClans", ref this._alreadyUsedIconIdsForNewClans);
		}

		// Token: 0x06003C76 RID: 15478 RVA: 0x00103B64 File Offset: 0x00101D64
		private void OnHeroRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
		{
			if (((effectiveHero == Hero.MainHero && effectiveHeroGainedRelationWith.IsPlayerCompanion) || (effectiveHero.IsPlayerCompanion && effectiveHeroGainedRelationWith == Hero.MainHero)) && relationChange < 0 && effectiveHero.GetRelation(effectiveHeroGainedRelationWith) < -10)
			{
				KillCharacterAction.ApplyByRemove(effectiveHero.IsPlayerCompanion ? effectiveHero : effectiveHeroGainedRelationWith, false, true);
			}
		}

		// Token: 0x06003C77 RID: 15479 RVA: 0x00103BB3 File Offset: 0x00101DB3
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x06003C78 RID: 15480 RVA: 0x00103BBC File Offset: 0x00101DBC
		protected void AddDialogs(CampaignGameStarter campaignGameStarter)
		{
			campaignGameStarter.AddPlayerLine("companion_rejoin_after_emprisonment_role", "hero_main_options", "companion_rejoin", "{=!}{COMPANION_REJOIN_LINE}", new ConversationSentence.OnConditionDelegate(this.companion_rejoin_after_emprisonment_role_on_condition), delegate
			{
				Campaign.Current.ConversationManager.ConversationEnd += this.companion_rejoin_after_emprisonment_role_on_consequence;
			}, 100, null, null);
			campaignGameStarter.AddDialogLine("companion_rejoin", "companion_rejoin", "close_window", "{=ppi6eVos}As you wish.", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("companion_start_role", "hero_main_options", "companion_role_pretalk", "{=d4t6oUCn}About your position in the clan...", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_role_discuss_on_condition), null, 100, null, null);
			campaignGameStarter.AddDialogLine("companion_pretalk", "companion_role_pretalk", "companion_role", "{=!}{COMPANION_ROLE}", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_has_role_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("companion_talk_fire", "companion_role", "companion_fire", "{=pRsCnGoo}I no longer have need of your services.", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_fire_condition), null, 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_talk_fire_2", "companion_role", "companion_assign_new_role", "{=2g18dlwo}I would like to assign you a new role.", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_assign_role_on_condition), null, 100, null, null);
			campaignGameStarter.AddDialogLine("companion_assign_new_role", "companion_assign_new_role", "companion_roles", "{=5ajobQiL}What role do you have in mind?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("companion_talk_fire_3", "companion_role", "companion_okay", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_becomes_engineer", "companion_roles", "companion_okay", "{=E91oU7oi}I no longer need you as Engineer.", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_fire_engineer_on_condition), new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_delete_party_role_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_becomes_surgeon", "companion_roles", "companion_okay", "{=Dga7sQOu}I no longer need you as Surgeon.", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_fire_surgeon_on_condition), new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_delete_party_role_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_becomes_quartermaster", "companion_roles", "companion_okay", "{=GjpJN2xE}I no longer need you as Quartermaster.", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_fire_quartermaster_on_condition), new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_delete_party_role_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_becomes_scout", "companion_roles", "companion_okay", "{=EUQnsZFb}I no longer need you as Scout.", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_fire_scout_on_condition), new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_delete_party_role_consequence), 100, null, null);
			campaignGameStarter.AddDialogLine("companion_role_response", "companion_okay", "hero_main_options", "{=dzXaXKaC}Very well.", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("companion_becomes_engineer_2", "companion_roles", "give_companion_roles", "{=UuFPafDj}Engineer {CURRENTLY_HELD_ENGINEER}", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_becomes_engineer_on_condition), new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_becomes_engineer_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_becomes_surgeon_2", "companion_roles", "give_companion_roles", "{=6xZ8U3Yz}Surgeon {CURRENTLY_HELD_SURGEON}", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_becomes_surgeon_on_condition), new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_becomes_surgeon_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_becomes_quartermaster_2", "companion_roles", "give_companion_roles", "{=B0VLXHHz}Quartermaster {CURRENTLY_HELD_QUARTERMASTER}", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_becomes_quartermaster_on_condition), new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_becomes_quartermaster_on_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_becomes_scout_2", "companion_roles", "give_companion_roles", "{=3aziL3Gs}Scout {CURRENTLY_HELD_SCOUT}", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_becomes_scout_on_condition), new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_becomes_scout_on_consequence), 100, null, null);
			campaignGameStarter.AddDialogLine("companion_role_response_2", "give_companion_roles", "hero_main_options", "{=5hhxQBTj}I would be honored.", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("companion_talk_return", "companion_roles", "companion_okay", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("companion_start_mission", "hero_main_options", "companion_mission_pretalk", "{=4ry48jbg}I have a mission for you...", () => HeroHelper.IsCompanionInPlayerParty(Hero.OneToOneConversationHero), null, 100, null);
			campaignGameStarter.AddDialogLine("companion_pretalk_2", "companion_mission_pretalk", "companion_mission", "{=7EoBCTX0}What do you want me to do?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("companion_mission_gather_troops", "companion_mission", "companion_recruit_troops", "{=MDik3Kfn}I want you to recruit some troops.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_mission_forage", "companion_mission", "companion_forage", "{=kAbebv72}I want you to go forage some food.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_mission_patrol", "companion_mission", "companion_patrol", "{=OMaM6ihN}I want you to patrol the area.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_mission_cancel", "companion_mission", "hero_main_options", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("companion_forage_1", "companion_forage", "companion_forage_2", "{=o2g6Wi9K}As you wish. Will I take some troops with me?", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("companion_forage_2", "companion_forage_2", "companion_forage_troops", "{=lVbQCibL}Yes. Take these troops with you.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_forage_3", "companion_forage_2", "companion_forage_3", "{=3bOcF1Cw}I can't spare anyone now. You will need to go alone.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("companion_fire", "companion_fire", "companion_fire2", "{=bUzU50P8}What? Why? Did I do something wrong?[ib:closed]", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("companion_fire_age", "companion_fire2", "companion_fire3", "{=ywtuRAmP}Time has taken its toll on us all, friend. It's time that you retire.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_fire_no_fit", "companion_fire2", "companion_fire3", "{=1s3bHupn}You're not getting along with the rest of the company. It's better you go.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_fire_no_fit_2", "companion_fire2", "companion_fire3", "{=Q0xPr6CP}I cannot be sure of your loyalty any longer.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_fire_underperforming", "companion_fire2", "companion_fire3", "{=aCwCaWGC}Your skills are not what I need.", null, null, 100, null, null);
			campaignGameStarter.AddPlayerLine("companion_fire_cancel", "companion_fire2", "companion_fire_cancel", "{=8VlqJteC}I was just jesting. I need you more than ever. Now go back to your job.", null, new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_talk_done_on_consequence), 100, null, null);
			campaignGameStarter.AddDialogLine("companion_fire_cancel2", "companion_fire_cancel", "close_window", "{=vctta154}Well {PLAYER.NAME}, it is certainly good to see you still retain your sense of humor.[if:convo_nervous][ib:normal2]", null, null, 100, null);
			campaignGameStarter.AddDialogLine("companion_fire_farewell", "companion_fire3", "close_window", "{=!}{AGREE_TO_LEAVE}[ib:nervous2]", new ConversationSentence.OnConditionDelegate(this.companion_agrees_to_leave_on_condition), new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_fire_on_consequence), 100, null);
			campaignGameStarter.AddPlayerLine("turn_companion_to_lord_start", "hero_main_options", "turn_companion_to_lord_talk_answer", "{=B9uT9wa6}I wish to reward you for your services.", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.turn_companion_to_lord_on_condition), null, 100, null, null);
			campaignGameStarter.AddDialogLine("turn_companion_to_lord_start_answer_2", "turn_companion_to_lord_talk_answer", "companion_leading_caravan", "{=IkH0pVhC}I would be honored, my {?PLAYER.GENDER}lady{?}lord{\\?}. But I can't take on any new responsibilities while leading this caravan. If you wish to relieve me of my duties, we can discuss this further.", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_is_leading_caravan_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("turn_companion_to_lord_start_answer_player", "companion_leading_caravan", "lord_pretalk", "{=i7k0AXsO}I see. We will speak again when you are relieved from your duty.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("turn_companion_to_lord_start_answer", "turn_companion_to_lord_talk_answer", "turn_companion_to_lord_talk", "{=TXO1ihiZ}Thank you, my {?PLAYER.GENDER}lady{?}lord{\\?}. I have often thought about that. If I had a fief, with revenues, and perhaps a title to go with it, I could marry well and pass my wealth down to my heirs, and of course raise troops to help defend the realm.", null, null, 100, null);
			campaignGameStarter.AddPlayerLine("turn_companion_to_lord_has_fief", "turn_companion_to_lord_talk", "check_player_has_fief_to_grant", "{=KqazzTWV}Indeed. You have shed your blood for me, and you deserve a fief of your own..", null, new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.fief_grant_answer_consequence), 100, null, null);
			campaignGameStarter.AddDialogLine("turn_companion_to_lord_has_no_fief", "check_player_has_fief_to_grant", "player_has_no_fief_to_grant", "{=Wx5ysDp1}My {?PLAYER.GENDER}lady{?}lord{\\?}, as much as I appreciate the gesture, I am not sure that you have a suitable estate to grant me.", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.turn_companion_to_lord_no_fief_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("turn_companion_to_lord_has_no_fief_player_answer", "player_has_no_fief_to_grant", "player_has_no_fief_to_grant_answer", "{=6uUzWz46}I see. Maybe we will speak again when I have one.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("turn_companion_to_lord_has_no_fief_companion_answer", "player_has_no_fief_to_grant_answer", "hero_main_options", "{=PP3LzCKk}As you wish, my {?PLAYER.GENDER}lady{?}lord{\\?}.", null, null, 100, null);
			campaignGameStarter.AddDialogLine("turn_companion_to_lord_has_fief_answer", "check_player_has_fief_to_grant", "player_has_fief_list", "{=ArNB7aaL}Where exactly did you have in mind?[if:convo_happy]", null, null, 100, null);
			campaignGameStarter.AddRepeatablePlayerLine("turn_companion_to_lord_has_fief_list", "player_has_fief_list", "player_selected_fief_to_grant", "{=3rHeoq6r}{SETTLEMENT_NAME}.", "{=sxc2D6NJ}I am thinking of a different location.", "check_player_has_fief_to_grant", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.list_player_fief_on_condition), new ConversationSentence.OnConsequenceDelegate(this.list_player_fief_selected_on_consequence), 100, new ConversationSentence.OnClickableConditionDelegate(CompanionRolesCampaignBehavior.list_player_fief_clickable_condition));
			campaignGameStarter.AddPlayerLine("turn_companion_to_lord_has_fief_list_cancel", "player_has_fief_list", "turn_companion_to_lord_fief_conclude", "{=UEbesbKZ}Actually, I have changed my mind.", null, new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.list_player_fief_cancel_on_consequence), 100, null, null);
			campaignGameStarter.AddDialogLine("turn_companion_to_lord_fief_selected", "player_selected_fief_to_grant", "turn_companion_to_lord_fief_selected_answer", "{=Mt9abZzi}{SETTLEMENT_NAME}? This is a great honor, my {?PLAYER.GENDER}lady{?}lord{\\?}. I will protect it until the last drop of my blood.[ib:hip][if:convo_happy]", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.fief_selected_on_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("turn_companion_to_lord_fief_selected_confirm", "turn_companion_to_lord_fief_selected_answer", "turn_companion_to_lord_fief_selected_confirm_box", "{=TtlwXnVc}I am pleased to grant you the title of {CULTURE_SPECIFIC_TITLE} and the fiefdom of {SETTLEMENT_NAME}.. You richly deserve it.", null, null, 100, new ConversationSentence.OnClickableConditionDelegate(CompanionRolesCampaignBehavior.fief_selected_confirm_clickable_on_condition), null);
			campaignGameStarter.AddPlayerLine("turn_companion_to_lord_fief_selected_reject", "turn_companion_to_lord_fief_selected_answer", "turn_companion_to_lord_fief_conclude", "{=LDGMSQJJ}Very well. Let me think on this a bit longer", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("turn_companion_to_lord_fief_selected_confirm_box", "turn_companion_to_lord_fief_selected_confirm_box", "turn_companion_to_lord_fief_conclude", "{=LOiZfCEy}My {?PLAYER.GENDER}lady{?}lord{\\?}, it would be an honor if you were to choose the name of my noble house.", null, new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.turn_companion_to_lord_consequence), 100, null);
			campaignGameStarter.AddDialogLine("turn_companion_to_lord_done_answer_thanks", "turn_companion_to_lord_fief_conclude", "close_window", "{=dpYhBgAC}Thank you my {?PLAYER.GENDER}lady{?}lord{\\?}. I will always remember this grand gesture.[ib:hip][if:convo_happy]", new ConversationSentence.OnConditionDelegate(CompanionRolesCampaignBehavior.companion_thanks_on_condition), new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_talk_done_on_consequence), 100, null);
			campaignGameStarter.AddDialogLine("turn_companion_to_lord_done_answer_rejected", "turn_companion_to_lord_fief_conclude", "hero_main_options", "{=SVEptNxR}It's only normal that you have second thoughts. I will be right by your side if you change your mind, my {?PLAYER.GENDER}lady{?}lord{\\?}.[ib:hip][if:convo_nervous]", null, new ConversationSentence.OnConsequenceDelegate(CompanionRolesCampaignBehavior.companion_talk_done_on_consequence), 100, null);
			campaignGameStarter.AddDialogLine("rescue_companion_start", "start", "rescue_companion_option_acknowledgement", "{=FVOfzPot}{SALUTATION}... Thank you for freeing me.", new ConversationSentence.OnConditionDelegate(this.companion_rescue_start_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("rescue_companion_option_acknowledgement", "rescue_companion_option_acknowledgement", "rescue_companion_preoptions", "{=YyNywO6Z}Think nothing of it. I'm glad you're safe.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("rescue_companion_preoptions", "rescue_companion_preoptions", "rescue_companion_options", "{=kaVMFgBs}What now?", new ConversationSentence.OnConditionDelegate(this.companion_rescue_start_condition), null, 100, null);
			campaignGameStarter.AddPlayerLine("rescue_companion_option_1", "rescue_companion_options", "rescue_companion_join_party", "{=drIfaTa7}Rejoin the others and let's be off.", null, new ConversationSentence.OnConsequenceDelegate(this.companion_rescue_answer_options_join_party_consequence), 100, null, null);
			campaignGameStarter.AddPlayerLine("rescue_companion_option_2", "rescue_companion_options", "rescue_companion_lead_party", "{=Y6Z8qNW9}I'll need you to lead a party.", null, null, 100, new ConversationSentence.OnClickableConditionDelegate(this.lead_a_party_clickable_condition), null);
			campaignGameStarter.AddPlayerLine("rescue_companion_option_3", "rescue_companion_options", "rescue_companion_do_nothing", "{=dRKk0E1V}Unfortunately, I can't take you back right now.", null, null, 100, null, null);
			campaignGameStarter.AddDialogLine("rescue_companion_lead_party_answer", "rescue_companion_lead_party", "close_window", "{=Q9Ltufg5}Tell me who to command.", null, new ConversationSentence.OnConsequenceDelegate(this.companion_rescue_answer_options_lead_party_consequence), 100, null);
			campaignGameStarter.AddDialogLine("rescue_companion_join_party_answer", "rescue_companion_join_party", "close_window", "{=92mngWSd}All right. It's good to be back.", null, new ConversationSentence.OnConsequenceDelegate(this.end_rescue_companion), 100, null);
			campaignGameStarter.AddDialogLine("rescue_companion_do_nothing_answer", "rescue_companion_do_nothing", "close_window", "{=gT2O4YXc}I will go off on my own, then. I can stay busy. But I'll remember - I owe you one!", null, new ConversationSentence.OnConsequenceDelegate(this.end_rescue_companion), 100, null);
			campaignGameStarter.AddDialogLine("rescue_companion_lead_party_create_party_continue_0", "start", "party_screen_rescue_continue", "{=ppi6eVos}As you wish.", new ConversationSentence.OnConditionDelegate(this.party_screen_continue_conversation_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("rescue_companion_lead_party_create_party_continue_1", "party_screen_rescue_continue", "rescue_companion_options", "{=ttWBYlxS}So, what shall I do?", new ConversationSentence.OnConditionDelegate(this.party_screen_opened_but_party_is_not_created_after_rescue_condition), null, 100, null);
			campaignGameStarter.AddDialogLine("rescue_companion_lead_party_create_party_continue_2", "party_screen_rescue_continue", "close_window", "{=DiEKuVGF}We'll make ready to set out at once.", new ConversationSentence.OnConditionDelegate(this.party_screen_opened_and_party_is_created_after_rescue_condition), new ConversationSentence.OnConsequenceDelegate(this.end_rescue_companion), 100, null);
			campaignGameStarter.AddDialogLine("default_conversation_for_wrongly_created_heroes", "start", "close_window", "{=BaeqKlQ6}I am not allowed to talk with you.", null, null, 0, null);
		}

		// Token: 0x06003C79 RID: 15481 RVA: 0x0010467A File Offset: 0x0010287A
		private static bool companion_fire_condition()
		{
			return Hero.OneToOneConversationHero.IsPlayerCompanion && Settlement.CurrentSettlement == null && (Hero.OneToOneConversationHero.PartyBelongedTo == null || !Hero.OneToOneConversationHero.PartyBelongedTo.IsInRaftState);
		}

		// Token: 0x06003C7A RID: 15482 RVA: 0x001046B1 File Offset: 0x001028B1
		private static bool turn_companion_to_lord_no_fief_on_condition()
		{
			return !Hero.MainHero.Clan.Settlements.Any((Settlement x) => x.IsTown || x.IsCastle);
		}

		// Token: 0x06003C7B RID: 15483 RVA: 0x001046EC File Offset: 0x001028EC
		private static bool turn_companion_to_lord_on_condition()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null && oneToOneConversationHero.IsPlayerCompanion && Hero.MainHero.IsKingdomLeader)
			{
				MobileParty partyBelongedTo = oneToOneConversationHero.PartyBelongedTo;
				if (partyBelongedTo == null || !partyBelongedTo.IsCurrentlyAtSea)
				{
					CompanionRolesCampaignBehavior.CurrentBehavior._playerConfirmedTheAction = false;
					return true;
				}
			}
			return false;
		}

		// Token: 0x06003C7C RID: 15484 RVA: 0x0010473C File Offset: 0x0010293C
		private static bool companion_is_leading_caravan_condition()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			return oneToOneConversationHero != null && oneToOneConversationHero.IsPlayerCompanion && oneToOneConversationHero.PartyBelongedTo != null && oneToOneConversationHero.PartyBelongedTo.IsCaravan;
		}

		// Token: 0x06003C7D RID: 15485 RVA: 0x0010476F File Offset: 0x0010296F
		private static void fief_grant_answer_consequence()
		{
			ConversationSentence.SetObjectsToRepeatOver((from x in Hero.MainHero.Clan.Settlements
				where x.IsTown || x.IsCastle
				select x).ToList<Settlement>(), 5);
		}

		// Token: 0x06003C7E RID: 15486 RVA: 0x001047B0 File Offset: 0x001029B0
		private static bool list_player_fief_clickable_condition(out TextObject explanation)
		{
			Kingdom kingdom = Hero.MainHero.MapFaction as Kingdom;
			Settlement fief = ConversationSentence.CurrentProcessedRepeatObject as Settlement;
			if (fief.SiegeEvent != null)
			{
				explanation = new TextObject("{=arCGUuR5}The settlement is under siege.", null);
				return false;
			}
			if (fief.Town.IsOwnerUnassigned || kingdom.UnresolvedDecisions.Any(delegate(KingdomDecision x)
			{
				SettlementClaimantDecision settlementClaimantDecision;
				SettlementClaimantPreliminaryDecision settlementClaimantPreliminaryDecision;
				return ((settlementClaimantDecision = x as SettlementClaimantDecision) != null && settlementClaimantDecision.Settlement == fief) || ((settlementClaimantPreliminaryDecision = x as SettlementClaimantPreliminaryDecision) != null && settlementClaimantPreliminaryDecision.Settlement == fief);
			}))
			{
				explanation = new TextObject("{=OiPqa3L8}This settlement's ownership will be decided through voting.", null);
				return false;
			}
			explanation = null;
			return true;
		}

		// Token: 0x06003C7F RID: 15487 RVA: 0x00104840 File Offset: 0x00102A40
		private static bool list_player_fief_on_condition()
		{
			Settlement settlement = ConversationSentence.CurrentProcessedRepeatObject as Settlement;
			if (settlement != null)
			{
				ConversationSentence.SelectedRepeatLine.SetTextVariable("SETTLEMENT_NAME", settlement.Name);
			}
			return true;
		}

		// Token: 0x06003C80 RID: 15488 RVA: 0x00104872 File Offset: 0x00102A72
		private void list_player_fief_selected_on_consequence()
		{
			this._selectedFief = ConversationSentence.SelectedRepeatObject as Settlement;
		}

		// Token: 0x06003C81 RID: 15489 RVA: 0x00104884 File Offset: 0x00102A84
		private static void turn_companion_to_lord_consequence()
		{
			TextObject textObject = new TextObject("{=ntDH7J3H}This action costs {NEEDED_GOLD_TO_GRANT_FIEF}{GOLD_ICON} and {NEEDED_INFLUENCE_TO_GRANT_FIEF}{INFLUENCE_ICON}. You will also be granting {SETTLEMENT} to {COMPANION.NAME}.", null);
			textObject.SetTextVariable("NEEDED_GOLD_TO_GRANT_FIEF", 20000);
			textObject.SetTextVariable("NEEDED_INFLUENCE_TO_GRANT_FIEF", 500);
			textObject.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
			textObject.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			textObject.SetCharacterProperties("COMPANION", Hero.OneToOneConversationHero.CharacterObject, false);
			textObject.SetTextVariable("SETTLEMENT", CompanionRolesCampaignBehavior.CurrentBehavior._selectedFief.Name);
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=awjomtnJ}Are you sure?", null).ToString(), textObject.ToString(), true, true, new TextObject("{=aeouhelq}Yes", null).ToString(), new TextObject("{=8OkPHu4f}No", null).ToString(), new Action(CompanionRolesCampaignBehavior.ConfirmTurningCompanionToLordConsequence), new Action(CompanionRolesCampaignBehavior.RejectTurningCompanionToLordConsequence), "", 0f, null, null, null), false, false);
		}

		// Token: 0x06003C82 RID: 15490 RVA: 0x0010497C File Offset: 0x00102B7C
		private static void ConfirmTurningCompanionToLordConsequence()
		{
			CompanionRolesCampaignBehavior.CurrentBehavior._playerConfirmedTheAction = true;
			object obj = new TextObject("{=4eStbG4S}Select {COMPANION.NAME}{.o} clan name: ", null);
			StringHelpers.SetCharacterProperties("COMPANION", Hero.OneToOneConversationHero.CharacterObject, null, false);
			InformationManager.ShowTextInquiry(new TextInquiryData(obj.ToString(), string.Empty, true, false, GameTexts.FindText("str_done", null).ToString(), null, new Action<string>(CompanionRolesCampaignBehavior.ClanNameSelectionIsDone), null, false, new Func<string, Tuple<bool, string>>(FactionHelper.IsClanNameApplicable), "", ""), false, false);
		}

		// Token: 0x06003C83 RID: 15491 RVA: 0x00104A03 File Offset: 0x00102C03
		private static void RejectTurningCompanionToLordConsequence()
		{
			CompanionRolesCampaignBehavior.CurrentBehavior._playerConfirmedTheAction = false;
			Campaign.Current.ConversationManager.ContinueConversation();
		}

		// Token: 0x06003C84 RID: 15492 RVA: 0x00104A20 File Offset: 0x00102C20
		private static void ClanNameSelectionIsDone(string clanName)
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			RemoveCompanionAction.ApplyByByTurningToLord(Hero.MainHero.Clan, oneToOneConversationHero);
			oneToOneConversationHero.SetNewOccupation(Occupation.Lord);
			TextObject textObject = GameTexts.FindText("str_generic_clan_name", null);
			textObject.SetTextVariable("CLAN_NAME", new TextObject(clanName, null));
			int randomBannerIdForNewClan = CompanionRolesCampaignBehavior.GetRandomBannerIdForNewClan();
			Clan clan = Clan.CreateCompanionToLordClan(oneToOneConversationHero, CompanionRolesCampaignBehavior.CurrentBehavior._selectedFief, textObject, randomBannerIdForNewClan);
			if (oneToOneConversationHero.PartyBelongedTo == MobileParty.MainParty)
			{
				MobileParty.MainParty.MemberRoster.AddToCounts(oneToOneConversationHero.CharacterObject, -1, false, 0, 0, true, -1);
			}
			MobileParty partyBelongedTo = oneToOneConversationHero.PartyBelongedTo;
			if (partyBelongedTo == null)
			{
				MobileParty mobileParty = LordPartyComponent.CreateLordParty(oneToOneConversationHero.CharacterObject.StringId, oneToOneConversationHero, MobileParty.MainParty.Position, 3f, CompanionRolesCampaignBehavior.CurrentBehavior._selectedFief, oneToOneConversationHero);
				mobileParty.MemberRoster.AddToCounts(clan.Culture.BasicTroop, MBRandom.RandomInt(12, 15), false, 0, 0, true, -1);
				mobileParty.MemberRoster.AddToCounts(clan.Culture.EliteBasicTroop, MBRandom.RandomInt(10, 15), false, 0, 0, true, -1);
			}
			else
			{
				partyBelongedTo.ActualClan = clan;
				partyBelongedTo.Party.SetVisualAsDirty();
			}
			CompanionRolesCampaignBehavior.AdjustCompanionsEquipment(oneToOneConversationHero);
			CompanionRolesCampaignBehavior.SpawnNewHeroesForNewCompanionClan(oneToOneConversationHero, clan, CompanionRolesCampaignBehavior.CurrentBehavior._selectedFief);
			GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, oneToOneConversationHero, 20000, false);
			GainKingdomInfluenceAction.ApplyForDefault(Hero.MainHero, -500f);
			ChangeRelationAction.ApplyPlayerRelation(oneToOneConversationHero, 50, true, true);
			Campaign.Current.ConversationManager.ContinueConversation();
		}

		// Token: 0x06003C85 RID: 15493 RVA: 0x00104B94 File Offset: 0x00102D94
		private static void AdjustCompanionsEquipment(Hero companionHero)
		{
			Equipment newEquipmentForCompanion = CompanionRolesCampaignBehavior.GetNewEquipmentForCompanion(companionHero, true);
			Equipment newEquipmentForCompanion2 = CompanionRolesCampaignBehavior.GetNewEquipmentForCompanion(companionHero, false);
			Equipment equipment = new Equipment(Equipment.EquipmentType.Civilian);
			Equipment equipment2 = new Equipment(Equipment.EquipmentType.Battle);
			for (int i = 0; i < 12; i++)
			{
				if (newEquipmentForCompanion2[i].Item != null && (companionHero.BattleEquipment[i].Item == null || companionHero.BattleEquipment[i].Item.Tier < newEquipmentForCompanion2[i].Item.Tier))
				{
					equipment2[i] = newEquipmentForCompanion2[i];
				}
				else
				{
					equipment2[i] = companionHero.BattleEquipment[i];
				}
				if (newEquipmentForCompanion[i].Item != null && (companionHero.CivilianEquipment[i].Item == null || companionHero.CivilianEquipment[i].Item.Tier < newEquipmentForCompanion[i].Item.Tier))
				{
					equipment[i] = newEquipmentForCompanion[i];
				}
				else
				{
					equipment[i] = companionHero.CivilianEquipment[i];
				}
			}
			EquipmentHelper.AssignHeroEquipmentFromEquipment(companionHero, equipment);
			EquipmentHelper.AssignHeroEquipmentFromEquipment(companionHero, equipment2);
		}

		// Token: 0x06003C86 RID: 15494 RVA: 0x00104CF0 File Offset: 0x00102EF0
		private static int GetRandomBannerIdForNewClan()
		{
			MBReadOnlyList<int> possibleClanBannerIconsIDs = Hero.MainHero.MapFaction.Culture.PossibleClanBannerIconsIDs;
			int num = possibleClanBannerIconsIDs.GetRandomElement<int>();
			if (CompanionRolesCampaignBehavior.CurrentBehavior._alreadyUsedIconIdsForNewClans.Contains(num))
			{
				int num2 = 0;
				do
				{
					num = possibleClanBannerIconsIDs.GetRandomElement<int>();
					num2++;
				}
				while (CompanionRolesCampaignBehavior.CurrentBehavior._alreadyUsedIconIdsForNewClans.Contains(num) && num2 < 20);
				bool flag = num2 != 20;
				if (!flag)
				{
					for (int i = 0; i < possibleClanBannerIconsIDs.Count; i++)
					{
						if (!CompanionRolesCampaignBehavior.CurrentBehavior._alreadyUsedIconIdsForNewClans.Contains(possibleClanBannerIconsIDs[i]))
						{
							num = possibleClanBannerIconsIDs[i];
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					num = possibleClanBannerIconsIDs.GetRandomElement<int>();
				}
			}
			if (!CompanionRolesCampaignBehavior.CurrentBehavior._alreadyUsedIconIdsForNewClans.Contains(num))
			{
				CompanionRolesCampaignBehavior.CurrentBehavior._alreadyUsedIconIdsForNewClans.Add(num);
			}
			return num;
		}

		// Token: 0x06003C87 RID: 15495 RVA: 0x00104DC8 File Offset: 0x00102FC8
		private static void SpawnNewHeroesForNewCompanionClan(Hero companionHero, Clan clan, Settlement settlement)
		{
			MBReadOnlyList<CharacterObject> lordTemplates = companionHero.Culture.LordTemplates;
			List<Hero> list = new List<Hero>();
			list.Add(CompanionRolesCampaignBehavior.CreateNewHeroForNewCompanionClan(lordTemplates.GetRandomElement<CharacterObject>(), settlement, new Dictionary<SkillObject, int>
			{
				{
					DefaultSkills.Steward,
					MBRandom.RandomInt(100, 175)
				},
				{
					DefaultSkills.Leadership,
					MBRandom.RandomInt(125, 175)
				},
				{
					DefaultSkills.OneHanded,
					MBRandom.RandomInt(125, 175)
				},
				{
					DefaultSkills.Medicine,
					MBRandom.RandomInt(125, 175)
				}
			}));
			list.Add(CompanionRolesCampaignBehavior.CreateNewHeroForNewCompanionClan(lordTemplates.GetRandomElement<CharacterObject>(), settlement, new Dictionary<SkillObject, int>
			{
				{
					DefaultSkills.OneHanded,
					MBRandom.RandomInt(100, 175)
				},
				{
					DefaultSkills.Leadership,
					MBRandom.RandomInt(125, 175)
				},
				{
					DefaultSkills.Tactics,
					MBRandom.RandomInt(125, 175)
				},
				{
					DefaultSkills.Engineering,
					MBRandom.RandomInt(125, 175)
				}
			}));
			list.Add(companionHero);
			foreach (Hero hero in list)
			{
				hero.Clan = clan;
				hero.ChangeState(Hero.CharacterStates.Active);
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, Hero.MainHero, MBRandom.RandomInt(5, 10), false);
				if (hero != companionHero)
				{
					EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
				}
				foreach (Hero hero2 in list)
				{
					if (hero != hero2)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(hero, hero2, MBRandom.RandomInt(5, 10), false);
					}
				}
			}
		}

		// Token: 0x06003C88 RID: 15496 RVA: 0x00104F8C File Offset: 0x0010318C
		private static Hero CreateNewHeroForNewCompanionClan(CharacterObject templateCharacter, Settlement settlement, Dictionary<SkillObject, int> startingSkills)
		{
			Hero hero = HeroCreator.CreateSpecialHero(templateCharacter, settlement, null, null, MBRandom.RandomInt(Campaign.Current.Models.AgeModel.HeroComesOfAge, 50));
			foreach (KeyValuePair<SkillObject, int> keyValuePair in startingSkills)
			{
				hero.HeroDeveloper.SetInitialSkillLevel(keyValuePair.Key, keyValuePair.Value);
			}
			return hero;
		}

		// Token: 0x06003C89 RID: 15497 RVA: 0x00105014 File Offset: 0x00103214
		private static Equipment GetNewEquipmentForCompanion(Hero companionHero, bool isCivilian)
		{
			return Campaign.Current.Models.EquipmentSelectionModel.GetEquipmentRostersForCompanion(companionHero, isCivilian).GetRandomElementInefficiently<MBEquipmentRoster>().AllEquipments.GetRandomElement<Equipment>();
		}

		// Token: 0x06003C8A RID: 15498 RVA: 0x0010503B File Offset: 0x0010323B
		private static void list_player_fief_cancel_on_consequence()
		{
			CompanionRolesCampaignBehavior.CurrentBehavior._playerConfirmedTheAction = false;
		}

		// Token: 0x06003C8B RID: 15499 RVA: 0x00105048 File Offset: 0x00103248
		private static bool fief_selected_on_condition()
		{
			MBTextManager.SetTextVariable("SETTLEMENT_NAME", CompanionRolesCampaignBehavior.CurrentBehavior._selectedFief.Name, false);
			return true;
		}

		// Token: 0x06003C8C RID: 15500 RVA: 0x00105065 File Offset: 0x00103265
		private static bool companion_thanks_on_condition()
		{
			return CompanionRolesCampaignBehavior.CurrentBehavior._playerConfirmedTheAction;
		}

		// Token: 0x06003C8D RID: 15501 RVA: 0x00105074 File Offset: 0x00103274
		private static bool fief_selected_confirm_clickable_on_condition(out TextObject explanation)
		{
			MBTextManager.SetTextVariable("CULTURE_SPECIFIC_TITLE", HeroHelper.GetTitleInIndefiniteCase(Hero.OneToOneConversationHero), false);
			MBTextManager.SetTextVariable("SETTLEMENT_NAME", CompanionRolesCampaignBehavior.CurrentBehavior._selectedFief.Name, false);
			bool flag = Hero.MainHero.Gold >= 20000;
			bool flag2 = Hero.MainHero.Clan.Influence >= 500f;
			MBTextManager.SetTextVariable("NEEDED_GOLD_TO_GRANT_FIEF", 20000);
			MBTextManager.SetTextVariable("NEEDED_INFLUENCE_TO_GRANT_FIEF", 500);
			MBTextManager.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">", false);
			MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);
			if (flag && flag2)
			{
				explanation = new TextObject("{=PxQEwCha}You will pay {NEEDED_GOLD_TO_GRANT_FIEF}{GOLD_ICON}, {NEEDED_INFLUENCE_TO_GRANT_FIEF}{INFLUENCE_ICON}.", null);
				return true;
			}
			explanation = new TextObject("{=!}{GOLD_REQUIREMENT}{INFLUENCE_REQUIREMENT}", null);
			if (!flag)
			{
				TextObject variable = new TextObject("{=yo2NvkQQ}You need {NEEDED_GOLD_TO_GRANT_FIEF}{GOLD_ICON}. ", null);
				explanation.SetTextVariable("GOLD_REQUIREMENT", variable);
			}
			if (!flag2)
			{
				TextObject variable2 = new TextObject("{=pDeFXZJd}You need {NEEDED_INFLUENCE_TO_GRANT_FIEF}{INFLUENCE_ICON}.", null);
				explanation.SetTextVariable("INFLUENCE_REQUIREMENT", variable2);
			}
			return false;
		}

		// Token: 0x06003C8E RID: 15502 RVA: 0x00105179 File Offset: 0x00103379
		private static void companion_talk_done_on_consequence()
		{
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.LeaveEncounter = true;
			}
		}

		// Token: 0x06003C8F RID: 15503 RVA: 0x00105188 File Offset: 0x00103388
		private static void companion_fire_on_consequence()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			RemoveCompanionAction.ApplyByFire(oneToOneConversationHero.CompanionOf, oneToOneConversationHero);
			KillCharacterAction.ApplyByRemove(oneToOneConversationHero, false, true);
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.LeaveEncounter = true;
			}
		}

		// Token: 0x06003C90 RID: 15504 RVA: 0x001051BC File Offset: 0x001033BC
		private bool companion_rejoin_after_emprisonment_role_on_condition()
		{
			if (Hero.OneToOneConversationHero != null && !Hero.OneToOneConversationHero.IsPartyLeader && (Hero.OneToOneConversationHero.IsPlayerCompanion || Hero.OneToOneConversationHero.Clan == Clan.PlayerClan) && Hero.OneToOneConversationHero.PartyBelongedTo != MobileParty.MainParty && (Hero.OneToOneConversationHero.PartyBelongedTo == null || !Hero.OneToOneConversationHero.PartyBelongedTo.IsCaravan))
			{
				if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown && Hero.OneToOneConversationHero.GovernorOf == Settlement.CurrentSettlement.Town)
				{
					MBTextManager.SetTextVariable("COMPANION_REJOIN_LINE", "{=Z5zAok5G}I need to recall you to my party, and to stop governing this town.", false);
				}
				else
				{
					MBTextManager.SetTextVariable("COMPANION_REJOIN_LINE", "{=gR0ksbaQ}Get your things. I'd like you to rejoin the party.", false);
				}
				return true;
			}
			return false;
		}

		// Token: 0x06003C91 RID: 15505 RVA: 0x0010527B File Offset: 0x0010347B
		private void companion_rejoin_after_emprisonment_role_on_consequence()
		{
			AddHeroToPartyAction.Apply(Hero.OneToOneConversationHero, MobileParty.MainParty, true);
			Campaign.Current.ConversationManager.ConversationEnd -= this.companion_rejoin_after_emprisonment_role_on_consequence;
		}

		// Token: 0x06003C92 RID: 15506 RVA: 0x001052A8 File Offset: 0x001034A8
		private void OnCompanionRemoved(Hero companion, RemoveCompanionAction.RemoveCompanionDetail detail)
		{
			if (LocationComplex.Current != null)
			{
				LocationComplex.Current.RemoveCharacterIfExists(companion);
			}
			if (PlayerEncounter.LocationEncounter != null)
			{
				PlayerEncounter.LocationEncounter.RemoveAccompanyingCharacter(companion);
			}
		}

		// Token: 0x06003C93 RID: 15507 RVA: 0x001052CE File Offset: 0x001034CE
		private bool companion_agrees_to_leave_on_condition()
		{
			MBTextManager.SetTextVariable("AGREE_TO_LEAVE", new TextObject("{=0geP718k}Well... I don't know what to say. Goodbye, then.", null), false);
			return true;
		}

		// Token: 0x06003C94 RID: 15508 RVA: 0x001052E8 File Offset: 0x001034E8
		private static bool companion_has_role_on_condition()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			PartyRole heroPartyRole = MobileParty.MainParty.GetHeroPartyRole(oneToOneConversationHero);
			if (heroPartyRole == PartyRole.None)
			{
				MBTextManager.SetTextVariable("COMPANION_ROLE", new TextObject("{=k7ebznzr}Yes?", null), false);
			}
			else
			{
				MBTextManager.SetTextVariable("COMPANION_ROLE", new TextObject("{=n3bvfe8t}I am currently working as {COMPANION_JOB}.", null), false);
				MBTextManager.SetTextVariable("COMPANION_JOB", GameTexts.FindText("role", heroPartyRole.ToString()), false);
			}
			return true;
		}

		// Token: 0x06003C95 RID: 15509 RVA: 0x0010535B File Offset: 0x0010355B
		private static bool companion_role_discuss_on_condition()
		{
			if (Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan)
			{
				MobileParty partyBelongedTo = Hero.OneToOneConversationHero.PartyBelongedTo;
				return partyBelongedTo != null && !partyBelongedTo.IsInRaftState;
			}
			return false;
		}

		// Token: 0x06003C96 RID: 15510 RVA: 0x0010538F File Offset: 0x0010358F
		private static bool companion_assign_role_on_condition()
		{
			return Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && Hero.OneToOneConversationHero.PartyBelongedTo == MobileParty.MainParty;
		}

		// Token: 0x06003C97 RID: 15511 RVA: 0x001053BC File Offset: 0x001035BC
		private static bool companion_becomes_engineer_on_condition()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			Hero roleHolder = oneToOneConversationHero.PartyBelongedTo.GetRoleHolder(PartyRole.Engineer);
			if (roleHolder != null)
			{
				TextObject textObject = new TextObject("{=QEp8t8u0}(Currently held by {COMPANION.LINK})", null);
				StringHelpers.SetCharacterProperties("COMPANION", roleHolder.CharacterObject, textObject, false);
				MBTextManager.SetTextVariable("CURRENTLY_HELD_ENGINEER", textObject, false);
			}
			else
			{
				MBTextManager.SetTextVariable("CURRENTLY_HELD_ENGINEER", "{=kNQMkh3j}(Currently unassigned)", false);
			}
			return roleHolder != oneToOneConversationHero && MobilePartyHelper.IsHeroAssignableForEngineerInParty(oneToOneConversationHero, oneToOneConversationHero.PartyBelongedTo);
		}

		// Token: 0x06003C98 RID: 15512 RVA: 0x0010542E File Offset: 0x0010362E
		private static void companion_becomes_engineer_on_consequence()
		{
			Hero.OneToOneConversationHero.PartyBelongedTo.SetPartyEngineer(Hero.OneToOneConversationHero);
		}

		// Token: 0x06003C99 RID: 15513 RVA: 0x00105444 File Offset: 0x00103644
		private static bool companion_becomes_surgeon_on_condition()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			Hero roleHolder = oneToOneConversationHero.PartyBelongedTo.GetRoleHolder(PartyRole.Surgeon);
			if (roleHolder != null)
			{
				TextObject textObject = new TextObject("{=QEp8t8u0}(Currently held by {COMPANION.LINK})", null);
				StringHelpers.SetCharacterProperties("COMPANION", roleHolder.CharacterObject, textObject, false);
				MBTextManager.SetTextVariable("CURRENTLY_HELD_SURGEON", textObject, false);
			}
			else
			{
				MBTextManager.SetTextVariable("CURRENTLY_HELD_SURGEON", "{=kNQMkh3j}(Currently unassigned)", false);
			}
			return roleHolder != oneToOneConversationHero && MobilePartyHelper.IsHeroAssignableForSurgeonInParty(oneToOneConversationHero, oneToOneConversationHero.PartyBelongedTo);
		}

		// Token: 0x06003C9A RID: 15514 RVA: 0x001054B6 File Offset: 0x001036B6
		private static void companion_becomes_surgeon_on_consequence()
		{
			Hero.OneToOneConversationHero.PartyBelongedTo.SetPartySurgeon(Hero.OneToOneConversationHero);
		}

		// Token: 0x06003C9B RID: 15515 RVA: 0x001054CC File Offset: 0x001036CC
		private static bool companion_becomes_quartermaster_on_condition()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			Hero roleHolder = oneToOneConversationHero.PartyBelongedTo.GetRoleHolder(PartyRole.Quartermaster);
			if (roleHolder != null)
			{
				TextObject textObject = new TextObject("{=QEp8t8u0}(Currently held by {COMPANION.LINK})", null);
				StringHelpers.SetCharacterProperties("COMPANION", roleHolder.CharacterObject, textObject, false);
				MBTextManager.SetTextVariable("CURRENTLY_HELD_QUARTERMASTER", textObject, false);
			}
			else
			{
				MBTextManager.SetTextVariable("CURRENTLY_HELD_QUARTERMASTER", "{=kNQMkh3j}(Currently unassigned)", false);
			}
			Hero oneToOneConversationHero2 = Hero.OneToOneConversationHero;
			return roleHolder != oneToOneConversationHero && MobilePartyHelper.IsHeroAssignableForQuartermasterInParty(oneToOneConversationHero2, Hero.OneToOneConversationHero.PartyBelongedTo);
		}

		// Token: 0x06003C9C RID: 15516 RVA: 0x00105549 File Offset: 0x00103749
		private static void companion_becomes_quartermaster_on_consequence()
		{
			Hero.OneToOneConversationHero.PartyBelongedTo.SetPartyQuartermaster(Hero.OneToOneConversationHero);
		}

		// Token: 0x06003C9D RID: 15517 RVA: 0x00105560 File Offset: 0x00103760
		private static bool companion_becomes_scout_on_condition()
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			Hero roleHolder = oneToOneConversationHero.PartyBelongedTo.GetRoleHolder(PartyRole.Scout);
			if (roleHolder != null)
			{
				TextObject textObject = new TextObject("{=QEp8t8u0}(Currently held by {COMPANION.LINK})", null);
				StringHelpers.SetCharacterProperties("COMPANION", roleHolder.CharacterObject, textObject, false);
				MBTextManager.SetTextVariable("CURRENTLY_HELD_SCOUT", textObject, false);
			}
			else
			{
				MBTextManager.SetTextVariable("CURRENTLY_HELD_SCOUT", "{=kNQMkh3j}(Currently unassigned)", false);
			}
			return roleHolder != oneToOneConversationHero && MobilePartyHelper.IsHeroAssignableForScoutInParty(oneToOneConversationHero, oneToOneConversationHero.PartyBelongedTo);
		}

		// Token: 0x06003C9E RID: 15518 RVA: 0x001055D3 File Offset: 0x001037D3
		private static void companion_becomes_scout_on_consequence()
		{
			Hero.OneToOneConversationHero.PartyBelongedTo.SetPartyScout(Hero.OneToOneConversationHero);
		}

		// Token: 0x06003C9F RID: 15519 RVA: 0x001055E9 File Offset: 0x001037E9
		private static void companion_delete_party_role_consequence()
		{
			Hero.OneToOneConversationHero.PartyBelongedTo.RemoveHeroPartyRole(Hero.OneToOneConversationHero);
		}

		// Token: 0x06003CA0 RID: 15520 RVA: 0x001055FF File Offset: 0x001037FF
		private static bool companion_fire_engineer_on_condition()
		{
			return Hero.OneToOneConversationHero.PartyBelongedTo.GetRoleHolder(PartyRole.Engineer) == Hero.OneToOneConversationHero && Hero.OneToOneConversationHero != Hero.OneToOneConversationHero.PartyBelongedTo.LeaderHero;
		}

		// Token: 0x06003CA1 RID: 15521 RVA: 0x00105633 File Offset: 0x00103833
		private static bool companion_fire_surgeon_on_condition()
		{
			return Hero.OneToOneConversationHero.PartyBelongedTo.GetRoleHolder(PartyRole.Surgeon) == Hero.OneToOneConversationHero && Hero.OneToOneConversationHero != Hero.OneToOneConversationHero.PartyBelongedTo.LeaderHero;
		}

		// Token: 0x06003CA2 RID: 15522 RVA: 0x00105667 File Offset: 0x00103867
		private static bool companion_fire_quartermaster_on_condition()
		{
			return Hero.OneToOneConversationHero.PartyBelongedTo.GetRoleHolder(PartyRole.Quartermaster) == Hero.OneToOneConversationHero && Hero.OneToOneConversationHero != Hero.OneToOneConversationHero.PartyBelongedTo.LeaderHero;
		}

		// Token: 0x06003CA3 RID: 15523 RVA: 0x0010569C File Offset: 0x0010389C
		private static bool companion_fire_scout_on_condition()
		{
			return Hero.OneToOneConversationHero.PartyBelongedTo.GetRoleHolder(PartyRole.Scout) == Hero.OneToOneConversationHero && Hero.OneToOneConversationHero != Hero.OneToOneConversationHero.PartyBelongedTo.LeaderHero;
		}

		// Token: 0x06003CA4 RID: 15524 RVA: 0x001056D4 File Offset: 0x001038D4
		private bool companion_rescue_start_condition()
		{
			if (Campaign.Current.CurrentConversationContext == ConversationContext.FreeOrCapturePrisonerHero)
			{
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (((oneToOneConversationHero != null) ? oneToOneConversationHero.CompanionOf : null) == Clan.PlayerClan && CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Wanderer)
				{
					MBTextManager.SetTextVariable("SALUTATION", Campaign.Current.ConversationManager.FindMatchingTextOrNull("str_salutation", CharacterObject.OneToOneConversationCharacter), false);
					return true;
				}
			}
			return false;
		}

		// Token: 0x06003CA5 RID: 15525 RVA: 0x0010573B File Offset: 0x0010393B
		private void companion_rescue_answer_options_join_party_consequence()
		{
			EndCaptivityAction.ApplyByReleasedAfterBattle(Hero.OneToOneConversationHero);
			Hero.OneToOneConversationHero.ChangeState(Hero.CharacterStates.Active);
			MobileParty.MainParty.AddElementToMemberRoster(CharacterObject.OneToOneConversationCharacter, 1, false);
		}

		// Token: 0x06003CA6 RID: 15526 RVA: 0x00105764 File Offset: 0x00103964
		private bool lead_a_party_clickable_condition(out TextObject reason)
		{
			bool flag = Clan.PlayerClan.CommanderLimit > Clan.PlayerClan.WarPartyComponents.Count;
			int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
			bool flag2 = Hero.MainHero.Gold > partyGoldLowerThreshold - Hero.OneToOneConversationHero.Gold;
			TextObject textObject = new TextObject("{=QH3pgsia}Creating the party will cost you {PARTY_COST}{GOLD_ICON}.", null).SetTextVariable("PARTY_COST", partyGoldLowerThreshold - Hero.OneToOneConversationHero.Gold).SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
			reason = textObject;
			if (!flag)
			{
				reason = GameTexts.FindText("str_clan_doesnt_have_empty_party_slots", null);
			}
			else if (!flag2)
			{
				reason = new TextObject("{=xpCdwmlX}You don't have enough gold to make {HERO.NAME} a party leader.", null);
				reason.SetCharacterProperties("HERO", Hero.OneToOneConversationHero.CharacterObject, false);
			}
			return flag && flag2;
		}

		// Token: 0x06003CA7 RID: 15527 RVA: 0x00105829 File Offset: 0x00103A29
		private void companion_rescue_answer_options_lead_party_consequence()
		{
			this.OpenPartyScreenForRescue();
		}

		// Token: 0x06003CA8 RID: 15528 RVA: 0x00105831 File Offset: 0x00103A31
		private void OpenPartyScreenForRescue()
		{
			PartyScreenHelper.OpenScreenAsCreateClanPartyForHero(Hero.OneToOneConversationHero, new PartyScreenClosedDelegate(this.PartyScreenClosed), new IsTroopTransferableDelegate(this.TroopTransferableDelegate));
		}

		// Token: 0x06003CA9 RID: 15529 RVA: 0x00105858 File Offset: 0x00103A58
		private void PartyScreenClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
		{
			if (!fromCancel)
			{
				CharacterObject character = leftMemberRoster.GetTroopRoster().FirstOrDefault(delegate(TroopRosterElement x)
				{
					Hero heroObject = x.Character.HeroObject;
					return heroObject != null && heroObject.IsPlayerCompanion;
				}).Character;
				EndCaptivityAction.ApplyByReleasedAfterBattle(character.HeroObject);
				character.HeroObject.ChangeState(Hero.CharacterStates.Active);
				MobileParty.MainParty.AddElementToMemberRoster(character, 1, false);
				this._partyCreatedAfterRescueForCompanion = true;
				int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
				if (character.HeroObject.Gold < partyGoldLowerThreshold)
				{
					GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, character.HeroObject, partyGoldLowerThreshold - character.HeroObject.Gold, false);
				}
				MobileParty mobileParty = MobilePartyHelper.CreateNewClanMobileParty(character.HeroObject, Clan.PlayerClan);
				foreach (TroopRosterElement troopRosterElement in leftMemberRoster.GetTroopRoster())
				{
					if (troopRosterElement.Character != character)
					{
						mobileParty.MemberRoster.Add(troopRosterElement);
						rightOwnerParty.MemberRoster.AddToCounts(troopRosterElement.Character, -troopRosterElement.Number, false, -troopRosterElement.WoundedNumber, -troopRosterElement.Xp, true, -1);
					}
				}
				foreach (TroopRosterElement troopRosterElement2 in leftPrisonRoster.GetTroopRoster())
				{
					mobileParty.MemberRoster.Add(troopRosterElement2);
					rightOwnerParty.PrisonRoster.AddToCounts(troopRosterElement2.Character, -troopRosterElement2.Number, false, -troopRosterElement2.WoundedNumber, -troopRosterElement2.Xp, true, -1);
				}
			}
		}

		// Token: 0x06003CAA RID: 15530 RVA: 0x00105A18 File Offset: 0x00103C18
		private bool TroopTransferableDelegate(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
		{
			return !character.IsHero;
		}

		// Token: 0x06003CAB RID: 15531 RVA: 0x00105A23 File Offset: 0x00103C23
		private bool party_screen_continue_conversation_condition()
		{
			if (Campaign.Current.CurrentConversationContext == ConversationContext.FreeOrCapturePrisonerHero)
			{
				Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
				if (((oneToOneConversationHero != null) ? oneToOneConversationHero.CompanionOf : null) == Clan.PlayerClan)
				{
					return CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Wanderer;
				}
			}
			return false;
		}

		// Token: 0x06003CAC RID: 15532 RVA: 0x00105A5A File Offset: 0x00103C5A
		private bool party_screen_opened_but_party_is_not_created_after_rescue_condition()
		{
			return !this._partyCreatedAfterRescueForCompanion;
		}

		// Token: 0x06003CAD RID: 15533 RVA: 0x00105A65 File Offset: 0x00103C65
		private bool party_screen_opened_and_party_is_created_after_rescue_condition()
		{
			return this._partyCreatedAfterRescueForCompanion;
		}

		// Token: 0x06003CAE RID: 15534 RVA: 0x00105A6D File Offset: 0x00103C6D
		private void end_rescue_companion()
		{
			this._partyCreatedAfterRescueForCompanion = false;
			if (Hero.OneToOneConversationHero.IsPrisoner)
			{
				EndCaptivityAction.ApplyByReleasedAfterBattle(Hero.OneToOneConversationHero);
			}
		}

		// Token: 0x04001253 RID: 4691
		private const int CompanionRelationLimit = -10;

		// Token: 0x04001254 RID: 4692
		private const int NeededGoldToGrantFief = 20000;

		// Token: 0x04001255 RID: 4693
		private const int NeededInfluenceToGrantFief = 500;

		// Token: 0x04001256 RID: 4694
		private const int RelationGainWhenCompanionToLordAction = 50;

		// Token: 0x04001257 RID: 4695
		private const int NewCreatedHeroForCompanionClanMaxAge = 50;

		// Token: 0x04001258 RID: 4696
		private const int NewHeroSkillUpperLimit = 175;

		// Token: 0x04001259 RID: 4697
		private const int NewHeroSkillLowerLimit = 125;

		// Token: 0x0400125A RID: 4698
		private Settlement _selectedFief;

		// Token: 0x0400125B RID: 4699
		private bool _playerConfirmedTheAction;

		// Token: 0x0400125C RID: 4700
		private List<int> _alreadyUsedIconIdsForNewClans = new List<int>();

		// Token: 0x0400125D RID: 4701
		private bool _partyCreatedAfterRescueForCompanion;
	}
}
