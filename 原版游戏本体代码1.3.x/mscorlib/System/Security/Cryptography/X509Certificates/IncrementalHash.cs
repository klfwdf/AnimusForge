using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002BA RID: 698
	internal class IncrementalHash : IDisposable
	{
		// Token: 0x060024F3 RID: 9459 RVA: 0x00085ADC File Offset: 0x00083CDC
		private IncrementalHash(HashAlgorithm algorithm)
		{
			this._algorithm = algorithm;
		}

		// Token: 0x060024F4 RID: 9460 RVA: 0x00085AEC File Offset: 0x00083CEC
		public static IncrementalHash CreateHash(HashAlgorithmName hashAlgorithm)
		{
			if (hashAlgorithm == HashAlgorithmName.MD5)
			{
				return new IncrementalHash(MD5.Create());
			}
			if (hashAlgorithm == HashAlgorithmName.SHA1)
			{
				return new IncrementalHash(SHA1.Create());
			}
			if (hashAlgorithm == HashAlgorithmName.SHA256)
			{
				return new IncrementalHash(SHA256.Create());
			}
			if (hashAlgorithm == HashAlgorithmName.SHA384)
			{
				return new IncrementalHash(SHA384.Create());
			}
			if (hashAlgorithm == HashAlgorithmName.SHA512)
			{
				return new IncrementalHash(SHA512.Create());
			}
			throw new CryptographicException();
		}

		// Token: 0x060024F5 RID: 9461 RVA: 0x00085B78 File Offset: 0x00083D78
		public void AppendData(ReadOnlySpan<byte> data)
		{
			ArraySegment<byte> arraySegment = data.DangerousGetArraySegment();
			this._algorithm.TransformBlock(arraySegment.Array, arraySegment.Offset, arraySegment.Count, null, 0);
		}

		// Token: 0x060024F6 RID: 9462 RVA: 0x00085BB0 File Offset: 0x00083DB0
		public bool TryGetHashAndReset(Span<byte> destination, out int bytesWritten)
		{
			if (destination.Length < this._algorithm.HashSize / 8)
			{
				bytesWritten = 0;
				return false;
			}
			this._algorithm.TransformFinalBlock(IncrementalHash.s_Empty, 0, 0);
			byte[] hash = this._algorithm.Hash;
			this._algorithm.Initialize();
			new ReadOnlyMemory<byte>(hash).CopyTo(destination);
			bytesWritten = hash.Length;
			return true;
		}

		// Token: 0x060024F7 RID: 9463 RVA: 0x00085C17 File Offset: 0x00083E17
		public void Dispose()
		{
			this._algorithm.Clear();
		}

		// Token: 0x04000DCC RID: 3532
		private readonly HashAlgorithm _algorithm;

		// Token: 0x04000DCD RID: 3533
		private static readonly byte[] s_Empty = new byte[0];
	}
}
