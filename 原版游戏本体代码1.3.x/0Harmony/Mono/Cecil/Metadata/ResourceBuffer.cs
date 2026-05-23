using System;
using Mono.Cecil.PE;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002CB RID: 715
	internal sealed class ResourceBuffer : ByteBuffer
	{
		// Token: 0x060012A9 RID: 4777 RVA: 0x0003AEDB File Offset: 0x000390DB
		public ResourceBuffer()
			: base(0)
		{
		}

		// Token: 0x060012AA RID: 4778 RVA: 0x0003AEE4 File Offset: 0x000390E4
		public uint AddResource(byte[] resource)
		{
			uint position = (uint)this.position;
			base.WriteInt32(resource.Length);
			base.WriteBytes(resource);
			return position;
		}
	}
}
