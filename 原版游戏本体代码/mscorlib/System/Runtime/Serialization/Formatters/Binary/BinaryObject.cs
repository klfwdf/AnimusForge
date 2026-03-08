using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000785 RID: 1925
	internal sealed class BinaryObject : IStreamable
	{
		// Token: 0x060053D8 RID: 21464 RVA: 0x00127137 File Offset: 0x00125337
		internal BinaryObject()
		{
		}

		// Token: 0x060053D9 RID: 21465 RVA: 0x0012713F File Offset: 0x0012533F
		internal void Set(int objectId, int mapId)
		{
			this.objectId = objectId;
			this.mapId = mapId;
		}

		// Token: 0x060053DA RID: 21466 RVA: 0x0012714F File Offset: 0x0012534F
		public void Write(__BinaryWriter sout)
		{
			sout.WriteByte(1);
			sout.WriteInt32(this.objectId);
			sout.WriteInt32(this.mapId);
		}

		// Token: 0x060053DB RID: 21467 RVA: 0x00127170 File Offset: 0x00125370
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.objectId = input.ReadInt32();
			this.mapId = input.ReadInt32();
		}

		// Token: 0x060053DC RID: 21468 RVA: 0x0012718A File Offset: 0x0012538A
		public void Dump()
		{
		}

		// Token: 0x060053DD RID: 21469 RVA: 0x0012718C File Offset: 0x0012538C
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			BCLDebug.CheckEnabled("BINARY");
		}

		// Token: 0x040025C6 RID: 9670
		internal int objectId;

		// Token: 0x040025C7 RID: 9671
		internal int mapId;
	}
}
