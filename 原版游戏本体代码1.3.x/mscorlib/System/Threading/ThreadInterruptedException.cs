using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Threading
{
	// Token: 0x02000519 RID: 1305
	[ComVisible(true)]
	[Serializable]
	public class ThreadInterruptedException : SystemException
	{
		// Token: 0x06003DBC RID: 15804 RVA: 0x000E6E6E File Offset: 0x000E506E
		public ThreadInterruptedException()
			: base(Exception.GetMessageFromNativeResources(Exception.ExceptionMessageKind.ThreadInterrupted))
		{
			base.SetErrorCode(-2146233063);
		}

		// Token: 0x06003DBD RID: 15805 RVA: 0x000E6E87 File Offset: 0x000E5087
		public ThreadInterruptedException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233063);
		}

		// Token: 0x06003DBE RID: 15806 RVA: 0x000E6E9B File Offset: 0x000E509B
		public ThreadInterruptedException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233063);
		}

		// Token: 0x06003DBF RID: 15807 RVA: 0x000E6EB0 File Offset: 0x000E50B0
		protected ThreadInterruptedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
