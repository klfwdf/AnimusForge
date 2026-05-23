using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Threading
{
	// Token: 0x02000512 RID: 1298
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public class SynchronizationLockException : SystemException
	{
		// Token: 0x06003D14 RID: 15636 RVA: 0x000E61A6 File Offset: 0x000E43A6
		[__DynamicallyInvokable]
		public SynchronizationLockException()
			: base(Environment.GetResourceString("Arg_SynchronizationLockException"))
		{
			base.SetErrorCode(-2146233064);
		}

		// Token: 0x06003D15 RID: 15637 RVA: 0x000E61C3 File Offset: 0x000E43C3
		[__DynamicallyInvokable]
		public SynchronizationLockException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233064);
		}

		// Token: 0x06003D16 RID: 15638 RVA: 0x000E61D7 File Offset: 0x000E43D7
		[__DynamicallyInvokable]
		public SynchronizationLockException(string message, Exception innerException)
			: base(message, innerException)
		{
			base.SetErrorCode(-2146233064);
		}

		// Token: 0x06003D17 RID: 15639 RVA: 0x000E61EC File Offset: 0x000E43EC
		protected SynchronizationLockException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
