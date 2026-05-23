using System;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.List
{
	// Token: 0x020000E3 RID: 227
	public class ListTypeVM : ViewModel
	{
		// Token: 0x06001555 RID: 5461 RVA: 0x00054050 File Offset: 0x00052250
		public ListTypeVM(EncyclopediaPage encyclopediaPage)
		{
			this.EncyclopediaPage = encyclopediaPage;
			this.ID = encyclopediaPage.GetIdentifierNames()[0];
			this.ImageID = encyclopediaPage.GetStringID();
			this.Order = encyclopediaPage.HomePageOrderIndex;
			this.RefreshValues();
		}

		// Token: 0x06001556 RID: 5462 RVA: 0x0005408B File Offset: 0x0005228B
		public override void RefreshValues()
		{
			base.RefreshValues();
			this.Name = this.EncyclopediaPage.GetName().ToString();
		}

		// Token: 0x06001557 RID: 5463 RVA: 0x000540A9 File Offset: 0x000522A9
		public void Execute()
		{
			Campaign.Current.EncyclopediaManager.GoToLink("ListPage", this.ID);
		}

		// Token: 0x1700070F RID: 1807
		// (get) Token: 0x06001558 RID: 5464 RVA: 0x000540C5 File Offset: 0x000522C5
		// (set) Token: 0x06001559 RID: 5465 RVA: 0x000540CD File Offset: 0x000522CD
		[DataSourceProperty]
		public string ID
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
					base.OnPropertyChangedWithValue<string>(value, "ID");
				}
			}
		}

		// Token: 0x17000710 RID: 1808
		// (get) Token: 0x0600155A RID: 5466 RVA: 0x000540F0 File Offset: 0x000522F0
		// (set) Token: 0x0600155B RID: 5467 RVA: 0x000540F8 File Offset: 0x000522F8
		[DataSourceProperty]
		public int Order
		{
			get
			{
				return this._order;
			}
			set
			{
				if (value != this._order)
				{
					this._order = value;
					base.OnPropertyChangedWithValue(value, "Order");
				}
			}
		}

		// Token: 0x17000711 RID: 1809
		// (get) Token: 0x0600155C RID: 5468 RVA: 0x00054116 File Offset: 0x00052316
		// (set) Token: 0x0600155D RID: 5469 RVA: 0x0005411E File Offset: 0x0005231E
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

		// Token: 0x17000712 RID: 1810
		// (get) Token: 0x0600155E RID: 5470 RVA: 0x00054141 File Offset: 0x00052341
		// (set) Token: 0x0600155F RID: 5471 RVA: 0x00054149 File Offset: 0x00052349
		[DataSourceProperty]
		public string ImageID
		{
			get
			{
				return this._imageId;
			}
			set
			{
				if (value != this._imageId)
				{
					this._imageId = value;
					base.OnPropertyChangedWithValue<string>(value, "ImageID");
				}
			}
		}

		// Token: 0x040009B8 RID: 2488
		public readonly EncyclopediaPage EncyclopediaPage;

		// Token: 0x040009B9 RID: 2489
		private string _name;

		// Token: 0x040009BA RID: 2490
		private string _id;

		// Token: 0x040009BB RID: 2491
		private string _imageId;

		// Token: 0x040009BC RID: 2492
		private int _order;
	}
}
