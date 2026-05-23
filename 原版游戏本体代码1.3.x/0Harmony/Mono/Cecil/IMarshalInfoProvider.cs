using System;

namespace Mono.Cecil
{
	// Token: 0x0200024C RID: 588
	internal interface IMarshalInfoProvider : IMetadataTokenProvider
	{
		// Token: 0x1700030E RID: 782
		// (get) Token: 0x06000D43 RID: 3395
		bool HasMarshalInfo { get; }

		// Token: 0x1700030F RID: 783
		// (get) Token: 0x06000D44 RID: 3396
		// (set) Token: 0x06000D45 RID: 3397
		MarshalInfo MarshalInfo { get; set; }
	}
}
