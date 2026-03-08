using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002CB RID: 715
	internal struct Pbkdf2Params
	{
		// Token: 0x0600254A RID: 9546 RVA: 0x00087CA0 File Offset: 0x00085EA0
		internal static Pbkdf2Params Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return Pbkdf2Params.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x0600254B RID: 9547 RVA: 0x00087CB0 File Offset: 0x00085EB0
		internal static Pbkdf2Params Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			Pbkdf2Params result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				Pbkdf2Params pbkdf2Params;
				Pbkdf2Params.DecodeCore(ref asnValueReader, expectedTag, encoded, out pbkdf2Params);
				asnValueReader.ThrowIfNotEmpty();
				result = pbkdf2Params;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x0600254C RID: 9548 RVA: 0x00087D00 File Offset: 0x00085F00
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out Pbkdf2Params decoded)
		{
			Pbkdf2Params.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x0600254D RID: 9549 RVA: 0x00087D10 File Offset: 0x00085F10
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Pbkdf2Params decoded)
		{
			try
			{
				Pbkdf2Params.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x0600254E RID: 9550 RVA: 0x00087D48 File Offset: 0x00085F48
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out Pbkdf2Params decoded)
		{
			decoded = default(Pbkdf2Params);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			Pbkdf2SaltChoice.Decode(ref asnValueReader, rebind, out decoded.Salt);
			if (!asnValueReader.TryReadInt32(out decoded.IterationCount))
			{
				asnValueReader.ThrowIfNotEmpty();
			}
			if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(Asn1Tag.Integer))
			{
				int value;
				if (asnValueReader.TryReadInt32(out value))
				{
					decoded.KeyLength = new int?(value);
				}
				else
				{
					asnValueReader.ThrowIfNotEmpty();
				}
			}
			if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(Asn1Tag.Sequence))
			{
				AlgorithmIdentifierAsn.Decode(ref asnValueReader, rebind, out decoded.Prf);
			}
			else
			{
				AsnValueReader asnValueReader2 = new AsnValueReader(Pbkdf2Params.s_DefaultPrf, AsnEncodingRules.DER);
				AlgorithmIdentifierAsn.Decode(ref asnValueReader2, rebind, out decoded.Prf);
			}
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x04000E0B RID: 3595
		private static readonly byte[] s_DefaultPrf = new byte[]
		{
			48, 12, 6, 8, 42, 134, 72, 134, 247, 13,
			2, 7, 5, 0
		};

		// Token: 0x04000E0C RID: 3596
		internal Pbkdf2SaltChoice Salt;

		// Token: 0x04000E0D RID: 3597
		internal int IterationCount;

		// Token: 0x04000E0E RID: 3598
		internal int? KeyLength;

		// Token: 0x04000E0F RID: 3599
		internal AlgorithmIdentifierAsn Prf;
	}
}
