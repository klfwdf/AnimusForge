using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x02000090 RID: 144
	[ComVisible(true)]
	[Serializable]
	public class ApplicationException : Exception
	{
		// Token: 0x0600076E RID: 1902 RVA: 0x00019F8F File Offset: 0x0001818F
		public ApplicationException()
			: base(Environment.GetResourceString("Arg_ApplicationException"))
		{
			base.SetErrorCode(-2146232832);
		}

		// Token: 0x0600076F RID: 1903 RVA: 0x00019FAC File Offset: 0x000181AC
		public ApplicationException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146232832);
		}

		// Token: 0x06000770 RID: 1904 RVA: 0x00019FC0 File Offset: 0x000181C0
		public ApplicationException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146232832);
		}

		// Token: 0x06000771 RID: 1905 RVA: 0x00019FD5 File Offset: 0x000181D5
		protected ApplicationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
