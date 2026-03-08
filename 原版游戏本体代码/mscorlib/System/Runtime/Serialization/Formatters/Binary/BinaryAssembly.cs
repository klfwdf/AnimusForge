using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000783 RID: 1923
	internal sealed class BinaryAssembly : IStreamable
	{
		// Token: 0x060053CD RID: 21453 RVA: 0x00127081 File Offset: 0x00125281
		internal BinaryAssembly()
		{
		}

		// Token: 0x060053CE RID: 21454 RVA: 0x00127089 File Offset: 0x00125289
		internal void Set(int assemId, string assemblyString)
		{
			this.assemId = assemId;
			this.assemblyString = assemblyString;
		}

		// Token: 0x060053CF RID: 21455 RVA: 0x00127099 File Offset: 0x00125299
		public void Write(__BinaryWriter sout)
		{
			sout.WriteByte(12);
			sout.WriteInt32(this.assemId);
			sout.WriteString(this.assemblyString);
		}

		// Token: 0x060053D0 RID: 21456 RVA: 0x001270BB File Offset: 0x001252BB
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.assemId = input.ReadInt32();
			this.assemblyString = input.ReadString();
		}

		// Token: 0x060053D1 RID: 21457 RVA: 0x001270D5 File Offset: 0x001252D5
		public void Dump()
		{
		}

		// Token: 0x060053D2 RID: 21458 RVA: 0x001270D7 File Offset: 0x001252D7
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			BCLDebug.CheckEnabled("BINARY");
		}

		// Token: 0x040025C2 RID: 9666
		internal int assemId;

		// Token: 0x040025C3 RID: 9667
		internal string assemblyString;
	}
}
