using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

public class ItemPreviewVM : ViewModel
{
	private Action _onClosed;

	private bool _isSelected;

	private string _itemName;

	private ItemCollectionElementViewModel _itemTableau;

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
				OnPropertyChanged("IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public string ItemName
	{
		get
		{
			return _itemName;
		}
		set
		{
			if (value != _itemName)
			{
				_itemName = value;
				OnPropertyChanged("ItemName");
			}
		}
	}

	[DataSourceProperty]
	public ItemCollectionElementViewModel ItemTableau
	{
		get
		{
			return _itemTableau;
		}
		set
		{
			if (value != _itemTableau)
			{
				_itemTableau = value;
				OnPropertyChangedWithValue(value, "ItemTableau");
			}
		}
	}

	public ItemPreviewVM(Action onClosed)
	{
		_onClosed = onClosed;
		ItemTableau = new ItemCollectionElementViewModel();
		RefreshValues();
	}

	public override void OnFinalize()
	{
		ItemTableau.OnFinalize();
		ItemTableau = null;
		base.OnFinalize();
	}

	public void Open(EquipmentElement item)
	{
		ItemTableau.FillFrom(item, Clan.PlayerClan.Banner);
		ItemName = item.Item.Name.ToString();
		IsSelected = true;
	}

	public void ExecuteClose()
	{
		Close();
	}

	public void Close()
	{
		_onClosed();
		IsSelected = false;
	}
}
