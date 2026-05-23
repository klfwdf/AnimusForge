using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000376 RID: 886
	internal struct CV_PRIMITIVE_TYPE
	{
		// Token: 0x04000BA3 RID: 2979
		private const uint CV_MMASK = 1792U;

		// Token: 0x04000BA4 RID: 2980
		private const uint CV_TMASK = 240U;

		// Token: 0x04000BA5 RID: 2981
		private const uint CV_SMASK = 15U;

		// Token: 0x04000BA6 RID: 2982
		private const int CV_MSHIFT = 8;

		// Token: 0x04000BA7 RID: 2983
		private const int CV_TSHIFT = 4;

		// Token: 0x04000BA8 RID: 2984
		private const int CV_SSHIFT = 0;

		// Token: 0x04000BA9 RID: 2985
		private const uint CV_FIRST_NONPRIM = 4096U;
	}
}
