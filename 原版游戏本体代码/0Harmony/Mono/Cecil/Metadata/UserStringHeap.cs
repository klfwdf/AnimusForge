using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002E4 RID: 740
	internal sealed class UserStringHeap : StringHeap
	{
		// Token: 0x060012E1 RID: 4833 RVA: 0x0003B648 File Offset: 0x00039848
		public UserStringHeap(byte[] data)
			: base(data)
		{
		}

		// Token: 0x060012E2 RID: 4834 RVA: 0x0003B654 File Offset: 0x00039854
		protected override string ReadStringAt(uint index)
		{
			int start = (int)index;
			uint length = (uint)((ulong)this.data.ReadCompressedUInt32(ref start) & 18446744073709551614UL);
			if (length < 1U)
			{
				return string.Empty;
			}
			char[] chars = new char[length / 2U];
			int i = start;
			int j = 0;
			while ((long)i < (long)start + (long)((ulong)length))
			{
				chars[j++] = (char)((int)this.data[i] | ((int)this.data[i + 1] << 8));
				i += 2;
			}
			return new string(chars);
		}
	}
}
