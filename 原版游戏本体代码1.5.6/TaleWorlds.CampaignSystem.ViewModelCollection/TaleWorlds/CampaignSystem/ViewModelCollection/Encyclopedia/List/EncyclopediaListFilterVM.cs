using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;

public class EncyclopediaListFilterVM : ViewModel
{
	public readonly EncyclopediaFilterItem Filter;

	private readonly Action<EncyclopediaListFilterVM> _updateFilters;

	private string _name;

	private bool _isSelected;

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
				Filter.IsActive = value;
				_updateFilters(this);
			}
		}
	}

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

	public EncyclopediaListFilterVM(EncyclopediaFilterItem filter, Action<EncyclopediaListFilterVM> UpdateFilters)
	{
		Filter = filter;
		_isSelected = Filter.IsActive;
		_updateFilters = UpdateFilters;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = Filter.Name.ToString();
	}

	public void CopyFilterFrom(Dictionary<EncyclopediaFilterItem, bool> filters)
	{
		if (filters.ContainsKey(Filter))
		{
			IsSelected = filters[Filter];
		}
	}

	public void ExecuteOnFilterActivated()
	{
		Game.Current.EventManager.TriggerEvent(new OnEncyclopediaFilterActivatedEvent());
	}
}
