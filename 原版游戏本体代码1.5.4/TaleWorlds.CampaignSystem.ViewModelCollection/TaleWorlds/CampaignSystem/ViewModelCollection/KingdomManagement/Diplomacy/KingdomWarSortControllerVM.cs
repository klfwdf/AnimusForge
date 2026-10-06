using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;

public class KingdomWarSortControllerVM : ViewModel
{
	public abstract class ItemComparerBase : IComparer<KingdomWarItemVM>
	{
		protected bool _isAscending;

		public void SetSortMode(bool isAscending)
		{
			_isAscending = isAscending;
		}

		public abstract int Compare(KingdomWarItemVM x, KingdomWarItemVM y);
	}

	public class ItemScoreComparer : ItemComparerBase
	{
		public override int Compare(KingdomWarItemVM x, KingdomWarItemVM y)
		{
			if (_isAscending)
			{
				return x.Score.CompareTo(y.Score);
			}
			return x.Score.CompareTo(y.Score) * -1;
		}
	}

	private readonly MBBindingList<KingdomWarItemVM> _listToControl;

	private readonly ItemScoreComparer _scoreComparer;

	private int _scoreState;

	private bool _isScoreSelected;

	[DataSourceProperty]
	public int ScoreState
	{
		get
		{
			return _scoreState;
		}
		set
		{
			if (value != _scoreState)
			{
				_scoreState = value;
				OnPropertyChangedWithValue(value, "ScoreState");
			}
		}
	}

	[DataSourceProperty]
	public bool IsScoreSelected
	{
		get
		{
			return _isScoreSelected;
		}
		set
		{
			if (value != _isScoreSelected)
			{
				_isScoreSelected = value;
				OnPropertyChangedWithValue(value, "IsScoreSelected");
			}
		}
	}

	public KingdomWarSortControllerVM(ref MBBindingList<KingdomWarItemVM> listToControl)
	{
		_listToControl = listToControl;
		_scoreComparer = new ItemScoreComparer();
	}

	private void ExecuteSortByScore()
	{
		int scoreState = ScoreState;
		SetAllStates(CampaignUIHelper.SortState.Default);
		ScoreState = (scoreState + 1) % 3;
		if (ScoreState == 0)
		{
			ScoreState++;
		}
		_scoreComparer.SetSortMode(ScoreState == 1);
		_listToControl.Sort(_scoreComparer);
		IsScoreSelected = true;
	}

	private void SetAllStates(CampaignUIHelper.SortState state)
	{
		ScoreState = (int)state;
		IsScoreSelected = false;
	}
}
