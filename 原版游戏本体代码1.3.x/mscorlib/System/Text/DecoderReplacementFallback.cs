using System;

namespace System.Text
{
	// Token: 0x02000A66 RID: 2662
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class DecoderReplacementFallback : DecoderFallback
	{
		// Token: 0x060067B2 RID: 26546 RVA: 0x0015E4B2 File Offset: 0x0015C6B2
		[__DynamicallyInvokable]
		public DecoderReplacementFallback()
			: this("?")
		{
		}

		// Token: 0x060067B3 RID: 26547 RVA: 0x0015E4C0 File Offset: 0x0015C6C0
		[__DynamicallyInvokable]
		public DecoderReplacementFallback(string replacement)
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

		// Token: 0x170011AB RID: 4523
		// (get) Token: 0x060067B4 RID: 26548 RVA: 0x0015E543 File Offset: 0x0015C743
		[__DynamicallyInvokable]
		public string DefaultString
		{
			[__DynamicallyInvokable]
			get
			{
				return this.strDefault;
			}
		}

		// Token: 0x060067B5 RID: 26549 RVA: 0x0015E54B File Offset: 0x0015C74B
		[__DynamicallyInvokable]
		public override DecoderFallbackBuffer CreateFallbackBuffer()
		{
			return new DecoderReplacementFallbackBuffer(this);
		}

		// Token: 0x170011AC RID: 4524
		// (get) Token: 0x060067B6 RID: 26550 RVA: 0x0015E553 File Offset: 0x0015C753
		[__DynamicallyInvokable]
		public override int MaxCharCount
		{
			[__DynamicallyInvokable]
			get
			{
				return this.strDefault.Length;
			}
		}

		// Token: 0x060067B7 RID: 26551 RVA: 0x0015E560 File Offset: 0x0015C760
		[__DynamicallyInvokable]
		public override bool Equals(object value)
		{
			DecoderReplacementFallback decoderReplacementFallback = value as DecoderReplacementFallback;
			return decoderReplacementFallback != null && this.strDefault == decoderReplacementFallback.strDefault;
		}

		// Token: 0x060067B8 RID: 26552 RVA: 0x0015E58A File Offset: 0x0015C78A
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return this.strDefault.GetHashCode();
		}

		// Token: 0x04002E51 RID: 11857
		private string strDefault;
	}
}
