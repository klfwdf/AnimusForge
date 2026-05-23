using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Threading
{
	// Token: 0x02000535 RID: 1333
	[ComVisible(false)]
	[__DynamicallyInvokable]
	[Serializable]
	public class WaitHandleCannotBeOpenedException : ApplicationException
	{
		// Token: 0x06003EA8 RID: 16040 RVA: 0x000E932D File Offset: 0x000E752D
		[__DynamicallyInvokable]
		public WaitHandleCannotBeOpenedException()
			: base(Environment.GetResourceString("Threading.WaitHandleCannotBeOpenedException"))
		{
			base.SetErrorCode(-2146233044);
		}

		// Token: 0x06003EA9 RID: 16041 RVA: 0x000E934A File Offset: 0x000E754A
		[__DynamicallyInvokable]
		public WaitHandleCannotBeOpenedException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233044);
		}

		// Token: 0x06003EAA RID: 16042 RVA: 0x000E935E File Offset: 0x000E755E
		[__DynamicallyInvokable]
		public WaitHandleCannotBeOpenedException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233044);
		}

		// Token: 0x06003EAB RID: 16043 RVA: 0x000E9373 File Offset: 0x000E7573
		protected WaitHandleCannotBeOpenedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
