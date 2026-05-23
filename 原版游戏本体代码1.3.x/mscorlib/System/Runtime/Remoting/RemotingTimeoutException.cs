using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Runtime.Remoting
{
	// Token: 0x020007C9 RID: 1993
	[ComVisible(true)]
	[Serializable]
	public class RemotingTimeoutException : RemotingException
	{
		// Token: 0x0600561D RID: 22045 RVA: 0x001316E5 File Offset: 0x0012F8E5
		public RemotingTimeoutException()
			: base(RemotingTimeoutException._nullMessage)
		{
		}

		// Token: 0x0600561E RID: 22046 RVA: 0x001316F2 File Offset: 0x0012F8F2
		public RemotingTimeoutException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233077);
		}

		// Token: 0x0600561F RID: 22047 RVA: 0x00131706 File Offset: 0x0012F906
		public RemotingTimeoutException(string message, Exception InnerException)
			: base(message, InnerException)
		{
			base.SetErrorCode(-2146233077);
		}

		// Token: 0x06005620 RID: 22048 RVA: 0x0013171B File Offset: 0x0012F91B
		internal RemotingTimeoutException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x04002791 RID: 10129
		private static string _nullMessage = Environment.GetResourceString("Remoting_Default");
	}
}
