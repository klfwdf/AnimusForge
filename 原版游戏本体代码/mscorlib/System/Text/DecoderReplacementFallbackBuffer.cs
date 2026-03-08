using System;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A67 RID: 2663
	public sealed class DecoderReplacementFallbackBuffer : DecoderFallbackBuffer
	{
		// Token: 0x060067B9 RID: 26553 RVA: 0x0015E597 File Offset: 0x0015C797
		public DecoderReplacementFallbackBuffer(DecoderReplacementFallback fallback)
		{
			this.strDefault = fallback.DefaultString;
		}

		// Token: 0x060067BA RID: 26554 RVA: 0x0015E5B9 File Offset: 0x0015C7B9
		public override bool Fallback(byte[] bytesUnknown, int index)
		{
			if (this.fallbackCount >= 1)
			{
				base.ThrowLastBytesRecursive(bytesUnknown);
			}
			if (this.strDefault.Length == 0)
			{
				return false;
			}
			this.fallbackCount = this.strDefault.Length;
			this.fallbackIndex = -1;
			return true;
		}

		// Token: 0x060067BB RID: 26555 RVA: 0x0015E5F4 File Offset: 0x0015C7F4
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

		// Token: 0x060067BC RID: 26556 RVA: 0x0015E64F File Offset: 0x0015C84F
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

		// Token: 0x170011AD RID: 4525
		// (get) Token: 0x060067BD RID: 26557 RVA: 0x0015E682 File Offset: 0x0015C882
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

		// Token: 0x060067BE RID: 26558 RVA: 0x0015E695 File Offset: 0x0015C895
		[SecuritySafeCritical]
		public override void Reset()
		{
			this.fallbackCount = -1;
			this.fallbackIndex = -1;
			this.byteStart = null;
		}

		// Token: 0x060067BF RID: 26559 RVA: 0x0015E6AD File Offset: 0x0015C8AD
		[SecurityCritical]
		internal unsafe override int InternalFallback(byte[] bytes, byte* pBytes)
		{
			return this.strDefault.Length;
		}

		// Token: 0x04002E52 RID: 11858
		private string strDefault;

		// Token: 0x04002E53 RID: 11859
		private int fallbackCount = -1;

		// Token: 0x04002E54 RID: 11860
		private int fallbackIndex = -1;
	}
}
