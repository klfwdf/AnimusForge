using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x020004FF RID: 1279
	[TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	[Serializable]
	public class LockRecursionException : Exception
	{
		// Token: 0x06003C5C RID: 15452 RVA: 0x000E4365 File Offset: 0x000E2565
		[__DynamicallyInvokable]
		public LockRecursionException()
		{
		}

		// Token: 0x06003C5D RID: 15453 RVA: 0x000E436D File Offset: 0x000E256D
		[__DynamicallyInvokable]
		public LockRecursionException(string message)
			: base(message)
		{
		}

		// Token: 0x06003C5E RID: 15454 RVA: 0x000E4376 File Offset: 0x000E2576
		protected LockRecursionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x06003C5F RID: 15455 RVA: 0x000E4380 File Offset: 0x000E2580
		[__DynamicallyInvokable]
		public LockRecursionException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
