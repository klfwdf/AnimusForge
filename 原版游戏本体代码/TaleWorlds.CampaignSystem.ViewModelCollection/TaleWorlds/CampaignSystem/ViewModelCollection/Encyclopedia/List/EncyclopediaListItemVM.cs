using System;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List
{
	// Token: 0x020000DD RID: 221
	public class EncyclopediaListItemVM : ViewModel
	{
		// Token: 0x170006F8 RID: 1784
		// (get) Token: 0x06001507 RID: 5383 RVA: 0x0005314A File Offset: 0x0005134A
		// (set) Token: 0x06001508 RID: 5384 RVA: 0x00053152 File Offset: 0x00051352
		public object Object { get; private set; }

		// Token: 0x170006F9 RID: 1785
		// (get) Token: 0x06001509 RID: 5385 RVA: 0x0005315B File Offset: 0x0005135B
		public EncyclopediaListItem ListItem { get; }

		// Token: 0x0600150A RID: 5386 RVA: 0x00053164 File Offset: 0x00051364
		public EncyclopediaListItemVM(EncyclopediaListItem listItem)
		{
			this.Object = listItem.Object;
			this.Id = listItem.Id;
			this._type = listItem.TypeName;
			this.ListItem = listItem;
			this.PlayerCanSeeValues = listItem.PlayerCanSeeValues;
			this._onShowTooltip = listItem.OnShowTooltip;
			this.RefreshValues();
		}

		// Token: 0x0600150B RID: 5387 RVA: 0x000531C0 File Offset: 0x000513C0
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.ListItem.Name;
		}

		// Token: 0x0600150C RID: 5388 RVA: 0x000531D9 File Offset: 0x000513D9
		public void Execute()
		{
			Campaign.Current.EncyclopediaManager.GoToLink(this._type, this.Id);
		}

		// Token: 0x0600150D RID: 5389 RVA: 0x000531F6 File Offset: 0x000513F6
		public void SetComparedValue(EncyclopediaListItemComparerBase comparer)
		{
			this.ComparedValue = comparer.GetComparedValueText(this.ListItem);
		}

		// Token: 0x0600150E RID: 5390 RVA: 0x0005320A File Offset: 0x0005140A
		public void ExecuteBeginTooltip()
		{
			Action onShowTooltip = this._onShowTooltip;
			if (onShowTooltip == null)
			{
				return;
			}
			onShowTooltip();
		}

		// Token: 0x0600150F RID: 5391 RVA: 0x0005321C File Offset: 0x0005141C
		public void ExecuteEndTooltip()
		{
			if (this._onShowTooltip != null)
			{
				MBInformationManager.HideInformations();
			}
		}

		// Token: 0x170006FA RID: 1786
		// (get) Token: 0x06001510 RID: 5392 RVA: 0x0005322B File Offset: 0x0005142B
		// (set) Token: 0x06001511 RID: 5393 RVA: 0x00053233 File Offset: 0x00051433
		[DataSourceProperty]
		public bool IsFiltered
		{
			get
			{
				return this._isFiltered;
			}
			set
			{
				if (value != this._isFiltered)
				{
					this._isFiltered = value;
					base.OnPropertyChangedWithValue(value, "IsFiltered");
				}
			}
		}

		// Token: 0x170006FB RID: 1787
		// (get) Token: 0x06001512 RID: 5394 RVA: 0x00053251 File Offset: 0x00051451
		// (set) Token: 0x06001513 RID: 5395 RVA: 0x00053259 File Offset: 0x00051459
		[DataSourceProperty]
		public bool PlayerCanSeeValues
		{
			get
			{
				return this._playerCanSeeValues;
			}
			set
			{
				if (value != this._playerCanSeeValues)
				{
					this._playerCanSeeValues = value;
					base.OnPropertyChangedWithValue(value, "PlayerCanSeeValues");
				}
			}
		}

		// Token: 0x170006FC RID: 1788
		// (get) Token: 0x06001514 RID: 5396 RVA: 0x00053277 File Offset: 0x00051477
		// (set) Token: 0x06001515 RID: 5397 RVA: 0x0005327F File Offset: 0x0005147F
		[DataSourceProperty]
		public string Id
		{
			get
			{
				return this._id;
			}
			set
			{
				if (value != this._id)
				{
					this._id = value;
					base.OnPropertyChangedWithValue<string>(value, "Id");
				}
			}
		}

		// Token: 0x170006FD RID: 1789
		// (get) Token: 0x06001516 RID: 5398 RVA: 0x000532A2 File Offset: 0x000514A2
		// (set) Token: 0x06001517 RID: 5399 RVA: 0x000532AA File Offset: 0x000514AA
		[DataSourceProperty]
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				if (value != this._name)
				{
					this._name = value;
					base.OnPropertyChangedWithValue<string>(value, "Name");
				}
			}
		}

		// Token: 0x170006FE RID: 1790
		// (get) Token: 0x06001518 RID: 5400 RVA: 0x000532CD File Offset: 0x000514CD
		// (set) Token: 0x06001519 RID: 5401 RVA: 0x000532D5 File Offset: 0x000514D5
		[DataSourceProperty]
		public string ComparedValue
		{
			get
			{
				return this._comparedValue;
			}
			set
			{
				if (value != this._comparedValue)
				{
					this._comparedValue = value;
					base.OnPropertyChangedWithValue<string>(value, "ComparedValue");
				}
			}
		}

		// Token: 0x170006FF RID: 1791
		// (get) Token: 0x0600151A RID: 5402 RVA: 0x000532F8 File Offset: 0x000514F8
		// (set) Token: 0x0600151B RID: 5403 RVA: 0x00053300 File Offset: 0x00051500
		[DataSourceProperty]
		public bool IsBookmarked
		{
			get
			{
				return this._isBookmarked;
			}
			set
			{
				if (value != this._isBookmarked)
				{
					this._isBookmarked = value;
					base.OnPropertyChangedWithValue(value, "IsBookmarked");
				}
			}
		}

		// Token: 0x0400099B RID: 2459
		private readonly string _type;

		// Token: 0x0400099C RID: 2460
		private readonly Action _onShowTooltip;

		// Token: 0x0400099D RID: 2461
		private string _id;

		// Token: 0x0400099E RID: 2462
		private string _name;

		// Token: 0x0400099F RID: 2463
		private string _comparedValue;

		// Token: 0x040009A0 RID: 2464
		private bool _isFiltered;

		// Token: 0x040009A1 RID: 2465
		private bool _isBookmarked;

		// Token: 0x040009A2 RID: 2466
		private bool _playerCanSeeValues;
	}
}
