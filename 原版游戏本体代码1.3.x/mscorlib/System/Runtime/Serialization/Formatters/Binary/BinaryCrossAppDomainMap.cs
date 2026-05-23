using System;
using System.Diagnostics;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x0200078A RID: 1930
	internal sealed class BinaryCrossAppDomainMap : IStreamable
	{
		// Token: 0x060053F8 RID: 21496 RVA: 0x00127DF3 File Offset: 0x00125FF3
		internal BinaryCrossAppDomainMap()
		{
		}

		// Token: 0x060053F9 RID: 21497 RVA: 0x00127DFB File Offset: 0x00125FFB
		public void Write(__BinaryWriter sout)
		{
			sout.WriteByte(18);
			sout.WriteInt32(this.crossAppDomainArrayIndex);
		}

		// Token: 0x060053FA RID: 21498 RVA: 0x00127E11 File Offset: 0x00126011
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
			this.crossAppDomainArrayIndex = input.ReadInt32();
		}

		// Token: 0x060053FB RID: 21499 RVA: 0x00127E1F File Offset: 0x0012601F
		public void Dump()
		{
		}

		// Token: 0x060053FC RID: 21500 RVA: 0x00127E21 File Offset: 0x00126021
		[Conditional("_LOGGING")]
		private void DumpInternal()
		{
			BCLDebug.CheckEnabled("BINARY");
		}

		// Token: 0x040025E5 RID: 9701
		internal int crossAppDomainArrayIndex;
	}
}
