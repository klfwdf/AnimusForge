using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000445 RID: 1093
	internal sealed class PdbIteratorScope : ILocalScope
	{
		// Token: 0x060017BC RID: 6076 RVA: 0x0004AD77 File Offset: 0x00048F77
		internal PdbIteratorScope(uint offset, uint length)
		{
			this.offset = offset;
			this.length = length;
		}

		// Token: 0x17000595 RID: 1429
		// (get) Token: 0x060017BD RID: 6077 RVA: 0x0004AD8D File Offset: 0x00048F8D
		public uint Offset
		{
			get
			{
				return this.offset;
			}
		}

		// Token: 0x17000596 RID: 1430
		// (get) Token: 0x060017BE RID: 6078 RVA: 0x0004AD95 File Offset: 0x00048F95
		public uint Length
		{
			get
			{
				return this.length;
			}
		}

		// Token: 0x0400104E RID: 4174
		private uint offset;

		// Token: 0x0400104F RID: 4175
		private uint length;
	}
}
