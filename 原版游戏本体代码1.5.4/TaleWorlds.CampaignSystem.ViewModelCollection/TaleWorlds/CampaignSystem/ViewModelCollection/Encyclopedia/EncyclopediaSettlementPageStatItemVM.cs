using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;

public class EncyclopediaSettlementPageStatItemVM : ViewModel
{
	public enum DescriptionType
	{
		Wall,
		Shipyard,
		Garrison,
		Militia,
		Food,
		Prosperity,
		Loyalty,
		Security
	}

	private BasicTooltipViewModel _basicTooltipViewModel;

	private string _typeString;

	private string _statText;

	[DataSourceProperty]
	public BasicTooltipViewModel BasicTooltipViewModel
	{
		get
		{
			return _basicTooltipViewModel;
		}
		set
		{
			if (value != _basicTooltipViewModel)
			{
				_basicTooltipViewModel = value;
				OnPropertyChangedWithValue(value, "BasicTooltipViewModel");
			}
		}
	}

	[DataSourceProperty]
	public string TypeString
	{
		get
		{
			return _typeString;
		}
		set
		{
			if (value != _typeString)
			{
				_typeString = value;
				OnPropertyChangedWithValue(value, "TypeString");
			}
		}
	}

	[DataSourceProperty]
	public string StatText
	{
		get
		{
			return _statText;
		}
		set
		{
			if (value != _statText)
			{
				_statText = value;
				OnPropertyChangedWithValue(value, "StatText");
			}
		}
	}

	public EncyclopediaSettlementPageStatItemVM(BasicTooltipViewModel basicTooltipViewModel, DescriptionType type, string statText)
	{
		_basicTooltipViewModel = basicTooltipViewModel;
		_typeString = type.ToString();
		_statText = statText;
	}
}
