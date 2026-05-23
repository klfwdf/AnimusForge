using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;

public abstract class KingdomCategoryVM : ViewModel
{
	private int _notificationCount;

	private string _categoryNameText;

	private string _noItemSelectedText;

	private bool _show;

	private bool _isAcceptableItemSelected;

	[DataSourceProperty]
	public string CategoryNameText
	{
		get
		{
			return _categoryNameText;
		}
		set
		{
			if (value != _categoryNameText)
			{
				_categoryNameText = value;
				OnPropertyChanged("NameText");
			}
		}
	}

	[DataSourceProperty]
	public string NoItemSelectedText
	{
		get
		{
			return _noItemSelectedText;
		}
		set
		{
			if (value != _noItemSelectedText)
			{
				_noItemSelectedText = value;
				OnPropertyChangedWithValue(value, "NoItemSelectedText");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAcceptableItemSelected
	{
		get
		{
			return _isAcceptableItemSelected;
		}
		set
		{
			if (value != _isAcceptableItemSelected)
			{
				_isAcceptableItemSelected = value;
				OnPropertyChangedWithValue(value, "IsAcceptableItemSelected");
			}
		}
	}

	[DataSourceProperty]
	public int NotificationCount
	{
		get
		{
			return _notificationCount;
		}
		set
		{
			if (value != _notificationCount)
			{
				_notificationCount = value;
				OnPropertyChanged("NotificationCount");
			}
		}
	}

	[DataSourceProperty]
	public bool Show
	{
		get
		{
			return _show;
		}
		set
		{
			if (value != _show)
			{
				_show = value;
				OnPropertyChanged("Show");
			}
		}
	}
}
