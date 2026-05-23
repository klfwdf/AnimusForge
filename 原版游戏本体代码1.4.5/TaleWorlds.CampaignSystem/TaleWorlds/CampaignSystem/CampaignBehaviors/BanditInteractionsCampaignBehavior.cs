using System;
using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
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

namespace TaleWorlds.CampaignSystem.CampaignBehaviors;

public class BanditInteractionsCampaignBehavior : CampaignBehaviorBase
{
	public class BanditInteractionsCampaignBehaviorTypeDefiner : SaveableTypeDefiner
	{
		public BanditInteractionsCampaignBehaviorTypeDefiner()
			: base(70000)
		{
		}

		protected override void DefineEnumTypes()
		{
			AddEnumDefinition(typeof(PlayerInteraction), 1);
		}

		protected override void DefineContainerDefinitions()
		{
			ConstructContainerDefinition(typeof(Dictionary<MobileParty, PlayerInteraction>));
		}
	}

	private enum PlayerInteraction
	{
		None,
		Friendly,
		PaidOffParty,
		Hostile
	}

	private Dictionary<MobileParty, PlayerInteraction> _interactedBandits = new Dictionary<MobileParty, PlayerInteraction>();

	private static int _goldAmount;

	public BanditInteractionsCampaignBehavior()
		: base("BanditsCampaignBehavior")
	{
	}

	public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddDialogs(campaignGameStarter);
	}

	public override void RegisterEvents()
	{
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnPartyDestroyed);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_interactedBandits", ref _interactedBandits);
	}

	private void OnPartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		if (_interactedBandits.ContainsKey(mobileParty))
		{
			_interactedBandits.Remove(mobileParty);
		}
	}

	private void SetPlayerInteraction(MobileParty mobileParty, PlayerInteraction interaction)
	{
		if (_interactedBandits.ContainsKey(mobileParty))
		{
			_interactedBandits[mobileParty] = interaction;
		}
		else
		{
			_interactedBandits.Add(mobileParty, interaction);
		}
	}

	private PlayerInteraction GetPlayerInteraction(MobileParty mobileParty)
	{
		if (_interactedBandits.TryGetValue(mobileParty, out var value))
		{
			return value;
		}
		return PlayerInteraction.None;
	}

	protected void AddDialogs(CampaignGameStarter campaignGameSystemStarter)
	{
		campaignGameSystemStarter.AddDialogLine("bandit_start_defender", "start", "bandit_defender", "{=!}{ROBBERY_THREAT}", bandit_start_defender_condition, null);
		campaignGameSystemStarter.AddPlayerLine("bandit_start_defender_1", "bandit_defender", "bandit_start_fight", "{=DEnFOGhS}Fight me if you dare!", bandit_start_defender_main_hero_defend_condition, null);
		campaignGameSystemStarter.AddPlayerLine("bandit_start_defender_3", "bandit_defender", "bandit_start_fight", "{=LLEffOga}We're in no shape to fight. We're at your mercy.", () => !bandit_start_defender_main_hero_defend_condition(), null);
		campaignGameSystemStarter.AddPlayerLine("bandit_start_defender_2", "bandit_defender", "barter_with_bandit_prebarter", "{=aQYMefHU}Maybe we can work out something.", bandit_start_barter_condition, null);
		campaignGameSystemStarter.AddDialogLine("bandit_start_fight", "bandit_start_fight", "close_window", "{=!}{ROBBERY_START_FIGHT}", null, conversation_bandit_set_hostile_on_consequence);
		campaignGameSystemStarter.AddDialogLine("barter_with_bandit_prebarter", "barter_with_bandit_prebarter", "barter_with_bandit_screen", "{=!}{ROBBERY_PAY_AGREEMENT}", null, null);
		campaignGameSystemStarter.AddDialogLine("barter_with_bandit_screen", "barter_with_bandit_screen", "barter_with_bandit_postbarter", "{=!}Barter screen goes here", null, bandit_start_barter_consequence);
		campaignGameSystemStarter.AddDialogLine("barter_with_bandit_postbarter_1", "barter_with_bandit_postbarter", "close_window", "{=!}{ROBBERY_CONCLUSION}", bandit_barter_successful_condition, bandit_barter_successful_on_consequence);
		campaignGameSystemStarter.AddDialogLine("barter_with_bandit_postbarter_2", "barter_with_bandit_postbarter", "close_window", "{=!}{ROBBERY_START_FIGHT}", () => !bandit_barter_successful_condition(), conversation_bandit_set_hostile_on_consequence);
		campaignGameSystemStarter.AddDialogLine("bandit_start_attacker", "start", "bandit_attacker", "{=!}{BANDIT_NEUTRAL_GREETING}", bandit_neutral_greet_on_condition, bandit_neutral_greet_on_consequence);
		campaignGameSystemStarter.AddPlayerLine("common_encounter_ultimatum", "bandit_attacker", "common_encounter_ultimatum_answer", "{=!}{BANDIT_ULTIMATUM}", null, null);
		campaignGameSystemStarter.AddPlayerLine("common_encounter_fight", "bandit_attacker", "bandit_attacker_leave", "{=3W3eEIIZ}Never mind. You can go.", null, null);
		campaignGameSystemStarter.AddDialogLine("common_encounter_ultimatum_surrender", "common_encounter_ultimatum_answer", "common_bandit_surrender_answer", "{=ji2eenPE}All right - we give up. We can't fight you. Maybe the likes of us don't deserve mercy, but... show what mercy you can.", conversation_bandits_surrender_on_condition, null);
		campaignGameSystemStarter.AddDialogLine("common_encounter_ultimatum_war", "common_encounter_ultimatum_answer", "close_window", "{=n99VA8KP}You'll never take us alive![if:convo_angry][ib:aggressive]", null, conversation_bandit_set_hostile_on_consequence);
		campaignGameSystemStarter.AddPlayerLine("common_bandit_join_player_declined_1", "bandits_we_can_join_you", "player_do_not_let_bandits_to_join", "{=JZvywHNy}You think I'm daft? I'm not trusting you an inch.", null, null);
		campaignGameSystemStarter.AddPlayerLine("common_bandit_join_player_declined_2", "bandits_we_can_join_you", "player_do_not_let_bandits_to_join", "{=z0WacPaW}No, justice demands you pay for your crimes.", null, conversation_bandit_set_hostile_on_consequence);
		campaignGameSystemStarter.AddPlayerLine("common_bandit_join_player_leave", "bandits_we_can_join_you", "bandit_attacker_leave", "{=D33fIGQe}Never mind.", null, null);
		campaignGameSystemStarter.AddDialogLine("common_encounter_declined_looters_to_join_war_surrender", "player_do_not_let_bandits_to_join", "common_bandit_surrender_answer", "{=ji2eenPE}All right - we give up. We can't fight you. Maybe the likes of us don't deserve mercy, but... show what mercy you can.", conversation_bandits_surrender_on_condition, null);
		campaignGameSystemStarter.AddPlayerLine("common_bandit_surrender_accepted", "common_bandit_surrender_answer", "close_window", "{=cbzJRaDJ}You are my prisoner now!", null, delegate
		{
			MobileParty party = MobileParty.ConversationParty;
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
			{
				conversation_bandits_surrender_on_consequence(party);
			};
		});
		campaignGameSystemStarter.AddPlayerLine("common_bandit_surrender_declined", "common_bandit_surrender_answer", "player_do_not_let_bandits_to_join", "{=z0WacPaW}No, justice demands you pay for your crimes.", null, conversation_bandit_set_hostile_on_consequence);
		campaignGameSystemStarter.AddPlayerLine("common_bandit_surrender_join_offer", "common_bandit_surrender_answer", "close_window", "{=e87vsXfI}You will earn back your lives by serving under my command.", null, delegate
		{
			MobileParty party = MobileParty.ConversationParty;
			Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
			{
				conversation_bandits_join_player_party_on_consequence(party);
			};
		}, 100, conversation_bandit_player_can_make_them_join_condition);
		campaignGameSystemStarter.AddDialogLine("common_encounter_ultimatum_war_2", "player_do_not_let_bandits_to_join", "close_window", "{=LDhU5urT}So that's how it is, is it? Right then - I'll make one of you bleed before I go down.[if:convo_angry][ib:aggressive]", null, null);
		campaignGameSystemStarter.AddDialogLine("bandit_attacker_try_leave_success", "bandit_attacker_leave", "close_window", "{=IDdyHef9}We'll be on our way, then!", bandit_attacker_try_leave_condition, delegate
		{
			PlayerEncounter.LeaveEncounter = true;
		});
		campaignGameSystemStarter.AddDialogLine("bandit_attacker_try_leave_fail", "bandit_attacker_leave", "bandit_defender", "{=6Wc1XErN}Wait, wait... You're not going anywhere just yet.", () => !bandit_attacker_try_leave_condition(), null);
		campaignGameSystemStarter.AddDialogLine("minor_faction_hostile", "start", "minor_faction_talk_hostile_response", "{=!}{MINOR_FACTION_ENCOUNTER}", conversation_minor_faction_hostile_on_condition, null);
		campaignGameSystemStarter.AddPlayerLine("minor_faction_talk_hostile_response_1", "minor_faction_talk_hostile_response", "close_window", "{=aaf5R99a}I'll give you nothing but cold steel, you scum!", null, null);
		campaignGameSystemStarter.AddPlayerLine("minor_faction_talk_hostile_response_2", "minor_faction_talk_hostile_response", "minor_faction_talk_background", "{=EVLzPv1t}Hold - tell me more about yourselves.", null, null);
		campaignGameSystemStarter.AddDialogLine("minor_faction_talk_background", "minor_faction_talk_background", "minor_faction_talk_background_next", "{=!}{MINOR_FACTION_SELFDESCRIPTION}", conversation_minor_faction_set_selfdescription, null);
		campaignGameSystemStarter.AddPlayerLine("minor_faction_talk_background_next_1", "minor_faction_talk_background_next", "minor_faction_talk_how_to_befriend", "{=vEsmC6M6}Is there any way we could not be enemies?", null, null);
		campaignGameSystemStarter.AddPlayerLine("minor_faction_talk_background_next_2", "minor_faction_talk_background_next", "close_window", "{=p2WPU1CU}Very good then. Now I know whom I slay.", null, null);
		campaignGameSystemStarter.AddDialogLine("minor_faction_talk_how_to_befriend", "minor_faction_talk_how_to_befriend", "minor_faction_talk_background_repeat_threat", "{=!}{MINOR_FACTION_HOWTOBEFRIEND}", conversation_minor_faction_set_how_to_befriend, null);
		campaignGameSystemStarter.AddDialogLine("minor_faction_talk_background_repeat_threat", "minor_faction_talk_background_repeat_threat", "minor_faction_talk_hostile_response", "{=ByOYHslS}That's enough talking for now. Make your choice.[if:convo_angry][[ib:aggressive]", null, null);
	}

	private bool bandit_start_defender_main_hero_defend_condition()
	{
		if (MobileParty.MainParty.MemberRoster.TotalHealthyCount > 0)
		{
			return !MobileParty.MainParty.IsInRaftState;
		}
		return false;
	}

	private bool bandit_barter_successful_condition()
	{
		return Campaign.Current.BarterManager.LastBarterIsAccepted;
	}

	private bool bandit_cheat_conversations_condition()
	{
		return Game.Current.IsDevelopmentMode;
	}

	private bool conversation_bandits_will_join_player_on_condition()
	{
		if (Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.PartnersInCrime) || MobileParty.ConversationParty.Party.MobileParty.IsInRaftState)
		{
			return true;
		}
		float resultNumber = Campaign.Current.Models.EncounterModel.GetBribeChance(MobileParty.ConversationParty, MobileParty.MainParty).ResultNumber;
		if (MobileParty.ConversationParty.Party.RandomFloatWithSeed(3u, 1f) > resultNumber)
		{
			return false;
		}
		return true;
	}

	private bool conversation_bandits_surrender_on_condition()
	{
		MobileParty conversationParty = MobileParty.ConversationParty;
		if (conversationParty != null && conversationParty.IsInRaftState)
		{
			return true;
		}
		if (GetPlayerInteraction(MobileParty.ConversationParty) == PlayerInteraction.Hostile)
		{
			return false;
		}
		float surrenderChance = Campaign.Current.Models.EncounterModel.GetSurrenderChance(MobileParty.ConversationParty, MobileParty.MainParty);
		if (MobileParty.ConversationParty.Party.RandomFloatWithSeed(4u, 1f) > surrenderChance)
		{
			return false;
		}
		return true;
	}

	private bool conversation_bandit_player_can_make_them_join_condition(out TextObject explanation)
	{
		return Campaign.Current.Models.EncounterModel.CanPlayerForceBanditsToJoin(out explanation);
	}

	private bool bandit_neutral_greet_on_condition()
	{
		if (Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && PlayerEncounter.Current != null && PlayerEncounter.EncounteredMobileParty != null && PlayerEncounter.EncounteredMobileParty.MapFaction.IsBanditFaction && PlayerEncounter.PlayerIsAttacker && MobileParty.ConversationParty != null)
		{
			if (PlayerEncounter.EncounteredMobileParty.IsCurrentlyAtSea)
			{
				MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=BStP06aa}Ahoy! What do you want with us?");
				MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=!}I want you to heave to and prepare to be boarded, pirate!");
				int num = MBRandom.RandomInt(3);
				switch (GetPlayerInteraction(MobileParty.ConversationParty))
				{
				case PlayerInteraction.PaidOffParty:
					MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=OAvtaBiX}Keep your distance, you, or we'll charge you again for slowing our progress.");
					MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=eP3tqvKX}We're here to fight. Ship your oars and surrender, or die!");
					break;
				default:
					if (PlayerEncounter.PlayerIsAttacker)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=38DvG2ba}Yeah? What is it now?");
					}
					else
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=5laJ37D8}Back for more, are you?");
					}
					MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=eP3tqvKX}We're here to fight. Ship your oars and surrender, or die!");
					break;
				case PlayerInteraction.None:
					switch (num)
					{
					case 1:
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=p09odFwA}Ahoy! We're simple fishermen. We've no quarrel with you.");
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=8Y23KVhW}But I have one with you, pirate! Give up now.");
						break;
					case 2:
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=3Dkh6QwL}Ahoy, you! Keep your distance or our sails will foul.");
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=a2Kla47b}Heave to and prepare to be boarded, pirate!");
						break;
					}
					break;
				}
			}
			else
			{
				MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=ZPj0ZAO7}Yeah? What do you want with us?");
				MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=5zUIQtTa}I want you to surrender or die, brigand!");
				int num2 = MBRandom.RandomInt(8);
				switch (GetPlayerInteraction(MobileParty.ConversationParty))
				{
				case PlayerInteraction.PaidOffParty:
					MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=Bm7U7TgG}If you're going to keep pestering us, traveller, we might need to take a bit more coin from you.");
					MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=KRfcro26}We're here to fight. Surrender or die!");
					break;
				default:
					if (PlayerEncounter.PlayerIsAttacker)
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=38DvG2ba}Yeah? What is it now?");
					}
					else
					{
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=5laJ37D8}Back for more, are you?");
					}
					MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=KRfcro26}We're here to fight. Surrender or die!");
					break;
				case PlayerInteraction.None:
					switch (num2)
					{
					case 1:
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=cO61R3va}We've got no quarrel with you.");
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=oJ6lpXmp}But I have one with you, brigand! Give up now.");
						break;
					case 2:
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=6XdHP9Pv}We're not looking for a fight.");
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=fiLWg11t}Neither am I, if you surrender. Otherwise...");
						break;
					case 3:
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=GUiT211X}You got a problem?");
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=idwOxnX5}Not if you give up now. If not, prepare to fight!");
						break;
					case 4:
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=mHBHKacJ}We're just harmless travellers...");
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=A5IJmN0X}I think not, brigand. Surrender or die!");
						if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "mountain_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=8rgH8CGc}We're just harmless shepherds...");
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "forest_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=kRASveAC}We're just harmless foresters...");
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "sea_raiders")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=k96R57KM}We're just harmless traders...");
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "steppe_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=odzS6rhH}We're just harmless herdsmen...");
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "desert_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=Vttb0P15}We're just harmless nomads...");
						}
						break;
					case 5:
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=wSwzyr6M}Mess with us and we'll sell our lives dearly.");
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=GLqb67cg}I don't care, brigand. Surrender or die!");
						break;
					case 6:
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=xQ0aBavD}Back off, stranger, unless you want trouble.");
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=BwIT8F0k}I don't mind, brigand. Surrender or die!");
						break;
					case 7:
						MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=8yPqbZmm}You best back off. There's dozens more of us hiding, just waiting for our signal.");
						MBTextManager.SetTextVariable("BANDIT_ULTIMATUM", "{=ASRpFaGF}Nice try, brigand. Surrender or die!");
						if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "mountain_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=TXzZwb7n}You best back off. Scores of our brothers are just over that ridge over there, waiting for our signal.");
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "forest_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=lZj61xTm}You don't know who you're messing with. There are scores of our brothers hiding in the woods, just waiting for our signal to pepper you with arrows.");
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "sea_raiders")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=7Sp6aNYo}You best let us be. There's dozens more of us hiding here, just waiting for our signal.");
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "steppe_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=EUbdov2r}Back off, stranger. There's dozens more of us hiding in that gully over there, just waiting for our signal.");
						}
						else if (PlayerEncounter.EncounteredMobileParty.MapFaction.StringId == "desert_bandits")
						{
							MBTextManager.SetTextVariable("BANDIT_NEUTRAL_GREETING", "{=RWxYalkR}Be warned, stranger. There's dozens more of us hiding in that wadi over there, just waiting for our signal.");
						}
						break;
					}
					break;
				}
			}
			return true;
		}
		return false;
	}

	private void bandit_barter_successful_on_consequence()
	{
		SetPlayerInteraction(MobileParty.ConversationParty, PlayerInteraction.PaidOffParty);
	}

	private void bandit_neutral_greet_on_consequence()
	{
		if (GetPlayerInteraction(MobileParty.ConversationParty) != PlayerInteraction.PaidOffParty)
		{
			SetPlayerInteraction(MobileParty.ConversationParty, PlayerInteraction.Friendly);
		}
	}

	private void conversation_bandit_set_hostile_on_consequence()
	{
		SetPlayerInteraction(MobileParty.ConversationParty, PlayerInteraction.Hostile);
	}

	private void GetMemberAndPrisonerRostersFromParties(List<MobileParty> parties, ref TroopRoster troopsTakenAsMember, ref TroopRoster troopsTakenAsPrisoner, bool doBanditsJoinPlayerSide)
	{
		foreach (MobileParty party in parties)
		{
			for (int i = 0; i < party.MemberRoster.Count; i++)
			{
				if (!party.MemberRoster.GetCharacterAtIndex(i).IsHero)
				{
					if (doBanditsJoinPlayerSide)
					{
						troopsTakenAsMember.AddToCounts(party.MemberRoster.GetCharacterAtIndex(i), party.MemberRoster.GetElementNumber(i));
					}
					else
					{
						troopsTakenAsPrisoner.AddToCounts(party.MemberRoster.GetCharacterAtIndex(i), party.MemberRoster.GetElementNumber(i));
					}
				}
			}
			for (int num = party.PrisonRoster.Count - 1; num > -1; num--)
			{
				CharacterObject characterAtIndex = party.PrisonRoster.GetCharacterAtIndex(num);
				if (!characterAtIndex.IsHero)
				{
					troopsTakenAsMember.AddToCounts(party.PrisonRoster.GetCharacterAtIndex(num), party.PrisonRoster.GetElementNumber(num));
				}
				else if (characterAtIndex.HeroObject.Clan == Clan.PlayerClan)
				{
					if (doBanditsJoinPlayerSide)
					{
						EndCaptivityAction.ApplyByPeace(characterAtIndex.HeroObject);
					}
					else
					{
						EndCaptivityAction.ApplyByReleasedAfterBattle(characterAtIndex.HeroObject);
					}
					characterAtIndex.HeroObject.ChangeState(Hero.CharacterStates.Active);
					AddHeroToPartyAction.Apply(characterAtIndex.HeroObject, MobileParty.MainParty);
				}
				else if (Clan.PlayerClan.IsAtWarWith(characterAtIndex.HeroObject.Clan))
				{
					TransferPrisonerAction.Apply(characterAtIndex, party.Party, PartyBase.MainParty);
				}
			}
		}
	}

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
		TroopRoster troopsTakenAsPrisoner = TroopRoster.CreateDummyTroopRoster();
		TroopRoster troopsTakenAsMember = TroopRoster.CreateDummyTroopRoster();
		GetMemberAndPrisonerRostersFromParties(list2, ref troopsTakenAsMember, ref troopsTakenAsPrisoner, doBanditsJoinPlayerSide);
		PartyScreenHelper.OpenScreenWithCondition(IsTroopTransferable, DoneButtonCondition, OnDoneClicked, null, PartyScreenLogic.TransferState.Transferable, PartyScreenLogic.TransferState.Transferable, PlayerEncounter.EncounteredParty.Name, troopsTakenAsMember.TotalManCount, showProgressBar: false, isDonating: false, PartyScreenHelper.PartyScreenMode.TroopsManage, troopsTakenAsMember);
		MBList<Ship> mBList = conversationParty.Ships.ToMBList();
		if (!mBList.IsEmpty())
		{
			PortStateHelper.OpenAsLoot(mBList);
		}
		for (int num = list2.Count - 1; num >= 0; num--)
		{
			MobileParty mobileParty = list2[num];
			CampaignEventDispatcher.Instance.OnBanditPartyRecruited(mobileParty);
			DestroyPartyAction.Apply(MobileParty.MainParty.Party, mobileParty);
		}
	}

	private void conversation_bandits_surrender_on_consequence(MobileParty conversationParty)
	{
		OpenRosterScreenAfterBanditEncounter(conversationParty, doBanditsJoinPlayerSide: false);
	}

	private void conversation_bandits_join_player_party_on_consequence(MobileParty conversationParty)
	{
		OpenRosterScreenAfterBanditEncounter(conversationParty, doBanditsJoinPlayerSide: true);
		PlayerEncounter.LeaveEncounter = true;
	}

	private bool OnDoneClicked(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty, PartyBase rightParty)
	{
		return true;
	}

	private Tuple<bool, TextObject> DoneButtonCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
	{
		foreach (TroopRosterElement item in rightMemberRoster.GetTroopRoster())
		{
			if (item.Character.IsHero && item.Character.HeroObject.HeroState == Hero.CharacterStates.Fugitive)
			{
				item.Character.HeroObject.ChangeState(Hero.CharacterStates.Active);
			}
		}
		return new Tuple<bool, TextObject>(item1: true, null);
	}

	private bool IsTroopTransferable(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty)
	{
		return true;
	}

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
				if (GameTexts.TryGetText("str_piracy_threat", out var textObject, i.ToString()))
				{
					list.Add(textObject);
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
				if (GameTexts.TryGetText("str_robbery_threat", out var textObject2, j.ToString()))
				{
					list.Add(textObject2);
					list2.Add(GameTexts.FindText("str_robbery_pay_agreement", j.ToString()));
					list3.Add(GameTexts.FindText("str_robbery_conclusion", j.ToString()));
					list4.Add(GameTexts.FindText("str_robbery_start_fight", j.ToString()));
				}
			}
			for (int k = 1; k <= 6; k++)
			{
				string variation = encounteredParty.MapFaction.StringId + "_" + k;
				if (GameTexts.TryGetText("str_robbery_threat", out var textObject3, variation))
				{
					for (int l = 0; l < 3; l++)
					{
						list.Add(textObject3);
						list2.Add(GameTexts.FindText("str_robbery_pay_agreement", variation));
						list3.Add(GameTexts.FindText("str_robbery_conclusion", variation));
						list4.Add(GameTexts.FindText("str_robbery_start_fight", variation));
					}
				}
			}
		}
		int index = MBRandom.RandomInt(0, list.Count);
		MBTextManager.SetTextVariable("ROBBERY_THREAT", list[index]);
		MBTextManager.SetTextVariable("ROBBERY_PAY_AGREEMENT", list2[index]);
		MBTextManager.SetTextVariable("ROBBERY_CONCLUSION", list3[index]);
		MBTextManager.SetTextVariable("ROBBERY_START_FIGHT", list4[index]);
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
		foreach (MobileParty item in list5)
		{
			num += item.Party.CalculateCurrentStrength();
		}
		float num2 = 0f;
		foreach (MobileParty item2 in list6)
		{
			num2 += item2.Party.CalculateCurrentStrength();
		}
		float num3 = (num2 + 1f) / (num + 1f);
		int num4 = Hero.MainHero.Gold / 100;
		double num5 = 2.0 * (double)TaleWorlds.Library.MathF.Max(0f, TaleWorlds.Library.MathF.Min(6f, num3 - 1f));
		float num6 = 0f;
		Settlement settlement = SettlementHelper.FindNearestSettlementToMobileParty(encounteredParty.MobileParty, MobileParty.NavigationType.All, (Settlement x) => x.IsTown || x.IsVillage);
		SettlementComponent settlementComponent = ((!settlement.IsTown) ? ((SettlementComponent)settlement.Village) : ((SettlementComponent)settlement.Town));
		foreach (ItemRosterElement item3 in MobileParty.MainParty.ItemRoster)
		{
			num6 += (float)(settlementComponent.GetItemPrice(item3.EquipmentElement, MobileParty.MainParty, isSelling: true) * item3.Amount);
		}
		float num7 = num6 / 100f;
		float num8 = 1f + 2f * TaleWorlds.Library.MathF.Max(0f, TaleWorlds.Library.MathF.Min(6f, num3 - 1f));
		_goldAmount = (int)((double)num4 * num5 + (double)(num7 * num8) + 100.0);
		MBTextManager.SetTextVariable("AMOUNT", _goldAmount.ToString());
		if (encounteredParty.IsMobile && encounteredParty.MapFaction.IsBanditFaction)
		{
			return PlayerEncounter.PlayerIsDefender;
		}
		return false;
	}

	private bool bandit_start_barter_condition()
	{
		if (PlayerEncounter.Current != null)
		{
			return PlayerEncounter.Current.PlayerSide == BattleSideEnum.Defender;
		}
		return false;
	}

	private void bandit_start_barter_consequence()
	{
		BarterManager.Instance.StartBarterOffer(Hero.MainHero, Hero.OneToOneConversationHero, PartyBase.MainParty, MobileParty.ConversationParty?.Party, null, BarterManager.Instance.InitializeSafePassageBarterContext, 0, isAIBarter: false, new Barterable[1]
		{
			new SafePassageBarterable(null, Hero.MainHero, MobileParty.ConversationParty?.Party, PartyBase.MainParty)
		});
	}

	private bool conversation_minor_faction_hostile_on_condition()
	{
		if (MapEvent.PlayerMapEvent != null)
		{
			foreach (PartyBase involvedParty in MapEvent.PlayerMapEvent.InvolvedParties)
			{
				if (PartyBase.MainParty.Side == BattleSideEnum.Attacker && involvedParty.IsMobile && involvedParty.MobileParty.IsBandit && involvedParty.MapFaction.IsMinorFaction)
				{
					string text = involvedParty.MapFaction.StringId + "_encounter";
					text = ((!FactionManager.IsAtWarAgainstFaction(involvedParty.MapFaction, Hero.MainHero.MapFaction)) ? (text + "_neutral") : (text + "_hostile"));
					MBTextManager.SetTextVariable("MINOR_FACTION_ENCOUNTER", GameTexts.FindText(text));
					return true;
				}
			}
		}
		return false;
	}

	private bool conversation_minor_faction_set_selfdescription()
	{
		foreach (PartyBase involvedParty in MapEvent.PlayerMapEvent.InvolvedParties)
		{
			if (PartyBase.MainParty.Side == BattleSideEnum.Attacker && involvedParty.IsMobile && involvedParty.MobileParty.IsBandit && involvedParty.MapFaction.IsMinorFaction)
			{
				string id = involvedParty.MapFaction.StringId + "_selfdescription";
				MBTextManager.SetTextVariable("MINOR_FACTION_SELFDESCRIPTION", GameTexts.FindText(id));
				return true;
			}
		}
		return true;
	}

	private bool conversation_minor_faction_set_how_to_befriend()
	{
		foreach (PartyBase involvedParty in MapEvent.PlayerMapEvent.InvolvedParties)
		{
			if (PartyBase.MainParty.Side == BattleSideEnum.Attacker && involvedParty.IsMobile && involvedParty.MobileParty.IsBandit && involvedParty.MapFaction.IsMinorFaction)
			{
				string id = involvedParty.MapFaction.StringId + "_how_to_befriend";
				MBTextManager.SetTextVariable("MINOR_FACTION_HOWTOBEFRIEND", GameTexts.FindText(id));
				return true;
			}
		}
		return true;
	}

	private bool bandit_attacker_try_leave_condition()
	{
		if (PlayerEncounter.EncounteredParty != null)
		{
			if ((!(PlayerEncounter.EncounteredParty.CalculateCurrentStrength() <= PartyBase.MainParty.CalculateCurrentStrength()) || MobileParty.MainParty.IsInRaftState) && GetPlayerInteraction(PlayerEncounter.EncounteredMobileParty) != PlayerInteraction.PaidOffParty)
			{
				return GetPlayerInteraction(PlayerEncounter.EncounteredMobileParty) == PlayerInteraction.Friendly;
			}
			return true;
		}
		return false;
	}
}
