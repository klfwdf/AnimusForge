using System;
using System.Collections.Generic;

namespace Mono.Cecil.Metadata
{
	// Token: 0x020002CE RID: 718
	internal sealed class GuidHeapBuffer : HeapBuffer
	{
		// Token: 0x170004D4 RID: 1236
		// (get) Token: 0x060012B2 RID: 4786 RVA: 0x0003AF71 File Offset: 0x00039171
		public override bool IsEmpty
		{
			get
			{
				return this.length == 0;
			}
		}

		// Token: 0x060012B3 RID: 4787 RVA: 0x0003AF7C File Offset: 0x0003917C
		public GuidHeapBuffer()
			: base(16)
		{
		}

		// Token: 0x060012B4 RID: 4788 RVA: 0x0003AF94 File Offset: 0x00039194
		public uint GetGuidIndex(Guid guid)
		{
			uint index;
			if (this.guids.TryGetValue(guid, out index))
			{
				return index;
			}
			index = (uint)(this.guids.Count + 1);
			this.WriteGuid(guid);
			this.guids.Add(guid, index);
			return index;
		}

		// Token: 0x060012B5 RID: 4789 RVA: 0x0003AFD6 File Offset: 0x000391D6
		private void WriteGuid(Guid guid)
		{
			base.WriteBytes(guid.ToByteArray());
		}

		// Token: 0x040006FF RID: 1791
		private readonly Dictionary<Guid, uint> guids = new Dictionary<Guid, uint>();
	}
}
