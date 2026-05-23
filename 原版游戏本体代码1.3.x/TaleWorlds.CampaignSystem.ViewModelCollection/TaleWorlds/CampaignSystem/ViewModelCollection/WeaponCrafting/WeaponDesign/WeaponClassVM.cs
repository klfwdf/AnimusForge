using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x02000109 RID: 265
	public class WeaponClassVM : ViewModel
	{
		// Token: 0x170007D8 RID: 2008
		// (get) Token: 0x06001785 RID: 6021 RVA: 0x00059D9E File Offset: 0x00057F9E
		// (set) Token: 0x06001786 RID: 6022 RVA: 0x00059DA6 File Offset: 0x00057FA6
		public int NewlyUnlockedPieceCount { get; set; }

		// Token: 0x170007D9 RID: 2009
		// (get) Token: 0x06001787 RID: 6023 RVA: 0x00059DAF File Offset: 0x00057FAF
		public CraftingTemplate Template { get; }

		// Token: 0x06001788 RID: 6024 RVA: 0x00059DB8 File Offset: 0x00057FB8
		public WeaponClassVM(int selectionIndex, CraftingTemplate template, Action<int> onSelect)
		{
			this._onSelect = onSelect;
			this.SelectionIndex = selectionIndex;
			this.Template = template;
			this._selectedPieces = new Dictionary<CraftingPiece.PieceTypes, string>
			{
				{
					CraftingPiece.PieceTypes.Blade,
					null
				},
				{
					CraftingPiece.PieceTypes.Guard,
					null
				},
				{
					CraftingPiece.PieceTypes.Handle,
					null
				},
				{
					CraftingPiece.PieceTypes.Pommel,
					null
				}
			};
			this.RefreshValues();
		}

		// Token: 0x06001789 RID: 6025 RVA: 0x00059E14 File Offset: 0x00058014
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.TemplateName = this.Template.TemplateName.ToString();
			this.UnlockedPiecesLabelText = new TextObject("{=OGbskMfz}Unlocked Parts:", null).ToString();
			this.WeaponType = this.Template.StringId;
		}

		// Token: 0x0600178A RID: 6026 RVA: 0x00059E64 File Offset: 0x00058064
		public void RegisterSelectedPiece(CraftingPiece.PieceTypes type, string pieceID)
		{
			string a;
			if (this._selectedPieces.TryGetValue(type, out a) && a != pieceID)
			{
				this._selectedPieces[type] = pieceID;
			}
		}

		// Token: 0x0600178B RID: 6027 RVA: 0x00059E98 File Offset: 0x00058098
		public string GetSelectedPieceData(CraftingPiece.PieceTypes type)
		{
			string result;
			if (this._selectedPieces.TryGetValue(type, out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x0600178C RID: 6028 RVA: 0x00059EB8 File Offset: 0x000580B8
		public void ExecuteSelect()
		{
			Action<int> onSelect = this._onSelect;
			if (onSelect == null)
			{
				return;
			}
			onSelect(this.SelectionIndex);
		}

		// Token: 0x170007DA RID: 2010
		// (get) Token: 0x0600178D RID: 6029 RVA: 0x00059ED0 File Offset: 0x000580D0
		// (set) Token: 0x0600178E RID: 6030 RVA: 0x00059ED8 File Offset: 0x000580D8
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

		// Token: 0x170007DB RID: 2011
		// (get) Token: 0x0600178F RID: 6031 RVA: 0x00059EF6 File Offset: 0x000580F6
		// (set) Token: 0x06001790 RID: 6032 RVA: 0x00059EFE File Offset: 0x000580FE
		[DataSourceProperty]
		public string UnlockedPiecesLabelText
		{
			get
			{
				return this._unlockedPiecesLabelText;
			}
			set
			{
				if (value != this._unlockedPiecesLabelText)
				{
					this._unlockedPiecesLabelText = value;
					base.OnPropertyChangedWithValue<string>(value, "UnlockedPiecesLabelText");
				}
			}
		}

		// Token: 0x170007DC RID: 2012
		// (get) Token: 0x06001791 RID: 6033 RVA: 0x00059F21 File Offset: 0x00058121
		// (set) Token: 0x06001792 RID: 6034 RVA: 0x00059F29 File Offset: 0x00058129
		[DataSourceProperty]
		public int UnlockedPiecesCount
		{
			get
			{
				return this._unlockedPiecesCount;
			}
			set
			{
				if (value != this._unlockedPiecesCount)
				{
					this._unlockedPiecesCount = value;
					base.OnPropertyChangedWithValue(value, "UnlockedPiecesCount");
				}
			}
		}

		// Token: 0x170007DD RID: 2013
		// (get) Token: 0x06001793 RID: 6035 RVA: 0x00059F47 File Offset: 0x00058147
		// (set) Token: 0x06001794 RID: 6036 RVA: 0x00059F4F File Offset: 0x0005814F
		[DataSourceProperty]
		public string TemplateName
		{
			get
			{
				return this._templateName;
			}
			set
			{
				if (value != this._templateName)
				{
					this._templateName = value;
					base.OnPropertyChangedWithValue<string>(value, "TemplateName");
				}
			}
		}

		// Token: 0x170007DE RID: 2014
		// (get) Token: 0x06001795 RID: 6037 RVA: 0x00059F72 File Offset: 0x00058172
		// (set) Token: 0x06001796 RID: 6038 RVA: 0x00059F7A File Offset: 0x0005817A
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

		// Token: 0x170007DF RID: 2015
		// (get) Token: 0x06001797 RID: 6039 RVA: 0x00059F98 File Offset: 0x00058198
		// (set) Token: 0x06001798 RID: 6040 RVA: 0x00059FA0 File Offset: 0x000581A0
		[DataSourceProperty]
		public int SelectionIndex
		{
			get
			{
				return this._selectionIndex;
			}
			set
			{
				if (value != this._selectionIndex)
				{
					this._selectionIndex = value;
					base.OnPropertyChangedWithValue(value, "SelectionIndex");
				}
			}
		}

		// Token: 0x170007E0 RID: 2016
		// (get) Token: 0x06001799 RID: 6041 RVA: 0x00059FBE File Offset: 0x000581BE
		// (set) Token: 0x0600179A RID: 6042 RVA: 0x00059FC6 File Offset: 0x000581C6
		[DataSourceProperty]
		public string WeaponType
		{
			get
			{
				return this._weaponType;
			}
			set
			{
				if (value != this._weaponType)
				{
					this._weaponType = value;
					base.OnPropertyChangedWithValue<string>(value, "WeaponType");
				}
			}
		}

		// Token: 0x04000AC8 RID: 2760
		private Action<int> _onSelect;

		// Token: 0x04000AC9 RID: 2761
		private Dictionary<CraftingPiece.PieceTypes, string> _selectedPieces;

		// Token: 0x04000ACA RID: 2762
		private bool _hasNewlyUnlockedPieces;

		// Token: 0x04000ACB RID: 2763
		private string _unlockedPiecesLabelText;

		// Token: 0x04000ACC RID: 2764
		private int _unlockedPiecesCount;

		// Token: 0x04000ACD RID: 2765
		private string _templateName;

		// Token: 0x04000ACE RID: 2766
		private bool _isSelected;

		// Token: 0x04000ACF RID: 2767
		private int _selectionIndex;

		// Token: 0x04000AD0 RID: 2768
		private string _weaponType;
	}
}
