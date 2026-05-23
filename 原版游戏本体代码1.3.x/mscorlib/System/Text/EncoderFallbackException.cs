using System;
using System.Runtime.Serialization;

namespace System.Text
{
	// Token: 0x02000A6E RID: 2670
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class EncoderFallbackException : ArgumentException
	{
		// Token: 0x060067F5 RID: 26613 RVA: 0x0015F472 File Offset: 0x0015D672
		[__DynamicallyInvokable]
		public EncoderFallbackException()
			: base(Environment.GetResourceString("Arg_ArgumentException"))
		{
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x060067F6 RID: 26614 RVA: 0x0015F48F File Offset: 0x0015D68F
		[__DynamicallyInvokable]
		public EncoderFallbackException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x060067F7 RID: 26615 RVA: 0x0015F4A3 File Offset: 0x0015D6A3
		[__DynamicallyInvokable]
		public EncoderFallbackException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147024809);
		}

		// Token: 0x060067F8 RID: 26616 RVA: 0x0015F4B8 File Offset: 0x0015D6B8
		internal EncoderFallbackException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x060067F9 RID: 26617 RVA: 0x0015F4C2 File Offset: 0x0015D6C2
		internal EncoderFallbackException(string message, char charUnknown, int index)
			: base(message)
		{
			this.charUnknown = charUnknown;
			this.index = index;
		}

		// Token: 0x060067FA RID: 26618 RVA: 0x0015F4DC File Offset: 0x0015D6DC
		internal EncoderFallbackException(string message, char charUnknownHigh, char charUnknownLow, int index)
			: base(message)
		{
			if (!char.IsHighSurrogate(charUnknownHigh))
			{
				throw new ArgumentOutOfRangeException("charUnknownHigh", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 55296, 56319 }));
			}
			if (!char.IsLowSurrogate(charUnknownLow))
			{
				throw new ArgumentOutOfRangeException("CharUnknownLow", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 56320, 57343 }));
			}
			this.charUnknownHigh = charUnknownHigh;
			this.charUnknownLow = charUnknownLow;
			this.index = index;
		}

		// Token: 0x170011B9 RID: 4537
		// (get) Token: 0x060067FB RID: 26619 RVA: 0x0015F580 File Offset: 0x0015D780
		[__DynamicallyInvokable]
		public char CharUnknown
		{
			[__DynamicallyInvokable]
			get
			{
				return this.charUnknown;
			}
		}

		// Token: 0x170011BA RID: 4538
		// (get) Token: 0x060067FC RID: 26620 RVA: 0x0015F588 File Offset: 0x0015D788
		[__DynamicallyInvokable]
		public char CharUnknownHigh
		{
			[__DynamicallyInvokable]
			get
			{
				return this.charUnknownHigh;
			}
		}

		// Token: 0x170011BB RID: 4539
		// (get) Token: 0x060067FD RID: 26621 RVA: 0x0015F590 File Offset: 0x0015D790
		[__DynamicallyInvokable]
		public char CharUnknownLow
		{
			[__DynamicallyInvokable]
			get
			{
				return this.charUnknownLow;
			}
		}

		// Token: 0x170011BC RID: 4540
		// (get) Token: 0x060067FE RID: 26622 RVA: 0x0015F598 File Offset: 0x0015D798
		[__DynamicallyInvokable]
		public int Index
		{
			[__DynamicallyInvokable]
			get
			{
				return this.index;
			}
		}

		// Token: 0x060067FF RID: 26623 RVA: 0x0015F5A0 File Offset: 0x0015D7A0
		[__DynamicallyInvokable]
		public bool IsUnknownSurrogate()
		{
			return this.charUnknownHigh > '\0';
		}

		// Token: 0x04002E63 RID: 11875
		private char charUnknown;

		// Token: 0x04002E64 RID: 11876
		private char charUnknownHigh;

		// Token: 0x04002E65 RID: 11877
		private char charUnknownLow;

		// Token: 0x04002E66 RID: 11878
		private int index;
	}
}
