using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace Bannerlord.ButterLib.ExceptionHandler.Utils;

internal static class MiniDump
{
	internal static class DbgHelpNativeMethods
	{
		[Flags]
		public enum MINIDUMP_TYPE
		{
			MiniDumpNormal = 0,
			MiniDumpWithDataSegs = 1,
			MiniDumpWithFullMemory = 2,
			MiniDumpWithHandleData = 4,
			MiniDumpFilterMemory = 8,
			MiniDumpScanMemory = 0x10,
			MiniDumpWithUnloadedModules = 0x20,
			MiniDumpWithIndirectlyReferencedMemory = 0x40,
			MiniDumpFilterModulePaths = 0x80,
			MiniDumpWithProcessThreadData = 0x100,
			MiniDumpWithPrivateReadWriteMemory = 0x200,
			MiniDumpWithoutOptionalData = 0x400,
			MiniDumpWithFullMemoryInfo = 0x800,
			MiniDumpWithThreadInfo = 0x1000,
			MiniDumpWithCodeSegs = 0x2000,
			MiniDumpWithoutAuxiliaryState = 0x4000,
			MiniDumpWithFullAuxiliaryState = 0x8000,
			MiniDumpWithPrivateWriteCopyMemory = 0x10000,
			MiniDumpIgnoreInaccessibleMemory = 0x20000,
			MiniDumpWithTokenInformation = 0x40000,
			MiniDumpWithModuleHeaders = 0x80000,
			MiniDumpFilterTriage = 0x100000,
			MiniDumpValidTypeFlags = MiniDumpWithDataSegs | MiniDumpWithFullMemory | MiniDumpWithHandleData | MiniDumpFilterMemory | MiniDumpScanMemory | MiniDumpWithUnloadedModules | MiniDumpWithIndirectlyReferencedMemory | MiniDumpFilterModulePaths | MiniDumpWithProcessThreadData | MiniDumpWithPrivateReadWriteMemory | MiniDumpWithoutOptionalData | MiniDumpWithFullMemoryInfo | MiniDumpWithThreadInfo | MiniDumpWithCodeSegs | MiniDumpWithoutAuxiliaryState | MiniDumpWithFullAuxiliaryState | MiniDumpWithPrivateWriteCopyMemory | MiniDumpIgnoreInaccessibleMemory | MiniDumpWithTokenInformation | MiniDumpWithModuleHeaders | MiniDumpFilterTriage,
			ConfigGameMini = 0xE9975,
			ConfigGameFull = 0xE9977,
			ConfigButterLibMini = 0xD0
		}

		[StructLayout(LayoutKind.Sequential, Pack = 4)]
		public struct MINIDUMP_EXCEPTION_INFORMATION
		{
			public uint ThreadId;

			public IntPtr ExceptionPointers;

			[MarshalAs(UnmanagedType.Bool)]
			public bool ClientPointers;
		}

		public struct MINIDUMP_USER_STREAM
		{
			public uint Type;

			public uint BufferSize;

			public IntPtr Buffer;
		}

		public struct MINIDUMP_USER_STREAM_INFORMATION
		{
			public uint UserStreamCount;

			public MINIDUMP_USER_STREAM[] UserStreamArray;
		}

		public enum MINIDUMP_CALLBACK_TYPE
		{
			ModuleCallback,
			ThreadCallback,
			ThreadExCallback,
			IncludeThreadCallback,
			IncludeModuleCallback,
			MemoryCallback,
			CancelCallback,
			WriteKernelMinidumpCallback,
			KernelMinidumpStatusCallback,
			RemoveMemoryCallback,
			IncludeVmRegionCallback,
			IoStartCallback,
			IoWriteAllCallback,
			IoFinishCallback,
			ReadMemoryFailureCallback,
			SecondaryFlagsCallback
		}

		public struct VS_FIXEDFILEINFO
		{
			public uint dwSignature;

			public uint dwStrucVersion;

			public uint dwFileVersionMS;

			public uint dwFileVersionLS;

			public uint dwProductVersionMS;

			public uint dwProductVersionLS;

			public uint dwFileFlagsMask;

			public uint dwFileFlags;

			public uint dwFileOS;

			public uint dwFileType;

			public uint dwFileSubtype;

			public uint dwFileDateMS;

			public uint dwFileDateLS;
		}

		public struct MINIDUMP_THREAD_CALLBACK
		{
			public uint ThreadId;

			public IntPtr ThreadHandle;

			public uint Pad;
		}

		public struct MINIDUMP_THREAD_EX_CALLBACK
		{
			public uint ThreadId;

			public IntPtr ThreadHandle;

			public uint Pad;
		}

		public struct MINIDUMP_MODULE_CALLBACK
		{
			public IntPtr FullPath;

			public ulong BaseOfImage;

			public uint SizeOfImage;

			public uint CheckSum;

			public uint TimeDateStamp;

			public VS_FIXEDFILEINFO VersionInfo;

			public IntPtr CvRecord;

			public uint SizeOfCvRecord;

			public IntPtr MiscRecord;

			public uint SizeOfMiscRecord;
		}

		public struct MINIDUMP_INCLUDE_THREAD_CALLBACK
		{
			public uint ThreadId;
		}

		public struct MINIDUMP_INCLUDE_MODULE_CALLBACK
		{
			public ulong BaseOfImage;
		}

		public struct MINIDUMP_IO_CALLBACK
		{
			public IntPtr Handle;

			public ulong Offset;

			public IntPtr Buffer;

			public uint BufferBytes;
		}

		public struct MINIDUMP_CALLBACK_INPUT
		{
			[StructLayout(LayoutKind.Explicit)]
			public struct UNION
			{
				[FieldOffset(0)]
				public int Status;

				[FieldOffset(0)]
				public MINIDUMP_THREAD_CALLBACK Thread;

				[FieldOffset(0)]
				public MINIDUMP_THREAD_EX_CALLBACK ThreadEx;

				[FieldOffset(0)]
				public MINIDUMP_MODULE_CALLBACK Module;

				[FieldOffset(0)]
				public MINIDUMP_INCLUDE_THREAD_CALLBACK IncludeThread;

				[FieldOffset(0)]
				public MINIDUMP_INCLUDE_MODULE_CALLBACK IncludeModule;

				[FieldOffset(0)]
				public MINIDUMP_IO_CALLBACK Io;
			}

			public uint ProcessId;

			public IntPtr ProcessHandle;

			public MINIDUMP_CALLBACK_TYPE CallbackType;

			public UNION Union;
		}

		[Flags]
		public enum MODULE_WRITE_FLAGS : uint
		{
			ModuleWriteModule = 1u,
			ModuleWriteDataSeg = 2u,
			ModuleWriteMiscRecord = 4u,
			ModuleWriteCvRecord = 8u,
			ModuleReferencedByMemory = 0x10u,
			ModuleWriteTlsData = 0x20u,
			ModuleWriteCodeSegs = 0x40u
		}

		[Flags]
		public enum THREAD_WRITE_FLAGS : uint
		{
			ThreadWriteThread = 1u,
			ThreadWriteStack = 2u,
			ThreadWriteContext = 4u,
			ThreadWriteBackingStore = 8u,
			ThreadWriteInstructionWindow = 0x10u,
			ThreadWriteThreadData = 0x20u,
			ThreadWriteThreadInfo = 0x40u
		}

		[StructLayout(LayoutKind.Explicit, Pack = 4)]
		public struct MINIDUMP_CALLBACK_OUTPUT
		{
			[FieldOffset(0)]
			public int Status;

			[FieldOffset(0)]
			public MODULE_WRITE_FLAGS ModuleWriteFlags;

			[FieldOffset(0)]
			public THREAD_WRITE_FLAGS ThreadWriteFlags;

			[FieldOffset(0)]
			public IntPtr Handle;
		}

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public delegate bool MINIDUMP_CALLBACK_ROUTINE([In] IntPtr CallbackParam, [In] ref MINIDUMP_CALLBACK_INPUT CallbackInput, [In][Out] ref MINIDUMP_CALLBACK_OUTPUT CallbackOutput);

		public struct MINIDUMP_CALLBACK_INFORMATION
		{
			public MINIDUMP_CALLBACK_ROUTINE CallbackRoutine;

			public IntPtr CallbackParam;
		}

		[DllImport("dbghelp.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, SafeHandle hFile, MINIDUMP_TYPE DumpType, ref readonly MINIDUMP_EXCEPTION_INFORMATION ExceptionParams, ref readonly MINIDUMP_USER_STREAM_INFORMATION UserStreamParam, ref readonly MINIDUMP_CALLBACK_INFORMATION CallbackParam);
	}

	internal static class Kernel32NativeMethods
	{
		[DllImport("kernel32.dll")]
		public static extern IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll")]
		public static extern uint GetCurrentProcessId();

		[DllImport("kernel32.dll")]
		public static extern uint GetCurrentThreadId();
	}

	public struct Info
	{
		public uint ThreadId;
	}

	public static bool TryDump([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out MemoryStream? dataStream)
	{
		try
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				string tempFileName = Path.GetTempFileName();
				using (FileStream fileStream = new FileStream(tempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
				{
					if (fileStream.SafeFileHandle == null || fileStream.SafeFileHandle.IsClosed || fileStream.SafeFileHandle.IsInvalid)
					{
						dataStream = null;
						return false;
					}
					if (!WriteMini(fileStream.SafeFileHandle))
					{
						dataStream = null;
						return false;
					}
					dataStream = new MemoryStream();
					fileStream.Position = 0L;
					fileStream.CopyTo(dataStream);
				}
				File.Delete(tempFileName);
				return true;
			}
		}
		catch (Exception)
		{
		}
		dataStream = null;
		return false;
	}

	private static bool Filter(IntPtr CallbackParam, ref DbgHelpNativeMethods.MINIDUMP_CALLBACK_INPUT CallbackInput, ref DbgHelpNativeMethods.MINIDUMP_CALLBACK_OUTPUT CallbackOutput)
	{
		switch (CallbackInput.CallbackType)
		{
		case DbgHelpNativeMethods.MINIDUMP_CALLBACK_TYPE.IncludeModuleCallback:
			return true;
		case DbgHelpNativeMethods.MINIDUMP_CALLBACK_TYPE.IncludeThreadCallback:
		{
			Info info = Marshal.PtrToStructure<Info>(CallbackParam);
			return CallbackInput.Union.IncludeThread.ThreadId == info.ThreadId;
		}
		case DbgHelpNativeMethods.MINIDUMP_CALLBACK_TYPE.ModuleCallback:
			if (!CallbackOutput.ModuleWriteFlags.HasFlag(DbgHelpNativeMethods.MODULE_WRITE_FLAGS.ModuleReferencedByMemory))
			{
				CallbackOutput.ModuleWriteFlags &= ~DbgHelpNativeMethods.MODULE_WRITE_FLAGS.ModuleWriteModule;
				return true;
			}
			return true;
		case DbgHelpNativeMethods.MINIDUMP_CALLBACK_TYPE.ThreadCallback:
			return true;
		case DbgHelpNativeMethods.MINIDUMP_CALLBACK_TYPE.ThreadExCallback:
			return true;
		case DbgHelpNativeMethods.MINIDUMP_CALLBACK_TYPE.MemoryCallback:
			return false;
		default:
			return true;
		}
	}

	private static bool WriteMini(SafeHandle file)
	{
		return Write(file, DbgHelpNativeMethods.MINIDUMP_TYPE.ConfigButterLibMini, Filter);
	}

	private static bool Write(SafeHandle file, DbgHelpNativeMethods.MINIDUMP_TYPE dumpType, DbgHelpNativeMethods.MINIDUMP_CALLBACK_ROUTINE callbackRoutine)
	{
		using (Process.GetCurrentProcess())
		{
			DbgHelpNativeMethods.MINIDUMP_EXCEPTION_INFORMATION ExceptionParams = new DbgHelpNativeMethods.MINIDUMP_EXCEPTION_INFORMATION
			{
				ThreadId = Kernel32NativeMethods.GetCurrentThreadId(),
				ExceptionPointers = Marshal.GetExceptionPointers(),
				ClientPointers = true
			};
			DbgHelpNativeMethods.MINIDUMP_USER_STREAM_INFORMATION UserStreamParam = default(DbgHelpNativeMethods.MINIDUMP_USER_STREAM_INFORMATION);
			IntPtr intPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Info>());
			Marshal.StructureToPtr(new Info
			{
				ThreadId = ExceptionParams.ThreadId
			}, intPtr, fDeleteOld: false);
			DbgHelpNativeMethods.MINIDUMP_CALLBACK_INFORMATION CallbackParam = new DbgHelpNativeMethods.MINIDUMP_CALLBACK_INFORMATION
			{
				CallbackParam = intPtr,
				CallbackRoutine = callbackRoutine
			};
			return DbgHelpNativeMethods.MiniDumpWriteDump(Kernel32NativeMethods.GetCurrentProcess(), Kernel32NativeMethods.GetCurrentProcessId(), file, dumpType, in ExceptionParams, in UserStreamParam, in CallbackParam);
		}
	}
}
