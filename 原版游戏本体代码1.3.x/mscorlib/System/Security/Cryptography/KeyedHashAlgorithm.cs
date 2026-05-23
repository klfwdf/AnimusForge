using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200026E RID: 622
	[ComVisible(true)]
	public abstract class KeyedHashAlgorithm : HashAlgorithm
	{
		// Token: 0x0600220A RID: 8714 RVA: 0x0007875C File Offset: 0x0007695C
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this.KeyValue != null)
				{
					Array.Clear(this.KeyValue, 0, this.KeyValue.Length);
				}
				this.KeyValue = null;
			}
			base.Dispose(disposing);
		}

		// Token: 0x1700043A RID: 1082
		// (get) Token: 0x0600220B RID: 8715 RVA: 0x0007878B File Offset: 0x0007698B
		// (set) Token: 0x0600220C RID: 8716 RVA: 0x0007879D File Offset: 0x0007699D
		public virtual byte[] Key
		{
			get
			{
				return (byte[])this.KeyValue.Clone();
			}
			set
			{
				if (this.State != 0)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_HashKeySet"));
				}
				this.KeyValue = (byte[])value.Clone();
			}
		}

		// Token: 0x0600220D RID: 8717 RVA: 0x000787C8 File Offset: 0x000769C8
		public new static KeyedHashAlgorithm Create()
		{
			return KeyedHashAlgorithm.Create("System.Security.Cryptography.KeyedHashAlgorithm");
		}

		// Token: 0x0600220E RID: 8718 RVA: 0x000787D4 File Offset: 0x000769D4
		public new static KeyedHashAlgorithm Create(string algName)
		{
			return (KeyedHashAlgorithm)CryptoConfig.CreateFromName(algName);
		}

		// Token: 0x04000C5F RID: 3167
		protected byte[] KeyValue;
	}
}
