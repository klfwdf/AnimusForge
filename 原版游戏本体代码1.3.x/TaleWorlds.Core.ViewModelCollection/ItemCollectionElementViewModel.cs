using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection
{
	// Token: 0x0200000B RID: 11
	public class ItemCollectionElementViewModel : ViewModel
	{
		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600005D RID: 93 RVA: 0x00002A14 File Offset: 0x00000C14
		// (set) Token: 0x0600005E RID: 94 RVA: 0x00002A1C File Offset: 0x00000C1C
		[DataSourceProperty]
		public string StringId
		{
			get
			{
				return this._stringId;
			}
			set
			{
				if (this._stringId != value)
				{
					this._stringId = value;
					base.OnPropertyChangedWithValue<string>(value, "StringId");
				}
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00002A3F File Offset: 0x00000C3F
		// (set) Token: 0x06000060 RID: 96 RVA: 0x00002A47 File Offset: 0x00000C47
		[DataSourceProperty]
		public int Ammo
		{
			get
			{
				return this._ammo;
			}
			set
			{
				if (this._ammo != value)
				{
					this._ammo = value;
					base.OnPropertyChangedWithValue(value, "Ammo");
				}
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000061 RID: 97 RVA: 0x00002A65 File Offset: 0x00000C65
		// (set) Token: 0x06000062 RID: 98 RVA: 0x00002A6D File Offset: 0x00000C6D
		[DataSourceProperty]
		public int AverageUnitCost
		{
			get
			{
				return this._averageUnitCost;
			}
			set
			{
				if (this._averageUnitCost != value)
				{
					this._averageUnitCost = value;
					base.OnPropertyChangedWithValue(value, "AverageUnitCost");
				}
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00002A8B File Offset: 0x00000C8B
		// (set) Token: 0x06000064 RID: 100 RVA: 0x00002A93 File Offset: 0x00000C93
		[DataSourceProperty]
		public string ItemModifierId
		{
			get
			{
				return this._itemModifierId;
			}
			set
			{
				if (this._itemModifierId != value)
				{
					this._itemModifierId = value;
					base.OnPropertyChangedWithValue<string>(value, "ItemModifierId");
				}
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000065 RID: 101 RVA: 0x00002AB6 File Offset: 0x00000CB6
		// (set) Token: 0x06000066 RID: 102 RVA: 0x00002ABE File Offset: 0x00000CBE
		[DataSourceProperty]
		public string BannerCode
		{
			get
			{
				return this._bannerCode;
			}
			set
			{
				if (value != this._bannerCode)
				{
					this._bannerCode = value;
					base.OnPropertyChangedWithValue<string>(value, "BannerCode");
				}
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000067 RID: 103 RVA: 0x00002AE1 File Offset: 0x00000CE1
		// (set) Token: 0x06000068 RID: 104 RVA: 0x00002AE9 File Offset: 0x00000CE9
		[DataSourceProperty]
		public float InitialPanRotation
		{
			get
			{
				return this._initialPanRotation;
			}
			set
			{
				if (value != this._initialPanRotation)
				{
					this._initialPanRotation = value;
					base.OnPropertyChangedWithValue(value, "InitialPanRotation");
				}
			}
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00002B08 File Offset: 0x00000D08
		public void FillFrom(EquipmentElement item, Banner banner = null)
		{
			this.StringId = ((item.Item != null) ? item.Item.StringId : "");
			this.Ammo = (int)((!item.IsEmpty && item.Item.PrimaryWeapon != null && item.Item.PrimaryWeapon.IsConsumable) ? item.GetModifiedStackCountForUsage(0) : 0);
			this.AverageUnitCost = item.ItemValue;
			this.ItemModifierId = ((item.ItemModifier != null) ? item.ItemModifier.StringId : "");
			this.BannerCode = ((banner != null) ? banner.BannerCode : "");
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00002BB7 File Offset: 0x00000DB7
		public override void OnFinalize()
		{
			base.OnFinalize();
			this.StringId = "";
			this.ItemModifierId = "";
		}

		// Token: 0x04000023 RID: 35
		private string _stringId;

		// Token: 0x04000024 RID: 36
		private int _ammo;

		// Token: 0x04000025 RID: 37
		private int _averageUnitCost;

		// Token: 0x04000026 RID: 38
		private string _itemModifierId;

		// Token: 0x04000027 RID: 39
		private string _bannerCode;

		// Token: 0x04000028 RID: 40
		private float _initialPanRotation;
	}
}
