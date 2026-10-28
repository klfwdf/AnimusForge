using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIScreen : IScreen
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetAspectRatioDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetDesktopHeightDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetDesktopWidthDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool GetMouseVisibleDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetRealScreenResolutionHeightDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetRealScreenResolutionWidthDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 GetUsableAreaPercentagesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsEnterButtonCrossDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMouseVisibleDelegate([MarshalAs(UnmanagedType.U1)] bool value);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static GetAspectRatioDelegate call_GetAspectRatioDelegate;

	public static GetDesktopHeightDelegate call_GetDesktopHeightDelegate;

	public static GetDesktopWidthDelegate call_GetDesktopWidthDelegate;

	public static GetMouseVisibleDelegate call_GetMouseVisibleDelegate;

	public static GetRealScreenResolutionHeightDelegate call_GetRealScreenResolutionHeightDelegate;

	public static GetRealScreenResolutionWidthDelegate call_GetRealScreenResolutionWidthDelegate;

	public static GetUsableAreaPercentagesDelegate call_GetUsableAreaPercentagesDelegate;

	public static IsEnterButtonCrossDelegate call_IsEnterButtonCrossDelegate;

	public static SetMouseVisibleDelegate call_SetMouseVisibleDelegate;

	public float GetAspectRatio()
	{
		return call_GetAspectRatioDelegate();
	}

	public float GetDesktopHeight()
	{
		return call_GetDesktopHeightDelegate();
	}

	public float GetDesktopWidth()
	{
		return call_GetDesktopWidthDelegate();
	}

	public bool GetMouseVisible()
	{
		return call_GetMouseVisibleDelegate();
	}

	public float GetRealScreenResolutionHeight()
	{
		return call_GetRealScreenResolutionHeightDelegate();
	}

	public float GetRealScreenResolutionWidth()
	{
		return call_GetRealScreenResolutionWidthDelegate();
	}

	public Vec2 GetUsableAreaPercentages()
	{
		return call_GetUsableAreaPercentagesDelegate();
	}

	public bool IsEnterButtonCross()
	{
		return call_IsEnterButtonCrossDelegate();
	}

	public void SetMouseVisible(bool value)
	{
		call_SetMouseVisibleDelegate(value);
	}
}
