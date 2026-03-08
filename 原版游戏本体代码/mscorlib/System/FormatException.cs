using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000E4 RID: 228
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class FormatException : SystemException
	{
		// Token: 0x06000E84 RID: 3716 RVA: 0x0002CCBF File Offset: 0x0002AEBF
		[__DynamicallyInvokable]
		public FormatException()
			: base(Environment.GetResourceString("Arg_FormatException"))
		{
			base.SetErrorCode(-2146233033);
		}

		// Token: 0x06000E85 RID: 3717 RVA: 0x0002CCDC File Offset: 0x0002AEDC
		[__DynamicallyInvokable]
		public FormatException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233033);
		}

		// Token: 0x06000E86 RID: 3718 RVA: 0x0002CCF0 File Offset: 0x0002AEF0
		[__DynamicallyInvokable]
		public FormatException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233033);
		}

		// Token: 0x06000E87 RID: 3719 RVA: 0x0002CD05 File Offset: 0x0002AF05
		protected FormatException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
