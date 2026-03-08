using System;
using Mono.Collections.Generic;

namespace Mono.Cecil
{
	// Token: 0x02000251 RID: 593
	internal interface IMethodSignature : IMetadataTokenProvider
	{
		// Token: 0x17000318 RID: 792
		// (get) Token: 0x06000D54 RID: 3412
		// (set) Token: 0x06000D55 RID: 3413
		bool HasThis { get; set; }

		// Token: 0x17000319 RID: 793
		// (get) Token: 0x06000D56 RID: 3414
		// (set) Token: 0x06000D57 RID: 3415
		bool ExplicitThis { get; set; }

		// Token: 0x1700031A RID: 794
		// (get) Token: 0x06000D58 RID: 3416
		// (set) Token: 0x06000D59 RID: 3417
		MethodCallingConvention CallingConvention { get; set; }

		// Token: 0x1700031B RID: 795
		// (get) Token: 0x06000D5A RID: 3418
		bool HasParameters { get; }

		// Token: 0x1700031C RID: 796
		// (get) Token: 0x06000D5B RID: 3419
		Collection<ParameterDefinition> Parameters { get; }

		// Token: 0x1700031D RID: 797
		// (get) Token: 0x06000D5C RID: 3420
		// (set) Token: 0x06000D5D RID: 3421
		TypeReference ReturnType { get; set; }

		// Token: 0x1700031E RID: 798
		// (get) Token: 0x06000D5E RID: 3422
		MethodReturnType MethodReturnType { get; }
	}
}
