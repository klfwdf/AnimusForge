using System;

namespace Mono
{
	// Token: 0x020001D9 RID: 473
	internal struct Disposable<T> : IDisposable where T : class, IDisposable
	{
		// Token: 0x0600088D RID: 2189 RVA: 0x0001B856 File Offset: 0x00019A56
		public Disposable(T value, bool owned)
		{
			this.value = value;
			this.owned = owned;
		}

		// Token: 0x0600088E RID: 2190 RVA: 0x0001B866 File Offset: 0x00019A66
		public void Dispose()
		{
			if (this.value != null && this.owned)
			{
				this.value.Dispose();
			}
		}

		// Token: 0x040002DF RID: 735
		internal readonly T value;

		// Token: 0x040002E0 RID: 736
		private readonly bool owned;
	}
}
