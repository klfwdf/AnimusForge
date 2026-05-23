using System;

namespace System.Runtime.Remoting
{
	// Token: 0x020007B2 RID: 1970
	internal struct IdOps
	{
		// Token: 0x0600556F RID: 21871 RVA: 0x0012F766 File Offset: 0x0012D966
		internal static bool bStrongIdentity(int flags)
		{
			return (flags & 2) != 0;
		}

		// Token: 0x06005570 RID: 21872 RVA: 0x0012F76E File Offset: 0x0012D96E
		internal static bool bIsInitializing(int flags)
		{
			return (flags & 4) != 0;
		}

		// Token: 0x0400275A RID: 10074
		internal const int None = 0;

		// Token: 0x0400275B RID: 10075
		internal const int GenerateURI = 1;

		// Token: 0x0400275C RID: 10076
		internal const int StrongIdentity = 2;

		// Token: 0x0400275D RID: 10077
		internal const int IsInitializing = 4;
	}
}
