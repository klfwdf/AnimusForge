using System;
using System.Globalization;

namespace System.Text
{
	// Token: 0x02000A62 RID: 2658
	public sealed class DecoderExceptionFallbackBuffer : DecoderFallbackBuffer
	{
		// Token: 0x06006793 RID: 26515 RVA: 0x0015E095 File Offset: 0x0015C295
		public override bool Fallback(byte[] bytesUnknown, int index)
		{
			this.Throw(bytesUnknown, index);
			return true;
		}

		// Token: 0x06006794 RID: 26516 RVA: 0x0015E0A0 File Offset: 0x0015C2A0
		public override char GetNextChar()
		{
			return '\0';
		}

		// Token: 0x06006795 RID: 26517 RVA: 0x0015E0A3 File Offset: 0x0015C2A3
		public override bool MovePrevious()
		{
			return false;
		}

		// Token: 0x170011A2 RID: 4514
		// (get) Token: 0x06006796 RID: 26518 RVA: 0x0015E0A6 File Offset: 0x0015C2A6
		public override int Remaining
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x06006797 RID: 26519 RVA: 0x0015E0AC File Offset: 0x0015C2AC
		private void Throw(byte[] bytesUnknown, int index)
		{
			StringBuilder stringBuilder = new StringBuilder(bytesUnknown.Length * 3);
			int num = 0;
			while (num < bytesUnknown.Length && num < 20)
			{
				stringBuilder.Append("[");
				stringBuilder.Append(bytesUnknown[num].ToString("X2", CultureInfo.InvariantCulture));
				stringBuilder.Append("]");
				num++;
			}
			if (num == 20)
			{
				stringBuilder.Append(" ...");
			}
			throw new DecoderFallbackException(Environment.GetResourceString("Argument_InvalidCodePageBytesIndex", new object[] { stringBuilder, index }), bytesUnknown, index);
		}
	}
}
