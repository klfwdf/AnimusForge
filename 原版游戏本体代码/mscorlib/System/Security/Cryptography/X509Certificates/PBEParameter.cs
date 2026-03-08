using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002C9 RID: 713
	internal struct PBEParameter
	{
		// Token: 0x06002540 RID: 9536 RVA: 0x00087A79 File Offset: 0x00085C79
		internal static PBEParameter Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return PBEParameter.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x06002541 RID: 9537 RVA: 0x00087A88 File Offset: 0x00085C88
		internal static PBEParameter Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			PBEParameter result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				PBEParameter pbeparameter;
				PBEParameter.DecodeCore(ref asnValueReader, expectedTag, encoded, out pbeparameter);
				asnValueReader.ThrowIfNotEmpty();
				result = pbeparameter;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x06002542 RID: 9538 RVA: 0x00087AD8 File Offset: 0x00085CD8
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out PBEParameter decoded)
		{
			PBEParameter.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x06002543 RID: 9539 RVA: 0x00087AE8 File Offset: 0x00085CE8
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PBEParameter decoded)
		{
			try
			{
				PBEParameter.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x06002544 RID: 9540 RVA: 0x00087B20 File Offset: 0x00085D20
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PBEParameter decoded)
		{
			decoded = default(PBEParameter);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			ReadOnlySpan<byte> span = rebind.Span;
			ReadOnlySpan<byte> destination;
			if (asnValueReader.TryReadPrimitiveOctetString(out destination))
			{
				int start;
				decoded.Salt = (span.Overlaps(destination, out start) ? rebind.Slice(start, destination.Length) : destination.ToArray());
			}
			else
			{
				decoded.Salt = asnValueReader.ReadOctetString();
			}
			if (!asnValueReader.TryReadInt32(out decoded.IterationCount))
			{
				asnValueReader.ThrowIfNotEmpty();
			}
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x04000E07 RID: 3591
		internal ReadOnlyMemory<byte> Salt;

		// Token: 0x04000E08 RID: 3592
		internal int IterationCount;
	}
}
