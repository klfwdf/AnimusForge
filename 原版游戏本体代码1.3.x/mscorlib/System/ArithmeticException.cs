using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000AA RID: 170
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class ArithmeticException : SystemException
	{
		// Token: 0x060009C0 RID: 2496 RVA: 0x0001F71A File Offset: 0x0001D91A
		[__DynamicallyInvokable]
		public ArithmeticException()
			: base(Environment.GetResourceString("Arg_ArithmeticException"))
		{
			base.SetErrorCode(-2147024362);
		}

		// Token: 0x060009C1 RID: 2497 RVA: 0x0001F737 File Offset: 0x0001D937
		[__DynamicallyInvokable]
		public ArithmeticException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147024362);
		}

		// Token: 0x060009C2 RID: 2498 RVA: 0x0001F74B File Offset: 0x0001D94B
		[__DynamicallyInvokable]
		public ArithmeticException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147024362);
		}

		// Token: 0x060009C3 RID: 2499 RVA: 0x0001F760 File Offset: 0x0001D960
		protected ArithmeticException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
