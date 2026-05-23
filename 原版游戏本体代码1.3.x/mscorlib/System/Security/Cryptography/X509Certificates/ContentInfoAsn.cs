using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002C3 RID: 707
	internal struct ContentInfoAsn
	{
		// Token: 0x06002521 RID: 9505 RVA: 0x00087236 File Offset: 0x00085436
		internal static ContentInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return ContentInfoAsn.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x06002522 RID: 9506 RVA: 0x00087244 File Offset: 0x00085444
		internal static ContentInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			ContentInfoAsn result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				ContentInfoAsn contentInfoAsn;
				ContentInfoAsn.DecodeCore(ref asnValueReader, expectedTag, encoded, out contentInfoAsn);
				asnValueReader.ThrowIfNotEmpty();
				result = contentInfoAsn;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x06002523 RID: 9507 RVA: 0x00087294 File Offset: 0x00085494
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out ContentInfoAsn decoded)
		{
			ContentInfoAsn.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x06002524 RID: 9508 RVA: 0x000872A4 File Offset: 0x000854A4
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out ContentInfoAsn decoded)
		{
			try
			{
				ContentInfoAsn.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x06002525 RID: 9509 RVA: 0x000872DC File Offset: 0x000854DC
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out ContentInfoAsn decoded)
		{
			decoded = default(ContentInfoAsn);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			ReadOnlySpan<byte> span = rebind.Span;
			decoded.ContentType = asnValueReader.ReadObjectIdentifier();
			AsnValueReader asnValueReader2 = asnValueReader.ReadSequence(new Asn1Tag?(new Asn1Tag(TagClass.ContextSpecific, 0)));
			ReadOnlySpan<byte> destination = asnValueReader2.ReadEncodedValue();
			int start;
			decoded.Content = (span.Overlaps(destination, out start) ? rebind.Slice(start, destination.Length) : destination.ToArray());
			asnValueReader2.ThrowIfNotEmpty();
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x04000DF7 RID: 3575
		internal byte[] ContentType;

		// Token: 0x04000DF8 RID: 3576
		internal ReadOnlyMemory<byte> Content;
	}
}
