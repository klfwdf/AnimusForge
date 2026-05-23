using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000248 RID: 584
	internal interface IGenericInstance : IMetadataTokenProvider
	{
		// Token: 0x17000304 RID: 772
		// (get) Token: 0x06000D39 RID: 3385
		bool HasGenericArguments { get; }

		// Token: 0x17000305 RID: 773
		// (get) Token: 0x06000D3A RID: 3386
		Collection<TypeReference> GenericArguments { get; }
	}
}
