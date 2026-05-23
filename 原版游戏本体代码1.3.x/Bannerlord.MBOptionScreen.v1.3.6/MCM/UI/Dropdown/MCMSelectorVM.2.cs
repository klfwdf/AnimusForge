using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MCM.UI.Dropdown
{
	// Token: 0x02000030 RID: 48
	[NullableContext(1)]
	[Nullable(new byte[] { 0, 1 })]
	internal class MCMSelectorVM<[Nullable(0)] TSelectorItemVM, TSelectorItemVMValueType> : MCMSelectorVM<TSelectorItemVM> where TSelectorItemVM : MCMSelectorItemVM<TSelectorItemVMValueType> where TSelectorItemVMValueType : class
	{
		// Token: 0x060001B0 RID: 432 RVA: 0x0000787D File Offset: 0x00005A7D
		public MCMSelectorVM(IEnumerable<TSelectorItemVMValueType> list, int selectedIndex)
		{
			this.Refresh(list, selectedIndex);
		}

		// Token: 0x060001B1 RID: 433 RVA: 0x00007890 File Offset: 0x00005A90
		public void Refresh(IEnumerable<TSelectorItemVMValueType> list, int selectedIndex)
		{
			base.ItemList.Clear();
			this._selectedIndex = -1;
			foreach (TSelectorItemVMValueType @ref in list)
			{
				TSelectorItemVM val = Activator.CreateInstance(typeof(TSelectorItemVM), new object[] { @ref }) as TSelectorItemVM;
				if (val != null)
				{
					base.ItemList.Add(val);
				}
			}
			base.HasSingleItem = base.ItemList.Count <= 1;
			base.SelectedIndex = selectedIndex;
		}
	}
}
