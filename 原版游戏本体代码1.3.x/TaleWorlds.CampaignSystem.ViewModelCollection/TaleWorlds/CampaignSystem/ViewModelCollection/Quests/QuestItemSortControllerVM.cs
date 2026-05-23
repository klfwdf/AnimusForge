using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Quests
{
	// Token: 0x02000021 RID: 33
	public class QuestItemSortControllerVM : ViewModel
	{
		// Token: 0x17000056 RID: 86
		// (get) Token: 0x060001EF RID: 495 RVA: 0x0001269E File Offset: 0x0001089E
		// (set) Token: 0x060001F0 RID: 496 RVA: 0x000126A6 File Offset: 0x000108A6
		public QuestItemSortControllerVM.QuestItemSortOption? CurrentSortOption { get; private set; }

		// Token: 0x060001F1 RID: 497 RVA: 0x000126B0 File Offset: 0x000108B0
		public QuestItemSortControllerVM(ref MBBindingList<QuestItemVM> listToControl)
		{
			this._listToControl = listToControl;
			this._dateStartedComparer = new QuestItemSortControllerVM.QuestItemDateStartedComparer();
			this._lastUpdatedComparer = new QuestItemSortControllerVM.QuestItemLastUpdatedComparer();
			this._timeDueComparer = new QuestItemSortControllerVM.QuestItemTimeDueComparer();
			this.IsThereAnyQuest = this._listToControl.Count > 0;
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x00012700 File Offset: 0x00010900
		private void ExecuteSortByDateStarted()
		{
			this._listToControl.Sort(this._dateStartedComparer);
			this.CurrentSortOption = new QuestItemSortControllerVM.QuestItemSortOption?(QuestItemSortControllerVM.QuestItemSortOption.DateStarted);
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x0001271F File Offset: 0x0001091F
		private void ExecuteSortByLastUpdated()
		{
			this._listToControl.Sort(this._lastUpdatedComparer);
			this.CurrentSortOption = new QuestItemSortControllerVM.QuestItemSortOption?(QuestItemSortControllerVM.QuestItemSortOption.LastUpdated);
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x0001273E File Offset: 0x0001093E
		private void ExecuteSortByTimeDue()
		{
			this._listToControl.Sort(this._timeDueComparer);
			this.CurrentSortOption = new QuestItemSortControllerVM.QuestItemSortOption?(QuestItemSortControllerVM.QuestItemSortOption.TimeDue);
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x0001275D File Offset: 0x0001095D
		public void SortByOption(QuestItemSortControllerVM.QuestItemSortOption sortOption)
		{
			if (sortOption == QuestItemSortControllerVM.QuestItemSortOption.DateStarted)
			{
				this.ExecuteSortByDateStarted();
				return;
			}
			if (sortOption == QuestItemSortControllerVM.QuestItemSortOption.LastUpdated)
			{
				this.ExecuteSortByLastUpdated();
				return;
			}
			if (sortOption == QuestItemSortControllerVM.QuestItemSortOption.TimeDue)
			{
				this.ExecuteSortByTimeDue();
			}
		}

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x060001F6 RID: 502 RVA: 0x0001277E File Offset: 0x0001097E
		// (set) Token: 0x060001F7 RID: 503 RVA: 0x00012786 File Offset: 0x00010986
		[DataSourceProperty]
		public bool IsThereAnyQuest
		{
			get
			{
				return this._isThereAnyQuest;
			}
			set
			{
				if (value != this._isThereAnyQuest)
				{
					this._isThereAnyQuest = value;
					base.OnPropertyChangedWithValue(value, "IsThereAnyQuest");
				}
			}
		}

		// Token: 0x040000E0 RID: 224
		private MBBindingList<QuestItemVM> _listToControl;

		// Token: 0x040000E1 RID: 225
		private QuestItemSortControllerVM.QuestItemDateStartedComparer _dateStartedComparer;

		// Token: 0x040000E2 RID: 226
		private QuestItemSortControllerVM.QuestItemLastUpdatedComparer _lastUpdatedComparer;

		// Token: 0x040000E3 RID: 227
		private QuestItemSortControllerVM.QuestItemTimeDueComparer _timeDueComparer;

		// Token: 0x040000E5 RID: 229
		private bool _isThereAnyQuest;

		// Token: 0x0200018C RID: 396
		public enum QuestItemSortOption
		{
			// Token: 0x0400106C RID: 4204
			DateStarted,
			// Token: 0x0400106D RID: 4205
			LastUpdated,
			// Token: 0x0400106E RID: 4206
			TimeDue
		}

		// Token: 0x0200018D RID: 397
		private abstract class QuestItemComparerBase : IComparer<QuestItemVM>
		{
			// Token: 0x0600228D RID: 8845
			public abstract int Compare(QuestItemVM x, QuestItemVM y);

			// Token: 0x0600228E RID: 8846 RVA: 0x0007CB60 File Offset: 0x0007AD60
			protected JournalLog GetJournalLogAt(QuestItemVM questItem, QuestItemSortControllerVM.QuestItemComparerBase.JournalLogIndex logIndex)
			{
				if (questItem.Quest == null && questItem.Stages.Count > 0)
				{
					int index = ((logIndex == QuestItemSortControllerVM.QuestItemComparerBase.JournalLogIndex.First) ? 0 : (questItem.Stages.Count - 1));
					return questItem.Stages[index].Log;
				}
				if (questItem.Quest != null && questItem.Quest.JournalEntries.Count > 0)
				{
					int index2 = ((logIndex == QuestItemSortControllerVM.QuestItemComparerBase.JournalLogIndex.First) ? 0 : (questItem.Quest.JournalEntries.Count - 1));
					return questItem.Quest.JournalEntries[index2];
				}
				return null;
			}

			// Token: 0x020002F1 RID: 753
			protected enum JournalLogIndex
			{
				// Token: 0x040013E0 RID: 5088
				First,
				// Token: 0x040013E1 RID: 5089
				Last
			}
		}

		// Token: 0x0200018E RID: 398
		private class QuestItemDateStartedComparer : QuestItemSortControllerVM.QuestItemComparerBase
		{
			// Token: 0x06002290 RID: 8848 RVA: 0x0007CBF8 File Offset: 0x0007ADF8
			public override int Compare(QuestItemVM first, QuestItemVM second)
			{
				JournalLog journalLogAt = base.GetJournalLogAt(first, QuestItemSortControllerVM.QuestItemComparerBase.JournalLogIndex.First);
				JournalLog journalLogAt2 = base.GetJournalLogAt(second, QuestItemSortControllerVM.QuestItemComparerBase.JournalLogIndex.First);
				if (journalLogAt != null && journalLogAt2 != null)
				{
					return journalLogAt.LogTime.CompareTo(journalLogAt2.LogTime);
				}
				if (journalLogAt == null && journalLogAt2 != null)
				{
					return -1;
				}
				if (journalLogAt != null && journalLogAt2 == null)
				{
					return 1;
				}
				return 0;
			}
		}

		// Token: 0x0200018F RID: 399
		private class QuestItemLastUpdatedComparer : QuestItemSortControllerVM.QuestItemComparerBase
		{
			// Token: 0x06002292 RID: 8850 RVA: 0x0007CC4C File Offset: 0x0007AE4C
			public override int Compare(QuestItemVM first, QuestItemVM second)
			{
				JournalLog journalLogAt = base.GetJournalLogAt(first, QuestItemSortControllerVM.QuestItemComparerBase.JournalLogIndex.Last);
				JournalLog journalLogAt2 = base.GetJournalLogAt(second, QuestItemSortControllerVM.QuestItemComparerBase.JournalLogIndex.Last);
				if (journalLogAt != null && journalLogAt2 != null)
				{
					return journalLogAt2.LogTime.CompareTo(journalLogAt.LogTime);
				}
				if (journalLogAt == null && journalLogAt2 != null)
				{
					return -1;
				}
				if (journalLogAt != null && journalLogAt2 == null)
				{
					return 1;
				}
				return 0;
			}
		}

		// Token: 0x02000190 RID: 400
		private class QuestItemTimeDueComparer : QuestItemSortControllerVM.QuestItemComparerBase
		{
			// Token: 0x06002294 RID: 8852 RVA: 0x0007CCA0 File Offset: 0x0007AEA0
			public override int Compare(QuestItemVM first, QuestItemVM second)
			{
				CampaignTime campaignTime = CampaignTime.Now;
				CampaignTime other = CampaignTime.Now;
				if (first.Quest != null)
				{
					campaignTime = first.Quest.QuestDueTime;
				}
				if (second.Quest != null)
				{
					other = second.Quest.QuestDueTime;
				}
				return campaignTime.CompareTo(other);
			}
		}
	}
}
