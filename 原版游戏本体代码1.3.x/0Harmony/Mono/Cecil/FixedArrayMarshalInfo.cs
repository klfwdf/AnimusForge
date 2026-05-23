using System;

namespace Mono.Cecil
{
	// Token: 0x02000260 RID: 608
	internal sealed class FixedArrayMarshalInfo : MarshalInfo
	{
		// Token: 0x1700032D RID: 813
		// (get) Token: 0x06000DBC RID: 3516 RVA: 0x0002D75A File Offset: 0x0002B95A
		// (set) Token: 0x06000DBD RID: 3517 RVA: 0x0002D762 File Offset: 0x0002B962
		public NativeType ElementType
		{
			get
			{
				return this.element_type;
			}
			set
			{
				this.element_type = value;
			}
		}

		// Token: 0x1700032E RID: 814
		// (get) Token: 0x06000DBE RID: 3518 RVA: 0x0002D76B File Offset: 0x0002B96B
		// (set) Token: 0x06000DBF RID: 3519 RVA: 0x0002D773 File Offset: 0x0002B973
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

		// Token: 0x06000DC0 RID: 3520 RVA: 0x0002D77C File Offset: 0x0002B97C
		public FixedArrayMarshalInfo()
			: base(NativeType.FixedArray)
		{
			this.element_type = NativeType.None;
		}

		// Token: 0x04000413 RID: 1043
		internal NativeType element_type;

		// Token: 0x04000414 RID: 1044
		internal int size;
	}
}
