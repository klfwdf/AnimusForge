using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002C1 RID: 705
	internal struct AlgorithmIdentifierAsn
	{
		// Token: 0x06002514 RID: 9492 RVA: 0x00086F56 File Offset: 0x00085156
		internal static AlgorithmIdentifierAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return AlgorithmIdentifierAsn.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x06002515 RID: 9493 RVA: 0x00086F64 File Offset: 0x00085164
		internal static AlgorithmIdentifierAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			AlgorithmIdentifierAsn result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				AlgorithmIdentifierAsn algorithmIdentifierAsn;
				AlgorithmIdentifierAsn.DecodeCore(ref asnValueReader, expectedTag, encoded, out algorithmIdentifierAsn);
				asnValueReader.ThrowIfNotEmpty();
				result = algorithmIdentifierAsn;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x06002516 RID: 9494 RVA: 0x00086FB4 File Offset: 0x000851B4
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out AlgorithmIdentifierAsn decoded)
		{
			AlgorithmIdentifierAsn.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x06002517 RID: 9495 RVA: 0x00086FC4 File Offset: 0x000851C4
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out AlgorithmIdentifierAsn decoded)
		{
			try
			{
				AlgorithmIdentifierAsn.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x06002518 RID: 9496 RVA: 0x00086FFC File Offset: 0x000851FC
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out AlgorithmIdentifierAsn decoded)
		{
			decoded = default(AlgorithmIdentifierAsn);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			ReadOnlySpan<byte> span = rebind.Span;
			decoded.Algorithm = asnValueReader.ReadObjectIdentifier();
			if (asnValueReader.HasData)
			{
				ReadOnlySpan<byte> destination = asnValueReader.ReadEncodedValue();
				int start;
				decoded.Parameters = new ReadOnlyMemory<byte>?(span.Overlaps(destination, out start) ? rebind.Slice(start, destination.Length) : destination.ToArray());
			}
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x06002519 RID: 9497 RVA: 0x0008707E File Offset: 0x0008527E
		internal bool HasNullEquivalentParameters()
		{
			return AlgorithmIdentifierAsn.RepresentsNull(this.Parameters);
		}

		// Token: 0x0600251A RID: 9498 RVA: 0x0008708C File Offset: 0x0008528C
		internal static bool RepresentsNull(ReadOnlyMemory<byte>? parameters)
		{
			if (parameters == null)
			{
				return true;
			}
			ReadOnlySpan<byte> span = parameters.Value.Span;
			return span.Length == 2 && span[0] == 5 && span[1] == 0;
		}

		// Token: 0x0600251B RID: 9499 RVA: 0x000870D8 File Offset: 0x000852D8
		// Note: this type is marked as 'beforefieldinit'.
		static AlgorithmIdentifierAsn()
		{
			byte[] array = new byte[2];
			array[0] = 5;
			AlgorithmIdentifierAsn.ExplicitDerNull = array;
		}

		// Token: 0x04000DF2 RID: 3570
		internal byte[] Algorithm;

		// Token: 0x04000DF3 RID: 3571
		internal ReadOnlyMemory<byte>? Parameters;

		// Token: 0x04000DF4 RID: 3572
		internal static readonly ReadOnlyMemory<byte> ExplicitDerNull;
	}
}
