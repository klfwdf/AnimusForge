using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace NavalDLC.CampaignBehaviors;

public class NavalDLCTutorialBoxCampaignBehavior : CampaignBehaviorBase
{
	private List<string> _shownTutorials = new List<string>();

	private readonly MBList<CampaignTutorial> _availableTutorials = new MBList<CampaignTutorial>();

	private Dictionary<string, int> _tutorialBackup = new Dictionary<string, int>();

	private List<CampaignTutorial> _tutorialsToResetAfterMission = new List<CampaignTutorial>();

	public MBReadOnlyList<CampaignTutorial> AvailableTutorials => _availableTutorials;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnTutorialCompletedEvent.AddNonSerializedListener(this, OnTutorialCompleted);
		CampaignEvents.CollectAvailableTutorialsEvent.AddNonSerializedListener(this, OnTutorialListRequested);
		CampaignEvents.OnQuestStartedEvent.AddNonSerializedListener(this, OnQuestStarted);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		Game.Current.EventManager.RegisterEvent<ResetAllTutorialsEvent>(OnResetAllTutorials);
		Input.OnGamepadActiveStateChanged = (Action)Delegate.Combine(Input.OnGamepadActiveStateChanged, new Action(UpdateKeyTexts));
		HotKeyManager.OnKeybindsChanged += UpdateKeyTexts;
		UpdateKeyTexts();
	}

	private void OnMissionEnded(IMission obj)
	{
		if (_tutorialsToResetAfterMission.Count <= 0)
		{
			return;
		}
		foreach (CampaignTutorial item in _tutorialsToResetAfterMission)
		{
			_availableTutorials.Add(item);
			_shownTutorials.Remove(item.TutorialTypeId);
			if (!_tutorialBackup.ContainsKey(item.TutorialTypeId))
			{
				_tutorialBackup.Add(item.TutorialTypeId, item.Priority);
			}
		}
		_availableTutorials.Sort(delegate(CampaignTutorial x, CampaignTutorial y)
		{
			int priority = x.Priority;
			return priority.CompareTo(y.Priority);
		});
		_tutorialsToResetAfterMission.Clear();
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_shownTutorials", ref _shownTutorials);
		dataStore.SyncData("_tutorialBackup", ref _tutorialBackup);
	}

	private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
	{
		AddTutorial("ShipControlTutorial", 1);
		AddTutorial("ShipOarsmanTutorial", 2);
		AddTutorial("ShipCameraTutorial", 3);
		AddTutorial("ShipSailTutorial", 4);
		AddTutorial("ShipCloseSailTutorial", 5);
		AddTutorial("ShipBoardingApproachTutorial", 6);
		AddTutorial("ShipBoardingAttemptBoardingTutorial", 7);
		AddTutorial("ShipBoardingTroopChargeTutorial", 8);
		AddTutorial("ShipCutLooseTutorial", 9);
		AddTutorial("ShipCommandingShipsTutorial", 10);
		_availableTutorials.Sort(delegate(CampaignTutorial x, CampaignTutorial y)
		{
			int priority = x.Priority;
			return priority.CompareTo(y.Priority);
		});
	}

	private void OnQuestStarted(QuestBase quest)
	{
		_availableTutorials.Sort(delegate(CampaignTutorial x, CampaignTutorial y)
		{
			int priority = x.Priority;
			return priority.CompareTo(y.Priority);
		});
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
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
			if (campaignTutorial.TutorialTypeId == "ShipControlTutorial" || campaignTutorial.TutorialTypeId == "ShipSailTutorial" || campaignTutorial.TutorialTypeId == "ShipOarsmanTutorial" || campaignTutorial.TutorialTypeId == "ShipBoardingApproachTutorial" || campaignTutorial.TutorialTypeId == "ShipBoardingAttemptBoardingTutorial" || campaignTutorial.TutorialTypeId == "ShipBoardingTroopChargeTutorial" || campaignTutorial.TutorialTypeId == "ShipCutLooseTutorial" || campaignTutorial.TutorialTypeId == "ShipCommandingShipsTutorial" || campaignTutorial.TutorialTypeId == "ShipCameraTutorial" || campaignTutorial.TutorialTypeId == "ShipCloseSailTutorial")
			{
				_tutorialsToResetAfterMission.Add(campaignTutorial);
			}
			_availableTutorials.Remove(campaignTutorial);
			_shownTutorials.Add(completedTutorialType);
			_tutorialBackup.Remove(completedTutorialType);
		}
	}

	private void OnTutorialListRequested(List<CampaignTutorial> campaignTutorials)
	{
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

	private static void UpdateKeyTexts()
	{
		string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("NavalShipControlsHotKeyCategory", 110));
		GameTexts.SetVariable("TOGGLE_SAIL_KEY", keyHyperlinkText);
		string keyHyperlinkText2 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("NavalShipControlsHotKeyCategory", 111));
		GameTexts.SetVariable("TOGGLE_OARSMEN_KEY", keyHyperlinkText2);
		string keyHyperlinkText3 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("NavalShipControlsHotKeyCategory", 112));
		GameTexts.SetVariable("TOGGLE_CAMERA_KEY", keyHyperlinkText3);
		string keyHyperlinkText4 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("NavalShipControlsHotKeyCategory", 111));
		GameTexts.SetVariable("CUT_LOOSE_KEY", keyHyperlinkText4);
		string keyHyperlinkText5 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("NavalShipControlsHotKeyCategory", 114));
		GameTexts.SetVariable("ATTEMPT_BOARDING_KEY", keyHyperlinkText5);
	}
}
