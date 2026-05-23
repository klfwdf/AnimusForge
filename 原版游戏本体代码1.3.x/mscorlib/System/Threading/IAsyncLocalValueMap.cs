using System;

namespace System.Threading
{
	// Token: 0x020004E8 RID: 1256
	internal interface IAsyncLocalValueMap
	{
		// Token: 0x06003B85 RID: 15237
		bool TryGetValue(IAsyncLocal key, out object value);

		// Token: 0x06003B86 RID: 15238
		IAsyncLocalValueMap Set(IAsyncLocal key, object value, bool treatNullValueAsNonexistent);
	}
}
