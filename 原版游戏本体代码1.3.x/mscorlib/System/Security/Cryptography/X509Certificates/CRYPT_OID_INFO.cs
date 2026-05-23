using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002AC RID: 684
	internal struct CRYPT_OID_INFO
	{
		// Token: 0x04000D86 RID: 3462
		internal int cbSize;

		// Token: 0x04000D87 RID: 3463
		[MarshalAs(UnmanagedType.LPStr)]
		internal string pszOID;

		// Token: 0x04000D88 RID: 3464
		[MarshalAs(UnmanagedType.LPWStr)]
		internal string pwszName;

		// Token: 0x04000D89 RID: 3465
		internal OidGroup dwGroupId;

		// Token: 0x04000D8A RID: 3466
		internal int AlgId;

		// Token: 0x04000D8B RID: 3467
		internal int cbData;

		// Token: 0x04000D8C RID: 3468
		internal IntPtr pbData;
	}
}
