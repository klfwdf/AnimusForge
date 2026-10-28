using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMBDebugExtensions : IMBDebugExtensions
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void OverrideNativeParameterDelegate(byte[] paramName, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReloadNativeParametersDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugArcOnTerrainDelegate(UIntPtr scenePointer, ref MatrixFrame frame, float radius, float beginAngle, float endAngle, uint color, [MarshalAs(UnmanagedType.U1)] bool depthCheck, [MarshalAs(UnmanagedType.U1)] bool isDotted);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugCircleOnTerrainDelegate(UIntPtr scenePointer, ref MatrixFrame frame, float radius, uint color, [MarshalAs(UnmanagedType.U1)] bool depthCheck, [MarshalAs(UnmanagedType.U1)] bool isDotted);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RenderDebugLineOnTerrainDelegate(UIntPtr scenePointer, Vec3 position, Vec3 direction, uint color, [MarshalAs(UnmanagedType.U1)] bool depthCheck, float time, [MarshalAs(UnmanagedType.U1)] bool isDotted, float pointDensity);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static OverrideNativeParameterDelegate call_OverrideNativeParameterDelegate;

	public static ReloadNativeParametersDelegate call_ReloadNativeParametersDelegate;

	public static RenderDebugArcOnTerrainDelegate call_RenderDebugArcOnTerrainDelegate;

	public static RenderDebugCircleOnTerrainDelegate call_RenderDebugCircleOnTerrainDelegate;

	public static RenderDebugLineOnTerrainDelegate call_RenderDebugLineOnTerrainDelegate;

	public void OverrideNativeParameter(string paramName, float value)
	{
		byte[] array = null;
		if (paramName != null)
		{
			int byteCount = _utf8.GetByteCount(paramName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(paramName, 0, paramName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_OverrideNativeParameterDelegate(array, value);
	}

	public void ReloadNativeParameters()
	{
		call_ReloadNativeParametersDelegate();
	}

	public void RenderDebugArcOnTerrain(UIntPtr scenePointer, ref MatrixFrame frame, float radius, float beginAngle, float endAngle, uint color, bool depthCheck, bool isDotted)
	{
		call_RenderDebugArcOnTerrainDelegate(scenePointer, ref frame, radius, beginAngle, endAngle, color, depthCheck, isDotted);
	}

	public void RenderDebugCircleOnTerrain(UIntPtr scenePointer, ref MatrixFrame frame, float radius, uint color, bool depthCheck, bool isDotted)
	{
		call_RenderDebugCircleOnTerrainDelegate(scenePointer, ref frame, radius, color, depthCheck, isDotted);
	}

	public void RenderDebugLineOnTerrain(UIntPtr scenePointer, Vec3 position, Vec3 direction, uint color, bool depthCheck, float time, bool isDotted, float pointDensity)
	{
		call_RenderDebugLineOnTerrainDelegate(scenePointer, position, direction, color, depthCheck, time, isDotted, pointDensity);
	}
}
