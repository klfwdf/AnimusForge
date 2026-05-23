using System;
using System.Security;

namespace System.Threading
{
	// Token: 0x020004FB RID: 1275
	public class HostExecutionContext : IDisposable
	{
		// Token: 0x17000918 RID: 2328
		// (get) Token: 0x06003C3E RID: 15422 RVA: 0x000E3FFD File Offset: 0x000E21FD
		// (set) Token: 0x06003C3F RID: 15423 RVA: 0x000E4005 File Offset: 0x000E2205
		protected internal object State
		{
			get
			{
				return this.state;
			}
			set
			{
				this.state = value;
			}
		}

		// Token: 0x06003C40 RID: 15424 RVA: 0x000E400E File Offset: 0x000E220E
		public HostExecutionContext()
		{
		}

		// Token: 0x06003C41 RID: 15425 RVA: 0x000E4016 File Offset: 0x000E2216
		public HostExecutionContext(object state)
		{
			this.state = state;
		}

		// Token: 0x06003C42 RID: 15426 RVA: 0x000E4028 File Offset: 0x000E2228
		[SecuritySafeCritical]
		public virtual HostExecutionContext CreateCopy()
		{
			object obj = this.state;
			if (this.state is IUnknownSafeHandle)
			{
				obj = ((IUnknownSafeHandle)this.state).Clone();
			}
			return new HostExecutionContext(this.state);
		}

		// Token: 0x06003C43 RID: 15427 RVA: 0x000E4065 File Offset: 0x000E2265
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06003C44 RID: 15428 RVA: 0x000E4074 File Offset: 0x000E2274
		public virtual void Dispose(bool disposing)
		{
		}

		// Token: 0x0400199C RID: 6556
		private object state;
	}
}
