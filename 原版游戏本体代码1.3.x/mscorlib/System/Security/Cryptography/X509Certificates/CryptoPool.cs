using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002DB RID: 731
	internal static class CryptoPool
	{
		// Token: 0x060025AD RID: 9645 RVA: 0x000896B0 File Offset: 0x000878B0
		public static byte[] Rent(int size)
		{
			return new byte[size];
		}

		// Token: 0x060025AE RID: 9646 RVA: 0x000896B8 File Offset: 0x000878B8
		public static void Return(byte[] array, int clearSize)
		{
			CryptographicOperations.ZeroMemory(new Span<byte>(array, 0, clearSize));
		}

		// Token: 0x060025AF RID: 9647 RVA: 0x000896C7 File Offset: 0x000878C7
		public static void Return(byte[] array)
		{
			CryptographicOperations.ZeroMemory(new Span<byte>(array));
		}

		// Token: 0x060025B0 RID: 9648 RVA: 0x000896D4 File Offset: 0x000878D4
		public static void Return(ArraySegment<byte> segment, int clearSize)
		{
			CryptographicOperations.ZeroMemory(new Span<byte>(segment).Slice(0, clearSize));
		}

		// Token: 0x060025B1 RID: 9649 RVA: 0x000896F6 File Offset: 0x000878F6
		public static void Return(ArraySegment<byte> segment)
		{
			CryptographicOperations.ZeroMemory(new Span<byte>(segment));
		}
	}
}
