using System;
using System.Runtime.Serialization;

namespace System.Threading
{
	// Token: 0x0200052B RID: 1323
	[Serializable]
	public sealed class ThreadStartException : SystemException
	{
		// Token: 0x06003E2D RID: 15917 RVA: 0x000E7CDC File Offset: 0x000E5EDC
		private ThreadStartException()
			: base(Environment.GetResourceString("Arg_ThreadStartException"))
		{
			base.SetErrorCode(-2146233051);
		}

		// Token: 0x06003E2E RID: 15918 RVA: 0x000E7CF9 File Offset: 0x000E5EF9
		private ThreadStartException(Exception reason)
			: base(Environment.GetResourceString("Arg_ThreadStartException"), reason)
		{
			base.SetErrorCode(-2146233051);
		}

		// Token: 0x06003E2F RID: 15919 RVA: 0x000E7D17 File Offset: 0x000E5F17
		internal ThreadStartException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
