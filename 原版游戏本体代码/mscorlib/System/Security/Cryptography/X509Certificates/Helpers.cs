using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002B9 RID: 697
	internal static class Helpers
	{
		// Token: 0x060024F1 RID: 9457 RVA: 0x000859E8 File Offset: 0x00083BE8
		internal static bool SequenceEqual(byte[] left, byte[] right)
		{
			if (left.Length != right.Length)
			{
				return false;
			}
			for (int i = 0; i < left.Length; i++)
			{
				if (left[i] != right[i])
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060024F2 RID: 9458 RVA: 0x00085A18 File Offset: 0x00083C18
		internal static ReadOnlyMemory<byte> DecodeOctetStringAsMemory(ReadOnlyMemory<byte> encodedOctetString)
		{
			ReadOnlyMemory<byte> result;
			try
			{
				ReadOnlySpan<byte> span = encodedOctetString.Span;
				ReadOnlySpan<byte> destination;
				int num;
				if (AsnDecoder.TryReadPrimitiveOctetString(span, AsnEncodingRules.BER, out destination, out num, null))
				{
					if (num != span.Length)
					{
						throw new CryptographicException("ASN1 corrupted data.");
					}
					int start;
					if (span.Overlaps(destination, out start))
					{
						return encodedOctetString.Slice(start, destination.Length);
					}
					Assert.Fail("input.Overlaps(primitive)", "input.Overlaps(primitive) failed after TryReadPrimitiveOctetString succeeded");
				}
				byte[] array = AsnDecoder.ReadOctetString(span, AsnEncodingRules.BER, out num, null);
				if (num != span.Length)
				{
					throw new CryptographicException("ASN1 corrupted data.");
				}
				result = array;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}
	}
}
