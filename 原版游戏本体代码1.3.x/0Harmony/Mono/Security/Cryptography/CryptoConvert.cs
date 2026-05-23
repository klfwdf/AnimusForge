using System;
using System.Security.Cryptography;

namespace Mono.Security.Cryptography
{
	// Token: 0x020001DD RID: 477
	internal static class CryptoConvert
	{
		// Token: 0x06000897 RID: 2199 RVA: 0x0001B9BF File Offset: 0x00019BBF
		private static int ToInt32LE(byte[] bytes, int offset)
		{
			return ((int)bytes[offset + 3] << 24) | ((int)bytes[offset + 2] << 16) | ((int)bytes[offset + 1] << 8) | (int)bytes[offset];
		}

		// Token: 0x06000898 RID: 2200 RVA: 0x0001B9BF File Offset: 0x00019BBF
		private static uint ToUInt32LE(byte[] bytes, int offset)
		{
			return (uint)(((int)bytes[offset + 3] << 24) | ((int)bytes[offset + 2] << 16) | ((int)bytes[offset + 1] << 8) | (int)bytes[offset]);
		}

		// Token: 0x06000899 RID: 2201 RVA: 0x0001B9DE File Offset: 0x00019BDE
		private static byte[] GetBytesLE(int val)
		{
			return new byte[]
			{
				(byte)(val & 255),
				(byte)((val >> 8) & 255),
				(byte)((val >> 16) & 255),
				(byte)((val >> 24) & 255)
			};
		}

		// Token: 0x0600089A RID: 2202 RVA: 0x0001BA1C File Offset: 0x00019C1C
		private static byte[] Trim(byte[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] != 0)
				{
					byte[] result = new byte[array.Length - i];
					Buffer.BlockCopy(array, i, result, 0, result.Length);
					return result;
				}
			}
			return null;
		}

		// Token: 0x0600089B RID: 2203 RVA: 0x0001BA58 File Offset: 0x00019C58
		private static RSA FromCapiPrivateKeyBlob(byte[] blob, int offset)
		{
			RSAParameters rsap = default(RSAParameters);
			try
			{
				if (blob[offset] != 7 || blob[offset + 1] != 2 || blob[offset + 2] != 0 || blob[offset + 3] != 0 || CryptoConvert.ToUInt32LE(blob, offset + 8) != 843141970U)
				{
					throw new CryptographicException("Invalid blob header");
				}
				int num = CryptoConvert.ToInt32LE(blob, offset + 12);
				byte[] exp = new byte[4];
				Buffer.BlockCopy(blob, offset + 16, exp, 0, 4);
				Array.Reverse(exp);
				rsap.Exponent = CryptoConvert.Trim(exp);
				int pos = offset + 20;
				int byteLen = num >> 3;
				rsap.Modulus = new byte[byteLen];
				Buffer.BlockCopy(blob, pos, rsap.Modulus, 0, byteLen);
				Array.Reverse(rsap.Modulus);
				pos += byteLen;
				int byteHalfLen = byteLen >> 1;
				rsap.P = new byte[byteHalfLen];
				Buffer.BlockCopy(blob, pos, rsap.P, 0, byteHalfLen);
				Array.Reverse(rsap.P);
				pos += byteHalfLen;
				rsap.Q = new byte[byteHalfLen];
				Buffer.BlockCopy(blob, pos, rsap.Q, 0, byteHalfLen);
				Array.Reverse(rsap.Q);
				pos += byteHalfLen;
				rsap.DP = new byte[byteHalfLen];
				Buffer.BlockCopy(blob, pos, rsap.DP, 0, byteHalfLen);
				Array.Reverse(rsap.DP);
				pos += byteHalfLen;
				rsap.DQ = new byte[byteHalfLen];
				Buffer.BlockCopy(blob, pos, rsap.DQ, 0, byteHalfLen);
				Array.Reverse(rsap.DQ);
				pos += byteHalfLen;
				rsap.InverseQ = new byte[byteHalfLen];
				Buffer.BlockCopy(blob, pos, rsap.InverseQ, 0, byteHalfLen);
				Array.Reverse(rsap.InverseQ);
				pos += byteHalfLen;
				rsap.D = new byte[byteLen];
				if (pos + byteLen + offset <= blob.Length)
				{
					Buffer.BlockCopy(blob, pos, rsap.D, 0, byteLen);
					Array.Reverse(rsap.D);
				}
			}
			catch (Exception e)
			{
				throw new CryptographicException("Invalid blob.", e);
			}
			RSA rsa = null;
			try
			{
				rsa = RSA.Create();
				rsa.ImportParameters(rsap);
			}
			catch (CryptographicException)
			{
				bool throws = false;
				try
				{
					rsa = new RSACryptoServiceProvider(new CspParameters
					{
						Flags = CspProviderFlags.UseMachineKeyStore
					});
					rsa.ImportParameters(rsap);
				}
				catch
				{
					throws = true;
				}
				if (throws)
				{
					throw;
				}
			}
			return rsa;
		}

		// Token: 0x0600089C RID: 2204 RVA: 0x0001BCC8 File Offset: 0x00019EC8
		private static RSA FromCapiPublicKeyBlob(byte[] blob, int offset)
		{
			RSA result;
			try
			{
				if (blob[offset] != 6 || blob[offset + 1] != 2 || blob[offset + 2] != 0 || blob[offset + 3] != 0 || CryptoConvert.ToUInt32LE(blob, offset + 8) != 826364754U)
				{
					throw new CryptographicException("Invalid blob header");
				}
				int num = CryptoConvert.ToInt32LE(blob, offset + 12);
				RSAParameters rsap = new RSAParameters
				{
					Exponent = new byte[3]
				};
				rsap.Exponent[0] = blob[offset + 18];
				rsap.Exponent[1] = blob[offset + 17];
				rsap.Exponent[2] = blob[offset + 16];
				int pos = offset + 20;
				int byteLen = num >> 3;
				rsap.Modulus = new byte[byteLen];
				Buffer.BlockCopy(blob, pos, rsap.Modulus, 0, byteLen);
				Array.Reverse(rsap.Modulus);
				RSA rsa = null;
				try
				{
					rsa = RSA.Create();
					rsa.ImportParameters(rsap);
				}
				catch (CryptographicException)
				{
					rsa = new RSACryptoServiceProvider(new CspParameters
					{
						Flags = CspProviderFlags.UseMachineKeyStore
					});
					rsa.ImportParameters(rsap);
				}
				result = rsa;
			}
			catch (Exception e)
			{
				throw new CryptographicException("Invalid blob.", e);
			}
			return result;
		}

		// Token: 0x0600089D RID: 2205 RVA: 0x0001BDE4 File Offset: 0x00019FE4
		public static RSA FromCapiKeyBlob(byte[] blob)
		{
			return CryptoConvert.FromCapiKeyBlob(blob, 0);
		}

		// Token: 0x0600089E RID: 2206 RVA: 0x0001BDF0 File Offset: 0x00019FF0
		public static RSA FromCapiKeyBlob(byte[] blob, int offset)
		{
			if (blob == null)
			{
				throw new ArgumentNullException("blob");
			}
			if (offset >= blob.Length)
			{
				throw new ArgumentException("blob is too small.");
			}
			byte b = blob[offset];
			if (b != 0)
			{
				if (b == 6)
				{
					return CryptoConvert.FromCapiPublicKeyBlob(blob, offset);
				}
				if (b == 7)
				{
					return CryptoConvert.FromCapiPrivateKeyBlob(blob, offset);
				}
			}
			else if (blob[offset + 12] == 6)
			{
				return CryptoConvert.FromCapiPublicKeyBlob(blob, offset + 12);
			}
			throw new CryptographicException("Unknown blob format.");
		}

		// Token: 0x0600089F RID: 2207 RVA: 0x0001BE5C File Offset: 0x0001A05C
		public static byte[] ToCapiPublicKeyBlob(RSA rsa)
		{
			RSAParameters p = rsa.ExportParameters(false);
			int keyLength = p.Modulus.Length;
			byte[] blob = new byte[20 + keyLength];
			blob[0] = 6;
			blob[1] = 2;
			blob[5] = 36;
			blob[8] = 82;
			blob[9] = 83;
			blob[10] = 65;
			blob[11] = 49;
			byte[] bitlen = CryptoConvert.GetBytesLE(keyLength << 3);
			blob[12] = bitlen[0];
			blob[13] = bitlen[1];
			blob[14] = bitlen[2];
			blob[15] = bitlen[3];
			int pos = 16;
			int i = p.Exponent.Length;
			while (i > 0)
			{
				blob[pos++] = p.Exponent[--i];
			}
			pos = 20;
			byte[] modulus = p.Modulus;
			int len = modulus.Length;
			Array.Reverse(modulus, 0, len);
			Buffer.BlockCopy(modulus, 0, blob, pos, len);
			pos += len;
			return blob;
		}
	}
}
