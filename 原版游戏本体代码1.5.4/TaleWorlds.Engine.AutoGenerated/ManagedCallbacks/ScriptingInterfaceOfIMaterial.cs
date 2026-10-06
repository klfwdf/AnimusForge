using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMaterial : IMaterial
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMaterialShaderFlagDelegate(UIntPtr materialPointer, byte[] flagName, [MarshalAs(UnmanagedType.U1)] bool showErrors);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateCopyDelegate(UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetAlphaBlendModeDelegate(UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate float GetAlphaTestValueDelegate(UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetDefaultMaterialDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate MaterialFlags GetFlagsDelegate(UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetFromResourceDelegate(byte[] materialName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNameDelegate(UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetOutlineMaterialDelegate(UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetShaderDelegate(UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate ulong GetShaderFlagsDelegate(UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetTextureDelegate(UIntPtr materialPointer, int textureType);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseDelegate(UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void RemoveMaterialShaderFlagDelegate(UIntPtr materialPointer, byte[] flagName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAlphaBlendModeDelegate(UIntPtr materialPointer, int alphaBlendMode);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAlphaTestValueDelegate(UIntPtr materialPointer, float alphaTestValue);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetAreaMapScaleDelegate(UIntPtr materialPointer, float scale);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetEnableSkinningDelegate(UIntPtr materialPointer, [MarshalAs(UnmanagedType.U1)] bool enable);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFlagsDelegate(UIntPtr materialPointer, MaterialFlags flags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMeshVectorArgumentDelegate(UIntPtr materialPointer, float x, float y, float z, float w);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetNameDelegate(UIntPtr materialPointer, byte[] name);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetShaderDelegate(UIntPtr materialPointer, UIntPtr shaderPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetShaderFlagsDelegate(UIntPtr materialPointer, ulong shaderFlags);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTextureDelegate(UIntPtr materialPointer, int textureType, UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetTextureAtSlotDelegate(UIntPtr materialPointer, int textureSlotIndex, UIntPtr texturePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool UsingSkinningDelegate(UIntPtr materialPointer);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddMaterialShaderFlagDelegate call_AddMaterialShaderFlagDelegate;

	public static CreateCopyDelegate call_CreateCopyDelegate;

	public static GetAlphaBlendModeDelegate call_GetAlphaBlendModeDelegate;

	public static GetAlphaTestValueDelegate call_GetAlphaTestValueDelegate;

	public static GetDefaultMaterialDelegate call_GetDefaultMaterialDelegate;

	public static GetFlagsDelegate call_GetFlagsDelegate;

	public static GetFromResourceDelegate call_GetFromResourceDelegate;

	public static GetNameDelegate call_GetNameDelegate;

	public static GetOutlineMaterialDelegate call_GetOutlineMaterialDelegate;

	public static GetShaderDelegate call_GetShaderDelegate;

	public static GetShaderFlagsDelegate call_GetShaderFlagsDelegate;

	public static GetTextureDelegate call_GetTextureDelegate;

	public static ReleaseDelegate call_ReleaseDelegate;

	public static RemoveMaterialShaderFlagDelegate call_RemoveMaterialShaderFlagDelegate;

	public static SetAlphaBlendModeDelegate call_SetAlphaBlendModeDelegate;

	public static SetAlphaTestValueDelegate call_SetAlphaTestValueDelegate;

	public static SetAreaMapScaleDelegate call_SetAreaMapScaleDelegate;

	public static SetEnableSkinningDelegate call_SetEnableSkinningDelegate;

	public static SetFlagsDelegate call_SetFlagsDelegate;

	public static SetMeshVectorArgumentDelegate call_SetMeshVectorArgumentDelegate;

	public static SetNameDelegate call_SetNameDelegate;

	public static SetShaderDelegate call_SetShaderDelegate;

	public static SetShaderFlagsDelegate call_SetShaderFlagsDelegate;

	public static SetTextureDelegate call_SetTextureDelegate;

	public static SetTextureAtSlotDelegate call_SetTextureAtSlotDelegate;

	public static UsingSkinningDelegate call_UsingSkinningDelegate;

	public void AddMaterialShaderFlag(UIntPtr materialPointer, string flagName, bool showErrors)
	{
		byte[] array = null;
		if (flagName != null)
		{
			int byteCount = _utf8.GetByteCount(flagName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(flagName, 0, flagName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddMaterialShaderFlagDelegate(materialPointer, array, showErrors);
	}

	public Material CreateCopy(UIntPtr materialPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateCopyDelegate(materialPointer);
		Material result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Material(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public int GetAlphaBlendMode(UIntPtr materialPointer)
	{
		return call_GetAlphaBlendModeDelegate(materialPointer);
	}

	public float GetAlphaTestValue(UIntPtr materialPointer)
	{
		return call_GetAlphaTestValueDelegate(materialPointer);
	}

	public Material GetDefaultMaterial()
	{
		NativeObjectPointer nativeObjectPointer = call_GetDefaultMaterialDelegate();
		Material result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Material(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public MaterialFlags GetFlags(UIntPtr materialPointer)
	{
		return call_GetFlagsDelegate(materialPointer);
	}

	public Material GetFromResource(string materialName)
	{
		byte[] array = null;
		if (materialName != null)
		{
			int byteCount = _utf8.GetByteCount(materialName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(materialName, 0, materialName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetFromResourceDelegate(array);
		Material result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Material(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public string GetName(UIntPtr materialPointer)
	{
		if (call_GetNameDelegate(materialPointer) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public Material GetOutlineMaterial(UIntPtr materialPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_GetOutlineMaterialDelegate(materialPointer);
		Material result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Material(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Shader GetShader(UIntPtr materialPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_GetShaderDelegate(materialPointer);
		Shader result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Shader(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public ulong GetShaderFlags(UIntPtr materialPointer)
	{
		return call_GetShaderFlagsDelegate(materialPointer);
	}

	public Texture GetTexture(UIntPtr materialPointer, int textureType)
	{
		NativeObjectPointer nativeObjectPointer = call_GetTextureDelegate(materialPointer, textureType);
		Texture result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Texture(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void Release(UIntPtr materialPointer)
	{
		call_ReleaseDelegate(materialPointer);
	}

	public void RemoveMaterialShaderFlag(UIntPtr materialPointer, string flagName)
	{
		byte[] array = null;
		if (flagName != null)
		{
			int byteCount = _utf8.GetByteCount(flagName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(flagName, 0, flagName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_RemoveMaterialShaderFlagDelegate(materialPointer, array);
	}

	public void SetAlphaBlendMode(UIntPtr materialPointer, int alphaBlendMode)
	{
		call_SetAlphaBlendModeDelegate(materialPointer, alphaBlendMode);
	}

	public void SetAlphaTestValue(UIntPtr materialPointer, float alphaTestValue)
	{
		call_SetAlphaTestValueDelegate(materialPointer, alphaTestValue);
	}

	public void SetAreaMapScale(UIntPtr materialPointer, float scale)
	{
		call_SetAreaMapScaleDelegate(materialPointer, scale);
	}

	public void SetEnableSkinning(UIntPtr materialPointer, bool enable)
	{
		call_SetEnableSkinningDelegate(materialPointer, enable);
	}

	public void SetFlags(UIntPtr materialPointer, MaterialFlags flags)
	{
		call_SetFlagsDelegate(materialPointer, flags);
	}

	public void SetMeshVectorArgument(UIntPtr materialPointer, float x, float y, float z, float w)
	{
		call_SetMeshVectorArgumentDelegate(materialPointer, x, y, z, w);
	}

	public void SetName(UIntPtr materialPointer, string name)
	{
		byte[] array = null;
		if (name != null)
		{
			int byteCount = _utf8.GetByteCount(name);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(name, 0, name.Length, array, 0);
			array[byteCount] = 0;
		}
		call_SetNameDelegate(materialPointer, array);
	}

	public void SetShader(UIntPtr materialPointer, UIntPtr shaderPointer)
	{
		call_SetShaderDelegate(materialPointer, shaderPointer);
	}

	public void SetShaderFlags(UIntPtr materialPointer, ulong shaderFlags)
	{
		call_SetShaderFlagsDelegate(materialPointer, shaderFlags);
	}

	public void SetTexture(UIntPtr materialPointer, int textureType, UIntPtr texturePointer)
	{
		call_SetTextureDelegate(materialPointer, textureType, texturePointer);
	}

	public void SetTextureAtSlot(UIntPtr materialPointer, int textureSlotIndex, UIntPtr texturePointer)
	{
		call_SetTextureAtSlotDelegate(materialPointer, textureSlotIndex, texturePointer);
	}

	public bool UsingSkinning(UIntPtr materialPointer)
	{
		return call_UsingSkinningDelegate(materialPointer);
	}
}
