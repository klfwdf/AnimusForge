using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000069 RID: 105
	public class MeshBuilder
	{
		// Token: 0x060009D8 RID: 2520 RVA: 0x00009AB5 File Offset: 0x00007CB5
		public MeshBuilder()
		{
			this.vertices = new List<Vec3>();
			this.faceCorners = new List<MeshBuilder.FaceCorner>();
			this.faces = new List<MeshBuilder.Face>();
		}

		// Token: 0x060009D9 RID: 2521 RVA: 0x00009AE0 File Offset: 0x00007CE0
		public int AddFaceCorner(Vec3 position, Vec3 normal, Vec2 uvCoord, uint color)
		{
			this.vertices.Add(new Vec3(position, -1f));
			MeshBuilder.FaceCorner item;
			item.vertexIndex = this.vertices.Count - 1;
			item.color = color;
			item.uvCoord = uvCoord;
			item.normal = normal;
			this.faceCorners.Add(item);
			return this.faceCorners.Count - 1;
		}

		// Token: 0x060009DA RID: 2522 RVA: 0x00009B4C File Offset: 0x00007D4C
		public int AddFace(int patchNode0, int patchNode1, int patchNode2)
		{
			MeshBuilder.Face item;
			item.fc0 = patchNode0;
			item.fc1 = patchNode1;
			item.fc2 = patchNode2;
			this.faces.Add(item);
			return this.faces.Count - 1;
		}

		// Token: 0x060009DB RID: 2523 RVA: 0x00009B8A File Offset: 0x00007D8A
		public void Clear()
		{
			this.vertices.Clear();
			this.faceCorners.Clear();
			this.faces.Clear();
		}

		// Token: 0x060009DC RID: 2524 RVA: 0x00009BB0 File Offset: 0x00007DB0
		public new Mesh Finalize()
		{
			Vec3[] array = this.vertices.ToArray();
			MeshBuilder.FaceCorner[] array2 = this.faceCorners.ToArray();
			MeshBuilder.Face[] array3 = this.faces.ToArray();
			Mesh result = EngineApplicationInterface.IMeshBuilder.FinalizeMeshBuilder(this.vertices.Count, array, this.faceCorners.Count, array2, this.faces.Count, array3);
			this.vertices.Clear();
			this.faceCorners.Clear();
			this.faces.Clear();
			return result;
		}

		// Token: 0x060009DD RID: 2525 RVA: 0x00009C30 File Offset: 0x00007E30
		public static Mesh CreateUnitMesh()
		{
			Mesh mesh = Mesh.CreateMeshWithMaterial(Material.GetDefaultMaterial());
			Vec3 position = new Vec3(0f, -1f, 0f, -1f);
			Vec3 position2 = new Vec3(1f, -1f, 0f, -1f);
			Vec3 position3 = new Vec3(1f, 0f, 0f, -1f);
			Vec3 position4 = new Vec3(0f, 0f, 0f, -1f);
			Vec3 normal = new Vec3(0f, 0f, 1f, -1f);
			Vec2 uvCoord = new Vec2(0f, 0f);
			Vec2 uvCoord2 = new Vec2(1f, 0f);
			Vec2 uvCoord3 = new Vec2(1f, 1f);
			Vec2 uvCoord4 = new Vec2(0f, 1f);
			UIntPtr uintPtr = mesh.LockEditDataWrite();
			int num = mesh.AddFaceCorner(position, normal, uvCoord, uint.MaxValue, uintPtr);
			int patchNode = mesh.AddFaceCorner(position2, normal, uvCoord2, uint.MaxValue, uintPtr);
			int num2 = mesh.AddFaceCorner(position3, normal, uvCoord3, uint.MaxValue, uintPtr);
			int patchNode2 = mesh.AddFaceCorner(position4, normal, uvCoord4, uint.MaxValue, uintPtr);
			mesh.AddFace(num, patchNode, num2, uintPtr);
			mesh.AddFace(num2, patchNode2, num, uintPtr);
			mesh.UpdateBoundingBox();
			mesh.UnlockEditDataWrite(uintPtr);
			return mesh;
		}

		// Token: 0x060009DE RID: 2526 RVA: 0x00009D86 File Offset: 0x00007F86
		public static Mesh CreateTilingWindowMesh(string baseMeshName, Vec2 meshSizeMin, Vec2 meshSizeMax, Vec2 borderThickness, Vec2 bgBorderThickness)
		{
			return EngineApplicationInterface.IMeshBuilder.CreateTilingWindowMesh(baseMeshName, ref meshSizeMin, ref meshSizeMax, ref borderThickness, ref bgBorderThickness);
		}

		// Token: 0x060009DF RID: 2527 RVA: 0x00009D9B File Offset: 0x00007F9B
		public static Mesh CreateTilingButtonMesh(string baseMeshName, Vec2 meshSizeMin, Vec2 meshSizeMax, Vec2 borderThickness)
		{
			return EngineApplicationInterface.IMeshBuilder.CreateTilingButtonMesh(baseMeshName, ref meshSizeMin, ref meshSizeMax, ref borderThickness);
		}

		// Token: 0x04000146 RID: 326
		private List<Vec3> vertices;

		// Token: 0x04000147 RID: 327
		private List<MeshBuilder.FaceCorner> faceCorners;

		// Token: 0x04000148 RID: 328
		private List<MeshBuilder.Face> faces;

		// Token: 0x020000C9 RID: 201
		[EngineStruct("rglMeshBuilder_face_corner", false, null)]
		public struct FaceCorner
		{
			// Token: 0x04000427 RID: 1063
			public int vertexIndex;

			// Token: 0x04000428 RID: 1064
			public Vec2 uvCoord;

			// Token: 0x04000429 RID: 1065
			public Vec3 normal;

			// Token: 0x0400042A RID: 1066
			public uint color;
		}

		// Token: 0x020000CA RID: 202
		[EngineStruct("rglMeshBuilder_face", false, null)]
		public struct Face
		{
			// Token: 0x0400042B RID: 1067
			public int fc0;

			// Token: 0x0400042C RID: 1068
			public int fc1;

			// Token: 0x0400042D RID: 1069
			public int fc2;
		}
	}
}
