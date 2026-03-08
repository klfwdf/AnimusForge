using System;
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

namespace SandBox.ViewModelCollection.Nameplate
{
	// Token: 0x0200001C RID: 28
	public class SettlementNameplateEventsVM : ViewModel
	{
		// Token: 0x170000E3 RID: 227
		// (get) Token: 0x060002AB RID: 683 RVA: 0x0000B958 File Offset: 0x00009B58
		// (set) Token: 0x060002AC RID: 684 RVA: 0x0000B960 File Offset: 0x00009B60
		public bool IsEventsRegistered { get; private set; }

		// Token: 0x060002AD RID: 685 RVA: 0x0000B969 File Offset: 0x00009B69
		public SettlementNameplateEventsVM(Settlement settlement)
		{
			this._settlement = settlement;
			this.EventsList = new MBBindingList<SettlementNameplateEventItemVM>();
			this.TrackQuests = new MBBindingList<QuestMarkerVM>();
			this._relatedQuests = new List<QuestBase>();
			if (settlement.IsVillage)
			{
				this.AddPrimaryProductionIcon();
			}
		}

		// Token: 0x060002AE RID: 686 RVA: 0x0000B9A7 File Offset: 0x00009BA7
		public void Tick()
		{
			if (this._areQuestsDirty)
			{
				this.RefreshQuestCounts();
				this._areQuestsDirty = false;
			}
		}

		// Token: 0x060002AF RID: 687 RVA: 0x0000B9BE File Offset: 0x00009BBE
		private void PopulateEventList()
		{
			if (Campaign.Current.TournamentManager.GetTournamentGame(this._settlement.Town) != null)
			{
				this.EventsList.Add(new SettlementNameplateEventItemVM(SettlementNameplateEventItemVM.SettlementEventType.Tournament));
			}
		}

		// Token: 0x060002B0 RID: 688 RVA: 0x0000B9F0 File Offset: 0x00009BF0
		public void RegisterEvents()
		{
			if (!this.IsEventsRegistered)
			{
				this.PopulateEventList();
				CampaignEvents.TournamentStarted.AddNonSerializedListener(this, new Action<Town>(this.OnTournamentStarted));
				CampaignEvents.TournamentFinished.AddNonSerializedListener(this, new Action<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject>(this.OnTournamentFinished));
				CampaignEvents.TournamentCancelled.AddNonSerializedListener(this, new Action<Town>(this.OnTournamentCancelled));
				CampaignEvents.OnNewIssueCreatedEvent.AddNonSerializedListener(this, new Action<IssueBase>(this.OnNewIssueCreated));
				CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, new Action<IssueBase, IssueBase.IssueUpdateDetails, Hero>(this.OnIssueUpdated));
				CampaignEvents.OnQuestStartedEvent.AddNonSerializedListener(this, new Action<QuestBase>(this.OnQuestStarted));
				CampaignEvents.QuestLogAddedEvent.AddNonSerializedListener(this, new Action<QuestBase, bool>(this.OnQuestLogAdded));
				CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, new Action<QuestBase, QuestBase.QuestCompleteDetails>(this.OnQuestCompleted));
				CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
				CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeft));
				CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(this.OnHeroTakenPrisoner));
				this.IsEventsRegistered = true;
				this.RefreshQuestCounts();
			}
		}

		// Token: 0x060002B1 RID: 689 RVA: 0x0000BB18 File Offset: 0x00009D18
		public void UnloadEvents()
		{
			if (this.IsEventsRegistered)
			{
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
				int num = this.EventsList.Count;
				for (int i = 0; i < num; i++)
				{
					if (this.EventsList[i].EventType != SettlementNameplateEventItemVM.SettlementEventType.Production)
					{
						this.EventsList.RemoveAt(i);
						num--;
						i--;
					}
				}
				this.IsEventsRegistered = false;
			}
		}

		// Token: 0x060002B2 RID: 690 RVA: 0x0000BBF0 File Offset: 0x00009DF0
		private void OnTournamentStarted(Town town)
		{
			if (this._settlement.Town != null && town == this._settlement.Town)
			{
				bool flag = false;
				for (int i = 0; i < this.EventsList.Count; i++)
				{
					if (this.EventsList[i].EventType == SettlementNameplateEventItemVM.SettlementEventType.Tournament)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					this.EventsList.Add(new SettlementNameplateEventItemVM(SettlementNameplateEventItemVM.SettlementEventType.Tournament));
				}
			}
		}

		// Token: 0x060002B3 RID: 691 RVA: 0x0000BC5B File Offset: 0x00009E5B
		private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
		{
			this.RemoveTournament(town);
		}

		// Token: 0x060002B4 RID: 692 RVA: 0x0000BC64 File Offset: 0x00009E64
		private void OnTournamentCancelled(Town town)
		{
			this.RemoveTournament(town);
		}

		// Token: 0x060002B5 RID: 693 RVA: 0x0000BC70 File Offset: 0x00009E70
		private void RemoveTournament(Town town)
		{
			if (this._settlement.Town != null && town == this._settlement.Town)
			{
				if (this.EventsList.Count((SettlementNameplateEventItemVM e) => e.EventType == SettlementNameplateEventItemVM.SettlementEventType.Tournament) > 0)
				{
					int num = -1;
					for (int i = 0; i < this.EventsList.Count; i++)
					{
						if (this.EventsList[i].EventType == SettlementNameplateEventItemVM.SettlementEventType.Tournament)
						{
							num = i;
							break;
						}
					}
					if (num != -1)
					{
						this.EventsList.RemoveAt(num);
						return;
					}
					Debug.FailedAssert("There should be a tournament item to remove", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.ViewModelCollection\\Nameplate\\SettlementNameplateEventsVM.cs", "RemoveTournament", 164);
				}
			}
		}

		// Token: 0x060002B6 RID: 694 RVA: 0x0000BD24 File Offset: 0x00009F24
		private void RefreshQuestCounts()
		{
			this._relatedQuests.Clear();
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = Campaign.Current.IssueManager.GetNumOfActiveIssuesInSettlement(this._settlement, false);
			int numOfAvailableIssuesInSettlement = Campaign.Current.IssueManager.GetNumOfAvailableIssuesInSettlement(this._settlement);
			this.TrackQuests.Clear();
			List<QuestBase> list;
			if (Campaign.Current.QuestManager.TrackedObjects.TryGetValue(this._settlement, out list))
			{
				foreach (QuestBase questBase in list)
				{
					if (questBase.IsSpecialQuest)
					{
						if (!this.TrackQuests.Any((QuestMarkerVM x) => x.IssueQuestFlag == CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest))
						{
							this.TrackQuests.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.TrackedStoryQuest, null, null));
							this._relatedQuests.Add(questBase);
							continue;
						}
					}
					if (!this.TrackQuests.Any((QuestMarkerVM x) => x.IssueQuestFlag == CampaignUIHelper.IssueQuestFlags.TrackedIssue))
					{
						this.TrackQuests.Add(new QuestMarkerVM(CampaignUIHelper.IssueQuestFlags.TrackedIssue, null, null));
						this._relatedQuests.Add(questBase);
					}
				}
			}
			List<ValueTuple<bool, QuestBase>> questsRelatedToSettlement = CampaignUIHelper.GetQuestsRelatedToSettlement(this._settlement);
			for (int i = 0; i < questsRelatedToSettlement.Count; i++)
			{
				if (questsRelatedToSettlement[i].Item1)
				{
					if (questsRelatedToSettlement[i].Item2.IsSpecialQuest)
					{
						num++;
					}
					else
					{
						num4++;
					}
				}
				else if (questsRelatedToSettlement[i].Item2.IsSpecialQuest)
				{
					num3++;
				}
				else
				{
					num2++;
				}
				this._relatedQuests.Add(questsRelatedToSettlement[i].Item2);
			}
			this.HandleIssueCount(numOfAvailableIssuesInSettlement, SettlementNameplateEventItemVM.SettlementEventType.AvailableIssue);
			this.HandleIssueCount(num4, SettlementNameplateEventItemVM.SettlementEventType.ActiveQuest);
			this.HandleIssueCount(num, SettlementNameplateEventItemVM.SettlementEventType.ActiveStoryQuest);
			this.HandleIssueCount(num2, SettlementNameplateEventItemVM.SettlementEventType.TrackedIssue);
			this.HandleIssueCount(num3, SettlementNameplateEventItemVM.SettlementEventType.TrackedStoryQuest);
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x0000BF38 File Offset: 0x0000A138
		private void OnNewIssueCreated(IssueBase issue)
		{
			if (issue.IssueSettlement != this._settlement)
			{
				Hero issueOwner = issue.IssueOwner;
				if (((issueOwner != null) ? issueOwner.CurrentSettlement : null) != this._settlement)
				{
					return;
				}
			}
			this._areQuestsDirty = true;
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x0000BF69 File Offset: 0x0000A169
		private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero hero)
		{
			if (issue.IssueSettlement == this._settlement && issue.IssueQuest == null)
			{
				this._areQuestsDirty = true;
			}
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x0000BF88 File Offset: 0x0000A188
		private void OnQuestStarted(QuestBase quest)
		{
			if (this.IsQuestRelated(quest))
			{
				this._areQuestsDirty = true;
			}
		}

		// Token: 0x060002BA RID: 698 RVA: 0x0000BF9A File Offset: 0x0000A19A
		private void OnQuestLogAdded(QuestBase quest, bool hideInformation)
		{
			if (this.IsQuestRelated(quest))
			{
				this._areQuestsDirty = true;
			}
		}

		// Token: 0x060002BB RID: 699 RVA: 0x0000BFAC File Offset: 0x0000A1AC
		private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
		{
			if (this.IsQuestRelated(quest))
			{
				this._areQuestsDirty = true;
			}
		}

		// Token: 0x060002BC RID: 700 RVA: 0x0000BFBE File Offset: 0x0000A1BE
		private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
		{
			if (settlement == this._settlement)
			{
				this._areQuestsDirty = true;
			}
		}

		// Token: 0x060002BD RID: 701 RVA: 0x0000BFD0 File Offset: 0x0000A1D0
		private void OnSettlementLeft(MobileParty party, Settlement settlement)
		{
			if (settlement == this._settlement)
			{
				this._areQuestsDirty = true;
			}
		}

		// Token: 0x060002BE RID: 702 RVA: 0x0000BFE2 File Offset: 0x0000A1E2
		private void OnHeroTakenPrisoner(PartyBase capturer, Hero prisoner)
		{
			if (prisoner.CurrentSettlement == this._settlement)
			{
				this._areQuestsDirty = true;
			}
		}

		// Token: 0x060002BF RID: 703 RVA: 0x0000BFFC File Offset: 0x0000A1FC
		private void AddPrimaryProductionIcon()
		{
			string stringId = this._settlement.Village.VillageType.PrimaryProduction.StringId;
			string productionIconId = (stringId.Contains("camel") ? "camel" : ((stringId.Contains("horse") || stringId.Contains("mule")) ? "horse" : stringId));
			this.EventsList.Add(new SettlementNameplateEventItemVM(productionIconId));
		}

		// Token: 0x060002C0 RID: 704 RVA: 0x0000C06C File Offset: 0x0000A26C
		private void HandleIssueCount(int count, SettlementNameplateEventItemVM.SettlementEventType eventType)
		{
			SettlementNameplateEventItemVM settlementNameplateEventItemVM = this.EventsList.FirstOrDefault((SettlementNameplateEventItemVM e) => e.EventType == eventType);
			if (count > 0 && settlementNameplateEventItemVM == null)
			{
				this.EventsList.Add(new SettlementNameplateEventItemVM(eventType));
				return;
			}
			if (count == 0 && settlementNameplateEventItemVM != null)
			{
				this.EventsList.Remove(settlementNameplateEventItemVM);
			}
		}

		// Token: 0x060002C1 RID: 705 RVA: 0x0000C0D0 File Offset: 0x0000A2D0
		private bool IsQuestRelated(QuestBase quest)
		{
			IssueBase issueOfQuest = IssueManager.GetIssueOfQuest(quest);
			return (issueOfQuest != null && issueOfQuest.IssueSettlement == this._settlement) || this._relatedQuests.Contains(quest) || CampaignUIHelper.IsQuestRelatedToSettlement(quest, this._settlement);
		}

		// Token: 0x170000E4 RID: 228
		// (get) Token: 0x060002C2 RID: 706 RVA: 0x0000C111 File Offset: 0x0000A311
		// (set) Token: 0x060002C3 RID: 707 RVA: 0x0000C119 File Offset: 0x0000A319
		[DataSourceProperty]
		public MBBindingList<QuestMarkerVM> TrackQuests
		{
			get
			{
				return this._trackQuests;
			}
			set
			{
				if (value != this._trackQuests)
				{
					this._trackQuests = value;
					base.OnPropertyChangedWithValue<MBBindingList<QuestMarkerVM>>(value, "TrackQuests");
				}
			}
		}

		// Token: 0x170000E5 RID: 229
		// (get) Token: 0x060002C4 RID: 708 RVA: 0x0000C137 File Offset: 0x0000A337
		// (set) Token: 0x060002C5 RID: 709 RVA: 0x0000C13F File Offset: 0x0000A33F
		public MBBindingList<SettlementNameplateEventItemVM> EventsList
		{
			get
			{
				return this._eventsList;
			}
			set
			{
				if (value != this._eventsList)
				{
					this._eventsList = value;
					base.OnPropertyChangedWithValue<MBBindingList<SettlementNameplateEventItemVM>>(value, "EventsList");
				}
			}
		}

		// Token: 0x04000155 RID: 341
		private List<QuestBase> _relatedQuests;

		// Token: 0x04000156 RID: 342
		private Settlement _settlement;

		// Token: 0x04000157 RID: 343
		private bool _areQuestsDirty;

		// Token: 0x04000158 RID: 344
		private MBBindingList<QuestMarkerVM> _trackQuests;

		// Token: 0x04000159 RID: 345
		private MBBindingList<SettlementNameplateEventItemVM> _eventsList;
	}
}
