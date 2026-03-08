using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Threading
{
	// Token: 0x0200052A RID: 1322
	[ComVisible(true)]
	[Serializable]
	public class ThreadStateException : SystemException
	{
		// Token: 0x06003E29 RID: 15913 RVA: 0x000E7C8C File Offset: 0x000E5E8C
		public ThreadStateException()
			: base(Environment.GetResourceString("Arg_ThreadStateException"))
		{
			base.SetErrorCode(-2146233056);
		}

		// Token: 0x06003E2A RID: 15914 RVA: 0x000E7CA9 File Offset: 0x000E5EA9
		public ThreadStateException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233056);
		}

		// Token: 0x06003E2B RID: 15915 RVA: 0x000E7CBD File Offset: 0x000E5EBD
		public ThreadStateException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233056);
		}

		// Token: 0x06003E2C RID: 15916 RVA: 0x000E7CD2 File Offset: 0x000E5ED2
		protected ThreadStateException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
