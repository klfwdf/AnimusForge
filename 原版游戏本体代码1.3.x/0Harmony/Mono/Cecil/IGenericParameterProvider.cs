using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000249 RID: 585
	internal interface IGenericParameterProvider : IMetadataTokenProvider
	{
		// Token: 0x17000306 RID: 774
		// (get) Token: 0x06000D3B RID: 3387
		bool HasGenericParameters { get; }

		// Token: 0x17000307 RID: 775
		// (get) Token: 0x06000D3C RID: 3388
		bool IsDefinition { get; }

		// Token: 0x17000308 RID: 776
		// (get) Token: 0x06000D3D RID: 3389
		ModuleDefinition Module { get; }

		// Token: 0x17000309 RID: 777
		// (get) Token: 0x06000D3E RID: 3390
		Collection<GenericParameter> GenericParameters { get; }

		// Token: 0x1700030A RID: 778
		// (get) Token: 0x06000D3F RID: 3391
		GenericParameterType GenericParameterType { get; }
	}
}
