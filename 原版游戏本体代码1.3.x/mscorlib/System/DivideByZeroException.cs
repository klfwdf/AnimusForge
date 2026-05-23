using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000D6 RID: 214
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class DivideByZeroException : ArithmeticException
	{
		// Token: 0x06000DB0 RID: 3504 RVA: 0x0002A492 File Offset: 0x00028692
		[__DynamicallyInvokable]
		public DivideByZeroException()
			: base(Environment.GetResourceString("Arg_DivideByZero"))
		{
			base.SetErrorCode(-2147352558);
		}

		// Token: 0x06000DB1 RID: 3505 RVA: 0x0002A4AF File Offset: 0x000286AF
		[__DynamicallyInvokable]
		public DivideByZeroException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147352558);
		}

		// Token: 0x06000DB2 RID: 3506 RVA: 0x0002A4C3 File Offset: 0x000286C3
		[__DynamicallyInvokable]
		public DivideByZeroException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147352558);
		}

		// Token: 0x06000DB3 RID: 3507 RVA: 0x0002A4D8 File Offset: 0x000286D8
		protected DivideByZeroException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
