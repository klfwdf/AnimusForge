using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[EngineClass("rglMeta_mesh")]
public sealed class MetaMesh : GameEntityComponent
{
	public bool IsValid => base.Pointer != UIntPtr.Zero;

	public int MeshCount => EngineApplicationInterface.IMetaMesh.GetMeshCount(base.Pointer);

	public MatrixFrame Frame
	{
		get
		{
			MatrixFrame outFrame = default(MatrixFrame);
			EngineApplicationInterface.IMetaMesh.GetFrame(base.Pointer, ref outFrame);
			return outFrame;
		}
		set
		{
			EngineApplicationInterface.IMetaMesh.SetFrame(base.Pointer, ref value);
		}
	}

	public Vec3 VectorUserData
	{
		get
		{
			return EngineApplicationInterface.IMetaMesh.GetVectorUserData(base.Pointer);
		}
		set
		{
			EngineApplicationInterface.IMetaMesh.SetVectorUserData(base.Pointer, ref value);
		}
	}

	internal MetaMesh(UIntPtr pointer)
		: base(pointer)
	{
	}

	public static MetaMesh CreateMetaMesh(string name = null)
	{
		return EngineApplicationInterface.IMetaMesh.CreateMetaMesh(name);
	}

	public int GetLodMaskForMeshAtIndex(int index)
	{
		return EngineApplicationInterface.IMetaMesh.GetLodMaskForMeshAtIndex(base.Pointer, index);
	}

	public int GetTotalGpuSize()
	{
		return EngineApplicationInterface.IMetaMesh.GetTotalGpuSize(base.Pointer);
	}

	public int RemoveMeshesWithTag(string tag)
	{
		return EngineApplicationInterface.IMetaMesh.RemoveMeshesWithTag(base.Pointer, tag);
	}

	public int RemoveMeshesWithoutTag(string tag)
	{
		return EngineApplicationInterface.IMetaMesh.RemoveMeshesWithoutTag(base.Pointer, tag);
	}

	public int GetMeshCountWithTag(string tag)
	{
		return EngineApplicationInterface.IMetaMesh.GetMeshCountWithTag(base.Pointer, tag);
	}

	public bool HasVertexBufferOrEditDataOrPackageItem()
	{
		return EngineApplicationInterface.IMetaMesh.HasVertexBufferOrEditDataOrPackageItem(base.Pointer);
	}

	public bool HasAnyGeneratedLods()
	{
		return EngineApplicationInterface.IMetaMesh.HasAnyGeneratedLods(base.Pointer);
	}

	public bool HasAnyLods()
	{
		return EngineApplicationInterface.IMetaMesh.HasAnyLods(base.Pointer);
	}

	public static MetaMesh GetCopy(string metaMeshName, bool showErrors = true, bool mayReturnNull = false)
	{
		return EngineApplicationInterface.IMetaMesh.CreateCopyFromName(metaMeshName, showErrors, mayReturnNull);
	}

	public void CopyTo(MetaMesh res, bool copyMeshes = true)
	{
		EngineApplicationInterface.IMetaMesh.CopyTo(base.Pointer, res.Pointer, copyMeshes);
	}

	public void ClearMeshesForOtherLods(int lodToKeep)
	{
		EngineApplicationInterface.IMetaMesh.ClearMeshesForOtherLods(base.Pointer, lodToKeep);
	}

	public void ClearMeshesForLod(int lodToClear)
	{
		EngineApplicationInterface.IMetaMesh.ClearMeshesForLod(base.Pointer, lodToClear);
	}

	public void ClearMeshesForLowerLods(int lodToClear)
	{
		EngineApplicationInterface.IMetaMesh.ClearMeshesForLowerLods(base.Pointer, lodToClear);
	}

	public void ClearMeshes()
	{
		EngineApplicationInterface.IMetaMesh.ClearMeshes(base.Pointer);
	}

	public void SetNumLods(int lodToClear)
	{
		EngineApplicationInterface.IMetaMesh.SetNumLods(base.Pointer, lodToClear);
	}

	public static void CheckMetaMeshExistence(string metaMeshName, int lod_count_check)
	{
		EngineApplicationInterface.IMetaMesh.CheckMetaMeshExistence(metaMeshName, lod_count_check);
	}

	public static MetaMesh GetMorphedCopy(string metaMeshName, float morphTarget, bool showErrors)
	{
		return EngineApplicationInterface.IMetaMesh.GetMorphedCopy(metaMeshName, morphTarget, showErrors);
	}

	public MetaMesh CreateCopy()
	{
		return EngineApplicationInterface.IMetaMesh.CreateCopy(base.Pointer);
	}

	public void AddMesh(Mesh mesh)
	{
		EngineApplicationInterface.IMetaMesh.AddMesh(base.Pointer, mesh.Pointer, 0u);
	}

	public void AddMesh(Mesh mesh, uint lodLevel)
	{
		EngineApplicationInterface.IMetaMesh.AddMesh(base.Pointer, mesh.Pointer, lodLevel);
	}

	public void AddMetaMesh(MetaMesh metaMesh)
	{
		EngineApplicationInterface.IMetaMesh.AddMetaMesh(base.Pointer, metaMesh.Pointer);
	}

	public void SetCullMode(MBMeshCullingMode cullMode)
	{
		EngineApplicationInterface.IMetaMesh.SetCullMode(base.Pointer, cullMode);
	}

	public void AddMaterialShaderFlag(string materialShaderFlag)
	{
		for (int i = 0; i < MeshCount; i++)
		{
			Mesh meshAtIndex = GetMeshAtIndex(i);
			Material material = meshAtIndex.GetMaterial();
			material = material.CreateCopy();
			material.AddMaterialShaderFlag(materialShaderFlag, showErrors: false);
			meshAtIndex.SetMaterial(material);
		}
	}

	public void MergeMultiMeshes(MetaMesh metaMesh)
	{
		EngineApplicationInterface.IMetaMesh.MergeMultiMeshes(base.Pointer, metaMesh.Pointer);
	}

	public void AssignClothBodyFrom(MetaMesh metaMesh)
	{
		EngineApplicationInterface.IMetaMesh.AssignClothBodyFrom(base.Pointer, metaMesh.Pointer);
	}

	public void BatchMultiMeshes(MetaMesh metaMesh)
	{
		EngineApplicationInterface.IMetaMesh.BatchMultiMeshes(base.Pointer, metaMesh.Pointer);
	}

	public bool HasClothData()
	{
		return EngineApplicationInterface.IMetaMesh.HasClothData(base.Pointer);
	}

	public void BatchMultiMeshesMultiple(List<MetaMesh> metaMeshes)
	{
		UIntPtr[] array = new UIntPtr[metaMeshes.Count];
		for (int i = 0; i < metaMeshes.Count; i++)
		{
			array[i] = metaMeshes[i].Pointer;
		}
		EngineApplicationInterface.IMetaMesh.BatchMultiMeshesMultiple(base.Pointer, array, metaMeshes.Count);
	}

	public void ClearEditData()
	{
		EngineApplicationInterface.IMetaMesh.ClearEditData(base.Pointer);
	}

	public Mesh GetMeshAtIndex(int meshIndex)
	{
		return EngineApplicationInterface.IMetaMesh.GetMeshAtIndex(base.Pointer, meshIndex);
	}

	public Mesh GetFirstMeshWithTag(string tag)
	{
		for (int i = 0; i < MeshCount; i++)
		{
			Mesh meshAtIndex = GetMeshAtIndex(i);
			if (meshAtIndex.HasTag(tag))
			{
				return meshAtIndex;
			}
		}
		return null;
	}

	private void Release()
	{
		EngineApplicationInterface.IMetaMesh.Release(base.Pointer);
	}

	public uint GetFactor1()
	{
		return EngineApplicationInterface.IMetaMesh.GetFactor1(base.Pointer);
	}

	public void SetGlossMultiplier(float value)
	{
		EngineApplicationInterface.IMetaMesh.SetGlossMultiplier(base.Pointer, value);
	}

	public uint GetFactor2()
	{
		return EngineApplicationInterface.IMetaMesh.GetFactor2(base.Pointer);
	}

	public void SetFactor1Linear(uint linearFactorColor1)
	{
		EngineApplicationInterface.IMetaMesh.SetFactor1Linear(base.Pointer, linearFactorColor1);
	}

	public void SetFactor2Linear(uint linearFactorColor2)
	{
		EngineApplicationInterface.IMetaMesh.SetFactor2Linear(base.Pointer, linearFactorColor2);
	}

	public void SetFactor1(uint factorColor1)
	{
		EngineApplicationInterface.IMetaMesh.SetFactor1(base.Pointer, factorColor1);
	}

	public void SetFactor2(uint factorColor2)
	{
		EngineApplicationInterface.IMetaMesh.SetFactor2(base.Pointer, factorColor2);
	}

	public void SetVectorArgument(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
	{
		EngineApplicationInterface.IMetaMesh.SetVectorArgument(base.Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
	}

	public void SetVectorArgument2(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
	{
		EngineApplicationInterface.IMetaMesh.SetVectorArgument2(base.Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
	}

	public Vec3 GetVectorArgument2()
	{
		return EngineApplicationInterface.IMetaMesh.GetVectorArgument2(base.Pointer);
	}

	public void SetMaterial(Material material)
	{
		EngineApplicationInterface.IMetaMesh.SetMaterial(base.Pointer, material.Pointer);
	}

	public void SetShaderToMaterial(string shaderName)
	{
		EngineApplicationInterface.IMetaMesh.SetShaderToMaterial(base.Pointer, shaderName);
	}

	public void SetLodBias(int lodBias)
	{
		EngineApplicationInterface.IMetaMesh.SetLodBias(base.Pointer, lodBias);
	}

	public void SetBillboarding(BillboardType billboard)
	{
		EngineApplicationInterface.IMetaMesh.SetBillboarding(base.Pointer, billboard);
	}

	public void UseHeadBoneFaceGenScaling(Skeleton skeleton, sbyte headLookDirectionBoneIndex, MatrixFrame frame)
	{
		EngineApplicationInterface.IMetaMesh.UseHeadBoneFaceGenScaling(base.Pointer, skeleton.Pointer, headLookDirectionBoneIndex, ref frame);
	}

	public void DrawTextWithDefaultFont(string text, Vec2 textPositionMin, Vec2 textPositionMax, Vec2 size, uint color, TextFlags flags)
	{
		EngineApplicationInterface.IMetaMesh.DrawTextWithDefaultFont(base.Pointer, text, textPositionMin, textPositionMax, size, color, flags);
	}

	public void PreloadForRendering()
	{
		EngineApplicationInterface.IMetaMesh.PreloadForRendering(base.Pointer);
	}

	public int CheckResources()
	{
		return EngineApplicationInterface.IMetaMesh.CheckResources(base.Pointer);
	}

	public void PreloadShaders(bool useTableau, bool useTeamColor)
	{
		EngineApplicationInterface.IMetaMesh.PreloadShaders(base.Pointer, useTableau, useTeamColor);
	}

	public void RecomputeBoundingBox(bool recomputeMeshes)
	{
		EngineApplicationInterface.IMetaMesh.RecomputeBoundingBox(base.Pointer, recomputeMeshes);
	}

	public void AddEditDataUser()
	{
		EngineApplicationInterface.IMetaMesh.AddEditDataUser(base.Pointer);
	}

	public void ReleaseEditDataUser()
	{
		EngineApplicationInterface.IMetaMesh.ReleaseEditDataUser(base.Pointer);
	}

	public void SetEditDataPolicy(EditDataPolicy policy)
	{
		EngineApplicationInterface.IMetaMesh.SetEditDataPolicy(base.Pointer, policy);
	}

	public MatrixFrame Fit()
	{
		MatrixFrame identity = MatrixFrame.Identity;
		Vec3 vec = new Vec3(1000000f, 1000000f, 1000000f);
		Vec3 vec2 = new Vec3(-1000000f, -1000000f, -1000000f);
		for (int i = 0; i != MeshCount; i++)
		{
			Vec3 boundingBoxMin = GetMeshAtIndex(i).GetBoundingBoxMin();
			Vec3 boundingBoxMax = GetMeshAtIndex(i).GetBoundingBoxMax();
			vec = Vec3.Vec3Min(vec, boundingBoxMin);
			vec2 = Vec3.Vec3Max(vec2, boundingBoxMax);
		}
		Vec3 vec3 = (vec + vec2) * 0.5f;
		float num = TaleWorlds.Library.MathF.Max(vec2.x - vec.x, vec2.y - vec.y);
		float num2 = 0.95f / num;
		identity.origin -= vec3 * num2;
		identity.rotation.ApplyScaleLocal(num2);
		return identity;
	}

	public BoundingBox GetBoundingBox()
	{
		BoundingBox outBoundingBox = default(BoundingBox);
		EngineApplicationInterface.IMetaMesh.GetBoundingBox(base.Pointer, ref outBoundingBox);
		return outBoundingBox;
	}

	public VisibilityMaskFlags GetVisibilityMask()
	{
		return EngineApplicationInterface.IMetaMesh.GetVisibilityMask(base.Pointer);
	}

	public void SetVisibilityMask(VisibilityMaskFlags visibilityMask)
	{
		EngineApplicationInterface.IMetaMesh.SetVisibilityMask(base.Pointer, visibilityMask);
	}

	public string GetName()
	{
		return EngineApplicationInterface.IMetaMesh.GetName(base.Pointer);
	}

	public static void GetAllMultiMeshes(ref List<MetaMesh> multiMeshList)
	{
		int multiMeshCount = EngineApplicationInterface.IMetaMesh.GetMultiMeshCount();
		UIntPtr[] array = new UIntPtr[multiMeshCount];
		EngineApplicationInterface.IMetaMesh.GetAllMultiMeshes(array);
		for (int i = 0; i < multiMeshCount; i++)
		{
			multiMeshList.Add(new MetaMesh(array[i]));
		}
	}

	public static MetaMesh GetMultiMesh(string name)
	{
		return EngineApplicationInterface.IMetaMesh.GetMultiMesh(name);
	}

	public void SetContourState(bool alwaysVisible)
	{
		EngineApplicationInterface.IMetaMesh.SetContourState(base.Pointer, alwaysVisible);
	}

	public void SetContourColor(uint color)
	{
		EngineApplicationInterface.IMetaMesh.SetContourColor(base.Pointer, color);
	}

	public void SetMaterialToSubMeshesWithTag(Material bodyMaterial, string tag)
	{
		EngineApplicationInterface.IMetaMesh.SetMaterialToSubMeshesWithTag(base.Pointer, bodyMaterial.Pointer, tag);
	}

	public void SetFactorColorToSubMeshesWithTag(uint color, string tag)
	{
		EngineApplicationInterface.IMetaMesh.SetFactorColorToSubMeshesWithTag(base.Pointer, color, tag);
	}
}
