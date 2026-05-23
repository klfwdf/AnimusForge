using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection
{
	// Token: 0x0200000A RID: 10
	public class CraftingItemViewModel : ViewModel
	{
		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000056 RID: 86 RVA: 0x000029B1 File Offset: 0x00000BB1
		// (set) Token: 0x06000057 RID: 87 RVA: 0x000029B9 File Offset: 0x00000BB9
		[DataSourceProperty]
		public string UsedPieces
		{
			get
			{
				return this._usedPieces;
			}
			set
			{
				this._usedPieces = value;
				base.OnPropertyChangedWithValue<string>(value, "UsedPieces");
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000058 RID: 88 RVA: 0x000029CE File Offset: 0x00000BCE
		// (set) Token: 0x06000059 RID: 89 RVA: 0x000029D6 File Offset: 0x00000BD6
		[DataSourceProperty]
		public int WeaponClass
		{
			get
			{
				return this._weaponClass;
			}
			set
			{
				if (value != this._weaponClass)
				{
					this._weaponClass = value;
					base.OnPropertyChangedWithValue(value, "WeaponClass");
				}
			}
		}

		// Token: 0x0600005A RID: 90 RVA: 0x000029F4 File Offset: 0x00000BF4
		public WeaponClass GetWeaponClass()
		{
			return (WeaponClass)this.WeaponClass;
		}

		// Token: 0x0600005B RID: 91 RVA: 0x000029FC File Offset: 0x00000BFC
		public void SetCraftingData(WeaponClass weaponClass, WeaponDesignElement[] craftingPieces)
		{
			this.WeaponClass = (int)weaponClass;
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00002A05 File Offset: 0x00000C05
		public CraftingItemViewModel()
		{
			this.WeaponClass = -1;
		}

		// Token: 0x04000021 RID: 33
		private string _usedPieces;

		// Token: 0x04000022 RID: 34
		private int _weaponClass;
	}
}
