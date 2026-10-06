using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglMesh")]
public sealed class Mesh : Resource
{
	public string Name
	{
		get
		{
			if (base.IsValid)
			{
				return EngineApplicationInterface.IMesh.GetName(base.Pointer);
			}
			return string.Empty;
		}
		set
		{
			EngineApplicationInterface.IMesh.SetName(base.Pointer, value);
		}
	}

	public MBMeshCullingMode CullingMode
	{
		set
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.SetCullingMode(base.Pointer, (uint)value);
			}
		}
	}

	public float MorphTime
	{
		set
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.SetMorphTime(base.Pointer, value);
			}
		}
	}

	public uint Color
	{
		get
		{
			return EngineApplicationInterface.IMesh.GetColor(base.Pointer);
		}
		set
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.SetColor(base.Pointer, value);
			}
			else
			{
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\Mesh.cs", "Color", 331);
			}
		}
	}

	public uint Color2
	{
		get
		{
			return EngineApplicationInterface.IMesh.GetColor2(base.Pointer);
		}
		set
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.SetColor2(base.Pointer, value);
			}
			else
			{
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\Mesh.cs", "Color2", 354);
			}
		}
	}

	public BillboardType Billboard
	{
		get
		{
			return EngineApplicationInterface.IMesh.GetBillboard(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.IMesh.SetBillboard(base.Pointer, value);
		}
	}

	public VisibilityMaskFlags VisibilityMask
	{
		get
		{
			return EngineApplicationInterface.IMesh.GetVisibilityMask(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.IMesh.SetVisibilityMask(base.Pointer, value);
		}
	}

	public int EditDataFaceCornerCount => EngineApplicationInterface.IMesh.GetEditDataFaceCornerCount(base.Pointer);

	internal Mesh(UIntPtr meshPointer)
		: base(meshPointer)
	{
	}

	public static Mesh CreateMeshWithMaterial(Material material)
	{
		return EngineApplicationInterface.IMesh.CreateMeshWithMaterial(material.Pointer);
	}

	public static Mesh CreateMesh(bool editable = true)
	{
		return EngineApplicationInterface.IMesh.CreateMesh(editable);
	}

	public Mesh GetBaseMesh()
	{
		return EngineApplicationInterface.IMesh.GetBaseMesh(base.Pointer);
	}

	public static Mesh GetFromResource(string meshName)
	{
		return EngineApplicationInterface.IMesh.GetMeshFromResource(meshName);
	}

	public static Mesh GetRandomMeshWithVdecl(int inputLayout)
	{
		return EngineApplicationInterface.IMesh.GetRandomMeshWithVdecl(inputLayout);
	}

	public void SetColorAndStroke(uint color, uint strokeColor, bool drawStroke)
	{
		Color = color;
		Color2 = strokeColor;
		EngineApplicationInterface.IMesh.SetColorAndStroke(base.Pointer, drawStroke);
	}

	public void SetMeshRenderOrder(int renderOrder)
	{
		EngineApplicationInterface.IMesh.SetMeshRenderOrder(base.Pointer, renderOrder);
	}

	public bool HasTag(string str)
	{
		return EngineApplicationInterface.IMesh.HasTag(base.Pointer, str);
	}

	public Mesh CreateCopy()
	{
		return EngineApplicationInterface.IMesh.CreateMeshCopy(base.Pointer);
	}

	public void SetMaterial(string newMaterialName)
	{
		EngineApplicationInterface.IMesh.SetMaterialByName(base.Pointer, newMaterialName);
	}

	public void SetVectorArgument(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
	{
		EngineApplicationInterface.IMesh.SetVectorArgument(base.Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
	}

	public void SetVectorArgument2(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
	{
		EngineApplicationInterface.IMesh.SetVectorArgument2(base.Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
	}

	public Vec3 GetVectorArgument()
	{
		return EngineApplicationInterface.IMesh.GetVectorArgument(base.Pointer);
	}

	public Vec3 GetVectorArgument2()
	{
		return EngineApplicationInterface.IMesh.GetVectorArgument2(base.Pointer);
	}

	public void SetupAdditionalBoneBuffer(int numBones)
	{
		EngineApplicationInterface.IMesh.SetupAdditionalBoneBuffer(base.Pointer, numBones);
	}

	public void SetAdditionalBoneFrame(int boneIndex, in MatrixFrame frame)
	{
		EngineApplicationInterface.IMesh.SetAdditionalBoneFrame(base.Pointer, boneIndex, in frame);
	}

	public void SetMaterial(Material material)
	{
		EngineApplicationInterface.IMesh.SetMaterial(base.Pointer, material.Pointer);
	}

	public Material GetMaterial()
	{
		return EngineApplicationInterface.IMesh.GetMaterial(base.Pointer);
	}

	public Material GetSecondMaterial()
	{
		return EngineApplicationInterface.IMesh.GetSecondMaterial(base.Pointer);
	}

	public int AddFaceCorner(Vec3 position, Vec3 normal, Vec2 uvCoord, uint color, UIntPtr lockHandle)
	{
		if (base.IsValid)
		{
			return EngineApplicationInterface.IMesh.AddFaceCorner(base.Pointer, position, normal, uvCoord, color, lockHandle);
		}
		return -1;
	}

	public int AddFace(int patchNode0, int patchNode1, int patchNode2, UIntPtr lockHandle)
	{
		if (base.IsValid)
		{
			return EngineApplicationInterface.IMesh.AddFace(base.Pointer, patchNode0, patchNode1, patchNode2, lockHandle);
		}
		return -1;
	}

	public void ClearMesh()
	{
		if (base.IsValid)
		{
			EngineApplicationInterface.IMesh.ClearMesh(base.Pointer);
		}
	}

	public void SetColorAlpha(uint newAlpha)
	{
		if (base.IsValid)
		{
			EngineApplicationInterface.IMesh.SetColorAlpha(base.Pointer, newAlpha);
		}
	}

	public uint GetFaceCount()
	{
		if (!base.IsValid)
		{
			return 0u;
		}
		return EngineApplicationInterface.IMesh.GetFaceCount(base.Pointer);
	}

	public uint GetFaceCornerCount()
	{
		if (!base.IsValid)
		{
			return 0u;
		}
		return EngineApplicationInterface.IMesh.GetFaceCornerCount(base.Pointer);
	}

	public void ComputeNormals()
	{
		if (base.IsValid)
		{
			EngineApplicationInterface.IMesh.ComputeNormals(base.Pointer);
		}
	}

	public void ComputeTangents()
	{
		if (base.IsValid)
		{
			EngineApplicationInterface.IMesh.ComputeTangents(base.Pointer);
		}
	}

	public void AddMesh(string meshResourceName, MatrixFrame meshFrame)
	{
		if (base.IsValid)
		{
			Mesh fromResource = GetFromResource(meshResourceName);
			EngineApplicationInterface.IMesh.AddMeshToMesh(base.Pointer, fromResource.Pointer, ref meshFrame);
		}
	}

	public void AddMesh(Mesh mesh, MatrixFrame meshFrame)
	{
		if (base.IsValid)
		{
			EngineApplicationInterface.IMesh.AddMeshToMesh(base.Pointer, mesh.Pointer, ref meshFrame);
		}
	}

	public MatrixFrame GetLocalFrame()
	{
		if (base.IsValid)
		{
			MatrixFrame outFrame = default(MatrixFrame);
			EngineApplicationInterface.IMesh.GetLocalFrame(base.Pointer, ref outFrame);
			return outFrame;
		}
		return default(MatrixFrame);
	}

	public void SetLocalFrame(MatrixFrame meshFrame)
	{
		if (base.IsValid)
		{
			EngineApplicationInterface.IMesh.SetLocalFrame(base.Pointer, ref meshFrame);
		}
	}

	public void SetVisibilityMask(VisibilityMaskFlags visibilityMask)
	{
		EngineApplicationInterface.IMesh.SetVisibilityMask(base.Pointer, visibilityMask);
	}

	public void UpdateBoundingBox()
	{
		if (base.IsValid)
		{
			EngineApplicationInterface.IMesh.UpdateBoundingBox(base.Pointer);
		}
	}

	public void SetAsNotEffectedBySeason()
	{
		EngineApplicationInterface.IMesh.SetAsNotEffectedBySeason(base.Pointer);
	}

	public float GetBoundingBoxWidth()
	{
		if (!base.IsValid)
		{
			return 0f;
		}
		return EngineApplicationInterface.IMesh.GetBoundingBoxWidth(base.Pointer);
	}

	public float GetBoundingBoxHeight()
	{
		if (!base.IsValid)
		{
			return 0f;
		}
		return EngineApplicationInterface.IMesh.GetBoundingBoxHeight(base.Pointer);
	}

	public Vec3 GetBoundingBoxMin()
	{
		return EngineApplicationInterface.IMesh.GetBoundingBoxMin(base.Pointer);
	}

	public Vec3 GetBoundingBoxMax()
	{
		return EngineApplicationInterface.IMesh.GetBoundingBoxMax(base.Pointer);
	}

	public void AddTriangle(Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint color, UIntPtr lockHandle)
	{
		EngineApplicationInterface.IMesh.AddTriangle(base.Pointer, p1, p2, p3, uv1, uv2, uv3, color, lockHandle);
	}

	public void AddTriangleWithVertexColors(Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint c1, uint c2, uint c3, UIntPtr lockHandle)
	{
		EngineApplicationInterface.IMesh.AddTriangleWithVertexColors(base.Pointer, p1, p2, p3, uv1, uv2, uv3, c1, c2, c3, lockHandle);
	}

	public void HintIndicesDynamic()
	{
		EngineApplicationInterface.IMesh.HintIndicesDynamic(base.Pointer);
	}

	public void HintVerticesDynamic()
	{
		EngineApplicationInterface.IMesh.HintVerticesDynamic(base.Pointer);
	}

	public void RecomputeBoundingBox()
	{
		EngineApplicationInterface.IMesh.RecomputeBoundingBox(base.Pointer);
	}

	public void SetEditDataFaceCornerVertexColor(int index, uint color)
	{
		EngineApplicationInterface.IMesh.SetEditDataFaceCornerVertexColor(base.Pointer, index, color);
	}

	public uint GetEditDataFaceCornerVertexColor(int index)
	{
		return EngineApplicationInterface.IMesh.GetEditDataFaceCornerVertexColor(base.Pointer, index);
	}

	public void PreloadForRendering()
	{
		EngineApplicationInterface.IMesh.PreloadForRendering(base.Pointer);
	}

	public void SetContourColor(Vec3 color, bool alwaysVisible, bool maskMesh)
	{
		EngineApplicationInterface.IMesh.SetContourColor(base.Pointer, color, alwaysVisible, maskMesh);
	}

	public void DisableContour()
	{
		EngineApplicationInterface.IMesh.DisableContour(base.Pointer);
	}

	public void SetExternalBoundingBox(BoundingBox bbox)
	{
		EngineApplicationInterface.IMesh.SetExternalBoundingBox(base.Pointer, ref bbox);
	}

	public void AddEditDataUser()
	{
		EngineApplicationInterface.IMesh.AddEditDataUser(base.Pointer);
	}

	public void ReleaseEditDataUser()
	{
		EngineApplicationInterface.IMesh.ReleaseEditDataUser(base.Pointer);
	}

	public void SetEditDataPolicy(EditDataPolicy policy)
	{
		EngineApplicationInterface.IMesh.SetEditDataPolicy(base.Pointer, policy);
	}

	public UIntPtr LockEditDataWrite()
	{
		return EngineApplicationInterface.IMesh.LockEditDataWrite(base.Pointer);
	}

	public void UnlockEditDataWrite(UIntPtr handle)
	{
		EngineApplicationInterface.IMesh.UnlockEditDataWrite(base.Pointer, handle);
	}

	public void SetCustomClipPlane(Vec3 clipPlanePosition, Vec3 clipPlaneNormal, int planeIndex)
	{
		EngineApplicationInterface.IMesh.SetCustomClipPlane(base.Pointer, clipPlanePosition, clipPlaneNormal, planeIndex);
	}

	public float GetClothLinearVelocityMultiplier()
	{
		return EngineApplicationInterface.IMesh.GetClothLinearVelocityMultiplier(base.Pointer);
	}

	public bool HasCloth()
	{
		return EngineApplicationInterface.IMesh.HasCloth(base.Pointer);
	}
}
