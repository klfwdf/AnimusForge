using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200043E RID: 1086
	internal struct PdbLine
	{
		// Token: 0x060017AF RID: 6063 RVA: 0x0004A921 File Offset: 0x00048B21
		internal PdbLine(uint offset, uint lineBegin, ushort colBegin, uint lineEnd, ushort colEnd)
		{
			this.offset = offset;
			this.lineBegin = lineBegin;
			this.colBegin = colBegin;
			this.lineEnd = lineEnd;
			this.colEnd = colEnd;
		}

		// Token: 0x0400102C RID: 4140
		internal uint offset;

		// Token: 0x0400102D RID: 4141
		internal uint lineBegin;

		// Token: 0x0400102E RID: 4142
		internal uint lineEnd;

		// Token: 0x0400102F RID: 4143
		internal ushort colBegin;

		// Token: 0x04001030 RID: 4144
		internal ushort colEnd;
	}
}
