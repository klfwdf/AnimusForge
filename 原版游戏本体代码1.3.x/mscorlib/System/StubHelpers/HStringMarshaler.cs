using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;

namespace System.StubHelpers
{
	// Token: 0x02000596 RID: 1430
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class HStringMarshaler
	{
		// Token: 0x060042DB RID: 17115 RVA: 0x000F97D8 File Offset: 0x000F79D8
		[SecurityCritical]
		internal unsafe static IntPtr ConvertToNative(string managed)
		{
			if (!Environment.IsWinRTSupported)
			{
				throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
			}
			if (managed == null)
			{
				throw new ArgumentNullException();
			}
			IntPtr result;
			int errorCode = UnsafeNativeMethods.WindowsCreateString(managed, managed.Length, &result);
			Marshal.ThrowExceptionForHR(errorCode, new IntPtr(-1));
			return result;
		}

		// Token: 0x060042DC RID: 17116 RVA: 0x000F9824 File Offset: 0x000F7A24
		[SecurityCritical]
		internal unsafe static IntPtr ConvertToNativeReference(string managed, [Out] HSTRING_HEADER* hstringHeader)
		{
			if (!Environment.IsWinRTSupported)
			{
				throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
			}
			if (managed == null)
			{
				throw new ArgumentNullException();
			}
			char* ptr = managed;
			if (ptr != null)
			{
				ptr += RuntimeHelpers.OffsetToStringData / 2;
			}
			IntPtr result;
			int errorCode = UnsafeNativeMethods.WindowsCreateStringReference(ptr, managed.Length, hstringHeader, &result);
			Marshal.ThrowExceptionForHR(errorCode, new IntPtr(-1));
			return result;
		}

		// Token: 0x060042DD RID: 17117 RVA: 0x000F987F File Offset: 0x000F7A7F
		[SecurityCritical]
		internal static string ConvertToManaged(IntPtr hstring)
		{
			if (!Environment.IsWinRTSupported)
			{
				throw new PlatformNotSupportedException(Environment.GetResourceString("PlatformNotSupported_WinRT"));
			}
			return WindowsRuntimeMarshal.HStringToString(hstring);
		}

		// Token: 0x060042DE RID: 17118 RVA: 0x000F989E File Offset: 0x000F7A9E
		[SecurityCritical]
		internal static void ClearNative(IntPtr hstring)
		{
			if (hstring != IntPtr.Zero)
			{
				UnsafeNativeMethods.WindowsDeleteString(hstring);
			}
		}
	}
}
