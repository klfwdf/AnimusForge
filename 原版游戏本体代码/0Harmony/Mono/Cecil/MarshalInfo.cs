using System;

namespace Mono.Cecil
{
	// Token: 0x0200025C RID: 604
	internal class MarshalInfo
	{
		// Token: 0x17000323 RID: 803
		// (get) Token: 0x06000DA4 RID: 3492 RVA: 0x0002D65F File Offset: 0x0002B85F
		// (set) Token: 0x06000DA5 RID: 3493 RVA: 0x0002D667 File Offset: 0x0002B867
		public NativeType NativeType
		{
			get
			{
				return this.native;
			}
			set
			{
				this.native = value;
			}
		}

		// Token: 0x06000DA6 RID: 3494 RVA: 0x0002D670 File Offset: 0x0002B870
		public MarshalInfo(NativeType native)
		{
			this.native = native;
		}

		// Token: 0x04000409 RID: 1033
		internal NativeType native;
	}
}
