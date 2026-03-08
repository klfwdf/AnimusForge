using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Runtime
{
	// Token: 0x0200071A RID: 1818
	public static class ProfileOptimization
	{
		// Token: 0x06005135 RID: 20789
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void InternalSetProfileRoot(string directoryPath);

		// Token: 0x06005136 RID: 20790
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		internal static extern void InternalStartProfile(string profile, IntPtr ptrNativeAssemblyLoadContext);

		// Token: 0x06005137 RID: 20791 RVA: 0x0011E705 File Offset: 0x0011C905
		[SecurityCritical]
		public static void SetProfileRoot(string directoryPath)
		{
			ProfileOptimization.InternalSetProfileRoot(directoryPath);
		}

		// Token: 0x06005138 RID: 20792 RVA: 0x0011E70D File Offset: 0x0011C90D
		[SecurityCritical]
		public static void StartProfile(string profile)
		{
			ProfileOptimization.InternalStartProfile(profile, IntPtr.Zero);
		}
	}
}
