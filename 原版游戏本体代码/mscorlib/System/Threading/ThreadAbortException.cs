using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;

namespace System.Threading
{
	// Token: 0x02000518 RID: 1304
	[ComVisible(true)]
	[Serializable]
	public sealed class ThreadAbortException : SystemException
	{
		// Token: 0x06003DB9 RID: 15801 RVA: 0x000E6E3F File Offset: 0x000E503F
		internal ThreadAbortException()
			: base(Exception.GetMessageFromNativeResources(Exception.ExceptionMessageKind.ThreadAbort))
		{
			base.SetErrorCode(-2146233040);
		}

		// Token: 0x06003DBA RID: 15802 RVA: 0x000E6E58 File Offset: 0x000E5058
		internal ThreadAbortException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x17000937 RID: 2359
		// (get) Token: 0x06003DBB RID: 15803 RVA: 0x000E6E62 File Offset: 0x000E5062
		public object ExceptionState
		{
			[SecuritySafeCritical]
			get
			{
				return Thread.CurrentThread.AbortReason;
			}
		}
	}
}
