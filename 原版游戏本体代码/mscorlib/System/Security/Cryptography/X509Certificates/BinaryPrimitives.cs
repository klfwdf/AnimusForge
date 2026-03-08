using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002DA RID: 730
	internal static class BinaryPrimitives
	{
		// Token: 0x060025AB RID: 9643 RVA: 0x00089672 File Offset: 0x00087872
		public static bool TryReadUInt16BigEndian(ReadOnlySpan<byte> bytes, out ushort value)
		{
			if (bytes.Length < 2)
			{
				value = 0;
				return false;
			}
			value = (ushort)((int)bytes[1] | ((int)bytes[0] << 8));
			return true;
		}

		// Token: 0x060025AC RID: 9644 RVA: 0x0008969A File Offset: 0x0008789A
		public static short ReadInt16BigEndian(ReadOnlySpan<byte> bytes)
		{
			return (short)((int)bytes[1] | ((int)bytes[0] << 8));
		}
	}
}
