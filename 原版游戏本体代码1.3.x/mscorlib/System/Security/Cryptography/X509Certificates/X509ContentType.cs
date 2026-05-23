using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002AE RID: 686
	[ComVisible(true)]
	public enum X509ContentType
	{
		// Token: 0x04000D8E RID: 3470
		Unknown,
		// Token: 0x04000D8F RID: 3471
		Cert,
		// Token: 0x04000D90 RID: 3472
		SerializedCert,
		// Token: 0x04000D91 RID: 3473
		Pfx,
		// Token: 0x04000D92 RID: 3474
		Pkcs12 = 3,
		// Token: 0x04000D93 RID: 3475
		SerializedStore,
		// Token: 0x04000D94 RID: 3476
		Pkcs7,
		// Token: 0x04000D95 RID: 3477
		Authenticode
	}
}
