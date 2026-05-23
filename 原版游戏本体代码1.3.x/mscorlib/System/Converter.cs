using System;

namespace System
{
	// Token: 0x02000053 RID: 83
	// (Invoke) Token: 0x06000285 RID: 645
	public delegate TOutput Converter<in TInput, out TOutput>(TInput input);
}
