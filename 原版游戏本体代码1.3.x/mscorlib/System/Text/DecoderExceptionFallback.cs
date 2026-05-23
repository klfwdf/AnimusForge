using System;

namespace System.Text
{
	// Token: 0x02000A61 RID: 2657
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class DecoderExceptionFallback : DecoderFallback
	{
		// Token: 0x0600678E RID: 26510 RVA: 0x0015E05F File Offset: 0x0015C25F
		[__DynamicallyInvokable]
		public DecoderExceptionFallback()
		{
		}

		// Token: 0x0600678F RID: 26511 RVA: 0x0015E067 File Offset: 0x0015C267
		[__DynamicallyInvokable]
		public override DecoderFallbackBuffer CreateFallbackBuffer()
		{
			return new DecoderExceptionFallbackBuffer();
		}

		// Token: 0x170011A1 RID: 4513
		// (get) Token: 0x06006790 RID: 26512 RVA: 0x0015E06E File Offset: 0x0015C26E
		[__DynamicallyInvokable]
		public override int MaxCharCount
		{
			[__DynamicallyInvokable]
			get
			{
				return 0;
			}
		}

		// Token: 0x06006791 RID: 26513 RVA: 0x0015E074 File Offset: 0x0015C274
		[__DynamicallyInvokable]
		public override bool Equals(object value)
		{
			return value is DecoderExceptionFallback;
		}

		// Token: 0x06006792 RID: 26514 RVA: 0x0015E08E File Offset: 0x0015C28E
		[__DynamicallyInvokable]
		public override int GetHashCode()
		{
			return 879;
		}
	}
}
