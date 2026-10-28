using System;
using System.IO;
using System.Security.Cryptography;

namespace TaleWorlds.Diamond;

public static class AesHelper
{
	public static byte[] Encrypt(byte[] plainData, byte[] key, byte[] initializationVector)
	{
		using Aes aes = Aes.Create();
		aes.Key = key;
		aes.IV = initializationVector;
		return EncryptStringToBytes(plainData, aes.Key, aes.IV);
	}

	public static byte[] Decrypt(byte[] encrypted, byte[] key, byte[] initializationVector)
	{
		using Aes aes = Aes.Create();
		aes.Key = key;
		aes.IV = initializationVector;
		return DecryptStringFromBytes(encrypted, aes.Key, aes.IV);
	}

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
		using Aes aes = Aes.Create();
		aes.Key = Key;
		aes.IV = IV;
		ICryptoTransform transform = aes.CreateEncryptor(aes.Key, aes.IV);
		using MemoryStream memoryStream = new MemoryStream();
		using CryptoStream output = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
		using (BinaryWriter binaryWriter = new BinaryWriter(output))
		{
			binaryWriter.Write(plainData);
		}
		return memoryStream.ToArray();
	}

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
		byte[] array = null;
		using Aes aes = Aes.Create();
		aes.Key = Key;
		aes.IV = IV;
		ICryptoTransform transform = aes.CreateDecryptor(aes.Key, aes.IV);
		using MemoryStream stream = new MemoryStream(cipherText);
		using CryptoStream input = new CryptoStream(stream, transform, CryptoStreamMode.Read);
		using BinaryReader reader = new BinaryReader(input);
		return ReadAllBytes(reader);
	}

	private static byte[] ReadAllBytes(BinaryReader reader)
	{
		using MemoryStream memoryStream = new MemoryStream();
		byte[] array = new byte[4096];
		int count;
		while ((count = reader.Read(array, 0, array.Length)) != 0)
		{
			memoryStream.Write(array, 0, count);
		}
		return memoryStream.ToArray();
	}
}
