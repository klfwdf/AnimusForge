using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002C8 RID: 712
	internal struct MacData
	{
		// Token: 0x0600253A RID: 9530 RVA: 0x000878CC File Offset: 0x00085ACC
		internal static MacData Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return MacData.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x0600253B RID: 9531 RVA: 0x000878DC File Offset: 0x00085ADC
		internal static MacData Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			MacData result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				MacData macData;
				MacData.DecodeCore(ref asnValueReader, expectedTag, encoded, out macData);
				asnValueReader.ThrowIfNotEmpty();
				result = macData;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x0600253C RID: 9532 RVA: 0x0008792C File Offset: 0x00085B2C
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out MacData decoded)
		{
			MacData.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x0600253D RID: 9533 RVA: 0x0008793C File Offset: 0x00085B3C
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out MacData decoded)
		{
			try
			{
				MacData.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x0600253E RID: 9534 RVA: 0x00087974 File Offset: 0x00085B74
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out MacData decoded)
		{
			decoded = default(MacData);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			ReadOnlySpan<byte> span = rebind.Span;
			DigestInfoAsn.Decode(ref asnValueReader, rebind, out decoded.Mac);
			ReadOnlySpan<byte> destination;
			if (asnValueReader.TryReadPrimitiveOctetString(out destination))
			{
				int start;
				decoded.MacSalt = (span.Overlaps(destination, out start) ? rebind.Slice(start, destination.Length) : destination.ToArray());
			}
			else
			{
				decoded.MacSalt = asnValueReader.ReadOctetString();
			}
			if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
			{
				if (!asnValueReader.TryReadInt32(out decoded.IterationCount))
				{
					asnValueReader.ThrowIfNotEmpty();
				}
			}
			else
			{
				AsnValueReader asnValueReader2 = new AsnValueReader(MacData.s_DefaultIterationCount, AsnEncodingRules.DER);
				if (!asnValueReader2.TryReadInt32(out decoded.IterationCount))
				{
					asnValueReader2.ThrowIfNotEmpty();
				}
			}
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x04000E03 RID: 3587
		private static readonly byte[] s_DefaultIterationCount = new byte[] { 2, 1, 1 };

		// Token: 0x04000E04 RID: 3588
		internal DigestInfoAsn Mac;

		// Token: 0x04000E05 RID: 3589
		internal ReadOnlyMemory<byte> MacSalt;

		// Token: 0x04000E06 RID: 3590
		internal int IterationCount;
	}
}
