using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x0200011E RID: 286
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class OverflowException : ArithmeticException
	{
		// Token: 0x060010D7 RID: 4311 RVA: 0x00032C7F File Offset: 0x00030E7F
		[__DynamicallyInvokable]
		public OverflowException()
			: base(Environment.GetResourceString("Arg_OverflowException"))
		{
			base.SetErrorCode(-2146233066);
		}

		// Token: 0x060010D8 RID: 4312 RVA: 0x00032C9C File Offset: 0x00030E9C
		[__DynamicallyInvokable]
		public OverflowException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233066);
		}

		// Token: 0x060010D9 RID: 4313 RVA: 0x00032CB0 File Offset: 0x00030EB0
		[__DynamicallyInvokable]
		public OverflowException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233066);
		}

		// Token: 0x060010DA RID: 4314 RVA: 0x00032CC5 File Offset: 0x00030EC5
		protected OverflowException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
