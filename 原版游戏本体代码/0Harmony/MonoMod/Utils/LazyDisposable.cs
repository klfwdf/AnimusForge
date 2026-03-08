using System;
using System.Runtime.CompilerServices;

namespace MonoMod.Utils
{
	// Token: 0x020008CD RID: 2253
	[NullableContext(2)]
	[Nullable(0)]
	internal sealed class LazyDisposable : IDisposable
	{
		// Token: 0x14000009 RID: 9
		// (add) Token: 0x06002EC9 RID: 11977 RVA: 0x000A1030 File Offset: 0x0009F230
		// (remove) Token: 0x06002ECA RID: 11978 RVA: 0x000A1068 File Offset: 0x0009F268
		public event Action OnDispose;

		// Token: 0x06002ECB RID: 11979 RVA: 0x00002B15 File Offset: 0x00000D15
		public LazyDisposable()
		{
		}

		// Token: 0x06002ECC RID: 11980 RVA: 0x000A109D File Offset: 0x0009F29D
		[NullableContext(1)]
		public LazyDisposable(Action a)
			: this()
		{
			this.OnDispose += a;
		}

		// Token: 0x06002ECD RID: 11981 RVA: 0x000A10AC File Offset: 0x0009F2AC
		public void Dispose()
		{
			Action onDispose = this.OnDispose;
			if (onDispose == null)
			{
				return;
			}
			onDispose();
		}
	}
}
