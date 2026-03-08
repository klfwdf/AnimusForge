using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Runtime.Remoting
{
	// Token: 0x020007C8 RID: 1992
	[ComVisible(true)]
	[Serializable]
	public class ServerException : SystemException
	{
		// Token: 0x06005618 RID: 22040 RVA: 0x00131689 File Offset: 0x0012F889
		public ServerException()
			: base(ServerException._nullMessage)
		{
			base.SetErrorCode(-2146233074);
		}

		// Token: 0x06005619 RID: 22041 RVA: 0x001316A1 File Offset: 0x0012F8A1
		public ServerException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233074);
		}

		// Token: 0x0600561A RID: 22042 RVA: 0x001316B5 File Offset: 0x0012F8B5
		public ServerException(string message, Exception InnerException)
			: base(message, InnerException)
		{
			base.SetErrorCode(-2146233074);
		}

		// Token: 0x0600561B RID: 22043 RVA: 0x001316CA File Offset: 0x0012F8CA
		internal ServerException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x04002790 RID: 10128
		private static string _nullMessage = Environment.GetResourceString("Remoting_Default");
	}
}
