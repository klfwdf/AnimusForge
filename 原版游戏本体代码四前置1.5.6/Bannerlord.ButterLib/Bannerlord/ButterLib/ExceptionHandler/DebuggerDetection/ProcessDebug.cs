using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Bannerlord.ButterLib.ExceptionHandler.DebuggerDetection;

internal static class ProcessDebug
{
	private const int PROCESS_DEBUG_OBJECT_HANDLE = 30;

	[DllImport("ntdll.dll", SetLastError = true)]
	private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, out IntPtr processInformation, int processInformationLength, IntPtr returnLength);

	public static bool CheckProcessDebugObjectHandle()
	{
		if (NtQueryInformationProcess(Process.GetCurrentProcess().Handle, 30, out var processInformation, IntPtr.Size, IntPtr.Zero) == 0)
		{
			return (IntPtr)0 != processInformation;
		}
		return false;
	}
}
