using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement
{
	// Token: 0x02000064 RID: 100
	public abstract class KingdomItemVM : ViewModel
	{
		// Token: 0x0600078A RID: 1930 RVA: 0x00023736 File Offset: 0x00021936
		protected virtual void OnSelect()
		{
			this.IsSelected = true;
		}

		// Token: 0x1700021C RID: 540
		// (get) Token: 0x0600078B RID: 1931 RVA: 0x0002373F File Offset: 0x0002193F
		// (set) Token: 0x0600078C RID: 1932 RVA: 0x00023747 File Offset: 0x00021947
		[DataSourceProperty]
		public bool IsNew
		{
			get
			{
				return this._isNew;
			}
			set
			{
				if (value != this._isNew)
				{
					this._isNew = value;
					base.OnPropertyChangedWithValue(value, "IsNew");
				}
			}
		}

		// Token: 0x1700021D RID: 541
		// (get) Token: 0x0600078D RID: 1933 RVA: 0x00023765 File Offset: 0x00021965
		// (set) Token: 0x0600078E RID: 1934 RVA: 0x0002376D File Offset: 0x0002196D
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

		// Token: 0x04000343 RID: 835
		private bool _isSelected;

		// Token: 0x04000344 RID: 836
		private bool _isNew;
	}
}
