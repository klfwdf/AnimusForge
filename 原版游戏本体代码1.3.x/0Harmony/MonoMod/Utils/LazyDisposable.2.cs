using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x020008CE RID: 2254
	internal sealed class LazyDisposable<[Nullable(2)] T> : IDisposable
	{
		// Token: 0x1400000A RID: 10
		// (add) Token: 0x06002ECE RID: 11982 RVA: 0x000A10C0 File Offset: 0x0009F2C0
		// (remove) Token: 0x06002ECF RID: 11983 RVA: 0x000A10F8 File Offset: 0x0009F2F8
		[Nullable(new byte[] { 2, 1 })]
		[Nullable(new byte[] { 2, 1 })]
		public event Action<T> OnDispose;

		// Token: 0x06002ED0 RID: 11984 RVA: 0x000A112D File Offset: 0x0009F32D
		[NullableContext(1)]
		public LazyDisposable(T instance)
		{
			this.Instance = instance;
		}

		// Token: 0x06002ED1 RID: 11985 RVA: 0x000A113C File Offset: 0x0009F33C
		[NullableContext(1)]
		public LazyDisposable(T instance, Action<T> a)
			: this(instance)
		{
			this.OnDispose += a;
		}

		// Token: 0x06002ED2 RID: 11986 RVA: 0x000A114C File Offset: 0x0009F34C
		public void Dispose()
		{
			Action<T> onDispose = this.OnDispose;
			if (onDispose != null)
			{
				onDispose(this.Instance);
			}
			this.Instance = default(T);
		}

		// Token: 0x04003B43 RID: 15171
		[Nullable(2)]
		private T Instance;
	}
}
