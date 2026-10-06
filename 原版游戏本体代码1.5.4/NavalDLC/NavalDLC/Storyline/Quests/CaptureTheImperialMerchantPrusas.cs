using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class CaptureTheImperialMerchantPrusas : NavalStorylineQuestBase
{
	private const int NumberOfCorsairParties = 2;

	private const int CalculatingBonusAmount = 50;

	private const int HonorBonusAmount = 50;

	private const int CorsairShipAiDisableTimeAsHours = 3;

	[SaveableField(1)]
	private List<MobileParty> _corsairParties;

	[SaveableField(2)]
	private JournalLog _playerStartsQuestLog;

	[SaveableField(3)]
	private CampaignVec2 _corsairSpawnPosition;

	[SaveableField(4)]
	private int _numberOfDefeatedCorsairParties;

	[SaveableField(5)]
	private MobileParty _bossCorsairParty;

	[SaveableField(6)]
	private bool _battleWon;

	[SaveableField(7)]
	private bool _willProgressStoryline;

	[SaveableField(8)]
	private int _selectedOption;

	[SaveableField(9)]
	private bool _checkpointReached;

	[SaveableField(10)]
	private bool _hasRanMissionBefore;

	private bool _shouldRunMission;

	private const string Act3Quest4CorsairPartyTemplateStringId = "storyline_act3_quest_4_corsair_generic_template";

	private const string Act3Quest4BossCorsairPartyTemplateStringId = "storyline_act3_quest_4_boss_corsair_template";

	public int SelectedOption => _selectedOption;

	public override bool WillProgressStoryline => _willProgressStoryline;

	public override TextObject Title => new TextObject("{=2eXHN7v8}Capture the Merchant Crusas");

	private TextObject DescriptionLogText => new TextObject("{=uGTU4k9w}Defeat Crusas' fleet and take him prisoner.");

	private TextObject MainCorsairShipSpawnedLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=6HCOzjBt}The way is now clear to attack {HERO.NAME}'s fleet. Destroy it!");
			textObject.SetCharacterProperties("HERO", NavalStorylineData.Prusas.CharacterObject);
			return textObject;
		}
	}

	private TextObject PlayerStartsQuestLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=vgnaNH9O}You've learned that Purig's ally, the merchant {HERO.NAME}, is anchored in the Skatria islands. You should sail there and defeat him, along with any other Sea Hounds you find there.");
			textObject.SetCharacterProperties("HERO", NavalStorylineData.Prusas.CharacterObject);
			textObject.SetCharacterProperties("ISSUE_GIVER", base.QuestGiver.CharacterObject);
			return textObject;
		}
	}

	private TextObject QuestSucceededWithHonorableOptionLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=GFERb4SK}You promised {HERO.NAME} his life if he helped you capture Purig's prisoner ship.  (+{HONOR_BONUS_AMOUNT} honor bonus)");
			textObject.SetTextVariable("HONOR_BONUS_AMOUNT", 50);
			return textObject;
		}
	}

	private TextObject QuestSucceededWithCalculatingOptionLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=4wJCXVb4}You forced {HERO.NAME} to help you capture Purig's prisoner ship, promising him nothing. (+{CALCULATING_BONUS_AMOUNT} calculating bonus)");
			textObject.SetTextVariable("CALCULATING_BONUS_AMOUNT", 50);
			return textObject;
		}
	}

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act3Quest4;

	protected override string MainPartyTemplateStringId => "storyline_act3_quest_4_main_party_template";

	public CaptureTheImperialMerchantPrusas(string questId, Hero questGiver, CampaignVec2 corsairSpawnPosition)
		: base(questId, questGiver, CampaignTime.Never, 0)
	{
		_willProgressStoryline = false;
		_numberOfDefeatedCorsairParties = 0;
		_corsairParties = new List<MobileParty>();
		_bossCorsairParty = null;
		_corsairSpawnPosition = corsairSpawnPosition;
		AddLog(DescriptionLogText);
	}

	protected override void OnFinalizeInternal()
	{
		_playerStartsQuestLog = null;
		DestroyCorsairParties();
	}

	protected override void InitializeQuestOnGameLoadInternal()
	{
		SetDialogs();
		AddGameMenus();
	}

	protected override void SetDialogs()
	{
		AddDialogsForFinalFight();
	}

	protected override void OnStartQuestInternal()
	{
		SetDialogs();
		AddGameMenus();
		_numberOfDefeatedCorsairParties = 2;
		SpawnMainCorsairParty();
		_willProgressStoryline = true;
		MBInformationManager.AddQuickInformation(new TextObject("{=vbrXtMyM}Feel that hot fetid air? It means we’re in the Skatrias, now. The foe is near…"), 200, NavalStorylineData.Gunnar.CharacterObject);
	}

	protected override void HourlyTick()
	{
		foreach (MobileParty corsairParty in _corsairParties)
		{
			if (corsairParty.IsActive && !corsairParty.IsMoving && !corsairParty.Ai.IsDisabled)
			{
				CampaignVec2 point = NavigationHelper.FindReachablePointAroundPosition(_corsairSpawnPosition, MobileParty.NavigationType.Naval, 20f, 5f);
				corsairParty.SetMoveGoToPoint(point, MobileParty.NavigationType.Naval);
			}
		}
	}

	protected override void IsNavalQuestPartyInternal(PartyBase party, NavalStorylinePartyData data)
	{
		if (_corsairParties.Any((MobileParty c) => c.Party == party))
		{
			PartyTemplateObject templateObject = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_4_corsair_generic_template");
			data.PartySize = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(templateObject).ResultNumber;
			data.IsQuestParty = true;
		}
		else if (_bossCorsairParty != null && _bossCorsairParty.Party == party)
		{
			PartyTemplateObject templateObject2 = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_4_boss_corsair_template");
			data.PartySize = (int)NavalDLCHelpers.GetMaxPartySizeLimitFromTemplate(templateObject2).ResultNumber;
			data.IsQuestParty = true;
		}
	}

	protected override void OnCompleteWithSuccessInternal()
	{
		MobileParty.MainParty.MemberRoster.RemoveTroop(NavalStorylineData.Bjolgur.CharacterObject);
		NavalStorylineData.Bjolgur.ChangeState(Hero.CharacterStates.Disabled);
		GainRenownAction.Apply(Hero.MainHero, 10f);
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest4Succeeded);
	}

	protected override void OnFailedInternal()
	{
		MobileParty.MainParty.MemberRoster.RemoveTroop(NavalStorylineData.Bjolgur.CharacterObject);
		NavalStorylineData.Bjolgur.ChangeState(Hero.CharacterStates.Disabled);
	}

	public void OnCheckPointReached()
	{
		_checkpointReached = true;
	}

	protected override void RegisterEventsInternal()
	{
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.OnShipOwnerChangedEvent.AddNonSerializedListener(this, OnShipOwnerChanged);
		CampaignEvents.BeforeGameMenuOpenedEvent.AddNonSerializedListener(this, OnBeforeGameMenuOpened);
		CampaignEvents.ConversationEnded.AddNonSerializedListener(this, OnConversationEnded);
	}

	private void OnMapEventStarted(MapEvent mapEvent, PartyBase partyBase1, PartyBase partyBase2)
	{
		if (partyBase1.IsNavalStorylineQuestParty())
		{
			foreach (Ship ship in partyBase1.Ships)
			{
				ship.IsInvulnerable = false;
			}
		}
		if (!partyBase2.IsNavalStorylineQuestParty())
		{
			return;
		}
		foreach (Ship ship2 in partyBase2.Ships)
		{
			ship2.IsInvulnerable = false;
		}
	}

	private void OnShipOwnerChanged(Ship ship, PartyBase partyBase, ChangeShipOwnerAction.ShipOwnerChangeDetail shipOwnerChangeDetail)
	{
		if (partyBase == PartyBase.MainParty && ship.IsInvulnerable)
		{
			ship.IsInvulnerable = false;
		}
	}

	private void OnConversationEnded(IEnumerable<CharacterObject> conversationCharacters)
	{
		if (NavalStorylineData.IsNavalStoryLineActive() && _battleWon && conversationCharacters.Contains(NavalStorylineData.Prusas.CharacterObject))
		{
			switch (_selectedOption)
			{
			case 1:
				OnPlayerSelectsOption1();
				break;
			case 2:
				OnPlayerSelectsOption2();
				break;
			default:
				Debug.FailedAssert("Quest selected option is wrong!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\Quests\\CaptureTheImperialMerchantPrusas.cs", "OnConversationEnded", 257);
				break;
			}
		}
	}

	private void OnBeforeGameMenuOpened(MenuCallbackArgs args)
	{
		if (NavalStorylineData.IsNavalStoryLineActive() && PlayerEncounter.EncounteredParty != null && PlayerEncounter.EncounteredParty.IsMobile && PlayerEncounter.EncounteredParty.IsNavalStorylineQuestParty() && (_corsairParties.Contains(PlayerEncounter.EncounteredParty.MobileParty) || PlayerEncounter.EncounteredParty == _bossCorsairParty?.Party))
		{
			string stringId = args.MenuContext.GameMenu.StringId;
			if (stringId == "naval_storyline_encounter_meeting")
			{
				PlayerEncounter.SetMeetingDone();
			}
			else if (stringId == "naval_storyline_encounter")
			{
				TextObject text = new TextObject("{=7b05ZaVm}You are in the Skatrias. The jagged silhouettes of small rocky islands, streaked with gull dung, stretch southwest to the horizon.{NEW_LINE}{NEW_LINE}Through the hazy air you make out the outline of a sail. It’s still quite distant, but closing fast. They are clearly Sea Hounds, ready to pounce on anyone who ventures into their hunting grounds in the Skatrias.").SetTextVariable("NEW_LINE", "\n");
				MBTextManager.SetTextVariable("ENCOUNTER_TEXT", text);
			}
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (args.MenuContext?.GameMenu?.StringId == "naval_storyline_encounter" && PlayerEncounter.EncounteredParty != null && NavalStorylineData.IsNavalStoryLineActive() && _bossCorsairParty?.Party == PlayerEncounter.EncounteredParty)
		{
			NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest4EncounterMenu);
			GameMenu.ActivateGameMenu("naval_storyline_act_3_quest_4_encounter_menu");
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		if (PlayerEncounter.Current == null || PlayerEncounter.EncounteredParty != _bossCorsairParty?.Party)
		{
			return;
		}
		if (PlayerEncounter.CampaignBattleResult != null && PlayerEncounter.CampaignBattleResult.BattleResolved)
		{
			if (PlayerEncounter.CampaignBattleResult.PlayerDefeat)
			{
				_battleWon = false;
			}
			else if (PlayerEncounter.CampaignBattleResult.PlayerVictory)
			{
				_battleWon = true;
			}
		}
		else if (PlayerEncounter.WinningSide == BattleSideEnum.None)
		{
			_battleWon = false;
		}
		else
		{
			Debug.FailedAssert("unhandled case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\Quests\\CaptureTheImperialMerchantPrusas.cs", "OnMissionEnded", 321);
		}
	}

	private void OnMobilePartyDestroyed(MobileParty party, PartyBase partyBase)
	{
		if (NavalStorylineData.IsNavalStoryLineActive() && _playerStartsQuestLog != null && _corsairParties.Contains(party))
		{
			_numberOfDefeatedCorsairParties++;
			_corsairParties.Remove(party);
			UpdateQuestTaskStage(_playerStartsQuestLog, _numberOfDefeatedCorsairParties);
			if (2 == _numberOfDefeatedCorsairParties)
			{
				SpawnMainCorsairParty();
				AddLog(MainCorsairShipSpawnedLogText);
				_bossCorsairParty.SetMoveGoToPoint(MobileParty.MainParty.Position, MobileParty.NavigationType.Naval);
			}
			else
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=Kal82TKK}There may be more Sea Hounds patrolling these islands. Let's keep searching."), 0, NavalStorylineData.Gunnar.CharacterObject);
			}
		}
	}

	private void AddDialogsForFinalFight()
	{
		TextObject npcText = new TextObject("{=A1e4qar9}[if:convo_grave][ib:normal]Did you see that big fiery ball? Not very accurate, I’ll warrant, but if one of our ships gets hit by one of those… Those who don’t jump into the sea in time will die a nasty death.");
		TextObject npcText2 = new TextObject("{=sawnbWQP}[ib:closed]I’ve heard Crusas does this… He doesn’t try to maneuver or run, but lashes his ships together, building himself a floating fortress. He mounts mangonels on them, and peppers any attackers with flaming pitch. Not a bad tactic, if you’ve got the time to prepare and you just want to be left alone. Most attackers will keep their distance and look for easier prey.");
		TextObject text = new TextObject("{=Rc2iUkN2}No fortress is invulnerable.");
		TextObject text2 = new TextObject("{=ZYheTO7N}How do we counter this?");
		TextObject npcText3 = new TextObject("{=G5gTXNKi}[if:convo_pondering]If all our ships row in together, we’d be presenting enough targets that we’re bound to get hit. So let’s not do that. Here’s another idea…");
		TextObject npcText4 = new TextObject("{=0AWyunPW}Our captured ship, the Golden Wasp, is fast and maneuverable and has that ballista. If we make it as light as possible by removing all cargo and move in our strongest rowers to man the oars, we can dart within range while avoiding that flaming pitch. Then we can use the ballista to take out the mangonels one by one, and when they’re all down, the rest of us will storm in and clear their decks.");
		TextObject text3 = new TextObject("{=b8XvnNSs}Sounds like good fun. I’ll do it.");
		TextObject text4 = new TextObject("{=PUxIpByI}I’m not sure about this. Maybe you can command the Golden Wasp.");
		TextObject npcText5 = new TextObject("{=kW7yU5CE}[ib:confident]I saw you handle that fireship at Omor, and I think you’re the one to take the helm. I’ll come with you though, to keep my men rowing briskly.");
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).GenerateToken(out var token).GenerateToken(out var token2)
			.NpcLine(npcText, IsBjolgur, IsMainHero)
			.Condition(() => Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(CaptureTheImperialMerchantPrusas)) && Hero.OneToOneConversationHero == NavalStorylineData.Bjolgur && !_hasRanMissionBefore)
			.NpcLine(npcText2, IsBjolgur, IsMainHero)
			.BeginPlayerOptions()
			.PlayerOption(text, IsBjolgur)
			.GotoDialogState(token)
			.PlayerOption(text2, IsBjolgur)
			.GotoDialogState(token)
			.EndPlayerOptions()
			.NpcLine(npcText3, IsBjolgur, IsMainHero, token)
			.NpcLine(npcText4, IsBjolgur, IsMainHero)
			.BeginPlayerOptions()
			.PlayerOption(text3, IsBjolgur)
			.GotoDialogState(token2)
			.PlayerOption(text4, IsBjolgur)
			.GotoDialogState(token2)
			.EndPlayerOptions()
			.NpcLine(npcText5, IsBjolgur, IsMainHero, token2)
			.CloseDialog()
			.Consequence(delegate
			{
				(Campaign.Current.QuestManager.Quests.FirstOrDefault((QuestBase x) => x is CaptureTheImperialMerchantPrusas) as CaptureTheImperialMerchantPrusas)._shouldRunMission = true;
			}), this);
		TextObject npcText6 = new TextObject("{=DaYQ2dm8}[ib:confident][if:convo_merry]That was a good fight! You did a fine job taking out those mangonels.");
		TextObject npcText7 = new TextObject("{=0x6OBWqY}Now then… I wish to present you with my old acquaintance, Salautas Crusas, who gave himself up when the last of his men fell to our swords. He seems very sure of himself for a man in his circumstances, and will no doubt try to bluster his way out of trouble.");
		TextObject npcText8 = new TextObject("{=1L0smluY}Crusas! Step forward.");
		TextObject npcText9 = new TextObject("{=1aHDn1cc}[ib:warrior][if:convo_grave]I am Salautas Crusas. I sail under the protection of the Sea Hounds. If you kill me, it will not go well for you.");
		TextObject text5 = new TextObject("{=Y2hbEtJN}Your threats mean nothing to me. Tell me about your deals with Purig.");
		TextObject text6 = new TextObject("{=zIBIcnNa}You’re a slaver, the scum of the seas. Talk fast if you value your life.");
		TextObject npcText10 = new TextObject("{=edUrD21k}[ib:nervous2]Yes, I buy slaves. They work my sulfur mines. Sulfur is valuable, and if I did not mine it another would. Anyway, these islands are part of no kingdom and I am violating no law. Since when does a pirate like yourself care about such things?");
		TextObject text7 = new TextObject("{=H5iCH92M}I am no pirate, but a liberator. I intend to free your captives.");
		TextObject text8 = new TextObject("{=kEBVuiUY}I have reason to believe that one of you slaving bastards has my sister.");
		TextObject textObject = new TextObject("{=hp67Xmzj}[ib:closed]So then… I believe I have heard of you. {PLAYER.NAME}? Purig spoke of you. From what I know, I think I can be of use to you. Do we have a bargain? I tell you what I know, and you give my freedom.");
		textObject.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
		TextObject text9 = new TextObject("{=v3NQFt1b}We might, if you speak truthfully.");
		TextObject text10 = new TextObject("{=J0j7IGno}You are in no place to speak of bargains.");
		TextObject npcText11 = new TextObject("{=l8XH22F7}[ib:normal]So then. When I last spoke to Purig, I saw your sister among his captives, and tried to buy her. ‘Not that one,’ he said. ‘That’s my insurance against a pair of avenging furies.’ I think he grudgingly admired how persistently you pursued him.");
		TextObject npcText12 = new TextObject("{=JYSOmwV8}He told me the whole story. Apparently, you had taken passage with him on some voyage to the north, hoping to find and free your sister from pirates. Then, you stole his ship - or so he said. Realizing that you were a dangerous enemy, he made inquiries among his Sea Hound allies to find her. Now he keeps her as a hostage on a ship in his fleet.");
		TextObject npcText13 = new TextObject("{=blWf6oTJ}[ib:confident][if:convo_nonchalant]So.. I can tell you how to find Purig, which means you’ve found your sister as well. But if you harm me, it’s likely you’ll never have such a chance again. So I repeat - do we have a bargain? ");
		TextObject text11 = new TextObject("{=jdBPxHZQ}And I repeat: speak the full truth, and we might.");
		TextObject text12 = new TextObject("{=MebLhJmj}You try my patience. Speak if you value your life.");
		TextObject npcText14 = new TextObject("{=udKuGe2a}[ib:normal]Indeed… So then, Purig has run a bit short of money, and has arranged to sell off some of his captives in Angranfjord, his hideaway in the north. He will be anchored there for the next several weeks, doing business with his favored buyers. You may be able to get close to him without him suspecting that anything is amiss. He will not sell your sister, though, as I explained.");
		TextObject npcText15 = new TextObject("{=Yj4RhLbo}[if:convo_grave][ib:closed]Were you to be one of these buyers?");
		TextObject npcText16 = new TextObject("{=kh5HVkT1}[ib:demure]Among others, yes.");
		TextObject npcText17 = new TextObject("{=G7ekdQvI}[ib:hip]Good. Then we will take your ship. It has fine lines and expensive fittings, and I have no doubt that Purig, who has an eye for costly things, would recognize it instantly");
		TextObject textObject2 = new TextObject("{=zDG4dNbj}{PLAYER.NAME}... If Purig is holding your sister as a hostage, then capturing his roundship will be a very delicate affair. If he sees Crusas’ ship and believes that we are Crusas, we may be able to allay his suspicions while we sneak aboard and turn things to our advantage.");
		textObject2.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
		TextObject npcText18 = new TextObject("{=0L1ZKRk4}[ib:closed2]We shall need to think on this, but it might even be good to keep Crusas with us, to converse with Purig or his crew.");
		TextObject npcText19 = new TextObject("{=QmIfTGw4}[ib:confident][if:convo_nonchalant]Good news, Crusas! You are indeed worth more to us alive than dead, for now.");
		TextObject npcText20 = new TextObject("{=SsAit4jx}[ib:nervous2][if:convo_grave]For now, you say. What, might I ask, is to be my fate?");
		TextObject textObject3 = new TextObject("{=ijvIIOfv}If you don’t play us false, we’ll have mercy on you. (+{HONOR_BONUS_AMOUNT} Honor Bonus)");
		textObject3.SetTextVariable("HONOR_BONUS_AMOUNT", 50);
		TextObject textObject4 = new TextObject("{=zkYn0OKb}I will make you no promises. (+{CALCULATING_BONUS_AMOUNT} Calculating Bonus)");
		textObject4.SetTextVariable("CALCULATING_BONUS_AMOUNT", 50);
		TextObject npcText21 = new TextObject("{=uUrrMnad}[if:convo_calm_friendly][ib:confident2]Well, that’s decided then. We should return to Ostican to refit and gather our allies, then prepare to sail for Angranfjord.");
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).GenerateToken(out var token3).GenerateToken(out var token4)
			.GenerateToken(out var token5)
			.GenerateToken(out var token6)
			.GenerateToken(out var token7)
			.NpcLine(npcText6, IsBjolgur, IsMainHero)
			.Condition(MultiAgentConversationCondition)
			.NpcLine(npcText7, IsBjolgur, IsMainHero)
			.NpcLine(npcText8, IsBjolgur, IsCrusas)
			.NpcLine(npcText9, IsCrusas, IsMainHero)
			.BeginPlayerOptions()
			.PlayerOption(text5, IsCrusas)
			.GotoDialogState(token3)
			.PlayerOption(text6, IsCrusas)
			.GotoDialogState(token3)
			.EndPlayerOptions()
			.NpcLine(npcText10, IsCrusas, IsMainHero, token3)
			.BeginPlayerOptions()
			.PlayerOption(text7, IsCrusas)
			.GotoDialogState(token4)
			.PlayerOption(text8, IsCrusas)
			.GotoDialogState(token4)
			.EndPlayerOptions()
			.NpcLine(textObject, IsCrusas, IsMainHero, token4)
			.BeginPlayerOptions()
			.PlayerOption(text9, IsCrusas)
			.GotoDialogState(token5)
			.PlayerOption(text10, IsCrusas)
			.GotoDialogState(token5)
			.EndPlayerOptions()
			.NpcLine(npcText11, IsCrusas, IsMainHero, token5)
			.NpcLine(npcText12, IsCrusas, IsMainHero)
			.NpcLine(npcText13, IsCrusas, IsMainHero)
			.BeginPlayerOptions()
			.PlayerOption(text11, IsCrusas)
			.GotoDialogState(token6)
			.PlayerOption(text12, IsCrusas)
			.GotoDialogState(token6)
			.EndPlayerOptions()
			.NpcLine(npcText14, IsCrusas, IsMainHero, token6)
			.NpcLine(npcText15, IsGunnar, IsCrusas)
			.NpcLine(npcText16, IsCrusas, IsGunnar)
			.NpcLine(npcText17, IsGunnar, IsCrusas)
			.NpcLine(textObject2, IsGunnar, IsMainHero)
			.NpcLine(npcText18, IsGunnar, IsMainHero)
			.NpcLine(npcText19, IsGunnar, IsCrusas)
			.NpcLine(npcText20, IsCrusas, IsGunnar)
			.BeginPlayerOptions()
			.PlayerOption(textObject3, IsCrusas)
			.Consequence(delegate
			{
				_selectedOption = 1;
			})
			.GotoDialogState(token7)
			.PlayerOption(textObject4, IsCrusas)
			.Consequence(delegate
			{
				_selectedOption = 2;
			})
			.GotoDialogState(token7)
			.EndPlayerOptions()
			.NpcLine(npcText21, IsBjolgur, IsMainHero, token7)
			.CloseDialog(), this);
	}

	private void OnPlayerSelectsOption1()
	{
		TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
		{
			new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
		});
		AddLog(QuestSucceededWithHonorableOptionLogText);
		CompleteQuestWithSuccess();
	}

	private void OnPlayerSelectsOption2()
	{
		TraitLevelingHelper.OnIssueSolvedThroughQuest(base.QuestGiver, new Tuple<TraitObject, int>[1]
		{
			new Tuple<TraitObject, int>(DefaultTraits.Calculating, 50)
		});
		AddLog(QuestSucceededWithCalculatingOptionLogText);
		CompleteQuestWithSuccess();
	}

	private bool IsMainHero(IAgent agent)
	{
		return agent.Character == CharacterObject.PlayerCharacter;
	}

	private bool IsCrusas(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Prusas.CharacterObject;
	}

	private bool IsBjolgur(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Bjolgur.CharacterObject;
	}

	private bool IsGunnar(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Gunnar.CharacterObject;
	}

	private bool MultiAgentConversationCondition()
	{
		if (Hero.OneToOneConversationHero == NavalStorylineData.Prusas && MobileParty.MainParty.IsCurrentlyAtSea && Mission.Current != null)
		{
			Agent item = SpawnBjolgur();
			Agent item2 = SpawnGunnar();
			Campaign.Current.ConversationManager.AddConversationAgents(new List<Agent> { item, item2 }, setActionsInstantly: true);
			return true;
		}
		return false;
	}

	private Agent SpawnBjolgur()
	{
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Bjolgur.CharacterObject);
		agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
		agentBuildData.InitialPosition(Mission.Current.Scene.FindEntityWithName("free_infantry_spawn_point_0").GlobalPosition);
		agentBuildData.InitialDirection(Agent.Main.LookDirection.AsVec2.Normalized());
		agentBuildData.NoHorses(noHorses: true);
		return Mission.Current.SpawnAgent(agentBuildData);
	}

	private Agent SpawnGunnar()
	{
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Gunnar.CharacterObject);
		agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
		agentBuildData.InitialPosition(Mission.Current.Scene.FindEntityWithName("free_infantry_spawn_point_1").GlobalPosition);
		agentBuildData.InitialDirection(Agent.Main.LookDirection.AsVec2.Normalized());
		agentBuildData.NoHorses(noHorses: true);
		return Mission.Current.SpawnAgent(agentBuildData);
	}

	private void StartBattle(bool startFromCheckpoint)
	{
		_battleWon = false;
		_hasRanMissionBefore = true;
		if (Hero.MainHero.IsWounded)
		{
			Hero.MainHero.Heal(Hero.MainHero.WoundedHealthLimit - Hero.MainHero.HitPoints + 1);
		}
		PlayerEncounter.Finish();
		PlayerEncounter.Start();
		PlayerEncounter.Current.SetupFields(PartyBase.MainParty, _bossCorsairParty.Party);
		PlayerEncounter.StartBattle();
		MissionInitializerRecord navalMissionInitializerTemplate = NavalStorylineData.GetNavalMissionInitializerTemplate("naval_storyline_act_3_quest_4");
		TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(MobileParty.MainParty.CurrentNavigationFace);
		navalMissionInitializerTemplate.TerrainType = (int)faceTerrainType;
		navalMissionInitializerTemplate.NeedsRandomTerrain = false;
		navalMissionInitializerTemplate.PlayingInCampaignMode = true;
		navalMissionInitializerTemplate.RandomTerrainSeed = MBRandom.RandomInt(10000);
		navalMissionInitializerTemplate.AtmosphereOnCampaign = Campaign.Current.Models.MapWeatherModel.GetAtmosphereModel(MobileParty.MainParty.Position);
		navalMissionInitializerTemplate.SceneHasMapPatch = false;
		NavalMissions.OpenFloatingFortressSetPieceBattleMission(navalMissionInitializerTemplate, startFromCheckpoint);
	}

	private void SpawnMainCorsairParty()
	{
		NavalStorylineData.Prusas.ChangeState(Hero.CharacterStates.Active);
		PartyTemplateObject partyTemplateObject = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_4_boss_corsair_template");
		_bossCorsairParty = BanditPartyComponent.CreateLooterParty("naval_corsair_boss", Clan.BanditFactions.FirstOrDefault((Clan x) => x.StringId == "southern_pirates"), NavalStorylineData.Act3Quest2TargetSettlement, isBossParty: false, partyTemplateObject, _corsairSpawnPosition);
		MobilePartyHelper.FillPartyManuallyAfterCreation(_bossCorsairParty, partyTemplateObject, partyTemplateObject.GetUpperTroopLimit());
		foreach (ShipTemplateStack shipHull in partyTemplateObject.ShipHulls)
		{
			for (int num = 0; num < shipHull.MaxValue; num++)
			{
				new Ship(shipHull.ShipHull).Owner = _bossCorsairParty.Party;
			}
		}
		TextObject textObject = GameTexts.FindText("str_lord_party_name");
		textObject.SetCharacterProperties("TROOP", NavalStorylineData.Prusas.CharacterObject);
		_bossCorsairParty.Party.SetCustomName(textObject);
		_bossCorsairParty.Party.SetCustomBanner(NavalStorylineData.CorsairBanner);
		_bossCorsairParty.IsInfoHidden = true;
		SetupCorsairParty(_bossCorsairParty);
	}

	private void SetupCorsairParty(MobileParty corsairParty)
	{
		corsairParty.SetPartyUsedByQuest(isActivelyUsed: true);
		AddTrackedObject(corsairParty);
		corsairParty.IsCurrentlyAtSea = true;
		corsairParty.IsVisible = MobileParty.MainParty.Position.Distance(corsairParty.Position) <= MobileParty.MainParty.SeeingRange;
		foreach (Ship ship in corsairParty.Ships)
		{
			ship.IsInvulnerable = true;
		}
		corsairParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		corsairParty.Ai.DisableForHours(3);
		corsairParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
		corsairParty.Party.SetVisualAsDirty();
	}

	private void DestroyCorsairParties()
	{
		foreach (MobileParty item in _corsairParties.ToList())
		{
			if (item != null && item.IsActive)
			{
				DestroyPartyAction.Apply(null, item);
			}
		}
		if (_bossCorsairParty != null && _bossCorsairParty.IsActive)
		{
			DestroyPartyAction.Apply(null, _bossCorsairParty);
		}
	}

	private void AddGameMenus()
	{
		AddGameMenu("naval_storyline_act_3_quest_4_encounter_menu", new TextObject("{=KBe6oPWy}You see the silhouette of a larger ship on the horizon, but its details are hard to make out. At first, you attribute this to the shimmering heat coming off of the sea, but as you close you can see that it is not one ship but several lashed together.\n\nSuddenly a flaming ball arcs out of the cluster of ships, tracing a line of smoke in the sky, before impacting a few arrow-shots from your prow and scattering fire across the water."), naval_storyline_act_3_quest_4_encounter_menu_on_init);
		AddGameMenuOption("naval_storyline_act_3_quest_4_encounter_menu", "naval_storyline_act_3_quest_4_encounter_menu_continue_option", new TextObject("{=DM6luo3c}Continue"), naval_storyline_act_3_quest_4_encounter_menu_continue_option_on_condition, naval_storyline_act_3_quest_4_encounter_menu_continue_option_on_consequence);
		AddGameMenu("naval_storyline_act_3_quest_4_encounter_retry", new TextObject("{=etH1IHNZ}You manage to put some distance between you and your enemies, and you have a moment to consider how to proceed."), null);
		AddGameMenuOption("naval_storyline_act_3_quest_4_encounter_retry", "naval_storyline_act_3_quest_4_encounter_retry_continue", new TextObject("{=YHMDy3lQ}Try again"), game_menu_encounter_retry_attack_on_condition, game_menu_encounter_retry_attack_on_consequence);
		AddGameMenuOption("naval_storyline_act_3_quest_4_encounter_retry", "naval_storyline_act_3_quest_4_encounter_retry_continue_from_checkpoint", new TextObject("{=rHlzkNFL}Try again from checkpoint"), game_menu_encounter_retry_continue_from_checkpoint_on_condition, game_menu_encounter_retry_continue_from_checkpoint_on_consequence);
		AddGameMenuOption("naval_storyline_act_3_quest_4_encounter_retry", "naval_storyline_act_3_quest_4_encounter_retry_leave", new TextObject("{=3sRdGQou}Leave"), game_menu_encounter_retry_leave_on_condition, game_menu_encounter_retry_leave_on_consequence, Isleave: true);
	}

	private bool game_menu_encounter_retry_attack_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return true;
	}

	private void game_menu_encounter_retry_attack_on_consequence(MenuCallbackArgs args)
	{
		CharacterObject.PlayerCharacter.HeroObject.Heal(CharacterObject.PlayerCharacter.HeroObject.MaxHitPoints);
		StartBattle(startFromCheckpoint: false);
	}

	private bool game_menu_encounter_retry_continue_from_checkpoint_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return _checkpointReached;
	}

	private void game_menu_encounter_retry_continue_from_checkpoint_on_consequence(MenuCallbackArgs args)
	{
		CharacterObject.PlayerCharacter.HeroObject.Heal(CharacterObject.PlayerCharacter.HeroObject.MaxHitPoints);
		StartBattle(startFromCheckpoint: true);
	}

	private bool game_menu_encounter_retry_leave_on_condition(MenuCallbackArgs args)
	{
		args.Tooltip = new TextObject("{=wmTjX28f}This will exit story mode and return you to the Sandbox. You can continue the storyline later by talking to Gunnar in the port again.");
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void game_menu_encounter_retry_leave_on_consequence(MenuCallbackArgs args)
	{
		CompleteQuestWithCancel();
		NavalStorylineData.DeactivateNavalStoryline();
	}

	private void naval_storyline_act_3_quest_4_encounter_menu_on_init(MenuCallbackArgs args)
	{
		if (_shouldRunMission)
		{
			_shouldRunMission = false;
			StartBattle(startFromCheckpoint: false);
		}
		else if (_battleWon)
		{
			PlayerEncounter.Finish();
			NavalStorylineData.Prusas.SetHasMet();
			ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true, noWeapon: false, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
			ConversationCharacterData conversationPartnerData = new ConversationCharacterData(NavalStorylineData.Prusas.CharacterObject, null, noHorse: true, noWeapon: true, spawnAfterFight: true, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
			CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "conversation_scene_sea_multi_agent", "", isMultiAgentConversation: true);
		}
		else if (_hasRanMissionBefore)
		{
			GameMenu.SwitchToMenu("naval_storyline_act_3_quest_4_encounter_retry");
		}
	}

	private bool naval_storyline_act_3_quest_4_encounter_menu_continue_option_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return true;
	}

	private void naval_storyline_act_3_quest_4_encounter_menu_continue_option_on_consequence(MenuCallbackArgs args)
	{
		ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true);
		ConversationCharacterData conversationPartnerData = new ConversationCharacterData(NavalStorylineData.Bjolgur.CharacterObject, PartyBase.MainParty, noHorse: true);
		CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
	}

	[GameMenuInitializationHandler("naval_storyline_act_3_quest_4_encounter_menu")]
	[GameMenuInitializationHandler("naval_storyline_act_3_quest_4_encounter_retry")]
	private static void quest_game_menus_on_init_background(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All).WaitMeshName);
	}

	public bool IsCrusasVisible()
	{
		if (_bossCorsairParty != null && _bossCorsairParty.IsActive)
		{
			return _bossCorsairParty.IsVisible;
		}
		return false;
	}
}
