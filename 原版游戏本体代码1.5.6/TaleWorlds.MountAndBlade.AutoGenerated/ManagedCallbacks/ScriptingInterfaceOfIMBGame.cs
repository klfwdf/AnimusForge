using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBGame : IMBGame
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void LoadModuleDataDelegate([MarshalAs(UnmanagedType.U1)] bool isLoadGame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StartNewDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static LoadModuleDataDelegate call_LoadModuleDataDelegate;

	public static StartNewDelegate call_StartNewDelegate;

	public void LoadModuleData(bool isLoadGame)
	{
		call_LoadModuleDataDelegate(isLoadGame);
	}

	public void StartNew()
	{
		call_StartNewDelegate();
	}
}
