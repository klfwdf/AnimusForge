using System;

namespace Mono.Cecil
{
	// Token: 0x02000261 RID: 609
	internal sealed class FixedSysStringMarshalInfo : MarshalInfo
	{
		// Token: 0x1700032F RID: 815
		// (get) Token: 0x06000DC1 RID: 3521 RVA: 0x0002D78E File Offset: 0x0002B98E
		// (set) Token: 0x06000DC2 RID: 3522 RVA: 0x0002D796 File Offset: 0x0002B996
		public int Size
		{
			get
			{
				return this.size;
			}
			set
			{
				this.size = value;
			}
		}

		// Token: 0x06000DC3 RID: 3523 RVA: 0x0002D79F File Offset: 0x0002B99F
		public FixedSysStringMarshalInfo()
			: base(NativeType.FixedSysString)
		{
			this.size = -1;
		}

		// Token: 0x04000415 RID: 1045
		internal int size;
	}
}
