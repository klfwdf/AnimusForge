using System;

namespace System.Text
{
	// Token: 0x02000A71 RID: 2673
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class EncoderReplacementFallback : EncoderFallback
	{
		// Token: 0x06006812 RID: 26642 RVA: 0x0015F853 File Offset: 0x0015DA53
		[__DynamicallyInvokable]
		public EncoderReplacementFallback()
			: this("?")
		{
		}

		// Token: 0x06006813 RID: 26643 RVA: 0x0015F860 File Offset: 0x0015DA60
		[__DynamicallyInvokable]
		public EncoderReplacementFallback(string replacement)
		{
			if (replacement == null)
			{
				throw new ArgumentNullException("replacement");
			}
			bool flag = false;
			for (int i = 0; i < replacement.Length; i++)
			{
				if (char.IsSurrogate(replacement, i))
				{
					if (char.IsHighSurrogate(replacement, i))
					{
						if (flag)
						{
							break;
						}
						flag = true;
					}
					else
					{
						if (!flag)
						{
							flag = true;
							break;
						}
						flag = false;
					}
				}
				else if (flag)
				{
					break;
				}
			}
			if (flag)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex", new object[] { "replacement" }));
			}
			this.strDefault = replacement;
		}

		// Token: 0x170011C2 RID: 4546
		// (get) Token: 0x06006814 RID: 26644 RVA: 0x0015F8E3 File Offset: 0x0015DAE3
		[__DynamicallyInvokable]
		public string DefaultString
		{
			[__DynamicallyInvokable]
			get
			{
				return this.strDefault;
			}
		}

		// Token: 0x06006815 RID: 26645 RVA: 0x0015F8EB File Offset: 0x0015DAEB
		[__DynamicallyInvokable]
		public override EncoderFallbackBuffer CreateFallbackBuffer()
		{
			return new EncoderReplacementFallbackBuffer(this);
		}

		// Token: 0x170011C3 RID: 4547
		// (get) Token: 0x06006816 RID: 26646 RVA: 0x0015F8F3 File Offset: 0x0015DAF3
		[__DynamicallyInvokable]
		public override int MaxCharCount
		{
			[__DynamicallyInvokable]
			get
			{
				return this.strDefault.Length;
			}
		}

		// Token: 0x06006817 RID: 26647 RVA: 0x0015F900 File Offset: 0x0015DB00
		[__DynamicallyInvokable]
		public override bool Equals(object value)
		{
			EncoderReplacementFallback encoderReplacementFallback = value as EncoderReplacementFallback;
			return encoderReplacementFallback != null && this.strDefault == encoderReplacementFallback.strDefault;
		}

		// Token: 0x06006818 RID: 26648 RVA: 0x0015F92A File Offset: 0x0015DB2A
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.strDefault.GetHashCode();
		}

		// Token: 0x04002E73 RID: 11891
		private string strDefault;
	}
}
