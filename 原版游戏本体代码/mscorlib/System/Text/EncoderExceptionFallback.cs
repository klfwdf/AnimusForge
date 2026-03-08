using System;

namespace System.Text
{
	// Token: 0x02000A6C RID: 2668
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class EncoderExceptionFallback : EncoderFallback
	{
		// Token: 0x060067EA RID: 26602 RVA: 0x0015F347 File Offset: 0x0015D547
		[__DynamicallyInvokable]
		public EncoderExceptionFallback()
		{
		}

		// Token: 0x060067EB RID: 26603 RVA: 0x0015F34F File Offset: 0x0015D54F
		[__DynamicallyInvokable]
		public override EncoderFallbackBuffer CreateFallbackBuffer()
		{
			return new EncoderExceptionFallbackBuffer();
		}

		// Token: 0x170011B7 RID: 4535
		// (get) Token: 0x060067EC RID: 26604 RVA: 0x0015F356 File Offset: 0x0015D556
		[__DynamicallyInvokable]
		public override int MaxCharCount
		{
			[__DynamicallyInvokable]
			get
			{
				return 0;
			}
		}

		// Token: 0x060067ED RID: 26605 RVA: 0x0015F35C File Offset: 0x0015D55C
		[__DynamicallyInvokable]
		public override bool Equals(object value)
		{
			return value is EncoderExceptionFallback;
		}

		// Token: 0x060067EE RID: 26606 RVA: 0x0015F376 File Offset: 0x0015D576
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return 654;
		}
	}
}
