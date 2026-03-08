using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x0200087A RID: 2170
	[NullableContext(2)]
	[Nullable(0)]
	internal abstract class ScopeHandlerBase<T> : ScopeHandlerBase
	{
		// Token: 0x06002C9F RID: 11423 RVA: 0x0009369A File Offset: 0x0009189A
		public sealed override void EndScope(object data)
		{
			this.EndScope((T)((object)data));
		}

		// Token: 0x06002CA0 RID: 11424
		[NullableContext(1)]
		public abstract void EndScope(T data);
	}
}
