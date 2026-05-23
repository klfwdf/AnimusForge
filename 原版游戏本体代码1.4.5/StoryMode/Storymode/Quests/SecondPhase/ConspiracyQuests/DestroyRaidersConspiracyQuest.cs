using System.Collections.Generic;
using System.Linq;
using Helpers;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace StoryMode.Quests.SecondPhase.ConspiracyQuests;

public class DestroyRaidersConspiracyQuest : ConspiracyQuestBase
{
	private const int QuestSuccededRelationBonus = 5;

	private const int QuestSucceededSecurityBonus = 5;

	private const int QuestSuceededProsperityBonus = 5;

	private const int QuestSuceededRenownBonus = 5;

	private const int QuestFailedRelationPenalty = -5;

	private const int NumberOfRegularRaidersToSpawn = 3;

	[SaveableField(1)]
	private readonly Settlement _targetSettlement;

	[SaveableField(2)]
	private readonly List<MobileParty> _regularRaiderParties;

	[SaveableField(3)]
	private MobileParty _specialRaiderParty;

	[SaveableField(4)]
	private JournalLog _regularPartiesProgressTracker;

	[SaveableField(5)]
	private JournalLog _specialPartyProgressTracker;

	[SaveableField(6)]
	private Clan _banditFaction;

	[SaveableField(7)]
	private CharacterObject _conspiracyCaptainCharacter;

	[SaveableField(8)]
	private Settlement _closestHideout;

	[SaveableField(9)]
	private List<MobileParty> _directedRaidersToEngagePlayer;

	private float RaiderPartyPlayerEncounterRadius => Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius * 3f;

	public override TextObject Title => new TextObject("{=DfiACGay}Destroy Raiders");

	public override float ConspiracyStrengthDecreaseAmount => 50f;

	private int RegularRaiderPartyTroopCount => 17 + MathF.Ceiling(23f * Campaign.Current.Models.IssueModel.GetIssueDifficultyMultiplier());

	private int SpecialRaiderPartyTroopCount => 33 + MathF.Ceiling(37f * Campaign.Current.Models.IssueModel.GetIssueDifficultyMultiplier());

	public override TextObject StartLog
	{
		get
		{
			TextObject textObject = new TextObject("{=Dr63pCHt}{MENTOR.LINK} has sent you a message about bandit attacks near {TARGET_SETTLEMENT}, and advises you to go there and eliminate them all before their actions turn the locals against your movement. ");
			StringHelpers.SetCharacterProperties("MENTOR", base.QuestGiver.CharacterObject, textObject);
			textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	public override TextObject StartMessageLogFromMentor
	{
		get
		{
			TextObject textObject = new TextObject("{=V5K8RpAa}{MENTOR.LINK}'s message: “Greetings, {PLAYER.NAME}. We have a new problem. I've had reports from my agents of unusual bandit activity near {TARGET_SETTLEMENT}. They appear to be raiding and killing travellers {?IS_EMPIRE}under the protection of the Empire{?}who aren't under the protection of the Empire{\\?}, and leaving the others alone. This seems very much like the work of {NEMESIS_MENTOR.LINK}, to terrorize local merchants so that no one will stand up for our cause. I advise you to wipe these bandits out as quickly as possible. That would send a good message, both to our allies and our enemies.”");
			StringHelpers.SetCharacterProperties("MENTOR", base.QuestGiver.CharacterObject, textObject);
			StringHelpers.SetCharacterProperties("PLAYER", Hero.MainHero.CharacterObject, textObject);
			bool isOnImperialQuestLine = StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine;
			StringHelpers.SetCharacterProperties("NEMESIS_MENTOR", isOnImperialQuestLine ? StoryModeHeroes.AntiImperialMentor.CharacterObject : StoryModeHeroes.ImperialMentor.CharacterObject, textObject);
			textObject.SetTextVariable("IS_IMPERIAL", isOnImperialQuestLine ? 1 : 0);
			textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	public override TextObject SideNotificationText
	{
		get
		{
			TextObject textObject = new TextObject("{=T7OTmJUp}{MENTOR.LINK} has a message for you");
			StringHelpers.SetCharacterProperties("MENTOR", base.QuestGiver.CharacterObject, textObject);
			return textObject;
		}
	}

	private TextObject _destroyRaidersQuestSucceededLogText
	{
		get
		{
			TextObject textObject = new TextObject("{=qg05CSZb}You have defeated all the raiders near {TARGET_SETTLEMENT}. Many people now hope you can bring peace and prosperity back to the region.");
			textObject.SetTextVariable("TARGET_SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _destroyRaidersQuestFailedOnTimedOutLogText => new TextObject("{=DaBN0O7N}You have failed to defeat all raider parties in time. Many of the locals feel that you've brought misfortune upon them, and want nothing to do with you.");

	private TextObject _destroyRaidersQuestFailedOnPlayerDefeatedByRaidersLogText => new TextObject("{=mN60B07k}You have lost the battle against raiders and failed to defeat conspiracy forces. Many of the locals feel that you've brought misfortune upon them, and want nothing to do with you.");

	private TextObject _destroyRaidersRegularPartiesProgress
	{
		get
		{
			TextObject textObject = new TextObject("{=dbLb3krw}Hunt the gangs of {RAIDER_NAME}");
			textObject.SetTextVariable("RAIDER_NAME", _banditFaction.Name);
			return textObject;
		}
	}

	private TextObject _destroyRaidersSpecialPartyProgress => new TextObject("{=QVkuaezc}Hunt the conspiracy war party");

	private TextObject _destroyRaidersRegularProgressNotification
	{
		get
		{
			TextObject textObject = new TextObject("{=US0VAHiE}You have eliminated a {RAIDER_NAME} party.");
			textObject.SetTextVariable("RAIDER_NAME", _banditFaction.Name);
			return textObject;
		}
	}

	private TextObject _destroyRaidersRegularProgressCompletedNotification
	{
		get
		{
			TextObject textObject = new TextObject("{=LfH7VXDH}You have eliminated all {RAIDER_NAME} gangs in the vicinity.");
			textObject.SetTextVariable("RAIDER_NAME", _banditFaction.Name);
			return textObject;
		}
	}

	private TextObject _destroyRaidersSpecialPartyInformationQuestLog
	{
		get
		{
			TextObject textObject = new TextObject("{=agrsO3qQ}Due to your successful skirmishes against {RAIDER_NAME}, a conspiracy war party is now patrolling around {SETTLEMENT}.");
			textObject.SetTextVariable("RAIDER_NAME", _banditFaction.Name);
			textObject.SetTextVariable("SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	private TextObject _destroyRaidersSpecialPartySpawnNotification
	{
		get
		{
			TextObject textObject = new TextObject("{=QOVLkdTp}A conspiracy war party is now patrolling around {SETTLEMENT}.");
			textObject.SetTextVariable("SETTLEMENT", _targetSettlement.EncyclopediaLinkWithName);
			return textObject;
		}
	}

	public DestroyRaidersConspiracyQuest(string questId, Hero questGiver)
		: base(questId, questGiver)
	{
		_regularRaiderParties = new List<MobileParty>(3);
		_directedRaidersToEngagePlayer = new List<MobileParty>(3);
		_targetSettlement = DetermineTargetSettlement();
		_banditFaction = GetBanditTypeForSettlement(_targetSettlement);
	}

	protected override void SetDialogs()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(GetConspiracyCaptainDialogue(), this);
	}

	protected override void RegisterEvents()
	{
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroTakenPrisoner);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, MobilePartyDestroyed);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
	}

	private void OnGameMenuOpened(MenuCallbackArgs menuCallbackArgs)
	{
		if (menuCallbackArgs.MenuContext.GameMenu.StringId == "prisoner_wait")
		{
			PartyBase captorParty = PlayerCaptivity.CaptorParty;
			if (captorParty != null && captorParty.IsMobile && (_regularRaiderParties.Contains(PlayerCaptivity.CaptorParty.MobileParty) || _specialRaiderParty == PlayerCaptivity.CaptorParty.MobileParty))
			{
				OnQuestFailedByDefeat();
			}
		}
	}

	protected override void InitializeQuestOnGameLoad()
	{
		SetDialogs();
		DetermineClosestHideouts();
		if (_directedRaidersToEngagePlayer == null)
		{
			_directedRaidersToEngagePlayer = new List<MobileParty>(3);
			return;
		}
		if (_directedRaidersToEngagePlayer.Count > _regularRaiderParties.Count)
		{
			_directedRaidersToEngagePlayer = new List<MobileParty>(3);
			{
				foreach (MobileParty regularRaiderParty in _regularRaiderParties)
				{
					SetDefaultRaiderAi(regularRaiderParty);
				}
				return;
			}
		}
		foreach (MobileParty regularRaiderParty2 in _regularRaiderParties)
		{
			CheckRaiderPartyPlayerEncounter(regularRaiderParty2);
		}
	}

	protected override void OnStartQuest()
	{
		base.OnStartQuest();
		string objectName = (StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine ? "conspiracy_commander_antiempire" : "conspiracy_commander_empire");
		_conspiracyCaptainCharacter = Game.Current.ObjectManager.GetObject<CharacterObject>(objectName);
		InitializeRaiders();
		_regularPartiesProgressTracker = AddDiscreteLog(_destroyRaidersRegularPartiesProgress, TextObject.GetEmpty(), 0, 3);
		SetDialogs();
		InitializeQuestOnCreation();
	}

	private Settlement DetermineTargetSettlement()
	{
		Settlement settlement = null;
		Settlement centralSettlement = StoryModeHeroes.ImperialMentor.HomeSettlement;
		if (!Clan.PlayerClan.Settlements.IsEmpty())
		{
			settlement = Clan.PlayerClan.Settlements.GetRandomElementWithPredicate((Settlement t) => (t.IsTown || t.IsCastle) && Campaign.Current.Models.MapDistanceModel.PathExistBetweenPoints(t.GatePosition, centralSettlement.GatePosition, MobileParty.NavigationType.Default));
		}
		else
		{
			MBList<Settlement> mBList = StoryModeManager.Current.MainStoryLine.PlayerSupportedKingdom.Settlements.Where((Settlement t) => t.IsTown || t.IsCastle).ToMBList();
			if (!mBList.IsEmpty())
			{
				settlement = mBList.GetRandomElementWithPredicate((Settlement t) => Campaign.Current.Models.MapDistanceModel.PathExistBetweenPoints(t.GatePosition, centralSettlement.GatePosition, MobileParty.NavigationType.Default));
			}
		}
		if (settlement == null)
		{
			Debug.FailedAssert("Destroy raiders conspiracy quest settlement is null", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\Quests\\SecondPhase\\ConspiracyQuests\\DestroyRaidersConspiracyQuest.cs", "DetermineTargetSettlement", 306);
			settlement = Settlement.All.GetRandomElementWithPredicate((Settlement t) => (t.IsTown || t.IsCastle) && Campaign.Current.Models.MapDistanceModel.PathExistBetweenPoints(t.GatePosition, centralSettlement.GatePosition, MobileParty.NavigationType.Default));
		}
		return settlement;
	}

	private void InitializeRaiders()
	{
		List<Settlement> source = DetermineClosestHideouts();
		for (int i = 0; i < 3; i++)
		{
			SpawnRaiderPartyAtHideout(source.ElementAt(i));
		}
	}

	private List<Settlement> DetermineClosestHideouts()
	{
		MapDistanceModel model = Campaign.Current.Models.MapDistanceModel;
		Settlement centralSettlement = StoryModeHeroes.ImperialMentor.HomeSettlement;
		List<Settlement> list = (from x in Hideout.All
			where Campaign.Current.Models.MapDistanceModel.PathExistBetweenPoints(x.Settlement.GatePosition, centralSettlement.GatePosition, MobileParty.NavigationType.Default)
			select x.Settlement into t
			orderby model.GetDistance(_targetSettlement, t, isFromPort: false, isTargetingPort: false, MobileParty.NavigationType.Default)
			select t).Take(3).ToList();
		_closestHideout = list[0];
		return list;
	}

	private void SpawnRaiderPartyAtHideout(Settlement hideout, bool isSpecialParty = false)
	{
		PartyTemplateObject partyTemplateObject;
		int num;
		TextObject customName;
		if (isSpecialParty)
		{
			partyTemplateObject = (StoryModeManager.Current.MainStoryLine.IsOnImperialQuestLine ? Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("conspiracy_anti_imperial_special_raider_party_template") : Campaign.Current.ObjectManager.GetObject<PartyTemplateObject>("conspiracy_imperial_special_raider_party_template"));
			num = SpecialRaiderPartyTroopCount;
			customName = new TextObject("{=GW7Zg3IP}Conspiracy War Party");
		}
		else
		{
			partyTemplateObject = _banditFaction.DefaultPartyTemplate;
			num = RegularRaiderPartyTroopCount;
			customName = _banditFaction.Name;
		}
		MobileParty mobileParty = BanditPartyComponent.CreateBanditParty(string.Concat("destroy_raiders_conspiracy_quest_", _banditFaction.Name, "_", CampaignTime.Now.ElapsedSecondsUntilNow), _banditFaction, hideout.Hideout, isBossParty: false, partyTemplateObject, hideout.GatePosition);
		mobileParty.Party.SetCustomName(customName);
		mobileParty.MemberRoster.Clear();
		mobileParty.SetPartyUsedByQuest(isActivelyUsed: true);
		SetDefaultRaiderAi(mobileParty);
		if (isSpecialParty)
		{
			_specialRaiderParty = mobileParty;
			mobileParty.MemberRoster.AddToCounts(_conspiracyCaptainCharacter, 1, insertAtFront: true);
			mobileParty.ItemRoster.Clear();
			mobileParty.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("vlandia_horse"), num / 2);
			MBInformationManager.AddQuickInformation(_destroyRaidersSpecialPartySpawnNotification);
		}
		else
		{
			_regularRaiderParties.Add(mobileParty);
		}
		DistributeConspiracyRaiderTroopsByLevel(partyTemplateObject, mobileParty.Party, num);
		AddTrackedObject(mobileParty);
	}

	private void SetDefaultRaiderAi(MobileParty raiderParty)
	{
		SetPartyAiAction.GetActionForPatrollingAroundSettlement(raiderParty, _targetSettlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
		raiderParty.Ai.CheckPartyNeedsUpdate();
		raiderParty.Ai.SetDoNotMakeNewDecisions(doNotMakeNewDecisions: true);
		raiderParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
	}

	private Clan GetBanditTypeForSettlement(Settlement settlement)
	{
		Hideout closestHideout = SettlementHelper.FindNearestHideoutToSettlement(settlement, MobileParty.NavigationType.Default, (Settlement x) => x.IsActive);
		if (closestHideout == null)
		{
			return Clan.BanditFactions.GetRandomElementInefficiently();
		}
		return Clan.BanditFactions.FirstOrDefault((Clan t) => t.Culture == closestHideout.Settlement.Culture);
	}

	private void MobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
	{
		if (destroyerParty != null && destroyerParty.MobileParty == MobileParty.MainParty)
		{
			if (_regularRaiderParties.Contains(mobileParty))
			{
				OnBanditPartyClearedByPlayer(mobileParty);
			}
			else if (_specialRaiderParty == mobileParty)
			{
				OnSpecialBanditPartyClearedByPlayer();
			}
		}
	}

	private void MapEventEnded(MapEvent mapEvent)
	{
		if (mapEvent.WinningSide == BattleSideEnum.None || mapEvent.DefeatedSide == BattleSideEnum.None || !mapEvent.IsPlayerMapEvent || !mapEvent.InvolvedParties.Any((PartyBase t) => t.IsMobile && (_regularRaiderParties.Contains(t.MobileParty) || t.MobileParty == _specialRaiderParty)))
		{
			return;
		}
		if (PlayerEncounter.Battle.WinningSide == PlayerEncounter.Battle.PlayerSide)
		{
			foreach (MapEventParty party in mapEvent.GetMapEventSide(mapEvent.DefeatedSide).Parties)
			{
				MobileParty mobileParty = party.Party.MobileParty;
				if (mobileParty != null && mobileParty.IsActive && mobileParty.Party.NumberOfHealthyMembers > 0 && (_regularRaiderParties.Contains(mobileParty) || _specialRaiderParty == mobileParty))
				{
					DestroyPartyAction.Apply(PartyBase.MainParty, mobileParty);
				}
			}
			return;
		}
		PartyBase captorParty = PlayerCaptivity.CaptorParty;
		if (captorParty == null || !captorParty.IsMobile || (!_regularRaiderParties.Contains(PlayerCaptivity.CaptorParty.MobileParty) && _specialRaiderParty != PlayerCaptivity.CaptorParty.MobileParty))
		{
			OnQuestFailedByDefeat();
		}
	}

	private void OnSpecialBanditPartyClearedByPlayer()
	{
		if (IsTracked(_specialRaiderParty))
		{
			RemoveTrackedObject(_specialRaiderParty);
		}
		_specialPartyProgressTracker.UpdateCurrentProgress(1);
		_specialRaiderParty = null;
		OnQuestSucceeded();
	}

	private void OnBanditPartyClearedByPlayer(MobileParty defeatedParty)
	{
		_regularRaiderParties.Remove(defeatedParty);
		_regularPartiesProgressTracker.UpdateCurrentProgress(3 - _regularRaiderParties.Count);
		if (_regularPartiesProgressTracker.HasBeenCompleted())
		{
			MBInformationManager.AddQuickInformation(_destroyRaidersRegularProgressCompletedNotification);
			AddLog(_destroyRaidersSpecialPartyInformationQuestLog);
			_specialPartyProgressTracker = AddDiscreteLog(_destroyRaidersSpecialPartyProgress, TextObject.GetEmpty(), 0, 1);
			SpawnRaiderPartyAtHideout(_closestHideout, isSpecialParty: true);
		}
		else
		{
			if (IsTracked(defeatedParty))
			{
				RemoveTrackedObject(defeatedParty);
			}
			MBInformationManager.AddQuickInformation(_destroyRaidersRegularProgressNotification);
		}
	}

	private void OnHeroTakenPrisoner(PartyBase capturer, Hero prisoner)
	{
		if (prisoner.Clan != Clan.PlayerClan && capturer.IsMobile && (_regularRaiderParties.Contains(capturer.MobileParty) || _specialRaiderParty == capturer.MobileParty))
		{
			Debug.FailedAssert("Hero has been taken prisoner by conspiracy raider party", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\StoryMode\\Quests\\SecondPhase\\ConspiracyQuests\\DestroyRaidersConspiracyQuest.cs", "OnHeroTakenPrisoner", 533);
			EndCaptivityAction.ApplyByEscape(prisoner);
		}
	}

	protected override void HourlyTick()
	{
		foreach (MobileParty regularRaiderParty in _regularRaiderParties)
		{
			CheckRaiderPartyPlayerEncounter(regularRaiderParty);
		}
	}

	private void CheckRaiderPartyPlayerEncounter(MobileParty raiderParty)
	{
		if (raiderParty.Position.DistanceSquared(MobileParty.MainParty.Position) <= RaiderPartyPlayerEncounterRadius && raiderParty.Ai.DoNotAttackMainPartyUntil.IsPast && raiderParty.Party.CalculateCurrentStrength() > PartyBase.MainParty.CalculateCurrentStrength() * 1.2f && MobileParty.MainParty.CurrentSettlement == null)
		{
			if (!_directedRaidersToEngagePlayer.Contains(raiderParty))
			{
				SetPartyAiAction.GetActionForEngagingParty(raiderParty, MobileParty.MainParty, MobileParty.NavigationType.Default, isFromPort: false);
				raiderParty.Ai.CheckPartyNeedsUpdate();
				_directedRaidersToEngagePlayer.Add(raiderParty);
			}
		}
		else if (_directedRaidersToEngagePlayer.Contains(raiderParty))
		{
			_directedRaidersToEngagePlayer.Remove(raiderParty);
			SetDefaultRaiderAi(raiderParty);
		}
	}

	private DialogFlow GetConspiracyCaptainDialogue()
	{
		return DialogFlow.CreateDialogFlow("start", 125).NpcLine("{=bzmcPtZ6}We know you. We were told to look out for you. We know what you're planning with {MENTOR.NAME}. You will fail, and you will die.[ib:closed][if:convo_predatory]").Condition(delegate
		{
			StringHelpers.SetCharacterProperties("MENTOR", base.QuestGiver.CharacterObject);
			return CharacterObject.OneToOneConversationCharacter == _conspiracyCaptainCharacter && _specialRaiderParty != null && !_specialPartyProgressTracker.HasBeenCompleted();
		})
			.BeginPlayerOptions()
			.PlayerOption("{=BrHU0NuE}Maybe. But if we do, you won't live to see it.")
			.Consequence(delegate
			{
				Campaign.Current.ConversationManager.ConversationEndOneShot += OnConspiracyCaptainDialogueEnd;
			})
			.NpcLine("{=EoLcoaHM}We'll see...")
			.CloseDialog()
			.PlayerOption("{=TLaxmQDF}You'll without a doubt perish by my sword, but today is not the day.")
			.Consequence(delegate
			{
				PlayerEncounter.LeaveEncounter = true;
			})
			.NpcLine("{=9aY0ifwi}We shall meet again...[if:convo_insulted]")
			.CloseDialog()
			.EndPlayerOptions()
			.CloseDialog();
	}

	private void OnConspiracyCaptainDialogueEnd()
	{
		PlayerEncounter.RestartPlayerEncounter(_specialRaiderParty.Party, PartyBase.MainParty);
		PlayerEncounter.StartBattle();
	}

	private void OnQuestSucceeded()
	{
		if (_targetSettlement.OwnerClan != Clan.PlayerClan && !_targetSettlement.OwnerClan.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			ChangeRelationAction.ApplyPlayerRelation(_targetSettlement.OwnerClan.Leader, 5);
		}
		Clan.PlayerClan.AddRenown(5f);
		_targetSettlement.Town.Security += 5f;
		_targetSettlement.Town.Prosperity += 5f;
		AddLog(_destroyRaidersQuestSucceededLogText);
		CompleteQuestWithSuccess();
	}

	private void OnQuestFailedByDefeat()
	{
		OnQuestFailed();
		AddLog(_destroyRaidersQuestFailedOnPlayerDefeatedByRaidersLogText);
		CompleteQuestWithFail();
	}

	private void OnQuestFailed()
	{
		foreach (MobileParty regularRaiderParty in _regularRaiderParties)
		{
			if (regularRaiderParty.IsActive)
			{
				DestroyPartyAction.Apply(null, regularRaiderParty);
			}
		}
		if (_specialRaiderParty != null && _specialRaiderParty.IsActive)
		{
			DestroyPartyAction.Apply(null, _specialRaiderParty);
		}
		if (_targetSettlement.OwnerClan != Clan.PlayerClan)
		{
			ChangeRelationAction.ApplyPlayerRelation(_targetSettlement.OwnerClan.Leader, -5);
		}
	}

	protected override void OnTimedOut()
	{
		OnQuestFailed();
		AddLog(_destroyRaidersQuestFailedOnTimedOutLogText);
	}

	internal static void AutoGeneratedStaticCollectObjectsDestroyRaidersConspiracyQuest(object o, List<object> collectedObjects)
	{
		((DestroyRaidersConspiracyQuest)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
	}

	protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
	{
		base.AutoGeneratedInstanceCollectObjects(collectedObjects);
		collectedObjects.Add(_targetSettlement);
		collectedObjects.Add(_regularRaiderParties);
		collectedObjects.Add(_specialRaiderParty);
		collectedObjects.Add(_regularPartiesProgressTracker);
		collectedObjects.Add(_specialPartyProgressTracker);
		collectedObjects.Add(_banditFaction);
		collectedObjects.Add(_conspiracyCaptainCharacter);
		collectedObjects.Add(_closestHideout);
		collectedObjects.Add(_directedRaidersToEngagePlayer);
	}

	internal static object AutoGeneratedGetMemberValue_targetSettlement(object o)
	{
		return ((DestroyRaidersConspiracyQuest)o)._targetSettlement;
	}

	internal static object AutoGeneratedGetMemberValue_regularRaiderParties(object o)
	{
		return ((DestroyRaidersConspiracyQuest)o)._regularRaiderParties;
	}

	internal static object AutoGeneratedGetMemberValue_specialRaiderParty(object o)
	{
		return ((DestroyRaidersConspiracyQuest)o)._specialRaiderParty;
	}

	internal static object AutoGeneratedGetMemberValue_regularPartiesProgressTracker(object o)
	{
		return ((DestroyRaidersConspiracyQuest)o)._regularPartiesProgressTracker;
	}

	internal static object AutoGeneratedGetMemberValue_specialPartyProgressTracker(object o)
	{
		return ((DestroyRaidersConspiracyQuest)o)._specialPartyProgressTracker;
	}

	internal static object AutoGeneratedGetMemberValue_banditFaction(object o)
	{
		return ((DestroyRaidersConspiracyQuest)o)._banditFaction;
	}

	internal static object AutoGeneratedGetMemberValue_conspiracyCaptainCharacter(object o)
	{
		return ((DestroyRaidersConspiracyQuest)o)._conspiracyCaptainCharacter;
	}

	internal static object AutoGeneratedGetMemberValue_closestHideout(object o)
	{
		return ((DestroyRaidersConspiracyQuest)o)._closestHideout;
	}

	internal static object AutoGeneratedGetMemberValue_directedRaidersToEngagePlayer(object o)
	{
		return ((DestroyRaidersConspiracyQuest)o)._directedRaidersToEngagePlayer;
	}
}
