using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfILight : ILight
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreatePointLightDelegate(float lightRadius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EnableShadowDelegate(UIntPtr lightpointer, [MarshalAs(UnmanagedType.U1)] bool shadowEnabled);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetFrameDelegate(UIntPtr lightPointer, out MatrixFrame result);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetIntensityDelegate(UIntPtr lightPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetLightColorDelegate(UIntPtr lightpointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetRadiusDelegate(UIntPtr lightpointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsShadowEnabledDelegate(UIntPtr lightpointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseDelegate(UIntPtr lightpointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFrameDelegate(UIntPtr lightPointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetIntensityDelegate(UIntPtr lightPointer, float value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLightColorDelegate(UIntPtr lightpointer, Vec3 color);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetLightFlickerDelegate(UIntPtr lightpointer, float magnitude, float interval);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetRadiusDelegate(UIntPtr lightpointer, float radius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetShadowsDelegate(UIntPtr lightPointer, int shadowType);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVisibilityDelegate(UIntPtr lightpointer, [MarshalAs(UnmanagedType.U1)] bool value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVolumetricPropertiesDelegate(UIntPtr lightpointer, [MarshalAs(UnmanagedType.U1)] bool volumelightenabled, float volumeparameter);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreatePointLightDelegate call_CreatePointLightDelegate;

	public static EnableShadowDelegate call_EnableShadowDelegate;

	public static GetFrameDelegate call_GetFrameDelegate;

	public static GetIntensityDelegate call_GetIntensityDelegate;

	public static GetLightColorDelegate call_GetLightColorDelegate;

	public static GetRadiusDelegate call_GetRadiusDelegate;

	public static IsShadowEnabledDelegate call_IsShadowEnabledDelegate;

	public static ReleaseDelegate call_ReleaseDelegate;

	public static SetFrameDelegate call_SetFrameDelegate;

	public static SetIntensityDelegate call_SetIntensityDelegate;

	public static SetLightColorDelegate call_SetLightColorDelegate;

	public static SetLightFlickerDelegate call_SetLightFlickerDelegate;

	public static SetRadiusDelegate call_SetRadiusDelegate;

	public static SetShadowsDelegate call_SetShadowsDelegate;

	public static SetVisibilityDelegate call_SetVisibilityDelegate;

	public static SetVolumetricPropertiesDelegate call_SetVolumetricPropertiesDelegate;

	public Light CreatePointLight(float lightRadius)
	{
		NativeObjectPointer nativeObjectPointer = call_CreatePointLightDelegate(lightRadius);
		Light result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Light(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void EnableShadow(UIntPtr lightpointer, bool shadowEnabled)
	{
		call_EnableShadowDelegate(lightpointer, shadowEnabled);
	}

	public void GetFrame(UIntPtr lightPointer, out MatrixFrame result)
	{
		call_GetFrameDelegate(lightPointer, out result);
	}

	public float GetIntensity(UIntPtr lightPointer)
	{
		return call_GetIntensityDelegate(lightPointer);
	}

	public Vec3 GetLightColor(UIntPtr lightpointer)
	{
		return call_GetLightColorDelegate(lightpointer);
	}

	public float GetRadius(UIntPtr lightpointer)
	{
		return call_GetRadiusDelegate(lightpointer);
	}

	public bool IsShadowEnabled(UIntPtr lightpointer)
	{
		return call_IsShadowEnabledDelegate(lightpointer);
	}

	public void Release(UIntPtr lightpointer)
	{
		call_ReleaseDelegate(lightpointer);
	}

	public void SetFrame(UIntPtr lightPointer, ref MatrixFrame frame)
	{
		call_SetFrameDelegate(lightPointer, ref frame);
	}

	public void SetIntensity(UIntPtr lightPointer, float value)
	{
		call_SetIntensityDelegate(lightPointer, value);
	}

	public void SetLightColor(UIntPtr lightpointer, Vec3 color)
	{
		call_SetLightColorDelegate(lightpointer, color);
	}

	public void SetLightFlicker(UIntPtr lightpointer, float magnitude, float interval)
	{
		call_SetLightFlickerDelegate(lightpointer, magnitude, interval);
	}

	public void SetRadius(UIntPtr lightpointer, float radius)
	{
		call_SetRadiusDelegate(lightpointer, radius);
	}

	public void SetShadows(UIntPtr lightPointer, int shadowType)
	{
		call_SetShadowsDelegate(lightPointer, shadowType);
	}

	public void SetVisibility(UIntPtr lightpointer, bool value)
	{
		call_SetVisibilityDelegate(lightpointer, value);
	}

	public void SetVolumetricProperties(UIntPtr lightpointer, bool volumelightenabled, float volumeparameter)
	{
		call_SetVolumetricPropertiesDelegate(lightpointer, volumelightenabled, volumeparameter);
	}
}
