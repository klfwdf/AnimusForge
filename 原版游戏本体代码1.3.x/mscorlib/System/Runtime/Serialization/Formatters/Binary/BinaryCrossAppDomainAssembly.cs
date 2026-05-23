using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000784 RID: 1924
	internal sealed class BinaryCrossAppDomainAssembly : IStreamable
	{
		// Token: 0x060053D3 RID: 21459 RVA: 0x001270E4 File Offset: 0x001252E4
		internal BinaryCrossAppDomainAssembly()
		{
		}

		// Token: 0x060053D4 RID: 21460 RVA: 0x001270EC File Offset: 0x001252EC
		public void Write(__BinaryWriter sout)
		{
			sout.WriteByte(20);
			sout.WriteInt32(this.assemId);
			sout.WriteInt32(this.assemblyIndex);
		}

		// Token: 0x060053D5 RID: 21461 RVA: 0x0012710E File Offset: 0x0012530E
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.assemId = input.ReadInt32();
			this.assemblyIndex = input.ReadInt32();
		}

		// Token: 0x060053D6 RID: 21462 RVA: 0x00127128 File Offset: 0x00125328
		public void Dump()
		{
		}

		// Token: 0x060053D7 RID: 21463 RVA: 0x0012712A File Offset: 0x0012532A
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			BCLDebug.CheckEnabled("BINARY");
		}

		// Token: 0x040025C4 RID: 9668
		internal int assemId;

		// Token: 0x040025C5 RID: 9669
		internal int assemblyIndex;
	}
}
