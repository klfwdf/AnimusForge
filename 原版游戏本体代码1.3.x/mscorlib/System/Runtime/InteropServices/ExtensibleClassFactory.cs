using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000963 RID: 2403
	[ComVisible(true)]
	public sealed class ExtensibleClassFactory
	{
		// Token: 0x06006224 RID: 25124 RVA: 0x0014F825 File Offset: 0x0014DA25
		private ExtensibleClassFactory()
		{
		}

		// Token: 0x06006225 RID: 25125
		[SecuritySafeCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		public static extern void RegisterObjectCreationCallback(ObjectCreationDelegate callback);
	}
}
