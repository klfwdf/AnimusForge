using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x02000100 RID: 256
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class InvalidOperationException : SystemException
	{
		// Token: 0x06000FB3 RID: 4019 RVA: 0x00030221 File Offset: 0x0002E421
		[__DynamicallyInvokable]
		public InvalidOperationException()
			: base(Environment.GetResourceString("Arg_InvalidOperationException"))
		{
			base.SetErrorCode(-2146233079);
		}

		// Token: 0x06000FB4 RID: 4020 RVA: 0x0003023E File Offset: 0x0002E43E
		[__DynamicallyInvokable]
		public InvalidOperationException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233079);
		}

		// Token: 0x06000FB5 RID: 4021 RVA: 0x00030252 File Offset: 0x0002E452
		[__DynamicallyInvokable]
		public InvalidOperationException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233079);
		}

		// Token: 0x06000FB6 RID: 4022 RVA: 0x00030267 File Offset: 0x0002E467
		protected InvalidOperationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
