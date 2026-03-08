using System;
using System.Runtime.CompilerServices;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002B8 RID: 696
	internal static class CryptographicOperations
	{
		// Token: 0x060024F0 RID: 9456 RVA: 0x000859DF File Offset: 0x00083BDF
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void ZeroMemory(Span<byte> buffer)
		{
			buffer.Clear();
		}
	}
}
