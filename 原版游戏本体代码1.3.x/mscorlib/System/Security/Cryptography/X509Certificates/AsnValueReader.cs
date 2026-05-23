using System;

namespace System.Security.Cryptography.X509Certificates
{
	// Token: 0x020002B7 RID: 695
	internal struct AsnValueReader
	{
		// Token: 0x060024D5 RID: 9429 RVA: 0x0008558F File Offset: 0x0008378F
		internal AsnValueReader(ReadOnlySpan<byte> span, AsnEncodingRules ruleSet)
		{
			this._span = span;
			this._ruleSet = ruleSet;
		}

		// Token: 0x1700049B RID: 1179
		// (get) Token: 0x060024D6 RID: 9430 RVA: 0x0008559F File Offset: 0x0008379F
		internal bool HasData
		{
			get
			{
				return !this._span.IsEmpty;
			}
		}

		// Token: 0x060024D7 RID: 9431 RVA: 0x000855AF File Offset: 0x000837AF
		internal void ThrowIfNotEmpty()
		{
			if (!this._span.IsEmpty)
			{
				new AsnReader(AsnValueReader.s_singleByte, this._ruleSet).ThrowIfNotEmpty();
			}
		}

		// Token: 0x060024D8 RID: 9432 RVA: 0x000855D8 File Offset: 0x000837D8
		internal Asn1Tag PeekTag()
		{
			int num;
			return Asn1Tag.Decode(this._span, out num);
		}

		// Token: 0x060024D9 RID: 9433 RVA: 0x000855F4 File Offset: 0x000837F4
		internal ReadOnlySpan<byte> PeekContentBytes()
		{
			int start;
			int length;
			int num;
			AsnDecoder.ReadEncodedValue(this._span, this._ruleSet, out start, out length, out num);
			return this._span.Slice(start, length);
		}

		// Token: 0x060024DA RID: 9434 RVA: 0x00085628 File Offset: 0x00083828
		internal ReadOnlySpan<byte> PeekEncodedValue()
		{
			int num;
			int num2;
			int length;
			AsnDecoder.ReadEncodedValue(this._span, this._ruleSet, out num, out num2, out length);
			return this._span.Slice(0, length);
		}

		// Token: 0x060024DB RID: 9435 RVA: 0x0008565C File Offset: 0x0008385C
		internal ReadOnlySpan<byte> ReadEncodedValue()
		{
			ReadOnlySpan<byte> result = this.PeekEncodedValue();
			this._span = this._span.Slice(result.Length);
			return result;
		}

		// Token: 0x060024DC RID: 9436 RVA: 0x0008568C File Offset: 0x0008388C
		internal bool TryReadInt32(out int value)
		{
			return this.TryReadInt32(out value, null);
		}

		// Token: 0x060024DD RID: 9437 RVA: 0x000856AC File Offset: 0x000838AC
		internal bool TryReadInt32(out int value, Asn1Tag? expectedTag)
		{
			int start;
			bool result = AsnDecoder.TryReadInt32(this._span, this._ruleSet, out value, out start, expectedTag);
			this._span = this._span.Slice(start);
			return result;
		}

		// Token: 0x060024DE RID: 9438 RVA: 0x000856E4 File Offset: 0x000838E4
		internal ReadOnlySpan<byte> ReadIntegerBytes()
		{
			return this.ReadIntegerBytes(null);
		}

		// Token: 0x060024DF RID: 9439 RVA: 0x00085700 File Offset: 0x00083900
		internal ReadOnlySpan<byte> ReadIntegerBytes(Asn1Tag? expectedTag)
		{
			int start;
			ReadOnlySpan<byte> result = AsnDecoder.ReadIntegerBytes(this._span, this._ruleSet, out start, expectedTag);
			this._span = this._span.Slice(start);
			return result;
		}

		// Token: 0x060024E0 RID: 9440 RVA: 0x00085738 File Offset: 0x00083938
		internal bool TryReadPrimitiveBitString(out int unusedBitCount, out ReadOnlySpan<byte> value)
		{
			return this.TryReadPrimitiveBitString(out unusedBitCount, out value, null);
		}

		// Token: 0x060024E1 RID: 9441 RVA: 0x00085758 File Offset: 0x00083958
		internal bool TryReadPrimitiveBitString(out int unusedBitCount, out ReadOnlySpan<byte> value, Asn1Tag? expectedTag)
		{
			int start;
			bool result = AsnDecoder.TryReadPrimitiveBitString(this._span, this._ruleSet, out unusedBitCount, out value, out start, expectedTag);
			this._span = this._span.Slice(start);
			return result;
		}

		// Token: 0x060024E2 RID: 9442 RVA: 0x00085790 File Offset: 0x00083990
		internal byte[] ReadBitString(out int unusedBitCount)
		{
			return this.ReadBitString(out unusedBitCount, null);
		}

		// Token: 0x060024E3 RID: 9443 RVA: 0x000857B0 File Offset: 0x000839B0
		internal byte[] ReadBitString(out int unusedBitCount, Asn1Tag? expectedTag)
		{
			int start;
			byte[] result = AsnDecoder.ReadBitString(this._span, this._ruleSet, out unusedBitCount, out start, expectedTag);
			this._span = this._span.Slice(start);
			return result;
		}

		// Token: 0x060024E4 RID: 9444 RVA: 0x000857E8 File Offset: 0x000839E8
		internal bool TryReadPrimitiveOctetString(out ReadOnlySpan<byte> value)
		{
			return this.TryReadPrimitiveOctetString(out value, null);
		}

		// Token: 0x060024E5 RID: 9445 RVA: 0x00085808 File Offset: 0x00083A08
		internal bool TryReadPrimitiveOctetString(out ReadOnlySpan<byte> value, Asn1Tag? expectedTag)
		{
			int start;
			bool result = AsnDecoder.TryReadPrimitiveOctetString(this._span, this._ruleSet, out value, out start, expectedTag);
			this._span = this._span.Slice(start);
			return result;
		}

		// Token: 0x060024E6 RID: 9446 RVA: 0x00085840 File Offset: 0x00083A40
		internal byte[] ReadOctetString()
		{
			return this.ReadOctetString(null);
		}

		// Token: 0x060024E7 RID: 9447 RVA: 0x0008585C File Offset: 0x00083A5C
		internal byte[] ReadOctetString(Asn1Tag? expectedTag)
		{
			int start;
			byte[] result = AsnDecoder.ReadOctetString(this._span, this._ruleSet, out start, expectedTag);
			this._span = this._span.Slice(start);
			return result;
		}

		// Token: 0x060024E8 RID: 9448 RVA: 0x00085894 File Offset: 0x00083A94
		internal byte[] ReadObjectIdentifier()
		{
			return this.ReadObjectIdentifier(null);
		}

		// Token: 0x060024E9 RID: 9449 RVA: 0x000858B0 File Offset: 0x00083AB0
		internal byte[] ReadObjectIdentifier(Asn1Tag? expectedTag)
		{
			int start;
			byte[] result = AsnDecoder.ReadObjectIdentifier(this._span, this._ruleSet, out start, expectedTag);
			this._span = this._span.Slice(start);
			return result;
		}

		// Token: 0x060024EA RID: 9450 RVA: 0x000858E8 File Offset: 0x00083AE8
		internal AsnValueReader ReadSequence()
		{
			return this.ReadSequence(null);
		}

		// Token: 0x060024EB RID: 9451 RVA: 0x00085904 File Offset: 0x00083B04
		internal AsnValueReader ReadSequence(Asn1Tag? expectedTag)
		{
			int start;
			int length;
			int start2;
			AsnDecoder.ReadSequence(this._span, this._ruleSet, out start, out length, out start2, expectedTag);
			ReadOnlySpan<byte> span = this._span.Slice(start, length);
			this._span = this._span.Slice(start2);
			return new AsnValueReader(span, this._ruleSet);
		}

		// Token: 0x060024EC RID: 9452 RVA: 0x00085958 File Offset: 0x00083B58
		internal AsnValueReader ReadSetOf()
		{
			return this.ReadSetOf(null, false);
		}

		// Token: 0x060024ED RID: 9453 RVA: 0x00085975 File Offset: 0x00083B75
		internal AsnValueReader ReadSetOf(Asn1Tag? expectedTag)
		{
			return this.ReadSetOf(expectedTag, false);
		}

		// Token: 0x060024EE RID: 9454 RVA: 0x00085980 File Offset: 0x00083B80
		internal AsnValueReader ReadSetOf(Asn1Tag? expectedTag, bool skipSortOrderValidation)
		{
			int start;
			int length;
			int start2;
			AsnDecoder.ReadSetOf(this._span, this._ruleSet, out start, out length, out start2, skipSortOrderValidation, expectedTag);
			ReadOnlySpan<byte> span = this._span.Slice(start, length);
			this._span = this._span.Slice(start2);
			return new AsnValueReader(span, this._ruleSet);
		}

		// Token: 0x04000DC9 RID: 3529
		private static readonly byte[] s_singleByte = new byte[1];

		// Token: 0x04000DCA RID: 3530
		private ReadOnlySpan<byte> _span;

		// Token: 0x04000DCB RID: 3531
		private readonly AsnEncodingRules _ruleSet;
	}
}
