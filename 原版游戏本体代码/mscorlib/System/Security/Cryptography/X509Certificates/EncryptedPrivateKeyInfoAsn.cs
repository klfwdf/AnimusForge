using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002C7 RID: 711
	internal struct EncryptedPrivateKeyInfoAsn
	{
		// Token: 0x06002535 RID: 9525 RVA: 0x00087798 File Offset: 0x00085998
		internal static EncryptedPrivateKeyInfoAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return EncryptedPrivateKeyInfoAsn.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x06002536 RID: 9526 RVA: 0x000877A8 File Offset: 0x000859A8
		internal static EncryptedPrivateKeyInfoAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			EncryptedPrivateKeyInfoAsn result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				EncryptedPrivateKeyInfoAsn encryptedPrivateKeyInfoAsn;
				EncryptedPrivateKeyInfoAsn.DecodeCore(ref asnValueReader, expectedTag, encoded, out encryptedPrivateKeyInfoAsn);
				asnValueReader.ThrowIfNotEmpty();
				result = encryptedPrivateKeyInfoAsn;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x06002537 RID: 9527 RVA: 0x000877F8 File Offset: 0x000859F8
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out EncryptedPrivateKeyInfoAsn decoded)
		{
			EncryptedPrivateKeyInfoAsn.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x06002538 RID: 9528 RVA: 0x00087808 File Offset: 0x00085A08
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedPrivateKeyInfoAsn decoded)
		{
			try
			{
				EncryptedPrivateKeyInfoAsn.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x06002539 RID: 9529 RVA: 0x00087840 File Offset: 0x00085A40
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedPrivateKeyInfoAsn decoded)
		{
			decoded = default(EncryptedPrivateKeyInfoAsn);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			ReadOnlySpan<byte> span = rebind.Span;
			AlgorithmIdentifierAsn.Decode(ref asnValueReader, rebind, out decoded.EncryptionAlgorithm);
			ReadOnlySpan<byte> destination;
			if (asnValueReader.TryReadPrimitiveOctetString(out destination))
			{
				int start;
				decoded.EncryptedData = (span.Overlaps(destination, out start) ? rebind.Slice(start, destination.Length) : destination.ToArray());
			}
			else
			{
				decoded.EncryptedData = asnValueReader.ReadOctetString();
			}
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x04000E01 RID: 3585
		internal AlgorithmIdentifierAsn EncryptionAlgorithm;

		// Token: 0x04000E02 RID: 3586
		internal ReadOnlyMemory<byte> EncryptedData;
	}
}
