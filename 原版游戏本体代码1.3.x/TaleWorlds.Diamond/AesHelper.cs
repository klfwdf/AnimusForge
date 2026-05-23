using System;
using System.IO;
using System.Security.Cryptography;

namespace TaleWorlds.Diamond
{
	// Token: 0x02000004 RID: 4
	public static class AesHelper
	{
		// Token: 0x06000009 RID: 9 RVA: 0x00002138 File Offset: 0x00000338
		public static byte[] Encrypt(byte[] plainData, byte[] key, byte[] initializationVector)
		{
			byte[] result;
			using (Aes aes = Aes.Create())
			{
				aes.Key = key;
				aes.IV = initializationVector;
				result = AesHelper.EncryptStringToBytes(plainData, aes.Key, aes.IV);
			}
			return result;
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000218C File Offset: 0x0000038C
		public static byte[] Decrypt(byte[] encrypted, byte[] key, byte[] initializationVector)
		{
			byte[] result;
			using (Aes aes = Aes.Create())
			{
				aes.Key = key;
				aes.IV = initializationVector;
				result = AesHelper.DecryptStringFromBytes(encrypted, aes.Key, aes.IV);
			}
			return result;
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000021E0 File Offset: 0x000003E0
		private static byte[] EncryptStringToBytes(byte[] plainData, byte[] Key, byte[] IV)
		{
			if (plainData == null || plainData.Length == 0)
			{
				throw new ArgumentNullException("plainText");
			}
			if (Key == null || Key.Length == 0)
			{
				throw new ArgumentNullException("Key");
			}
			if (IV == null || IV.Length == 0)
			{
				throw new ArgumentNullException("IV");
			}
			byte[] result;
			using (Aes aes = Aes.Create())
			{
				aes.Key = Key;
				aes.IV = IV;
				ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
				using (MemoryStream memoryStream = new MemoryStream())
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
					{
						using (BinaryWriter binaryWriter = new BinaryWriter(cryptoStream))
						{
							binaryWriter.Write(plainData);
						}
						result = memoryStream.ToArray();
					}
				}
			}
			return result;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x000022D8 File Offset: 0x000004D8
		private static byte[] DecryptStringFromBytes(byte[] cipherText, byte[] Key, byte[] IV)
		{
			if (cipherText == null || cipherText.Length == 0)
			{
				throw new ArgumentNullException("cipherText");
			}
			if (Key == null || Key.Length == 0)
			{
				throw new ArgumentNullException("Key");
			}
			if (IV == null || IV.Length == 0)
			{
				throw new ArgumentNullException("IV");
			}
			byte[] result = null;
			using (Aes aes = Aes.Create())
			{
				aes.Key = Key;
				aes.IV = IV;
				ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
				using (MemoryStream memoryStream = new MemoryStream(cipherText))
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read))
					{
						using (BinaryReader binaryReader = new BinaryReader(cryptoStream))
						{
							result = AesHelper.ReadAllBytes(binaryReader);
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x000023CC File Offset: 0x000005CC
		private static byte[] ReadAllBytes(BinaryReader reader)
		{
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				byte[] array = new byte[4096];
				int count;
				while ((count = reader.Read(array, 0, array.Length)) != 0)
				{
					memoryStream.Write(array, 0, count);
				}
				result = memoryStream.ToArray();
			}
			return result;
		}
	}
}
