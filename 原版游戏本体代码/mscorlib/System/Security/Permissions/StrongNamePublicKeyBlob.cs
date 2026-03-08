using System;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Permissions
{
	// Token: 0x0200030B RID: 779
	[ComVisible(true)]
	[Serializable]
	public sealed class StrongNamePublicKeyBlob
	{
		// Token: 0x0600276B RID: 10091 RVA: 0x0008F5F2 File Offset: 0x0008D7F2
		internal StrongNamePublicKeyBlob()
		{
		}

		// Token: 0x0600276C RID: 10092 RVA: 0x0008F5FA File Offset: 0x0008D7FA
		public StrongNamePublicKeyBlob(byte[] publicKey)
		{
			if (publicKey == null)
			{
				throw new ArgumentNullException("PublicKey");
			}
			this.PublicKey = new byte[publicKey.Length];
			Array.Copy(publicKey, 0, this.PublicKey, 0, publicKey.Length);
		}

		// Token: 0x0600276D RID: 10093 RVA: 0x0008F62F File Offset: 0x0008D82F
		internal StrongNamePublicKeyBlob(string publicKey)
		{
			this.PublicKey = Hex.DecodeHexString(publicKey);
		}

		// Token: 0x0600276E RID: 10094 RVA: 0x0008F644 File Offset: 0x0008D844
		private static bool CompareArrays(byte[] first, byte[] second)
		{
			if (first.Length != second.Length)
			{
				return false;
			}
			int num = first.Length;
			for (int i = 0; i < num; i++)
			{
				if (first[i] != second[i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x0600276F RID: 10095 RVA: 0x0008F676 File Offset: 0x0008D876
		internal bool Equals(StrongNamePublicKeyBlob blob)
		{
			return blob != null && StrongNamePublicKeyBlob.CompareArrays(this.PublicKey, blob.PublicKey);
		}

		// Token: 0x06002770 RID: 10096 RVA: 0x0008F68E File Offset: 0x0008D88E
		public override bool Equals(object obj)
		{
			return obj != null && obj is StrongNamePublicKeyBlob && this.Equals((StrongNamePublicKeyBlob)obj);
		}

		// Token: 0x06002771 RID: 10097 RVA: 0x0008F6AC File Offset: 0x0008D8AC
		private static int GetByteArrayHashCode(byte[] baData)
		{
			if (baData == null)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < baData.Length; i++)
			{
				num = (num << 8) ^ (int)baData[i] ^ (num >> 24);
			}
			return num;
		}

		// Token: 0x06002772 RID: 10098 RVA: 0x0008F6DC File Offset: 0x0008D8DC
		public override int GetHashCode()
		{
			return StrongNamePublicKeyBlob.GetByteArrayHashCode(this.PublicKey);
		}

		// Token: 0x06002773 RID: 10099 RVA: 0x0008F6E9 File Offset: 0x0008D8E9
		public override string ToString()
		{
			return Hex.EncodeHexString(this.PublicKey);
		}

		// Token: 0x04000F4A RID: 3914
		internal byte[] PublicKey;
	}
}
