using System;
using Mono.Cecil.PE;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002CC RID: 716
	internal sealed class DataBuffer : ByteBuffer
	{
		// Token: 0x060012AB RID: 4779 RVA: 0x0003AEFC File Offset: 0x000390FC
		public DataBuffer()
			: base(0)
		{
		}

		// Token: 0x060012AC RID: 4780 RVA: 0x0003AF0C File Offset: 0x0003910C
		private void Align(int align)
		{
			align--;
			base.WriteBytes(((this.position + align) & ~align) - this.position);
		}

		// Token: 0x060012AD RID: 4781 RVA: 0x0003AF2B File Offset: 0x0003912B
		public uint AddData(byte[] data, int align)
		{
			if (this.buffer_align < align)
			{
				this.buffer_align = align;
			}
			this.Align(align);
			uint position = (uint)this.position;
			base.WriteBytes(data);
			return position;
		}

		// Token: 0x170004D1 RID: 1233
		// (get) Token: 0x060012AE RID: 4782 RVA: 0x0003AF51 File Offset: 0x00039151
		public int BufferAlign
		{
			get
			{
				return this.buffer_align;
			}
		}

		// Token: 0x040006FE RID: 1790
		private int buffer_align = 4;
	}
}
