using System;

namespace System.Text
{
	// Token: 0x02000A6D RID: 2669
	public sealed class EncoderExceptionFallbackBuffer : EncoderFallbackBuffer
	{
		// Token: 0x060067F0 RID: 26608 RVA: 0x0015F385 File Offset: 0x0015D585
		public override bool Fallback(char charUnknown, int index)
		{
			throw new EncoderFallbackException(Environment.GetResourceString("Argument_InvalidCodePageConversionIndex", new object[]
			{
				(int)charUnknown,
				index
			}), charUnknown, index);
		}

		// Token: 0x060067F1 RID: 26609 RVA: 0x0015F3B0 File Offset: 0x0015D5B0
		public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
		{
			if (!char.IsHighSurrogate(charUnknownHigh))
			{
				throw new ArgumentOutOfRangeException("charUnknownHigh", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 55296, 56319 }));
			}
			if (!char.IsLowSurrogate(charUnknownLow))
			{
				throw new ArgumentOutOfRangeException("CharUnknownLow", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 56320, 57343 }));
			}
			int num = char.ConvertToUtf32(charUnknownHigh, charUnknownLow);
			throw new EncoderFallbackException(Environment.GetResourceString("Argument_InvalidCodePageConversionIndex", new object[] { num, index }), charUnknownHigh, charUnknownLow, index);
		}

		// Token: 0x060067F2 RID: 26610 RVA: 0x0015F469 File Offset: 0x0015D669
		public override char GetNextChar()
		{
			return '\0';
		}

		// Token: 0x060067F3 RID: 26611 RVA: 0x0015F46C File Offset: 0x0015D66C
		public override bool MovePrevious()
		{
			return false;
		}

		// Token: 0x170011B8 RID: 4536
		// (get) Token: 0x060067F4 RID: 26612 RVA: 0x0015F46F File Offset: 0x0015D66F
		public override int Remaining
		{
			get
			{
				return 0;
			}
		}
	}
}
