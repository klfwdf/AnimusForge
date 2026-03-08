using System;
using System.Collections.Generic;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200043D RID: 1085
	internal class PdbInfo
	{
		// Token: 0x04001026 RID: 4134
		public PdbFunction[] Functions;

		// Token: 0x04001027 RID: 4135
		public Dictionary<uint, PdbTokenLine> TokenToSourceMapping;

		// Token: 0x04001028 RID: 4136
		public string SourceServerData;

		// Token: 0x04001029 RID: 4137
		public int Age;

		// Token: 0x0400102A RID: 4138
		public Guid Guid;

		// Token: 0x0400102B RID: 4139
		public byte[] SourceLinkData;
	}
}
