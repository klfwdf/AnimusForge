using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x0200087B RID: 2171
	[NullableContext(1)]
	[Nullable(0)]
	internal readonly struct DataScope<[Nullable(2)] T> : IDisposable
	{
		// Token: 0x1700082F RID: 2095
		// (get) Token: 0x06002CA2 RID: 11426 RVA: 0x000936B0 File Offset: 0x000918B0
		public T Data
		{
			get
			{
				return this.data;
			}
		}

		// Token: 0x06002CA3 RID: 11427 RVA: 0x000936B8 File Offset: 0x000918B8
		public DataScope(ScopeHandlerBase<T> handler, T data)
		{
			this.handler = handler;
			this.data = data;
		}

		// Token: 0x06002CA4 RID: 11428 RVA: 0x000936C8 File Offset: 0x000918C8
		public void Dispose()
		{
			if (this.handler != null)
			{
				this.handler.EndScope(this.data);
			}
		}

		// Token: 0x04003A5D RID: 14941
		[Nullable(new byte[] { 2, 1 })]
		private readonly ScopeHandlerBase<T> handler;

		// Token: 0x04003A5E RID: 14942
		private readonly T data;
	}
}
