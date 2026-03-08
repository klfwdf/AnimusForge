using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002C5 RID: 709
	internal struct EncryptedContentInfoAsn
	{
		// Token: 0x0600252B RID: 9515 RVA: 0x000874A4 File Offset: 0x000856A4
		internal static EncryptedContentInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return EncryptedContentInfoAsn.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x0600252C RID: 9516 RVA: 0x000874B4 File Offset: 0x000856B4
		internal static EncryptedContentInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			EncryptedContentInfoAsn result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				EncryptedContentInfoAsn encryptedContentInfoAsn;
				EncryptedContentInfoAsn.DecodeCore(ref asnValueReader, expectedTag, encoded, out encryptedContentInfoAsn);
				asnValueReader.ThrowIfNotEmpty();
				result = encryptedContentInfoAsn;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x0600252D RID: 9517 RVA: 0x00087504 File Offset: 0x00085704
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
		{
			EncryptedContentInfoAsn.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x0600252E RID: 9518 RVA: 0x00087514 File Offset: 0x00085714
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
		{
			try
			{
				EncryptedContentInfoAsn.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x0600252F RID: 9519 RVA: 0x0008754C File Offset: 0x0008574C
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedContentInfoAsn decoded)
		{
			decoded = default(EncryptedContentInfoAsn);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			ReadOnlySpan<byte> span = rebind.Span;
			decoded.ContentType = asnValueReader.ReadObjectIdentifier();
			AlgorithmIdentifierAsn.Decode(ref asnValueReader, rebind, out decoded.ContentEncryptionAlgorithm);
			if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
			{
				ReadOnlySpan<byte> destination;
				if (asnValueReader.TryReadPrimitiveOctetString(out destination, new Asn1Tag?(new Asn1Tag(TagClass.ContextSpecific, 0))))
				{
					int start;
					decoded.EncryptedContent = new ReadOnlyMemory<byte>?(span.Overlaps(destination, out start) ? rebind.Slice(start, destination.Length) : destination.ToArray());
				}
				else
				{
					decoded.EncryptedContent = new ReadOnlyMemory<byte>?(asnValueReader.ReadOctetString(new Asn1Tag?(new Asn1Tag(TagClass.ContextSpecific, 0))));
				}
			}
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x04000DFB RID: 3579
		internal byte[] ContentType;

		// Token: 0x04000DFC RID: 3580
		internal AlgorithmIdentifierAsn ContentEncryptionAlgorithm;

		// Token: 0x04000DFD RID: 3581
		internal ReadOnlyMemory<byte>? EncryptedContent;
	}
}
