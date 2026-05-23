using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Barter
{
	// Token: 0x02000159 RID: 345
	public class BarterItemVM : EncyclopediaLinkVM
	{
		// Token: 0x06002055 RID: 8277 RVA: 0x00075A6C File Offset: 0x00073C6C
		public BarterItemVM(Barterable barterable, BarterItemVM.BarterTransferEventDelegate OnTransfer, Action onAmountChange, bool isFixed = false)
		{
			this.Barterable = barterable;
			base.ActiveLink = barterable.GetEncyclopediaLink();
			this._onTransfer = OnTransfer;
			this._onAmountChange = onAmountChange;
			this._isFixed = isFixed;
			this.IsItemTransferrable = !isFixed;
			this.BarterableType = this.Barterable.StringID;
			ImageIdentifier visualIdentifier = this.Barterable.GetVisualIdentifier();
			this.HasVisualIdentifier = visualIdentifier != null;
			if (visualIdentifier != null)
			{
				this.VisualIdentifier = new GenericImageIdentifierVM(visualIdentifier);
			}
			else
			{
				this.VisualIdentifier = null;
				FiefBarterable fiefBarterable;
				if ((fiefBarterable = this.Barterable as FiefBarterable) != null)
				{
					this.FiefFileName = fiefBarterable.TargetSettlement.SettlementComponent.BackgroundMeshName;
				}
			}
			this.TotalItemCount = this.Barterable.MaxAmount;
			this.CurrentOfferedAmount = 1;
			this.IsMultiple = this.TotalItemCount > 1;
			this.IsOffered = this.Barterable.IsOffered;
			this.RefreshValues();
		}

		// Token: 0x06002056 RID: 8278 RVA: 0x00075B72 File Offset: 0x00073D72
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.ItemLbl = this.Barterable.Name.ToString();
		}

		// Token: 0x06002057 RID: 8279 RVA: 0x00075B90 File Offset: 0x00073D90
		public void RefreshCompabilityWithItem(BarterItemVM item, bool isItemGotOffered)
		{
			if (isItemGotOffered && !item.Barterable.IsCompatible(this.Barterable))
			{
				this._incompatibleItems.Add(item.Barterable);
			}
			else if (!isItemGotOffered && this._incompatibleItems.Contains(item.Barterable))
			{
				this._incompatibleItems.Remove(item.Barterable);
			}
			this.IsItemTransferrable = this._incompatibleItems.Count <= 0;
		}

		// Token: 0x06002058 RID: 8280 RVA: 0x00075C08 File Offset: 0x00073E08
		public void ExecuteAddOffered()
		{
			int num = (BarterItemVM.IsEntireStackModifierActive ? this.TotalItemCount : (this.CurrentOfferedAmount + (BarterItemVM.IsFiveStackModifierActive ? 5 : 1)));
			this.CurrentOfferedAmount = ((num < this.TotalItemCount) ? num : this.TotalItemCount);
		}

		// Token: 0x06002059 RID: 8281 RVA: 0x00075C50 File Offset: 0x00073E50
		public void ExecuteRemoveOffered()
		{
			int num = (BarterItemVM.IsEntireStackModifierActive ? 1 : (this.CurrentOfferedAmount - (BarterItemVM.IsFiveStackModifierActive ? 5 : 1)));
			this.CurrentOfferedAmount = ((num > 1) ? num : 1);
		}

		// Token: 0x0600205A RID: 8282 RVA: 0x00075C88 File Offset: 0x00073E88
		public void ExecuteAction()
		{
			if (this.IsItemTransferrable)
			{
				this._onTransfer(this, false);
			}
		}

		// Token: 0x17000B01 RID: 2817
		// (get) Token: 0x0600205B RID: 8283 RVA: 0x00075C9F File Offset: 0x00073E9F
		// (set) Token: 0x0600205C RID: 8284 RVA: 0x00075CA7 File Offset: 0x00073EA7
		[DataSourceProperty]
		public int TotalItemCount
		{
			get
			{
				return this._totalItemCount;
			}
			set
			{
				if (this._totalItemCount != value)
				{
					this._totalItemCount = value;
					base.OnPropertyChangedWithValue(value, "TotalItemCount");
					this.TotalItemCountText = CampaignUIHelper.GetAbbreviatedValueTextFromValue(value);
				}
			}
		}

		// Token: 0x17000B02 RID: 2818
		// (get) Token: 0x0600205D RID: 8285 RVA: 0x00075CD1 File Offset: 0x00073ED1
		// (set) Token: 0x0600205E RID: 8286 RVA: 0x00075CD9 File Offset: 0x00073ED9
		[DataSourceProperty]
		public string TotalItemCountText
		{
			get
			{
				return this._totalItemCountText;
			}
			set
			{
				if (this._totalItemCountText != value)
				{
					this._totalItemCountText = value;
					base.OnPropertyChangedWithValue<string>(value, "TotalItemCountText");
				}
			}
		}

		// Token: 0x17000B03 RID: 2819
		// (get) Token: 0x0600205F RID: 8287 RVA: 0x00075CFC File Offset: 0x00073EFC
		// (set) Token: 0x06002060 RID: 8288 RVA: 0x00075D04 File Offset: 0x00073F04
		[DataSourceProperty]
		public int CurrentOfferedAmount
		{
			get
			{
				return this._currentOfferedAmount;
			}
			set
			{
				if (this._currentOfferedAmount != value)
				{
					this.Barterable.CurrentAmount = value;
					Action onAmountChange = this._onAmountChange;
					if (onAmountChange != null)
					{
						onAmountChange();
					}
					this._currentOfferedAmount = value;
					base.OnPropertyChangedWithValue(value, "CurrentOfferedAmount");
					this.CurrentOfferedAmountText = CampaignUIHelper.GetAbbreviatedValueTextFromValue(value);
				}
			}
		}

		// Token: 0x17000B04 RID: 2820
		// (get) Token: 0x06002061 RID: 8289 RVA: 0x00075D56 File Offset: 0x00073F56
		// (set) Token: 0x06002062 RID: 8290 RVA: 0x00075D5E File Offset: 0x00073F5E
		[DataSourceProperty]
		public string CurrentOfferedAmountText
		{
			get
			{
				return this._currentOfferedAmountText;
			}
			set
			{
				if (this._currentOfferedAmountText != value)
				{
					this._currentOfferedAmountText = value;
					base.OnPropertyChangedWithValue<string>(value, "CurrentOfferedAmountText");
				}
			}
		}

		// Token: 0x17000B05 RID: 2821
		// (get) Token: 0x06002063 RID: 8291 RVA: 0x00075D81 File Offset: 0x00073F81
		// (set) Token: 0x06002064 RID: 8292 RVA: 0x00075D89 File Offset: 0x00073F89
		[DataSourceProperty]
		public string BarterableType
		{
			get
			{
				return this._barterableType;
			}
			set
			{
				if (this._barterableType != value)
				{
					this._barterableType = value;
					base.OnPropertyChangedWithValue<string>(value, "BarterableType");
				}
			}
		}

		// Token: 0x17000B06 RID: 2822
		// (get) Token: 0x06002065 RID: 8293 RVA: 0x00075DAC File Offset: 0x00073FAC
		// (set) Token: 0x06002066 RID: 8294 RVA: 0x00075DB4 File Offset: 0x00073FB4
		[DataSourceProperty]
		public bool HasVisualIdentifier
		{
			get
			{
				return this._hasVisualIdentifier;
			}
			set
			{
				if (this._hasVisualIdentifier != value)
				{
					this._hasVisualIdentifier = value;
					base.OnPropertyChangedWithValue(value, "HasVisualIdentifier");
				}
			}
		}

		// Token: 0x17000B07 RID: 2823
		// (get) Token: 0x06002067 RID: 8295 RVA: 0x00075DD2 File Offset: 0x00073FD2
		// (set) Token: 0x06002068 RID: 8296 RVA: 0x00075DDA File Offset: 0x00073FDA
		[DataSourceProperty]
		public bool IsMultiple
		{
			get
			{
				return this._isMultiple;
			}
			set
			{
				if (this._isMultiple != value)
				{
					this._isMultiple = value;
					base.OnPropertyChangedWithValue(value, "IsMultiple");
				}
			}
		}

		// Token: 0x17000B08 RID: 2824
		// (get) Token: 0x06002069 RID: 8297 RVA: 0x00075DF8 File Offset: 0x00073FF8
		// (set) Token: 0x0600206A RID: 8298 RVA: 0x00075E00 File Offset: 0x00074000
		[DataSourceProperty]
		public bool IsSelectorActive
		{
			get
			{
				return this._isSelectorActive;
			}
			set
			{
				if (this._isSelectorActive != value)
				{
					this._isSelectorActive = value;
					base.OnPropertyChangedWithValue(value, "IsSelectorActive");
				}
			}
		}

		// Token: 0x17000B09 RID: 2825
		// (get) Token: 0x0600206B RID: 8299 RVA: 0x00075E1E File Offset: 0x0007401E
		// (set) Token: 0x0600206C RID: 8300 RVA: 0x00075E26 File Offset: 0x00074026
		[DataSourceProperty]
		public ImageIdentifierVM VisualIdentifier
		{
			get
			{
				return this._visualIdentifier;
			}
			set
			{
				if (this._visualIdentifier != value)
				{
					this._visualIdentifier = value;
					base.OnPropertyChangedWithValue<ImageIdentifierVM>(value, "VisualIdentifier");
				}
			}
		}

		// Token: 0x17000B0A RID: 2826
		// (get) Token: 0x0600206D RID: 8301 RVA: 0x00075E44 File Offset: 0x00074044
		// (set) Token: 0x0600206E RID: 8302 RVA: 0x00075E4C File Offset: 0x0007404C
		[DataSourceProperty]
		public string ItemLbl
		{
			get
			{
				return this._itemLbl;
			}
			set
			{
				this._itemLbl = value;
				base.OnPropertyChangedWithValue<string>(value, "ItemLbl");
			}
		}

		// Token: 0x17000B0B RID: 2827
		// (get) Token: 0x0600206F RID: 8303 RVA: 0x00075E61 File Offset: 0x00074061
		// (set) Token: 0x06002070 RID: 8304 RVA: 0x00075E69 File Offset: 0x00074069
		[DataSourceProperty]
		public string FiefFileName
		{
			get
			{
				return this._fiefFileName;
			}
			set
			{
				this._fiefFileName = value;
				base.OnPropertyChangedWithValue<string>(value, "FiefFileName");
			}
		}

		// Token: 0x17000B0C RID: 2828
		// (get) Token: 0x06002071 RID: 8305 RVA: 0x00075E7E File Offset: 0x0007407E
		// (set) Token: 0x06002072 RID: 8306 RVA: 0x00075E86 File Offset: 0x00074086
		[DataSourceProperty]
		public bool IsItemTransferrable
		{
			get
			{
				return this._isItemTransferrable;
			}
			set
			{
				if (this._isFixed)
				{
					value = false;
				}
				if (this._isItemTransferrable != value)
				{
					this._isItemTransferrable = value;
					base.OnPropertyChangedWithValue(value, "IsItemTransferrable");
				}
			}
		}

		// Token: 0x17000B0D RID: 2829
		// (get) Token: 0x06002073 RID: 8307 RVA: 0x00075EAF File Offset: 0x000740AF
		// (set) Token: 0x06002074 RID: 8308 RVA: 0x00075EB7 File Offset: 0x000740B7
		[DataSourceProperty]
		public bool IsOffered
		{
			get
			{
				return this._isOffered;
			}
			set
			{
				if (value != this._isOffered)
				{
					this._isOffered = value;
					base.OnPropertyChangedWithValue(value, "IsOffered");
				}
			}
		}

		// Token: 0x04000F08 RID: 3848
		public static bool IsEntireStackModifierActive;

		// Token: 0x04000F09 RID: 3849
		public static bool IsFiveStackModifierActive;

		// Token: 0x04000F0A RID: 3850
		private readonly BarterItemVM.BarterTransferEventDelegate _onTransfer;

		// Token: 0x04000F0B RID: 3851
		private readonly Action _onAmountChange;

		// Token: 0x04000F0C RID: 3852
		private bool _isFixed;

		// Token: 0x04000F0D RID: 3853
		private List<Barterable> _incompatibleItems = new List<Barterable>();

		// Token: 0x04000F0E RID: 3854
		public Barterable Barterable;

		// Token: 0x04000F0F RID: 3855
		public bool _isOffered;

		// Token: 0x04000F10 RID: 3856
		private bool _isItemTransferrable = true;

		// Token: 0x04000F11 RID: 3857
		private string _itemLbl;

		// Token: 0x04000F12 RID: 3858
		private string _fiefFileName;

		// Token: 0x04000F13 RID: 3859
		private string _barterableType = "NULL";

		// Token: 0x04000F14 RID: 3860
		private string _currentOfferedAmountText;

		// Token: 0x04000F15 RID: 3861
		private ImageIdentifierVM _visualIdentifier;

		// Token: 0x04000F16 RID: 3862
		private bool _isSelectorActive;

		// Token: 0x04000F17 RID: 3863
		private bool _hasVisualIdentifier;

		// Token: 0x04000F18 RID: 3864
		private bool _isMultiple;

		// Token: 0x04000F19 RID: 3865
		private int _totalItemCount;

		// Token: 0x04000F1A RID: 3866
		private string _totalItemCountText;

		// Token: 0x04000F1B RID: 3867
		private int _currentOfferedAmount;

		// Token: 0x020002E2 RID: 738
		// (Invoke) Token: 0x060026F8 RID: 9976
		public delegate void BarterTransferEventDelegate(BarterItemVM itemVM, bool transferAll);
	}
}
