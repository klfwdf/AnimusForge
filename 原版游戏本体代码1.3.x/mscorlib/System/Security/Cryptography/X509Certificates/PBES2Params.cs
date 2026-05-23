using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002CA RID: 714
	internal struct PBES2Params
	{
		// Token: 0x06002545 RID: 9541 RVA: 0x00087BB4 File Offset: 0x00085DB4
		internal static PBES2Params Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return PBES2Params.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x06002546 RID: 9542 RVA: 0x00087BC4 File Offset: 0x00085DC4
		internal static PBES2Params Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			PBES2Params result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				PBES2Params pbes2Params;
				PBES2Params.DecodeCore(ref asnValueReader, expectedTag, encoded, out pbes2Params);
				asnValueReader.ThrowIfNotEmpty();
				result = pbes2Params;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x06002547 RID: 9543 RVA: 0x00087C14 File Offset: 0x00085E14
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out PBES2Params decoded)
		{
			PBES2Params.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x06002548 RID: 9544 RVA: 0x00087C24 File Offset: 0x00085E24
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PBES2Params decoded)
		{
			try
			{
				PBES2Params.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x06002549 RID: 9545 RVA: 0x00087C5C File Offset: 0x00085E5C
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out PBES2Params decoded)
		{
			decoded = default(PBES2Params);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			AlgorithmIdentifierAsn.Decode(ref asnValueReader, rebind, out decoded.KeyDerivationFunc);
			AlgorithmIdentifierAsn.Decode(ref asnValueReader, rebind, out decoded.EncryptionScheme);
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x04000E09 RID: 3593
		internal AlgorithmIdentifierAsn KeyDerivationFunc;

		// Token: 0x04000E0A RID: 3594
		internal AlgorithmIdentifierAsn EncryptionScheme;
	}
}
