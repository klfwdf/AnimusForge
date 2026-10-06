using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public class MeshBuilder
{
	[EngineStruct("rglMeshBuilder_face_corner", false, null)]
	public struct FaceCorner
	{
		public int vertexIndex;

		public Vec2 uvCoord;

		public Vec3 normal;

		public uint color;
	}

	[EngineStruct("rglMeshBuilder_face", false, null)]
	public struct Face
	{
		public int fc0;

		public int fc1;

		public int fc2;
	}

	private List<Vec3> vertices;

	private List<FaceCorner> faceCorners;

	private List<Face> faces;

	public MeshBuilder()
	{
		vertices = new List<Vec3>();
		faceCorners = new List<FaceCorner>();
		faces = new List<Face>();
	}

	public int AddFaceCorner(Vec3 position, Vec3 normal, Vec2 uvCoord, uint color)
	{
		vertices.Add(new Vec3(position));
		FaceCorner item = default(FaceCorner);
		item.vertexIndex = vertices.Count - 1;
		item.color = color;
		item.uvCoord = uvCoord;
		item.normal = normal;
		faceCorners.Add(item);
		return faceCorners.Count - 1;
	}

	public int AddFace(int patchNode0, int patchNode1, int patchNode2)
	{
		Face item = default(Face);
		item.fc0 = patchNode0;
		item.fc1 = patchNode1;
		item.fc2 = patchNode2;
		faces.Add(item);
		return faces.Count - 1;
	}

	public void Clear()
	{
		vertices.Clear();
		faceCorners.Clear();
		faces.Clear();
	}

	public Mesh Finalize()
	{
		Vec3[] array = vertices.ToArray();
		FaceCorner[] array2 = faceCorners.ToArray();
		Face[] array3 = faces.ToArray();
		Mesh result = EngineApplicationInterface.IMeshBuilder.FinalizeMeshBuilder(vertices.Count, array, faceCorners.Count, array2, faces.Count, array3);
		vertices.Clear();
		faceCorners.Clear();
		faces.Clear();
		return result;
	}

	public static Mesh CreateUnitMesh()
	{
		Mesh mesh = Mesh.CreateMeshWithMaterial(Material.GetDefaultMaterial());
		Vec3 position = new Vec3(0f, -1f);
		Vec3 position2 = new Vec3(1f, -1f);
		Vec3 position3 = new Vec3(1f);
		Vec3 position4 = new Vec3(0f, 0f, 0f, -1f);
		Vec3 normal = new Vec3(0f, 0f, 1f);
		Vec2 uvCoord = new Vec2(0f, 0f);
		Vec2 uvCoord2 = new Vec2(1f, 0f);
		Vec2 uvCoord3 = new Vec2(1f, 1f);
		Vec2 uvCoord4 = new Vec2(0f, 1f);
		UIntPtr uIntPtr = mesh.LockEditDataWrite();
		int num = mesh.AddFaceCorner(position, normal, uvCoord, uint.MaxValue, uIntPtr);
		int patchNode = mesh.AddFaceCorner(position2, normal, uvCoord2, uint.MaxValue, uIntPtr);
		int num2 = mesh.AddFaceCorner(position3, normal, uvCoord3, uint.MaxValue, uIntPtr);
		int patchNode2 = mesh.AddFaceCorner(position4, normal, uvCoord4, uint.MaxValue, uIntPtr);
		mesh.AddFace(num, patchNode, num2, uIntPtr);
		mesh.AddFace(num2, patchNode2, num, uIntPtr);
		mesh.UpdateBoundingBox();
		mesh.UnlockEditDataWrite(uIntPtr);
		return mesh;
	}

	public static Mesh CreateTilingWindowMesh(string baseMeshName, Vec2 meshSizeMin, Vec2 meshSizeMax, Vec2 borderThickness, Vec2 bgBorderThickness)
	{
		return EngineApplicationInterface.IMeshBuilder.CreateTilingWindowMesh(baseMeshName, ref meshSizeMin, ref meshSizeMax, ref borderThickness, ref bgBorderThickness);
	}

	public static Mesh CreateTilingButtonMesh(string baseMeshName, Vec2 meshSizeMin, Vec2 meshSizeMax, Vec2 borderThickness)
	{
		return EngineApplicationInterface.IMeshBuilder.CreateTilingButtonMesh(baseMeshName, ref meshSizeMin, ref meshSizeMax, ref borderThickness);
	}
}
