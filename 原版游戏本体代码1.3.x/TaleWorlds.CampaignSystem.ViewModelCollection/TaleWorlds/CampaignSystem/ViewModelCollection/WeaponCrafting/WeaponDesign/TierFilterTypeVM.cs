using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x02000103 RID: 259
	public class TierFilterTypeVM : ViewModel
	{
		// Token: 0x170007CB RID: 1995
		// (get) Token: 0x06001761 RID: 5985 RVA: 0x000598AA File Offset: 0x00057AAA
		public WeaponDesignVM.CraftingPieceTierFilter FilterType { get; }

		// Token: 0x06001762 RID: 5986 RVA: 0x000598B2 File Offset: 0x00057AB2
		public TierFilterTypeVM(WeaponDesignVM.CraftingPieceTierFilter filterType, Action<WeaponDesignVM.CraftingPieceTierFilter> onSelect, string tierName)
		{
			this.FilterType = filterType;
			this._onSelect = onSelect;
			this.TierName = tierName;
		}

		// Token: 0x06001763 RID: 5987 RVA: 0x000598CF File Offset: 0x00057ACF
		public void ExecuteSelectTier()
		{
			Action<WeaponDesignVM.CraftingPieceTierFilter> onSelect = this._onSelect;
			if (onSelect == null)
			{
				return;
			}
			onSelect(this.FilterType);
		}

		// Token: 0x170007CC RID: 1996
		// (get) Token: 0x06001764 RID: 5988 RVA: 0x000598E7 File Offset: 0x00057AE7
		// (set) Token: 0x06001765 RID: 5989 RVA: 0x000598EF File Offset: 0x00057AEF
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

		// Token: 0x170007CD RID: 1997
		// (get) Token: 0x06001766 RID: 5990 RVA: 0x0005990D File Offset: 0x00057B0D
		// (set) Token: 0x06001767 RID: 5991 RVA: 0x00059915 File Offset: 0x00057B15
		[DataSourceProperty]
		public string TierName
		{
			get
			{
				return this._tierName;
			}
			set
			{
				if (value != this._tierName)
				{
					this._tierName = value;
					base.OnPropertyChangedWithValue<string>(value, "TierName");
				}
			}
		}

		// Token: 0x04000AB5 RID: 2741
		private readonly Action<WeaponDesignVM.CraftingPieceTierFilter> _onSelect;

		// Token: 0x04000AB6 RID: 2742
		private bool _isSelected;

		// Token: 0x04000AB7 RID: 2743
		private string _tierName;
	}
}
