using System;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009F4 RID: 2548
	internal static class UnsafeNativeMethods
	{
		// Token: 0x060064C2 RID: 25794
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("api-ms-win-core-winrt-error-l1-1-1.dll", PreserveSig = false)]
		internal static extern IRestrictedErrorInfo GetRestrictedErrorInfo();

		// Token: 0x060064C3 RID: 25795
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("api-ms-win-core-winrt-error-l1-1-1.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool RoOriginateLanguageException(int error, [MarshalAs(UnmanagedType.HString)] string message, IntPtr languageException);

		// Token: 0x060064C4 RID: 25796
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("api-ms-win-core-winrt-error-l1-1-1.dll", PreserveSig = false)]
		internal static extern void RoReportUnhandledError(IRestrictedErrorInfo error);

		// Token: 0x060064C5 RID: 25797
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
		internal unsafe static extern int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString, int length, [Out] IntPtr* hstring);

		// Token: 0x060064C6 RID: 25798
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
		internal unsafe static extern int WindowsCreateStringReference(char* sourceString, int length, [Out] HSTRING_HEADER* hstringHeader, [Out] IntPtr* hstring);

		// Token: 0x060064C7 RID: 25799
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
		internal static extern int WindowsDeleteString(IntPtr hstring);

		// Token: 0x060064C8 RID: 25800
		[SecurityCritical]
		[SuppressUnmanagedCodeSecurity]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CallingConvention = CallingConvention.StdCall)]
		internal unsafe static extern char* WindowsGetStringRawBuffer(IntPtr hstring, [Out] uint* length);
	}
}
