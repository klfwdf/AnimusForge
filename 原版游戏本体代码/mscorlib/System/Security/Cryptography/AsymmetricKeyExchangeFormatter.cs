using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200024B RID: 587
	[ComVisible(true)]
	public abstract class AsymmetricKeyExchangeFormatter
	{
		// Token: 0x170003F4 RID: 1012
		// (get) Token: 0x060020E5 RID: 8421
		public abstract string Parameters { get; }

		// Token: 0x060020E6 RID: 8422
		public abstract void SetKey(AsymmetricAlgorithm key);

		// Token: 0x060020E7 RID: 8423
		public abstract byte[] CreateKeyExchange(byte[] data);

		// Token: 0x060020E8 RID: 8424
		public abstract byte[] CreateKeyExchange(byte[] data, Type symAlgType);
	}
}
