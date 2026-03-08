using System;

namespace System.Text
{
	// Token: 0x02000A5F RID: 2655
	[Serializable]
	internal sealed class InternalDecoderBestFitFallback : DecoderFallback
	{
		// Token: 0x06006780 RID: 26496 RVA: 0x0015DD95 File Offset: 0x0015BF95
		internal InternalDecoderBestFitFallback(Encoding encoding)
		{
			this.encoding = encoding;
			this.bIsMicrosoftBestFitFallback = true;
		}

		// Token: 0x06006781 RID: 26497 RVA: 0x0015DDB3 File Offset: 0x0015BFB3
		public override DecoderFallbackBuffer CreateFallbackBuffer()
		{
			return new InternalDecoderBestFitFallbackBuffer(this);
		}

		// Token: 0x1700119E RID: 4510
		// (get) Token: 0x06006782 RID: 26498 RVA: 0x0015DDBB File Offset: 0x0015BFBB
		public override int MaxCharCount
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x06006783 RID: 26499 RVA: 0x0015DDC0 File Offset: 0x0015BFC0
		public override bool Equals(object value)
		{
			InternalDecoderBestFitFallback internalDecoderBestFitFallback = value as InternalDecoderBestFitFallback;
			return internalDecoderBestFitFallback != null && this.encoding.CodePage == internalDecoderBestFitFallback.encoding.CodePage;
		}

		// Token: 0x06006784 RID: 26500 RVA: 0x0015DDF1 File Offset: 0x0015BFF1
		public override int GetHashCode()
		{
			return this.encoding.CodePage;
		}

		// Token: 0x04002E41 RID: 11841
		internal Encoding encoding;

		// Token: 0x04002E42 RID: 11842
		internal char[] arrayBestFit;

		// Token: 0x04002E43 RID: 11843
		internal char cReplacement = '?';
	}
}
