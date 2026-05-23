using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System
{
	// Token: 0x02000160 RID: 352
	[FriendAccessAllowed]
	internal class CLRConfig
	{
		// Token: 0x060015CB RID: 5579
		[FriendAccessAllowed]
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool CheckLegacyManagedDeflateStream();

		// Token: 0x060015CC RID: 5580
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool CheckThrowUnobservedTaskExceptions();
	}
}
