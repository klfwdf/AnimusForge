using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace System.Security.Cryptography
{
	// Token: 0x02000279 RID: 633
	[ComVisible(true)]
	public class Rfc2898DeriveBytes : DeriveBytes
	{
		// Token: 0x06002267 RID: 8807 RVA: 0x00079C1F File Offset: 0x00077E1F
		public Rfc2898DeriveBytes(string password, int saltSize)
			: this(password, saltSize, 1000)
		{
		}

		// Token: 0x06002268 RID: 8808 RVA: 0x00079C2E File Offset: 0x00077E2E
		public Rfc2898DeriveBytes(string password, int saltSize, int iterations)
			: this(password, saltSize, iterations, HashAlgorithmName.SHA1)
		{
		}

		// Token: 0x06002269 RID: 8809 RVA: 0x00079C40 File Offset: 0x00077E40
		[SecuritySafeCritical]
		public Rfc2898DeriveBytes(string password, int saltSize, int iterations, HashAlgorithmName hashAlgorithm)
		{
			this.m_cspParams = new CspParameters();
			base..ctor();
			if (saltSize < 0)
			{
				throw new ArgumentOutOfRangeException("saltSize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (string.IsNullOrEmpty(hashAlgorithm.Name))
			{
				throw new ArgumentException(Environment.GetResourceString("Cryptography_HashAlgorithmNameNullOrEmpty"), "hashAlgorithm");
			}
			HMAC hmac = HMAC.Create("HMAC" + hashAlgorithm.Name);
			if (hmac == null)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_UnknownHashAlgorithm", new object[] { hashAlgorithm.Name }));
			}
			byte[] array = new byte[saltSize];
			Utils.StaticRandomNumberGenerator.GetBytes(array);
			this.Salt = array;
			this.IterationCount = iterations;
			this.m_password = new UTF8Encoding(false).GetBytes(password);
			hmac.Key = this.m_password;
			this.m_hmac = hmac;
			this.m_blockSize = hmac.HashSize >> 3;
			this.Initialize();
		}

		// Token: 0x0600226A RID: 8810 RVA: 0x00079D2D File Offset: 0x00077F2D
		public Rfc2898DeriveBytes(string password, byte[] salt)
			: this(password, salt, 1000)
		{
		}

		// Token: 0x0600226B RID: 8811 RVA: 0x00079D3C File Offset: 0x00077F3C
		public Rfc2898DeriveBytes(string password, byte[] salt, int iterations)
			: this(password, salt, iterations, HashAlgorithmName.SHA1)
		{
		}

		// Token: 0x0600226C RID: 8812 RVA: 0x00079D4C File Offset: 0x00077F4C
		public Rfc2898DeriveBytes(string password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm)
			: this(new UTF8Encoding(false).GetBytes(password), salt, iterations, hashAlgorithm)
		{
		}

		// Token: 0x0600226D RID: 8813 RVA: 0x00079D64 File Offset: 0x00077F64
		public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations)
			: this(password, salt, iterations, HashAlgorithmName.SHA1)
		{
		}

		// Token: 0x0600226E RID: 8814 RVA: 0x00079D74 File Offset: 0x00077F74
		[SecuritySafeCritical]
		public Rfc2898DeriveBytes(byte[] password, byte[] salt, int iterations, HashAlgorithmName hashAlgorithm)
		{
			this.m_cspParams = new CspParameters();
			base..ctor();
			if (string.IsNullOrEmpty(hashAlgorithm.Name))
			{
				throw new ArgumentException(Environment.GetResourceString("Cryptography_HashAlgorithmNameNullOrEmpty"), "hashAlgorithm");
			}
			HMAC hmac = HMAC.Create("HMAC" + hashAlgorithm.Name);
			if (hmac == null)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_UnknownHashAlgorithm", new object[] { hashAlgorithm.Name }));
			}
			this.Salt = salt;
			this.IterationCount = iterations;
			this.m_password = password;
			hmac.Key = password;
			this.m_hmac = hmac;
			this.m_blockSize = hmac.HashSize >> 3;
			this.Initialize();
		}

		// Token: 0x1700044B RID: 1099
		// (get) Token: 0x0600226F RID: 8815 RVA: 0x00079E26 File Offset: 0x00078026
		// (set) Token: 0x06002270 RID: 8816 RVA: 0x00079E2E File Offset: 0x0007802E
		public int IterationCount
		{
			get
			{
				return (int)this.m_iterations;
			}
			set
			{
				if (value <= 0)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
				}
				this.m_iterations = (uint)value;
				this.Initialize();
			}
		}

		// Token: 0x1700044C RID: 1100
		// (get) Token: 0x06002271 RID: 8817 RVA: 0x00079E56 File Offset: 0x00078056
		// (set) Token: 0x06002272 RID: 8818 RVA: 0x00079E68 File Offset: 0x00078068
		public byte[] Salt
		{
			get
			{
				return (byte[])this.m_salt.Clone();
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (value.Length < 8)
				{
					throw new ArgumentException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_FewBytesSalt"));
				}
				this.m_salt = (byte[])value.Clone();
				this.Initialize();
			}
		}

		// Token: 0x06002273 RID: 8819 RVA: 0x00079EA8 File Offset: 0x000780A8
		public override byte[] GetBytes(int cb)
		{
			if (cb <= 0)
			{
				throw new ArgumentOutOfRangeException("cb", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			byte[] array = new byte[cb];
			int i = 0;
			int num = this.m_endIndex - this.m_startIndex;
			if (num > 0)
			{
				if (cb < num)
				{
					Buffer.InternalBlockCopy(this.m_buffer, this.m_startIndex, array, 0, cb);
					this.m_startIndex += cb;
					return array;
				}
				Buffer.InternalBlockCopy(this.m_buffer, this.m_startIndex, array, 0, num);
				this.m_startIndex = (this.m_endIndex = 0);
				i += num;
			}
			while (i < cb)
			{
				byte[] src = this.Func();
				int num2 = cb - i;
				if (num2 <= this.m_blockSize)
				{
					Buffer.InternalBlockCopy(src, 0, array, i, num2);
					i += num2;
					Buffer.InternalBlockCopy(src, num2, this.m_buffer, this.m_startIndex, this.m_blockSize - num2);
					this.m_endIndex += this.m_blockSize - num2;
					return array;
				}
				Buffer.InternalBlockCopy(src, 0, array, i, this.m_blockSize);
				i += this.m_blockSize;
			}
			return array;
		}

		// Token: 0x06002274 RID: 8820 RVA: 0x00079FBF File Offset: 0x000781BF
		public override void Reset()
		{
			this.Initialize();
		}

		// Token: 0x06002275 RID: 8821 RVA: 0x00079FC8 File Offset: 0x000781C8
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				if (this.m_hmac != null)
				{
					((IDisposable)this.m_hmac).Dispose();
				}
				if (this.m_buffer != null)
				{
					Array.Clear(this.m_buffer, 0, this.m_buffer.Length);
				}
				if (this.m_salt != null)
				{
					Array.Clear(this.m_salt, 0, this.m_salt.Length);
				}
			}
		}

		// Token: 0x06002276 RID: 8822 RVA: 0x0007A02C File Offset: 0x0007822C
		private void Initialize()
		{
			if (this.m_buffer != null)
			{
				Array.Clear(this.m_buffer, 0, this.m_buffer.Length);
			}
			this.m_buffer = new byte[this.m_blockSize];
			this.m_block = 1U;
			this.m_startIndex = (this.m_endIndex = 0);
		}

		// Token: 0x06002277 RID: 8823 RVA: 0x0007A080 File Offset: 0x00078280
		private byte[] Func()
		{
			byte[] array = Utils.Int(this.m_block);
			this.m_hmac.TransformBlock(this.m_salt, 0, this.m_salt.Length, null, 0);
			this.m_hmac.TransformBlock(array, 0, array.Length, null, 0);
			this.m_hmac.TransformFinalBlock(EmptyArray<byte>.Value, 0, 0);
			byte[] hashValue = this.m_hmac.HashValue;
			this.m_hmac.Initialize();
			byte[] array2 = hashValue;
			int num = 2;
			while ((long)num <= (long)((ulong)this.m_iterations))
			{
				this.m_hmac.TransformBlock(hashValue, 0, hashValue.Length, null, 0);
				this.m_hmac.TransformFinalBlock(EmptyArray<byte>.Value, 0, 0);
				hashValue = this.m_hmac.HashValue;
				for (int i = 0; i < this.m_blockSize; i++)
				{
					byte[] array3 = array2;
					int num2 = i;
					array3[num2] ^= hashValue[i];
				}
				this.m_hmac.Initialize();
				num++;
			}
			this.m_block += 1U;
			return array2;
		}

		// Token: 0x06002278 RID: 8824 RVA: 0x0007A17C File Offset: 0x0007837C
		[SecuritySafeCritical]
		public byte[] CryptDeriveKey(string algname, string alghashname, int keySize, byte[] rgbIV)
		{
			if (keySize < 0)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));
			}
			int num = X509Utils.NameOrOidToAlgId(alghashname, OidGroup.HashAlgorithm);
			if (num == 0)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidAlgorithm"));
			}
			int num2 = X509Utils.NameOrOidToAlgId(algname, OidGroup.AllGroups);
			if (num2 == 0)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidAlgorithm"));
			}
			if (rgbIV == null)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidIV"));
			}
			byte[] result = null;
			Rfc2898DeriveBytes.DeriveKey(this.ProvHandle, num2, num, this.m_password, this.m_password.Length, keySize << 16, rgbIV, rgbIV.Length, JitHelpers.GetObjectHandleOnStack<byte[]>(ref result));
			return result;
		}

		// Token: 0x1700044D RID: 1101
		// (get) Token: 0x06002279 RID: 8825 RVA: 0x0007A218 File Offset: 0x00078418
		private SafeProvHandle ProvHandle
		{
			[SecurityCritical]
			get
			{
				if (this._safeProvHandle == null)
				{
					lock (this)
					{
						if (this._safeProvHandle == null)
						{
							SafeProvHandle safeProvHandle = Utils.AcquireProvHandle(this.m_cspParams);
							Thread.MemoryBarrier();
							this._safeProvHandle = safeProvHandle;
						}
					}
				}
				return this._safeProvHandle;
			}
		}

		// Token: 0x0600227A RID: 8826
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void DeriveKey(SafeProvHandle hProv, int algid, int algidHash, byte[] password, int cbPassword, int dwFlags, byte[] IV, int cbIV, ObjectHandleOnStack retKey);

		// Token: 0x04000C7F RID: 3199
		private byte[] m_buffer;

		// Token: 0x04000C80 RID: 3200
		private byte[] m_salt;

		// Token: 0x04000C81 RID: 3201
		private HMAC m_hmac;

		// Token: 0x04000C82 RID: 3202
		private byte[] m_password;

		// Token: 0x04000C83 RID: 3203
		private CspParameters m_cspParams;

		// Token: 0x04000C84 RID: 3204
		private uint m_iterations;

		// Token: 0x04000C85 RID: 3205
		private uint m_block;

		// Token: 0x04000C86 RID: 3206
		private int m_startIndex;

		// Token: 0x04000C87 RID: 3207
		private int m_endIndex;

		// Token: 0x04000C88 RID: 3208
		private int m_blockSize;

		// Token: 0x04000C89 RID: 3209
		[SecurityCritical]
		private SafeProvHandle _safeProvHandle;
	}
}
