using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x02000116 RID: 278
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class NotSupportedException : SystemException
	{
		// Token: 0x06001083 RID: 4227 RVA: 0x00031641 File Offset: 0x0002F841
		[__DynamicallyInvokable]
		public NotSupportedException()
			: base(Environment.GetResourceString("Arg_NotSupportedException"))
		{
			base.SetErrorCode(-2146233067);
		}

		// Token: 0x06001084 RID: 4228 RVA: 0x0003165E File Offset: 0x0002F85E
		[__DynamicallyInvokable]
		public NotSupportedException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233067);
		}

		// Token: 0x06001085 RID: 4229 RVA: 0x00031672 File Offset: 0x0002F872
		[__DynamicallyInvokable]
		public NotSupportedException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233067);
		}

		// Token: 0x06001086 RID: 4230 RVA: 0x00031687 File Offset: 0x0002F887
		protected NotSupportedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
