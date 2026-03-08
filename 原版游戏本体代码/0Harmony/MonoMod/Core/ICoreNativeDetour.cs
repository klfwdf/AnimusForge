using System;

namespace MonoMod.Core
{
	// Token: 0x020004DD RID: 1245
	internal interface ICoreNativeDetour : ICoreDetourBase, IDisposable
	{
		// Token: 0x170005F3 RID: 1523
		// (get) Token: 0x06001B9F RID: 7071
		IntPtr Source { get; }

		// Token: 0x170005F4 RID: 1524
		// (get) Token: 0x06001BA0 RID: 7072
		IntPtr Target { get; }

		// Token: 0x170005F5 RID: 1525
		// (get) Token: 0x06001BA1 RID: 7073
		bool HasOrigEntrypoint { get; }

		// Token: 0x170005F6 RID: 1526
		// (get) Token: 0x06001BA2 RID: 7074
		IntPtr OrigEntrypoint { get; }
	}
}
