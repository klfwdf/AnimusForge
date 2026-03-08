using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x02000101 RID: 257
	public class CraftingPieceListVM : ViewModel
	{
		// Token: 0x0600173A RID: 5946 RVA: 0x00059417 File Offset: 0x00057617
		public CraftingPieceListVM(MBBindingList<CraftingPieceVM> pieceList, CraftingPiece.PieceTypes pieceType, Action<CraftingPiece.PieceTypes, bool> onSelect)
		{
			this.Pieces = pieceList;
			this.PieceType = pieceType;
			this._onSelect = onSelect;
		}

		// Token: 0x0600173B RID: 5947 RVA: 0x00059434 File Offset: 0x00057634
		public void ExecuteSelect()
		{
			Action<CraftingPiece.PieceTypes, bool> onSelect = this._onSelect;
			if (onSelect != null)
			{
				onSelect(this.PieceType, true);
			}
			this.HasNewlyUnlockedPieces = false;
		}

		// Token: 0x0600173C RID: 5948 RVA: 0x00059455 File Offset: 0x00057655
		public void Refresh()
		{
			this.HasNewlyUnlockedPieces = this.Pieces.Any((CraftingPieceVM x) => x.IsNewlyUnlocked);
		}

		// Token: 0x170007BC RID: 1980
		// (get) Token: 0x0600173D RID: 5949 RVA: 0x00059487 File Offset: 0x00057687
		// (set) Token: 0x0600173E RID: 5950 RVA: 0x0005948F File Offset: 0x0005768F
		[DataSourceProperty]
		public bool HasNewlyUnlockedPieces
		{
			get
			{
				return this._hasNewlyUnlockedPieces;
			}
			set
			{
				if (value != this._hasNewlyUnlockedPieces)
				{
					this._hasNewlyUnlockedPieces = value;
					base.OnPropertyChangedWithValue(value, "HasNewlyUnlockedPieces");
				}
			}
		}

		// Token: 0x170007BD RID: 1981
		// (get) Token: 0x0600173F RID: 5951 RVA: 0x000594AD File Offset: 0x000576AD
		// (set) Token: 0x06001740 RID: 5952 RVA: 0x000594B5 File Offset: 0x000576B5
		[DataSourceProperty]
		public MBBindingList<CraftingPieceVM> Pieces
		{
			get
			{
				return this._pieces;
			}
			set
			{
				if (value != this._pieces)
				{
					this._pieces = value;
					base.OnPropertyChangedWithValue<MBBindingList<CraftingPieceVM>>(value, "Pieces");
				}
			}
		}

		// Token: 0x170007BE RID: 1982
		// (get) Token: 0x06001741 RID: 5953 RVA: 0x000594D3 File Offset: 0x000576D3
		// (set) Token: 0x06001742 RID: 5954 RVA: 0x000594DB File Offset: 0x000576DB
		[DataSourceProperty]
		public bool IsSelected
		{
			get
			{
				return this._isSelected;
			}
			set
			{
				if (value != this._isSelected)
				{
					this._isSelected = value;
					base.OnPropertyChangedWithValue(value, "IsSelected");
				}
			}
		}

		// Token: 0x170007BF RID: 1983
		// (get) Token: 0x06001743 RID: 5955 RVA: 0x000594F9 File Offset: 0x000576F9
		// (set) Token: 0x06001744 RID: 5956 RVA: 0x00059501 File Offset: 0x00057701
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x170007C0 RID: 1984
		// (get) Token: 0x06001745 RID: 5957 RVA: 0x0005951F File Offset: 0x0005771F
		// (set) Token: 0x06001746 RID: 5958 RVA: 0x00059527 File Offset: 0x00057727
		[DataSourceProperty]
		public CraftingPieceVM SelectedPiece
		{
			get
			{
				return this._selectedPiece;
			}
			set
			{
				if (value != this._selectedPiece)
				{
					this._selectedPiece = value;
					base.OnPropertyChangedWithValue<CraftingPieceVM>(value, "SelectedPiece");
				}
			}
		}

		// Token: 0x04000AA0 RID: 2720
		public CraftingPiece.PieceTypes PieceType;

		// Token: 0x04000AA1 RID: 2721
		private Action<CraftingPiece.PieceTypes, bool> _onSelect;

		// Token: 0x04000AA2 RID: 2722
		private bool _hasNewlyUnlockedPieces;

		// Token: 0x04000AA3 RID: 2723
		private MBBindingList<CraftingPieceVM> _pieces;

		// Token: 0x04000AA4 RID: 2724
		private bool _isSelected;

		// Token: 0x04000AA5 RID: 2725
		private bool _isEnabled;

		// Token: 0x04000AA6 RID: 2726
		private CraftingPieceVM _selectedPiece;
	}
}
