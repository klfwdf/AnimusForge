using System;

namespace Steamworks
{
	// Token: 0x020001B6 RID: 438
	[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
	internal class CallbackIdentityAttribute : Attribute
	{
		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000AEC RID: 2796 RVA: 0x0000EEB7 File Offset: 0x0000D0B7
		// (set) Token: 0x06000AED RID: 2797 RVA: 0x0000EEBF File Offset: 0x0000D0BF
		public int Identity { get; set; }

		// Token: 0x06000AEE RID: 2798 RVA: 0x0000EEC8 File Offset: 0x0000D0C8
		public CallbackIdentityAttribute(int callbackNum)
		{
			this.Identity = callbackNum;
		}
	}
}
