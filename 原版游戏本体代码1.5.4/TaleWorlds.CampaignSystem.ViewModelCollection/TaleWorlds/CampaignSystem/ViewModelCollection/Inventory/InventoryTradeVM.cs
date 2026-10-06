using System;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;

public class InventoryTradeVM : ViewModel
{
	private InventoryLogic _inventoryLogic;

	private ItemRosterElement _referenceItemRoster;

	private Action<int, bool> _onApplyTransaction;

	private string _pieceLblSingular;

	private string _pieceLblPlural;

	private bool _isPlayerItem;

	private string _thisStockLbl;

	private string _otherStockLbl;

	private string _averagePriceLbl;

	private string _pieceLbl;

	private HintViewModel _applyExchangeHint;

	private bool _isExchangeAvailable;

	private string _averagePrice;

	private string _pieceChange;

	private string _priceChange;

	private int _thisStock = -1;

	private int _initialThisStock;

	private int _otherStock = -1;

	private int _initialOtherStock;

	private int _totalStock;

	private bool _isThisStockIncreasable;

	private bool _isOtherStockIncreasable;

	private bool _isTrading;

	private bool _isTradeable;

	private HintViewModel _takeHint;

	private HintViewModel _giveHint;

	[DataSourceProperty]
	public string ThisStockLbl
	{
		get
		{
			return _thisStockLbl;
		}
		set
		{
			if (value != _thisStockLbl)
			{
				_thisStockLbl = value;
				OnPropertyChangedWithValue(value, "ThisStockLbl");
			}
		}
	}

	[DataSourceProperty]
	public string OtherStockLbl
	{
		get
		{
			return _otherStockLbl;
		}
		set
		{
			if (value != _otherStockLbl)
			{
				_otherStockLbl = value;
				OnPropertyChangedWithValue(value, "OtherStockLbl");
			}
		}
	}

	[DataSourceProperty]
	public string PieceLbl
	{
		get
		{
			return _pieceLbl;
		}
		set
		{
			if (value != _pieceLbl)
			{
				_pieceLbl = value;
				OnPropertyChangedWithValue(value, "PieceLbl");
			}
		}
	}

	[DataSourceProperty]
	public string AveragePriceLbl
	{
		get
		{
			return _averagePriceLbl;
		}
		set
		{
			if (value != _averagePriceLbl)
			{
				_averagePriceLbl = value;
				OnPropertyChangedWithValue(value, "AveragePriceLbl");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel ApplyExchangeHint
	{
		get
		{
			return _applyExchangeHint;
		}
		set
		{
			if (value != _applyExchangeHint)
			{
				_applyExchangeHint = value;
				OnPropertyChangedWithValue(value, "ApplyExchangeHint");
			}
		}
	}

	[DataSourceProperty]
	public bool IsExchangeAvailable
	{
		get
		{
			return _isExchangeAvailable;
		}
		set
		{
			if (value != _isExchangeAvailable)
			{
				_isExchangeAvailable = value;
				OnPropertyChangedWithValue(value, "IsExchangeAvailable");
			}
		}
	}

	[DataSourceProperty]
	public string PriceChange
	{
		get
		{
			return _priceChange;
		}
		set
		{
			if (value != _priceChange)
			{
				_priceChange = value;
				OnPropertyChangedWithValue(value, "PriceChange");
			}
		}
	}

	[DataSourceProperty]
	public string PieceChange
	{
		get
		{
			return _pieceChange;
		}
		set
		{
			if (value != _pieceChange)
			{
				_pieceChange = value;
				OnPropertyChangedWithValue(value, "PieceChange");
			}
		}
	}

	[DataSourceProperty]
	public string AveragePrice
	{
		get
		{
			return _averagePrice;
		}
		set
		{
			if (value != _averagePrice)
			{
				_averagePrice = value;
				OnPropertyChangedWithValue(value, "AveragePrice");
			}
		}
	}

	[DataSourceProperty]
	public int ThisStock
	{
		get
		{
			return _thisStock;
		}
		set
		{
			if (value != _thisStock)
			{
				_thisStock = value;
				OnPropertyChangedWithValue(value, "ThisStock");
				ThisStockUpdated();
			}
		}
	}

	[DataSourceProperty]
	public int InitialThisStock
	{
		get
		{
			return _initialThisStock;
		}
		set
		{
			if (value != _initialThisStock)
			{
				_initialThisStock = value;
				OnPropertyChangedWithValue(value, "InitialThisStock");
			}
		}
	}

	[DataSourceProperty]
	public int OtherStock
	{
		get
		{
			return _otherStock;
		}
		set
		{
			if (value != _otherStock)
			{
				_otherStock = value;
				OnPropertyChangedWithValue(value, "OtherStock");
			}
		}
	}

	[DataSourceProperty]
	public int InitialOtherStock
	{
		get
		{
			return _initialOtherStock;
		}
		set
		{
			if (value != _initialOtherStock)
			{
				_initialOtherStock = value;
				OnPropertyChangedWithValue(value, "InitialOtherStock");
			}
		}
	}

	[DataSourceProperty]
	public int TotalStock
	{
		get
		{
			return _totalStock;
		}
		set
		{
			if (value != _totalStock)
			{
				_totalStock = value;
				OnPropertyChangedWithValue(value, "TotalStock");
			}
		}
	}

	[DataSourceProperty]
	public bool IsThisStockIncreasable
	{
		get
		{
			return _isThisStockIncreasable;
		}
		set
		{
			if (value != _isThisStockIncreasable)
			{
				_isThisStockIncreasable = value;
				OnPropertyChangedWithValue(value, "IsThisStockIncreasable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsOtherStockIncreasable
	{
		get
		{
			return _isOtherStockIncreasable;
		}
		set
		{
			if (value != _isOtherStockIncreasable)
			{
				_isOtherStockIncreasable = value;
				OnPropertyChangedWithValue(value, "IsOtherStockIncreasable");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTrading
	{
		get
		{
			return _isTrading;
		}
		set
		{
			if (value != _isTrading)
			{
				_isTrading = value;
				OnPropertyChangedWithValue(value, "IsTrading");
			}
		}
	}

	[DataSourceProperty]
	public bool IsTradeable
	{
		get
		{
			return _isTradeable;
		}
		set
		{
			if (value != _isTradeable)
			{
				_isTradeable = value;
				OnPropertyChangedWithValue(value, "IsTradeable");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel TakeHint
	{
		get
		{
			return _takeHint;
		}
		set
		{
			if (value != _takeHint)
			{
				_takeHint = value;
				OnPropertyChangedWithValue(value, "TakeHint");
			}
		}
	}

	[DataSourceProperty]
	public HintViewModel GiveHint
	{
		get
		{
			return _giveHint;
		}
		set
		{
			if (value != _giveHint)
			{
				_giveHint = value;
				OnPropertyChangedWithValue(value, "GiveHint");
			}
		}
	}

	public static event Action RemoveZeroCounts;

	public InventoryTradeVM(InventoryLogic inventoryLogic, ItemRosterElement itemRoster, InventoryLogic.InventorySide side, Action<int, bool> onApplyTransaction)
	{
		_inventoryLogic = inventoryLogic;
		_referenceItemRoster = itemRoster;
		_isPlayerItem = side == InventoryLogic.InventorySide.PlayerInventory;
		_onApplyTransaction = onApplyTransaction;
		PieceLbl = _pieceLblSingular;
		IsTrading = _inventoryLogic?.IsTrading ?? false;
		TakeHint = new HintViewModel(GameTexts.FindText("str_take"));
		GiveHint = new HintViewModel(GameTexts.FindText("str_give"));
		UpdateItemData(itemRoster, side);
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		ThisStockLbl = GameTexts.FindText("str_inventory_this_stock").ToString();
		OtherStockLbl = GameTexts.FindText("str_inventory_total_stock").ToString();
		AveragePriceLbl = GameTexts.FindText("str_inventory_average_price").ToString();
		_pieceLblSingular = GameTexts.FindText("str_inventory_piece").ToString();
		_pieceLblPlural = GameTexts.FindText("str_inventory_pieces").ToString();
		ApplyExchangeHint = new HintViewModel(GameTexts.FindText("str_party_apply_exchange"));
	}

	public void UpdateItemData(ItemRosterElement itemRoster, InventoryLogic.InventorySide side, bool forceUpdate = true)
	{
		if (side == InventoryLogic.InventorySide.OtherInventory || side == InventoryLogic.InventorySide.PlayerInventory)
		{
			ItemRosterElement? itemRosterElement = itemRoster;
			ItemRosterElement? itemRosterElement2 = null;
			switch (side)
			{
			case InventoryLogic.InventorySide.PlayerInventory:
				itemRosterElement2 = FindItemFromSide(itemRoster.EquipmentElement, InventoryLogic.InventorySide.OtherInventory);
				break;
			case InventoryLogic.InventorySide.OtherInventory:
				itemRosterElement2 = FindItemFromSide(itemRoster.EquipmentElement, InventoryLogic.InventorySide.PlayerInventory);
				break;
			}
			if (forceUpdate)
			{
				InitialThisStock = itemRosterElement?.Amount ?? 0;
				InitialOtherStock = itemRosterElement2?.Amount ?? 0;
				TotalStock = InitialThisStock + InitialOtherStock;
				ThisStock = InitialThisStock;
				OtherStock = InitialOtherStock;
				ThisStockUpdated();
			}
		}
	}

	private ItemRosterElement? FindItemFromSide(EquipmentElement item, InventoryLogic.InventorySide side)
	{
		return _inventoryLogic.FindItemFromSide(side, item);
	}

	private void ThisStockUpdated()
	{
		ExecuteApplyTransaction();
		OtherStock = TotalStock - ThisStock;
		IsThisStockIncreasable = OtherStock > 0;
		IsOtherStockIncreasable = OtherStock < TotalStock;
		UpdateProperties();
	}

	private void UpdateProperties()
	{
		int num = ThisStock - InitialThisStock;
		bool flag = num >= 0;
		int num2 = (flag ? num : (-num));
		if (num2 == 0)
		{
			PieceChange = num2.ToString();
			PriceChange = "0";
			AveragePrice = "0";
			IsExchangeAvailable = false;
		}
		else
		{
			int lastPrice;
			int itemTotalPrice = _inventoryLogic.GetItemTotalPrice(_referenceItemRoster, num2, out lastPrice, flag);
			PieceChange = (flag ? "+" : "-") + num2;
			PriceChange = (flag ? "-" : "+") + itemTotalPrice * num2;
			AveragePrice = GetAveragePrice(itemTotalPrice, lastPrice, flag);
			IsExchangeAvailable = true;
		}
		PieceLbl = ((num2 <= 1) ? _pieceLblSingular : _pieceLblPlural);
	}

	public string GetAveragePrice(int totalPrice, int lastPrice, bool isBuying)
	{
		InventoryLogic.InventorySide side = ((!isBuying) ? InventoryLogic.InventorySide.PlayerInventory : InventoryLogic.InventorySide.OtherInventory);
		int costOfItemRosterElement = _inventoryLogic.GetCostOfItemRosterElement(_referenceItemRoster, side);
		if (costOfItemRosterElement != lastPrice)
		{
			if (costOfItemRosterElement < lastPrice)
			{
				return costOfItemRosterElement + " - " + lastPrice;
			}
			return lastPrice + " - " + costOfItemRosterElement;
		}
		return costOfItemRosterElement.ToString();
	}

	public void ExecuteIncreaseThisStock()
	{
		if (ThisStock < TotalStock)
		{
			ThisStock++;
		}
	}

	public void ExecuteIncreaseOtherStock()
	{
		if (ThisStock > 0)
		{
			ThisStock--;
		}
	}

	public void ExecuteReset()
	{
		ThisStock = InitialThisStock;
	}

	public void ExecuteApplyTransaction()
	{
		int num = ThisStock - InitialThisStock;
		if (num != 0 && _onApplyTransaction != null)
		{
			bool flag = num >= 0;
			int arg = (flag ? num : (-num));
			bool arg2 = (_isPlayerItem ? flag : (!flag));
			_onApplyTransaction(arg, arg2);
		}
	}

	public void ExecuteRemoveZeroCounts()
	{
		InventoryTradeVM.RemoveZeroCounts?.Invoke();
	}
}
