using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education;

public class EducationReviewVM : ViewModel
{
	private readonly int _pageCount;

	private readonly TextObject _educationPageTitle = new TextObject("{=m1Yynagz}Page {NUMBER}");

	private readonly TextObject _stageCompleteTextObject = new TextObject("{=flxDkoMh}Stage Complete");

	private MBBindingList<EducationReviewItemVM> _reviewList;

	private bool _isEnabled;

	private string _stageCompleteText;

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public string StageCompleteText
	{
		get
		{
			return _stageCompleteText;
		}
		set
		{
			if (value != _stageCompleteText)
			{
				_stageCompleteText = value;
				OnPropertyChangedWithValue(value, "StageCompleteText");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EducationReviewItemVM> ReviewList
	{
		get
		{
			return _reviewList;
		}
		set
		{
			if (value != _reviewList)
			{
				_reviewList = value;
				OnPropertyChangedWithValue(value, "ReviewList");
			}
		}
	}

	public EducationReviewVM(int pageCount)
	{
		_pageCount = pageCount;
		ReviewList = new MBBindingList<EducationReviewItemVM>();
		for (int i = 0; i < _pageCount - 1; i++)
		{
			ReviewList.Add(new EducationReviewItemVM());
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		for (int i = 0; i < ReviewList.Count; i++)
		{
			_educationPageTitle.SetTextVariable("NUMBER", i + 1);
			ReviewList[i].Title = _educationPageTitle.ToString();
		}
		StageCompleteText = _stageCompleteTextObject.ToString();
	}

	public void SetGainForStage(int pageIndex, string gainText)
	{
		if (pageIndex >= 0 && pageIndex < _pageCount)
		{
			ReviewList[pageIndex].UpdateWith(gainText);
		}
	}

	public void SetCurrentPage(int currentPageIndex)
	{
		IsEnabled = currentPageIndex == _pageCount - 1;
	}
}
