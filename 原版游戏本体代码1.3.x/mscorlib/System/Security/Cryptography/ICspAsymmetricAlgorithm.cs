using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200026D RID: 621
	[ComVisible(true)]
	public interface ICspAsymmetricAlgorithm
	{
		// Token: 0x17000439 RID: 1081
		// (get) Token: 0x06002206 RID: 8710
		CspKeyContainerInfo CspKeyContainerInfo { get; }

		// Token: 0x06002207 RID: 8711
		byte[] ExportCspBlob(bool includePrivateParameters);

		// Token: 0x06002208 RID: 8712
		void ImportCspBlob(byte[] rawData);
	}
}
