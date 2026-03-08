using System;
using System.Collections.Generic;
using Mono.Cecil.PE;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002D1 RID: 721
	internal sealed class BlobHeapBuffer : HeapBuffer
	{
		// Token: 0x170004D6 RID: 1238
		// (get) Token: 0x060012BF RID: 4799 RVA: 0x0003AFE5 File Offset: 0x000391E5
		public override bool IsEmpty
		{
			get
			{
				return this.length <= 1;
			}
		}

		// Token: 0x060012C0 RID: 4800 RVA: 0x0003B1EF File Offset: 0x000393EF
		public BlobHeapBuffer()
			: base(1)
		{
			base.WriteByte(0);
		}

		// Token: 0x060012C1 RID: 4801 RVA: 0x0003B210 File Offset: 0x00039410
		public uint GetBlobIndex(ByteBuffer blob)
		{
			uint index;
			if (this.blobs.TryGetValue(blob, out index))
			{
				return index;
			}
			index = (uint)this.position;
			this.WriteBlob(blob);
			this.blobs.Add(blob, index);
			return index;
		}

		// Token: 0x060012C2 RID: 4802 RVA: 0x0003B24B File Offset: 0x0003944B
		private void WriteBlob(ByteBuffer blob)
		{
			base.WriteCompressedUInt32((uint)blob.length);
			base.WriteBytes(blob);
		}

		// Token: 0x04000701 RID: 1793
		private readonly Dictionary<ByteBuffer, uint> blobs = new Dictionary<ByteBuffer, uint>(new ByteBufferEqualityComparer());
	}
}
