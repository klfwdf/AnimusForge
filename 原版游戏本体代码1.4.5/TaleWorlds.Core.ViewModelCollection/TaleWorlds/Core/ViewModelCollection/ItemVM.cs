using System;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection;

public class ItemVM : ViewModel
{
	public static Action<ItemVM> ProcessEquipItem;

	public static Action<ItemVM> ProcessPreviewItem;

	public static Action<ItemVM> ProcessUnequipItem;

	public static Action<ItemVM, bool> ProcessBuyItem;

	public static Action<ItemVM> ProcessItemSelect;

	public static Action<ItemVM> ProcessItemTooltip;

	private string _typeName;

	private int _itemCost = -1;

	private bool _isFiltered;

	private string _itemDescription;

	public ItemRosterElement ItemRosterElement;

	public EquipmentIndex _itemType = EquipmentIndex.None;

	private ItemImageIdentifierVM _imageIdentifier;

	private HintViewModel _previewHint;

	private HintViewModel _equipHint;

	private HintViewModel _unequipHint;

	private BasicTooltipViewModel _buyAndEquip;

	private BasicTooltipViewModel _sellHint;

	private BasicTooltipViewModel _buyHint;

	private HintViewModel _lockHint;

	private BasicTooltipViewModel _slaughterHint;

	private BasicTooltipViewModel _donateHint;

	private string _stringId;

	public int TypeId { get; private set; }

	public int Version { get; protected set; }

	[DataSourceProperty]
	public EquipmentIndex ItemType
	{
		get
		{
			if (_itemType == EquipmentIndex.None)
			{
				return GetItemTypeWithItemObject();
			}
			return _itemType;
		}
	}

	[DataSourceProperty]
	public ItemImageIdentifierVM ImageIdentifier
	{
		get
		{
			return _imageIdentifier;
		}
		set
		{
			if (value != _imageIdentifier)
			{
				_imageIdentifier = value;
				OnPropertyChangedWithValue(value, "ImageIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public string StringId
	{
		get
		{
			return _stringId;
		}
		set
		{
			if (value != _stringId)
			{
				_stringId = value;
				OnPropertyChangedWithValue(value, "StringId");
			}
		}
	}

	[DataSourceProperty]
	public string ItemDescription
	{
		get
		{
			return _itemDescription;
		}
		set
		{
			if (value != _itemDescription)
			{
				_itemDescription = value;
				OnPropertyChangedWithValue(value, "ItemDescription");
			}
		}
	}

	[DataSourceProperty]
	public bool IsFiltered
	{
		get
		{
			return _isFiltered;
		}
		set
		{
			if (value != _isFiltered)
			{
				_isFiltered = value;
				OnPropertyChangedWithValue(value, "IsFiltered");
			}
		}
	}

	[DataSourceProperty]
	public int ItemCost
	{
		get
		{
			return _itemCost;
		}
		set
		{
			if (value != _itemCost)
			{
				_itemCost = value;
				OnPropertyChangedWithValue(value, "ItemCost");
			}
		}
	}

	[DataSourceProperty]
	public string TypeName
	{
		get
		{
			return _typeName;
		}
		set
		{
			if (value != _typeName)
			{
				_typeName = value;
				OnPropertyChangedWithValue(value, "TypeName");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel PreviewHint
	{
		get
		{
			return _previewHint;
		}
		set
		{
			if (value != _previewHint)
			{
				_previewHint = value;
				OnPropertyChangedWithValue(value, "PreviewHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel EquipHint
	{
		get
		{
			return _equipHint;
		}
		set
		{
			if (value != _equipHint)
			{
				_equipHint = value;
				OnPropertyChangedWithValue(value, "EquipHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel UnequipHint
	{
		get
		{
			return _unequipHint;
		}
		set
		{
			if (value != _unequipHint)
			{
				_unequipHint = value;
				OnPropertyChangedWithValue(value, "UnequipHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel SlaughterHint
	{
		get
		{
			return _slaughterHint;
		}
		set
		{
			if (value != _slaughterHint)
			{
				_slaughterHint = value;
				OnPropertyChangedWithValue(value, "SlaughterHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel DonateHint
	{
		get
		{
			return _donateHint;
		}
		set
		{
			if (value != _donateHint)
			{
				_donateHint = value;
				OnPropertyChangedWithValue(value, "DonateHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel BuyAndEquipHint
	{
		get
		{
			return _buyAndEquip;
		}
		set
		{
			if (value != _buyAndEquip)
			{
				_buyAndEquip = value;
				OnPropertyChangedWithValue(value, "BuyAndEquipHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel SellHint
	{
		get
		{
			return _sellHint;
		}
		set
		{
			if (value != _sellHint)
			{
				_sellHint = value;
				OnPropertyChangedWithValue(value, "SellHint");
			}
		}
	}

	[DataSourceProperty]
	public BasicTooltipViewModel BuyHint
	{
		get
		{
			return _buyHint;
		}
		set
		{
			if (value != _buyHint)
			{
				_buyHint = value;
				OnPropertyChangedWithValue(value, "BuyHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel LockHint
	{
		get
		{
			return _lockHint;
		}
		set
		{
			if (value != _lockHint)
			{
				_lockHint = value;
				OnPropertyChangedWithValue(value, "LockHint");
			}
		}
	}

	public ItemVM()
	{
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
	}

	public void ExecutePreviewItem()
	{
		if (!UiStringHelper.IsStringNoneOrEmptyForUi(StringId))
		{
			ProcessPreviewItem(this);
		}
	}

	public void ExecuteUnequipItem()
	{
		if (!UiStringHelper.IsStringNoneOrEmptyForUi(StringId))
		{
			ProcessUnequipItem(this);
		}
	}

	public void ExecuteEquipItem()
	{
		if (!UiStringHelper.IsStringNoneOrEmptyForUi(StringId))
		{
			ProcessEquipItem(this);
		}
	}

	public static void ReleaseStaticContent()
	{
		ProcessEquipItem = null;
		ProcessPreviewItem = null;
		ProcessUnequipItem = null;
		ProcessBuyItem = null;
		ProcessItemSelect = null;
		ProcessItemTooltip = null;
	}

	public void ExecuteRefreshTooltip()
	{
		if (ProcessItemTooltip != null && !UiStringHelper.IsStringNoneOrEmptyForUi(StringId))
		{
			ProcessItemTooltip(this);
		}
	}

	public void ExecuteCancelTooltip()
	{
	}

	public void ExecuteBuyItem()
	{
		if (!UiStringHelper.IsStringNoneOrEmptyForUi(StringId))
		{
			ProcessBuyItem(this, arg2: false);
		}
	}

	public void ExecuteSelectItem()
	{
		if (!UiStringHelper.IsStringNoneOrEmptyForUi(StringId))
		{
			ProcessItemSelect(this);
		}
	}

	public EquipmentIndex GetItemTypeWithItemObject()
	{
		if (ItemRosterElement.EquipmentElement.Item == null)
		{
			return EquipmentIndex.None;
		}
		switch (ItemRosterElement.EquipmentElement.Item.Type)
		{
		case ItemObject.ItemTypeEnum.BodyArmor:
			return EquipmentIndex.Body;
		case ItemObject.ItemTypeEnum.LegArmor:
			return EquipmentIndex.Leg;
		case ItemObject.ItemTypeEnum.Cape:
			return EquipmentIndex.Cape;
		case ItemObject.ItemTypeEnum.HandArmor:
			return EquipmentIndex.Gloves;
		case ItemObject.ItemTypeEnum.HeadArmor:
			return EquipmentIndex.NumAllWeaponSlots;
		case ItemObject.ItemTypeEnum.HorseHarness:
			return EquipmentIndex.HorseHarness;
		case ItemObject.ItemTypeEnum.Horse:
			return EquipmentIndex.ArmorItemEndSlot;
		case ItemObject.ItemTypeEnum.Arrows:
		case ItemObject.ItemTypeEnum.Bolts:
		case ItemObject.ItemTypeEnum.SlingStones:
			return EquipmentIndex.WeaponItemBeginSlot;
		case ItemObject.ItemTypeEnum.Banner:
			return EquipmentIndex.ExtraWeaponSlot;
		case ItemObject.ItemTypeEnum.Shield:
			if (_typeName == ItemObject.ItemTypeEnum.Invalid.ToString())
			{
				_typeName = ItemObject.ItemTypeEnum.Horse.ToString();
			}
			return EquipmentIndex.WeaponItemBeginSlot;
		default:
			if (ItemRosterElement.EquipmentElement.Item.WeaponComponent != null)
			{
				return EquipmentIndex.WeaponItemBeginSlot;
			}
			return EquipmentIndex.None;
		}
	}

	protected void OnItemTypeUpdated()
	{
		TypeId = (int)ItemRosterElement.EquipmentElement.Item.Type;
		TypeName = ItemRosterElement.EquipmentElement.Item.Type.ToString();
	}
}
