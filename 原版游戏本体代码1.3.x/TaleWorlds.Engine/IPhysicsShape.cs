using System;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000022 RID: 34
	[ApplicationInterfaceBase]
	internal interface IPhysicsShape
	{
		// Token: 0x060001E8 RID: 488
		[EngineMethod("get_from_resource", false, null, false)]
		PhysicsShape GetFromResource(string bodyName, bool mayReturnNull);

		// Token: 0x060001E9 RID: 489
		[EngineMethod("add_preload_queue_with_name", false, null, false)]
		void AddPreloadQueueWithName(string bodyName, ref Vec3 scale);

		// Token: 0x060001EA RID: 490
		[EngineMethod("process_preload_queue", false, null, false)]
		void ProcessPreloadQueue();

		// Token: 0x060001EB RID: 491
		[EngineMethod("unload_dynamic_bodies", false, null, false)]
		void UnloadDynamicBodies();

		// Token: 0x060001EC RID: 492
		[EngineMethod("create_body_copy", false, null, false)]
		PhysicsShape CreateBodyCopy(UIntPtr bodyPointer);

		// Token: 0x060001ED RID: 493
		[EngineMethod("get_name", false, null, false)]
		string GetName(PhysicsShape shape);

		// Token: 0x060001EE RID: 494
		[EngineMethod("triangle_mesh_count", false, null, false)]
		int TriangleMeshCount(UIntPtr pointer);

		// Token: 0x060001EF RID: 495
		[EngineMethod("triangle_count_in_triangle_mesh", false, null, false)]
		int TriangleCountInTriangleMesh(UIntPtr pointer, int meshIndex);

		// Token: 0x060001F0 RID: 496
		[EngineMethod("get_dominant_material_index_for_mesh_at_index", false, null, false)]
		int GetDominantMaterialForTriangleMesh(PhysicsShape shape, int meshIndex);

		// Token: 0x060001F1 RID: 497
		[EngineMethod("get_triangle", false, null, false)]
		void GetTriangle(UIntPtr pointer, Vec3[] data, int meshIndex, int triangleIndex);

		// Token: 0x060001F2 RID: 498
		[EngineMethod("sphere_count", false, null, false)]
		int SphereCount(UIntPtr pointer);

		// Token: 0x060001F3 RID: 499
		[EngineMethod("get_sphere", false, null, false)]
		void GetSphere(UIntPtr shapePointer, ref SphereData data, int sphereIndex);

		// Token: 0x060001F4 RID: 500
		[EngineMethod("get_sphere_with_material", false, null, false)]
		void GetSphereWithMaterial(UIntPtr shapePointer, ref SphereData data, ref int materialIndex, int sphereIndex);

		// Token: 0x060001F5 RID: 501
		[EngineMethod("prepare", false, null, false)]
		void Prepare(UIntPtr shapePointer);

		// Token: 0x060001F6 RID: 502
		[EngineMethod("capsule_count", false, null, false)]
		int CapsuleCount(UIntPtr shapePointer);

		// Token: 0x060001F7 RID: 503
		[EngineMethod("add_capsule", false, null, false)]
		void AddCapsule(UIntPtr shapePointer, ref CapsuleData data);

		// Token: 0x060001F8 RID: 504
		[EngineMethod("init_description", false, null, false)]
		void InitDescription(UIntPtr shapePointer);

		// Token: 0x060001F9 RID: 505
		[EngineMethod("add_sphere", false, null, false)]
		void AddSphere(UIntPtr shapePointer, ref Vec3 origin, float radius);

		// Token: 0x060001FA RID: 506
		[EngineMethod("set_capsule", false, null, false)]
		void SetCapsule(UIntPtr shapePointer, ref CapsuleData data, int index);

		// Token: 0x060001FB RID: 507
		[EngineMethod("get_capsule", false, null, false)]
		void GetCapsule(UIntPtr shapePointer, ref CapsuleData data, int index);

		// Token: 0x060001FC RID: 508
		[EngineMethod("get_capsule_with_material", false, null, false)]
		void GetCapsuleWithMaterial(UIntPtr shapePointer, ref CapsuleData data, ref int materialIndex, int index);

		// Token: 0x060001FD RID: 509
		[EngineMethod("clear", false, null, false)]
		void clear(UIntPtr shapePointer);

		// Token: 0x060001FE RID: 510
		[EngineMethod("transform", false, null, false)]
		void Transform(UIntPtr shapePointer, ref MatrixFrame frame);

		// Token: 0x060001FF RID: 511
		[EngineMethod("get_bounding_box", false, null, false)]
		void GetBoundingBox(UIntPtr shapePointer, out BoundingBox boundingBox);

		// Token: 0x06000200 RID: 512
		[EngineMethod("get_bounding_box_center", false, null, false)]
		Vec3 GetBoundingBoxCenter(UIntPtr shapePointer);
	}
}
