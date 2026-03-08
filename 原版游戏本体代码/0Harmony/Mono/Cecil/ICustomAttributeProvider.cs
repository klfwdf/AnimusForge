using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000247 RID: 583
	internal interface ICustomAttributeProvider : IMetadataTokenProvider
	{
		// Token: 0x17000302 RID: 770
		// (get) Token: 0x06000D37 RID: 3383
		Collection<CustomAttribute> CustomAttributes { get; }

		// Token: 0x17000303 RID: 771
		// (get) Token: 0x06000D38 RID: 3384
		bool HasCustomAttributes { get; }
	}
}
