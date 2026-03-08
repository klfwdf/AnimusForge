using System;
using System.Text;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002D9 RID: 729
	internal static class Utility
	{
		// Token: 0x060025A6 RID: 9638 RVA: 0x0008959C File Offset: 0x0008779C
		public static Span<T> GetSpanForArray<T>(T[] array, int offset)
		{
			return Utility.GetSpanForArray<T>(array, offset, array.Length - offset);
		}

		// Token: 0x060025A7 RID: 9639 RVA: 0x000895AA File Offset: 0x000877AA
		public static Span<T> GetSpanForArray<T>(T[] array, int offset, int count)
		{
			return new Span<T>(array, offset, count);
		}

		// Token: 0x060025A8 RID: 9640 RVA: 0x000895B4 File Offset: 0x000877B4
		public static int EncodingGetByteCount(Encoding encoding, ReadOnlySpan<char> input)
		{
			if (input.IsNull)
			{
				return encoding.GetByteCount(new char[0]);
			}
			ArraySegment<char> arraySegment = input.DangerousGetArraySegment();
			return encoding.GetByteCount(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
		}

		// Token: 0x060025A9 RID: 9641 RVA: 0x000895FC File Offset: 0x000877FC
		public static int EncodingGetBytes(Encoding encoding, char[] input, Span<byte> destination)
		{
			ArraySegment<byte> arraySegment = destination.DangerousGetArraySegment();
			return encoding.GetBytes(input, 0, input.Length, arraySegment.Array, arraySegment.Offset);
		}

		// Token: 0x060025AA RID: 9642 RVA: 0x0008962C File Offset: 0x0008782C
		public static int EncodingGetBytes(Encoding encoding, ReadOnlySpan<char> input, Span<byte> destination)
		{
			ArraySegment<byte> arraySegment = destination.DangerousGetArraySegment();
			ArraySegment<char> arraySegment2 = input.DangerousGetArraySegment();
			return encoding.GetBytes(arraySegment2.Array, arraySegment2.Offset, arraySegment2.Count, arraySegment.Array, arraySegment.Offset);
		}
	}
}
