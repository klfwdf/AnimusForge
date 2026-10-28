using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBWindowManager : IMBWindowManager
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void DontChangeCursorPosDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EraseMessageLinesDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec2 GetScreenResolutionDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PreDisplayDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ScreenToWorldDelegate(UIntPtr pointer, float screenX, float screenY, float z, ref Vec3 worldSpacePosition);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float WorldToScreenDelegate(UIntPtr cameraPointer, Vec3 worldSpacePosition, ref float screenX, ref float screenY, ref float w);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float WorldToScreenWithFixedZDelegate(UIntPtr cameraPointer, Vec3 cameraPosition, Vec3 worldSpacePosition, ref float screenX, ref float screenY, ref float w);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static DontChangeCursorPosDelegate call_DontChangeCursorPosDelegate;

	public static EraseMessageLinesDelegate call_EraseMessageLinesDelegate;

	public static GetScreenResolutionDelegate call_GetScreenResolutionDelegate;

	public static PreDisplayDelegate call_PreDisplayDelegate;

	public static ScreenToWorldDelegate call_ScreenToWorldDelegate;

	public static WorldToScreenDelegate call_WorldToScreenDelegate;

	public static WorldToScreenWithFixedZDelegate call_WorldToScreenWithFixedZDelegate;

	public void DontChangeCursorPos()
	{
		call_DontChangeCursorPosDelegate();
	}

	public void EraseMessageLines()
	{
		call_EraseMessageLinesDelegate();
	}

	public Vec2 GetScreenResolution()
	{
		return call_GetScreenResolutionDelegate();
	}

	public void PreDisplay()
	{
		call_PreDisplayDelegate();
	}

	public void ScreenToWorld(UIntPtr pointer, float screenX, float screenY, float z, ref Vec3 worldSpacePosition)
	{
		call_ScreenToWorldDelegate(pointer, screenX, screenY, z, ref worldSpacePosition);
	}

	public float WorldToScreen(UIntPtr cameraPointer, Vec3 worldSpacePosition, ref float screenX, ref float screenY, ref float w)
	{
		return call_WorldToScreenDelegate(cameraPointer, worldSpacePosition, ref screenX, ref screenY, ref w);
	}

	public float WorldToScreenWithFixedZ(UIntPtr cameraPointer, Vec3 cameraPosition, Vec3 worldSpacePosition, ref float screenX, ref float screenY, ref float w)
	{
		return call_WorldToScreenWithFixedZDelegate(cameraPointer, cameraPosition, worldSpacePosition, ref screenX, ref screenY, ref w);
	}
}
