using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000788 RID: 1928
	internal sealed class BinaryObjectString : IStreamable
	{
		// Token: 0x060053ED RID: 21485 RVA: 0x00127D3E File Offset: 0x00125F3E
		internal BinaryObjectString()
		{
		}

		// Token: 0x060053EE RID: 21486 RVA: 0x00127D46 File Offset: 0x00125F46
		internal void Set(int objectId, string value)
		{
			this.objectId = objectId;
			this.value = value;
		}

		// Token: 0x060053EF RID: 21487 RVA: 0x00127D56 File Offset: 0x00125F56
		public void Write(__BinaryWriter sout)
		{
			sout.WriteByte(6);
			sout.WriteInt32(this.objectId);
			sout.WriteString(this.value);
		}

		// Token: 0x060053F0 RID: 21488 RVA: 0x00127D77 File Offset: 0x00125F77
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.objectId = input.ReadInt32();
			this.value = input.ReadString();
		}

		// Token: 0x060053F1 RID: 21489 RVA: 0x00127D91 File Offset: 0x00125F91
		public void Dump()
		{
		}

		// Token: 0x060053F2 RID: 21490 RVA: 0x00127D93 File Offset: 0x00125F93
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			BCLDebug.CheckEnabled("BINARY");
		}

		// Token: 0x040025E1 RID: 9697
		internal int objectId;

		// Token: 0x040025E2 RID: 9698
		internal string value;
	}
}
