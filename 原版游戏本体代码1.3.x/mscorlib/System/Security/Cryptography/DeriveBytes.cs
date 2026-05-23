using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200025B RID: 603
	[ComVisible(true)]
	public abstract class DeriveBytes : IDisposable
	{
		// Token: 0x06002165 RID: 8549
		public abstract byte[] GetBytes(int cb);

		// Token: 0x06002166 RID: 8550
		public abstract void Reset();

		// Token: 0x06002167 RID: 8551 RVA: 0x000764B1 File Offset: 0x000746B1
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		// Token: 0x06002168 RID: 8552 RVA: 0x000764C0 File Offset: 0x000746C0
		protected virtual void Dispose(bool disposing)
		{
		}
	}
}
