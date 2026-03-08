using System;
using System.Runtime.Versioning;

namespace System.Collections.Generic
{
	// Token: 0x020004DE RID: 1246
	internal static class IntrospectiveSortUtilities
	{
		// Token: 0x06003B37 RID: 15159 RVA: 0x000E0BA0 File Offset: 0x000DEDA0
		internal static int FloorLog2(int n)
		{
			int num = 0;
			while (n >= 1)
			{
				num++;
				n /= 2;
			}
			return num;
		}

		// Token: 0x06003B38 RID: 15160 RVA: 0x000E0BBF File Offset: 0x000DEDBF
		internal static void ThrowOrIgnoreBadComparer(object comparer)
		{
			if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_BogusIComparer", new object[] { comparer }));
			}
		}

		// Token: 0x04001961 RID: 6497
		internal const int IntrosortSizeThreshold = 16;

		// Token: 0x04001962 RID: 6498
		internal const int QuickSortDepthThreshold = 32;
	}
}
