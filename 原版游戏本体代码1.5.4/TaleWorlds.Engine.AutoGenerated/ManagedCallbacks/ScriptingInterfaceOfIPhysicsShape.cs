using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks;

internal class ScriptingInterfaceOfIPhysicsShape : IPhysicsShape
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddCapsuleDelegate(UIntPtr shapePointer, ref CapsuleData data);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddPreloadQueueWithNameDelegate(byte[] bodyName, ref Vec3 scale);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void AddSphereDelegate(UIntPtr shapePointer, ref Vec3 origin, float radius);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int CapsuleCountDelegate(UIntPtr shapePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void clearDelegate(UIntPtr shapePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer CreateBodyCopyDelegate(UIntPtr bodyPointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetBoundingBoxDelegate(UIntPtr shapePointer, out BoundingBox boundingBox);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate Vec3 GetBoundingBoxCenterDelegate(UIntPtr shapePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetCapsuleDelegate(UIntPtr shapePointer, ref CapsuleData data, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetCapsuleWithMaterialDelegate(UIntPtr shapePointer, ref CapsuleData data, ref int materialIndex, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetDominantMaterialForTriangleMeshDelegate(UIntPtr shape, int meshIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate NativeObjectPointer GetFromResourceDelegate(byte[] bodyName, [MarshalAs(UnmanagedType.U1)] bool mayReturnNull);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int GetNameDelegate(UIntPtr shape);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetSphereDelegate(UIntPtr shapePointer, ref SphereData data, int sphereIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetSphereWithMaterialDelegate(UIntPtr shapePointer, ref SphereData data, ref int materialIndex, int sphereIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void GetTriangleDelegate(UIntPtr pointer, IntPtr data, int meshIndex, int triangleIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void InitDescriptionDelegate(UIntPtr shapePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void PrepareDelegate(UIntPtr shapePointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void ProcessPreloadQueueDelegate();

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void SetCapsuleDelegate(UIntPtr shapePointer, ref CapsuleData data, int index);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int SphereCountDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void TransformDelegate(UIntPtr shapePointer, ref MatrixFrame frame);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int TriangleCountInTriangleMeshDelegate(UIntPtr pointer, int meshIndex);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate int TriangleMeshCountDelegate(UIntPtr pointer);

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	[SuppressUnmanagedCodeSecurity]
	[MonoNativeFunctionWrapper]
	public delegate void UnloadDynamicBodiesDelegate();

	private static readonly Encoding _utf8 = Encoding.UTF8;

	public static AddCapsuleDelegate call_AddCapsuleDelegate;

	public static AddPreloadQueueWithNameDelegate call_AddPreloadQueueWithNameDelegate;

	public static AddSphereDelegate call_AddSphereDelegate;

	public static CapsuleCountDelegate call_CapsuleCountDelegate;

	public static clearDelegate call_clearDelegate;

	public static CreateBodyCopyDelegate call_CreateBodyCopyDelegate;

	public static GetBoundingBoxDelegate call_GetBoundingBoxDelegate;

	public static GetBoundingBoxCenterDelegate call_GetBoundingBoxCenterDelegate;

	public static GetCapsuleDelegate call_GetCapsuleDelegate;

	public static GetCapsuleWithMaterialDelegate call_GetCapsuleWithMaterialDelegate;

	public static GetDominantMaterialForTriangleMeshDelegate call_GetDominantMaterialForTriangleMeshDelegate;

	public static GetFromResourceDelegate call_GetFromResourceDelegate;

	public static GetNameDelegate call_GetNameDelegate;

	public static GetSphereDelegate call_GetSphereDelegate;

	public static GetSphereWithMaterialDelegate call_GetSphereWithMaterialDelegate;

	public static GetTriangleDelegate call_GetTriangleDelegate;

	public static InitDescriptionDelegate call_InitDescriptionDelegate;

	public static PrepareDelegate call_PrepareDelegate;

	public static ProcessPreloadQueueDelegate call_ProcessPreloadQueueDelegate;

	public static SetCapsuleDelegate call_SetCapsuleDelegate;

	public static SphereCountDelegate call_SphereCountDelegate;

	public static TransformDelegate call_TransformDelegate;

	public static TriangleCountInTriangleMeshDelegate call_TriangleCountInTriangleMeshDelegate;

	public static TriangleMeshCountDelegate call_TriangleMeshCountDelegate;

	public static UnloadDynamicBodiesDelegate call_UnloadDynamicBodiesDelegate;

	public void AddCapsule(UIntPtr shapePointer, ref CapsuleData data)
	{
		call_AddCapsuleDelegate(shapePointer, ref data);
	}

	public void AddPreloadQueueWithName(string bodyName, ref Vec3 scale)
	{
		byte[] array = null;
		if (bodyName != null)
		{
			int byteCount = _utf8.GetByteCount(bodyName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(bodyName, 0, bodyName.Length, array, 0);
			array[byteCount] = 0;
		}
		call_AddPreloadQueueWithNameDelegate(array, ref scale);
	}

	public void AddSphere(UIntPtr shapePointer, ref Vec3 origin, float radius)
	{
		call_AddSphereDelegate(shapePointer, ref origin, radius);
	}

	public int CapsuleCount(UIntPtr shapePointer)
	{
		return call_CapsuleCountDelegate(shapePointer);
	}

	public void clear(UIntPtr shapePointer)
	{
		call_clearDelegate(shapePointer);
	}

	public PhysicsShape CreateBodyCopy(UIntPtr bodyPointer)
	{
		NativeObjectPointer nativeObjectPointer = call_CreateBodyCopyDelegate(bodyPointer);
		PhysicsShape result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new PhysicsShape(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public void GetBoundingBox(UIntPtr shapePointer, out BoundingBox boundingBox)
	{
		call_GetBoundingBoxDelegate(shapePointer, out boundingBox);
	}

	public Vec3 GetBoundingBoxCenter(UIntPtr shapePointer)
	{
		return call_GetBoundingBoxCenterDelegate(shapePointer);
	}

	public void GetCapsule(UIntPtr shapePointer, ref CapsuleData data, int index)
	{
		call_GetCapsuleDelegate(shapePointer, ref data, index);
	}

	public void GetCapsuleWithMaterial(UIntPtr shapePointer, ref CapsuleData data, ref int materialIndex, int index)
	{
		call_GetCapsuleWithMaterialDelegate(shapePointer, ref data, ref materialIndex, index);
	}

	public int GetDominantMaterialForTriangleMesh(PhysicsShape shape, int meshIndex)
	{
		UIntPtr shape2 = ((shape != null) ? shape.Pointer : UIntPtr.Zero);
		return call_GetDominantMaterialForTriangleMeshDelegate(shape2, meshIndex);
	}

	public PhysicsShape GetFromResource(string bodyName, bool mayReturnNull)
	{
		byte[] array = null;
		if (bodyName != null)
		{
			int byteCount = _utf8.GetByteCount(bodyName);
			array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
			_utf8.GetBytes(bodyName, 0, bodyName.Length, array, 0);
			array[byteCount] = 0;
		}
		NativeObjectPointer nativeObjectPointer = call_GetFromResourceDelegate(array, mayReturnNull);
		PhysicsShape result = null;
		if (nativeObjectPointer.Pointer != UIntPtr.Zero)
		{
			result = new PhysicsShape(nativeObjectPointer.Pointer);
			LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
		}
		return result;
	}

	public string GetName(PhysicsShape shape)
	{
		UIntPtr shape2 = ((shape != null) ? shape.Pointer : UIntPtr.Zero);
		if (call_GetNameDelegate(shape2) != 1)
		{
			return null;
		}
		return Managed.ReturnValueFromEngine;
	}

	public void GetSphere(UIntPtr shapePointer, ref SphereData data, int sphereIndex)
	{
		call_GetSphereDelegate(shapePointer, ref data, sphereIndex);
	}

	public void GetSphereWithMaterial(UIntPtr shapePointer, ref SphereData data, ref int materialIndex, int sphereIndex)
	{
		call_GetSphereWithMaterialDelegate(shapePointer, ref data, ref materialIndex, sphereIndex);
	}

	public void GetTriangle(UIntPtr pointer, Vec3[] data, int meshIndex, int triangleIndex)
	{
		PinnedArrayData<Vec3> pinnedArrayData = new PinnedArrayData<Vec3>(data);
		IntPtr pointer2 = pinnedArrayData.Pointer;
		call_GetTriangleDelegate(pointer, pointer2, meshIndex, triangleIndex);
		pinnedArrayData.Dispose();
	}

	public void InitDescription(UIntPtr shapePointer)
	{
		call_InitDescriptionDelegate(shapePointer);
	}

	public void Prepare(UIntPtr shapePointer)
	{
		call_PrepareDelegate(shapePointer);
	}

	public void ProcessPreloadQueue()
	{
		call_ProcessPreloadQueueDelegate();
	}

	public void SetCapsule(UIntPtr shapePointer, ref CapsuleData data, int index)
	{
		call_SetCapsuleDelegate(shapePointer, ref data, index);
	}

	public int SphereCount(UIntPtr pointer)
	{
		return call_SphereCountDelegate(pointer);
	}

	public void Transform(UIntPtr shapePointer, ref MatrixFrame frame)
	{
		call_TransformDelegate(shapePointer, ref frame);
	}

	public int TriangleCountInTriangleMesh(UIntPtr pointer, int meshIndex)
	{
		return call_TriangleCountInTriangleMeshDelegate(pointer, meshIndex);
	}

	public int TriangleMeshCount(UIntPtr pointer)
	{
		return call_TriangleMeshCountDelegate(pointer);
	}

	public void UnloadDynamicBodies()
	{
		call_UnloadDynamicBodiesDelegate();
	}
}
