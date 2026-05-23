using System;

namespace Mono.Cecil
{
	// Token: 0x02000246 RID: 582
	internal interface IConstantProvider : IMetadataTokenProvider
	{
		// Token: 0x17000300 RID: 768
		// (get) Token: 0x06000D33 RID: 3379
		// (set) Token: 0x06000D34 RID: 3380
		bool HasConstant { get; set; }

		// Token: 0x17000301 RID: 769
		// (get) Token: 0x06000D35 RID: 3381
		// (set) Token: 0x06000D36 RID: 3382
		object Constant { get; set; }
	}
}
