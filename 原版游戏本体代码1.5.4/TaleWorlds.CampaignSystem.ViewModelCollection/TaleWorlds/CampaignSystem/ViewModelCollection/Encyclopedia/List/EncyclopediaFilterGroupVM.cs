using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List;

public class EncyclopediaFilterGroupVM : ViewModel
{
	public readonly EncyclopediaFilterGroup FilterGroup;

	private MBBindingList<EncyclopediaListFilterVM> _filters;

	private string _filterName;

	[DataSourceProperty]
	public string FilterName
	{
		get
		{
			return _filterName;
		}
		set
		{
			if (value != _filterName)
			{
				_filterName = value;
				OnPropertyChangedWithValue(value, "FilterName");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<EncyclopediaListFilterVM> Filters
	{
		get
		{
			return _filters;
		}
		set
		{
			if (value != _filters)
			{
				_filters = value;
				OnPropertyChangedWithValue(value, "Filters");
			}
		}
	}

	public EncyclopediaFilterGroupVM(EncyclopediaFilterGroup filterGroup, Action<EncyclopediaListFilterVM> UpdateFilters)
	{
		FilterGroup = filterGroup;
		Filters = new MBBindingList<EncyclopediaListFilterVM>();
		foreach (EncyclopediaFilterItem filter in filterGroup.Filters)
		{
			Filters.Add(new EncyclopediaListFilterVM(filter, UpdateFilters));
		}
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Filters.ApplyActionOnAllItems(delegate(EncyclopediaListFilterVM x)
		{
			x.RefreshValues();
		});
		FilterName = FilterGroup.Name.ToString();
	}

	public void CopyFiltersFrom(Dictionary<EncyclopediaFilterItem, bool> filters)
	{
		Filters.ApplyActionOnAllItems(delegate(EncyclopediaListFilterVM x)
		{
			x.CopyFilterFrom(filters);
		});
	}
}
