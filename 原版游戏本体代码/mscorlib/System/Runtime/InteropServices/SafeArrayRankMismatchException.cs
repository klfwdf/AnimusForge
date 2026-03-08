using System;
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000977 RID: 2423
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class SafeArrayRankMismatchException : SystemException
	{
		// Token: 0x0600625F RID: 25183 RVA: 0x00151052 File Offset: 0x0014F252
		[__DynamicallyInvokable]
		public SafeArrayRankMismatchException()
			: base(Environment.GetResourceString("Arg_SafeArrayRankMismatchException"))
		{
			base.SetErrorCode(-2146233032);
		}

		// Token: 0x06006260 RID: 25184 RVA: 0x0015106F File Offset: 0x0014F26F
		[__DynamicallyInvokable]
		public SafeArrayRankMismatchException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233032);
		}

		// Token: 0x06006261 RID: 25185 RVA: 0x00151083 File Offset: 0x0014F283
		[__DynamicallyInvokable]
		public SafeArrayRankMismatchException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233032);
		}

		// Token: 0x06006262 RID: 25186 RVA: 0x00151098 File Offset: 0x0014F298
		protected SafeArrayRankMismatchException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
