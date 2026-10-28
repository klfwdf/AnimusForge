using System;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace Bannerlord.ButterLib.ExceptionHandler.Extensions;

internal static class ProcessExtensions
{
	[Flags]
	private enum SnapshotFlags : uint
	{
		HeapList = 1u,
		Process = 2u,
		Thread = 4u,
		Module = 8u,
		Module32 = 0x10u,
		All = HeapList | Process | Thread | Module,
		Inherit = 0x80000000u,
		NoHeaps = 0x40000000u
	}

	private struct PROCESSENTRY32
	{
		public uint dwSize;

		public uint cntUsage;

		public uint th32ProcessID;

		public IntPtr th32DefaultHeapID;

		public uint th32ModuleID;

		public uint cntThreads;

		public uint th32ParentProcessID;

		public int pcPriClassBase;

		public uint dwFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szExeFile;
	}

	[SuppressUnmanagedCodeSecurity]
	[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
	private sealed class SafeSnapshotHandle : SafeHandleMinusOneIsInvalid
	{
		internal SafeSnapshotHandle()
			: base(ownsHandle: true)
		{
		}

		[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
		internal SafeSnapshotHandle(IntPtr handle)
			: base(ownsHandle: true)
		{
			SetHandle(handle);
		}

		protected override bool ReleaseHandle()
		{
			return CloseHandle(handle);
		}

		[DllImport("kernel32.dll")]
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		private static extern bool CloseHandle(IntPtr handle);
	}

	public static Process? ParentProcess(this Process process)
	{
		int num = ParentProcessId(process.Id);
		if (num == -1)
		{
			return null;
		}
		return Process.GetProcessById(num);
	}

	private static int ParentProcessId(int Id)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			PROCESSENTRY32 lppe = new PROCESSENTRY32
			{
				dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32))
			};
			using SafeSnapshotHandle safeSnapshotHandle = CreateToolhelp32Snapshot(SnapshotFlags.Process, (uint)Id);
			if (safeSnapshotHandle.IsInvalid)
			{
				return -1;
			}
			if (!Process32First(safeSnapshotHandle, ref lppe))
			{
				return -1;
			}
			do
			{
				if (lppe.th32ProcessID == (uint)Id)
				{
					return (int)lppe.th32ParentProcessID;
				}
			}
			while (Process32Next(safeSnapshotHandle, ref lppe));
		}
		return -1;
	}

	[DllImport("kernel32.dll")]
	private static extern SafeSnapshotHandle CreateToolhelp32Snapshot(SnapshotFlags flags, uint id);

	[DllImport("kernel32.dll")]
	private static extern bool Process32First(SafeSnapshotHandle hSnapshot, ref PROCESSENTRY32 lppe);

	[DllImport("kernel32.dll")]
	private static extern bool Process32Next(SafeSnapshotHandle hSnapshot, ref PROCESSENTRY32 lppe);
}
