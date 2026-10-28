using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;

public abstract class KingdomItemVM : ViewModel
{
	private bool _isSelected;

	private bool _isNew;

	[DataSourceProperty]
	public bool IsNew
	{
		get
		{
			return _isNew;
		}
		set
		{
			if (value != _isNew)
			{
				_isNew = value;
				OnPropertyChangedWithValue(value, "IsNew");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	protected virtual void OnSelect()
	{
		IsSelected = true;
	}
}
