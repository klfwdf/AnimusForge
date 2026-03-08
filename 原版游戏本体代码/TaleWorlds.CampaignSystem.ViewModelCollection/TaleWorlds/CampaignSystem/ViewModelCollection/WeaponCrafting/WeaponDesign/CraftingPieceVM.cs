using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x02000102 RID: 258
	public class CraftingPieceVM : ViewModel
	{
		// Token: 0x06001747 RID: 5959 RVA: 0x00059545 File Offset: 0x00057745
		public CraftingPieceVM()
		{
			this.ImageIdentifier = new CraftingPieceImageIdentifierVM(null, string.Empty);
		}

		// Token: 0x06001748 RID: 5960 RVA: 0x00059568 File Offset: 0x00057768
		public CraftingPieceVM(Action<CraftingPieceVM> selectWeaponPart, string templateId, WeaponDesignElement usableCraftingPiece, int pieceType, int index, bool isOpened)
		{
			this._selectWeaponPiece = selectWeaponPart;
			this.CraftingPiece = usableCraftingPiece;
			this.Tier = usableCraftingPiece.CraftingPiece.PieceTier;
			this.TierText = Common.ToRoman(this.Tier);
			this.ImageIdentifier = new CraftingPieceImageIdentifierVM(usableCraftingPiece.CraftingPiece, templateId);
			this.PieceType = pieceType;
			this.Index = index;
			this.PlayerHasPiece = isOpened;
			this.ItemAttributeIcons = new MBBindingList<CraftingItemFlagVM>();
			this.IsEmpty = string.IsNullOrEmpty(this.CraftingPiece.CraftingPiece.MeshName);
			this.RefreshFlagIcons();
		}

		// Token: 0x06001749 RID: 5961 RVA: 0x00059608 File Offset: 0x00057808
		public void RefreshFlagIcons()
		{
			this.ItemAttributeIcons.Clear();
			foreach (Tuple<string, TextObject> tuple in CampaignUIHelper.GetItemFlagDetails(this.CraftingPiece.CraftingPiece.AdditionalItemFlags))
			{
				this.ItemAttributeIcons.Add(new CraftingItemFlagVM(tuple.Item1, tuple.Item2, true));
			}
			foreach (ValueTuple<string, TextObject> valueTuple in CampaignUIHelper.GetWeaponFlagDetails(this.CraftingPiece.CraftingPiece.AdditionalWeaponFlags, null))
			{
				this.ItemAttributeIcons.Add(new CraftingItemFlagVM(valueTuple.Item1, valueTuple.Item2, true));
			}
		}

		// Token: 0x0600174A RID: 5962 RVA: 0x000596F4 File Offset: 0x000578F4
		public void ExecuteOpenTooltip()
		{
			InformationManager.ShowTooltip(typeof(WeaponDesignElement), new object[] { this.CraftingPiece });
		}

		// Token: 0x0600174B RID: 5963 RVA: 0x00059714 File Offset: 0x00057914
		public void ExecuteCloseTooltip()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x0600174C RID: 5964 RVA: 0x0005971B File Offset: 0x0005791B
		public void ExecuteSelect()
		{
			this._selectWeaponPiece(this);
		}

		// Token: 0x170007C1 RID: 1985
		// (get) Token: 0x0600174D RID: 5965 RVA: 0x00059729 File Offset: 0x00057929
		// (set) Token: 0x0600174E RID: 5966 RVA: 0x00059731 File Offset: 0x00057931
		[DataSourceProperty]
		public bool IsFilteredOut
		{
			get
			{
				return this._isFilteredOut;
			}
			set
			{
				if (value != this._isFilteredOut)
				{
					this._isFilteredOut = value;
					base.OnPropertyChangedWithValue(value, "IsFilteredOut");
				}
			}
		}

		// Token: 0x170007C2 RID: 1986
		// (get) Token: 0x0600174F RID: 5967 RVA: 0x0005974F File Offset: 0x0005794F
		// (set) Token: 0x06001750 RID: 5968 RVA: 0x00059757 File Offset: 0x00057957
		[DataSourceProperty]
		public MBBindingList<CraftingItemFlagVM> ItemAttributeIcons
		{
			get
			{
				return this._itemAttributeIcons;
			}
			set
			{
				if (value != this._itemAttributeIcons)
				{
					this._itemAttributeIcons = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingItemFlagVM>>(value, "ItemAttributeIcons");
				}
			}
		}

		// Token: 0x170007C3 RID: 1987
		// (get) Token: 0x06001751 RID: 5969 RVA: 0x00059775 File Offset: 0x00057975
		// (set) Token: 0x06001752 RID: 5970 RVA: 0x0005977D File Offset: 0x0005797D
		[DataSourceProperty]
		public bool PlayerHasPiece
		{
			get
			{
				return this._playerHasPiece;
			}
			set
			{
				if (this._playerHasPiece != value)
				{
					this._playerHasPiece = value;
					base.OnPropertyChangedWithValue(value, "PlayerHasPiece");
				}
			}
		}

		// Token: 0x170007C4 RID: 1988
		// (get) Token: 0x06001753 RID: 5971 RVA: 0x0005979B File Offset: 0x0005799B
		// (set) Token: 0x06001754 RID: 5972 RVA: 0x000597A3 File Offset: 0x000579A3
		[DataSourceProperty]
		public bool IsEmpty
		{
			get
			{
				return this._isEmpty;
			}
			set
			{
				if (this._isEmpty != value)
				{
					this._isEmpty = value;
					base.OnPropertyChangedWithValue(value, "IsEmpty");
				}
			}
		}

		// Token: 0x170007C5 RID: 1989
		// (get) Token: 0x06001755 RID: 5973 RVA: 0x000597C1 File Offset: 0x000579C1
		// (set) Token: 0x06001756 RID: 5974 RVA: 0x000597C9 File Offset: 0x000579C9
		[DataSourceProperty]
		public string TierText
		{
			get
			{
				return this._tierText;
			}
			set
			{
				if (this._tierText != value)
				{
					this._tierText = value;
					base.OnPropertyChangedWithValue<string>(value, "TierText");
				}
			}
		}

		// Token: 0x170007C6 RID: 1990
		// (get) Token: 0x06001757 RID: 5975 RVA: 0x000597EC File Offset: 0x000579EC
		// (set) Token: 0x06001758 RID: 5976 RVA: 0x000597F4 File Offset: 0x000579F4
		[DataSourceProperty]
		public int Tier
		{
			get
			{
				return this._tier;
			}
			set
			{
				if (this._tier != value)
				{
					this._tier = value;
					base.OnPropertyChangedWithValue(value, "Tier");
				}
			}
		}

		// Token: 0x170007C7 RID: 1991
		// (get) Token: 0x06001759 RID: 5977 RVA: 0x00059812 File Offset: 0x00057A12
		// (set) Token: 0x0600175A RID: 5978 RVA: 0x0005981A File Offset: 0x00057A1A
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (this._isSelected != value)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x170007C8 RID: 1992
		// (get) Token: 0x0600175B RID: 5979 RVA: 0x00059838 File Offset: 0x00057A38
		// (set) Token: 0x0600175C RID: 5980 RVA: 0x00059840 File Offset: 0x00057A40
		[DataSourceProperty]
		public CraftingPieceImageIdentifierVM ImageIdentifier
		{
			get
			{
				return this._imageIdentifier;
			}
			set
			{
				if (this._imageIdentifier != value)
				{
					this._imageIdentifier = value;
					base.OnPropertyChangedWithValue<CraftingPieceImageIdentifierVM>(value, "ImageIdentifier");
				}
			}
		}

		// Token: 0x170007C9 RID: 1993
		// (get) Token: 0x0600175D RID: 5981 RVA: 0x0005985E File Offset: 0x00057A5E
		// (set) Token: 0x0600175E RID: 5982 RVA: 0x00059866 File Offset: 0x00057A66
		[DataSourceProperty]
		public int PieceType
		{
			get
			{
				return this._pieceType;
			}
			set
			{
				if (this._pieceType != value)
				{
					this._pieceType = value;
					base.OnPropertyChangedWithValue(value, "PieceType");
				}
			}
		}

		// Token: 0x170007CA RID: 1994
		// (get) Token: 0x0600175F RID: 5983 RVA: 0x00059884 File Offset: 0x00057A84
		// (set) Token: 0x06001760 RID: 5984 RVA: 0x0005988C File Offset: 0x00057A8C
		[DataSourceProperty]
		public bool IsNewlyUnlocked
		{
			get
			{
				return this._isNewlyUnlocked;
			}
			set
			{
				if (value != this._isNewlyUnlocked)
				{
					this._isNewlyUnlocked = value;
					base.OnPropertyChangedWithValue(value, "IsNewlyUnlocked");
				}
			}
		}

		// Token: 0x04000AA7 RID: 2727
		public WeaponDesignElement CraftingPiece;

		// Token: 0x04000AA8 RID: 2728
		public int Index;

		// Token: 0x04000AA9 RID: 2729
		private readonly Action<CraftingPieceVM> _selectWeaponPiece;

		// Token: 0x04000AAA RID: 2730
		private bool _isFilteredOut;

		// Token: 0x04000AAB RID: 2731
		public CraftingPieceImageIdentifierVM _imageIdentifier;

		// Token: 0x04000AAC RID: 2732
		public int _pieceType = -1;

		// Token: 0x04000AAD RID: 2733
		public int _tier;

		// Token: 0x04000AAE RID: 2734
		public bool _isSelected;

		// Token: 0x04000AAF RID: 2735
		public bool _playerHasPiece;

		// Token: 0x04000AB0 RID: 2736
		private bool _isEmpty;

		// Token: 0x04000AB1 RID: 2737
		public string _tierText;

		// Token: 0x04000AB2 RID: 2738
		private MBBindingList<CraftingItemFlagVM> _itemAttributeIcons;

		// Token: 0x04000AB3 RID: 2739
		private bool _isNewlyUnlocked;
	}
}
