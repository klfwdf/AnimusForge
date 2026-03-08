using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000DC RID: 220
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class DllNotFoundException : TypeLoadException
	{
		// Token: 0x06000E2D RID: 3629 RVA: 0x0002BC8B File Offset: 0x00029E8B
		[__DynamicallyInvokable]
		public DllNotFoundException()
			: base(Environment.GetResourceString("Arg_DllNotFoundException"))
		{
			base.SetErrorCode(-2146233052);
		}

		// Token: 0x06000E2E RID: 3630 RVA: 0x0002BCA8 File Offset: 0x00029EA8
		[__DynamicallyInvokable]
		public DllNotFoundException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233052);
		}

		// Token: 0x06000E2F RID: 3631 RVA: 0x0002BCBC File Offset: 0x00029EBC
		[__DynamicallyInvokable]
		public DllNotFoundException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233052);
		}

		// Token: 0x06000E30 RID: 3632 RVA: 0x0002BCD1 File Offset: 0x00029ED1
		protected DllNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
