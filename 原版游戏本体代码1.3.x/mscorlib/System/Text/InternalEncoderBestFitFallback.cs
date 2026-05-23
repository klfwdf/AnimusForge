using System;

namespace System.Text
{
	// Token: 0x02000A6A RID: 2666
	[Serializable]
	internal class InternalEncoderBestFitFallback : EncoderFallback
	{
		// Token: 0x060067DC RID: 26588 RVA: 0x0015F035 File Offset: 0x0015D235
		internal InternalEncoderBestFitFallback(Encoding encoding)
		{
			this.encoding = encoding;
			this.bIsMicrosoftBestFitFallback = true;
		}

		// Token: 0x060067DD RID: 26589 RVA: 0x0015F04B File Offset: 0x0015D24B
		public override EncoderFallbackBuffer CreateFallbackBuffer()
		{
			return new InternalEncoderBestFitFallbackBuffer(this);
		}

		// Token: 0x170011B4 RID: 4532
		// (get) Token: 0x060067DE RID: 26590 RVA: 0x0015F053 File Offset: 0x0015D253
		public override int MaxCharCount
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x060067DF RID: 26591 RVA: 0x0015F058 File Offset: 0x0015D258
		public override bool Equals(object value)
		{
			InternalEncoderBestFitFallback internalEncoderBestFitFallback = value as InternalEncoderBestFitFallback;
			return internalEncoderBestFitFallback != null && this.encoding.CodePage == internalEncoderBestFitFallback.encoding.CodePage;
		}

		// Token: 0x060067E0 RID: 26592 RVA: 0x0015F089 File Offset: 0x0015D289
		public override int GetHashCode()
		{
			return this.encoding.CodePage;
		}

		// Token: 0x04002E5C RID: 11868
		internal Encoding encoding;

		// Token: 0x04002E5D RID: 11869
		internal char[] arrayBestFit;
	}
}
