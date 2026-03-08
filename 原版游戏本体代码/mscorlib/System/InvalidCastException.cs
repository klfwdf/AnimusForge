using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000FF RID: 255
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class InvalidCastException : SystemException
	{
		// Token: 0x06000FAE RID: 4014 RVA: 0x000301C1 File Offset: 0x0002E3C1
		[__DynamicallyInvokable]
		public InvalidCastException()
			: base(Environment.GetResourceString("Arg_InvalidCastException"))
		{
			base.SetErrorCode(-2147467262);
		}

		// Token: 0x06000FAF RID: 4015 RVA: 0x000301DE File Offset: 0x0002E3DE
		[__DynamicallyInvokable]
		public InvalidCastException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147467262);
		}

		// Token: 0x06000FB0 RID: 4016 RVA: 0x000301F2 File Offset: 0x0002E3F2
		[__DynamicallyInvokable]
		public InvalidCastException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147467262);
		}

		// Token: 0x06000FB1 RID: 4017 RVA: 0x00030207 File Offset: 0x0002E407
		protected InvalidCastException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x06000FB2 RID: 4018 RVA: 0x00030211 File Offset: 0x0002E411
		[__DynamicallyInvokable]
		public InvalidCastException(string message, int errorCode)
			: base(message)
		{
			base.SetErrorCode(errorCode);
		}
	}
}
