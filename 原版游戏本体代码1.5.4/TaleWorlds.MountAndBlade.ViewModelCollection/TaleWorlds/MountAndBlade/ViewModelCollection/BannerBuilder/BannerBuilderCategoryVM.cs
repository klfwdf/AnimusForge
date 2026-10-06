using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.BannerBuilder;

public class BannerBuilderCategoryVM : ViewModel
{
	private readonly BannerIconGroup _category;

	private readonly Action<BannerBuilderItemVM> _onItemSelection;

	private string _title;

	private bool _isPattern;

	private bool _isEnabled;

	private MBBindingList<BannerBuilderItemVM> _itemsList;

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
	public bool IsPattern
	{
		get
		{
			return _isPattern;
		}
		set
		{
			if (value != _isPattern)
			{
				_isPattern = value;
				OnPropertyChangedWithValue(value, "IsPattern");
			}
		}
	}

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
	public MBBindingList<BannerBuilderItemVM> ItemsList
	{
		get
		{
			return _itemsList;
		}
		set
		{
			if (value != _itemsList)
			{
				_itemsList = value;
				OnPropertyChangedWithValue(value, "ItemsList");
			}
		}
	}

	public BannerBuilderCategoryVM(BannerIconGroup category, Action<BannerBuilderItemVM> onItemSelection)
	{
		ItemsList = new MBBindingList<BannerBuilderItemVM>();
		_category = category;
		_onItemSelection = onItemSelection;
		IsPattern = _category.IsPattern;
		IsEnabled = true;
		PopulateItems();
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Title = _category.Name.ToString();
	}

	private void PopulateItems()
	{
		ItemsList.Clear();
		if (IsPattern)
		{
			for (int i = 0; i < _category.AllBackgrounds.Count; i++)
			{
				KeyValuePair<int, string> keyValuePair = _category.AllBackgrounds.ElementAt(i);
				ItemsList.Add(new BannerBuilderItemVM(keyValuePair.Key, keyValuePair.Value, _onItemSelection));
			}
		}
		else
		{
			for (int j = 0; j < _category.AllIcons.Count; j++)
			{
				KeyValuePair<int, BannerIconData> keyValuePair2 = _category.AllIcons.ElementAt(j);
				ItemsList.Add(new BannerBuilderItemVM(keyValuePair2.Key, keyValuePair2.Value, _onItemSelection));
			}
		}
	}
}
