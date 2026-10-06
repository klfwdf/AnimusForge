using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;

public class EncyclopediaContentPageVM : EncyclopediaPageVM
{
	private EncyclopediaListItemVM _previousItem;

	private EncyclopediaListItemVM _nextItem;

	private TextObject _previousButtonLabelText = new TextObject("{=zlcMGAbn}Previous Page");

	private TextObject _nextButtonLabelText = new TextObject("{=QFfMd5q3}Next Page");

	private bool _isPreviousButtonEnabled;

	private bool _isNextButtonEnabled;

	private string _previousButtonLabel;

	private string _nextButtonLabel;

	private HintViewModel _previousButtonHint;

	private HintViewModel _nextButtonHint;

	[DataSourceProperty]
	public bool IsPreviousButtonEnabled
	{
		get
		{
			return _isPreviousButtonEnabled;
		}
		set
		{
			if (value != _isPreviousButtonEnabled)
			{
				_isPreviousButtonEnabled = value;
				OnPropertyChangedWithValue(value, "IsPreviousButtonEnabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNextButtonEnabled
	{
		get
		{
			return _isNextButtonEnabled;
		}
		set
		{
			if (value != _isNextButtonEnabled)
			{
				_isNextButtonEnabled = value;
				OnPropertyChangedWithValue(value, "IsNextButtonEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string PreviousButtonLabel
	{
		get
		{
			return _previousButtonLabel;
		}
		set
		{
			if (value != _previousButtonLabel)
			{
				_previousButtonLabel = value;
				OnPropertyChangedWithValue(value, "PreviousButtonLabel");
			}
		}
	}

	[DataSourceProperty]
	public string NextButtonLabel
	{
		get
		{
			return _nextButtonLabel;
		}
		set
		{
			if (value != _nextButtonLabel)
			{
				_nextButtonLabel = value;
				OnPropertyChangedWithValue(value, "NextButtonLabel");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PreviousButtonHint
	{
		get
		{
			return _previousButtonHint;
		}
		set
		{
			if (value != _previousButtonHint)
			{
				_previousButtonHint = value;
				OnPropertyChangedWithValue(value, "PreviousButtonHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel NextButtonHint
	{
		get
		{
			return _nextButtonHint;
		}
		set
		{
			if (value != _nextButtonHint)
			{
				_nextButtonHint = value;
				OnPropertyChangedWithValue(value, "NextButtonHint");
			}
		}
	}

	public EncyclopediaContentPageVM(EncyclopediaPageArgs args)
		: base(args)
	{
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		PreviousButtonLabel = _previousButtonLabelText.ToString();
		NextButtonLabel = _nextButtonLabelText.ToString();
	}

	public void InitializeQuickNavigation(EncyclopediaListVM list)
	{
		if (list == null || list.Items == null)
		{
			return;
		}
		List<EncyclopediaListItemVM> list2 = list.Items.Where((EncyclopediaListItemVM x) => !x.IsFiltered).ToList();
		int count = list2.Count;
		int num = list2.FindIndex((EncyclopediaListItemVM x) => x.Object == base.Obj);
		if (count > 1 && num > -1)
		{
			if (num > 0)
			{
				_previousItem = list2[num - 1];
				PreviousButtonHint = new HintViewModel(new TextObject(_previousItem.Name));
				IsPreviousButtonEnabled = true;
			}
			if (num < count - 1)
			{
				_nextItem = list2[num + 1];
				NextButtonHint = new HintViewModel(new TextObject(_nextItem.Name));
				IsNextButtonEnabled = true;
			}
		}
	}

	public void ExecuteGoToNextItem()
	{
		if (_nextItem != null)
		{
			_nextItem.Execute();
		}
		else
		{
			Debug.FailedAssert("If the next button is enabled then next item should not be null.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Encyclopedia\\Pages\\EncyclopediaContentPageVM.cs", "ExecuteGoToNextItem", 66);
		}
	}

	public void ExecuteGoToPreviousItem()
	{
		if (_previousItem != null)
		{
			_previousItem.Execute();
		}
		else
		{
			Debug.FailedAssert("If the previous button is enabled then previous item should not be null.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Encyclopedia\\Pages\\EncyclopediaContentPageVM.cs", "ExecuteGoToPreviousItem", 78);
		}
	}
}
