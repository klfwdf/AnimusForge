using System;
using System.Runtime.InteropServices;
using System.Security;

namespace TaleWorlds.Starter.Library;

internal static class MBDotNet
{
	public const string MainDllName = "TaleWorlds.Native.dll";

	[DllImport("TaleWorlds.Native.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "WotsMainSDLL")]
	[SuppressUnmanagedCodeSecurity]
	public static extern int WotsMainDotNet(string args);

	[DllImport("TaleWorlds.Native.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "pass_controller_methods")]
	[SuppressUnmanagedCodeSecurity]
	public static extern void PassControllerMethods(Delegate currentDomainInitializer);

	[DllImport("TaleWorlds.Native.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "pass_managed_initialize_method_pointer")]
	[SuppressUnmanagedCodeSecurity]
	public static extern void PassManagedInitializeMethodPointerDotNet([MarshalAs(UnmanagedType.FunctionPtr)] Delegate initalizer);

	[DllImport("TaleWorlds.Native.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "pass_managed_library_callback_method_pointers")]
	[SuppressUnmanagedCodeSecurity]
	public static extern void PassManagedEngineCallbackMethodPointersDotNet([MarshalAs(UnmanagedType.FunctionPtr)] Delegate methodDelegate);

	[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
	[SuppressUnmanagedCodeSecurity]
	public static extern int SetCurrentDirectory(string args);
}
