using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200043B RID: 1083
	internal class PdbSynchronizationInformation
	{
		// Token: 0x060017A9 RID: 6057 RVA: 0x0004A884 File Offset: 0x00048A84
		internal PdbSynchronizationInformation(BitAccess bits)
		{
			bits.ReadUInt32(out this.kickoffMethodToken);
			bits.ReadUInt32(out this.generatedCatchHandlerIlOffset);
			uint asyncStepInfoCount;
			bits.ReadUInt32(out asyncStepInfoCount);
			this.synchronizationPoints = new PdbSynchronizationPoint[asyncStepInfoCount];
			for (uint i = 0U; i < asyncStepInfoCount; i += 1U)
			{
				this.synchronizationPoints[(int)i] = new PdbSynchronizationPoint(bits);
			}
		}

		// Token: 0x17000592 RID: 1426
		// (get) Token: 0x060017AA RID: 6058 RVA: 0x0004A8DD File Offset: 0x00048ADD
		public uint GeneratedCatchHandlerOffset
		{
			get
			{
				return this.generatedCatchHandlerIlOffset;
			}
		}

		// Token: 0x04001020 RID: 4128
		internal uint kickoffMethodToken;

		// Token: 0x04001021 RID: 4129
		internal uint generatedCatchHandlerIlOffset;

		// Token: 0x04001022 RID: 4130
		internal PdbSynchronizationPoint[] synchronizationPoints;
	}
}
