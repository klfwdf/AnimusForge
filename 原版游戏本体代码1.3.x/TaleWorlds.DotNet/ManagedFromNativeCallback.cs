using System;

namespace TaleWorlds.DotNet
{
	// Token: 0x0200001F RID: 31
	public class ManagedFromNativeCallback : Attribute
	{
		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000090 RID: 144 RVA: 0x000030A1 File Offset: 0x000012A1
		// (set) Token: 0x06000091 RID: 145 RVA: 0x000030A9 File Offset: 0x000012A9
		public string[] Conditionals { get; private set; }

		// Token: 0x06000092 RID: 146 RVA: 0x000030B2 File Offset: 0x000012B2
		public ManagedFromNativeCallback(string[] conditionals = null, bool isMultiThreadCallable = false)
		{
			this.Conditionals = conditionals;
			this.IsMultiThreadCallable = isMultiThreadCallable;
		}

		// Token: 0x0400003F RID: 63
		public bool IsMultiThreadCallable;
	}
}
