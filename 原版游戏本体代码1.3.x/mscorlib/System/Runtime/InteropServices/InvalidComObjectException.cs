using System;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000968 RID: 2408
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class InvalidComObjectException : SystemException
	{
		// Token: 0x06006229 RID: 25129 RVA: 0x0014F82D File Offset: 0x0014DA2D
		[__DynamicallyInvokable]
		public InvalidComObjectException()
			: base(Environment.GetResourceString("Arg_InvalidComObjectException"))
		{
			base.SetErrorCode(-2146233049);
		}

		// Token: 0x0600622A RID: 25130 RVA: 0x0014F84A File Offset: 0x0014DA4A
		[__DynamicallyInvokable]
		public InvalidComObjectException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233049);
		}

		// Token: 0x0600622B RID: 25131 RVA: 0x0014F85E File Offset: 0x0014DA5E
		[__DynamicallyInvokable]
		public InvalidComObjectException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233049);
		}

		// Token: 0x0600622C RID: 25132 RVA: 0x0014F873 File Offset: 0x0014DA73
		protected InvalidComObjectException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
