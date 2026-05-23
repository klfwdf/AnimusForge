using System;
using Mono.Cecil.PE;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002CD RID: 717
	internal abstract class HeapBuffer : ByteBuffer
	{
		// Token: 0x170004D2 RID: 1234
		// (get) Token: 0x060012AF RID: 4783 RVA: 0x0003AF59 File Offset: 0x00039159
		public bool IsLarge
		{
			get
			{
				return this.length > 65535;
			}
		}

		// Token: 0x170004D3 RID: 1235
		// (get) Token: 0x060012B0 RID: 4784
		public abstract bool IsEmpty { get; }

		// Token: 0x060012B1 RID: 4785 RVA: 0x0003AF68 File Offset: 0x00039168
		protected HeapBuffer(int length)
			: base(length)
		{
		}
	}
}
