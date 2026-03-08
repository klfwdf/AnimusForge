using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x02000879 RID: 2169
	[NullableContext(2)]
	[Nullable(0)]
	internal readonly struct DataScope : IDisposable
	{
		// Token: 0x1700082E RID: 2094
		// (get) Token: 0x06002C9C RID: 11420 RVA: 0x00093667 File Offset: 0x00091867
		public object Data
		{
			get
			{
				return this.data;
			}
		}

		// Token: 0x06002C9D RID: 11421 RVA: 0x0009366F File Offset: 0x0009186F
		[NullableContext(1)]
		public DataScope(ScopeHandlerBase handler, [Nullable(2)] object data)
		{
			this.handler = handler;
			this.data = data;
		}

		// Token: 0x06002C9E RID: 11422 RVA: 0x0009367F File Offset: 0x0009187F
		public void Dispose()
		{
			if (this.handler != null)
			{
				this.handler.EndScope(this.data);
			}
		}

		// Token: 0x04003A5B RID: 14939
		private readonly ScopeHandlerBase handler;

		// Token: 0x04003A5C RID: 14940
		private readonly object data;
	}
}
