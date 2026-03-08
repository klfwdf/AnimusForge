using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002BE RID: 702
	[SuppressUnmanagedCodeSecurity]
	internal static class Pbkdf2
	{
		// Token: 0x0600250C RID: 9484 RVA: 0x00086CE8 File Offset: 0x00084EE8
		[SecuritySafeCritical]
		static Pbkdf2()
		{
			int num = Pbkdf2.BCryptOpenAlgorithmProvider(out Pbkdf2._sha1, "SHA1", "Microsoft Primitive Provider", OpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG);
			if (num != 0)
			{
				throw new CryptographicException(string.Format(CultureInfo.CurrentCulture, "A provider could not be found for algorithm '{0}'.", "SHA1"));
			}
			num = Pbkdf2.BCryptOpenAlgorithmProvider(out Pbkdf2._sha256, "SHA256", "Microsoft Primitive Provider", OpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG);
			if (num != 0)
			{
				throw new CryptographicException(string.Format(CultureInfo.CurrentCulture, "A provider could not be found for algorithm '{0}'.", "SHA256"));
			}
			num = Pbkdf2.BCryptOpenAlgorithmProvider(out Pbkdf2._sha384, "SHA384", "Microsoft Primitive Provider", OpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG);
			if (num != 0)
			{
				throw new CryptographicException(string.Format(CultureInfo.CurrentCulture, "A provider could not be found for algorithm '{0}'.", "SHA384"));
			}
			num = Pbkdf2.BCryptOpenAlgorithmProvider(out Pbkdf2._sha512, "SHA512", "Microsoft Primitive Provider", OpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG);
			if (num != 0)
			{
				throw new CryptographicException(string.Format(CultureInfo.CurrentCulture, "A provider could not be found for algorithm '{0}'.", "SHA512"));
			}
		}

		// Token: 0x0600250D RID: 9485
		[SecurityCritical]
		[DllImport("bcrypt.dll")]
		private static extern int BCryptOpenAlgorithmProvider(out SafeBCryptAlgorithmHandle phAlgorithm, [MarshalAs(UnmanagedType.LPWStr)] [In] string pszAlgId, [MarshalAs(UnmanagedType.LPWStr)] [In] string pszImplementation, [In] OpenAlgorithmProviderFlags dwFlags);

		// Token: 0x0600250E RID: 9486 RVA: 0x00086DC4 File Offset: 0x00084FC4
		[SecuritySafeCritical]
		internal unsafe static byte[] Derive(string hashAlgorithm, byte[] password, byte[] salt, int iterations, int length)
		{
			if (length <= 0)
			{
				throw new ArgumentOutOfRangeException("length");
			}
			if (iterations <= 0)
			{
				throw new ArgumentOutOfRangeException("iterations");
			}
			KdfWorkLimiter.RecordIterations(iterations);
			byte[] array = new byte[length];
			SafeBCryptAlgorithmHandle hPrf;
			if (!(hashAlgorithm == "SHA1"))
			{
				if (!(hashAlgorithm == "SHA256"))
				{
					if (!(hashAlgorithm == "SHA384"))
					{
						if (!(hashAlgorithm == "SHA512"))
						{
							throw new CryptographicException(string.Format(CultureInfo.CurrentCulture, "'{0}' is not a known hash algorithm.", hashAlgorithm));
						}
						hPrf = Pbkdf2._sha512;
					}
					else
					{
						hPrf = Pbkdf2._sha384;
					}
				}
				else
				{
					hPrf = Pbkdf2._sha256;
				}
			}
			else
			{
				hPrf = Pbkdf2._sha1;
			}
			fixed (byte[] array2 = password)
			{
				byte* ptr;
				if (password == null || array2.Length == 0)
				{
					ptr = null;
				}
				else
				{
					ptr = &array2[0];
				}
				fixed (byte[] array3 = salt)
				{
					byte* ptr2;
					if (salt == null || array3.Length == 0)
					{
						ptr2 = null;
					}
					else
					{
						ptr2 = &array3[0];
					}
					byte[] array4;
					byte* pbDerivedKey;
					if ((array4 = array) == null || array4.Length == 0)
					{
						pbDerivedKey = null;
					}
					else
					{
						pbDerivedKey = &array4[0];
					}
					byte b = 0;
					int num = Pbkdf2.BCryptDeriveKeyPBKDF2(hPrf, (ptr != null) ? ptr : (&b), password.Length, (ptr2 != null) ? ptr2 : (&b), salt.Length, (ulong)((long)iterations), pbDerivedKey, array.Length, 0U);
					if (num != 0)
					{
						throw new CryptographicException(string.Format(CultureInfo.CurrentCulture, "A call to BCryptDeriveKeyPBKDF2 failed with code '{0}'.", num));
					}
					array4 = null;
				}
			}
			return array;
		}

		// Token: 0x0600250F RID: 9487
		[SecurityCritical]
		[DllImport("bcrypt.dll")]
		internal unsafe static extern int BCryptDeriveKeyPBKDF2(SafeBCryptAlgorithmHandle hPrf, byte* pbPassword, int cbPassword, byte* pbSalt, int cbSalt, ulong cIterations, byte* pbDerivedKey, int cbDerivedKey, uint dwFlags);

		// Token: 0x04000DE8 RID: 3560
		internal const string BCRYPT_LIB = "bcrypt.dll";

		// Token: 0x04000DE9 RID: 3561
		private const string MS_PRIMITIVE_PROVIDER = "Microsoft Primitive Provider";

		// Token: 0x04000DEA RID: 3562
		private const int NtStatusSuccess = 0;

		// Token: 0x04000DEB RID: 3563
		[SecurityCritical]
		internal static readonly SafeBCryptAlgorithmHandle _sha1;

		// Token: 0x04000DEC RID: 3564
		[SecurityCritical]
		internal static readonly SafeBCryptAlgorithmHandle _sha256;

		// Token: 0x04000DED RID: 3565
		[SecurityCritical]
		internal static readonly SafeBCryptAlgorithmHandle _sha384;

		// Token: 0x04000DEE RID: 3566
		[SecurityCritical]
		internal static readonly SafeBCryptAlgorithmHandle _sha512;
	}
}
