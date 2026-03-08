using System;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement
{
	// Token: 0x02000062 RID: 98
	public abstract class KingdomCategoryVM : ViewModel
	{
		// Token: 0x17000207 RID: 519
		// (get) Token: 0x06000755 RID: 1877 RVA: 0x0002311D File Offset: 0x0002131D
		// (set) Token: 0x06000756 RID: 1878 RVA: 0x00023125 File Offset: 0x00021325
		[DataSourceProperty]
		public string CategoryNameText
		{
			get
			{
				return this._categoryNameText;
			}
			set
			{
				if (value != this._categoryNameText)
				{
					this._categoryNameText = value;
					base.OnPropertyChanged("NameText");
				}
			}
		}

		// Token: 0x17000208 RID: 520
		// (get) Token: 0x06000757 RID: 1879 RVA: 0x00023147 File Offset: 0x00021347
		// (set) Token: 0x06000758 RID: 1880 RVA: 0x0002314F File Offset: 0x0002134F
		[DataSourceProperty]
		public string NoItemSelectedText
		{
			get
			{
				return this._noItemSelectedText;
			}
			set
			{
				if (value != this._noItemSelectedText)
				{
					this._noItemSelectedText = value;
					base.OnPropertyChangedWithValue<string>(value, "NoItemSelectedText");
				}
			}
		}

		// Token: 0x17000209 RID: 521
		// (get) Token: 0x06000759 RID: 1881 RVA: 0x00023172 File Offset: 0x00021372
		// (set) Token: 0x0600075A RID: 1882 RVA: 0x0002317A File Offset: 0x0002137A
		[DataSourceProperty]
		public bool IsAcceptableItemSelected
		{
			get
			{
				return this._isAcceptableItemSelected;
			}
			set
			{
				if (value != this._isAcceptableItemSelected)
				{
					this._isAcceptableItemSelected = value;
					base.OnPropertyChangedWithValue(value, "IsAcceptableItemSelected");
				}
			}
		}

		// Token: 0x1700020A RID: 522
		// (get) Token: 0x0600075B RID: 1883 RVA: 0x00023198 File Offset: 0x00021398
		// (set) Token: 0x0600075C RID: 1884 RVA: 0x000231A0 File Offset: 0x000213A0
		[DataSourceProperty]
		public int NotificationCount
		{
			get
			{
				return this._notificationCount;
			}
			set
			{
				if (value != this._notificationCount)
				{
					this._notificationCount = value;
					base.OnPropertyChanged("NotificationCount");
				}
			}
		}

		// Token: 0x1700020B RID: 523
		// (get) Token: 0x0600075D RID: 1885 RVA: 0x000231BD File Offset: 0x000213BD
		// (set) Token: 0x0600075E RID: 1886 RVA: 0x000231C5 File Offset: 0x000213C5
		[DataSourceProperty]
		public bool Show
		{
			get
			{
				return this._show;
			}
			set
			{
				if (value != this._show)
				{
					this._show = value;
					base.OnPropertyChanged("Show");
				}
			}
		}

		// Token: 0x0400032C RID: 812
		private int _notificationCount;

		// Token: 0x0400032D RID: 813
		private string _categoryNameText;

		// Token: 0x0400032E RID: 814
		private string _noItemSelectedText;

		// Token: 0x0400032F RID: 815
		private bool _show;

		// Token: 0x04000330 RID: 816
		private bool _isAcceptableItemSelected;
	}
}
