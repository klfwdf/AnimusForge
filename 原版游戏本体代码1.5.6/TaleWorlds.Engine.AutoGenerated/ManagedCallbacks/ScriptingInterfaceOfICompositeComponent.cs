using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfICompositeComponent : ICompositeComponent
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddComponentDelegate(UIntPtr pointer, UIntPtr componentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddMultiMeshDelegate(UIntPtr compositeComponentPointer, byte[] multiMeshName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddPrefabEntityDelegate(UIntPtr pointer, UIntPtr scenePointer, byte[] prefabName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateCompositeComponentDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateCopyDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoundingBoxDelegate(UIntPtr compositeComponentPointer, ref BoundingBox outBoundingBox);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetFactor1Delegate(UIntPtr compositeComponentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate uint GetFactor2Delegate(UIntPtr compositeComponentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetFirstMetaMeshDelegate(UIntPtr compositeComponentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetFrameDelegate(UIntPtr compositeComponentPointer, ref MatrixFrame outFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetVectorUserDataDelegate(UIntPtr compositeComponentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool IsVisibleDelegate(UIntPtr compositeComponentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ReleaseDelegate(UIntPtr compositeComponentPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFactor1Delegate(UIntPtr compositeComponentPointer, uint factorColor1);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFactor2Delegate(UIntPtr compositeComponentPointer, uint factorColor2);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetFrameDelegate(UIntPtr compositeComponentPointer, ref MatrixFrame meshFrame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetMaterialDelegate(UIntPtr compositeComponentPointer, UIntPtr materialPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVectorArgumentDelegate(UIntPtr compositeComponentPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVectorUserDataDelegate(UIntPtr compositeComponentPointer, ref Vec3 vectorArg);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVisibilityMaskDelegate(UIntPtr compositeComponentPointer, VisibilityMaskFlags visibilityMask);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetVisibleDelegate(UIntPtr compositeComponentPointer, [MarshalAs(UnmanagedType.U1)] bool visible);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddComponentDelegate call_AddComponentDelegate;

	public static AddMultiMeshDelegate call_AddMultiMeshDelegate;

	public static AddPrefabEntityDelegate call_AddPrefabEntityDelegate;

	public static CreateCompositeComponentDelegate call_CreateCompositeComponentDelegate;

	public static CreateCopyDelegate call_CreateCopyDelegate;

	public static GetBoundingBoxDelegate call_GetBoundingBoxDelegate;

	public static GetFactor1Delegate call_GetFactor1Delegate;

	public static GetFactor2Delegate call_GetFactor2Delegate;

	public static GetFirstMetaMeshDelegate call_GetFirstMetaMeshDelegate;

	public static GetFrameDelegate call_GetFrameDelegate;

	public static GetVectorUserDataDelegate call_GetVectorUserDataDelegate;

	public static IsVisibleDelegate call_IsVisibleDelegate;

	public static ReleaseDelegate call_ReleaseDelegate;

	public static SetFactor1Delegate call_SetFactor1Delegate;

	public static SetFactor2Delegate call_SetFactor2Delegate;

	public static SetFrameDelegate call_SetFrameDelegate;

	public static SetMaterialDelegate call_SetMaterialDelegate;

	public static SetVectorArgumentDelegate call_SetVectorArgumentDelegate;

	public static SetVectorUserDataDelegate call_SetVectorUserDataDelegate;

	public static SetVisibilityMaskDelegate call_SetVisibilityMaskDelegate;

	public static SetVisibleDelegate call_SetVisibleDelegate;

	public void AddComponent(UIntPtr pointer, UIntPtr componentPointer)
	{
		call_AddComponentDelegate(pointer, componentPointer);
	}

	public void AddMultiMesh(UIntPtr compositeComponentPointer, string multiMeshName)
	{
		byte[] array = null;
		if (multiMeshName != null)
		{
			int byteCount = _utf8.GetByteCount(multiMeshName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(multiMeshName, 0, multiMeshName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddMultiMeshDelegate(compositeComponentPointer, array);
	}

	public void AddPrefabEntity(UIntPtr pointer, UIntPtr scenePointer, string prefabName)
	{
		byte[] array = null;
		if (prefabName != null)
		{
			int byteCount = _utf8.GetByteCount(prefabName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(prefabName, 0, prefabName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddPrefabEntityDelegate(pointer, scenePointer, array);
	}

	public CompositeComponent CreateCompositeComponent()
	{
		NativeObjectPointer nativeObjectPointer = call_CreateCompositeComponentDelegate();
		CompositeComponent result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new CompositeComponent(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public CompositeComponent CreateCopy(UIntPtr pointer)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateCopyDelegate(pointer);
		CompositeComponent result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new CompositeComponent(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void GetBoundingBox(UIntPtr compositeComponentPointer, ref BoundingBox outBoundingBox)
	{
		call_GetBoundingBoxDelegate(compositeComponentPointer, ref outBoundingBox);
	}

	public uint GetFactor1(UIntPtr compositeComponentPointer)
	{
		return call_GetFactor1Delegate(compositeComponentPointer);
	}

	public uint GetFactor2(UIntPtr compositeComponentPointer)
	{
		return call_GetFactor2Delegate(compositeComponentPointer);
	}

	public MetaMesh GetFirstMetaMesh(UIntPtr compositeComponentPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_GetFirstMetaMeshDelegate(compositeComponentPointer);
		MetaMesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new MetaMesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void GetFrame(UIntPtr compositeComponentPointer, ref MatrixFrame outFrame)
	{
		call_GetFrameDelegate(compositeComponentPointer, ref outFrame);
	}

	public Vec3 GetVectorUserData(UIntPtr compositeComponentPointer)
	{
		return call_GetVectorUserDataDelegate(compositeComponentPointer);
	}

	public bool IsVisible(UIntPtr compositeComponentPointer)
	{
		return call_IsVisibleDelegate(compositeComponentPointer);
	}

	public void Release(UIntPtr compositeComponentPointer)
	{
		call_ReleaseDelegate(compositeComponentPointer);
	}

	public void SetFactor1(UIntPtr compositeComponentPointer, uint factorColor1)
	{
		call_SetFactor1Delegate(compositeComponentPointer, factorColor1);
	}

	public void SetFactor2(UIntPtr compositeComponentPointer, uint factorColor2)
	{
		call_SetFactor2Delegate(compositeComponentPointer, factorColor2);
	}

	public void SetFrame(UIntPtr compositeComponentPointer, ref MatrixFrame meshFrame)
	{
		call_SetFrameDelegate(compositeComponentPointer, ref meshFrame);
	}

	public void SetMaterial(UIntPtr compositeComponentPointer, UIntPtr materialPointer)
	{
		call_SetMaterialDelegate(compositeComponentPointer, materialPointer);
	}

	public void SetVectorArgument(UIntPtr compositeComponentPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
	{
		call_SetVectorArgumentDelegate(compositeComponentPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
	}

	public void SetVectorUserData(UIntPtr compositeComponentPointer, ref Vec3 vectorArg)
	{
		call_SetVectorUserDataDelegate(compositeComponentPointer, ref vectorArg);
	}

	public void SetVisibilityMask(UIntPtr compositeComponentPointer, VisibilityMaskFlags visibilityMask)
	{
		call_SetVisibilityMaskDelegate(compositeComponentPointer, visibilityMask);
	}

	public void SetVisible(UIntPtr compositeComponentPointer, bool visible)
	{
		call_SetVisibleDelegate(compositeComponentPointer, visible);
	}
}
