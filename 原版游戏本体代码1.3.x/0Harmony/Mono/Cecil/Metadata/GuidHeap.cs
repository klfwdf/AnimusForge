using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002D6 RID: 726
	internal sealed class GuidHeap : Heap
	{
		// Token: 0x060012C8 RID: 4808 RVA: 0x0003AA03 File Offset: 0x00038C03
		public GuidHeap(byte[] data)
			: base(data)
		{
		}

		// Token: 0x060012C9 RID: 4809 RVA: 0x0003B324 File Offset: 0x00039524
		public Guid Read(uint index)
		{
			if (index == 0U || (ulong)(index - 1U + 16U) > (ulong)((long)this.data.Length))
			{
				return default(Guid);
			}
			byte[] buffer = new byte[16];
			Buffer.BlockCopy(this.data, (int)((index - 1U) * 16U), buffer, 0, 16);
			return new Guid(buffer);
		}
	}
}
