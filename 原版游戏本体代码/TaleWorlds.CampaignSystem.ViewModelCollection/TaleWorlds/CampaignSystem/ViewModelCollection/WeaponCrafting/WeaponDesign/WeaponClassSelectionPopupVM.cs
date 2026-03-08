using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x02000105 RID: 261
	public class WeaponClassSelectionPopupVM : ViewModel
	{
		// Token: 0x0600176E RID: 5998 RVA: 0x00059A10 File Offset: 0x00057C10
		public WeaponClassSelectionPopupVM(ICraftingCampaignBehavior craftingBehavior, List<CraftingTemplate> templatesList, Action<int> onSelect, Func<CraftingTemplate, int> getUnlockedPiecesCount)
		{
			this.WeaponClasses = new MBBindingList<WeaponClassVM>();
			this._craftingBehavior = craftingBehavior;
			this._onSelect = onSelect;
			this._templatesList = templatesList;
			this._getUnlockedPiecesCount = getUnlockedPiecesCount;
			foreach (CraftingTemplate craftingTemplate in this._templatesList)
			{
				this.WeaponClasses.Add(new WeaponClassVM(this._templatesList.IndexOf(craftingTemplate), craftingTemplate, new Action<int>(this.ExecuteSelectWeaponClass)));
			}
			this.RefreshList();
			this.RefreshValues();
		}

		// Token: 0x0600176F RID: 5999 RVA: 0x00059AC0 File Offset: 0x00057CC0
		private void RefreshList()
		{
			foreach (WeaponClassVM weaponClassVM in this.WeaponClasses)
			{
				WeaponClassVM weaponClassVM2 = weaponClassVM;
				Func<CraftingTemplate, int> getUnlockedPiecesCount = this._getUnlockedPiecesCount;
				weaponClassVM2.UnlockedPiecesCount = ((getUnlockedPiecesCount != null) ? getUnlockedPiecesCount(weaponClassVM.Template) : 0);
				weaponClassVM.HasNewlyUnlockedPieces = weaponClassVM.NewlyUnlockedPieceCount > 0;
			}
		}

		// Token: 0x06001770 RID: 6000 RVA: 0x00059B34 File Offset: 0x00057D34
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.PopupHeader = new TextObject("{=wZGj3qO1}Choose What to Craft", null).ToString();
		}

		// Token: 0x06001771 RID: 6001 RVA: 0x00059B54 File Offset: 0x00057D54
		public void UpdateNewlyUnlockedPiecesCount(List<CraftingPiece> newlyUnlockedPieces)
		{
			for (int i = 0; i < this.WeaponClasses.Count; i++)
			{
				WeaponClassVM weaponClassVM = this.WeaponClasses[i];
				int num = 0;
				for (int j = 0; j < newlyUnlockedPieces.Count; j++)
				{
					CraftingPiece craftingPiece = newlyUnlockedPieces[j];
					if (weaponClassVM.Template.IsPieceTypeUsable(craftingPiece.PieceType))
					{
						CraftingPiece craftingPiece2 = this.FindPieceInTemplate(weaponClassVM.Template, craftingPiece);
						if (craftingPiece2 != null && !craftingPiece2.IsHiddenOnDesigner && this._craftingBehavior.IsOpened(craftingPiece2, weaponClassVM.Template))
						{
							num++;
						}
					}
				}
				weaponClassVM.NewlyUnlockedPieceCount = num;
			}
		}

		// Token: 0x06001772 RID: 6002 RVA: 0x00059BF4 File Offset: 0x00057DF4
		private CraftingPiece FindPieceInTemplate(CraftingTemplate template, CraftingPiece piece)
		{
			foreach (CraftingPiece craftingPiece in template.Pieces)
			{
				if (piece.StringId == craftingPiece.StringId)
				{
					return craftingPiece;
				}
			}
			return null;
		}

		// Token: 0x06001773 RID: 6003 RVA: 0x00059C5C File Offset: 0x00057E5C
		public void ExecuteSelectWeaponClass(int index)
		{
			if (this.WeaponClasses[index].IsSelected)
			{
				this.ExecuteClosePopup();
				return;
			}
			Action<int> onSelect = this._onSelect;
			if (onSelect != null)
			{
				onSelect(index);
			}
			this.ExecuteClosePopup();
		}

		// Token: 0x06001774 RID: 6004 RVA: 0x00059C90 File Offset: 0x00057E90
		public void ExecuteClosePopup()
		{
			this.IsVisible = false;
		}

		// Token: 0x06001775 RID: 6005 RVA: 0x00059C99 File Offset: 0x00057E99
		public void ExecuteOpenPopup()
		{
			this.IsVisible = true;
			this.RefreshList();
		}

		// Token: 0x170007D2 RID: 2002
		// (get) Token: 0x06001776 RID: 6006 RVA: 0x00059CA8 File Offset: 0x00057EA8
		// (set) Token: 0x06001777 RID: 6007 RVA: 0x00059CB0 File Offset: 0x00057EB0
		[DataSourceProperty]
		public string PopupHeader
		{
			get
			{
				return this._popupHeader;
			}
			set
			{
				if (value != this._popupHeader)
				{
					this._popupHeader = value;
					base.OnPropertyChangedWithValue<string>(value, "PopupHeader");
				}
			}
		}

		// Token: 0x170007D3 RID: 2003
		// (get) Token: 0x06001778 RID: 6008 RVA: 0x00059CD3 File Offset: 0x00057ED3
		// (set) Token: 0x06001779 RID: 6009 RVA: 0x00059CDB File Offset: 0x00057EDB
		[DataSourceProperty]
		public bool IsVisible
		{
			get
			{
				return this._isVisible;
			}
			set
			{
				if (value != this._isVisible)
				{
					this._isVisible = value;
					base.OnPropertyChangedWithValue(value, "IsVisible");
					Game game = Game.Current;
					if (game == null)
					{
						return;
					}
					game.EventManager.TriggerEvent<CraftingWeaponClassSelectionOpenedEvent>(new CraftingWeaponClassSelectionOpenedEvent(this._isVisible));
				}
			}
		}

		// Token: 0x170007D4 RID: 2004
		// (get) Token: 0x0600177A RID: 6010 RVA: 0x00059D18 File Offset: 0x00057F18
		// (set) Token: 0x0600177B RID: 6011 RVA: 0x00059D20 File Offset: 0x00057F20
		[DataSourceProperty]
		public MBBindingList<WeaponClassVM> WeaponClasses
		{
			get
			{
				return this._weaponClasses;
			}
			set
			{
				if (value != this._weaponClasses)
				{
					this._weaponClasses = value;
					base.OnPropertyChangedWithValue<MBBindingList<WeaponClassVM>>(value, "WeaponClasses");
				}
			}
		}

		// Token: 0x04000ABC RID: 2748
		private readonly ICraftingCampaignBehavior _craftingBehavior;

		// Token: 0x04000ABD RID: 2749
		private readonly Action<int> _onSelect;

		// Token: 0x04000ABE RID: 2750
		private readonly List<CraftingTemplate> _templatesList;

		// Token: 0x04000ABF RID: 2751
		private readonly Func<CraftingTemplate, int> _getUnlockedPiecesCount;

		// Token: 0x04000AC0 RID: 2752
		private string _popupHeader;

		// Token: 0x04000AC1 RID: 2753
		private bool _isVisible;

		// Token: 0x04000AC2 RID: 2754
		private MBBindingList<WeaponClassVM> _weaponClasses;
	}
}
