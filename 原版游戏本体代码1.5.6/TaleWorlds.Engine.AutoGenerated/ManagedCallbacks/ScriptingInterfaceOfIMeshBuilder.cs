using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIMeshBuilder : IMeshBuilder
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateTilingButtonMeshDelegate(byte[] baseMeshName, ref Vec2 meshSizeMin, ref Vec2 meshSizeMax, ref Vec2 borderThickness);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateTilingWindowMeshDelegate(byte[] baseMeshName, ref Vec2 meshSizeMin, ref Vec2 meshSizeMax, ref Vec2 borderThickness, ref Vec2 backgroundBorderThickness);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer FinalizeMeshBuilderDelegate(int num_vertices, IntPtr vertices, int num_face_corners, IntPtr faceCorners, int num_faces, IntPtr faces);

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static CreateTilingButtonMeshDelegate call_CreateTilingButtonMeshDelegate;

	public static CreateTilingWindowMeshDelegate call_CreateTilingWindowMeshDelegate;

	public static FinalizeMeshBuilderDelegate call_FinalizeMeshBuilderDelegate;

	public Mesh CreateTilingButtonMesh(string baseMeshName, ref Vec2 meshSizeMin, ref Vec2 meshSizeMax, ref Vec2 borderThickness)
	{
		byte[] array = null;
		if (baseMeshName != null)
		{
			int byteCount = _utf8.GetByteCount(baseMeshName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(baseMeshName, 0, baseMeshName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateTilingButtonMeshDelegate(array, ref meshSizeMin, ref meshSizeMax, ref borderThickness);
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Mesh CreateTilingWindowMesh(string baseMeshName, ref Vec2 meshSizeMin, ref Vec2 meshSizeMax, ref Vec2 borderThickness, ref Vec2 backgroundBorderThickness)
	{
		byte[] array = null;
		if (baseMeshName != null)
		{
			int byteCount = _utf8.GetByteCount(baseMeshName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(baseMeshName, 0, baseMeshName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_CreateTilingWindowMeshDelegate(array, ref meshSizeMin, ref meshSizeMax, ref borderThickness, ref backgroundBorderThickness);
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public Mesh FinalizeMeshBuilder(int num_vertices, Vec3[] vertices, int num_face_corners, MeshBuilder.FaceCorner[] faceCorners, int num_faces, MeshBuilder.Face[] faces)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(vertices);
		IntPtr pointer = pinnedArrayData.Pointer;
		PinnedArrayData<MeshBuilder.FaceCorner> pinnedArrayData2 = new PinnedArrayData<MeshBuilder.FaceCorner>(faceCorners);
		IntPtr pointer2 = pinnedArrayData2.Pointer;
		PinnedArrayData<MeshBuilder.Face> pinnedArrayData3 = new PinnedArrayData<MeshBuilder.Face>(faces);
		IntPtr pointer3 = pinnedArrayData3.Pointer;
		NativeObjectPointer nativeObjectPointer = call_FinalizeMeshBuilderDelegate(num_vertices, pointer, num_face_corners, pointer2, num_faces, pointer3);
		pinnedArrayData.Dispose();
		pinnedArrayData2.Dispose();
		pinnedArrayData3.Dispose();
		Mesh result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new Mesh(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}
}
