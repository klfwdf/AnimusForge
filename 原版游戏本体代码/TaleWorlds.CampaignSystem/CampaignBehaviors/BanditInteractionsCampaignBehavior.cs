using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x020003CF RID: 975
	public class BanditInteractionsCampaignBehavior : CampaignBehaviorBase
	{
		// Token: 0x060039E4 RID: 14820 RVA: 0x000ED944 File Offset: 0x000EBB44
		public BanditInteractionsCampaignBehavior()
			: base("BanditsCampaignBehavior")
		{
		}

		// Token: 0x060039E5 RID: 14821 RVA: 0x000ED95C File Offset: 0x000EBB5C
		public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
		{
			this.AddDialogs(campaignGameStarter);
		}

		// Token: 0x060039E6 RID: 14822 RVA: 0x000ED965 File Offset: 0x000EBB65
		public override void RegisterEvents()
		{
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.OnPartyDestroyed));
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
		}

		// Token: 0x060039E7 RID: 14823 RVA: 0x000ED995 File Offset: 0x000EBB95
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<Dictionary<MobileParty, BanditInteractionsCampaignBehavior.PlayerInteraction>>("_interactedBandits", ref this._interactedBandits);
		}

		// Token: 0x060039E8 RID: 14824 RVA: 0x000ED9A9 File Offset: 0x000EBBA9
		private void OnPartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
		{
			if (this._interactedBandits.ContainsKey(mobileParty))
			{
				this._interactedBandits.Remove(mobileParty);
			}
		}

		// Token: 0x060039E9 RID: 14825 RVA: 0x000ED9C6 File Offset: 0x000EBBC6
		private void SetPlayerInteraction(MobileParty mobileParty, BanditInteractionsCampaignBehavior.PlayerInteraction interaction)
		{
			if (this._interactedBandits.ContainsKey(mobileParty))
			{
				this._interactedBandits[mobileParty] = interaction;
				return;
			}
			this._interactedBandits.Add(mobileParty, interaction);
		}

		// Token: 0x060039EA RID: 14826 RVA: 0x000ED9F4 File Offset: 0x000EBBF4
		private BanditInteractionsCampaignBehavior.PlayerInteraction GetPlayerInteraction(MobileParty mobileParty)
		{
			BanditInteractionsCampaignBehavior.PlayerInteraction result;
			if (this._interactedBandits.TryGetValue(mobileParty, out result))
			{
				return result;
			}
			return BanditInteractionsCampaignBehavior.PlayerInteraction.None;
		}

		// Token: 0x060039EB RID: 14827 RVA: 0x000EDA14 File Offset: 0x000EBC14
		protected void AddDialogs(CampaignGameStarter campaignGameSystemStarter)
		{
			campaignGameSystemStarter.AddDialogLine("bandit_start_defender", "start", "bandit_defender", "{=!}{ROBBERY_THREAT}", new ConversationSentence.OnConditionDelegate(this.bandit_start_defender_condition), null, 100, null);
			campaignGameSystemStarter.AddPlayerLine("bandit_start_defender_1", "bandit_defender", "bandit_start_fight", "{=DEnFOGhS}Fight me if you dare!", new ConversationSentence.OnConditionDelegate(this.bandit_start_defender_main_hero_defend_condition), null, 100, null, null);
			campaignGameSystemStarter.AddPlayerLine("bandit_start_defender_3", "bandit_defender", "bandit_start_fight", "{=LLEffOga}We're in no shape to fight. We're at your mercy.", () => !this.bandit_start_defender_main_hero_defend_condition(), null, 100, null, null);
			campaignGameSystemStarter.AddPlayerLine("bandit_start_defender_2", "bandit_defender", "barter_with_bandit_prebarter", "{=aQYMefHU}Maybe we can work out something.", new ConversationSentence.OnConditionDelegate(this.bandit_start_barter_condition), null, 100, null, null);
			campaignGameSystemStarter.AddDialogLine("bandit_start_fight", "bandit_start_fight", "close_window", "{=!}{ROBBERY_START_FIGHT}", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_bandit_set_hostile_on_consequence), 100, null);
			campaignGameSystemStarter.AddDialogLine("barter_with_bandit_prebarter", "barter_with_bandit_prebarter", "barter_with_bandit_screen", "{=!}{ROBBERY_PAY_AGREEMENT}", null, null, 100, null);
			campaignGameSystemStarter.AddDialogLine("barter_with_bandit_screen", "barter_with_bandit_screen", "barter_with_bandit_postbarter", "{=!}Barter screen goes here", null, new ConversationSentence.OnConsequenceDelegate(this.bandit_start_barter_consequence), 100, null);
			campaignGameSystemStarter.AddDialogLine("barter_with_bandit_postbarter_1", "barter_with_bandit_postbarter", "close_window", "{=!}{ROBBERY_CONCLUSION}", new ConversationSentence.OnConditionDelegate(this.bandit_barter_successful_condition), new ConversationSentence.OnConsequenceDelegate(this.bandit_barter_successful_on_consequence), 100, null);
			campaignGameSystemStarter.AddDialogLine("barter_with_bandit_postbarter_2", "barter_with_bandit_postbarter", "close_window", "{=!}{ROBBERY_START_FIGHT}", () => !this.bandit_barter_successful_condition(), new ConversationSentence.OnConsequenceDelegate(this.conversation_bandit_set_hostile_on_consequence), 100, null);
			campaignGameSystemStarter.AddDialogLine("bandit_start_attacker", "start", "bandit_attacker", "{=!}{BANDIT_NEUTRAL_GREETING}", new ConversationSentence.OnConditionDelegate(this.bandit_neutral_greet_on_condition), new ConversationSentence.OnConsequenceDelegate(this.bandit_neutral_greet_on_consequence), 100, null);
			campaignGameSystemStarter.AddPlayerLine("common_encounter_ultimatum", "bandit_attacker", "common_encounter_ultimatum_answer", "{=!}{BANDIT_ULTIMATUM}", null, null, 100, null, null);
			campaignGameSystemStarter.AddPlayerLine("common_encounter_fight", "bandit_attacker", "bandit_attacker_leave", "{=3W3eEIIZ}Never mind. You can go.", null, null, 100, null, null);
			campaignGameSystemStarter.AddDialogLine("common_encounter_ultimatum_surrender", "common_encounter_ultimatum_answer", "common_bandit_surrender_answer", "{=ji2eenPE}All right - we give up. We can't fight you. Maybe the likes of us don't deserve mercy, but... show what mercy you can.", new ConversationSentence.OnConditionDelegate(this.conversation_bandits_surrender_on_condition), null, 100, null);
			campaignGameSystemStarter.AddDialogLine("common_encounter_ultimatum_war", "common_encounter_ultimatum_answer", "close_window", "{=n99VA8KP}You'll never take us alive![if:convo_angry][ib:aggressive]", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_bandit_set_hostile_on_consequence), 100, null);
			campaignGameSystemStarter.AddPlayerLine("common_bandit_join_player_declined_1", "bandits_we_can_join_you", "player_do_not_let_bandits_to_join", "{=JZvywHNy}You think I'm daft? I'm not trusting you an inch.", null, null, 100, null, null);
			campaignGameSystemStarter.AddPlayerLine("common_bandit_join_player_declined_2", "bandits_we_can_join_you", "player_do_not_let_bandits_to_join", "{=z0WacPaW}No, justice demands you pay for your crimes.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_bandit_set_hostile_on_consequence), 100, null, null);
			campaignGameSystemStarter.AddPlayerLine("common_bandit_join_player_leave", "bandits_we_can_join_you", "bandit_attacker_leave", "{=D33fIGQe}Never mind.", null, null, 100, null, null);
			campaignGameSystemStarter.AddDialogLine("common_encounter_declined_looters_to_join_war_surrender", "player_do_not_let_bandits_to_join", "common_bandit_surrender_answer", "{=ji2eenPE}All right - we give up. We can't fight you. Maybe the likes of us don't deserve mercy, but... show what mercy you can.", new ConversationSentence.OnConditionDelegate(this.conversation_bandits_surrender_on_condition), null, 100, null);
			campaignGameSystemStarter.AddPlayerLine("common_bandit_surrender_accepted", "common_bandit_surrender_answer", "close_window", "{=cbzJRaDJ}You are my prisoner now!", null, delegate
			{
				MobileParty party = MobileParty.ConversationParty;
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate()
				{
					this.conversation_bandits_surrender_on_consequence(party);
				};
			}, 100, null, null);
			campaignGameSystemStarter.AddPlayerLine("common_bandit_surrender_declined", "common_bandit_surrender_answer", "player_do_not_let_bandits_to_join", "{=z0WacPaW}No, justice demands you pay for your crimes.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_bandit_set_hostile_on_consequence), 100, null, null);
			campaignGameSystemStarter.AddPlayerLine("common_bandit_surrender_join_offer", "common_bandit_surrender_answer", "close_window", "{=e87vsXfI}You will earn back your lives by serving under my command.", null, delegate
			{
				MobileParty party = MobileParty.ConversationParty;
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate()
				{
					this.conversation_bandits_join_player_party_on_consequence(party);
				};
			}, 100, new ConversationSentence.OnClickableConditionDelegate(this.conversation_bandit_player_can_make_them_join_condition), null);
			campaignGameSystemStarter.AddDialogLine("common_encounter_ultimatum_war_2", "player_do_not_let_bandits_to_join", "close_window", "{=LDhU5urT}So that's how it is, is it? Right then - I'll make one of you bleed before I go down.[if:convo_angry][ib:aggressive]", null, null, 100, null);
			campaignGameSystemStarter.AddDialogLine("bandit_attacker_try_leave_success", "bandit_attacker_leave", "close_window", "{=IDdyHef9}We'll be on our way, then!", new ConversationSentence.OnConditionDelegate(this.bandit_attacker_try_leave_condition), delegate()
			{
				PlayerEncounter.LeaveEncounter = true;
			}, 100, null);
			campaignGameSystemStarter.AddDialogLine("bandit_attacker_try_leave_fail", "bandit_attacker_leave", "bandit_defender", "{=6Wc1XErN}Wait, wait... You're not going anywhere just yet.", () => !this.bandit_attacker_try_leave_condition(), null, 100, null);
			campaignGameSystemStarter.AddDialogLine("minor_faction_hostile", "start", "minor_faction_talk_hostile_response", "{=!}{MINOR_FACTION_ENCOUNTER}", new ConversationSentence.OnConditionDelegate(this.conversation_minor_faction_hostile_on_condition), null, 100, null);
			campaignGameSystemStarter.AddPlayerLine("minor_faction_talk_hostile_response_1", "minor_faction_talk_hostile_response", "close_window", "{=aaf5R99a}I'll give you nothing but cold steel, you scum!", null, null, 100, null, null);
			campaignGameSystemStarter.AddPlayerLine("minor_faction_talk_hostile_response_2", "minor_faction_talk_hostile_response", "minor_faction_talk_background", "{=EVLzPv1t}Hold - tell me more about yourselves.", null, null, 100, null, null);
			campaignGameSystemStarter.AddDialogLine("minor_faction_talk_background", "minor_faction_talk_background", "minor_faction_talk_background_next", "{=!}{MINOR_FACTION_SELFDESCRIPTION}", new ConversationSentence.OnConditionDelegate(this.conversation_minor_faction_set_selfdescription), null, 100, null);
			campaignGameSystemStarter.AddPlayerLine("minor_faction_talk_background_next_1", "minor_faction_talk_background_next", "minor_faction_talk_how_to_befriend", "{=vEsmC6M6}Is there any way we could not be enemies?", null, null, 100, null, null);
			campaignGameSystemStarter.AddPlayerLine("minor_faction_talk_background_next_2", "minor_faction_talk_background_next", "close_window", "{=p2WPU1CU}Very good then. Now I know whom I slay.", null, null, 100, null, null);
			campaignGameSystemStarter.AddDialogLine("minor_faction_talk_how_to_befriend", "minor_faction_talk_how_to_befriend", "minor_faction_talk_background_repeat_threat", "{=!}{MINOR_FACTION_HOWTOBEFRIEND}", new ConversationSentence.OnConditionDelegate(this.conversation_minor_faction_set_how_to_befriend), null, 100, null);
			campaignGameSystemStarter.AddDialogLine("minor_faction_talk_background_repeat_threat", "minor_faction_talk_background_repeat_threat", "minor_faction_talk_hostile_response", "{=ByOYHslS}That's enough talking for now. Make your choice.[if:convo_angry][[ib:aggressive]", null, null, 100, null);
		}

		// Token: 0x060039EC RID: 14828 RVA: 0x000EDF61 File Offset: 0x000EC161
		private bool bandit_start_defender_main_hero_defend_condition()
		{
			return MobileParty.MainParty.MemberRoster.TotalHealthyCount > 0 && !MobileParty.MainParty.IsInRaftState;
		}

		// Token: 0x060039ED RID: 14829 RVA: 0x000EDF84 File Offset: 0x000EC184
		private bool bandit_barter_successful_condition()
		{
			return Campaign.Current.BarterManager.LastBarterIsAccepted;
		}

		// Token: 0x060039EE RID: 14830 RVA: 0x000EDF95 File Offset: 0x000EC195
		private bool bandit_cheat_conversations_condition()
		{
			return Game.Current.IsDevelopmentMode;
		}

		// Token: 0x060039EF RID: 14831 RVA: 0x000EDFA4 File Offset: 0x000EC1A4
		private bool conversation_bandits_will_join_player_on_condition()
		{
			if (Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.PartnersInCrime) || MobileParty.ConversationParty.Party.MobileParty.IsInRaftState)
			{
				return true;
			}
			float resultNumber = Campaign.Current.Models.EncounterModel.GetBribeChance(MobileParty.ConversationParty, MobileParty.MainParty).ResultNumber;
			return MobileParty.ConversationParty.Party.RandomFloatWithSeed(3U, 1f) <= resultNumber;
		}

		// Token: 0x060039F0 RID: 14832 RVA: 0x000EE01C File Offset: 0x000EC21C
		private bool conversation_bandits_surrender_on_condition()
		{
			MobileParty conversationParty = MobileParty.ConversationParty;
			if (conversationParty != null && conversationParty.IsInRaftState)
			{
				return true;
			}
			if (this.GetPlayerInteraction(MobileParty.ConversationParty) == BanditInteractionsCampaignBehavior.PlayerInteraction.Hostile)
			{
				return false;
			}
			float surrenderChance = Campaign.Current.Models.EncounterModel.GetSurrenderChance(MobileParty.ConversationParty, MobileParty.MainParty);
			return MobileParty.ConversationParty.Party.RandomFloatWithSeed(4U, 1f) <= surrenderChance;
		}

		// Token: 0x060039F1 RID: 14833 RVA: 0x000EE088 File Offset: 0x000EC288
		private bool conversation_bandit_player_can_make_them_join_condition(out TextObject explanation)
		{
			return Campaign.Current.Models.EncounterModel.CanPlayerForceBanditsToJoin(out explanation);
		}

		// Token: 0x060039F2 RID: 14834 RVA: 0x000EE0A0 File Offset: 0x000EC2A0
		private bool bandit_neutral_greet_on_condition()
		{
			if (Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && PlayerEncounter.Current != null && PlayerEncounter.EncounteredMobileParty != null && PlayerEncounter.EncounteredMobileParty.MapFaction.IsBanditFaction && PlayerEncounter.PlayerIsAttacker && MobileParty.ConversationParty != null)
			{
				if (PlayerEncounter.EncounteredMobileParty.IsCurrentlyAtSea)
				{
					MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=BStP06aa}Ahoy! What do you want with us?", false);
					MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=!}I want you to heave to and prepare to be boarded, pirate!", false);
					int num = MBRandom.RandomInt(3);
					BanditInteractionsCampaignBehavior.PlayerInteraction playerInteraction = this.GetPlayerInteraction(MobileParty.ConversationParty);
					if (playerInteraction == BanditInteractionsCampaignBehavior.PlayerInteraction.PaidOffParty)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=OAvtaBiX}Keep your distance, you, or we'll charge you again for slowing our progress.", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=eP3tqvKX}We're here to fight. Ship your oars and surrender, or die!", false);
					}
					else if (playerInteraction != BanditInteractionsCampaignBehavior.PlayerInteraction.None)
					{
						if (PlayerEncounter.PlayerIsAttacker)
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=38DvG2ba}Yeah? What is it now?", false);
						}
						else
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=5laJ37D8}Back for more, are you?", false);
						}
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=eP3tqvKX}We're here to fight. Ship your oars and surrender, or die!", false);
					}
					else if (num == 1)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=p09odFwA}Ahoy! We're simple fishermen. We've no quarrel with you.", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=8Y23KVhW}But I have one with you, pirate! Give up now.", false);
					}
					else if (num == 2)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=3Dkh6QwL}Ahoy, you! Keep your distance or our sails will foul.", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=a2Kla47b}Heave to and prepare to be boarded, pirate!", false);
					}
				}
				else
				{
					MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=ZPj0ZAO7}Yeah? What do you want with us?", false);
					MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=5zUIQtTa}I want you to surrender or die, brigand!", false);
					int num2 = MBRandom.RandomInt(8);
					BanditInteractionsCampaignBehavior.PlayerInteraction playerInteraction2 = this.GetPlayerInteraction(MobileParty.ConversationParty);
					if (playerInteraction2 == BanditInteractionsCampaignBehavior.PlayerInteraction.PaidOffParty)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=Bm7U7TgG}If you're going to keep pestering us, traveller, we might need to take a bit more coin from you.", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=KRfcro26}We're here to fight. Surrender or die!", false);
					}
					else if (playerInteraction2 != BanditInteractionsCampaignBehavior.PlayerInteraction.None)
					{
						if (PlayerEncounter.PlayerIsAttacker)
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=38DvG2ba}Yeah? What is it now?", false);
						}
						else
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=5laJ37D8}Back for more, are you?", false);
						}
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=KRfcro26}We're here to fight. Surrender or die!", false);
					}
					else if (num2 == 1)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=cO61R3va}We've got no quarrel with you.", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=oJ6lpXmp}But I have one with you, brigand! Give up now.", false);
					}
					else if (num2 == 2)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=6XdHP9Pv}We're not looking for a fight.", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=fiLWg11t}Neither am I, if you surrender. Otherwise...", false);
					}
					else if (num2 == 3)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=GUiT211X}You got a problem?", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=idwOxnX5}Not if you give up now. If not, prepare to fight!", false);
					}
					else if (num2 == 4)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=mHBHKacJ}We're just harmless travellers...", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=A5IJmN0X}I think not, brigand. Surrender or die!", false);
						if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "mountain_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=8rgH8CGc}We're just harmless shepherds...", false);
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "forest_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=kRASveAC}We're just harmless foresters...", false);
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "sea_raiders")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=k96R57KM}We're just harmless traders...", false);
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "steppe_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=odzS6rhH}We're just harmless herdsmen...", false);
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "desert_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=Vttb0P15}We're just harmless nomads...", false);
						}
					}
					else if (num2 == 5)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=wSwzyr6M}Mess with us and we'll sell our lives dearly.", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=GLqb67cg}I don't care, brigand. Surrender or die!", false);
					}
					else if (num2 == 6)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=xQ0aBavD}Back off, stranger, unless you want trouble.", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=BwIT8F0k}I don't mind, brigand. Surrender or die!", false);
					}
					else if (num2 == 7)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=8yPqbZmm}You best back off. There's dozens more of us hiding, just waiting for our signal.", false);
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=ASRpFaGF}Nice try, brigand. Surrender or die!", false);
						if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "mountain_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=TXzZwb7n}You best back off. Scores of our brothers are just over that ridge over there, waiting for our signal.", false);
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "forest_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=lZj61xTm}You don't know who you're messing with. There are scores of our brothers hiding in the woods, just waiting for our signal to pepper you with arrows.", false);
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "sea_raiders")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=7Sp6aNYo}You best let us be. There's dozens more of us hiding here, just waiting for our signal.", false);
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "steppe_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=EUbdov2r}Back off, stranger. There's dozens more of us hiding in that gully over there, just waiting for our signal.", false);
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "desert_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=RWxYalkR}Be warned, stranger. There's dozens more of us hiding in that wadi over there, just waiting for our signal.", false);
						}
					}
				}
				return true;
			}
			return false;
		}

		// Token: 0x060039F3 RID: 14835 RVA: 0x000EE58D File Offset: 0x000EC78D
		private void bandit_barter_successful_on_consequence()
		{
			this.SetPlayerInteraction(MobileParty.ConversationParty, BanditInteractionsCampaignBehavior.PlayerInteraction.PaidOffParty);
		}

		// Token: 0x060039F4 RID: 14836 RVA: 0x000EE59B File Offset: 0x000EC79B
		private void bandit_neutral_greet_on_consequence()
		{
			if (this.GetPlayerInteraction(MobileParty.ConversationParty) != BanditInteractionsCampaignBehavior.PlayerInteraction.PaidOffParty)
			{
				this.SetPlayerInteraction(MobileParty.ConversationParty, BanditInteractionsCampaignBehavior.PlayerInteraction.Friendly);
			}
		}

		// Token: 0x060039F5 RID: 14837 RVA: 0x000EE5B7 File Offset: 0x000EC7B7
		private void conversation_bandit_set_hostile_on_consequence()
		{
			this.SetPlayerInteraction(MobileParty.ConversationParty, BanditInteractionsCampaignBehavior.PlayerInteraction.Hostile);
		}

		// Token: 0x060039F6 RID: 14838 RVA: 0x000EE5C8 File Offset: 0x000EC7C8
		private void GetMemberAndPrisonerRostersFromParties(List<MobileParty> parties, ref TroopRoster troopsTakenAsMember, ref TroopRoster troopsTakenAsPrisoner, bool doBanditsJoinPlayerSide)
		{
			foreach (MobileParty mobileParty in parties)
			{
				for (int i = 0; i < mobileParty.MemberRoster.Count; i++)
				{
					if (!mobileParty.MemberRoster.GetCharacterAtIndex(i).IsHero)
					{
						if (doBanditsJoinPlayerSide)
						{
							troopsTakenAsMember.AddToCounts(mobileParty.MemberRoster.GetCharacterAtIndex(i), mobileParty.MemberRoster.GetElementNumber(i), false, 0, 0, true, -1);
						}
						else
						{
							troopsTakenAsPrisoner.AddToCounts(mobileParty.MemberRoster.GetCharacterAtIndex(i), mobileParty.MemberRoster.GetElementNumber(i), false, 0, 0, true, -1);
						}
					}
				}
				for (int j = mobileParty.PrisonRoster.Count - 1; j > -1; j--)
				{
					CharacterObject characterAtIndex = mobileParty.PrisonRoster.GetCharacterAtIndex(j);
					if (!characterAtIndex.IsHero)
					{
						troopsTakenAsMember.AddToCounts(mobileParty.PrisonRoster.GetCharacterAtIndex(j), mobileParty.PrisonRoster.GetElementNumber(j), false, 0, 0, true, -1);
					}
					else if (characterAtIndex.HeroObject.Clan == Clan.PlayerClan)
					{
						if (doBanditsJoinPlayerSide)
						{
							EndCaptivityAction.ApplyByPeace(characterAtIndex.HeroObject, null);
						}
						else
						{
							EndCaptivityAction.ApplyByReleasedAfterBattle(characterAtIndex.HeroObject);
						}
						characterAtIndex.HeroObject.ChangeState(Hero.CharacterStates.Active);
						AddHeroToPartyAction.Apply(characterAtIndex.HeroObject, MobileParty.MainParty, true);
					}
					else if (Clan.PlayerClan.IsAtWarWith(characterAtIndex.HeroObject.Clan))
					{
						TransferPrisonerAction.Apply(characterAtIndex, mobileParty.Party, PartyBase.MainParty);
					}
				}
			}
		}

		// Token: 0x060039F7 RID: 14839 RVA: 0x000EE774 File Offset: 0x000EC974
		private void OpenRosterScreenAfterBanditEncounter(MobileParty conversationParty, bool doBanditsJoinPlayerSide)
		{
			if (!doBanditsJoinPlayerSide)
			{
				if (PlayerEncounter.Battle == null)
				{
					PlayerEncounter.StartBattle();
				}
				PlayerEncounter.Battle.SetOverrideWinner(PlayerEncounter.Battle.PlayerSide);
				PlayerEncounter.EnemySurrender = true;
				return;
			}
			List<MobileParty> list = new List<MobileParty>();
			List<MobileParty> list2 = new List<MobileParty>();
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list, list2);
			}
			if (!list.Contains(MobileParty.MainParty))
			{
				list.Add(MobileParty.MainParty);
			}
			if (PlayerEncounter.EncounteredMobileParty != null && !list2.Contains(PlayerEncounter.EncounteredMobileParty))
			{
				list2.Add(PlayerEncounter.EncounteredMobileParty);
			}
			TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
			TroopRoster troopRoster2 = TroopRoster.CreateDummyTroopRoster();
			this.GetMemberAndPrisonerRostersFromParties(list2, ref troopRoster2, ref troopRoster, doBanditsJoinPlayerSide);
			PartyScreenHelper.OpenScreenWithCondition(new IsTroopTransferableDelegate(this.IsTroopTransferable), new PartyPresentationDoneButtonConditionDelegate(this.DoneButtonCondition), new PartyPresentationDoneButtonDelegate(this.OnDoneClicked), null, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, PlayerEncounter.EncounteredParty.Name, troopRoster2.TotalManCount, false, false, PartyScreenHelper.PartyScreenMode.TroopsManage, troopRoster2, null);
			MBList<Ship> mblist = conversationParty.Ships.ToMBList<Ship>();
			if (!mblist.IsEmpty<Ship>())
			{
				PortStateHelper.OpenAsLoot(mblist);
			}
			for (int i = list2.Count - 1; i >= 0; i--)
			{
				MobileParty mobileParty = list2[i];
				CampaignEventDispatcher.Instance.OnBanditPartyRecruited(mobileParty);
				DestroyPartyAction.Apply(MobileParty.MainParty.Party, mobileParty);
			}
		}

		// Token: 0x060039F8 RID: 14840 RVA: 0x000EE8B6 File Offset: 0x000ECAB6
		private void conversation_bandits_surrender_on_consequence(MobileParty conversationParty)
		{
			this.OpenRosterScreenAfterBanditEncounter(conversationParty, false);
		}

		// Token: 0x060039F9 RID: 14841 RVA: 0x000EE8C0 File Offset: 0x000ECAC0
		private void conversation_bandits_join_player_party_on_consequence(MobileParty conversationParty)
		{
			this.OpenRosterScreenAfterBanditEncounter(conversationParty, true);
			PlayerEncounter.LeaveEncounter = true;
		}

		// Token: 0x060039FA RID: 14842 RVA: 0x000EE8D0 File Offset: 0x000ECAD0
		private bool OnDoneClicked(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty, PartyBase rightParty)
		{
			return true;
		}

		// Token: 0x060039FB RID: 14843 RVA: 0x000EE8D4 File Offset: 0x000ECAD4
		private Tuple<bool, TextObject> DoneButtonCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
		{
			foreach (TroopRosterElement troopRosterElement in rightMemberRoster.GetTroopRoster())
			{
				if (troopRosterElement.Character.IsHero && troopRosterElement.Character.HeroObject.HeroState == Hero.CharacterStates.Fugitive)
				{
					troopRosterElement.Character.HeroObject.ChangeState(Hero.CharacterStates.Active);
				}
			}
			return new Tuple<bool, TextObject>(true, null);
		}

		// Token: 0x060039FC RID: 14844 RVA: 0x000EE958 File Offset: 0x000ECB58
		private bool IsTroopTransferable(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
		{
			return true;
		}

		// Token: 0x060039FD RID: 14845 RVA: 0x000EE95C File Offset: 0x000ECB5C
		private bool bandit_start_defender_condition()
		{
			PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
			if ((Hero.OneToOneConversationHero != null && Hero.OneToOneConversationHero.MapFaction != null && !Hero.OneToOneConversationHero.MapFaction.IsBanditFaction) || encounteredParty == null || !encounteredParty.IsMobile || !encounteredParty.MapFaction.IsBanditFaction)
			{
				return false;
			}
			List<TextObject> list = new List<TextObject>();
			List<TextObject> list2 = new List<TextObject>();
			List<TextObject> list3 = new List<TextObject>();
			List<TextObject> list4 = new List<TextObject>();
			if (encounteredParty.MobileParty.IsCurrentlyAtSea)
			{
				for (int i = 1; i <= 3; i++)
				{
					TextObject item;
					if (GameTexts.TryGetText("str_piracy_threat", out item, i.ToString()))
					{
						list.Add(item);
						list2.Add(GameTexts.FindText("str_piracy_pay_agreement", i.ToString()));
						list3.Add(GameTexts.FindText("str_piracy_conclusion", i.ToString()));
						list4.Add(GameTexts.FindText("str_piracy_start_fight", i.ToString()));
					}
				}
			}
			else
			{
				for (int j = 1; j <= 6; j++)
				{
					TextObject item2;
					if (GameTexts.TryGetText("str_robbery_threat", out item2, j.ToString()))
					{
						list.Add(item2);
						list2.Add(GameTexts.FindText("str_robbery_pay_agreement", j.ToString()));
						list3.Add(GameTexts.FindText("str_robbery_conclusion", j.ToString()));
						list4.Add(GameTexts.FindText("str_robbery_start_fight", j.ToString()));
					}
				}
				for (int k = 1; k <= 6; k++)
				{
					string variation = encounteredParty.MapFaction.StringId + "_" + k.ToString();
					TextObject item3;
					if (GameTexts.TryGetText("str_robbery_threat", out item3, variation))
					{
						for (int l = 0; l < 3; l++)
						{
							list.Add(item3);
							list2.Add(GameTexts.FindText("str_robbery_pay_agreement", variation));
							list3.Add(GameTexts.FindText("str_robbery_conclusion", variation));
							list4.Add(GameTexts.FindText("str_robbery_start_fight", variation));
						}
					}
				}
			}
			int index = MBRandom.RandomInt(0, list.Count);
			MBTextManager.SetTextVariable("ROBBERY_THREAT", list[index], false);
			MBTextManager.SetTextVariable("ROBBERY_PAY_AGREEMENT", list2[index], false);
			MBTextManager.SetTextVariable("ROBBERY_CONCLUSION", list3[index], false);
			MBTextManager.SetTextVariable("ROBBERY_START_FIGHT", list4[index], false);
			List<MobileParty> list5 = new List<MobileParty>();
			List<MobileParty> list6 = new List<MobileParty>();
			if (PlayerEncounter.Current != null)
			{
				PlayerEncounter.Current.FindAllNpcPartiesWhoWillJoinEvent(list5, list6);
			}
			if (!list5.Contains(MobileParty.MainParty))
			{
				list5.Add(MobileParty.MainParty);
			}
			if (MobileParty.ConversationParty != null && !list6.Contains(MobileParty.ConversationParty))
			{
				list6.Add(MobileParty.ConversationParty);
			}
			float num = 0f;
			foreach (MobileParty mobileParty in list5)
			{
				num += mobileParty.Party.CalculateCurrentStrength();
			}
			float num2 = 0f;
			foreach (MobileParty mobileParty2 in list6)
			{
				num2 += mobileParty2.Party.CalculateCurrentStrength();
			}
			float num3 = (num2 + 1f) / (num + 1f);
			int num4 = Hero.MainHero.Gold / 100;
			double num5 = 2.0 * (double)MathF.Max(0f, MathF.Min(6f, num3 - 1f));
			float num6 = 0f;
			Settlement settlement = SettlementHelper.FindNearestSettlementToMobileParty(encounteredParty.MobileParty, MobileParty.NavigationType.All, (Settlement x) => x.IsTown || x.IsVillage);
			SettlementComponent settlementComponent;
			if (settlement.IsTown)
			{
				settlementComponent = settlement.Town;
			}
			else
			{
				settlementComponent = settlement.Village;
			}
			foreach (ItemRosterElement itemRosterElement in MobileParty.MainParty.ItemRoster)
			{
				num6 += (float)(settlementComponent.GetItemPrice(itemRosterElement.EquipmentElement, MobileParty.MainParty, true) * itemRosterElement.Amount);
			}
			float num7 = num6 / 100f;
			float num8 = 1f + 2f * MathF.Max(0f, MathF.Min(6f, num3 - 1f));
			BanditInteractionsCampaignBehavior._goldAmount = (int)((double)num4 * num5 + (double)(num7 * num8) + 100.0);
			MBTextManager.SetTextVariable("AMOUNT", BanditInteractionsCampaignBehavior._goldAmount.ToString(), false);
			return encounteredParty.IsMobile && encounteredParty.MapFaction.IsBanditFaction && PlayerEncounter.PlayerIsDefender;
		}

		// Token: 0x060039FE RID: 14846 RVA: 0x000EEE30 File Offset: 0x000ED030
		private bool bandit_start_barter_condition()
		{
			return PlayerEncounter.Current != null && PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender;
		}

		// Token: 0x060039FF RID: 14847 RVA: 0x000EEE48 File Offset: 0x000ED048
		private void bandit_start_barter_consequence()
		{
			BarterManager instance = BarterManager.Instance;
			Hero mainHero = Hero.MainHero;
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			PartyBase mainParty = PartyBase.MainParty;
			MobileParty conversationParty = MobileParty.ConversationParty;
			PartyBase otherParty = ((conversationParty != null) ? conversationParty.Party : null);
			Hero beneficiaryOfOtherHero = null;
			BarterManager.BarterContextInitializer initContext = new BarterManager.BarterContextInitializer(BarterManager.Instance.InitializeSafePassageBarterContext);
			int persuasionCostReduction = 0;
			bool isAIBarter = false;
			Barterable[] array = new Barterable[1];
			int num = 0;
			Hero originalOwner = null;
			Hero mainHero2 = Hero.MainHero;
			MobileParty conversationParty2 = MobileParty.ConversationParty;
			array[num] = new SafePassageBarterable(originalOwner, mainHero2, (conversationParty2 != null) ? conversationParty2.Party : null, PartyBase.MainParty);
			instance.StartBarterOffer(mainHero, oneToOneConversationHero, mainParty, otherParty, beneficiaryOfOtherHero, initContext, persuasionCostReduction, isAIBarter, array);
		}

		// Token: 0x06003A00 RID: 14848 RVA: 0x000EEEBC File Offset: 0x000ED0BC
		private bool conversation_minor_faction_hostile_on_condition()
		{
			if (MapEvent.PlayerMapEvent != null)
			{
				foreach (PartyBase partyBase in MapEvent.PlayerMapEvent.InvolvedParties)
				{
					if (PartyBase.MainParty.Side == BattleSideEnum.Attacker && partyBase.IsMobile && partyBase.MobileParty.IsBandit && partyBase.MapFaction.IsMinorFaction)
					{
						string text = partyBase.MapFaction.StringId + "_encounter";
						if (FactionManager.IsAtWarAgainstFaction(partyBase.MapFaction, Hero.MainHero.MapFaction))
						{
							text += "_hostile";
						}
						else
						{
							text += "_neutral";
						}
						MBTextManager.SetTextVariable("MINOR_FACTION_ENCOUNTER", GameTexts.FindText(text, null), false);
						return true;
					}
				}
				return false;
			}
			return false;
		}

		// Token: 0x06003A01 RID: 14849 RVA: 0x000EEFA8 File Offset: 0x000ED1A8
		private bool conversation_minor_faction_set_selfdescription()
		{
			foreach (PartyBase partyBase in MapEvent.PlayerMapEvent.InvolvedParties)
			{
				if (PartyBase.MainParty.Side == BattleSideEnum.Attacker && partyBase.IsMobile && partyBase.MobileParty.IsBandit && partyBase.MapFaction.IsMinorFaction)
				{
					string id = partyBase.MapFaction.StringId + "_selfdescription";
					MBTextManager.SetTextVariable("MINOR_FACTION_SELFDESCRIPTION", GameTexts.FindText(id, null), false);
					return true;
				}
			}
			return true;
		}

		// Token: 0x06003A02 RID: 14850 RVA: 0x000EF050 File Offset: 0x000ED250
		private bool conversation_minor_faction_set_how_to_befriend()
		{
			foreach (PartyBase partyBase in MapEvent.PlayerMapEvent.InvolvedParties)
			{
				if (PartyBase.MainParty.Side == BattleSideEnum.Attacker && partyBase.IsMobile && partyBase.MobileParty.IsBandit && partyBase.MapFaction.IsMinorFaction)
				{
					string id = partyBase.MapFaction.StringId + "_how_to_befriend";
					MBTextManager.SetTextVariable("MINOR_FACTION_HOWTOBEFRIEND", GameTexts.FindText(id, null), false);
					return true;
				}
			}
			return true;
		}

		// Token: 0x06003A03 RID: 14851 RVA: 0x000EF0F8 File Offset: 0x000ED2F8
		private bool bandit_attacker_try_leave_condition()
		{
			return PlayerEncounter.EncounteredParty != null && ((PlayerEncounter.EncounteredParty.CalculateCurrentStrength() <= PartyBase.MainParty.CalculateCurrentStrength() && !MobileParty.MainParty.IsInRaftState) || this.GetPlayerInteraction(PlayerEncounter.EncounteredMobileParty) == BanditInteractionsCampaignBehavior.PlayerInteraction.PaidOffParty || this.GetPlayerInteraction(PlayerEncounter.EncounteredMobileParty) == BanditInteractionsCampaignBehavior.PlayerInteraction.Friendly);
		}

		// Token: 0x04001205 RID: 4613
		private Dictionary<MobileParty, BanditInteractionsCampaignBehavior.PlayerInteraction> _interactedBandits = new Dictionary<MobileParty, BanditInteractionsCampaignBehavior.PlayerInteraction>();

		// Token: 0x04001206 RID: 4614
		private static int _goldAmount;

		// Token: 0x020007AF RID: 1967
		public class BanditInteractionsCampaignBehaviorTypeDefiner : SaveableTypeDefiner
		{
			// Token: 0x0600623C RID: 25148 RVA: 0x001BACFC File Offset: 0x001B8EFC
			public BanditInteractionsCampaignBehaviorTypeDefiner()
				: base(70000)
			{
			}

			// Token: 0x0600623D RID: 25149 RVA: 0x001BAD09 File Offset: 0x001B8F09
			protected override void DefineEnumTypes()
			{
				base.AddEnumDefinition(typeof(BanditInteractionsCampaignBehavior.PlayerInteraction), 1, null);
			}

			// Token: 0x0600623E RID: 25150 RVA: 0x001BAD1D File Offset: 0x001B8F1D
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(Dictionary<MobileParty, BanditInteractionsCampaignBehavior.PlayerInteraction>));
			}
		}

		// Token: 0x020007B0 RID: 1968
		private enum PlayerInteraction
		{
			// Token: 0x04001EBB RID: 7867
			None,
			// Token: 0x04001EBC RID: 7868
			Friendly,
			// Token: 0x04001EBD RID: 7869
			PaidOffParty,
			// Token: 0x04001EBE RID: 7870
			Hostile
		}
	}
}
