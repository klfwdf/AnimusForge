using System;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000978 RID: 2424
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class SafeArrayTypeMismatchException : SystemException
	{
		// Token: 0x06006263 RID: 25187 RVA: 0x001510A2 File Offset: 0x0014F2A2
		[__DynamicallyInvokable]
		public SafeArrayTypeMismatchException()
			: base(Environment.GetResourceString("Arg_SafeArrayTypeMismatchException"))
		{
			base.SetErrorCode(-2146233037);
		}

		// Token: 0x06006264 RID: 25188 RVA: 0x001510BF File Offset: 0x0014F2BF
		[__DynamicallyInvokable]
		public SafeArrayTypeMismatchException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233037);
		}

		// Token: 0x06006265 RID: 25189 RVA: 0x001510D3 File Offset: 0x0014F2D3
		[__DynamicallyInvokable]
		public SafeArrayTypeMismatchException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233037);
		}

		// Token: 0x06006266 RID: 25190 RVA: 0x001510E8 File Offset: 0x0014F2E8
		protected SafeArrayTypeMismatchException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
