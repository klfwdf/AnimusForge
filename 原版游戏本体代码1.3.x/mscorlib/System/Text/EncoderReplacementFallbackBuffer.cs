using System;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A72 RID: 2674
	public sealed class EncoderReplacementFallbackBuffer : EncoderFallbackBuffer
	{
		// Token: 0x06006819 RID: 26649 RVA: 0x0015F937 File Offset: 0x0015DB37
		public EncoderReplacementFallbackBuffer(EncoderReplacementFallback fallback)
		{
			this.strDefault = fallback.DefaultString + fallback.DefaultString;
		}

		// Token: 0x0600681A RID: 26650 RVA: 0x0015F964 File Offset: 0x0015DB64
		public override bool Fallback(char charUnknown, int index)
		{
			if (this.fallbackCount >= 1)
			{
				if (char.IsHighSurrogate(charUnknown) && this.fallbackCount >= 0 && char.IsLowSurrogate(this.strDefault[this.fallbackIndex + 1]))
				{
					base.ThrowLastCharRecursive(char.ConvertToUtf32(charUnknown, this.strDefault[this.fallbackIndex + 1]));
				}
				base.ThrowLastCharRecursive((int)charUnknown);
			}
			this.fallbackCount = this.strDefault.Length / 2;
			this.fallbackIndex = -1;
			return this.fallbackCount != 0;
		}

		// Token: 0x0600681B RID: 26651 RVA: 0x0015F9F0 File Offset: 0x0015DBF0
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
			if (this.fallbackCount >= 1)
			{
				base.ThrowLastCharRecursive(char.ConvertToUtf32(charUnknownHigh, charUnknownLow));
			}
			this.fallbackCount = this.strDefault.Length;
			this.fallbackIndex = -1;
			return this.fallbackCount != 0;
		}

		// Token: 0x0600681C RID: 26652 RVA: 0x0015FAB0 File Offset: 0x0015DCB0
		public override char GetNextChar()
		{
			this.fallbackCount--;
			this.fallbackIndex++;
			if (this.fallbackCount < 0)
			{
				return '\0';
			}
			if (this.fallbackCount == 2147483647)
			{
				this.fallbackCount = -1;
				return '\0';
			}
			return this.strDefault[this.fallbackIndex];
		}

		// Token: 0x0600681D RID: 26653 RVA: 0x0015FB0B File Offset: 0x0015DD0B
		public override bool MovePrevious()
		{
			if (this.fallbackCount >= -1 && this.fallbackIndex >= 0)
			{
				this.fallbackIndex--;
				this.fallbackCount++;
				return true;
			}
			return false;
		}

		// Token: 0x170011C4 RID: 4548
		// (get) Token: 0x0600681E RID: 26654 RVA: 0x0015FB3E File Offset: 0x0015DD3E
		public override int Remaining
		{
			get
			{
				if (this.fallbackCount >= 0)
				{
					return this.fallbackCount;
				}
				return 0;
			}
		}

		// Token: 0x0600681F RID: 26655 RVA: 0x0015FB51 File Offset: 0x0015DD51
		[SecuritySafeCritical]
		public override void Reset()
		{
			this.fallbackCount = -1;
			this.fallbackIndex = 0;
			this.charStart = null;
			this.bFallingBack = false;
		}

		// Token: 0x04002E74 RID: 11892
		private string strDefault;

		// Token: 0x04002E75 RID: 11893
		private int fallbackCount = -1;

		// Token: 0x04002E76 RID: 11894
		private int fallbackIndex = -1;
	}
}
