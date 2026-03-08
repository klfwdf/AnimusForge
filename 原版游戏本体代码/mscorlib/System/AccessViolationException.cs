using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x0200008F RID: 143
	[ComVisible(true)]
	[Serializable]
	public class AccessViolationException : SystemException
	{
		// Token: 0x0600076A RID: 1898 RVA: 0x00019F3F File Offset: 0x0001813F
		public AccessViolationException()
			: base(Environment.GetResourceString("Arg_AccessViolationException"))
		{
			base.SetErrorCode(-2147467261);
		}

		// Token: 0x0600076B RID: 1899 RVA: 0x00019F5C File Offset: 0x0001815C
		public AccessViolationException(string message)
			: base(message)
		{
			base.SetErrorCode(-2147467261);
		}

		// Token: 0x0600076C RID: 1900 RVA: 0x00019F70 File Offset: 0x00018170
		public AccessViolationException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2147467261);
		}

		// Token: 0x0600076D RID: 1901 RVA: 0x00019F85 File Offset: 0x00018185
		protected AccessViolationException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x04000377 RID: 887
		private IntPtr _ip;

		// Token: 0x04000378 RID: 888
		private IntPtr _target;

		// Token: 0x04000379 RID: 889
		private int _accessType;
	}
}
