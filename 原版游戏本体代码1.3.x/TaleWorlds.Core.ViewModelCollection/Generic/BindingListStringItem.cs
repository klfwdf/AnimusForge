using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic
{
	// Token: 0x02000025 RID: 37
	public class BindingListStringItem : ViewModel
	{
		// Token: 0x060001AF RID: 431 RVA: 0x00005BDE File Offset: 0x00003DDE
		public BindingListStringItem(string value)
		{
			this.Item = value;
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x060001B0 RID: 432 RVA: 0x00005BED File Offset: 0x00003DED
		// (set) Token: 0x060001B1 RID: 433 RVA: 0x00005BF5 File Offset: 0x00003DF5
		[DataSourceProperty]
		public string Item
		{
			get
			{
				return this._item;
			}
			set
			{
				if (value != this._item)
				{
					this._item = value;
					base.OnPropertyChangedWithValue<string>(value, "Item");
				}
			}
		}

		// Token: 0x040000AC RID: 172
		private string _item;
	}
}
