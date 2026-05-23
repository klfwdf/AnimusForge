using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200024A RID: 586
	[ComVisible(true)]
	public abstract class AsymmetricKeyExchangeDeformatter
	{
		// Token: 0x170003F3 RID: 1011
		// (get) Token: 0x060020E0 RID: 8416
		// (set) Token: 0x060020E1 RID: 8417
		public abstract string Parameters { get; set; }

		// Token: 0x060020E2 RID: 8418
		public abstract void SetKey(AsymmetricAlgorithm key);

		// Token: 0x060020E3 RID: 8419
		public abstract byte[] DecryptKeyExchange(byte[] rgb);
	}
}
