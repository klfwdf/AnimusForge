using System.Collections.Generic;
using StoryMode.Quests.TutorialPhase;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace StoryMode.GameComponents.CampaignBehaviors;

public class StoryModeTutorialBoxCampaignBehavior : CampaignBehaviorBase
{
	private List<string> _shownTutorials;

	private readonly MBList<CampaignTutorial> _availableTutorials;

	private Dictionary<string, int> _tutorialBackup;

	public MBReadOnlyList<CampaignTutorial> AvailableTutorials => _availableTutorials;

	public StoryModeTutorialBoxCampaignBehavior()
	{
		_shownTutorials = new List<string>();
		_availableTutorials = new MBList<CampaignTutorial>();
		_tutorialBackup = new Dictionary<string, int>();
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnTutorialCompletedEvent.AddNonSerializedListener(this, OnTutorialCompleted);
		CampaignEvents.CollectAvailableTutorialsEvent.AddNonSerializedListener(this, OnTutorialListRequested);
		CampaignEvents.OnQuestStartedEvent.AddNonSerializedListener(this, OnQuestStarted);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		StoryModeEvents.OnTravelToVillageTutorialQuestStartedEvent.AddNonSerializedListener(this, OnTravelToVillageTutorialQuestStarted);
		Game.Current.EventManager.RegisterEvent<ResetAllTutorialsEvent>(OnResetAllTutorials);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_shownTutorials", ref _shownTutorials);
		dataStore.SyncData("_tutorialBackup", ref _tutorialBackup);
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		BackupTutorial("MovementInMissionTutorial", 5);
		int num = 100;
		BackupTutorial("EncyclopediaHomeTutorial", num++);
		BackupTutorial("EncyclopediaSettlementsTutorial", num++);
		BackupTutorial("EncyclopediaTroopsTutorial", num++);
		BackupTutorial("EncyclopediaKingdomsTutorial", num++);
		BackupTutorial("EncyclopediaClansTutorial", num++);
		BackupTutorial("EncyclopediaConceptsTutorial", num++);
		BackupTutorial("EncyclopediaTrackTutorial", num++);
		BackupTutorial("EncyclopediaSearchTutorial", num++);
		BackupTutorial("EncyclopediaFiltersTutorial", num++);
		BackupTutorial("EncyclopediaSortTutorial", num++);
		BackupTutorial("EncyclopediaFogOfWarTutorial", num++);
		BackupTutorial("RaidVillageStep1", num++);
		BackupTutorial("UpgradingTroopsStep1", num++);
		BackupTutorial("UpgradingTroopsStep2", num++);
		BackupTutorial("UpgradingTroopsStep3", num++);
		BackupTutorial("ChoosingPerkUpgradesStep1", num++);
		BackupTutorial("ChoosingPerkUpgradesStep2", num++);
		BackupTutorial("ChoosingPerkUpgradesStep3", num++);
		BackupTutorial("ChoosingSkillFocusStep1", num++);
		BackupTutorial("ChoosingSkillFocusStep2", num++);
		BackupTutorial("GettingCompanionsStep1", num++);
		BackupTutorial("GettingCompanionsStep2", num++);
		BackupTutorial("GettingCompanionsStep3", num++);
		BackupTutorial("RansomingPrisonersStep1", num++);
		BackupTutorial("RansomingPrisonersStep2", num++);
		BackupTutorial("EquipmentSets", num++);
		BackupTutorial("PartySpeed", num++);
		BackupTutorial("ArmyCohesionStep1", num++);
		BackupTutorial("ArmyCohesionStep2", num++);
		BackupTutorial("CreateArmyStep2", num++);
		BackupTutorial("CreateArmyStep3", num++);
		BackupTutorial("OrderOfBattleTutorialStep1", num++);
		BackupTutorial("OrderOfBattleTutorialStep2", num++);
		BackupTutorial("OrderOfBattleTutorialStep3", num++);
		BackupTutorial("CraftingStep1Tutorial", num++);
		BackupTutorial("CraftingOrdersTutorial", num++);
		BackupTutorial("InventoryBannerItemTutorial", num++);
		BackupTutorial("CrimeTutorial", num++);
		BackupTutorial("AssignRolesTutorial", num++);
		BackupTutorial("BombardmentStep1", num++);
		BackupTutorial("KingdomDecisionVotingTutorial", num++);
		foreach (KeyValuePair<string, int> item in _tutorialBackup)
		{
			AddTutorial(item.Key, item.Value);
		}
	}

	private void OnTravelToVillageTutorialQuestStarted()
	{
		AddTutorial("SeeMarkersInMissionTutorial", 1);
		AddTutorial("NavigateOnMapTutorialStep1", 2);
		AddTutorial("NavigateOnMapTutorialStep2", 3);
		AddTutorial("EnterVillageTutorial", 4);
	}

	private void OnQuestStarted(QuestBase quest)
	{
		if (quest is PurchaseGrainTutorialQuest)
		{
			AddTutorial("PressLeaveToReturnFromMissionType1", 10);
			AddTutorial("GetSuppliesTutorialStep1", 20);
			AddTutorial("GetSuppliesTutorialStep3", 22);
		}
		else if (quest is RecruitTroopsTutorialQuest)
		{
			AddTutorial("RecruitmentTutorialStep1", 11);
			AddTutorial("RecruitmentTutorialStep2", 12);
		}
		else if (quest is LocateAndRescueTravellerTutorialQuest)
		{
			AddTutorial("PressLeaveToReturnFromMissionType2", 30);
			AddTutorial("OrderTutorial1TutorialStep2", 33);
			AddTutorial("TakeAndRescuePrisonerTutorial", 34);
			AddTutorial("OrderTutorial2Tutorial", 35);
		}
		else if (quest is VillagersInNeed)
		{
			AddTutorial("StealthCrouchTutorial", 36);
			AddTutorial("StealthWalkSlowTutorial", 37);
			AddTutorial("StealthHideInBushesTutorial", 38);
			AddTutorial("StealthDistractionTutorial", 39);
			AddTutorial("StealthDarkZoneTutorial", 40);
			AddTutorial("StealthStealthKillTutorial", 41);
			AddTutorial("StealthHideCorpseTutorial", 42);
		}
		_availableTutorials.Sort(delegate(CampaignTutorial x, CampaignTutorial y)
		{
			int priority = x.Priority;
			return priority.CompareTo(y.Priority);
		});
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		if (TutorialPhase.Instance.TutorialQuestPhase == TutorialQuestPhase.RecruitAndPurchaseStarted && ((quest is RecruitTroopsTutorialQuest && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(PurchaseGrainTutorialQuest))) || (quest is PurchaseGrainTutorialQuest && !Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(RecruitTroopsTutorialQuest)))))
		{
			AddTutorial("TalkToNotableTutorialStep1", 40);
			AddTutorial("TalkToNotableTutorialStep2", 41);
		}
		_availableTutorials.Sort(delegate(CampaignTutorial x, CampaignTutorial y)
		{
			int priority = x.Priority;
			return priority.CompareTo(y.Priority);
		});
	}

	private void OnTutorialCompleted(string completedTutorialType)
	{
		CampaignTutorial campaignTutorial = _availableTutorials.Find((CampaignTutorial t) => t.TutorialTypeId == completedTutorialType);
		if (campaignTutorial != null)
		{
			_availableTutorials.Remove(campaignTutorial);
			_shownTutorials.Add(completedTutorialType);
			_tutorialBackup.Remove(completedTutorialType);
		}
	}

	private void OnTutorialListRequested(List<CampaignTutorial> campaignTutorials)
	{
		if (!BannerlordConfig.EnableTutorialHints)
		{
			return;
		}
		MBTextManager.SetTextVariable("TUTORIAL_SETTLEMENT_NAME", MBObjectManager.Instance.GetObject<Settlement>("village_ES3_2").Name);
		foreach (CampaignTutorial availableTutorial in AvailableTutorials)
		{
			campaignTutorials.Add(availableTutorial);
		}
	}

	private void BackupTutorial(string tutorialTypeId, int priority)
	{
		if (!_shownTutorials.Contains(tutorialTypeId) && !_tutorialBackup.ContainsKey(tutorialTypeId))
		{
			_tutorialBackup.Add(tutorialTypeId, priority);
		}
	}

	private void AddTutorial(string tutorialTypeId, int priority)
	{
		if (!_shownTutorials.Contains(tutorialTypeId))
		{
			CampaignTutorial item = new CampaignTutorial(tutorialTypeId, priority);
			_availableTutorials.Add(item);
			if (!_tutorialBackup.ContainsKey(tutorialTypeId))
			{
				_tutorialBackup.Add(tutorialTypeId, priority);
			}
		}
	}

	public void OnResetAllTutorials(ResetAllTutorialsEvent obj)
	{
		_shownTutorials.Clear();
	}
}
