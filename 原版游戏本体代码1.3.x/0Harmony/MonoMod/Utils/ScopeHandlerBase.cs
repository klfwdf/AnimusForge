using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x02000878 RID: 2168
	internal abstract class ScopeHandlerBase
	{
		// Token: 0x06002C9A RID: 11418
		[NullableContext(2)]
		public abstract void EndScope(object data);
	}
}
