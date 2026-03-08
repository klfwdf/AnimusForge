using System;

namespace MonoMod.Core.Platforms
{
	// Token: 0x02000515 RID: 1301
	[Flags]
	internal enum SystemFeature
	{
		// Token: 0x04001206 RID: 4614
		None = 0,
		// Token: 0x04001207 RID: 4615
		RWXPages = 1,
		// Token: 0x04001208 RID: 4616
		RXPages = 2,
		// Token: 0x04001209 RID: 4617
		MayUseNativeJitHooks = 16
	}
}
