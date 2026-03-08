using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	// Token: 0x0200008D RID: 141
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class MemberAccessException : SystemException
	{
		// Token: 0x06000743 RID: 1859 RVA: 0x000198CC File Offset: 0x00017ACC
		[__DynamicallyInvokable]
		public MemberAccessException()
			: base(Environment.GetResourceString("Arg_AccessException"))
		{
			base.SetErrorCode(-2146233062);
		}

		// Token: 0x06000744 RID: 1860 RVA: 0x000198E9 File Offset: 0x00017AE9
		[__DynamicallyInvokable]
		public MemberAccessException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233062);
		}

		// Token: 0x06000745 RID: 1861 RVA: 0x000198FD File Offset: 0x00017AFD
		[__DynamicallyInvokable]
		public MemberAccessException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233062);
		}

		// Token: 0x06000746 RID: 1862 RVA: 0x00019912 File Offset: 0x00017B12
		protected MemberAccessException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
