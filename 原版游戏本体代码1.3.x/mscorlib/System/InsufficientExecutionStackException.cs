using System;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x020000F6 RID: 246
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class InsufficientExecutionStackException : SystemException
	{
		// Token: 0x06000F0E RID: 3854 RVA: 0x0002EDF1 File Offset: 0x0002CFF1
		[__DynamicallyInvokable]
		public InsufficientExecutionStackException()
			: base(Environment.GetResourceString("Arg_InsufficientExecutionStackException"))
		{
			base.SetErrorCode(-2146232968);
		}

		// Token: 0x06000F0F RID: 3855 RVA: 0x0002EE0E File Offset: 0x0002D00E
		[__DynamicallyInvokable]
		public InsufficientExecutionStackException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146232968);
		}

		// Token: 0x06000F10 RID: 3856 RVA: 0x0002EE22 File Offset: 0x0002D022
		[__DynamicallyInvokable]
		public InsufficientExecutionStackException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146232968);
		}

		// Token: 0x06000F11 RID: 3857 RVA: 0x0002EE37 File Offset: 0x0002D037
		private InsufficientExecutionStackException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
