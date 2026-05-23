using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000789 RID: 1929
	internal sealed class BinaryCrossAppDomainString : IStreamable
	{
		// Token: 0x060053F3 RID: 21491 RVA: 0x00127DA0 File Offset: 0x00125FA0
		internal BinaryCrossAppDomainString()
		{
		}

		// Token: 0x060053F4 RID: 21492 RVA: 0x00127DA8 File Offset: 0x00125FA8
		public void Write(__BinaryWriter sout)
		{
			sout.WriteByte(19);
			sout.WriteInt32(this.objectId);
			sout.WriteInt32(this.value);
		}

		// Token: 0x060053F5 RID: 21493 RVA: 0x00127DCA File Offset: 0x00125FCA
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.objectId = input.ReadInt32();
			this.value = input.ReadInt32();
		}

		// Token: 0x060053F6 RID: 21494 RVA: 0x00127DE4 File Offset: 0x00125FE4
		public void Dump()
		{
		}

		// Token: 0x060053F7 RID: 21495 RVA: 0x00127DE6 File Offset: 0x00125FE6
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			BCLDebug.CheckEnabled("BINARY");
		}

		// Token: 0x040025E3 RID: 9699
		internal int objectId;

		// Token: 0x040025E4 RID: 9700
		internal int value;
	}
}
