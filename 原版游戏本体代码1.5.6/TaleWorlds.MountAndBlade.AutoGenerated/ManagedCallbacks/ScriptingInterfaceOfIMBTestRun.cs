using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBTestRun : IMBTestRun
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int AutoContinueDelegate(int type);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool CloseSceneDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool EnterEditModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetFPSDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool LeaveEditModeDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool NewSceneDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool OpenDefaultSceneDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool OpenSceneDelegate(byte[] sceneName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool SaveSceneDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void StartMissionDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AutoContinueDelegate call_AutoContinueDelegate;

	public static CloseSceneDelegate call_CloseSceneDelegate;

	public static EnterEditModeDelegate call_EnterEditModeDelegate;

	public static GetFPSDelegate call_GetFPSDelegate;

	public static LeaveEditModeDelegate call_LeaveEditModeDelegate;

	public static NewSceneDelegate call_NewSceneDelegate;

	public static OpenDefaultSceneDelegate call_OpenDefaultSceneDelegate;

	public static OpenSceneDelegate call_OpenSceneDelegate;

	public static SaveSceneDelegate call_SaveSceneDelegate;

	public static StartMissionDelegate call_StartMissionDelegate;

	public int AutoContinue(int type)
	{
		return call_AutoContinueDelegate(type);
	}

	public bool CloseScene()
	{
		return call_CloseSceneDelegate();
	}

	public bool EnterEditMode()
	{
		return call_EnterEditModeDelegate();
	}

	public int GetFPS()
	{
		return call_GetFPSDelegate();
	}

	public bool LeaveEditMode()
	{
		return call_LeaveEditModeDelegate();
	}

	public bool NewScene()
	{
		return call_NewSceneDelegate();
	}

	public bool OpenDefaultScene()
	{
		return call_OpenDefaultSceneDelegate();
	}

	public bool OpenScene(string sceneName)
	{
		byte[] array = null;
		if (sceneName != null)
		{
			int byteCount = _utf8.GetByteCount(sceneName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(sceneName, 0, sceneName.Length, array, 0);
			array[byteCount] = 0;
		}
		return call_OpenSceneDelegate(array);
	}

	public bool SaveScene()
	{
		return call_SaveSceneDelegate();
	}

	public void StartMission()
	{
		call_StartMissionDelegate();
	}
}
