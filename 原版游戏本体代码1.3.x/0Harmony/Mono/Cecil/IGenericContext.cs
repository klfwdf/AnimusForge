using System;

namespace Mono.Cecil
{
	// Token: 0x0200024B RID: 587
	internal interface IGenericContext
	{
		// Token: 0x1700030B RID: 779
		// (get) Token: 0x06000D40 RID: 3392
		bool IsDefinition { get; }

		// Token: 0x1700030C RID: 780
		// (get) Token: 0x06000D41 RID: 3393
		IGenericParameterProvider Type { get; }

		// Token: 0x1700030D RID: 781
		// (get) Token: 0x06000D42 RID: 3394
		IGenericParameterProvider Method { get; }
	}
}
