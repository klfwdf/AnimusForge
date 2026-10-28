using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglPhysics_shape")]
public sealed class PhysicsShape : Resource
{
	public static PhysicsShape GetFromResource(string bodyName, bool mayReturnNull = false)
	{
		return EngineApplicationInterface.IPhysicsShape.GetFromResource(bodyName, mayReturnNull);
	}

	public static void AddPreloadQueueWithName(string bodyName, Vec3 scale)
	{
		EngineApplicationInterface.IPhysicsShape.AddPreloadQueueWithName(bodyName, ref scale);
	}

	public static void ProcessPreloadQueue()
	{
		EngineApplicationInterface.IPhysicsShape.ProcessPreloadQueue();
	}

	public static void UnloadDynamicBodies()
	{
		EngineApplicationInterface.IPhysicsShape.UnloadDynamicBodies();
	}

	internal PhysicsShape(UIntPtr bodyPointer)
		: base(bodyPointer)
	{
	}

	public PhysicsShape CreateCopy()
	{
		return EngineApplicationInterface.IPhysicsShape.CreateBodyCopy(base.Pointer);
	}

	public int SphereCount()
	{
		return EngineApplicationInterface.IPhysicsShape.SphereCount(base.Pointer);
	}

	public void GetSphere(ref SphereData data, int index)
	{
		EngineApplicationInterface.IPhysicsShape.GetSphere(base.Pointer, ref data, index);
	}

	public void GetSphere(ref SphereData data, out PhysicsMaterial material, int index)
	{
		int materialIndex = -1;
		EngineApplicationInterface.IPhysicsShape.GetSphereWithMaterial(base.Pointer, ref data, ref materialIndex, index);
		material = new PhysicsMaterial(materialIndex);
	}

	public PhysicsMaterial GetDominantMaterialForTriangleMesh(int meshIndex)
	{
		int dominantMaterialForTriangleMesh = EngineApplicationInterface.IPhysicsShape.GetDominantMaterialForTriangleMesh(this, meshIndex);
		return new PhysicsMaterial(dominantMaterialForTriangleMesh);
	}

	public string GetName()
	{
		return EngineApplicationInterface.IPhysicsShape.GetName(this);
	}

	public int TriangleMeshCount()
	{
		return EngineApplicationInterface.IPhysicsShape.TriangleMeshCount(base.Pointer);
	}

	public int TriangleCountInTriangleMesh(int meshIndex)
	{
		return EngineApplicationInterface.IPhysicsShape.TriangleCountInTriangleMesh(base.Pointer, meshIndex);
	}

	public void GetTriangle(Vec3[] triangle, int meshIndex, int triangleIndex)
	{
		EngineApplicationInterface.IPhysicsShape.GetTriangle(base.Pointer, triangle, meshIndex, triangleIndex);
	}

	public void Prepare()
	{
		EngineApplicationInterface.IPhysicsShape.Prepare(base.Pointer);
	}

	public int CapsuleCount()
	{
		return EngineApplicationInterface.IPhysicsShape.CapsuleCount(base.Pointer);
	}

	public void AddCapsule(CapsuleData data)
	{
		EngineApplicationInterface.IPhysicsShape.AddCapsule(base.Pointer, ref data);
	}

	public void InitDescription()
	{
		EngineApplicationInterface.IPhysicsShape.InitDescription(base.Pointer);
	}

	public void AddSphere(SphereData data)
	{
		EngineApplicationInterface.IPhysicsShape.AddSphere(base.Pointer, ref data.Origin, data.Radius);
	}

	public void SetCapsule(CapsuleData data, int index)
	{
		EngineApplicationInterface.IPhysicsShape.SetCapsule(base.Pointer, ref data, index);
	}

	public void GetCapsule(ref CapsuleData data, int index)
	{
		EngineApplicationInterface.IPhysicsShape.GetCapsule(base.Pointer, ref data, index);
	}

	public void GetCapsule(ref CapsuleData data, out PhysicsMaterial material, int index)
	{
		int materialIndex = -1;
		EngineApplicationInterface.IPhysicsShape.GetCapsuleWithMaterial(base.Pointer, ref data, ref materialIndex, index);
		material = new PhysicsMaterial(materialIndex);
	}

	public void GetBoundingBox(out BoundingBox boundingBox)
	{
		EngineApplicationInterface.IPhysicsShape.GetBoundingBox(base.Pointer, out boundingBox);
	}

	public Vec3 GetBoundingBoxCenter()
	{
		return EngineApplicationInterface.IPhysicsShape.GetBoundingBoxCenter(base.Pointer);
	}

	public void Transform(ref MatrixFrame frame)
	{
		EngineApplicationInterface.IPhysicsShape.Transform(base.Pointer, ref frame);
	}

	public void Clear()
	{
		EngineApplicationInterface.IPhysicsShape.clear(base.Pointer);
	}
}
