using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Quests;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Nameplate;

public class SettlementNameplateEventsVM : ViewModel
{
	private List<QuestBase> _relatedQuests;

	private Settlement _settlement;

	private bool _areQuestsDirty;

	private MBBindingList<QuestMarkerVM> _trackQuests;

	private MBBindingList<SettlementNameplateEventItemVM> _eventsList;

	public bool IsEventsRegistered { get; private set; }

	[DataSourceProperty]
	public MBBindingList<QuestMarkerVM> TrackQuests
	{
		get
		{
			return _trackQuests;
		}
		set
		{
			if (value != _trackQuests)
			{
				_trackQuests = value;
				OnPropertyChangedWithValue(value, "TrackQuests");
			}
		}
	}

	public MBBindingList<SettlementNameplateEventItemVM> EventsList
	{
		get
		{
			return _eventsList;
		}
		set
		{
			if (value != _eventsList)
			{
				_eventsList = value;
				OnPropertyChangedWithValue(value, "EventsList");
			}
		}
	}

	public SettlementNameplateEventsVM(Settlement settlement)
	{
		_settlement = settlement;
		EventsList = new MBBindingList<SettlementNameplateEventItemVM>();
		TrackQuests = new MBBindingList<QuestMarkerVM>();
		_relatedQuests = new List<QuestBase>();
		if (settlement.IsVillage)
		{
			AddPrimaryProductionIcon();
		}
	}

	public void Tick()
	{
		if (_areQuestsDirty)
		{
			RefreshQuestCounts();
			_areQuestsDirty = false;
		}
	}

	private void PopulateEventList()
	{
		if (Campaign.Current.TournamentManager.GetTournamentGame(_settlement.Town) != null)
		{
			EventsList.Add(new SettlementNameplateEventItemVM(SettlementNameplateEventItemVM.SettlementEventType.Tournament));
		}
	}

	public void RegisterEvents()
	{
		if (!IsEventsRegistered)
		{
			PopulateEventList();
			CampaignEvents.TournamentStarted.AddNonSerializedListener(this, OnTournamentStarted);
			CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
			CampaignEvents.TournamentCancelled.AddNonSerializedListener(this, OnTournamentCancelled);
			CampaignEvents.OnNewIssueCreatedEvent.AddNonSerializedListener(this, OnNewIssueCreated);
			CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueUpdated);
			CampaignEvents.OnQuestStartedEvent.AddNonSerializedListener(this, OnQuestStarted);
			CampaignEvents.QuestLogAddedEvent.AddNonSerializedListener(this, OnQuestLogAdded);
			CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroTakenPrisoner);
			IsEventsRegistered = true;
			RefreshQuestCounts();
		}
	}

	public void UnloadEvents()
	{
		if (!IsEventsRegistered)
		{
			return;
		}
		CampaignEvents.TournamentStarted.ClearListeners(this);
		CampaignEvents.TournamentFinished.ClearListeners(this);
		CampaignEvents.TournamentCancelled.ClearListeners(this);
		CampaignEvents.OnNewIssueCreatedEvent.ClearListeners(this);
		CampaignEvents.OnIssueUpdatedEvent.ClearListeners(this);
		CampaignEvents.OnQuestStartedEvent.ClearListeners(this);
		CampaignEvents.QuestLogAddedEvent.ClearListeners(this);
		CampaignEvents.OnQuestCompletedEvent.ClearListeners(this);
		CampaignEvents.SettlementEntered.ClearListeners(this);
		CampaignEvents.OnSettlementLeftEvent.ClearListeners(this);
		CampaignEvents.HeroPrisonerTaken.ClearListeners(this);
		int num = EventsList.Count;
		for (int i = 0; i < num; i++)
		{
			if (EventsList[i].EventType != SettlementNameplateEventItemVM.SettlementEventType.Production)
			{
				EventsList.RemoveAt(i);
				num--;
				i--;
			}
		}
		IsEventsRegistered = false;
	}

	private void OnTournamentStarted(Town town)
	{
		if (_settlement.Town == null || town != _settlement.Town)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < EventsList.Count; i++)
		{
			if (EventsList[i].EventType == SettlementNameplateEventItemVM.SettlementEventType.Tournament)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			EventsList.Add(new SettlementNameplateEventItemVM(SettlementNameplateEventItemVM.SettlementEventType.Tournament));
		}
	}

	private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
		RemoveTournament(town);
	}

	private void OnTournamentCancelled(Town town)
	{
		RemoveTournament(town);
	}

	private void RemoveTournament(Town town)
	{
		if (_settlement.Town == null || town != _settlement.Town || EventsList.Count((SettlementNameplateEventItemVM e) => e.EventType == SettlementNameplateEventItemVM.SettlementEventType.Tournament) <= 0)
		{
			return;
		}
		int num = -1;
		for (int num2 = 0; num2 < EventsList.Count; num2++)
		{
			if (EventsList[num2].EventType == SettlementNameplateEventItemVM.SettlementEventType.Tournament)
			{
				num = num2;
				break;
			}
		}
		if (num != -1)
		{
			EventsList.RemoveAt(num);
		}
		else
		{
			Debug.FailedAssert("There should be a tournament item to remove", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Nameplate\\SettlementNameplateEventsVM.cs", "RemoveTournament", 164);
		}
	}

	private void RefreshQuestCounts()
	{
		_relatedQuests.Clear();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = Campaign.Current.IssueManager.GetNumOfActiveIssuesInSettlement(_settlement, includeQuests: false);
		int numOfAvailableIssuesInSettlement = Campaign.Current.IssueManager.GetNumOfAvailableIssuesInSettlement(_settlement);
		TrackQuests.Clear();
		if (Campaign.Current.QuestManager.TrackedObjects.TryGetValue(_settlement, out var value))
		{
			foreach (QuestBase item in value)
			{
				if (item.IsSpecialQuest && !TrackQuests.Any((QuestMarkerVM x) => x.IssueQuestFlag == CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest))
				{
					TrackQuests.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest));
					_relatedQuests.Add(item);
				}
				else if (!TrackQuests.Any((QuestMarkerVM x) => x.IssueQuestFlag == CampaignUIHelper.IssueQuestFlags.TrackedIssue))
				{
					TrackQuests.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.TrackedIssue));
					_relatedQuests.Add(item);
				}
			}
		}
		List<(bool, QuestBase)> questsRelatedToSettlement = CampaignUIHelper.GetQuestsRelatedToSettlement(_settlement);
		for (int num5 = 0; num5 < questsRelatedToSettlement.Count; num5++)
		{
			if (questsRelatedToSettlement[num5].Item1)
			{
				if (questsRelatedToSettlement[num5].Item2.IsSpecialQuest)
				{
					num++;
				}
				else
				{
					num4++;
				}
			}
			else if (questsRelatedToSettlement[num5].Item2.IsSpecialQuest)
			{
				num3++;
			}
			else
			{
				num2++;
			}
			_relatedQuests.Add(questsRelatedToSettlement[num5].Item2);
		}
		HandleIssueCount(numOfAvailableIssuesInSettlement, SettlementNameplateEventItemVM.SettlementEventType.AvailableIssue);
		HandleIssueCount(num4, SettlementNameplateEventItemVM.SettlementEventType.ActiveQuest);
		HandleIssueCount(num, SettlementNameplateEventItemVM.SettlementEventType.ActiveStoryQuest);
		HandleIssueCount(num2, SettlementNameplateEventItemVM.SettlementEventType.TrackedIssue);
		HandleIssueCount(num3, SettlementNameplateEventItemVM.SettlementEventType.TrackedStoryQuest);
	}

	private void OnNewIssueCreated(IssueBase issue)
	{
		if (issue.IssueSettlement == _settlement || issue.IssueOwner?.CurrentSettlement == _settlement)
		{
			_areQuestsDirty = true;
		}
	}

	private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero hero)
	{
		if (issue.IssueSettlement == _settlement && issue.IssueQuest == null)
		{
			_areQuestsDirty = true;
		}
	}

	private void OnQuestStarted(QuestBase quest)
	{
		if (IsQuestRelated(quest))
		{
			_areQuestsDirty = true;
		}
	}

	private void OnQuestLogAdded(QuestBase quest, bool hideInformation)
	{
		if (IsQuestRelated(quest))
		{
			_areQuestsDirty = true;
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
	{
		if (IsQuestRelated(quest))
		{
			_areQuestsDirty = true;
		}
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (settlement == _settlement)
		{
			_areQuestsDirty = true;
		}
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (settlement == _settlement)
		{
			_areQuestsDirty = true;
		}
	}

	private void OnHeroTakenPrisoner(PartyBase capturer, Hero prisoner)
	{
		if (prisoner.CurrentSettlement == _settlement)
		{
			_areQuestsDirty = true;
		}
	}

	private void AddPrimaryProductionIcon()
	{
		string stringId = _settlement.Village.VillageType.PrimaryProduction.StringId;
		string productionIconId = (stringId.Contains("camel") ? "camel" : ((stringId.Contains("horse") || stringId.Contains("mule")) ? "horse" : stringId));
		EventsList.Add(new SettlementNameplateEventItemVM(productionIconId));
	}

	private void HandleIssueCount(int count, SettlementNameplateEventItemVM.SettlementEventType eventType)
	{
		SettlementNameplateEventItemVM settlementNameplateEventItemVM = EventsList.FirstOrDefault((SettlementNameplateEventItemVM e) => e.EventType == eventType);
		if (count > 0 && settlementNameplateEventItemVM == null)
		{
			EventsList.Add(new SettlementNameplateEventItemVM(eventType));
		}
		else if (count == 0 && settlementNameplateEventItemVM != null)
		{
			EventsList.Remove(settlementNameplateEventItemVM);
		}
	}

	private bool IsQuestRelated(QuestBase quest)
	{
		IssueBase issueOfQuest = IssueManager.GetIssueOfQuest(quest);
		if ((issueOfQuest == null || issueOfQuest.IssueSettlement != _settlement) && !_relatedQuests.Contains(quest))
		{
			return CampaignUIHelper.IsQuestRelatedToSettlement(quest, _settlement);
		}
		return true;
	}
}
