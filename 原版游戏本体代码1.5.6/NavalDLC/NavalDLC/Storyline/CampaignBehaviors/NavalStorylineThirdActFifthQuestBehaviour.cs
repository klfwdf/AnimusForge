using System.Linq;
using Helpers;
using NavalDLC.Storyline.MissionControllers;
using NavalDLC.Storyline.Quests;
using StoryMode;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline.CampaignBehaviors;

public class NavalStorylineThirdActFifthQuestBehaviour : CampaignBehaviorBase
{
	public enum NavalStorylineFinalQuestState
	{
		TalkWithGunnarAtPort,
		GunnarWaitsForAnAnswer,
		Quest5IsInProgress,
		TalkWithGunnarAfterFight,
		SpeakToGunnarAndSister,
		End
	}

	private const string QuestConversationMenuId = "naval_storyline_act_3_quest_5_conversation_menu";

	private const string GunnarsLongshipStringId = "northern_medium_ship";

	private const string Tier3NordInfantryStringId = "nord_spear_warrior";

	private const string Tier4NordInfantryStringId = "nord_vargr";

	private const int Tier3NordInfantryCount = 10;

	private const int Tier4NordInfantryCount = 10;

	private NavalStorylineFinalQuestState _navalStorylineFinalQuestState;

	private Quest5SetPieceBattleMissionController.BossFightOutComeEnum _bossFightOutCome;

	private bool _isQuestAcceptedThroughMission;

	private readonly float _strengthModifier = 1f;

	public override void RegisterEvents()
	{
		if (!NavalStorylineData.IsNavalStorylineCanceled())
		{
			CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
			CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
			CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		}
	}

	private void OnGameLoadFinished()
	{
		NavalStorylineData.NavalStorylineStage storylineStage = NavalStorylineData.GetStorylineStage();
		if (storylineStage == NavalStorylineData.NavalStorylineStage.Act3Quest4 && Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(FreeTheSeaHoundsCaptivesQuest)))
		{
			_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.Quest5IsInProgress;
		}
		else if (storylineStage == NavalStorylineData.NavalStorylineStage.Act3Quest5 && Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(ReturnToBaseQuest)))
		{
			_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.SpeakToGunnarAndSister;
		}
		else if (storylineStage >= NavalStorylineData.NavalStorylineStage.Act3Quest5)
		{
			_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.End;
		}
		if (MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.14") && _navalStorylineFinalQuestState == NavalStorylineFinalQuestState.Quest5IsInProgress && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(FreeTheSeaHoundsCaptivesQuest)))
		{
			_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.TalkWithGunnarAtPort;
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (args.MenuContext.GameMenu.StringId == "naval_storyline_outside_town" && _navalStorylineFinalQuestState > NavalStorylineFinalQuestState.Quest5IsInProgress)
		{
			GameMenu.SwitchToMenu("naval_storyline_finalize_menu");
		}
		if (_navalStorylineFinalQuestState <= NavalStorylineFinalQuestState.Quest5IsInProgress && NavalStorylineData.IsStorylineActivationPossible() && NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest4) && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(FreeTheSeaHoundsCaptivesQuest)) && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement && !Campaign.Current.VisualTrackerManager.CheckTracked(NavalStorylineData.Gunnar))
		{
			Campaign.Current.VisualTrackerManager.RegisterObject(NavalStorylineData.Gunnar);
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		if (detail == QuestBase.QuestCompleteDetails.Success && quest is CaptureTheImperialMerchantPrusas)
		{
			_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.TalkWithGunnarAtPort;
		}
		else if (quest is FreeTheSeaHoundsCaptivesQuest)
		{
			if (detail == QuestBase.QuestCompleteDetails.Success)
			{
				NavalStorylineData.DeactivateNavalStoryline();
				_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.TalkWithGunnarAfterFight;
				_bossFightOutCome = ((FreeTheSeaHoundsCaptivesQuest)quest).BossFightOutCome;
			}
			else
			{
				_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.TalkWithGunnarAtPort;
			}
		}
		else if (detail == QuestBase.QuestCompleteDetails.Success && quest is SpeakToGunnarAndSisterQuest)
		{
			_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.End;
		}
	}

	private void OnAfterSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		if (StoryModeManager.Current != null)
		{
			AddDialogs();
			AddGameMenus(campaignGameStarter);
		}
	}

	private void AddDialogs()
	{
		DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 1200).NpcLine(new TextObject("{=jWDBinsb}Well... Here we are. Ready to set sail for Angranfjord and settle accounts with our enemies, once and for all. Lahar will sail with us, and Bjolgur, and more of his brothers may join us at our destination. We have Crusas' ship – and Crusas too of course, much as he might not like it – and hopefully the element of surprise. We just need to consider how to turn this best to our advantage.")).Condition(() => _navalStorylineFinalQuestState == NavalStorylineFinalQuestState.TalkWithGunnarAtPort && Quest5ConversationStartCondition())
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=el44RZG4}Let us set out, then."))
			.Consequence(delegate
			{
				if (Mission.Current == null)
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += ActivateQuest5;
				}
				else
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
					{
						NavalStorylineData.OnPlayerAcceptsQuest(QuestAccepted, delegate
						{
							_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.GunnarWaitsForAnAnswer;
							NavalStorylineData.OnPlayerPostponedQuestStart();
						});
					};
				}
			})
			.CloseDialog()
			.PlayerOption(new TextObject("{=a0j86F9C}I need a bit more time."))
			.Consequence(delegate
			{
				_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.GunnarWaitsForAnAnswer;
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions();
		DialogFlow dialogFlow2 = DialogFlow.CreateDialogFlow("start", 1200).NpcLine(new TextObject("{=0Y3S817q}Are you ready to sail to the Angranfjord to carry out our plan? Purig may not be waiting there for much longer.")).Condition(() => _navalStorylineFinalQuestState == NavalStorylineFinalQuestState.GunnarWaitsForAnAnswer && Quest5ConversationStartCondition())
			.BeginPlayerOptions()
			.PlayerOption(new TextObject("{=qcYkbX2a}Let us sail."))
			.Consequence(delegate
			{
				if (Mission.Current == null)
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += ActivateQuest5;
				}
				else
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
					{
						NavalStorylineData.OnPlayerAcceptsQuest(QuestAccepted, delegate
						{
							_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.GunnarWaitsForAnAnswer;
							NavalStorylineData.OnPlayerPostponedQuestStart();
						});
					};
				}
			})
			.CloseDialog()
			.PlayerOption(new TextObject("{=4LhjHfSY}I am still not ready."))
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += NavalStorylineData.OnPlayerPostponedQuestStart;
			})
			.CloseDialog()
			.PlayerOption("{=aEKNUI45}This war on the Sea Hounds is too risky. There must be another way to get my sister back.")
			.GotoDialogState("gunnar_ransom_sister")
			.EndPlayerOptions();
		TextObject text = new TextObject("{=7SzwQ5NK}[if:convo_merry]{PLAYER.NAME}, welcome! I've been entertaining the village with tales of our adventurers. If you're looking for recruits, then I doubt you'll find a more promising batch than the lads of Lagsholfn. You always have a place by my hearth, old friend.");
		TextObject text2 = new TextObject("{=dV5ai0PF}[if:convo_grave]Well, {PLAYER.NAME}... Alas, you appear to have made some enemies here. I do not know if what they say is true, and at any rate, I will never raise a hand against you. But I do not think it is good for you to stay here just now.");
		DialogFlow dialogFlow3 = DialogFlow.CreateDialogFlow("start", 1200).BeginNpcOptions().NpcOption(text, delegate
		{
			if (GunnarNotableConditions())
			{
				Settlement currentSettlement = NavalStorylineData.Gunnar.CurrentSettlement;
				if (currentSettlement == null)
				{
					return false;
				}
				return currentSettlement.Owner?.GetRelationWithPlayer() >= 0f;
			}
			return false;
		})
			.GotoDialogState("lord_start")
			.NpcOption(text2, delegate
			{
				if (GunnarNotableConditions())
				{
					Settlement currentSettlement = NavalStorylineData.Gunnar.CurrentSettlement;
					if (currentSettlement == null)
					{
						return false;
					}
					return currentSettlement.Owner?.GetRelationWithPlayer() < 0f;
				}
				return false;
			})
			.GotoDialogState("lord_start")
			.EndNpcOptions();
		DialogFlow dialogFlow4 = DialogFlow.CreateDialogFlow("start", 1500).NpcLine("{=!}{GUNNAR_FINAL_DIALOG_LINE_1}").Condition(delegate
		{
			int num;
			if (_navalStorylineFinalQuestState == NavalStorylineFinalQuestState.TalkWithGunnarAfterFight)
			{
				num = ((Hero.OneToOneConversationHero == NavalStorylineData.Gunnar) ? 1 : 0);
				if (num != 0)
				{
					DecideGunnarDialogue();
				}
			}
			else
			{
				num = 0;
			}
			return (byte)num != 0;
		})
			.NpcLine("{=!}{GUNNAR_FINAL_DIALOG_LINE_2}")
			.NpcLine("{=xxxjoDxM}My men, though... I've had a word with them, and some of them have been quite impressed by your leadership. They want to follow you, if you'll have them. And as I mentioned, they prefer to sail on our ship here, the Wave-Steed, so I guess that's yours too, if you'll have it. She'll carry you well, especially in the rough seas of the north.")
			.BeginPlayerOptions()
			.PlayerOption("{=qatVcvrX}I welcome your ship and crew.")
			.Consequence(OnPlayerWelcomedGunnarsCrew)
			.GotoDialogState("gunnar_final_dialog_token_1")
			.PlayerOption("{=FaZ1dSuh}I am honored, but I cannot take on your companions.")
			.GotoDialogState("gunnar_final_dialog_token_1")
			.EndPlayerOptions()
			.NpcLine("{=!}{GUNNAR_FINAL_DIALOG_LINE_3}", null, null, "gunnar_final_dialog_token_1")
			.BeginPlayerOptions()
			.PlayerOption("{=uh2W7Jh3}Farewell. Perhaps I will take you up on your reputation.")
			.GotoDialogState("gunnar_final_dialog_token_2")
			.PlayerOption("{=C94hXQp3}Farewell, and good hunting.")
			.GotoDialogState("gunnar_final_dialog_token_2")
			.EndPlayerOptions()
			.NpcLine("{=Vcr7BYxJ}Farewell, {PLAYER.NAME}.", null, null, "gunnar_final_dialog_token_2")
			.Consequence(GunnarConversationOnConsequence)
			.CloseDialog();
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow2);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow3);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow4);
	}

	private void GunnarConversationOnConsequence()
	{
		NavalDLCHelpers.AddSisterToClan();
		MakeGunnarNotable();
		_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.End;
	}

	private void MakeGunnarNotable()
	{
		Village village = Village.All.FirstOrDefault((Village x) => x.Settlement.StringId == "village_N1_2");
		if (village != null)
		{
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(NavalStorylineData.Gunnar, village.Settlement);
		}
	}

	private void QuestAccepted()
	{
		_isQuestAcceptedThroughMission = true;
		OpenQuestMenu();
		Mission.Current.EndMission();
		_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.Quest5IsInProgress;
	}

	private void OpenQuestMenu()
	{
		GameMenu.ActivateGameMenu("naval_storyline_act_3_quest_5_conversation_menu");
	}

	private void AddGameMenus(CampaignGameStarter starter)
	{
		starter.AddGameMenu("naval_storyline_act_3_quest_5_conversation_menu", string.Empty, naval_storyline_act_3_quest_5_conversation_menu_on_init);
		starter.AddGameMenu("naval_storyline_finalize_menu", "{=l1VpTx3x}You have returned to Ostican harbor. Word spreads fast among seafolk, and a trading ship leaving the harbor dips its oars in salute to your victory. As the crews of your ships come ashore, they are clapped on the back by the local fishermen and dock workers and taken to the taverns to drink to the demise of the Sea Hounds.", naval_storyline_finalize_menu_on_init);
		starter.AddGameMenuOption("naval_storyline_finalize_menu", "naval_storyline_finalize_menu_continue_option", "{=DM6luo3c}Continue", naval_storyline_finalize_menu_continue_option_on_condition, naval_storyline_finalize_menu_continue_option_on_consequence);
	}

	private void naval_storyline_act_3_quest_5_conversation_menu_on_init(MenuCallbackArgs args)
	{
		if (_isQuestAcceptedThroughMission && Mission.Current == null)
		{
			ActivateQuest5();
			_isQuestAcceptedThroughMission = false;
		}
	}

	private void naval_storyline_finalize_menu_on_init(MenuCallbackArgs args)
	{
		if (_navalStorylineFinalQuestState == NavalStorylineFinalQuestState.TalkWithGunnarAfterFight)
		{
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true, noWeapon: true, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(NavalStorylineData.Gunnar.CharacterObject, PartyBase.MainParty, noHorse: true, noWeapon: true, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: true, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "conversation_scene_sea_multi_agent", "", isMultiAgentConversation: true);
		}
		if (Game.Current.GameStateManager.ActiveState is MapState mapState)
		{
			mapState.Handler.TeleportCameraToMainParty();
		}
		string backgroundMeshName = Settlement.CurrentSettlement.Culture.StringId + "_port";
		args.MenuContext.SetBackgroundMeshName(backgroundMeshName);
		args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/port");
	}

	private bool naval_storyline_finalize_menu_continue_option_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.VisitPort;
		return true;
	}

	private void naval_storyline_finalize_menu_continue_option_on_consequence(MenuCallbackArgs args)
	{
		if (_navalStorylineFinalQuestState == NavalStorylineFinalQuestState.SpeakToGunnarAndSister && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(SpeakToGunnarAndSisterQuest)))
		{
			new SpeakToGunnarAndSisterQuest(_bossFightOutCome).StartQuest();
		}
		Settlement settlement = Settlement.CurrentSettlement ?? PlayerEncounter.EncounterSettlement;
		GameMenu.SwitchToMenu(MobileParty.MainParty.HasNavalNavigationCapability ? "naval_town_outside" : Campaign.Current.Models.EncounterGameMenuModel.GetEncounterMenu(PartyBase.MainParty, settlement.Party, out var _, out var _));
	}

	private void ActivateQuest5()
	{
		if (!Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(FreeTheSeaHoundsCaptivesQuest)))
		{
			Campaign.Current.VisualTrackerManager.RemoveTrackedObject(NavalStorylineData.Gunnar);
			new FreeTheSeaHoundsCaptivesQuest("naval_storyline_act3_quest5_1", _strengthModifier).StartQuest();
			_navalStorylineFinalQuestState = NavalStorylineFinalQuestState.Quest5IsInProgress;
		}
	}

	private bool Quest5ConversationStartCondition()
	{
		if (NavalStorylineData.IsStorylineActivationPossible() && NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest4) && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(FreeTheSeaHoundsCaptivesQuest)) && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement)
		{
			return Hero.OneToOneConversationHero == NavalStorylineData.Gunnar;
		}
		return false;
	}

	private bool GunnarNotableConditions()
	{
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
		if (Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && !NavalStorylineData.IsNavalStoryLineActive())
		{
			return NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest5);
		}
		return false;
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_navalStorylineFinalQuestState", ref _navalStorylineFinalQuestState);
		dataStore.SyncData("_bossFightOutCome", ref _bossFightOutCome);
	}

	public Quest5SetPieceBattleMissionController.BossFightOutComeEnum GetBossFightOutcome()
	{
		return _bossFightOutCome;
	}

	private void OnPlayerWelcomedGunnarsCrew()
	{
		Ship ship = new Ship(MBObjectManager.Instance.GetObject<ShipHull>("northern_medium_ship"));
		ship.SetName(new TextObject("{=EUAsSTeT}Wave-Steed"));
		ChangeShipOwnerAction.ApplyByLooting(PartyBase.MainParty, ship);
		CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>("nord_spear_warrior");
		MobileParty.MainParty.MemberRoster.AddToCounts(character, 10);
		CharacterObject character2 = MBObjectManager.Instance.GetObject<CharacterObject>("nord_vargr");
		MobileParty.MainParty.MemberRoster.AddToCounts(character2, 10);
		if (!MobileParty.MainParty.Anchor.IsValid && Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.HasPort)
		{
			MobileParty.MainParty.Anchor.Settlement = Settlement.CurrentSettlement;
		}
		TextObject textObject = new TextObject("{=06sIBlHR}{NUMBER} troops and {SHIP_NAME} were added to your party.");
		textObject.SetTextVariable("NUMBER", 20);
		textObject.SetTextVariable("SHIP_NAME", ship.Name);
		InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0f, 1f, 0f)));
	}

	private void DecideGunnarDialogue()
	{
		TextObject text;
		TextObject text2;
		if (_bossFightOutCome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerRefusedTheDuel)
		{
			text = new TextObject("{=dI8a424b}Well then... Your sister is free, thank the gods. You gave Purig the death he deserved. None will mourn him. And the Sea Hounds... Well, I doubt they'll recover from the thrashing we gave them today. The north will thank you.");
			text2 = new TextObject("{=UAq8cW8O}Now, I think, I will go ashore, and make my way home. Lagshofn is not far from here. I've settled what I wish to settle, and all this rowing and ramming and climbing and jostling and fighting is hard on my old bones.");
		}
		else if (_bossFightOutCome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerAcceptedAndWonTheDuel)
		{
			text = new TextObject("{=0TP1KQLE}Well then... Your sister is free, thank the gods. You put an end to the Sea Hounds, and gave Purig a far more honorable death than he deserved. Men will speak well of you.");
			text2 = new TextObject("{=UAq8cW8O}Now, I think, I will go ashore, and make my way home. Lagshofn is not far from here. I've settled what I wish to settle, and all this rowing and ramming and climbing and jostling and fighting is hard on my old bones.");
		}
		else if (_bossFightOutCome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerAcceptedTheDuelLostItAndLetPurigGo)
		{
			text = new TextObject("{=XDzsJmMP}Well then...  Your sister is free, thank the gods. Purig may have gotten away, but I doubt the Sea Hounds will be troubling us much more.");
			text2 = new TextObject("{=dPaN65B1}It was an honorable thing, to duel him, and I am glad you kept your word to him, though he did not deserve it. For my part, though, I owe him nothing. I continue to hunt him, here in Beinland, and as it is much easier for him to evade a large group than a single hunter, I will do so alone.");
		}
		else
		{
			text = new TextObject("{=8j3z1dBZ}Well then... Your sister is free, thank the gods. Purig is dead, and none will mourn him. I might wish that his death could have come some other way, but I will not dwell on it.");
			text2 = new TextObject("{=UAq8cW8O}Now, I think, I will go ashore, and make my way home. Lagshofn is not far from here. I've settled what I wish to settle, and all this rowing and ramming and climbing and jostling and fighting is hard on my old bones.");
		}
		TextObject text3 = ((_bossFightOutCome != Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerAcceptedTheDuelLostItAndLetPurigGo) ? new TextObject("{=IGnbxJHn}You should come see me in my village, Lagshofn, in Beinland. It's not much, not for a {?PLAYER.GENDER}warrior{?}man{\\?} like you, who's no doubt seen all the wonders of the Empire and the lands beyond, but we can pass a summer's night on the beach and drink to our deeds.") : new TextObject("{=1PPiv2ns}I suspect Purig will try to travel as far from these parts as possible. Perhaps deep into the south, or to the east... Perhaps I will take years to find him, or perhaps my old age will finally catch up to me on the road or on the seas. I do not know if we will meet again."));
		MBTextManager.SetTextVariable("GUNNAR_FINAL_DIALOG_LINE_1", text);
		MBTextManager.SetTextVariable("GUNNAR_FINAL_DIALOG_LINE_2", text2);
		MBTextManager.SetTextVariable("GUNNAR_FINAL_DIALOG_LINE_3", text3);
	}
}
