using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core
{
	// Token: 0x020004DE RID: 1246
	[NullableContext(1)]
	[CLSCompliant(true)]
	internal interface IDetourFactory
	{
		// Token: 0x06001BA3 RID: 7075
		ICoreDetour CreateDetour(CreateDetourRequest request);

		// Token: 0x06001BA4 RID: 7076
		ICoreNativeDetour CreateNativeDetour(CreateNativeDetourRequest request);

		// Token: 0x170005F7 RID: 1527
		// (get) Token: 0x06001BA5 RID: 7077
		bool SupportsNativeDetourOrigEntrypoint { get; }
	}
}
