using System.Collections.Generic;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace NavalDLC.Storyline.Quests;

public class InquireAtOstican : QuestBase
{
	[SaveableField(1)]
	private bool _isGunnarSaved;

	private bool _playCutscene;

	private bool _northernerQuestOptions2Answer1Selected;

	private bool _northernerQuestOptions1Selected;

	public override TextObject Title
	{
		get
		{
			TextObject textObject = new TextObject("{=GOYpy4gI}Inquire at {SETTLEMENT}");
			textObject.SetTextVariable("SETTLEMENT", NavalStorylineData.HomeSettlement.Name);
			return textObject;
		}
	}

	public override bool IsRemainingTimeHidden => true;

	public override string SpecialQuestType => "NavalStoryline";

	private TextObject _questStartLog
	{
		get
		{
			TextObject textObject = new TextObject("{=JFNtXUF2}You have heard that bandits might be selling captives to pirates on the Vlandian coast, and the port of {SETTLEMENT} might be a good place to start.");
			textObject.SetTextVariable("SETTLEMENT", NavalStorylineData.HomeSettlement.Name);
			return textObject;
		}
	}

	private TextObject _isGunnarSavedLog
	{
		get
		{
			TextObject textObject = new TextObject("{=Rynxrlis}You met {GUNNAR.LINK} after helping him fight off some attackers. He suggested you come on a voyage north with him. Go to the tavern at {SETTLEMENT} and talk to his comrade {NORTHERNER.LINK}.");
			textObject.SetCharacterProperties("GUNNAR", NavalStorylineData.Gunnar.CharacterObject);
			textObject.SetCharacterProperties("NORTHERNER", NavalStorylineData.Purig.CharacterObject);
			textObject.SetTextVariable("SETTLEMENT", NavalStorylineData.HomeSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _tutorialSkippedLog
	{
		get
		{
			TextObject textObject = new TextObject("{=3mvfEsqk}You declined to join {GUNNAR.LINK} on his voyage, but may be able to find him later at {SETTLEMENT}.");
			textObject.SetCharacterProperties("GUNNAR", NavalStorylineData.Gunnar.CharacterObject);
			textObject.SetTextVariable("SETTLEMENT", NavalStorylineData.HomeSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _cancelQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=nHc1jonU}You decided to stop searching for your sister.");
			textObject.SetCharacterProperties("NORTHERNER", NavalStorylineData.Purig.CharacterObject);
			textObject.SetTextVariable("SETTLEMENT", NavalStorylineData.HomeSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	public InquireAtOstican()
		: base("inquire_at_ostican", null, CampaignTime.Never, 0)
	{
	}

	protected override void OnStartQuest()
	{
		base.OnStartQuest();
		AddLog(_questStartLog);
		AddTrackedObject(NavalStorylineData.HomeSettlement);
	}

	protected override void SetDialogs()
	{
		AddNorthernerDialog();
	}

	protected override void InitializeQuestOnGameLoad()
	{
		if (_isGunnarSaved)
		{
			SetDialogs();
		}
	}

	protected override void RegisterEvents()
	{
		NavalDLCEvents.OnGunnarSavedEvent.AddNonSerializedListener(this, OnGunnarSaved);
		NavalDLCEvents.OnNavalStorylineCanceledEvent.AddNonSerializedListener(this, OnNavalStorylineCanceled);
		CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, LocationCharactersAreReadyToSpawn);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		NavalDLCEvents.OnNavalStorylineTutorialSkippedEvent.AddNonSerializedListener(this, OnNavalTutorialSkipped);
	}

	private void OnGunnarSaved()
	{
		_isGunnarSaved = true;
		SetDialogs();
		AddLog(_isGunnarSavedLog);
		AddTrackedObject(NavalStorylineData.Purig);
		NavalStorylineData.Gunnar.SetPersonalRelation(Hero.MainHero, 15);
	}

	private void OnNavalTutorialSkipped()
	{
		AddLog(_tutorialSkippedLog);
		CompleteQuestWithSuccess();
		NavalStorylineData.Gunnar.SetPersonalRelation(Hero.MainHero, 10);
	}

	private void OnNavalStorylineCanceled(NavalStorylineData.StorylineCancelDetail detail)
	{
		if (NavalStorylineData.Gunnar.IsActive)
		{
			DisableHeroAction.Apply(NavalStorylineData.Gunnar);
			Location location = Settlement.CurrentSettlement.LocationComplex?.GetLocationOfCharacter(NavalStorylineData.Gunnar);
			if (location != null && location.GetLocationCharacter(NavalStorylineData.Gunnar) != null)
			{
				Settlement.CurrentSettlement.LocationComplex.RemoveCharacterIfExists(NavalStorylineData.Gunnar);
				PlayerEncounter.LocationEncounter?.RemoveAccompanyingCharacter(NavalStorylineData.Gunnar);
			}
		}
		CompleteQuestWithFail(_cancelQuestLog);
	}

	protected override void HourlyTick()
	{
	}

	public override GameMenuOption.IssueQuestFlags IsLocationTrackedByQuest(Location location)
	{
		if (Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement)
		{
			if (_isGunnarSaved)
			{
				if (location.StringId == "tavern" && !location.ContainsCharacter(NavalStorylineData.Purig))
				{
					return GameMenuOption.IssueQuestFlags.TrackedStoryQuest;
				}
			}
			else if (location.StringId == "port")
			{
				return GameMenuOption.IssueQuestFlags.ActiveStoryQuest;
			}
		}
		return GameMenuOption.IssueQuestFlags.None;
	}

	private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
	{
		Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
		if (NavalStorylineData.HomeSettlement == settlement && settlement.IsTown && CampaignMission.Current != null)
		{
			Location location = CampaignMission.Current.Location;
			if (location != null && location.StringId == "tavern" && !NavalStorylineData.Purig.IsDead && _isGunnarSaved)
			{
				location.AddLocationCharacters(CreateNortherner, settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
			}
		}
	}

	private void OnGameMenuOpened(MenuCallbackArgs args)
	{
		if (_playCutscene && GameStateManager.Current.ActiveState is MapState)
		{
			_playCutscene = false;
			VideoPlaybackState videoPlaybackState = Game.Current.GameStateManager.CreateState<VideoPlaybackState>();
			string text = ModuleHelper.GetModuleFullPath("NavalDLC") + "Videos/Storyline/";
			string subtitleFileBasePath = text + "naval_storyline_intro";
			float frameRate = 24f;
			string videoPath = text + "naval_storyline_intro_cinematic.ivf";
			string audioPath = text + "naval_storyline_intro_cinematic.ogg";
			videoPlaybackState.SetStartingParameters(videoPath, audioPath, subtitleFileBasePath, frameRate);
			videoPlaybackState.SetOnVideoFinisedDelegate(OnCinematicCompleted);
			Game.Current.GameStateManager.PushState(videoPlaybackState);
		}
	}

	private LocationCharacter CreateNortherner(CultureObject culture, LocationCharacter.CharacterRelations relation)
	{
		CharacterObject characterObject = NavalStorylineData.Purig.CharacterObject;
		Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement");
		AgentData agentData = new AgentData(new SimpleAgentOrigin(characterObject)).Monster(monsterWithSuffix);
		return new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddCompanionBehaviors, "sp_storyline_npc", fixedLocation: true, relation, ActionSetCode.GenerateActionSetNameWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, "_villager"), useCivilianEquipment: true);
	}

	private void AddNorthernerDialog()
	{
		DialogFlow dialogFlow = DialogFlow.CreateDialogFlow("start", 1200);
		dialogFlow.AddDialogLine("northerner_meet_dialog_start_before_met", "start", "northerner_meet_dialog_player_options", "{=ay0tHozl}[ib:aggressive]Aye? So who're you, then?", northerner_meet_dialog_start_before_met_on_condition, null, this, 1200);
		dialogFlow.AddPlayerLine("northerner_meet_dialog_player_options_1", "northerner_meet_dialog_player_options", "northerner_meet_dialog_continue", "{=HXnni7no}I am {PLAYER.NAME}. Gunnar sent me. We were in a fight.", null, null, this);
		dialogFlow.AddPlayerLine("northerner_meet_dialog_player_options_2", "northerner_meet_dialog_player_options", "northerner_meet_dialog_continue", "{=O4kwRlyY}I helped out Gunnar in a fight. He said he planned to sail with you.", null, null, this);
		dialogFlow.AddDialogLine("northerner_meet_dialog_continue_1_line", "northerner_meet_dialog_continue", "northerner_meet_dialogue_continue_2", "{=4K9ycbC8}[ib:hip][if:convo_thinking]A fight, you say… I take it that Gunnar and you won?", null, null, this);
		dialogFlow.AddPlayerLine("northerner_meet_dialog_continue_2_line", "northerner_meet_dialogue_continue_2", "northerner_meet_dialog_aftermath", "{=uyWWPIxA}Yes, we defeated three Sea Hounds. Now I wish to sail with you.", null, null, this);
		dialogFlow.AddPlayerLine("northerner_meet_dialog_continue_3_line", "northerner_meet_dialogue_continue_2", "northerner_meet_dialog_aftermath", "{=Ic4e9HVF}We won, and now I wish to join you against our common enemy.", null, null, this);
		dialogFlow.AddDialogLine("northerner_meet_dialog_aftermath_line_1", "northerner_meet_dialog_aftermath", "northerner_meet_dialog_aftermath_2", "{=Ni7ienXY}[ib:confident][if:convo_bemused]Well... Good for you two! Gunnar is a tough old goat and rather hard to kill. I shall have to ask him all about it when I get the chance. So... Yes, I agreed to help him in his little feud with the Sea Hounds, for old time's sake. I've got my ship and men ready to sail.", null, null, this);
		dialogFlow.AddDialogLine("northerner_meet_dialog_aftermath_line_2", "northerner_meet_dialog_aftermath_2", "northerner_quest_options", "{=0JNfhDrT}[ib:closed2]If you're indeed of a mind to go with us, I'm happy to take you. But I've got room for only you. So if you've got any traveling companions, you'll need to leave them in this port. I'm sure you'll be back soon to rejoin them, safe and sound.", null, null, this);
		ConversationSentence.OnConsequenceDelegate consequenceDelegate = northerner_quest_options_1_consequence;
		ConversationSentence.OnClickableConditionDelegate clickableConditionDelegate = northerner_quest_options_1_clickable_condition;
		dialogFlow.AddPlayerLine("northerner_quest_options_1_line", "northerner_quest_options", "northerner_quest_options_1", "{=S1ES8FFM}I'd feel better if my men could come along as well...", null, consequenceDelegate, this, 100, clickableConditionDelegate);
		dialogFlow.AddDialogLine("northerner_quest_options_1_line_continue", "northerner_quest_options_1", "northerner_quest_options", "{=MjIfvPk9}The northern seas aren't for everyone! Even if you had your own ship, it would just slow us down. Don't worry, me and my boys know those waters like the back of our hands. We won't let you slip overboard.", null, null, this);
		ConversationSentence.OnConsequenceDelegate consequenceDelegate2 = northerner_quest_options_2_answer_1_consequence;
		clickableConditionDelegate = northerner_quest_options_2_answer_1_clickable_condition;
		dialogFlow.AddPlayerLine("northerner_quest_options_2_line", "northerner_quest_options", "northerner_quest_options_2_answer_1", "{=R6CH1xOc}Did you also fight in this rebellion with Gunnar?", null, consequenceDelegate2, this, 100, clickableConditionDelegate);
		dialogFlow.AddDialogLine("northerner_quest_options_2_continue_1", "northerner_quest_options_2_answer_1", "northerner_quest_options_2_answer_2", "{=sfuFR9fr}[if:convo_focused_happy][ib:confident2]I did, I did. We started out as young men with nothing but our swords, our sweet mistress the sea whispering promises of wealth and glory in our ears... We served no kings and had no lords. Those were fine times!", null, null, this);
		dialogFlow.AddDialogLine("northerner_quest_options_2_continue_2", "northerner_quest_options_2_answer_2", "northerner_quest_options_2_answer_3", "{=pGeYLxkL}[if:convo_thinking][ib:closed2]Then old Volbjorn brought down the full weight of the north on our brotherhood. Against those odds we could not fight. But some of our old comrades weren't quite ready to abandon that life, and they  turned pirate and became the Sea Hounds...", null, null, this);
		dialogFlow.AddDialogLine("northerner_quest_options_2_continue_3", "northerner_quest_options_2_answer_3", "northerner_quest_options", "{=06hn50KS}[ib:closed2][if:convo_bemused]Now Gunnar says they are even worse than the king and the jarls we fought, preying upon the farmers and fishermen of the coast. There's no honor in attacking the weak, he told me so many times. And he's right, of course - it's just that it's so much easier to take their wealth!", null, null, this);
		dialogFlow.AddPlayerLine("northerner_quest_options_3_line", "northerner_quest_options", "northerner_quest_options_3_answer", "{=roU1EPwp}Very well. I'll make ready to sail.", null, null, this, 100, CanSetSailWithNortherner);
		dialogFlow.AddDialogLine("northerner_quest_options_3_continue", "northerner_quest_options_3_answer", "close_window", "{=5LbipyXT}[ib:confident3]Come down to the ship with me, then! Wind and tide are with us, and I won't tarry long.", null, northerner_quest_options_3_continue_on_consequence, this);
		dialogFlow.AddPlayerLine("northerner_quest_options_4_line", "northerner_quest_options", "northerner_quest_options_4_answer", "{=18bzzaFH}I'm not ready to sail just yet.", null, null, this);
		dialogFlow.AddDialogLine("northerner_quest_options_4_continue", "northerner_quest_options_4_answer", "close_window", "{=s9Rz14CU}[if:convo_mocking_teasing]Are you sure you're cut out for a life at sea? Make haste when wind and tide are with you, friend! Anyway, come back when you're ready.", null, null, this);
		dialogFlow.AddDialogLine("northerner_meet_dialog_start_after_met", "start", "northerner_returned_options", "{=b9hRGOhC}[if:convo_mocking_teasing]All is good? Packed your bag, kissed your mother and your sweetheart good-bye? Of course my lads and I won't mind if you want to tarry here a little longer. Oh no. There's no hurry at all.", northerner_meet_dialog_start_after_met_on_condition, null, this, 1200);
		dialogFlow.AddPlayerLine("northerner_returned_options_1", "northerner_returned_options", "northerner_quest_options_3_answer", "{=nLM7Lu2m}All is good. I am ready to sail.", null, null, this, 100, CanSetSailWithNortherner);
		dialogFlow.AddPlayerLine("northerner_returned_options_2", "northerner_returned_options", "northerner_quest_options_4_answer", "{=18bzzaFH}I'm not ready to sail just yet.", null, null, this);
		Campaign.Current.ConversationManager.AddDialogFlow(dialogFlow);
	}

	private bool CanSetSailWithNortherner(out TextObject reasonText)
	{
		reasonText = null;
		bool num = NavalStorylineData.IsStorylineActivationPossible();
		if (!num)
		{
			if (MobileParty.MainParty.Army != null)
			{
				reasonText = new TextObject("{=q9fzW0W3}You cannot do this while you are in an army.");
				return num;
			}
			if (Campaign.Current.IsMainHeroDisguised)
			{
				reasonText = new TextObject("{=V9Ub68T7}You cannot do this while disguised.");
				return num;
			}
			reasonText = new TextObject("{=H6F5BxgB}This isn't the right time.");
		}
		return num;
	}

	private bool northerner_meet_dialog_came_back_on_condition()
	{
		if (Hero.OneToOneConversationHero == NavalStorylineData.Purig)
		{
			return Hero.OneToOneConversationHero.HasMet;
		}
		return false;
	}

	private bool northerner_meet_dialog_start_before_met_on_condition()
	{
		StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
		if (Hero.OneToOneConversationHero == NavalStorylineData.Purig && !Hero.OneToOneConversationHero.HasMet)
		{
			return NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.None);
		}
		return false;
	}

	private bool northerner_meet_dialog_start_after_met_on_condition()
	{
		StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject);
		if (Hero.OneToOneConversationHero == NavalStorylineData.Purig && Hero.OneToOneConversationHero.HasMet)
		{
			return NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.None);
		}
		return false;
	}

	private void northerner_quest_options_1_consequence()
	{
		_northernerQuestOptions1Selected = true;
	}

	private bool northerner_quest_options_1_clickable_condition(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		return !_northernerQuestOptions1Selected;
	}

	private void northerner_quest_options_2_answer_1_consequence()
	{
		_northernerQuestOptions2Answer1Selected = true;
	}

	private bool northerner_quest_options_2_answer_1_clickable_condition(out TextObject explanation)
	{
		explanation = TextObject.GetEmpty();
		return !_northernerQuestOptions2Answer1Selected;
	}

	private void northerner_quest_options_3_continue_on_consequence()
	{
		Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
		{
			NavalStorylineData.OnPlayerAcceptsQuest(OnDialogueEnded, NavalStorylineData.OnPlayerPostponedQuestStart);
		};
	}

	private void OnDialogueEnded()
	{
		_playCutscene = true;
		Mission.Current?.EndMission();
	}

	private void OnCinematicCompleted()
	{
		GameStateManager.Current.PopState();
		Settlement.CurrentSettlement.LocationComplex.RemoveCharacterIfExists(NavalStorylineData.Purig);
		CompleteQuestWithSuccess();
		new DefeatTheCaptorsQuest("naval_storyline_defeat_the_captors_quest").StartQuest();
	}
}
