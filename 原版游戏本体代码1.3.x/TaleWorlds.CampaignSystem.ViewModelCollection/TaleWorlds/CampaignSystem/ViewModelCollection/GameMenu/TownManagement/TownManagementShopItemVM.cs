using System;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TownManagement
{
	// Token: 0x020000AA RID: 170
	public class TownManagementShopItemVM : ViewModel
	{
		// Token: 0x06001044 RID: 4164 RVA: 0x0004231C File Offset: 0x0004051C
		public TownManagementShopItemVM(Workshop workshop)
		{
			this._workshop = workshop;
			this.IsEmpty = this._workshop.WorkshopType == null;
			if (!this.IsEmpty)
			{
				this.ShopId = this._workshop.WorkshopType.StringId;
			}
			else
			{
				this.ShopId = "empty";
			}
			this.RefreshValues();
		}

		// Token: 0x06001045 RID: 4165 RVA: 0x0004237C File Offset: 0x0004057C
		public override void RefreshValues()
		{
			base.RefreshValues();
			if (!this.IsEmpty)
			{
				this.ShopName = this._workshop.WorkshopType.Name.ToString();
				return;
			}
			this.ShopName = GameTexts.FindText("str_empty", null).ToString();
		}

		// Token: 0x06001046 RID: 4166 RVA: 0x000423C9 File Offset: 0x000405C9
		public void ExecuteBeginHint()
		{
			if (this._workshop.WorkshopType != null)
			{
				InformationManager.ShowTooltip(typeof(Workshop), new object[] { this._workshop });
			}
		}

		// Token: 0x06001047 RID: 4167 RVA: 0x000423F6 File Offset: 0x000405F6
		public void ExecuteEndHint()
		{
			MBInformationManager.HideInformations();
		}

		// Token: 0x17000543 RID: 1347
		// (get) Token: 0x06001048 RID: 4168 RVA: 0x000423FD File Offset: 0x000405FD
		// (set) Token: 0x06001049 RID: 4169 RVA: 0x00042405 File Offset: 0x00040605
		[DataSourceProperty]
		public bool IsEmpty
		{
			get
			{
				return this._isEmpty;
			}
			set
			{
				if (value != this._isEmpty)
				{
					this._isEmpty = value;
					base.OnPropertyChangedWithValue(value, "IsEmpty");
				}
			}
		}

		// Token: 0x17000544 RID: 1348
		// (get) Token: 0x0600104A RID: 4170 RVA: 0x00042423 File Offset: 0x00040623
		// (set) Token: 0x0600104B RID: 4171 RVA: 0x0004242B File Offset: 0x0004062B
		[DataSourceProperty]
		public string ShopName
		{
			get
			{
				return this._shopName;
			}
			set
			{
				if (value != this._shopName)
				{
					this._shopName = value;
					base.OnPropertyChangedWithValue<string>(value, "ShopName");
				}
			}
		}

		// Token: 0x17000545 RID: 1349
		// (get) Token: 0x0600104C RID: 4172 RVA: 0x0004244E File Offset: 0x0004064E
		// (set) Token: 0x0600104D RID: 4173 RVA: 0x00042456 File Offset: 0x00040656
		[DataSourceProperty]
		public string ShopId
		{
			get
			{
				return this._shopId;
			}
			set
			{
				if (value != this._shopId)
				{
					this._shopId = value;
					base.OnPropertyChangedWithValue<string>(value, "ShopId");
				}
			}
		}

		// Token: 0x0400076F RID: 1903
		private readonly Workshop _workshop;

		// Token: 0x04000770 RID: 1904
		private bool _isEmpty;

		// Token: 0x04000771 RID: 1905
		private string _shopName;

		// Token: 0x04000772 RID: 1906
		private string _shopId;
	}
}
