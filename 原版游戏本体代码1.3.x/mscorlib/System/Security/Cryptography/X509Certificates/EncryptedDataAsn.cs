using System;
using System.Collections.Generic;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002C6 RID: 710
	internal struct EncryptedDataAsn
	{
		// Token: 0x06002530 RID: 9520 RVA: 0x00087638 File Offset: 0x00085838
		internal static EncryptedDataAsn Decode(ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			return EncryptedDataAsn.Decode(Asn1Tag.Sequence, encoded, ruleSet);
		}

		// Token: 0x06002531 RID: 9521 RVA: 0x00087648 File Offset: 0x00085848
		internal static EncryptedDataAsn Decode(Asn1Tag expectedTag, ReadOnlyMemory<byte> encoded, AsnEncodingRules ruleSet)
		{
			EncryptedDataAsn result;
			try
			{
				AsnValueReader asnValueReader = new AsnValueReader(encoded.Span, ruleSet);
				EncryptedDataAsn encryptedDataAsn;
				EncryptedDataAsn.DecodeCore(ref asnValueReader, expectedTag, encoded, out encryptedDataAsn);
				asnValueReader.ThrowIfNotEmpty();
				result = encryptedDataAsn;
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
			return result;
		}

		// Token: 0x06002532 RID: 9522 RVA: 0x00087698 File Offset: 0x00085898
		internal static void Decode(ref AsnValueReader reader, ReadOnlyMemory<byte> rebind, out EncryptedDataAsn decoded)
		{
			EncryptedDataAsn.Decode(ref reader, Asn1Tag.Sequence, rebind, out decoded);
		}

		// Token: 0x06002533 RID: 9523 RVA: 0x000876A8 File Offset: 0x000858A8
		internal static void Decode(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedDataAsn decoded)
		{
			try
			{
				EncryptedDataAsn.DecodeCore(ref reader, expectedTag, rebind, out decoded);
			}
			catch (InvalidOperationException inner)
			{
				throw new CryptographicException("ASN1 corrupted data.", inner);
			}
		}

		// Token: 0x06002534 RID: 9524 RVA: 0x000876E0 File Offset: 0x000858E0
		private static void DecodeCore(ref AsnValueReader reader, Asn1Tag expectedTag, ReadOnlyMemory<byte> rebind, out EncryptedDataAsn decoded)
		{
			decoded = default(EncryptedDataAsn);
			AsnValueReader asnValueReader = reader.ReadSequence(new Asn1Tag?(expectedTag));
			if (!asnValueReader.TryReadInt32(out decoded.Version))
			{
				asnValueReader.ThrowIfNotEmpty();
			}
			EncryptedContentInfoAsn.Decode(ref asnValueReader, rebind, out decoded.EncryptedContentInfo);
			if (asnValueReader.HasData && asnValueReader.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 1)))
			{
				AsnValueReader asnValueReader2 = asnValueReader.ReadSetOf(new Asn1Tag?(new Asn1Tag(TagClass.ContextSpecific, 1)));
				List<AttributeAsn> list = new List<AttributeAsn>();
				while (asnValueReader2.HasData)
				{
					AttributeAsn item;
					AttributeAsn.Decode(ref asnValueReader2, rebind, out item);
					list.Add(item);
				}
				decoded.UnprotectedAttributes = list.ToArray();
			}
			asnValueReader.ThrowIfNotEmpty();
		}

		// Token: 0x04000DFE RID: 3582
		internal int Version;

		// Token: 0x04000DFF RID: 3583
		internal EncryptedContentInfoAsn EncryptedContentInfo;

		// Token: 0x04000E00 RID: 3584
		internal AttributeAsn[] UnprotectedAttributes;
	}
}
