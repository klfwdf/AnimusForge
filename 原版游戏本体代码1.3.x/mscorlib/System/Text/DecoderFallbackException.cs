using System;
using System.Runtime.Serialization;

namespace System.Text
{
	// Token: 0x02000A63 RID: 2659
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class DecoderFallbackException : ArgumentException
	{
		// Token: 0x06006799 RID: 26521 RVA: 0x0015E149 File Offset: 0x0015C349
		[__DynamicallyInvokable]
		public DecoderFallbackException()
			: base(Environment.GetResourceString("Arg_ArgumentException"))
		{
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x0600679A RID: 26522 RVA: 0x0015E166 File Offset: 0x0015C366
		[__DynamicallyInvokable]
		public DecoderFallbackException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x0600679B RID: 26523 RVA: 0x0015E17A File Offset: 0x0015C37A
		[__DynamicallyInvokable]
		public DecoderFallbackException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x0600679C RID: 26524 RVA: 0x0015E18F File Offset: 0x0015C38F
		internal DecoderFallbackException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x0600679D RID: 26525 RVA: 0x0015E199 File Offset: 0x0015C399
		[__DynamicallyInvokable]
		public DecoderFallbackException(string message, byte[] bytesUnknown, int index)
			: base(message)
		{
			this.bytesUnknown = bytesUnknown;
			this.index = index;
		}

		// Token: 0x170011A3 RID: 4515
		// (get) Token: 0x0600679E RID: 26526 RVA: 0x0015E1B0 File Offset: 0x0015C3B0
		[__DynamicallyInvokable]
		public byte[] BytesUnknown
		{
			[__DynamicallyInvokable]
			get
			{
				return this.bytesUnknown;
			}
		}

		// Token: 0x170011A4 RID: 4516
		// (get) Token: 0x0600679F RID: 26527 RVA: 0x0015E1B8 File Offset: 0x0015C3B8
		[__DynamicallyInvokable]
		public int Index
		{
			[__DynamicallyInvokable]
			get
			{
				return this.index;
			}
		}

		// Token: 0x04002E49 RID: 11849
		private byte[] bytesUnknown;

		// Token: 0x04002E4A RID: 11850
		private int index;
	}
}
