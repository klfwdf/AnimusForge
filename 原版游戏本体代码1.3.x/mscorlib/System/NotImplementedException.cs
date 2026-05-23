using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x02000115 RID: 277
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class NotImplementedException : SystemException
	{
		// Token: 0x0600107F RID: 4223 RVA: 0x000315F1 File Offset: 0x0002F7F1
		[__DynamicallyInvokable]
		public NotImplementedException()
			: base(Environment.GetResourceString("Arg_NotImplementedException"))
		{
			base.SetErrorCode(-2147467263);
		}

		// Token: 0x06001080 RID: 4224 RVA: 0x0003160E File Offset: 0x0002F80E
		[__DynamicallyInvokable]
		public NotImplementedException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147467263);
		}

		// Token: 0x06001081 RID: 4225 RVA: 0x00031622 File Offset: 0x0002F822
		[__DynamicallyInvokable]
		public NotImplementedException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2147467263);
		}

		// Token: 0x06001082 RID: 4226 RVA: 0x00031637 File Offset: 0x0002F837
		protected NotImplementedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
