using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Core.Utils
{
	// Token: 0x020004F0 RID: 1264
	[NullableContext(1)]
	internal interface IInitialize<[Nullable(2)] T>
	{
		// Token: 0x06001C3B RID: 7227
		void Initialize(T value);
	}
}
