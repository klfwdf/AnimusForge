using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
	// Token: 0x0200026F RID: 623
	[ComVisible(true)]
	public class MACTripleDES : KeyedHashAlgorithm
	{
		// Token: 0x0600220F RID: 8719 RVA: 0x000787E4 File Offset: 0x000769E4
		public MACTripleDES()
		{
			this.KeyValue = new byte[24];
			Utils.StaticRandomNumberGenerator.GetBytes(this.KeyValue);
			this.des = TripleDES.Create();
			this.HashSizeValue = this.des.BlockSize;
			this.m_bytesPerBlock = this.des.BlockSize / 8;
			this.des.IV = new byte[this.m_bytesPerBlock];
			this.des.Padding = PaddingMode.Zeros;
			this.m_encryptor = null;
		}

		// Token: 0x06002210 RID: 8720 RVA: 0x0007886C File Offset: 0x00076A6C
		public MACTripleDES(byte[] rgbKey)
			: this("System.Security.Cryptography.TripleDES", rgbKey)
		{
		}

		// Token: 0x06002211 RID: 8721 RVA: 0x0007887C File Offset: 0x00076A7C
		public MACTripleDES(string strTripleDES, byte[] rgbKey)
		{
			if (rgbKey == null)
			{
				throw new ArgumentNullException("rgbKey");
			}
			if (strTripleDES == null)
			{
				this.des = TripleDES.Create();
			}
			else
			{
				this.des = TripleDES.Create(strTripleDES);
			}
			this.HashSizeValue = this.des.BlockSize;
			this.KeyValue = (byte[])rgbKey.Clone();
			this.m_bytesPerBlock = this.des.BlockSize / 8;
			this.des.IV = new byte[this.m_bytesPerBlock];
			this.des.Padding = PaddingMode.Zeros;
			this.m_encryptor = null;
		}

		// Token: 0x06002212 RID: 8722 RVA: 0x00078917 File Offset: 0x00076B17
		public override void Initialize()
		{
			this.m_encryptor = null;
		}

		// Token: 0x1700043B RID: 1083
		// (get) Token: 0x06002213 RID: 8723 RVA: 0x00078920 File Offset: 0x00076B20
		// (set) Token: 0x06002214 RID: 8724 RVA: 0x0007892D File Offset: 0x00076B2D
		[ComVisible(false)]
		public PaddingMode Padding
		{
			get
			{
				return this.des.Padding;
			}
			set
			{
				if (value < PaddingMode.None || PaddingMode.ISO10126 < value)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidPaddingMode"));
				}
				this.des.Padding = value;
			}
		}

		// Token: 0x06002215 RID: 8725 RVA: 0x00078954 File Offset: 0x00076B54
		protected override void HashCore(byte[] rgbData, int ibStart, int cbSize)
		{
			if (this.m_encryptor == null)
			{
				this.des.Key = this.Key;
				this.m_encryptor = this.des.CreateEncryptor();
				this._ts = new TailStream(this.des.BlockSize / 8);
				this._cs = new CryptoStream(this._ts, this.m_encryptor, CryptoStreamMode.Write);
			}
			this._cs.Write(rgbData, ibStart, cbSize);
		}

		// Token: 0x06002216 RID: 8726 RVA: 0x000789CC File Offset: 0x00076BCC
		protected override byte[] HashFinal()
		{
			if (this.m_encryptor == null)
			{
				this.des.Key = this.Key;
				this.m_encryptor = this.des.CreateEncryptor();
				this._ts = new TailStream(this.des.BlockSize / 8);
				this._cs = new CryptoStream(this._ts, this.m_encryptor, CryptoStreamMode.Write);
			}
			this._cs.FlushFinalBlock();
			return this._ts.Buffer;
		}

		// Token: 0x06002217 RID: 8727 RVA: 0x00078A4C File Offset: 0x00076C4C
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this.des != null)
				{
					this.des.Clear();
				}
				if (this.m_encryptor != null)
				{
					this.m_encryptor.Dispose();
				}
				if (this._cs != null)
				{
					this._cs.Clear();
				}
				if (this._ts != null)
				{
					this._ts.Clear();
				}
			}
			base.Dispose(disposing);
		}

		// Token: 0x04000C60 RID: 3168
		private ICryptoTransform m_encryptor;

		// Token: 0x04000C61 RID: 3169
		private CryptoStream _cs;

		// Token: 0x04000C62 RID: 3170
		private TailStream _ts;

		// Token: 0x04000C63 RID: 3171
		private const int m_bitsPerByte = 8;

		// Token: 0x04000C64 RID: 3172
		private int m_bytesPerBlock;

		// Token: 0x04000C65 RID: 3173
		private TripleDES des;
	}
}
