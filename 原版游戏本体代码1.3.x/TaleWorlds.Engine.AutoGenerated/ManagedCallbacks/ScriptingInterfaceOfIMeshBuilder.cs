using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x0200001A RID: 26
	internal class ScriptingInterfaceOfIMeshBuilder : IMeshBuilder
	{
		// Token: 0x06000354 RID: 852 RVA: 0x00013F68 File Offset: 0x00012168
		public Mesh CreateTilingButtonMesh(string baseMeshName, ref Vec2 meshSizeMin, ref Vec2 meshSizeMax, ref Vec2 borderThickness)
		{
			byte[] array = null;
			if (baseMeshName != null)
			{
				int byteCount = ScriptingInterfaceOfIMeshBuilder._utf8.GetByteCount(baseMeshName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMeshBuilder._utf8.GetBytes(baseMeshName, 0, baseMeshName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMeshBuilder.call_CreateTilingButtonMeshDelegate(array, ref meshSizeMin, ref meshSizeMax, ref borderThickness);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000355 RID: 853 RVA: 0x00013FF8 File Offset: 0x000121F8
		public Mesh CreateTilingWindowMesh(string baseMeshName, ref Vec2 meshSizeMin, ref Vec2 meshSizeMax, ref Vec2 borderThickness, ref Vec2 backgroundBorderThickness)
		{
			byte[] array = null;
			if (baseMeshName != null)
			{
				int byteCount = ScriptingInterfaceOfIMeshBuilder._utf8.GetByteCount(baseMeshName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMeshBuilder._utf8.GetBytes(baseMeshName, 0, baseMeshName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMeshBuilder.call_CreateTilingWindowMeshDelegate(array, ref meshSizeMin, ref meshSizeMax, ref borderThickness, ref backgroundBorderThickness);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000356 RID: 854 RVA: 0x0001408C File Offset: 0x0001228C
		public Mesh FinalizeMeshBuilder(int num_vertices, Vec3[] vertices, int num_face_corners, MeshBuilder.FaceCorner[] faceCorners, int num_faces, MeshBuilder.Face[] faces)
		{
			PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(vertices, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			PinnedArrayData<MeshBuilder.FaceCorner> pinnedArrayData2 = new PinnedArrayData<MeshBuilder.FaceCorner>(faceCorners, false);
			IntPtr pointer2 = pinnedArrayData2.Pointer;
			PinnedArrayData<MeshBuilder.Face> pinnedArrayData3 = new PinnedArrayData<MeshBuilder.Face>(faces, false);
			IntPtr pointer3 = pinnedArrayData3.Pointer;
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMeshBuilder.call_FinalizeMeshBuilderDelegate(num_vertices, pointer, num_face_corners, pointer2, num_faces, pointer3);
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

		// Token: 0x040002C8 RID: 712
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x040002C9 RID: 713
		public static ScriptingInterfaceOfIMeshBuilder.CreateTilingButtonMeshDelegate call_CreateTilingButtonMeshDelegate;

		// Token: 0x040002CA RID: 714
		public static ScriptingInterfaceOfIMeshBuilder.CreateTilingWindowMeshDelegate call_CreateTilingWindowMeshDelegate;

		// Token: 0x040002CB RID: 715
		public static ScriptingInterfaceOfIMeshBuilder.FinalizeMeshBuilderDelegate call_FinalizeMeshBuilderDelegate;

		// Token: 0x0200033D RID: 829
		// (Invoke) Token: 0x06001307 RID: 4871
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateTilingButtonMeshDelegate(byte[] baseMeshName, ref Vec2 meshSizeMin, ref Vec2 meshSizeMax, ref Vec2 borderThickness);

		// Token: 0x0200033E RID: 830
		// (Invoke) Token: 0x0600130B RID: 4875
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateTilingWindowMeshDelegate(byte[] baseMeshName, ref Vec2 meshSizeMin, ref Vec2 meshSizeMax, ref Vec2 borderThickness, ref Vec2 backgroundBorderThickness);

		// Token: 0x0200033F RID: 831
		// (Invoke) Token: 0x0600130F RID: 4879
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer FinalizeMeshBuilderDelegate(int num_vertices, IntPtr vertices, int num_face_corners, IntPtr faceCorners, int num_faces, IntPtr faces);
	}
}
