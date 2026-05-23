using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace System.Security.Cryptography
{
	// Token: 0x02000275 RID: 629
	[ComVisible(true)]
	public class PasswordDeriveBytes : DeriveBytes
	{
		// Token: 0x17000442 RID: 1090
		// (get) Token: 0x06002239 RID: 8761 RVA: 0x00079080 File Offset: 0x00077280
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
							SafeProvHandle safeProvHandle = Utils.AcquireProvHandle(this._cspParams);
							Thread.MemoryBarrier();
							this._safeProvHandle = safeProvHandle;
						}
					}
				}
				return this._safeProvHandle;
			}
		}

		// Token: 0x0600223A RID: 8762 RVA: 0x000790E4 File Offset: 0x000772E4
		public PasswordDeriveBytes(string strPassword, byte[] rgbSalt)
			: this(strPassword, rgbSalt, new CspParameters())
		{
		}

		// Token: 0x0600223B RID: 8763 RVA: 0x000790F3 File Offset: 0x000772F3
		public PasswordDeriveBytes(byte[] password, byte[] salt)
			: this(password, salt, new CspParameters())
		{
		}

		// Token: 0x0600223C RID: 8764 RVA: 0x00079102 File Offset: 0x00077302
		public PasswordDeriveBytes(string strPassword, byte[] rgbSalt, string strHashName, int iterations)
			: this(strPassword, rgbSalt, strHashName, iterations, new CspParameters())
		{
		}

		// Token: 0x0600223D RID: 8765 RVA: 0x00079114 File Offset: 0x00077314
		public PasswordDeriveBytes(byte[] password, byte[] salt, string hashName, int iterations)
			: this(password, salt, hashName, iterations, new CspParameters())
		{
		}

		// Token: 0x0600223E RID: 8766 RVA: 0x00079126 File Offset: 0x00077326
		public PasswordDeriveBytes(string strPassword, byte[] rgbSalt, CspParameters cspParams)
			: this(strPassword, rgbSalt, "SHA1", 100, cspParams)
		{
		}

		// Token: 0x0600223F RID: 8767 RVA: 0x00079138 File Offset: 0x00077338
		public PasswordDeriveBytes(byte[] password, byte[] salt, CspParameters cspParams)
			: this(password, salt, "SHA1", 100, cspParams)
		{
		}

		// Token: 0x06002240 RID: 8768 RVA: 0x0007914A File Offset: 0x0007734A
		public PasswordDeriveBytes(string strPassword, byte[] rgbSalt, string strHashName, int iterations, CspParameters cspParams)
			: this(new UTF8Encoding(false).GetBytes(strPassword), rgbSalt, strHashName, iterations, cspParams)
		{
		}

		// Token: 0x06002241 RID: 8769 RVA: 0x00079164 File Offset: 0x00077364
		[SecuritySafeCritical]
		public PasswordDeriveBytes(byte[] password, byte[] salt, string hashName, int iterations, CspParameters cspParams)
		{
			this.IterationCount = iterations;
			this.Salt = salt;
			this.HashName = hashName;
			this._password = password;
			this._cspParams = cspParams;
		}

		// Token: 0x17000443 RID: 1091
		// (get) Token: 0x06002242 RID: 8770 RVA: 0x00079191 File Offset: 0x00077391
		// (set) Token: 0x06002243 RID: 8771 RVA: 0x0007919C File Offset: 0x0007739C
		public string HashName
		{
			get
			{
				return this._hashName;
			}
			set
			{
				if (this._baseValue != null)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed", new object[] { "HashName" }));
				}
				this._hashName = value;
				this._hash = (HashAlgorithm)CryptoConfig.CreateFromName(this._hashName);
			}
		}

		// Token: 0x17000444 RID: 1092
		// (get) Token: 0x06002244 RID: 8772 RVA: 0x000791EC File Offset: 0x000773EC
		// (set) Token: 0x06002245 RID: 8773 RVA: 0x000791F4 File Offset: 0x000773F4
		public int IterationCount
		{
			get
			{
				return this._iterations;
			}
			set
			{
				if (value <= 0)
				{
					throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
				}
				if (this._baseValue != null)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed", new object[] { "IterationCount" }));
				}
				this._iterations = value;
			}
		}

		// Token: 0x17000445 RID: 1093
		// (get) Token: 0x06002246 RID: 8774 RVA: 0x00079247 File Offset: 0x00077447
		// (set) Token: 0x06002247 RID: 8775 RVA: 0x00079264 File Offset: 0x00077464
		public byte[] Salt
		{
			get
			{
				if (this._salt == null)
				{
					return null;
				}
				return (byte[])this._salt.Clone();
			}
			set
			{
				if (this._baseValue != null)
				{
					throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed", new object[] { "Salt" }));
				}
				if (value == null)
				{
					this._salt = null;
					return;
				}
				this._salt = (byte[])value.Clone();
			}
		}

		// Token: 0x06002248 RID: 8776 RVA: 0x000792B4 File Offset: 0x000774B4
		[SecuritySafeCritical]
		[Obsolete("Rfc2898DeriveBytes replaces PasswordDeriveBytes for deriving key material from a password and is preferred in new applications.")]
		public override byte[] GetBytes(int cb)
		{
			int num = 0;
			byte[] array = new byte[cb];
			if (this._baseValue == null)
			{
				this.ComputeBaseValue();
			}
			else if (this._extra != null)
			{
				num = this._extra.Length - this._extraCount;
				if (num >= cb)
				{
					Buffer.InternalBlockCopy(this._extra, this._extraCount, array, 0, cb);
					if (num > cb)
					{
						this._extraCount += cb;
					}
					else
					{
						this._extra = null;
					}
					return array;
				}
				Buffer.InternalBlockCopy(this._extra, num, array, 0, num);
				this._extra = null;
			}
			byte[] array2 = this.ComputeBytes(cb - num);
			Buffer.InternalBlockCopy(array2, 0, array, num, cb - num);
			if (array2.Length + num > cb)
			{
				this._extra = array2;
				this._extraCount = cb - num;
			}
			return array;
		}

		// Token: 0x06002249 RID: 8777 RVA: 0x0007936D File Offset: 0x0007756D
		public override void Reset()
		{
			this._prefix = 0;
			this._extra = null;
			this._baseValue = null;
		}

		// Token: 0x0600224A RID: 8778 RVA: 0x00079384 File Offset: 0x00077584
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				if (this._hash != null)
				{
					this._hash.Dispose();
				}
				if (this._baseValue != null)
				{
					Array.Clear(this._baseValue, 0, this._baseValue.Length);
				}
				if (this._extra != null)
				{
					Array.Clear(this._extra, 0, this._extra.Length);
				}
				if (this._password != null)
				{
					Array.Clear(this._password, 0, this._password.Length);
				}
				if (this._salt != null)
				{
					Array.Clear(this._salt, 0, this._salt.Length);
				}
			}
		}

		// Token: 0x0600224B RID: 8779 RVA: 0x00079424 File Offset: 0x00077624
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
			PasswordDeriveBytes.DeriveKey(this.ProvHandle, num2, num, this._password, this._password.Length, keySize << 16, rgbIV, rgbIV.Length, JitHelpers.GetObjectHandleOnStack<byte[]>(ref result));
			return result;
		}

		// Token: 0x0600224C RID: 8780
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern void DeriveKey(SafeProvHandle hProv, int algid, int algidHash, byte[] password, int cbPassword, int dwFlags, byte[] IV, int cbIV, ObjectHandleOnStack retKey);

		// Token: 0x0600224D RID: 8781 RVA: 0x000794C0 File Offset: 0x000776C0
		private byte[] ComputeBaseValue()
		{
			this._hash.Initialize();
			this._hash.TransformBlock(this._password, 0, this._password.Length, this._password, 0);
			if (this._salt != null)
			{
				this._hash.TransformBlock(this._salt, 0, this._salt.Length, this._salt, 0);
			}
			this._hash.TransformFinalBlock(EmptyArray<byte>.Value, 0, 0);
			this._baseValue = this._hash.Hash;
			this._hash.Initialize();
			for (int i = 1; i < this._iterations - 1; i++)
			{
				this._hash.ComputeHash(this._baseValue);
				this._baseValue = this._hash.Hash;
			}
			return this._baseValue;
		}

		// Token: 0x0600224E RID: 8782 RVA: 0x00079590 File Offset: 0x00077790
		[SecurityCritical]
		private byte[] ComputeBytes(int cb)
		{
			int num = 0;
			this._hash.Initialize();
			int num2 = this._hash.HashSize / 8;
			byte[] array = new byte[(cb + num2 - 1) / num2 * num2];
			using (CryptoStream cryptoStream = new CryptoStream(Stream.Null, this._hash, CryptoStreamMode.Write))
			{
				this.HashPrefix(cryptoStream);
				cryptoStream.Write(this._baseValue, 0, this._baseValue.Length);
				cryptoStream.Close();
			}
			Buffer.InternalBlockCopy(this._hash.Hash, 0, array, num, num2);
			num += num2;
			while (cb > num)
			{
				this._hash.Initialize();
				using (CryptoStream cryptoStream2 = new CryptoStream(Stream.Null, this._hash, CryptoStreamMode.Write))
				{
					this.HashPrefix(cryptoStream2);
					cryptoStream2.Write(this._baseValue, 0, this._baseValue.Length);
					cryptoStream2.Close();
				}
				Buffer.InternalBlockCopy(this._hash.Hash, 0, array, num, num2);
				num += num2;
			}
			return array;
		}

		// Token: 0x0600224F RID: 8783 RVA: 0x000796AC File Offset: 0x000778AC
		private void HashPrefix(CryptoStream cs)
		{
			int num = 0;
			byte[] array = new byte[] { 48, 48, 48 };
			if (this._prefix > 999)
			{
				throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_TooManyBytes"));
			}
			if (this._prefix >= 100)
			{
				byte[] array2 = array;
				int num2 = 0;
				array2[num2] += (byte)(this._prefix / 100);
				num++;
			}
			if (this._prefix >= 10)
			{
				byte[] array3 = array;
				int num3 = num;
				array3[num3] += (byte)(this._prefix % 100 / 10);
				num++;
			}
			if (this._prefix > 0)
			{
				byte[] array4 = array;
				int num4 = num;
				array4[num4] += (byte)(this._prefix % 10);
				num++;
				cs.Write(array, 0, num);
			}
			this._prefix++;
		}

		// Token: 0x04000C6E RID: 3182
		private int _extraCount;

		// Token: 0x04000C6F RID: 3183
		private int _prefix;

		// Token: 0x04000C70 RID: 3184
		private int _iterations;

		// Token: 0x04000C71 RID: 3185
		private byte[] _baseValue;

		// Token: 0x04000C72 RID: 3186
		private byte[] _extra;

		// Token: 0x04000C73 RID: 3187
		private byte[] _salt;

		// Token: 0x04000C74 RID: 3188
		private string _hashName;

		// Token: 0x04000C75 RID: 3189
		private byte[] _password;

		// Token: 0x04000C76 RID: 3190
		private HashAlgorithm _hash;

		// Token: 0x04000C77 RID: 3191
		private CspParameters _cspParams;

		// Token: 0x04000C78 RID: 3192
		[SecurityCritical]
		private SafeProvHandle _safeProvHandle;
	}
}
