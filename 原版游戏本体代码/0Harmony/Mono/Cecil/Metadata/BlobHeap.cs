using System;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002C9 RID: 713
	internal sealed class BlobHeap : Heap
	{
		// Token: 0x06001291 RID: 4753 RVA: 0x0003AA03 File Offset: 0x00038C03
		public BlobHeap(byte[] data)
			: base(data)
		{
		}

		// Token: 0x06001292 RID: 4754 RVA: 0x0003AA0C File Offset: 0x00038C0C
		public byte[] Read(uint index)
		{
			if (index == 0U || (ulong)index > (ulong)((long)(this.data.Length - 1)))
			{
				return Empty<byte>.Array;
			}
			int position = (int)index;
			int length = (int)this.data.ReadCompressedUInt32(ref position);
			if (length > this.data.Length - position)
			{
				return Empty<byte>.Array;
			}
			byte[] buffer = new byte[length];
			Buffer.BlockCopy(this.data, position, buffer, 0, length);
			return buffer;
		}

		// Token: 0x06001293 RID: 4755 RVA: 0x0003AA6C File Offset: 0x00038C6C
		public void GetView(uint signature, out byte[] buffer, out int index, out int length)
		{
			if (signature == 0U || (ulong)signature > (ulong)((long)(this.data.Length - 1)))
			{
				buffer = null;
				index = (length = 0);
				return;
			}
			buffer = this.data;
			index = (int)signature;
			length = (int)buffer.ReadCompressedUInt32(ref index);
		}
	}
}
