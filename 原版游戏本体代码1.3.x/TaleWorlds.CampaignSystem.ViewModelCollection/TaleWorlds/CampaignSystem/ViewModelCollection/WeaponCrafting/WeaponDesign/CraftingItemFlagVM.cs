using System;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign
{
	// Token: 0x02000100 RID: 256
	public class CraftingItemFlagVM : ItemFlagVM
	{
		// Token: 0x06001735 RID: 5941 RVA: 0x000593A4 File Offset: 0x000575A4
		public CraftingItemFlagVM(string iconPath, TextObject hint, bool isDisplayed)
			: base(iconPath, hint)
		{
			this.IsDisplayed = isDisplayed;
			this.IconPath = "SPGeneral\\" + iconPath;
		}

		// Token: 0x170007BA RID: 1978
		// (get) Token: 0x06001736 RID: 5942 RVA: 0x000593C6 File Offset: 0x000575C6
		// (set) Token: 0x06001737 RID: 5943 RVA: 0x000593CE File Offset: 0x000575CE
		[DataSourceProperty]
		public bool IsDisplayed
		{
			get
			{
				return this._isDisplayed;
			}
			set
			{
				if (value != this._isDisplayed)
				{
					this._isDisplayed = value;
					base.OnPropertyChangedWithValue(value, "IsDisplayed");
				}
			}
		}

		// Token: 0x170007BB RID: 1979
		// (get) Token: 0x06001738 RID: 5944 RVA: 0x000593EC File Offset: 0x000575EC
		// (set) Token: 0x06001739 RID: 5945 RVA: 0x000593F4 File Offset: 0x000575F4
		[DataSourceProperty]
		public string IconPath
		{
			get
			{
				return this._iconPath;
			}
			set
			{
				if (value != this._iconPath)
				{
					this._iconPath = value;
					base.OnPropertyChangedWithValue<string>(value, "IconPath");
				}
			}
		}

		// Token: 0x04000A9E RID: 2718
		private bool _isDisplayed;

		// Token: 0x04000A9F RID: 2719
		private string _iconPath;
	}
}
