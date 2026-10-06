using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Education;

public class EducationReviewItemVM : ViewModel
{
	private string _title;

	private string _gainText;

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

	[DataSourceProperty]
	public string GainText
	{
		get
		{
			return _gainText;
		}
		set
		{
			if (value != _gainText)
			{
				_gainText = value;
				OnPropertyChangedWithValue(value, "GainText");
			}
		}
	}

	public void UpdateWith(string gainText)
	{
		GainText = gainText;
	}
}
