using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200043C RID: 1084
	internal class PdbSynchronizationPoint
	{
		// Token: 0x060017AB RID: 6059 RVA: 0x0004A8E5 File Offset: 0x00048AE5
		internal PdbSynchronizationPoint(BitAccess bits)
		{
			bits.ReadUInt32(out this.synchronizeOffset);
			bits.ReadUInt32(out this.continuationMethodToken);
			bits.ReadUInt32(out this.continuationOffset);
		}

		// Token: 0x17000593 RID: 1427
		// (get) Token: 0x060017AC RID: 6060 RVA: 0x0004A911 File Offset: 0x00048B11
		public uint SynchronizeOffset
		{
			get
			{
				return this.synchronizeOffset;
			}
		}

		// Token: 0x17000594 RID: 1428
		// (get) Token: 0x060017AD RID: 6061 RVA: 0x0004A919 File Offset: 0x00048B19
		public uint ContinuationOffset
		{
			get
			{
				return this.continuationOffset;
			}
		}

		// Token: 0x04001023 RID: 4131
		internal uint synchronizeOffset;

		// Token: 0x04001024 RID: 4132
		internal uint continuationMethodToken;

		// Token: 0x04001025 RID: 4133
		internal uint continuationOffset;
	}
}
