using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfITwoDimensionView : ITwoDimensionView
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool AddCachedTextMeshDelegate(UIntPtr pointer, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddNewMeshDelegate(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddNewQuadMeshDelegate(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddNewTextMeshDelegate(UIntPtr pointer, IntPtr vertices, IntPtr uvs, IntPtr indices, int vertexCount, int indexCount, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void BeginFrameDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ClearDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateTwoDimensionViewDelegate(byte[] viewName);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void EndFrameDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate UIntPtr GetOrCreateMaterialDelegate(UIntPtr pointer, UIntPtr mainTexture, UIntPtr overlayTexture);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddCachedTextMeshDelegate call_AddCachedTextMeshDelegate;

	public static AddNewMeshDelegate call_AddNewMeshDelegate;

	public static AddNewQuadMeshDelegate call_AddNewQuadMeshDelegate;

	public static AddNewTextMeshDelegate call_AddNewTextMeshDelegate;

	public static BeginFrameDelegate call_BeginFrameDelegate;

	public static ClearDelegate call_ClearDelegate;

	public static CreateTwoDimensionViewDelegate call_CreateTwoDimensionViewDelegate;

	public static EndFrameDelegate call_EndFrameDelegate;

	public static GetOrCreateMaterialDelegate call_GetOrCreateMaterialDelegate;

	public bool AddCachedTextMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData)
	{
		return call_AddCachedTextMeshDelegate(pointer, material, ref meshDrawData);
	}

	public void AddNewMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData)
	{
		call_AddNewMeshDelegate(pointer, material, ref meshDrawData);
	}

	public void AddNewQuadMesh(UIntPtr pointer, UIntPtr material, ref TwoDimensionMeshDrawData meshDrawData)
	{
		call_AddNewQuadMeshDelegate(pointer, material, ref meshDrawData);
	}

	public void AddNewTextMesh(UIntPtr pointer, float[] vertices, float[] uvs, uint[] indices, int vertexCount, int indexCount, UIntPtr material, ref TwoDimensionTextMeshDrawData meshDrawData)
	{
		PinnedArrayData<float> pinnedArrayData = new PinnedArrayData<float>(vertices);
		IntPtr pointer2 = pinnedArrayData.Pointer;
		PinnedArrayData<float> pinnedArrayData2 = new PinnedArrayData<float>(uvs);
		IntPtr pointer3 = pinnedArrayData2.Pointer;
		PinnedArrayData<uint> pinnedArrayData3 = new PinnedArrayData<uint>(indices);
		IntPtr pointer4 = pinnedArrayData3.Pointer;
		call_AddNewTextMeshDelegate(pointer, pointer2, pointer3, pointer4, vertexCount, indexCount, material, ref meshDrawData);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		pinnedArrayData3.Dispose();
	}

	public void BeginFrame(UIntPtr pointer)
	{
		call_BeginFrameDelegate(pointer);
	}

	public void Clear(UIntPtr pointer)
	{
		call_ClearDelegate(pointer);
	}

	public TwoDimensionView CreateTwoDimensionView(string viewName)
	{
		byte[] array = null;
		if (viewName != null)
		{
			int byteCount = _utf8.GetByteCount(viewName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(viewName, 0, viewName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateTwoDimensionViewDelegate(array);
		TwoDimensionView result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new TwoDimensionView(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void EndFrame(UIntPtr pointer)
	{
		call_EndFrameDelegate(pointer);
	}

	public UIntPtr GetOrCreateMaterial(UIntPtr pointer, UIntPtr mainTexture, UIntPtr overlayTexture)
	{
		return call_GetOrCreateMaterialDelegate(pointer, mainTexture, overlayTexture);
	}
}
