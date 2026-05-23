using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002C4 RID: 708
	internal struct DigestInfoAsn
	{
		// Token: 0x06002526 RID: 9510 RVA: 0x00087371 File Offset: 0x00085571
		internal static DigestInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return DigestInfoAsn.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x06002527 RID: 9511 RVA: 0x00087380 File Offset: 0x00085580
		internal static DigestInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			DigestInfoAsn result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				DigestInfoAsn digestInfoAsn;
				DigestInfoAsn.DecodeCore(ref asnValueReader, expectedTag, encoded, out digestInfoAsn);
				asnValueReader.ThrowIfNotEmpty();
				result = digestInfoAsn;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x06002528 RID: 9512 RVA: 0x000873D0 File Offset: 0x000855D0
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out DigestInfoAsn decoded)
		{
			DigestInfoAsn.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x06002529 RID: 9513 RVA: 0x000873E0 File Offset: 0x000855E0
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out DigestInfoAsn decoded)
		{
			try
			{
				DigestInfoAsn.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x0600252A RID: 9514 RVA: 0x00087418 File Offset: 0x00085618
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out DigestInfoAsn decoded)
		{
			decoded = default(DigestInfoAsn);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			ReadOnlySpan<byte> span = rebind.Span;
			AlgorithmIdentifierAsn.Decode(ref asnValueReader, rebind, out decoded.DigestAlgorithm);
			ReadOnlySpan<byte> destination;
			if (asnValueReader.TryReadPrimitiveOctetString(out destination))
			{
				int start;
				decoded.Digest = (span.Overlaps(destination, out start) ? rebind.Slice(start, destination.Length) : destination.ToArray());
			}
			else
			{
				decoded.Digest = asnValueReader.ReadOctetString();
			}
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x04000DF9 RID: 3577
		internal AlgorithmIdentifierAsn DigestAlgorithm;

		// Token: 0x04000DFA RID: 3578
		internal ReadOnlyMemory<byte> Digest;
	}
}
