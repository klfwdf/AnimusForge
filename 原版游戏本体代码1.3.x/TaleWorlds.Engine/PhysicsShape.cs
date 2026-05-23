using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000007 RID: 7
	[EngineClass("rglPhysics_shape")]
	public sealed class PhysicsShape : Resource
	{
		// Token: 0x06000011 RID: 17 RVA: 0x00002351 File Offset: 0x00000551
		public static PhysicsShape GetFromResource(string bodyName, bool mayReturnNull = false)
		{
			return EngineApplicationInterface.IPhysicsShape.GetFromResource(bodyName, mayReturnNull);
		}

		// Token: 0x06000012 RID: 18 RVA: 0x0000235F File Offset: 0x0000055F
		public static void AddPreloadQueueWithName(string bodyName, Vec3 scale)
		{
			EngineApplicationInterface.IPhysicsShape.AddPreloadQueueWithName(bodyName, ref scale);
		}

		// Token: 0x06000013 RID: 19 RVA: 0x0000236E File Offset: 0x0000056E
		public static void ProcessPreloadQueue()
		{
			EngineApplicationInterface.IPhysicsShape.ProcessPreloadQueue();
		}

		// Token: 0x06000014 RID: 20 RVA: 0x0000237A File Offset: 0x0000057A
		public static void UnloadDynamicBodies()
		{
			EngineApplicationInterface.IPhysicsShape.UnloadDynamicBodies();
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002386 File Offset: 0x00000586
		internal PhysicsShape(UIntPtr bodyPointer)
			: base(bodyPointer)
		{
		}

		// Token: 0x06000016 RID: 22 RVA: 0x0000238F File Offset: 0x0000058F
		public PhysicsShape CreateCopy()
		{
			return EngineApplicationInterface.IPhysicsShape.CreateBodyCopy(base.Pointer);
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000023A1 File Offset: 0x000005A1
		public int SphereCount()
		{
			return EngineApplicationInterface.IPhysicsShape.SphereCount(base.Pointer);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000023B3 File Offset: 0x000005B3
		public void GetSphere(ref SphereData data, int index)
		{
			EngineApplicationInterface.IPhysicsShape.GetSphere(base.Pointer, ref data, index);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x000023C8 File Offset: 0x000005C8
		public void GetSphere(ref SphereData data, out PhysicsMaterial material, int index)
		{
			int index2 = -1;
			EngineApplicationInterface.IPhysicsShape.GetSphereWithMaterial(base.Pointer, ref data, ref index2, index);
			material = new PhysicsMaterial(index2);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000023F8 File Offset: 0x000005F8
		public PhysicsMaterial GetDominantMaterialForTriangleMesh(int meshIndex)
		{
			int dominantMaterialForTriangleMesh = EngineApplicationInterface.IPhysicsShape.GetDominantMaterialForTriangleMesh(this, meshIndex);
			return new PhysicsMaterial(dominantMaterialForTriangleMesh);
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002418 File Offset: 0x00000618
		public string GetName()
		{
			return EngineApplicationInterface.IPhysicsShape.GetName(this);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002425 File Offset: 0x00000625
		public int TriangleMeshCount()
		{
			return EngineApplicationInterface.IPhysicsShape.TriangleMeshCount(base.Pointer);
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002437 File Offset: 0x00000637
		public int TriangleCountInTriangleMesh(int meshIndex)
		{
			return EngineApplicationInterface.IPhysicsShape.TriangleCountInTriangleMesh(base.Pointer, meshIndex);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000244A File Offset: 0x0000064A
		public void GetTriangle(Vec3[] triangle, int meshIndex, int triangleIndex)
		{
			EngineApplicationInterface.IPhysicsShape.GetTriangle(base.Pointer, triangle, meshIndex, triangleIndex);
		}

		// Token: 0x0600001F RID: 31 RVA: 0x0000245F File Offset: 0x0000065F
		public void Prepare()
		{
			EngineApplicationInterface.IPhysicsShape.Prepare(base.Pointer);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002471 File Offset: 0x00000671
		public int CapsuleCount()
		{
			return EngineApplicationInterface.IPhysicsShape.CapsuleCount(base.Pointer);
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002483 File Offset: 0x00000683
		public void AddCapsule(CapsuleData data)
		{
			EngineApplicationInterface.IPhysicsShape.AddCapsule(base.Pointer, ref data);
		}

		// Token: 0x06000022 RID: 34 RVA: 0x00002497 File Offset: 0x00000697
		public void InitDescription()
		{
			EngineApplicationInterface.IPhysicsShape.InitDescription(base.Pointer);
		}

		// Token: 0x06000023 RID: 35 RVA: 0x000024A9 File Offset: 0x000006A9
		public void AddSphere(SphereData data)
		{
			EngineApplicationInterface.IPhysicsShape.AddSphere(base.Pointer, ref data.Origin, data.Radius);
		}

		// Token: 0x06000024 RID: 36 RVA: 0x000024C8 File Offset: 0x000006C8
		public void SetCapsule(CapsuleData data, int index)
		{
			EngineApplicationInterface.IPhysicsShape.SetCapsule(base.Pointer, ref data, index);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x000024DD File Offset: 0x000006DD
		public void GetCapsule(ref CapsuleData data, int index)
		{
			EngineApplicationInterface.IPhysicsShape.GetCapsule(base.Pointer, ref data, index);
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000024F4 File Offset: 0x000006F4
		public void GetCapsule(ref CapsuleData data, out PhysicsMaterial material, int index)
		{
			int index2 = -1;
			EngineApplicationInterface.IPhysicsShape.GetCapsuleWithMaterial(base.Pointer, ref data, ref index2, index);
			material = new PhysicsMaterial(index2);
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002523 File Offset: 0x00000723
		public void GetBoundingBox(out BoundingBox boundingBox)
		{
			EngineApplicationInterface.IPhysicsShape.GetBoundingBox(base.Pointer, out boundingBox);
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002536 File Offset: 0x00000736
		public Vec3 GetBoundingBoxCenter()
		{
			return EngineApplicationInterface.IPhysicsShape.GetBoundingBoxCenter(base.Pointer);
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002548 File Offset: 0x00000748
		public void Transform(ref MatrixFrame frame)
		{
			EngineApplicationInterface.IPhysicsShape.Transform(base.Pointer, ref frame);
		}

		// Token: 0x0600002A RID: 42 RVA: 0x0000255B File Offset: 0x0000075B
		public void Clear()
		{
			EngineApplicationInterface.IPhysicsShape.clear(base.Pointer);
		}
	}
}
