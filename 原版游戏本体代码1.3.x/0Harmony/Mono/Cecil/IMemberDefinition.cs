using System;

namespace Mono.Cecil
{
	// Token: 0x0200024D RID: 589
	internal interface IMemberDefinition : ICustomAttributeProvider, IMetadataTokenProvider
	{
		// Token: 0x17000310 RID: 784
		// (get) Token: 0x06000D46 RID: 3398
		// (set) Token: 0x06000D47 RID: 3399
		string Name { get; set; }

		// Token: 0x17000311 RID: 785
		// (get) Token: 0x06000D48 RID: 3400
		string FullName { get; }

		// Token: 0x17000312 RID: 786
		// (get) Token: 0x06000D49 RID: 3401
		// (set) Token: 0x06000D4A RID: 3402
		bool IsSpecialName { get; set; }

		// Token: 0x17000313 RID: 787
		// (get) Token: 0x06000D4B RID: 3403
		// (set) Token: 0x06000D4C RID: 3404
		bool IsRuntimeSpecialName { get; set; }

		// Token: 0x17000314 RID: 788
		// (get) Token: 0x06000D4D RID: 3405
		// (set) Token: 0x06000D4E RID: 3406
		TypeDefinition DeclaringType { get; set; }
	}
}
