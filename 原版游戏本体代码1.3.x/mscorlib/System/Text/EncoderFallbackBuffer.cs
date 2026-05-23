using System;
using System.Security;

namespace System.Text
{
	// Token: 0x02000A70 RID: 2672
	[__DynamicallyInvokable]
	public abstract class EncoderFallbackBuffer
	{
		// Token: 0x06006806 RID: 26630
		[__DynamicallyInvokable]
		public abstract bool Fallback(char charUnknown, int index);

		// Token: 0x06006807 RID: 26631
		[__DynamicallyInvokable]
		public abstract bool Fallback(char charUnknownHigh, char charUnknownLow, int index);

		// Token: 0x06006808 RID: 26632
		[__DynamicallyInvokable]
		public abstract char GetNextChar();

		// Token: 0x06006809 RID: 26633
		[__DynamicallyInvokable]
		public abstract bool MovePrevious();

		// Token: 0x170011C1 RID: 4545
		// (get) Token: 0x0600680A RID: 26634
		[__DynamicallyInvokable]
		public abstract int Remaining
		{
			[__DynamicallyInvokable]
			get;
		}

		// Token: 0x0600680B RID: 26635 RVA: 0x0015F6A0 File Offset: 0x0015D8A0
		[__DynamicallyInvokable]
		public virtual void Reset()
		{
			while (this.GetNextChar() != '\0')
			{
			}
		}

		// Token: 0x0600680C RID: 26636 RVA: 0x0015F6AA File Offset: 0x0015D8AA
		[SecurityCritical]
		internal void InternalReset()
		{
			this.charStart = null;
			this.bFallingBack = false;
			this.iRecursionCount = 0;
			this.Reset();
		}

		// Token: 0x0600680D RID: 26637 RVA: 0x0015F6C8 File Offset: 0x0015D8C8
		[SecurityCritical]
		internal unsafe void InternalInitialize(char* charStart, char* charEnd, EncoderNLS encoder, bool setEncoder)
		{
			this.charStart = charStart;
			this.charEnd = charEnd;
			this.encoder = encoder;
			this.setEncoder = setEncoder;
			this.bUsedEncoder = false;
			this.bFallingBack = false;
			this.iRecursionCount = 0;
		}

		// Token: 0x0600680E RID: 26638 RVA: 0x0015F6FC File Offset: 0x0015D8FC
		internal char InternalGetNextChar()
		{
			char nextChar = this.GetNextChar();
			this.bFallingBack = nextChar > '\0';
			if (nextChar == '\0')
			{
				this.iRecursionCount = 0;
			}
			return nextChar;
		}

		// Token: 0x0600680F RID: 26639 RVA: 0x0015F728 File Offset: 0x0015D928
		[SecurityCritical]
		internal unsafe virtual bool InternalFallback(char ch, ref char* chars)
		{
			int index = (chars - this.charStart) / 2 - 1;
			if (char.IsHighSurrogate(ch))
			{
				if (chars >= this.charEnd)
				{
					if (this.encoder != null && !this.encoder.MustFlush)
					{
						if (this.setEncoder)
						{
							this.bUsedEncoder = true;
							this.encoder.charLeftOver = ch;
						}
						this.bFallingBack = false;
						return false;
					}
				}
				else
				{
					char c = (char)(*chars);
					if (char.IsLowSurrogate(c))
					{
						if (this.bFallingBack)
						{
							int num = this.iRecursionCount;
							this.iRecursionCount = num + 1;
							if (num > 250)
							{
								this.ThrowLastCharRecursive(char.ConvertToUtf32(ch, c));
							}
						}
						chars += 2;
						this.bFallingBack = this.Fallback(ch, c, index);
						return this.bFallingBack;
					}
				}
			}
			if (this.bFallingBack)
			{
				int num = this.iRecursionCount;
				this.iRecursionCount = num + 1;
				if (num > 250)
				{
					this.ThrowLastCharRecursive((int)ch);
				}
			}
			this.bFallingBack = this.Fallback(ch, index);
			return this.bFallingBack;
		}

		// Token: 0x06006810 RID: 26640 RVA: 0x0015F826 File Offset: 0x0015DA26
		internal void ThrowLastCharRecursive(int charRecursive)
		{
			throw new ArgumentException(Environment.GetResourceString("Argument_RecursiveFallback", new object[] { charRecursive }), "chars");
		}

		// Token: 0x06006811 RID: 26641 RVA: 0x0015F84B File Offset: 0x0015DA4B
		[__DynamicallyInvokable]
		protected EncoderFallbackBuffer()
		{
		}

		// Token: 0x04002E6B RID: 11883
		[SecurityCritical]
		internal unsafe char* charStart;

		// Token: 0x04002E6C RID: 11884
		[SecurityCritical]
		internal unsafe char* charEnd;

		// Token: 0x04002E6D RID: 11885
		internal EncoderNLS encoder;

		// Token: 0x04002E6E RID: 11886
		internal bool setEncoder;

		// Token: 0x04002E6F RID: 11887
		internal bool bUsedEncoder;

		// Token: 0x04002E70 RID: 11888
		internal bool bFallingBack;

		// Token: 0x04002E71 RID: 11889
		internal int iRecursionCount;

		// Token: 0x04002E72 RID: 11890
		private const int iMaxRecursion = 250;
	}
}
