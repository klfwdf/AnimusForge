using System;
using System.IO;
using System.Security.Cryptography;
using Mono.Cecil.PE;
using Mono.Security.Cryptography;

namespace Mono.Cecil
{
	// Token: 0x020002BC RID: 700
	internal static class CryptoService
	{
		// Token: 0x060011DE RID: 4574 RVA: 0x000371F2 File Offset: 0x000353F2
		private static SHA1 CreateSHA1()
		{
			return new SHA1CryptoServiceProvider();
		}

		// Token: 0x060011DF RID: 4575 RVA: 0x000371FC File Offset: 0x000353FC
		public static byte[] GetPublicKey(WriterParameters parameters)
		{
			byte[] result;
			using (RSA rsa = parameters.CreateRSA())
			{
				byte[] cspBlob = CryptoConvert.ToCapiPublicKeyBlob(rsa);
				byte[] publicKey = new byte[12 + cspBlob.Length];
				Buffer.BlockCopy(cspBlob, 0, publicKey, 12, cspBlob.Length);
				publicKey[1] = 36;
				publicKey[4] = 4;
				publicKey[5] = 128;
				publicKey[8] = (byte)cspBlob.Length;
				publicKey[9] = (byte)(cspBlob.Length >> 8);
				publicKey[10] = (byte)(cspBlob.Length >> 16);
				publicKey[11] = (byte)(cspBlob.Length >> 24);
				result = publicKey;
			}
			return result;
		}

		// Token: 0x060011E0 RID: 4576 RVA: 0x00037288 File Offset: 0x00035488
		public static void StrongName(Stream stream, ImageWriter writer, WriterParameters parameters)
		{
			int strong_name_pointer;
			byte[] strong_name = CryptoService.CreateStrongName(parameters, CryptoService.HashStream(stream, writer, out strong_name_pointer));
			CryptoService.PatchStrongName(stream, strong_name_pointer, strong_name);
		}

		// Token: 0x060011E1 RID: 4577 RVA: 0x000372AD File Offset: 0x000354AD
		private static void PatchStrongName(Stream stream, int strong_name_pointer, byte[] strong_name)
		{
			stream.Seek((long)strong_name_pointer, SeekOrigin.Begin);
			stream.Write(strong_name, 0, strong_name.Length);
		}

		// Token: 0x060011E2 RID: 4578 RVA: 0x000372C4 File Offset: 0x000354C4
		private static byte[] CreateStrongName(WriterParameters parameters, byte[] hash)
		{
			byte[] result;
			using (RSA rsa = parameters.CreateRSA())
			{
				RSAPKCS1SignatureFormatter rsapkcs1SignatureFormatter = new RSAPKCS1SignatureFormatter(rsa);
				rsapkcs1SignatureFormatter.SetHashAlgorithm("SHA1");
				byte[] array = rsapkcs1SignatureFormatter.CreateSignature(hash);
				Array.Reverse(array);
				result = array;
			}
			return result;
		}

		// Token: 0x060011E3 RID: 4579 RVA: 0x00037314 File Offset: 0x00035514
		private static byte[] HashStream(Stream stream, ImageWriter writer, out int strong_name_pointer)
		{
			Section text = writer.text;
			int header_size = (int)writer.GetHeaderSize();
			int text_section_pointer = (int)text.PointerToRawData;
			DataDirectory strong_name_directory = writer.GetStrongNameSignatureDirectory();
			if (strong_name_directory.Size == 0U)
			{
				throw new InvalidOperationException();
			}
			strong_name_pointer = (int)((long)text_section_pointer + (long)((ulong)(strong_name_directory.VirtualAddress - text.VirtualAddress)));
			int strong_name_length = (int)strong_name_directory.Size;
			SHA1 sha = CryptoService.CreateSHA1();
			byte[] buffer = new byte[8192];
			using (CryptoStream crypto_stream = new CryptoStream(Stream.Null, sha, CryptoStreamMode.Write))
			{
				stream.Seek(0L, SeekOrigin.Begin);
				CryptoService.CopyStreamChunk(stream, crypto_stream, buffer, header_size);
				stream.Seek((long)text_section_pointer, SeekOrigin.Begin);
				CryptoService.CopyStreamChunk(stream, crypto_stream, buffer, strong_name_pointer - text_section_pointer);
				stream.Seek((long)strong_name_length, SeekOrigin.Current);
				CryptoService.CopyStreamChunk(stream, crypto_stream, buffer, (int)(stream.Length - (long)(strong_name_pointer + strong_name_length)));
			}
			return sha.Hash;
		}

		// Token: 0x060011E4 RID: 4580 RVA: 0x00037400 File Offset: 0x00035600
		public static void CopyStreamChunk(Stream stream, Stream dest_stream, byte[] buffer, int length)
		{
			while (length > 0)
			{
				int read = stream.Read(buffer, 0, Math.Min(buffer.Length, length));
				dest_stream.Write(buffer, 0, read);
				length -= read;
			}
		}

		// Token: 0x060011E5 RID: 4581 RVA: 0x00037434 File Offset: 0x00035634
		public static byte[] ComputeHash(string file)
		{
			if (!File.Exists(file))
			{
				return Empty<byte>.Array;
			}
			byte[] result;
			using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				result = CryptoService.ComputeHash(stream);
			}
			return result;
		}

		// Token: 0x060011E6 RID: 4582 RVA: 0x00037480 File Offset: 0x00035680
		public static byte[] ComputeHash(Stream stream)
		{
			SHA1 sha = CryptoService.CreateSHA1();
			byte[] buffer = new byte[8192];
			using (CryptoStream crypto_stream = new CryptoStream(Stream.Null, sha, CryptoStreamMode.Write))
			{
				CryptoService.CopyStreamChunk(stream, crypto_stream, buffer, (int)stream.Length);
			}
			return sha.Hash;
		}

		// Token: 0x060011E7 RID: 4583 RVA: 0x000374DC File Offset: 0x000356DC
		public static byte[] ComputeHash(params ByteBuffer[] buffers)
		{
			SHA1 sha = CryptoService.CreateSHA1();
			using (CryptoStream crypto_stream = new CryptoStream(Stream.Null, sha, CryptoStreamMode.Write))
			{
				for (int i = 0; i < buffers.Length; i++)
				{
					crypto_stream.Write(buffers[i].buffer, 0, buffers[i].length);
				}
			}
			return sha.Hash;
		}

		// Token: 0x060011E8 RID: 4584 RVA: 0x00037544 File Offset: 0x00035744
		public static Guid ComputeGuid(byte[] hash)
		{
			byte[] guid = new byte[16];
			Buffer.BlockCopy(hash, 0, guid, 0, 16);
			guid[7] = (guid[7] & 15) | 64;
			guid[8] = (guid[8] & 63) | 128;
			return new Guid(guid);
		}
	}
}
