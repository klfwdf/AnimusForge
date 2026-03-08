using System;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic
{
	// Token: 0x02000024 RID: 36
	public class BindingListFloatItem : ViewModel
	{
		// Token: 0x060001AC RID: 428 RVA: 0x00005BA9 File Offset: 0x00003DA9
		public BindingListFloatItem(float value)
		{
			this.Item = value;
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x060001AD RID: 429 RVA: 0x00005BB8 File Offset: 0x00003DB8
		// (set) Token: 0x060001AE RID: 430 RVA: 0x00005BC0 File Offset: 0x00003DC0
		[DataSourceProperty]
		public float Item
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
					base.OnPropertyChangedWithValue(value, "Item");
				}
			}
		}

		// Token: 0x040000AB RID: 171
		private float _item;
	}
}
