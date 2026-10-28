using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Information;

public class TooltipTraitLevelVM : ViewModel
{
	private string _name;

	private bool _isSelected;

	private bool _isFirstTrait;

	private bool _isLastTrait;

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
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

	[DataSourceProperty]
	public bool IsFirstTrait
	{
		get
		{
			return _isFirstTrait;
		}
		set
		{
			if (value != _isFirstTrait)
			{
				_isFirstTrait = value;
				OnPropertyChangedWithValue(value, "IsFirstTrait");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLastTrait
	{
		get
		{
			return _isLastTrait;
		}
		set
		{
			if (value != _isLastTrait)
			{
				_isLastTrait = value;
				OnPropertyChangedWithValue(value, "IsLastTrait");
			}
		}
	}

	public TooltipTraitLevelVM(string name, bool isSelected, bool isFirstTrait, bool isLastTrait)
	{
		Name = name;
		IsSelected = isSelected;
		IsFirstTrait = isFirstTrait;
		IsLastTrait = isLastTrait;
	}
}
