using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.BannerBuilder;

public class BannerBuilderColorSelectionVM : ViewModel
{
	private Action<BannerBuilderColorItemVM> _onSelection;

	private MBBindingList<BannerBuilderColorItemVM> _items;

	private bool _isEnabled;

	[DataSourceProperty]
	public MBBindingList<BannerBuilderColorItemVM> Items
	{
		get
		{
			return _items;
		}
		set
		{
			if (value != _items)
			{
				_items = value;
				OnPropertyChangedWithValue(value, "Items");
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

	public BannerBuilderColorSelectionVM()
	{
		Items = new MBBindingList<BannerBuilderColorItemVM>();
		PopulateItems();
	}

	public void EnableWith(int selectedColorID, Action<BannerBuilderColorItemVM> onSelection)
	{
		_onSelection = onSelection;
		Items.ApplyActionOnAllItems(delegate(BannerBuilderColorItemVM i)
		{
			i.IsSelected = i.ColorID == selectedColorID;
		});
		IsEnabled = true;
	}

	private void OnItemSelection(BannerBuilderColorItemVM item)
	{
		_onSelection?.Invoke(item);
		_onSelection = null;
		IsEnabled = false;
	}

	private void PopulateItems()
	{
		Items.Clear();
		MBReadOnlyDictionary<int, BannerColor> readOnlyColorPalette = BannerManager.Instance.ReadOnlyColorPalette;
		for (int i = 0; i < readOnlyColorPalette.Count; i++)
		{
			KeyValuePair<int, BannerColor> keyValuePair = readOnlyColorPalette.ElementAt(i);
			Items.Add(new BannerBuilderColorItemVM(OnItemSelection, keyValuePair.Key, keyValuePair.Value));
		}
	}
}
