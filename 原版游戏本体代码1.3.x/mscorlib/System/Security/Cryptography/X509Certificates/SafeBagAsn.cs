using System;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002CF RID: 719
	internal struct SafeBagAsn
	{
		// Token: 0x06002561 RID: 9569 RVA: 0x00088744 File Offset: 0x00086944
		internal static SafeBagAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return SafeBagAsn.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x06002562 RID: 9570 RVA: 0x00088754 File Offset: 0x00086954
		internal static SafeBagAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			SafeBagAsn result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				SafeBagAsn safeBagAsn;
				SafeBagAsn.DecodeCore(ref asnValueReader, expectedTag, encoded, out safeBagAsn);
				asnValueReader.ThrowIfNotEmpty();
				result = safeBagAsn;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x06002563 RID: 9571 RVA: 0x000887A4 File Offset: 0x000869A4
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out SafeBagAsn decoded)
		{
			SafeBagAsn.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x06002564 RID: 9572 RVA: 0x000887B4 File Offset: 0x000869B4
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SafeBagAsn decoded)
		{
			try
			{
				SafeBagAsn.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x06002565 RID: 9573 RVA: 0x000887EC File Offset: 0x000869EC
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out SafeBagAsn decoded)
		{
			decoded = default(SafeBagAsn);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			ReadOnlySpan<byte> span = rebind.Span;
			decoded.BagId = asnValueReader.ReadObjectIdentifier();
			AsnValueReader asnValueReader2 = asnValueReader.ReadSequence(new Asn1Tag?(new Asn1Tag(TagClass.ContextSpecific, 0)));
			ReadOnlySpan<byte> destination = asnValueReader2.ReadEncodedValue();
			int start;
			decoded.BagValue = (span.Overlaps(destination, out start) ? rebind.Slice(start, destination.Length) : destination.ToArray());
			asnValueReader2.ThrowIfNotEmpty();
			if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(Asn1Tag.SetOf))
			{
				AsnValueReader asnValueReader3 = asnValueReader.ReadSetOf();
				List<AttributeAsn> list = new List<AttributeAsn>();
				while (asnValueReader3.HasData)
				{
					AttributeAsn item;
					AttributeAsn.Decode(ref asnValueReader3, rebind, out item);
					list.Add(item);
				}
				decoded.BagAttributes = list.ToArray();
			}
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x04000E19 RID: 3609
		internal byte[] BagId;

		// Token: 0x04000E1A RID: 3610
		internal ReadOnlyMemory<byte> BagValue;

		// Token: 0x04000E1B RID: 3611
		internal AttributeAsn[] BagAttributes;
	}
}
