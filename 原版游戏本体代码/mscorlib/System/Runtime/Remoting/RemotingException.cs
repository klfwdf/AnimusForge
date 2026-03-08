using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Runtime.Remoting
{
	// Token: 0x020007C7 RID: 1991
	[ComVisible(true)]
	[Serializable]
	public class RemotingException : SystemException
	{
		// Token: 0x06005613 RID: 22035 RVA: 0x0013162D File Offset: 0x0012F82D
		public RemotingException()
			: base(RemotingException._nullMessage)
		{
			base.SetErrorCode(-2146233077);
		}

		// Token: 0x06005614 RID: 22036 RVA: 0x00131645 File Offset: 0x0012F845
		public RemotingException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233077);
		}

		// Token: 0x06005615 RID: 22037 RVA: 0x00131659 File Offset: 0x0012F859
		public RemotingException(string message, Exception InnerException)
			: base(message, InnerException)
		{
			base.SetErrorCode(-2146233077);
		}

		// Token: 0x06005616 RID: 22038 RVA: 0x0013166E File Offset: 0x0012F86E
		protected RemotingException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x0400278F RID: 10127
		private static string _nullMessage = Environment.GetResourceString("Remoting_Default");
	}
}
