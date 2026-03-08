using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200029A RID: 666
	[ComVisible(true)]
	public class SignatureDescription
	{
		// Token: 0x06002379 RID: 9081 RVA: 0x0008071C File Offset: 0x0007E91C
		public SignatureDescription()
		{
		}

		// Token: 0x0600237A RID: 9082 RVA: 0x00080724 File Offset: 0x0007E924
		public SignatureDescription(SecurityElement el)
		{
			if (el == null)
			{
				throw new ArgumentNullException("el");
			}
			this._strKey = el.SearchForTextOfTag("Key");
			this._strDigest = el.SearchForTextOfTag("Digest");
			this._strFormatter = el.SearchForTextOfTag("Formatter");
			this._strDeformatter = el.SearchForTextOfTag("Deformatter");
		}

		// Token: 0x17000477 RID: 1143
		// (get) Token: 0x0600237B RID: 9083 RVA: 0x00080789 File Offset: 0x0007E989
		// (set) Token: 0x0600237C RID: 9084 RVA: 0x00080791 File Offset: 0x0007E991
		public string KeyAlgorithm
		{
			get
			{
				return this._strKey;
			}
			set
			{
				this._strKey = value;
			}
		}

		// Token: 0x17000478 RID: 1144
		// (get) Token: 0x0600237D RID: 9085 RVA: 0x0008079A File Offset: 0x0007E99A
		// (set) Token: 0x0600237E RID: 9086 RVA: 0x000807A2 File Offset: 0x0007E9A2
		public string DigestAlgorithm
		{
			get
			{
				return this._strDigest;
			}
			set
			{
				this._strDigest = value;
			}
		}

		// Token: 0x17000479 RID: 1145
		// (get) Token: 0x0600237F RID: 9087 RVA: 0x000807AB File Offset: 0x0007E9AB
		// (set) Token: 0x06002380 RID: 9088 RVA: 0x000807B3 File Offset: 0x0007E9B3
		public string FormatterAlgorithm
		{
			get
			{
				return this._strFormatter;
			}
			set
			{
				this._strFormatter = value;
			}
		}

		// Token: 0x1700047A RID: 1146
		// (get) Token: 0x06002381 RID: 9089 RVA: 0x000807BC File Offset: 0x0007E9BC
		// (set) Token: 0x06002382 RID: 9090 RVA: 0x000807C4 File Offset: 0x0007E9C4
		public string DeformatterAlgorithm
		{
			get
			{
				return this._strDeformatter;
			}
			set
			{
				this._strDeformatter = value;
			}
		}

		// Token: 0x06002383 RID: 9091 RVA: 0x000807D0 File Offset: 0x0007E9D0
		public virtual AsymmetricSignatureDeformatter CreateDeformatter(AsymmetricAlgorithm key)
		{
			AsymmetricSignatureDeformatter asymmetricSignatureDeformatter = (AsymmetricSignatureDeformatter)CryptoConfig.CreateFromName(this._strDeformatter);
			asymmetricSignatureDeformatter.SetKey(key);
			return asymmetricSignatureDeformatter;
		}

		// Token: 0x06002384 RID: 9092 RVA: 0x000807F8 File Offset: 0x0007E9F8
		public virtual AsymmetricSignatureFormatter CreateFormatter(AsymmetricAlgorithm key)
		{
			AsymmetricSignatureFormatter asymmetricSignatureFormatter = (AsymmetricSignatureFormatter)CryptoConfig.CreateFromName(this._strFormatter);
			asymmetricSignatureFormatter.SetKey(key);
			return asymmetricSignatureFormatter;
		}

		// Token: 0x06002385 RID: 9093 RVA: 0x0008081E File Offset: 0x0007EA1E
		public virtual HashAlgorithm CreateDigest()
		{
			return (HashAlgorithm)CryptoConfig.CreateFromName(this._strDigest);
		}

		// Token: 0x04000CEB RID: 3307
		private string _strKey;

		// Token: 0x04000CEC RID: 3308
		private string _strDigest;

		// Token: 0x04000CED RID: 3309
		private string _strFormatter;

		// Token: 0x04000CEE RID: 3310
		private string _strDeformatter;
	}
}
