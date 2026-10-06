using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Quests;

public class QuestItemSortControllerVM : ViewModel
{
	public enum QuestItemSortOption
	{
		DateStarted,
		LastUpdated,
		TimeDue
	}

	private abstract class QuestItemComparerBase : IComparer<QuestItemVM>
	{
		protected enum JournalLogIndex
		{
			First,
			Last
		}

		public abstract int Compare(QuestItemVM x, QuestItemVM y);

		protected JournalLog GetJournalLogAt(QuestItemVM questItem, JournalLogIndex logIndex)
		{
			if (questItem.Quest == null && questItem.Stages.Count > 0)
			{
				int index = ((logIndex != JournalLogIndex.First) ? (questItem.Stages.Count - 1) : 0);
				return questItem.Stages[index].Log;
			}
			if (questItem.Quest != null && questItem.Quest.JournalEntries.Count > 0)
			{
				int index2 = ((logIndex != JournalLogIndex.First) ? (questItem.Quest.JournalEntries.Count - 1) : 0);
				return questItem.Quest.JournalEntries[index2];
			}
			return null;
		}
	}

	private class QuestItemDateStartedComparer : QuestItemComparerBase
	{
		public override int Compare(QuestItemVM first, QuestItemVM second)
		{
			JournalLog journalLogAt = GetJournalLogAt(first, JournalLogIndex.First);
			JournalLog journalLogAt2 = GetJournalLogAt(second, JournalLogIndex.First);
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

	private class QuestItemLastUpdatedComparer : QuestItemComparerBase
	{
		public override int Compare(QuestItemVM first, QuestItemVM second)
		{
			JournalLog journalLogAt = GetJournalLogAt(first, JournalLogIndex.Last);
			JournalLog journalLogAt2 = GetJournalLogAt(second, JournalLogIndex.Last);
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

	private class QuestItemTimeDueComparer : QuestItemComparerBase
	{
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

	private MBBindingList<QuestItemVM> _listToControl;

	private QuestItemDateStartedComparer _dateStartedComparer;

	private QuestItemLastUpdatedComparer _lastUpdatedComparer;

	private QuestItemTimeDueComparer _timeDueComparer;

	private bool _isThereAnyQuest;

	public QuestItemSortOption? CurrentSortOption { get; private set; }

	[DataSourceProperty]
	public bool IsThereAnyQuest
	{
		get
		{
			return _isThereAnyQuest;
		}
		set
		{
			if (value != _isThereAnyQuest)
			{
				_isThereAnyQuest = value;
				OnPropertyChangedWithValue(value, "IsThereAnyQuest");
			}
		}
	}

	public QuestItemSortControllerVM(ref MBBindingList<QuestItemVM> listToControl)
	{
		_listToControl = listToControl;
		_dateStartedComparer = new QuestItemDateStartedComparer();
		_lastUpdatedComparer = new QuestItemLastUpdatedComparer();
		_timeDueComparer = new QuestItemTimeDueComparer();
		IsThereAnyQuest = _listToControl.Count > 0;
	}

	private void ExecuteSortByDateStarted()
	{
		_listToControl.Sort(_dateStartedComparer);
		CurrentSortOption = QuestItemSortOption.DateStarted;
	}

	private void ExecuteSortByLastUpdated()
	{
		_listToControl.Sort(_lastUpdatedComparer);
		CurrentSortOption = QuestItemSortOption.LastUpdated;
	}

	private void ExecuteSortByTimeDue()
	{
		_listToControl.Sort(_timeDueComparer);
		CurrentSortOption = QuestItemSortOption.TimeDue;
	}

	public void SortByOption(QuestItemSortOption sortOption)
	{
		switch (sortOption)
		{
		case QuestItemSortOption.DateStarted:
			ExecuteSortByDateStarted();
			break;
		case QuestItemSortOption.LastUpdated:
			ExecuteSortByLastUpdated();
			break;
		case QuestItemSortOption.TimeDue:
			ExecuteSortByTimeDue();
			break;
		}
	}
}
