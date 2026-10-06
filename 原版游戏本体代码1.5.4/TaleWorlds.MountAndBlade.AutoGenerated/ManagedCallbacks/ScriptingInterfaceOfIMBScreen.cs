using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBScreen : IMBScreen
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OnEditModeEnterPressDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OnEditModeEnterReleaseDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OnExitButtonClickDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static OnEditModeEnterPressDelegate call_OnEditModeEnterPressDelegate;

	public static OnEditModeEnterReleaseDelegate call_OnEditModeEnterReleaseDelegate;

	public static OnExitButtonClickDelegate call_OnExitButtonClickDelegate;

	public void OnEditModeEnterPress()
	{
		call_OnEditModeEnterPressDelegate();
	}

	public void OnEditModeEnterRelease()
	{
		call_OnEditModeEnterReleaseDelegate();
	}

	public void OnExitButtonClick()
	{
		call_OnExitButtonClickDelegate();
	}
}
