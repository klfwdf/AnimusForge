using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Threading
{
	// Token: 0x02000511 RID: 1297
	[ComVisible(false)]
	[TypeForwardedFrom("System, Version=2.0.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
	[__DynamicallyInvokable]
	[Serializable]
	public class SemaphoreFullException : SystemException
	{
		// Token: 0x06003D10 RID: 15632 RVA: 0x000E6177 File Offset: 0x000E4377
		[__DynamicallyInvokable]
		public SemaphoreFullException()
			: base(Environment.GetResourceString("Threading_SemaphoreFullException"))
		{
		}

		// Token: 0x06003D11 RID: 15633 RVA: 0x000E6189 File Offset: 0x000E4389
		[__DynamicallyInvokable]
		public SemaphoreFullException(string message)
			: base(message)
		{
		}

		// Token: 0x06003D12 RID: 15634 RVA: 0x000E6192 File Offset: 0x000E4392
		[__DynamicallyInvokable]
		public SemaphoreFullException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		// Token: 0x06003D13 RID: 15635 RVA: 0x000E619C File Offset: 0x000E439C
		protected SemaphoreFullException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
