using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class TierFilterTypeVM : ViewModel
{
	private readonly Action<WeaponDesignVM.CraftingPieceTierFilter> _onSelect;

	private bool _isSelected;

	private string _tierName;

	public WeaponDesignVM.CraftingPieceTierFilter FilterType { get; }

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
	public string TierName
	{
		get
		{
			return _tierName;
		}
		set
		{
			if (value != _tierName)
			{
				_tierName = value;
				OnPropertyChangedWithValue(value, "TierName");
			}
		}
	}

	public TierFilterTypeVM(WeaponDesignVM.CraftingPieceTierFilter filterType, Action<WeaponDesignVM.CraftingPieceTierFilter> onSelect, string tierName)
	{
		FilterType = filterType;
		_onSelect = onSelect;
		TierName = tierName;
	}

	public void ExecuteSelectTier()
	{
		_onSelect?.Invoke(FilterType);
	}
}
