using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Reflection
{
	// Token: 0x02000623 RID: 1571
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class TargetParameterCountException : ApplicationException
	{
		// Token: 0x060048BC RID: 18620 RVA: 0x00107A33 File Offset: 0x00105C33
		[__DynamicallyInvokable]
		public TargetParameterCountException()
			: base(Environment.GetResourceString("Arg_TargetParameterCountException"))
		{
			base.SetErrorCode(-2147352562);
		}

		// Token: 0x060048BD RID: 18621 RVA: 0x00107A50 File Offset: 0x00105C50
		[__DynamicallyInvokable]
		public TargetParameterCountException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147352562);
		}

		// Token: 0x060048BE RID: 18622 RVA: 0x00107A64 File Offset: 0x00105C64
		[__DynamicallyInvokable]
		public TargetParameterCountException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2147352562);
		}

		// Token: 0x060048BF RID: 18623 RVA: 0x00107A79 File Offset: 0x00105C79
		internal TargetParameterCountException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
