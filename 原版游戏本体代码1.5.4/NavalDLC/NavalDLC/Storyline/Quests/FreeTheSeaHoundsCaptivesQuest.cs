using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using NavalDLC.Missions;
using NavalDLC.SceneInformationPopupTypes;
using NavalDLC.Storyline.MissionControllers;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class FreeTheSeaHoundsCaptivesQuest : NavalStorylineQuestBase
{
	public enum FreeTheSeaHoundsCaptivesQuestState
	{
		None,
		RestartMission,
		GoToSeaHoundPartyPosition,
		EncounteredWithSeaHoundsParty,
		TalkedWithGunnarBeforeFight,
		TalkedWithPurigBeforeBossFight,
		PlayerLostBossFight,
		DefeatedPurig,
		HeadBackToOstican
	}

	private const int PlayerLostDuelAndLetPurigGoHonorBonus = 50;

	private const int PlayerLostDuelAndKilledPurigHonorPenalty = -50;

	private const int PlayerLostDuelAndKilledPurigRenownBonus = 50;

	private const string SeaHoundSetPieceBattlePartyTemplateString = "storyline_act3_quest_5_sea_hounds_set_piece_battle_template";

	private const string SeaHoundPartyTemplateStringId = "storyline_act3_quest_5_sea_hounds_template";

	private const string EncounterMenuId = "act_3_quest_5_encounter_menu";

	private const string MissionMenuId = "act_3_quest_5_mission_menu";

	private const string SetPieceBattleSceneName = "naval_storyline_act_3_quest_5";

	private const int SeaHoundPartySize = 67;

	private const string NordMediumShipStringId = "nord_medium_ship";

	private const string AseraiHeavyShipStringId = "aserai_heavy_ship";

	[SaveableField(1)]
	private MobileParty _seaHoundsParty;

	private bool _shouldMissionContinueFromCheckpoint;

	[SaveableField(0)]
	private FreeTheSeaHoundsCaptivesQuestState _currentState;

	[SaveableField(7)]
	private float _strengthModifier;

	private bool _isPurigKilledViaConversation;

	private bool _isSisterSavedSceneNotificationTriggered;

	[SaveableField(12)]
	private readonly MapMarker _skatriaIslandsMarker;

	[SaveableField(13)]
	private Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState _lastHitCheckpoint;

	[SaveableField(14)]
	public Quest5SetPieceBattleMissionController.BossFightOutComeEnum BossFightOutCome;

	private readonly List<KeyValuePair<string, string>> _seaHoundPartyShipUpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("sail", "sails_lvl2"),
		new KeyValuePair<string, string>("side", "side_northern_shields_lvl2")
	};

	private readonly List<KeyValuePair<string, string>> _nordMediumShipyShipUpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("sail", "sails_lvl2"),
		new KeyValuePair<string, string>("side", "side_northern_shields_lvl2")
	};

	private readonly List<KeyValuePair<string, string>> _aseraiHeavyShipUpgradePieceList = new List<KeyValuePair<string, string>>
	{
		new KeyValuePair<string, string>("fore", "fore_ballista"),
		new KeyValuePair<string, string>("aft", "aft_battlement_lvl3_wbarracks"),
		new KeyValuePair<string, string>("deck", "deck_arrow_and_javelin_crates_lvl2"),
		new KeyValuePair<string, string>("sail", "sails_lvl2")
	};

	public override TextObject Title => new TextObject("{=JYCrUhnu}Free the Sea Hounds' captives");

	public override NavalStorylineData.NavalStorylineStage Stage => NavalStorylineData.NavalStorylineStage.Act3Quest5;

	public override bool WillProgressStoryline => true;

	protected override string MainPartyTemplateStringId => "storyline_act3_quest_5_main_party_template";

	private CampaignVec2 _seaHoundsSpawnPosition => new CampaignVec2(new Vec2(260f, 815f), isOnLand: false);

	private TextObject _allyDefeatedText => new TextObject("{=9sfcVI0Q}Your allies were defeated. You will have to try again.");

	private TextObject _findSeaHoundsQuestLog => new TextObject("{=mp0EKEI9}Go to Angranfjord and locate the Sea Hounds.");

	private TextObject _arrivedAngranfjordQuestLog => new TextObject("{=7Gl82o4g}You have arrived at Angranfjord, Purig's lair.");

	public FreeTheSeaHoundsCaptivesQuest(string questId, float strengthModifier)
		: base(questId, NavalStorylineData.Gunnar, CampaignTime.Never, 0)
	{
		_strengthModifier = strengthModifier;
		_skatriaIslandsMarker = Campaign.Current.MapTrackerManager.CreateMapMarker(NavalStorylineData.CorsairBanner, new TextObject("{=GSksjBCZ}Angranfjord"), _seaHoundsSpawnPosition.AsVec3(), isVisibleOnMap: true, base.StringId);
		_currentState = FreeTheSeaHoundsCaptivesQuestState.GoToSeaHoundPartyPosition;
		SetDialogs();
		AddGameMenus();
	}

	protected override void PreAfterLoad()
	{
		if (NavalStorylineData.Purig.IsDead)
		{
			return;
		}
		if (NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest5) || NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3SpeakToGunnarAndSister))
		{
			KillCharacterAction.ApplyByRemove(NavalStorylineData.Purig);
		}
		else if (NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest4) && NavalStorylineData.Purig.VolunteerTypes == null)
		{
			MobileParty partyBelongedTo = NavalStorylineData.Purig.PartyBelongedTo;
			if (partyBelongedTo != null && partyBelongedTo.MapEvent?.IsPlayerMapEvent == true)
			{
				NavalStorylineData.Purig.PartyBelongedTo.MapEvent.FinalizeEvent();
			}
			KillCharacterAction.ApplyByRemove(NavalStorylineData.Purig);
			_lastHitCheckpoint = Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.End;
			_currentState = FreeTheSeaHoundsCaptivesQuestState.DefeatedPurig;
		}
	}

	protected override void InitializeQuestOnGameLoadInternal()
	{
		base.InitializeQuestOnGameLoadInternal();
		SetDialogs();
		AddGameMenus();
		if (_lastHitCheckpoint == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.End)
		{
			if (BossFightOutCome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.None)
			{
				BossFightOutCome = Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerRefusedTheDuel;
			}
			ShowNavalSaveSisterSceneNotification();
		}
	}

	protected override void RegisterEventsInternal()
	{
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.CanHeroBecomePrisonerEvent.AddNonSerializedListener(this, CanHeroBecomePrisoner);
		CampaignEvents.PartyVisibilityChangedEvent.AddNonSerializedListener(this, OnPartyVisibilityChanged);
	}

	protected override void SetDialogs()
	{
		DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 1200).NpcLine(new TextObject("{=qn00ppJR}[if:convo_stern]There they are. With your sister as their hostage, a straight-out attack is out of the question. Throughout this voyage, I have been thinking on what we might do to ensure her safety, and I recommend that we try an old corsair's trick."), IsGunnar, IsPlayer).Condition(GunnarInitialMeetingDialogCondition)
			.NpcLine(new TextObject("{=axgouPEG}[ib:hip2]Do you see that big cluster of ships back there? That's got to be where they're holding the prisoners. That smaller vessel out front, though - that's got to be a picket, and it will stop us before we get too close. Let's approach it, pretending to be a buyer, while Bjolgur and Lahar stay out of sight. Crusas can banter with them a bit as a distraction. One of our men shall stand at his side with a dagger, lest he betray us."), IsGunnar, IsPlayer)
			.NpcLine(new TextObject("{=HzlWiTns}[ib:confident][if:convo_calm_friendly]You and I, meanwhile, shall dive off the side of our ship, swim round to the stern of the prisoner ship, and climb up the side. Then together we can try to find your sister on board. Once we succeed, well, we'll just have to figure it out from there."), IsGunnar, IsPlayer)
			.PlayerLine(new TextObject("{=kJaiDDRi}Let's proceed, then."), IsGunnar)
			.Consequence(GunnarInitialMeetingDialogConsequence)
			.CloseDialog();
		DialogFlow dialogFlow2 = DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=Q5B3Uvoa}Who's there? What's going on??[if:convo_dismayed]", IsSister, IsPlayer).Condition(delegate
		{
			if (Mission.Current == null)
			{
				return false;
			}
			Quest5SetPieceBattleMissionController missionBehavior = Mission.Current.GetMissionBehavior<Quest5SetPieceBattleMissionController>();
			return missionBehavior != null && Hero.OneToOneConversationHero == StoryModeHeroes.LittleSister && missionBehavior.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.Phase1ShipInteriorPhase;
		})
			.PlayerLine("{=0lTm2sy1}{SISTER.NAME}... Is that you? It's me!", IsSister)
			.Condition(delegate
			{
				StringHelpers.SetCharacterProperties("SISTER", StoryModeHeroes.LittleSister.CharacterObject);
				return true;
			})
			.NpcLine("{=IC9Fvl54}[ib:warrior][if:convo_astonished]{?PLAYER.GENDER}Sister{?}Brother{\\?}! Heaven's mercy! What are you doing here?", IsSister, IsPlayer)
			.BeginPlayerOptions()
			.PlayerOption("{=HKx2nxGt}It is. We're here to rescue you! Just... Keep your voice low.", IsSister)
			.GotoDialogState("sister_answer_1")
			.PlayerOption("{=gvOJ43Na}{SISTER.NAME}, I just need you to be patient and strong a little longer.", IsSister)
			.GotoDialogState("sister_answer_1")
			.EndPlayerOptions()
			.NpcLine("{=OLTofDbM}[ib:weary][if:convo_grave]I'll be silent. What's going on?", IsSister, IsPlayer, "sister_answer_1")
			.BeginPlayerOptions()
			.PlayerOption("{=jrloQtMP}I'm going to take this ship, and get you to safety.", IsSister)
			.GotoDialogState("sister_answer_2")
			.PlayerOption("{=aLaA3jZ2}I'm going to free you, and kill every last one of those slavers!", IsSister)
			.GotoDialogState("sister_answer_2")
			.EndPlayerOptions()
			.NpcLine("{=w83SHIYa}[ib:normal][if:convo_normal]Can you get me out of here?", IsSister, IsPlayer, "sister_answer_2")
			.BeginPlayerOptions()
			.PlayerOption("{=21BSwRCQ}Those timbers on your cell look thick. I don't have time now to chop through them.", IsSister)
			.GotoDialogState("sister_answer_3")
			.PlayerOption("{=kfHpv0Jg}I'll finish off the slavers and sail this ship out of here, then we can break you out.", IsSister)
			.GotoDialogState("sister_answer_3")
			.EndPlayerOptions()
			.NpcLine("{=jjjS4TLY}[if:convo_calm_friendly]I understand. Heaven protect you, {?PLAYER.GENDER}Sister{?}Brother{\\?}!", IsSister, IsPlayer, "sister_answer_3")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
				{
					Mission.Current.GetMissionBehavior<Quest5SetPieceBattleMissionController>().SetTalkedWithSister();
				};
			})
			.CloseDialog();
		DialogFlow dialogFlow3 = DialogFlow.CreateDialogFlow("start", 5200).NpcLine("{=Ja5bHsro}[ib:warrior][if:convo_furious]You... You and {QUEST_5_COMPANION.NAME} have been slaughtering my allies all up and down this coast, and now it comes to this.", IsPurig, IsPlayer).Condition(delegate
		{
			if (Mission.Current == null)
			{
				return false;
			}
			StringHelpers.SetCharacterProperties("QUEST_5_COMPANION", NavalStorylineData.Gunnar.CharacterObject);
			Quest5SetPieceBattleMissionController missionBehavior = Mission.Current.GetMissionBehavior<Quest5SetPieceBattleMissionController>();
			return missionBehavior != null && Hero.OneToOneConversationHero == NavalStorylineData.Purig && missionBehavior.State == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.BossFightConversationInProgress;
		})
			.NpcLine("{=naMWdTPV}[ib:normal]I was going to forge the Sea Hounds into a weapon of vengeance against the house of Volbjorn.", IsPurig, IsPlayer)
			.NpcLine("{=MR1tc1Ao}[if:convo_evil_smile]I would have drowned them in their own blood. But to the free warriors of the north, to the men who stood against the tyrant - I would have showered them with gold. I would have given them the fame that they deserved. We would have ruled the northern seas.", IsPurig, IsPlayer)
			.NpcLine("{=7rCvGfgb}[if:convo_stern]But that is all for nothing. Instead, the kings of Nordvyg, the men that {QUEST_5_COMPANION.NAME} and I fought, will have the last laugh. So, do you like what you've wrought?", IsPurig, IsPlayer)
			.BeginPlayerOptions()
			.PlayerOption("{=fiSglIaN}You'd have been twice the tyrant that Volbjorn was.", IsPurig)
			.GotoDialogState("purig_answer")
			.PlayerOption("{=7pWJKkQx}I don't care about your old wars. You put my sister in a cage.", IsPurig)
			.GotoDialogState("purig_answer")
			.PlayerOption("{=Mkxm5l1N}You are outnumbered. Stop bandying words.", IsPurig)
			.GotoDialogState("purig_answer")
			.EndPlayerOptions()
			.NpcLine("{=U9CfaZTF}[ib:confident][if:convo_bemused]Not much honor in having your men just cut me down, is there? Fight me one-to-one. If I win, I go free, and we need never see each other again. If you win, people will remember you as the one who slew the terror of the north.", IsPurig, IsPlayer, "purig_answer")
			.BeginPlayerOptions()
			.PlayerOption("{=16CMD4HL}I am willing to duel.", IsPurig)
			.Consequence(delegate
			{
				Mission.Current.GetMissionBehavior<Quest5SetPieceBattleMissionController>().StartBossFight(isDuel: true);
				_currentState = FreeTheSeaHoundsCaptivesQuestState.TalkedWithPurigBeforeBossFight;
			})
			.CloseDialog()
			.PlayerOption("{=pspOcQY7}You dare talk to me of honor? Kill him, lads!", IsPurig)
			.Consequence(delegate
			{
				Mission.Current.GetMissionBehavior<Quest5SetPieceBattleMissionController>().StartBossFight(isDuel: false);
				_currentState = FreeTheSeaHoundsCaptivesQuestState.TalkedWithPurigBeforeBossFight;
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
		DialogFlow dialogFlow4 = DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=bMaepOl8}[ib:warrior][if:convo_mocking_revenge]Had enough, have you? Well, are you going to honor your word and put us ashore?", IsPurig, IsPlayer).Condition(() => Hero.OneToOneConversationHero == NavalStorylineData.Purig && BossFightOutCome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerDefeatedWaitingForConversation)
			.BeginPlayerOptions()
			.PlayerOption("{=da9N56ba}You won fairly, Purig. You and your men shall be put ashore.", IsPurig)
			.NpcLine("{=mnBuBKhI}[ib:demure2][if:convo_bemused]Good. Perhaps {QUEST_5_COMPANION.NAME} and I will find each other some day and settle things our own way, but you will never see me again.", IsPurig, IsPlayer)
			.Consequence(delegate
			{
				_isPurigKilledViaConversation = false;
				BossFightOutCome = Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerAcceptedTheDuelLostItAndLetPurigGo;
				StringHelpers.SetCharacterProperties("QUEST_5_COMPANION", NavalStorylineData.Gunnar.CharacterObject);
				Campaign.Current.ConversationManager.ConversationEndOneShot += BossFightAftermathConversationWithPurigConsequence;
			})
			.CloseDialog()
			.PlayerOption("{=fsumvsjK}I'll repay your treachery in your own coin. Finish him, lads!", IsPurig)
			.Consequence(delegate
			{
				_isPurigKilledViaConversation = true;
				BossFightOutCome = Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerAcceptedTheDuelLostItAndHadPurigKilledAnyway;
				Campaign.Current.ConversationManager.ConversationEndOneShot += BossFightAftermathConversationWithPurigConsequence;
			})
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
		TextObject textObject = new TextObject("{=FW5OE4fE}[ib:warrior][if:convo_delighted]{PLAYER.NAME}... {?PLAYER.GENDER}Sister{?}Brother{\\?}... Heaven's mercy, I had given up hope. I thought I'd die in that dark place, in the power of those cruel men.");
		textObject.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
		TextObject textObject2 = new TextObject("{=6Bx9b4JH}[ib:demure]Heaven bless you, {?PLAYER.GENDER}sister{?}brother{\\?}! I am ready to do my part, for our family and our future! But I can see your men calling you. Get us to safety, and we will speak again.");
		textObject2.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
		new TextObject("{=V52pdTgC}[ib:warrior]{PLAYER.NAME}... I hate to interrupt, but we need to move fast. We've got men badly hurt, and our water stocks are low. My lads won't be leaving any loot behind, though, not after they bled for it. We shall see you in Ostican!").SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
		string token;
		string token2;
		DialogFlow dialogFlow5 = DialogFlow.CreateDialogFlow("start", 1200).GenerateToken(out token).GenerateToken(out token2)
			.NpcLine(textObject, IsSister, IsPlayer)
			.Condition(() => _currentState == FreeTheSeaHoundsCaptivesQuestState.DefeatedPurig)
			.Consequence(delegate
			{
				SpawnBjolgur();
				_currentState = FreeTheSeaHoundsCaptivesQuestState.HeadBackToOstican;
			})
			.BeginPlayerOptions()
			.PlayerOption("{=iP0fWuZA}My sister... What you must have gone through...", outputToken: token, listenerDelegate: IsSister)
			.PlayerOption("{=0vwGcEoV}You're safe now. Rest. We can speak later.", outputToken: token, listenerDelegate: IsSister)
			.EndPlayerOptions()
			.NpcLine("{=CZ6yprOg}[if:convo_grave][ib:demure]That awful night... I awoke to cries and screaming and smoke. Father and mother... I won't speak of it. Some of those villains grabbed me and threw me over a horse. In the camp I saw our little brother, and my heart sank, but I did not see you, and that gave me hope.", IsSister, IsPlayer, token)
			.NpcLine("{=O5xn66z4}They separated us and took the younger stronger ones to be marched to the coast. They mocked us, telling us that we would be worked until our deaths on some hot island mine or on a frozen shoreline. I told them that you would come after me with an army of warriors and see them all hanged. I did not believe it, though... I just could not bear to have no answer to their taunts.", IsSister, IsPlayer)
			.NpcLine("{=ugyC5nt9}We arrived in Ostican. We were smuggled in by night, as the slave trade was banned by the Vlandian king, though many there clearly profited from it. Eventually Purig came to buy us. He questioned all of us closely, about our families. At first I thought he was trying to find out whether he could get a ransom for us, but no, he was trying to find someone related to you! He feared you, and was keeping me to protect himself from you! That made me proud, despite my misery.", IsSister, IsPlayer)
			.NpcLine("{=rTlhgDi8}[if:convo_calm_friendly][ib:normal]They threw me in that cell, where you found me, and we sailed from port to port. Sometimes I could press my ear to the door and I could hear Purig discussing his plans to topple the Nord king and build a pirate empire. And I heard your name again and again, as their schemes were foiled and the noose around his neck grew tighter. And then, just a short while ago, I heard your voice at the door of my cell, and I knew Heaven had answered my prayers!", IsSister, IsPlayer)
			.BeginPlayerOptions()
			.PlayerOption("{=JUwcYtEY}I would never have given up trying to rescue you, or our little brother or any of us!", outputToken: token2, listenerDelegate: IsSister)
			.PlayerOption("{=5J3vrPII}Our fortunes have changed. This morning you were a captive, but now you are a lady of rank.", outputToken: token2, listenerDelegate: IsSister)
			.EndPlayerOptions()
			.NpcLine(textObject2, IsSister, IsPlayer)
			.Consequence(base.CompleteQuestWithSuccess)
			.CloseDialog();
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow2);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow3);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow4);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow5);
		static bool IsGunnar(IAgent agent)
		{
			return agent.Character == NavalStorylineData.Gunnar.CharacterObject;
		}
		static bool IsPlayer(IAgent agent)
		{
			return agent.Character == CharacterObject.PlayerCharacter;
		}
		static bool IsPurig(IAgent agent)
		{
			return agent.Character == NavalStorylineData.Purig.CharacterObject;
		}
		static bool IsSister(IAgent agent)
		{
			return agent.Character == StoryModeHeroes.LittleSister.CharacterObject;
		}
	}

	private void SpawnBjolgur()
	{
		AgentBuildData agentBuildData = new AgentBuildData(NavalStorylineData.Bjolgur.CharacterObject);
		agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
		agentBuildData.InitialPosition(Mission.Current.Scene.FindEntityWithName("free_infantry_spawn_point_0").GlobalPosition);
		agentBuildData.InitialDirection(Agent.Main.LookDirection.AsVec2.Normalized());
		agentBuildData.NoHorses(noHorses: true);
		Agent agent = Mission.Current.SpawnAgent(agentBuildData);
		Campaign.Current.ConversationManager.AddConversationAgents(new Agent[1] { agent }, setActionsInstantly: true);
	}

	private void BossFightAftermathConversationWithPurigConsequence()
	{
		_currentState = FreeTheSeaHoundsCaptivesQuestState.DefeatedPurig;
		TextObject textObject;
		if (_isPurigKilledViaConversation)
		{
			textObject = new TextObject("{=T76bsVKF}Your men make quick work of Purig and his crew, assured that few will blame them for giving the Sea Hounds a taste of their own villainy. Meanwhile, you return to the roundship, which your men have already begun to search for loot and captives to free. As hopeful cries well up from the hold, they pry open the hatches, and look below.");
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, 50)
			});
		}
		else
		{
			textObject = new TextObject("{=bWFRemi6}Purig and his men jump into the waters of the bay and wade to shore. They disappear into the forested cliffs by the fjord. Meanwhile, you return to the Sea Hounds' roundship, which your men have already begun to search for loot and captives to free. As hopeful cries well up from the hold, they pry open the hatches, and look below.");
			TraitLevelingHelper.OnIssueSolvedThroughQuest(Hero.MainHero, new Tuple<TraitObject, int>[1]
			{
				new Tuple<TraitObject, int>(DefaultTraits.Honor, -50)
			});
			Clan.PlayerClan.AddRenown(50f);
		}
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=fNLTX4VS}Sister Saved").ToString(), textObject.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, GameTexts.FindText("str_ok").ToString(), string.Empty, DuelLostPopUpConsequence, null));
	}

	private bool GunnarInitialMeetingDialogCondition()
	{
		if (_currentState == FreeTheSeaHoundsCaptivesQuestState.EncounteredWithSeaHoundsParty && Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && NavalStorylineData.IsStorylineActivationPossible() && NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest4))
		{
			return Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(FreeTheSeaHoundsCaptivesQuest));
		}
		return false;
	}

	private void GunnarInitialMeetingDialogConsequence()
	{
		_currentState = FreeTheSeaHoundsCaptivesQuestState.TalkedWithGunnarBeforeFight;
	}

	private void DuelLostPopUpConsequence()
	{
		ShowNavalSaveSisterSceneNotification();
	}

	private void AddGameMenus()
	{
		AddGameMenu("act_3_quest_5_encounter_menu", new TextObject("{=oPap9pvt}You have arrived at your destination, Angranfjord. The entrance to the inlet between forested crags is hard to spot from the open sea, but Crusas points it out to you. You row forward in Crusas' ship while Bjolgur and Lahar hold back, keeping watch for the Shield Brother reinforcements. Soon you see a cluster of vessels, sitting at anchor. This must be Purig's fleet."), game_menu_encounter_on_init);
		AddGameMenuOption("act_3_quest_5_encounter_menu", "continue", new TextObject("{=DM6luo3c}Continue"), encounter_menu_continue_on_condition, encounter_menu_continue_on_consequence);
		AddGameMenu("act_3_quest_5_mission_menu", new TextObject("{=etH1IHNZ}You manage to put some distance between you and your enemies, and you have a moment to consider how to proceed."), mission_menu_on_init);
		AddGameMenuOption("act_3_quest_5_mission_menu", "checkpoint", new TextObject("{=mBAxWNpo}Try again from last checkpoint"), encounter_menu_checkpoint_on_condition, encounter_menu_checkpoint_on_consequence);
		AddGameMenuOption("act_3_quest_5_mission_menu", "start_over", new TextObject("{=lvbqEglM}Start over"), encounter_menu_start_over_on_condition, encounter_menu_start_over_on_consequence);
		AddGameMenuOption("act_3_quest_5_mission_menu", "leave", new TextObject("{=3sRdGQou}Leave"), encounter_menu_leave_on_condition, encounter_menu_leave_on_consequence, Isleave: true);
	}

	private void HandleMenuInitState()
	{
		if (_currentState == FreeTheSeaHoundsCaptivesQuestState.TalkedWithGunnarBeforeFight)
		{
			if (PlayerEncounter.Battle == null)
			{
				PlayerEncounter.StartBattle();
			}
			InitializeSetPieceBattleMission();
		}
		else if (_currentState == FreeTheSeaHoundsCaptivesQuestState.DefeatedPurig)
		{
			PlayerEncounter.LeaveEncounter = true;
			GameMenu.ExitToLast();
			if (BossFightOutCome != Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerAcceptedTheDuelLostItAndHadPurigKilledAnyway && BossFightOutCome != Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerAcceptedTheDuelLostItAndLetPurigGo)
			{
				ShowNavalSaveSisterSceneNotification();
			}
		}
		else if (_currentState == FreeTheSeaHoundsCaptivesQuestState.PlayerLostBossFight && BossFightOutCome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerDefeatedWaitingForConversation)
		{
			CampaignMission.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty), new ConversationCharacterData(NavalStorylineData.Purig.CharacterObject, _seaHoundsParty.Party));
		}
	}

	private void mission_menu_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All).WaitMeshName);
		HandleMenuInitState();
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest5MissionMenu);
	}

	private void game_menu_encounter_on_init(MenuCallbackArgs args)
	{
		if (_lastHitCheckpoint == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.None || _lastHitCheckpoint == Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase1Part1)
		{
			args.MenuContext.SetBackgroundMeshName("encounter_naval");
			HandleMenuInitState();
		}
		else
		{
			GameMenu.SwitchToMenu("act_3_quest_5_mission_menu");
		}
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest5EncounterMenu);
	}

	[GameMenuInitializationHandler("act_3_quest_5_encounter_menu")]
	private static void quest_game_menus_on_init_background(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All).WaitMeshName);
	}

	private bool encounter_menu_continue_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		return true;
	}

	private void encounter_menu_continue_on_consequence(MenuCallbackArgs args)
	{
		_currentState = FreeTheSeaHoundsCaptivesQuestState.EncounteredWithSeaHoundsParty;
		ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty);
		ConversationCharacterData conversationPartnerData = new ConversationCharacterData(NavalStorylineData.Gunnar.CharacterObject, PartyBase.MainParty);
		CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData);
		GameMenu.ActivateGameMenu("act_3_quest_5_mission_menu");
	}

	private bool encounter_menu_checkpoint_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return CanStartFromCheckPoint();
	}

	private void encounter_menu_checkpoint_on_consequence(MenuCallbackArgs args)
	{
		InitializeSetPieceBattleMission(_lastHitCheckpoint);
	}

	private bool encounter_menu_start_over_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return !CanStartFromCheckPoint();
	}

	private void encounter_menu_start_over_on_consequence(MenuCallbackArgs args)
	{
		InitializeSetPieceBattleMission();
	}

	private bool encounter_menu_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void encounter_menu_leave_on_consequence(MenuCallbackArgs args)
	{
		if (MobileParty.MainParty.MapEvent != null)
		{
			MenuHelper.EncounterLeaveConsequence();
		}
		NavalStorylineData.DeactivateNavalStoryline();
		GameMenu.ExitToLast();
	}

	private void InitializeSetPieceBattleMission(Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState checkpoint = Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase1Part1)
	{
		if (NavalStorylineData.Purig.PartyBelongedTo != _seaHoundsParty && !NavalStorylineData.Purig.IsDead)
		{
			if (NavalStorylineData.Purig.HeroState != Hero.CharacterStates.Active)
			{
				NavalStorylineData.Purig.ChangeState(Hero.CharacterStates.Active);
			}
			_seaHoundsParty.Party.MemberRoster.AddToCounts(NavalStorylineData.Purig.CharacterObject, 1);
		}
		NavalMissions.OpenNavalStorylineQuest5SetPieceBattleMission(NavalStorylineData.GetNavalMissionInitializerTemplate("naval_storyline_act_3_quest_5"), _seaHoundsParty, checkpoint);
	}

	protected override void OnStartQuestInternal()
	{
		AddLog(_findSeaHoundsQuestLog);
		CreateSeaHoundParty();
		AddTrackedObject(_skatriaIslandsMarker);
		foreach (Ship ship in MobileParty.MainParty.Ships)
		{
			if (ship.ShipHull.StringId == "nord_medium_ship")
			{
				ship.ChangeFigurehead(DefaultFigureheads.Raven);
				foreach (KeyValuePair<string, string> nordMediumShipyShipUpgradePiece in _nordMediumShipyShipUpgradePieceList)
				{
					if (!string.IsNullOrEmpty(nordMediumShipyShipUpgradePiece.Value))
					{
						ShipUpgradePiece newUpgradePiece = MBObjectManager.Instance.GetObject<ShipUpgradePiece>(nordMediumShipyShipUpgradePiece.Value);
						ship.EquipUpgradePiece(nordMediumShipyShipUpgradePiece.Key, newUpgradePiece);
					}
				}
			}
			else
			{
				if (!(ship.ShipHull.StringId == "aserai_heavy_ship"))
				{
					continue;
				}
				foreach (KeyValuePair<string, string> aseraiHeavyShipUpgradePiece in _aseraiHeavyShipUpgradePieceList)
				{
					if (!string.IsNullOrEmpty(aseraiHeavyShipUpgradePiece.Value))
					{
						ShipUpgradePiece newUpgradePiece2 = MBObjectManager.Instance.GetObject<ShipUpgradePiece>(aseraiHeavyShipUpgradePiece.Value);
						ship.EquipUpgradePiece(aseraiHeavyShipUpgradePiece.Key, newUpgradePiece2);
					}
				}
			}
		}
	}

	private void OnPartyVisibilityChanged(PartyBase party)
	{
		if (_currentState == FreeTheSeaHoundsCaptivesQuestState.GoToSeaHoundPartyPosition && party == _seaHoundsParty.Party && _seaHoundsParty.IsVisible)
		{
			AddLog(_arrivedAngranfjordQuestLog);
		}
	}

	private void CanHeroBecomePrisoner(Hero hero, ref bool result)
	{
		if (hero == Hero.MainHero)
		{
			result = false;
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		if (MobileParty.MainParty.MapEvent == mapEvent && mapEvent.HasWinner)
		{
			_ = mapEvent.WinningSide;
			_ = mapEvent.PlayerSide;
		}
	}

	private void OnHourlyTick()
	{
		if (_skatriaIslandsMarker.Position.Distance(MobileParty.MainParty.Position.AsVec3()) > 15f)
		{
			_skatriaIslandsMarker.IsVisibleOnMap = true;
		}
		else
		{
			_skatriaIslandsMarker.IsVisibleOnMap = false;
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		Quest5SetPieceBattleMissionController missionBehavior = ((Mission)mission).GetMissionBehavior<Quest5SetPieceBattleMissionController>();
		if (missionBehavior != null)
		{
			BossFightOutCome = missionBehavior.BossFightOutCome;
			_lastHitCheckpoint = missionBehavior.LastHitCheckpoint;
			_shouldMissionContinueFromCheckpoint = missionBehavior.ShouldMissionContinueFromCheckpoint;
		}
		if (_lastHitCheckpoint != Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.None && _lastHitCheckpoint < Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.End)
		{
			_currentState = FreeTheSeaHoundsCaptivesQuestState.RestartMission;
		}
		if (_currentState > FreeTheSeaHoundsCaptivesQuestState.TalkedWithGunnarBeforeFight)
		{
			if (_currentState == FreeTheSeaHoundsCaptivesQuestState.TalkedWithPurigBeforeBossFight && BossFightOutCome == Quest5SetPieceBattleMissionController.BossFightOutComeEnum.PlayerDefeatedWaitingForConversation)
			{
				_currentState = FreeTheSeaHoundsCaptivesQuestState.PlayerLostBossFight;
			}
			else if (PlayerEncounter.EncounteredMobileParty == _seaHoundsParty && MapEvent.PlayerMapEvent != null && MapEvent.PlayerMapEvent.HasWinner && MapEvent.PlayerMapEvent.WinningSide == mission.PlayerTeam.Side)
			{
				_currentState = FreeTheSeaHoundsCaptivesQuestState.DefeatedPurig;
			}
		}
	}

	protected override void OnFinalizeInternal()
	{
		DestroySeaHoundParty();
		if (NavalStorylineData.Purig.IsAlive)
		{
			KillCharacterAction.ApplyByRemove(NavalStorylineData.Purig);
		}
	}

	protected override void OnCompleteWithSuccessInternal()
	{
		GainRenownAction.Apply(Hero.MainHero, 30f);
		NavalStorylineData.OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint.Act3Quest5Succeeded);
	}

	private void CreateSeaHoundParty()
	{
		Hideout hideout = SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All, (Settlement x) => x.IsActive);
		Clan clan = Clan.All.FirstOrDefault((Clan x) => x.StringId == "northern_pirates");
		PartyTemplateObject partyTemplateObject = Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("storyline_act3_quest_5_sea_hounds_template") ?? clan.DefaultPartyTemplate;
		_seaHoundsParty = BanditPartyComponent.CreateBanditParty("free_the_sea_hounds_captives_initial_quest_party", clan, hideout.Settlement.Hideout, isBossParty: false, partyTemplateObject, _seaHoundsSpawnPosition);
		_seaHoundsParty.Party.SetCustomName(new TextObject("{=SKC3FeGR}Sea Hounds"));
		_seaHoundsParty.SetPartyUsedByQuest(isActivelyUsed: true);
		_seaHoundsParty.IsInfoHidden = true;
		_seaHoundsParty.IgnoreByOtherPartiesTill(CampaignTime.Years(999f));
		_seaHoundsParty.SetLandNavigationAccess(access: false);
		_seaHoundsParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		_seaHoundsParty.Party.SetCustomBanner(NavalStorylineData.CorsairBanner);
		MobileParty.UpdateLocator(_seaHoundsParty);
		_seaHoundsParty.MemberRoster.Clear();
		FillParty(_seaHoundsParty, partyTemplateObject, TaleWorlds.Library.MathF.Round(67f * _strengthModifier));
		AddTrackedObject(_seaHoundsParty);
		foreach (Ship ship in _seaHoundsParty.Ships)
		{
			ship.ChangeFigurehead(DefaultFigureheads.Dragon);
			foreach (KeyValuePair<string, string> seaHoundPartyShipUpgradePiece in _seaHoundPartyShipUpgradePieceList)
			{
				if (!string.IsNullOrEmpty(seaHoundPartyShipUpgradePiece.Value))
				{
					ShipUpgradePiece newUpgradePiece = MBObjectManager.Instance.GetObject<ShipUpgradePiece>(seaHoundPartyShipUpgradePiece.Value);
					ship.EquipUpgradePiece(seaHoundPartyShipUpgradePiece.Key, newUpgradePiece);
				}
			}
		}
		_seaHoundsParty.InitializePartyTrade(QuestHelper.CalculateInitialGoldForBanditQuestParty(_seaHoundsParty));
	}

	private void DestroySeaHoundParty()
	{
		if (_seaHoundsParty != null && _seaHoundsParty.IsActive)
		{
			DestroyPartyAction.Apply(null, _seaHoundsParty);
		}
	}

	private static void FillParty(MobileParty mobileParty, PartyTemplateObject partyTemplate, int desiredMenCount)
	{
		float num = 0f;
		int num2 = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MinValue);
		int num3 = partyTemplate.Stacks.Sum((PartyTemplateStack s) => s.MaxValue);
		num = ((desiredMenCount < num2) ? ((float)desiredMenCount / (float)num2 - 1f) : ((num2 > desiredMenCount || desiredMenCount > num3) ? ((float)desiredMenCount / (float)num3) : ((float)(desiredMenCount - num2) / (float)(num3 - num2))));
		for (int num4 = 0; num4 < partyTemplate.Stacks.Count; num4++)
		{
			PartyTemplateStack partyTemplateStack = partyTemplate.Stacks[num4];
			int minValue = partyTemplateStack.MinValue;
			int maxValue = partyTemplateStack.MaxValue;
			int num5 = ((-1f <= num && num < 0f) ? MBRandom.RoundRandomized((float)minValue + (float)minValue * num) : ((!(0f <= num) || !(num <= 1f)) ? MBRandom.RoundRandomized((float)maxValue * num) : MBRandom.RoundRandomized((float)minValue + (float)(maxValue - minValue) * num)));
			if (num5 > 0)
			{
				mobileParty.MemberRoster.AddToCounts(partyTemplateStack.Character, num5);
			}
		}
		while (mobileParty.MemberRoster.TotalManCount > desiredMenCount)
		{
			int index = MBRandom.RoundRandomized(MBRandom.RandomFloatRanged(partyTemplate.Stacks.Count - 1));
			CharacterObject character = partyTemplate.Stacks[index].Character;
			mobileParty.MemberRoster.AddToCounts(character, -1);
		}
		while (mobileParty.MemberRoster.TotalManCount < desiredMenCount)
		{
			int index2 = MBRandom.RoundRandomized(MBRandom.RandomFloatRanged(partyTemplate.Stacks.Count - 1));
			CharacterObject character2 = partyTemplate.Stacks[index2].Character;
			mobileParty.MemberRoster.AddToCounts(character2, 1);
		}
	}

	private void ShowNavalSaveSisterSceneNotification()
	{
		if (!_isSisterSavedSceneNotificationTriggered)
		{
			MBInformationManager.ShowSceneNotification(new NavalSaveSisterSceneNotificationItem(Hero.MainHero, StoryModeHeroes.LittleSister, OnNavalSaveSisterSceneNotificationClosed));
			_isSisterSavedSceneNotificationTriggered = true;
		}
	}

	private void OnNavalSaveSisterSceneNotificationClosed()
	{
		ConversationCharacterData playerCharacterData = new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true, noWeapon: true, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
		ConversationCharacterData conversationPartnerData = new ConversationCharacterData(StoryModeHeroes.LittleSister.CharacterObject, PartyBase.MainParty, noHorse: true, noWeapon: true, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: true, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true);
		CampaignMission.OpenConversationMission(playerCharacterData, conversationPartnerData, "conversation_scene_sea_multi_agent", "", isMultiAgentConversation: true);
	}

	private void ShowAllyDefeatedPopUp()
	{
		TextObject textObject = new TextObject("{=cH3Kpkwg}Ally Defeated");
		InformationManager.ShowInquiry(new InquiryData(affirmativeText: new TextObject("{=DM6luo3c}Continue").ToString(), titleText: textObject.ToString(), text: _allyDefeatedText.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, negativeText: null, affirmativeAction: OnAllyDefeatedPopUpClosed, negativeAction: null), pauseGameActiveState: true);
	}

	private void OnAllyDefeatedPopUpClosed()
	{
		CompleteQuestWithCancel(_allyDefeatedText);
		NavalStorylineData.DeactivateNavalStoryline();
	}

	private bool CanStartFromCheckPoint()
	{
		if (_lastHitCheckpoint != Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.None)
		{
			return _lastHitCheckpoint != Quest5SetPieceBattleMissionController.Quest5SetPieceBattleMissionState.InitializePhase1Part1;
		}
		return false;
	}
}
