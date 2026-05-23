using System;
using System.Runtime.CompilerServices;

namespace MCM.UI.Dropdown
{
	// Token: 0x0200002D RID: 45
	[NullableContext(1)]
	[Nullable(0)]
	internal class MCMSelectorItemVM<T> : MCMSelectorItemVMBase where T : class
	{
		// Token: 0x17000081 RID: 129
		// (get) Token: 0x06000191 RID: 401 RVA: 0x0000739F File Offset: 0x0000559F
		public T OriginalItem { get; }

		// Token: 0x06000192 RID: 402 RVA: 0x000073A7 File Offset: 0x000055A7
		public MCMSelectorItemVM(T @object)
		{
			this.OriginalItem = @object;
			this.RefreshValues();
		}

		// Token: 0x06000193 RID: 403 RVA: 0x000073BC File Offset: 0x000055BC
		[NullableContext(2)]
		public override string ToString()
		{
			return base.StringItem;
		}

		// Token: 0x06000194 RID: 404 RVA: 0x000073C4 File Offset: 0x000055C4
		public override void RefreshValues()
		{
			base.RefreshValues();
			this._stringItem = this.OriginalItem.ToString() ?? "ERROR";
		}
	}
}
