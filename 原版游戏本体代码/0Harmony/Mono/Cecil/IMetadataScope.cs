using System;

namespace Mono.Cecil
{
	// Token: 0x0200024F RID: 591
	internal interface IMetadataScope : IMetadataTokenProvider
	{
		// Token: 0x17000315 RID: 789
		// (get) Token: 0x06000D4F RID: 3407
		MetadataScopeType MetadataScopeType { get; }

		// Token: 0x17000316 RID: 790
		// (get) Token: 0x06000D50 RID: 3408
		// (set) Token: 0x06000D51 RID: 3409
		string Name { get; set; }
	}
}
