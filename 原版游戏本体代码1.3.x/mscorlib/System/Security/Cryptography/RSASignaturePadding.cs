using System;

namespace System.Security.Cryptography
{
	// Token: 0x0200027E RID: 638
	public sealed class RSASignaturePadding : IEquatable<RSASignaturePadding>
	{
		// Token: 0x060022A6 RID: 8870 RVA: 0x0007CD3B File Offset: 0x0007AF3B
		private RSASignaturePadding(RSASignaturePaddingMode mode)
		{
			this._mode = mode;
		}

		// Token: 0x17000450 RID: 1104
		// (get) Token: 0x060022A7 RID: 8871 RVA: 0x0007CD4A File Offset: 0x0007AF4A
		public static RSASignaturePadding Pkcs1
		{
			get
			{
				return RSASignaturePadding.s_pkcs1;
			}
		}

		// Token: 0x17000451 RID: 1105
		// (get) Token: 0x060022A8 RID: 8872 RVA: 0x0007CD51 File Offset: 0x0007AF51
		public static RSASignaturePadding Pss
		{
			get
			{
				return RSASignaturePadding.s_pss;
			}
		}

		// Token: 0x17000452 RID: 1106
		// (get) Token: 0x060022A9 RID: 8873 RVA: 0x0007CD58 File Offset: 0x0007AF58
		public RSASignaturePaddingMode Mode
		{
			get
			{
				return this._mode;
			}
		}

		// Token: 0x060022AA RID: 8874 RVA: 0x0007CD60 File Offset: 0x0007AF60
		public override int GetHashCode()
		{
			return this._mode.GetHashCode();
		}

		// Token: 0x060022AB RID: 8875 RVA: 0x0007CD81 File Offset: 0x0007AF81
		public override bool Equals(object obj)
		{
			return this.Equals(obj as RSASignaturePadding);
		}

		// Token: 0x060022AC RID: 8876 RVA: 0x0007CD8F File Offset: 0x0007AF8F
		public bool Equals(RSASignaturePadding other)
		{
			return other != null && this._mode == other._mode;
		}

		// Token: 0x060022AD RID: 8877 RVA: 0x0007CDAA File Offset: 0x0007AFAA
		public static bool operator ==(RSASignaturePadding left, RSASignaturePadding right)
		{
			if (left == null)
			{
				return right == null;
			}
			return left.Equals(right);
		}

		// Token: 0x060022AE RID: 8878 RVA: 0x0007CDBB File Offset: 0x0007AFBB
		public static bool operator !=(RSASignaturePadding left, RSASignaturePadding right)
		{
			return !(left == right);
		}

		// Token: 0x060022AF RID: 8879 RVA: 0x0007CDC8 File Offset: 0x0007AFC8
		public override string ToString()
		{
			return this._mode.ToString();
		}

		// Token: 0x04000C96 RID: 3222
		private static readonly RSASignaturePadding s_pkcs1 = new RSASignaturePadding(RSASignaturePaddingMode.Pkcs1);

		// Token: 0x04000C97 RID: 3223
		private static readonly RSASignaturePadding s_pss = new RSASignaturePadding(RSASignaturePaddingMode.Pss);

		// Token: 0x04000C98 RID: 3224
		private readonly RSASignaturePaddingMode _mode;
	}
}
