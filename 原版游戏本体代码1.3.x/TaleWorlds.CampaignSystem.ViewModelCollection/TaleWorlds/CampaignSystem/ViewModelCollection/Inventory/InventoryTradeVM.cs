using System;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Inventory
{
	// Token: 0x0200008E RID: 142
	public class InventoryTradeVM : ViewModel
	{
		// Token: 0x14000003 RID: 3
		// (add) Token: 0x06000C32 RID: 3122 RVA: 0x000320F8 File Offset: 0x000302F8
		// (remove) Token: 0x06000C33 RID: 3123 RVA: 0x0003212C File Offset: 0x0003032C
		public static event Action RemoveZeroCounts;

		// Token: 0x06000C34 RID: 3124 RVA: 0x00032160 File Offset: 0x00030360
		public InventoryTradeVM(InventoryLogic inventoryLogic, ItemRosterElement itemRoster, InventoryLogic.InventorySide side, Action<int, bool> onApplyTransaction)
		{
			this._inventoryLogic = inventoryLogic;
			this._referenceItemRoster = itemRoster;
			this._isPlayerItem = side == InventoryLogic.InventorySide.PlayerInventory;
			this._onApplyTransaction = onApplyTransaction;
			this.PieceLbl = this._pieceLblSingular;
			InventoryLogic inventoryLogic2 = this._inventoryLogic;
			this.IsTrading = inventoryLogic2 != null && inventoryLogic2.IsTrading;
			this.UpdateItemData(itemRoster, side, true);
			this.RefreshValues();
		}

		// Token: 0x06000C35 RID: 3125 RVA: 0x000321D4 File Offset: 0x000303D4
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ThisStockLbl = GameTexts.FindText("str_inventory_this_stock", null).ToString();
			this.OtherStockLbl = GameTexts.FindText("str_inventory_total_stock", null).ToString();
			this.AveragePriceLbl = GameTexts.FindText("str_inventory_average_price", null).ToString();
			this._pieceLblSingular = GameTexts.FindText("str_inventory_piece", null).ToString();
			this._pieceLblPlural = GameTexts.FindText("str_inventory_pieces", null).ToString();
			this.ApplyExchangeHint = new HintViewModel(GameTexts.FindText("str_party_apply_exchange", null), null);
		}

		// Token: 0x06000C36 RID: 3126 RVA: 0x0003226C File Offset: 0x0003046C
		public void UpdateItemData(ItemRosterElement itemRoster, InventoryLogic.InventorySide side, bool forceUpdate = true)
		{
			if (side != InventoryLogic.InventorySide.OtherInventory && side != InventoryLogic.InventorySide.PlayerInventory)
			{
				return;
			}
			ItemRosterElement? itemRosterElement = new ItemRosterElement?(itemRoster);
			ItemRosterElement? itemRosterElement2 = null;
			if (side == InventoryLogic.InventorySide.PlayerInventory)
			{
				itemRosterElement2 = this.FindItemFromSide(itemRoster.EquipmentElement, InventoryLogic.InventorySide.OtherInventory);
			}
			else if (side == InventoryLogic.InventorySide.OtherInventory)
			{
				itemRosterElement2 = this.FindItemFromSide(itemRoster.EquipmentElement, InventoryLogic.InventorySide.PlayerInventory);
			}
			if (forceUpdate)
			{
				this.InitialThisStock = ((itemRosterElement != null) ? itemRosterElement.GetValueOrDefault().Amount : 0);
				this.InitialOtherStock = ((itemRosterElement2 != null) ? itemRosterElement2.GetValueOrDefault().Amount : 0);
				this.TotalStock = this.InitialThisStock + this.InitialOtherStock;
				this.ThisStock = this.InitialThisStock;
				this.OtherStock = this.InitialOtherStock;
				this.ThisStockUpdated();
			}
		}

		// Token: 0x06000C37 RID: 3127 RVA: 0x0003232E File Offset: 0x0003052E
		private ItemRosterElement? FindItemFromSide(EquipmentElement item, InventoryLogic.InventorySide side)
		{
			return this._inventoryLogic.FindItemFromSide(side, item);
		}

		// Token: 0x06000C38 RID: 3128 RVA: 0x00032340 File Offset: 0x00030540
		private void ThisStockUpdated()
		{
			this.ExecuteApplyTransaction();
			this.OtherStock = this.TotalStock - this.ThisStock;
			this.IsThisStockIncreasable = this.OtherStock > 0;
			this.IsOtherStockIncreasable = this.OtherStock < this.TotalStock;
			this.UpdateProperties();
		}

		// Token: 0x06000C39 RID: 3129 RVA: 0x00032390 File Offset: 0x00030590
		private void UpdateProperties()
		{
			int num = this.ThisStock - this.InitialThisStock;
			bool flag = num >= 0;
			int num2 = (flag ? num : (-num));
			if (num2 == 0)
			{
				this.PieceChange = num2.ToString();
				this.PriceChange = "0";
				this.AveragePrice = "0";
				this.IsExchangeAvailable = false;
			}
			else
			{
				int lastPrice;
				int itemTotalPrice = this._inventoryLogic.GetItemTotalPrice(this._referenceItemRoster, num2, out lastPrice, flag);
				this.PieceChange = (flag ? "+" : "-") + num2;
				this.PriceChange = (flag ? "-" : "+") + itemTotalPrice * num2;
				this.AveragePrice = this.GetAveragePrice(itemTotalPrice, lastPrice, flag);
				this.IsExchangeAvailable = true;
			}
			this.PieceLbl = ((num2 <= 1) ? this._pieceLblSingular : this._pieceLblPlural);
		}

		// Token: 0x06000C3A RID: 3130 RVA: 0x00032474 File Offset: 0x00030674
		public string GetAveragePrice(int totalPrice, int lastPrice, bool isBuying)
		{
			InventoryLogic.InventorySide side = (isBuying ? InventoryLogic.InventorySide.OtherInventory : InventoryLogic.InventorySide.PlayerInventory);
			int costOfItemRosterElement = this._inventoryLogic.GetCostOfItemRosterElement(this._referenceItemRoster, side);
			if (costOfItemRosterElement == lastPrice)
			{
				return costOfItemRosterElement.ToString();
			}
			if (costOfItemRosterElement < lastPrice)
			{
				return costOfItemRosterElement + " - " + lastPrice;
			}
			return lastPrice + " - " + costOfItemRosterElement;
		}

		// Token: 0x06000C3B RID: 3131 RVA: 0x000324D9 File Offset: 0x000306D9
		public void ExecuteIncreaseThisStock()
		{
			if (this.ThisStock < this.TotalStock)
			{
				this.ThisStock++;
			}
		}

		// Token: 0x06000C3C RID: 3132 RVA: 0x000324F7 File Offset: 0x000306F7
		public void ExecuteIncreaseOtherStock()
		{
			if (this.ThisStock > 0)
			{
				this.ThisStock--;
			}
		}

		// Token: 0x06000C3D RID: 3133 RVA: 0x00032510 File Offset: 0x00030710
		public void ExecuteReset()
		{
			this.ThisStock = this.InitialThisStock;
		}

		// Token: 0x06000C3E RID: 3134 RVA: 0x00032520 File Offset: 0x00030720
		public void ExecuteApplyTransaction()
		{
			int num = this.ThisStock - this.InitialThisStock;
			if (num == 0 || this._onApplyTransaction == null)
			{
				return;
			}
			bool flag = num >= 0;
			int arg = (flag ? num : (-num));
			bool arg2 = (this._isPlayerItem ? flag : (!flag));
			this._onApplyTransaction(arg, arg2);
		}

		// Token: 0x06000C3F RID: 3135 RVA: 0x00032575 File Offset: 0x00030775
		public void ExecuteRemoveZeroCounts()
		{
			Action removeZeroCounts = InventoryTradeVM.RemoveZeroCounts;
			if (removeZeroCounts == null)
			{
				return;
			}
			removeZeroCounts();
		}

		// Token: 0x170003EA RID: 1002
		// (get) Token: 0x06000C40 RID: 3136 RVA: 0x00032586 File Offset: 0x00030786
		// (set) Token: 0x06000C41 RID: 3137 RVA: 0x0003258E File Offset: 0x0003078E
		[DataSourceProperty]
		public string ThisStockLbl
		{
			get
			{
				return this._thisStockLbl;
			}
			set
			{
				if (value != this._thisStockLbl)
				{
					this._thisStockLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "ThisStockLbl");
				}
			}
		}

		// Token: 0x170003EB RID: 1003
		// (get) Token: 0x06000C42 RID: 3138 RVA: 0x000325B1 File Offset: 0x000307B1
		// (set) Token: 0x06000C43 RID: 3139 RVA: 0x000325B9 File Offset: 0x000307B9
		[DataSourceProperty]
		public string OtherStockLbl
		{
			get
			{
				return this._otherStockLbl;
			}
			set
			{
				if (value != this._otherStockLbl)
				{
					this._otherStockLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "OtherStockLbl");
				}
			}
		}

		// Token: 0x170003EC RID: 1004
		// (get) Token: 0x06000C44 RID: 3140 RVA: 0x000325DC File Offset: 0x000307DC
		// (set) Token: 0x06000C45 RID: 3141 RVA: 0x000325E4 File Offset: 0x000307E4
		[DataSourceProperty]
		public string PieceLbl
		{
			get
			{
				return this._pieceLbl;
			}
			set
			{
				if (value != this._pieceLbl)
				{
					this._pieceLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "PieceLbl");
				}
			}
		}

		// Token: 0x170003ED RID: 1005
		// (get) Token: 0x06000C46 RID: 3142 RVA: 0x00032607 File Offset: 0x00030807
		// (set) Token: 0x06000C47 RID: 3143 RVA: 0x0003260F File Offset: 0x0003080F
		[DataSourceProperty]
		public string AveragePriceLbl
		{
			get
			{
				return this._averagePriceLbl;
			}
			set
			{
				if (value != this._averagePriceLbl)
				{
					this._averagePriceLbl = value;
					base.OnPropertyChangedWithValue<string>(value, "AveragePriceLbl");
				}
			}
		}

		// Token: 0x170003EE RID: 1006
		// (get) Token: 0x06000C48 RID: 3144 RVA: 0x00032632 File Offset: 0x00030832
		// (set) Token: 0x06000C49 RID: 3145 RVA: 0x0003263A File Offset: 0x0003083A
		[DataSourceProperty]
		public HintViewModel ApplyExchangeHint
		{
			get
			{
				return this._applyExchangeHint;
			}
			set
			{
				if (value != this._applyExchangeHint)
				{
					this._applyExchangeHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ApplyExchangeHint");
				}
			}
		}

		// Token: 0x170003EF RID: 1007
		// (get) Token: 0x06000C4A RID: 3146 RVA: 0x00032658 File Offset: 0x00030858
		// (set) Token: 0x06000C4B RID: 3147 RVA: 0x00032660 File Offset: 0x00030860
		[DataSourceProperty]
		public bool IsExchangeAvailable
		{
			get
			{
				return this._isExchangeAvailable;
			}
			set
			{
				if (value != this._isExchangeAvailable)
				{
					this._isExchangeAvailable = value;
					base.OnPropertyChangedWithValue(value, "IsExchangeAvailable");
				}
			}
		}

		// Token: 0x170003F0 RID: 1008
		// (get) Token: 0x06000C4C RID: 3148 RVA: 0x0003267E File Offset: 0x0003087E
		// (set) Token: 0x06000C4D RID: 3149 RVA: 0x00032686 File Offset: 0x00030886
		[DataSourceProperty]
		public string PriceChange
		{
			get
			{
				return this._priceChange;
			}
			set
			{
				if (value != this._priceChange)
				{
					this._priceChange = value;
					base.OnPropertyChangedWithValue<string>(value, "PriceChange");
				}
			}
		}

		// Token: 0x170003F1 RID: 1009
		// (get) Token: 0x06000C4E RID: 3150 RVA: 0x000326A9 File Offset: 0x000308A9
		// (set) Token: 0x06000C4F RID: 3151 RVA: 0x000326B1 File Offset: 0x000308B1
		[DataSourceProperty]
		public string PieceChange
		{
			get
			{
				return this._pieceChange;
			}
			set
			{
				if (value != this._pieceChange)
				{
					this._pieceChange = value;
					base.OnPropertyChangedWithValue<string>(value, "PieceChange");
				}
			}
		}

		// Token: 0x170003F2 RID: 1010
		// (get) Token: 0x06000C50 RID: 3152 RVA: 0x000326D4 File Offset: 0x000308D4
		// (set) Token: 0x06000C51 RID: 3153 RVA: 0x000326DC File Offset: 0x000308DC
		[DataSourceProperty]
		public string AveragePrice
		{
			get
			{
				return this._averagePrice;
			}
			set
			{
				if (value != this._averagePrice)
				{
					this._averagePrice = value;
					base.OnPropertyChangedWithValue<string>(value, "AveragePrice");
				}
			}
		}

		// Token: 0x170003F3 RID: 1011
		// (get) Token: 0x06000C52 RID: 3154 RVA: 0x000326FF File Offset: 0x000308FF
		// (set) Token: 0x06000C53 RID: 3155 RVA: 0x00032707 File Offset: 0x00030907
		[DataSourceProperty]
		public int ThisStock
		{
			get
			{
				return this._thisStock;
			}
			set
			{
				if (value != this._thisStock)
				{
					this._thisStock = value;
					base.OnPropertyChangedWithValue(value, "ThisStock");
					this.ThisStockUpdated();
				}
			}
		}

		// Token: 0x170003F4 RID: 1012
		// (get) Token: 0x06000C54 RID: 3156 RVA: 0x0003272B File Offset: 0x0003092B
		// (set) Token: 0x06000C55 RID: 3157 RVA: 0x00032733 File Offset: 0x00030933
		[DataSourceProperty]
		public int InitialThisStock
		{
			get
			{
				return this._initialThisStock;
			}
			set
			{
				if (value != this._initialThisStock)
				{
					this._initialThisStock = value;
					base.OnPropertyChangedWithValue(value, "InitialThisStock");
				}
			}
		}

		// Token: 0x170003F5 RID: 1013
		// (get) Token: 0x06000C56 RID: 3158 RVA: 0x00032751 File Offset: 0x00030951
		// (set) Token: 0x06000C57 RID: 3159 RVA: 0x00032759 File Offset: 0x00030959
		[DataSourceProperty]
		public int OtherStock
		{
			get
			{
				return this._otherStock;
			}
			set
			{
				if (value != this._otherStock)
				{
					this._otherStock = value;
					base.OnPropertyChangedWithValue(value, "OtherStock");
				}
			}
		}

		// Token: 0x170003F6 RID: 1014
		// (get) Token: 0x06000C58 RID: 3160 RVA: 0x00032777 File Offset: 0x00030977
		// (set) Token: 0x06000C59 RID: 3161 RVA: 0x0003277F File Offset: 0x0003097F
		[DataSourceProperty]
		public int InitialOtherStock
		{
			get
			{
				return this._initialOtherStock;
			}
			set
			{
				if (value != this._initialOtherStock)
				{
					this._initialOtherStock = value;
					base.OnPropertyChangedWithValue(value, "InitialOtherStock");
				}
			}
		}

		// Token: 0x170003F7 RID: 1015
		// (get) Token: 0x06000C5A RID: 3162 RVA: 0x0003279D File Offset: 0x0003099D
		// (set) Token: 0x06000C5B RID: 3163 RVA: 0x000327A5 File Offset: 0x000309A5
		[DataSourceProperty]
		public int TotalStock
		{
			get
			{
				return this._totalStock;
			}
			set
			{
				if (value != this._totalStock)
				{
					this._totalStock = value;
					base.OnPropertyChangedWithValue(value, "TotalStock");
				}
			}
		}

		// Token: 0x170003F8 RID: 1016
		// (get) Token: 0x06000C5C RID: 3164 RVA: 0x000327C3 File Offset: 0x000309C3
		// (set) Token: 0x06000C5D RID: 3165 RVA: 0x000327CB File Offset: 0x000309CB
		[DataSourceProperty]
		public bool IsThisStockIncreasable
		{
			get
			{
				return this._isThisStockIncreasable;
			}
			set
			{
				if (value != this._isThisStockIncreasable)
				{
					this._isThisStockIncreasable = value;
					base.OnPropertyChangedWithValue(value, "IsThisStockIncreasable");
				}
			}
		}

		// Token: 0x170003F9 RID: 1017
		// (get) Token: 0x06000C5E RID: 3166 RVA: 0x000327E9 File Offset: 0x000309E9
		// (set) Token: 0x06000C5F RID: 3167 RVA: 0x000327F1 File Offset: 0x000309F1
		[DataSourceProperty]
		public bool IsOtherStockIncreasable
		{
			get
			{
				return this._isOtherStockIncreasable;
			}
			set
			{
				if (value != this._isOtherStockIncreasable)
				{
					this._isOtherStockIncreasable = value;
					base.OnPropertyChangedWithValue(value, "IsOtherStockIncreasable");
				}
			}
		}

		// Token: 0x170003FA RID: 1018
		// (get) Token: 0x06000C60 RID: 3168 RVA: 0x0003280F File Offset: 0x00030A0F
		// (set) Token: 0x06000C61 RID: 3169 RVA: 0x00032817 File Offset: 0x00030A17
		[DataSourceProperty]
		public bool IsTrading
		{
			get
			{
				return this._isTrading;
			}
			set
			{
				if (value != this._isTrading)
				{
					this._isTrading = value;
					base.OnPropertyChangedWithValue(value, "IsTrading");
				}
			}
		}

		// Token: 0x170003FB RID: 1019
		// (get) Token: 0x06000C62 RID: 3170 RVA: 0x00032835 File Offset: 0x00030A35
		// (set) Token: 0x06000C63 RID: 3171 RVA: 0x0003283D File Offset: 0x00030A3D
		[DataSourceProperty]
		public bool IsTradeable
		{
			get
			{
				return this._isTradeable;
			}
			set
			{
				if (value != this._isTradeable)
				{
					this._isTradeable = value;
					base.OnPropertyChangedWithValue(value, "IsTradeable");
				}
			}
		}

		// Token: 0x04000570 RID: 1392
		private InventoryLogic _inventoryLogic;

		// Token: 0x04000571 RID: 1393
		private ItemRosterElement _referenceItemRoster;

		// Token: 0x04000572 RID: 1394
		private Action<int, bool> _onApplyTransaction;

		// Token: 0x04000573 RID: 1395
		private string _pieceLblSingular;

		// Token: 0x04000574 RID: 1396
		private string _pieceLblPlural;

		// Token: 0x04000575 RID: 1397
		private bool _isPlayerItem;

		// Token: 0x04000576 RID: 1398
		private string _thisStockLbl;

		// Token: 0x04000577 RID: 1399
		private string _otherStockLbl;

		// Token: 0x04000578 RID: 1400
		private string _averagePriceLbl;

		// Token: 0x04000579 RID: 1401
		private string _pieceLbl;

		// Token: 0x0400057A RID: 1402
		private HintViewModel _applyExchangeHint;

		// Token: 0x0400057B RID: 1403
		private bool _isExchangeAvailable;

		// Token: 0x0400057C RID: 1404
		private string _averagePrice;

		// Token: 0x0400057D RID: 1405
		private string _pieceChange;

		// Token: 0x0400057E RID: 1406
		private string _priceChange;

		// Token: 0x0400057F RID: 1407
		private int _thisStock = -1;

		// Token: 0x04000580 RID: 1408
		private int _initialThisStock;

		// Token: 0x04000581 RID: 1409
		private int _otherStock = -1;

		// Token: 0x04000582 RID: 1410
		private int _initialOtherStock;

		// Token: 0x04000583 RID: 1411
		private int _totalStock;

		// Token: 0x04000584 RID: 1412
		private bool _isThisStockIncreasable;

		// Token: 0x04000585 RID: 1413
		private bool _isOtherStockIncreasable;

		// Token: 0x04000586 RID: 1414
		private bool _isTrading;

		// Token: 0x04000587 RID: 1415
		private bool _isTradeable;
	}
}
