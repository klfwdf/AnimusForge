using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IPhysicsShape
{
	[EngineMethod("get_from_resource", false, null, false)]
	PhysicsShape GetFromResource(string bodyName, bool mayReturnNull);

	[EngineMethod("add_preload_queue_with_name", false, null, false)]
	void AddPreloadQueueWithName(string bodyName, ref Vec3 scale);

	[EngineMethod("process_preload_queue", false, null, false)]
	void ProcessPreloadQueue();

	[EngineMethod("unload_dynamic_bodies", false, null, false)]
	void UnloadDynamicBodies();

	[EngineMethod("create_body_copy", false, null, false)]
	PhysicsShape CreateBodyCopy(UIntPtr bodyPointer);

	[EngineMethod("get_name", false, null, false)]
	string GetName(PhysicsShape shape);

	[EngineMethod("triangle_mesh_count", false, null, false)]
	int TriangleMeshCount(UIntPtr pointer);

	[EngineMethod("triangle_count_in_triangle_mesh", false, null, false)]
	int TriangleCountInTriangleMesh(UIntPtr pointer, int meshIndex);

	[EngineMethod("get_dominant_material_index_for_mesh_at_index", false, null, false)]
	int GetDominantMaterialForTriangleMesh(PhysicsShape shape, int meshIndex);

	[EngineMethod("get_triangle", false, null, false)]
	void GetTriangle(UIntPtr pointer, Vec3[] data, int meshIndex, int triangleIndex);

	[EngineMethod("sphere_count", false, null, false)]
	int SphereCount(UIntPtr pointer);

	[EngineMethod("get_sphere", false, null, false)]
	void GetSphere(UIntPtr shapePointer, ref SphereData data, int sphereIndex);

	[EngineMethod("get_sphere_with_material", false, null, false)]
	void GetSphereWithMaterial(UIntPtr shapePointer, ref SphereData data, ref int materialIndex, int sphereIndex);

	[EngineMethod("prepare", false, null, false)]
	void Prepare(UIntPtr shapePointer);

	[EngineMethod("capsule_count", false, null, false)]
	int CapsuleCount(UIntPtr shapePointer);

	[EngineMethod("add_capsule", false, null, false)]
	void AddCapsule(UIntPtr shapePointer, ref CapsuleData data);

	[EngineMethod("init_description", false, null, false)]
	void InitDescription(UIntPtr shapePointer);

	[EngineMethod("add_sphere", false, null, false)]
	void AddSphere(UIntPtr shapePointer, ref Vec3 origin, float radius);

	[EngineMethod("set_capsule", false, null, false)]
	void SetCapsule(UIntPtr shapePointer, ref CapsuleData data, int index);

	[EngineMethod("get_capsule", false, null, false)]
	void GetCapsule(UIntPtr shapePointer, ref CapsuleData data, int index);

	[EngineMethod("get_capsule_with_material", false, null, false)]
	void GetCapsuleWithMaterial(UIntPtr shapePointer, ref CapsuleData data, ref int materialIndex, int index);

	[EngineMethod("clear", false, null, false)]
	void clear(UIntPtr shapePointer);

	[EngineMethod("transform", false, null, false)]
	void Transform(UIntPtr shapePointer, ref MatrixFrame frame);

	[EngineMethod("get_bounding_box", false, null, false)]
	void GetBoundingBox(UIntPtr shapePointer, out BoundingBox boundingBox);

	[EngineMethod("get_bounding_box_center", false, null, false)]
	Vec3 GetBoundingBoxCenter(UIntPtr shapePointer);
}
