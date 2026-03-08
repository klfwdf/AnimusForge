using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000019 RID: 25
	internal class ScriptingInterfaceOfIMesh : IMesh
	{
		// Token: 0x0600030C RID: 780 RVA: 0x00013822 File Offset: 0x00011A22
		public void AddEditDataUser(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_AddEditDataUserDelegate(meshPointer);
		}

		// Token: 0x0600030D RID: 781 RVA: 0x0001382F File Offset: 0x00011A2F
		public int AddFace(UIntPtr meshPointer, int faceCorner0, int faceCorner1, int faceCorner2, UIntPtr lockHandle)
		{
			return ScriptingInterfaceOfIMesh.call_AddFaceDelegate(meshPointer, faceCorner0, faceCorner1, faceCorner2, lockHandle);
		}

		// Token: 0x0600030E RID: 782 RVA: 0x00013842 File Offset: 0x00011A42
		public int AddFaceCorner(UIntPtr meshPointer, Vec3 vertexPosition, Vec3 vertexNormal, Vec2 vertexUVCoordinates, uint vertexColor, UIntPtr lockHandle)
		{
			return ScriptingInterfaceOfIMesh.call_AddFaceCornerDelegate(meshPointer, vertexPosition, vertexNormal, vertexUVCoordinates, vertexColor, lockHandle);
		}

		// Token: 0x0600030F RID: 783 RVA: 0x00013857 File Offset: 0x00011A57
		public void AddMeshToMesh(UIntPtr meshPointer, UIntPtr newMeshPointer, ref MatrixFrame meshFrame)
		{
			ScriptingInterfaceOfIMesh.call_AddMeshToMeshDelegate(meshPointer, newMeshPointer, ref meshFrame);
		}

		// Token: 0x06000310 RID: 784 RVA: 0x00013868 File Offset: 0x00011A68
		public void AddTriangle(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint color, UIntPtr lockHandle)
		{
			ScriptingInterfaceOfIMesh.call_AddTriangleDelegate(meshPointer, p1, p2, p3, uv1, uv2, uv3, color, lockHandle);
		}

		// Token: 0x06000311 RID: 785 RVA: 0x00013890 File Offset: 0x00011A90
		public void AddTriangleWithVertexColors(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint c1, uint c2, uint c3, UIntPtr lockHandle)
		{
			ScriptingInterfaceOfIMesh.call_AddTriangleWithVertexColorsDelegate(meshPointer, p1, p2, p3, uv1, uv2, uv3, c1, c2, c3, lockHandle);
		}

		// Token: 0x06000312 RID: 786 RVA: 0x000138BA File Offset: 0x00011ABA
		public void ClearMesh(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_ClearMeshDelegate(meshPointer);
		}

		// Token: 0x06000313 RID: 787 RVA: 0x000138C7 File Offset: 0x00011AC7
		public void ComputeNormals(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_ComputeNormalsDelegate(meshPointer);
		}

		// Token: 0x06000314 RID: 788 RVA: 0x000138D4 File Offset: 0x00011AD4
		public void ComputeTangents(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_ComputeTangentsDelegate(meshPointer);
		}

		// Token: 0x06000315 RID: 789 RVA: 0x000138E4 File Offset: 0x00011AE4
		public Mesh CreateMesh(bool editable)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMesh.call_CreateMeshDelegate(editable);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000316 RID: 790 RVA: 0x00013930 File Offset: 0x00011B30
		public Mesh CreateMeshCopy(UIntPtr meshPointer)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMesh.call_CreateMeshCopyDelegate(meshPointer);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000317 RID: 791 RVA: 0x0001397C File Offset: 0x00011B7C
		public Mesh CreateMeshWithMaterial(UIntPtr ptr)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMesh.call_CreateMeshWithMaterialDelegate(ptr);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000318 RID: 792 RVA: 0x000139C6 File Offset: 0x00011BC6
		public void DisableContour(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_DisableContourDelegate(meshPointer);
		}

		// Token: 0x06000319 RID: 793 RVA: 0x000139D4 File Offset: 0x00011BD4
		public Mesh GetBaseMesh(UIntPtr ptr)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMesh.call_GetBaseMeshDelegate(ptr);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600031A RID: 794 RVA: 0x00013A1E File Offset: 0x00011C1E
		public BillboardType GetBillboard(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetBillboardDelegate(meshPointer);
		}

		// Token: 0x0600031B RID: 795 RVA: 0x00013A2B File Offset: 0x00011C2B
		public float GetBoundingBoxHeight(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetBoundingBoxHeightDelegate(meshPointer);
		}

		// Token: 0x0600031C RID: 796 RVA: 0x00013A38 File Offset: 0x00011C38
		public Vec3 GetBoundingBoxMax(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetBoundingBoxMaxDelegate(meshPointer);
		}

		// Token: 0x0600031D RID: 797 RVA: 0x00013A45 File Offset: 0x00011C45
		public Vec3 GetBoundingBoxMin(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetBoundingBoxMinDelegate(meshPointer);
		}

		// Token: 0x0600031E RID: 798 RVA: 0x00013A52 File Offset: 0x00011C52
		public float GetBoundingBoxWidth(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetBoundingBoxWidthDelegate(meshPointer);
		}

		// Token: 0x0600031F RID: 799 RVA: 0x00013A5F File Offset: 0x00011C5F
		public float GetClothLinearVelocityMultiplier(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetClothLinearVelocityMultiplierDelegate(meshPointer);
		}

		// Token: 0x06000320 RID: 800 RVA: 0x00013A6C File Offset: 0x00011C6C
		public uint GetColor(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetColorDelegate(meshPointer);
		}

		// Token: 0x06000321 RID: 801 RVA: 0x00013A79 File Offset: 0x00011C79
		public uint GetColor2(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetColor2Delegate(meshPointer);
		}

		// Token: 0x06000322 RID: 802 RVA: 0x00013A86 File Offset: 0x00011C86
		public int GetEditDataFaceCornerCount(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetEditDataFaceCornerCountDelegate(meshPointer);
		}

		// Token: 0x06000323 RID: 803 RVA: 0x00013A93 File Offset: 0x00011C93
		public uint GetEditDataFaceCornerVertexColor(UIntPtr meshPointer, int index)
		{
			return ScriptingInterfaceOfIMesh.call_GetEditDataFaceCornerVertexColorDelegate(meshPointer, index);
		}

		// Token: 0x06000324 RID: 804 RVA: 0x00013AA1 File Offset: 0x00011CA1
		public uint GetFaceCornerCount(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetFaceCornerCountDelegate(meshPointer);
		}

		// Token: 0x06000325 RID: 805 RVA: 0x00013AAE File Offset: 0x00011CAE
		public uint GetFaceCount(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetFaceCountDelegate(meshPointer);
		}

		// Token: 0x06000326 RID: 806 RVA: 0x00013ABB File Offset: 0x00011CBB
		public void GetLocalFrame(UIntPtr meshPointer, ref MatrixFrame outFrame)
		{
			ScriptingInterfaceOfIMesh.call_GetLocalFrameDelegate(meshPointer, ref outFrame);
		}

		// Token: 0x06000327 RID: 807 RVA: 0x00013ACC File Offset: 0x00011CCC
		public Material GetMaterial(UIntPtr meshPointer)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMesh.call_GetMaterialDelegate(meshPointer);
			Material result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Material(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000328 RID: 808 RVA: 0x00013B18 File Offset: 0x00011D18
		public Mesh GetMeshFromResource(string materialName)
		{
			byte[] array = null;
			if (materialName != null)
			{
				int byteCount = ScriptingInterfaceOfIMesh._utf8.GetByteCount(materialName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMesh._utf8.GetBytes(materialName, 0, materialName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMesh.call_GetMeshFromResourceDelegate(array);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000329 RID: 809 RVA: 0x00013BA4 File Offset: 0x00011DA4
		public string GetName(UIntPtr meshPointer)
		{
			if (ScriptingInterfaceOfIMesh.call_GetNameDelegate(meshPointer) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x0600032A RID: 810 RVA: 0x00013BBC File Offset: 0x00011DBC
		public Mesh GetRandomMeshWithVdecl(int vdecl)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMesh.call_GetRandomMeshWithVdeclDelegate(vdecl);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600032B RID: 811 RVA: 0x00013C08 File Offset: 0x00011E08
		public Material GetSecondMaterial(UIntPtr meshPointer)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMesh.call_GetSecondMaterialDelegate(meshPointer);
			Material result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Material(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600032C RID: 812 RVA: 0x00013C52 File Offset: 0x00011E52
		public Vec3 GetVectorArgument(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetVectorArgumentDelegate(meshPointer);
		}

		// Token: 0x0600032D RID: 813 RVA: 0x00013C5F File Offset: 0x00011E5F
		public Vec3 GetVectorArgument2(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetVectorArgument2Delegate(meshPointer);
		}

		// Token: 0x0600032E RID: 814 RVA: 0x00013C6C File Offset: 0x00011E6C
		public VisibilityMaskFlags GetVisibilityMask(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_GetVisibilityMaskDelegate(meshPointer);
		}

		// Token: 0x0600032F RID: 815 RVA: 0x00013C79 File Offset: 0x00011E79
		public bool HasCloth(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_HasClothDelegate(meshPointer);
		}

		// Token: 0x06000330 RID: 816 RVA: 0x00013C88 File Offset: 0x00011E88
		public bool HasTag(UIntPtr meshPointer, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIMesh._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMesh._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIMesh.call_HasTagDelegate(meshPointer, array);
		}

		// Token: 0x06000331 RID: 817 RVA: 0x00013CE3 File Offset: 0x00011EE3
		public void HintIndicesDynamic(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_HintIndicesDynamicDelegate(meshPointer);
		}

		// Token: 0x06000332 RID: 818 RVA: 0x00013CF0 File Offset: 0x00011EF0
		public void HintVerticesDynamic(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_HintVerticesDynamicDelegate(meshPointer);
		}

		// Token: 0x06000333 RID: 819 RVA: 0x00013CFD File Offset: 0x00011EFD
		public UIntPtr LockEditDataWrite(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMesh.call_LockEditDataWriteDelegate(meshPointer);
		}

		// Token: 0x06000334 RID: 820 RVA: 0x00013D0A File Offset: 0x00011F0A
		public void PreloadForRendering(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_PreloadForRenderingDelegate(meshPointer);
		}

		// Token: 0x06000335 RID: 821 RVA: 0x00013D17 File Offset: 0x00011F17
		public void RecomputeBoundingBox(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_RecomputeBoundingBoxDelegate(meshPointer);
		}

		// Token: 0x06000336 RID: 822 RVA: 0x00013D24 File Offset: 0x00011F24
		public void ReleaseEditDataUser(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_ReleaseEditDataUserDelegate(meshPointer);
		}

		// Token: 0x06000337 RID: 823 RVA: 0x00013D31 File Offset: 0x00011F31
		public void ReleaseResources(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_ReleaseResourcesDelegate(meshPointer);
		}

		// Token: 0x06000338 RID: 824 RVA: 0x00013D3E File Offset: 0x00011F3E
		public void SetAdditionalBoneFrame(UIntPtr meshPointer, int boneIndex, in MatrixFrame frame)
		{
			ScriptingInterfaceOfIMesh.call_SetAdditionalBoneFrameDelegate(meshPointer, boneIndex, frame);
		}

		// Token: 0x06000339 RID: 825 RVA: 0x00013D4D File Offset: 0x00011F4D
		public void SetAsNotEffectedBySeason(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_SetAsNotEffectedBySeasonDelegate(meshPointer);
		}

		// Token: 0x0600033A RID: 826 RVA: 0x00013D5A File Offset: 0x00011F5A
		public void SetBillboard(UIntPtr meshPointer, BillboardType value)
		{
			ScriptingInterfaceOfIMesh.call_SetBillboardDelegate(meshPointer, value);
		}

		// Token: 0x0600033B RID: 827 RVA: 0x00013D68 File Offset: 0x00011F68
		public void SetColor(UIntPtr meshPointer, uint newColor)
		{
			ScriptingInterfaceOfIMesh.call_SetColorDelegate(meshPointer, newColor);
		}

		// Token: 0x0600033C RID: 828 RVA: 0x00013D76 File Offset: 0x00011F76
		public void SetColor2(UIntPtr meshPointer, uint newColor2)
		{
			ScriptingInterfaceOfIMesh.call_SetColor2Delegate(meshPointer, newColor2);
		}

		// Token: 0x0600033D RID: 829 RVA: 0x00013D84 File Offset: 0x00011F84
		public void SetColorAlpha(UIntPtr meshPointer, uint newColorAlpha)
		{
			ScriptingInterfaceOfIMesh.call_SetColorAlphaDelegate(meshPointer, newColorAlpha);
		}

		// Token: 0x0600033E RID: 830 RVA: 0x00013D92 File Offset: 0x00011F92
		public void SetColorAndStroke(UIntPtr meshPointer, bool drawStroke)
		{
			ScriptingInterfaceOfIMesh.call_SetColorAndStrokeDelegate(meshPointer, drawStroke);
		}

		// Token: 0x0600033F RID: 831 RVA: 0x00013DA0 File Offset: 0x00011FA0
		public void SetContourColor(UIntPtr meshPointer, Vec3 color, bool alwaysVisible, bool maskMesh)
		{
			ScriptingInterfaceOfIMesh.call_SetContourColorDelegate(meshPointer, color, alwaysVisible, maskMesh);
		}

		// Token: 0x06000340 RID: 832 RVA: 0x00013DB1 File Offset: 0x00011FB1
		public void SetCullingMode(UIntPtr meshPointer, uint newCullingMode)
		{
			ScriptingInterfaceOfIMesh.call_SetCullingModeDelegate(meshPointer, newCullingMode);
		}

		// Token: 0x06000341 RID: 833 RVA: 0x00013DBF File Offset: 0x00011FBF
		public void SetCustomClipPlane(UIntPtr meshPointer, Vec3 clipPlanePosition, Vec3 clipPlaneNormal, int planeIndex)
		{
			ScriptingInterfaceOfIMesh.call_SetCustomClipPlaneDelegate(meshPointer, clipPlanePosition, clipPlaneNormal, planeIndex);
		}

		// Token: 0x06000342 RID: 834 RVA: 0x00013DD0 File Offset: 0x00011FD0
		public void SetEditDataFaceCornerVertexColor(UIntPtr meshPointer, int index, uint color)
		{
			ScriptingInterfaceOfIMesh.call_SetEditDataFaceCornerVertexColorDelegate(meshPointer, index, color);
		}

		// Token: 0x06000343 RID: 835 RVA: 0x00013DDF File Offset: 0x00011FDF
		public void SetEditDataPolicy(UIntPtr meshPointer, EditDataPolicy policy)
		{
			ScriptingInterfaceOfIMesh.call_SetEditDataPolicyDelegate(meshPointer, policy);
		}

		// Token: 0x06000344 RID: 836 RVA: 0x00013DED File Offset: 0x00011FED
		public void SetExternalBoundingBox(UIntPtr meshPointer, ref BoundingBox bbox)
		{
			ScriptingInterfaceOfIMesh.call_SetExternalBoundingBoxDelegate(meshPointer, ref bbox);
		}

		// Token: 0x06000345 RID: 837 RVA: 0x00013DFB File Offset: 0x00011FFB
		public void SetLocalFrame(UIntPtr meshPointer, ref MatrixFrame meshFrame)
		{
			ScriptingInterfaceOfIMesh.call_SetLocalFrameDelegate(meshPointer, ref meshFrame);
		}

		// Token: 0x06000346 RID: 838 RVA: 0x00013E09 File Offset: 0x00012009
		public void SetMaterial(UIntPtr meshPointer, UIntPtr materialpointer)
		{
			ScriptingInterfaceOfIMesh.call_SetMaterialDelegate(meshPointer, materialpointer);
		}

		// Token: 0x06000347 RID: 839 RVA: 0x00013E18 File Offset: 0x00012018
		public void SetMaterialByName(UIntPtr meshPointer, string materialName)
		{
			byte[] array = null;
			if (materialName != null)
			{
				int byteCount = ScriptingInterfaceOfIMesh._utf8.GetByteCount(materialName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMesh._utf8.GetBytes(materialName, 0, materialName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIMesh.call_SetMaterialByNameDelegate(meshPointer, array);
		}

		// Token: 0x06000348 RID: 840 RVA: 0x00013E73 File Offset: 0x00012073
		public void SetMeshRenderOrder(UIntPtr meshPointer, int renderorder)
		{
			ScriptingInterfaceOfIMesh.call_SetMeshRenderOrderDelegate(meshPointer, renderorder);
		}

		// Token: 0x06000349 RID: 841 RVA: 0x00013E81 File Offset: 0x00012081
		public void SetMorphTime(UIntPtr meshPointer, float newTime)
		{
			ScriptingInterfaceOfIMesh.call_SetMorphTimeDelegate(meshPointer, newTime);
		}

		// Token: 0x0600034A RID: 842 RVA: 0x00013E90 File Offset: 0x00012090
		public void SetName(UIntPtr meshPointer, string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIMesh._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMesh._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIMesh.call_SetNameDelegate(meshPointer, array);
		}

		// Token: 0x0600034B RID: 843 RVA: 0x00013EEB File Offset: 0x000120EB
		public void SetupAdditionalBoneBuffer(UIntPtr meshPointer, int numBones)
		{
			ScriptingInterfaceOfIMesh.call_SetupAdditionalBoneBufferDelegate(meshPointer, numBones);
		}

		// Token: 0x0600034C RID: 844 RVA: 0x00013EF9 File Offset: 0x000120F9
		public void SetVectorArgument(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			ScriptingInterfaceOfIMesh.call_SetVectorArgumentDelegate(meshPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x0600034D RID: 845 RVA: 0x00013F0C File Offset: 0x0001210C
		public void SetVectorArgument2(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			ScriptingInterfaceOfIMesh.call_SetVectorArgument2Delegate(meshPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x0600034E RID: 846 RVA: 0x00013F1F File Offset: 0x0001211F
		public void SetVisibilityMask(UIntPtr meshPointer, VisibilityMaskFlags value)
		{
			ScriptingInterfaceOfIMesh.call_SetVisibilityMaskDelegate(meshPointer, value);
		}

		// Token: 0x0600034F RID: 847 RVA: 0x00013F2D File Offset: 0x0001212D
		public void UnlockEditDataWrite(UIntPtr meshPointer, UIntPtr handle)
		{
			ScriptingInterfaceOfIMesh.call_UnlockEditDataWriteDelegate(meshPointer, handle);
		}

		// Token: 0x06000350 RID: 848 RVA: 0x00013F3B File Offset: 0x0001213B
		public void UpdateBoundingBox(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMesh.call_UpdateBoundingBoxDelegate(meshPointer);
		}

		// Token: 0x06000353 RID: 851 RVA: 0x00013F5C File Offset: 0x0001215C
		void IMesh.SetAdditionalBoneFrame(UIntPtr meshPointer, int boneIndex, in MatrixFrame frame)
		{
			this.SetAdditionalBoneFrame(meshPointer, boneIndex, frame);
		}

		// Token: 0x04000282 RID: 642
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x04000283 RID: 643
		public static ScriptingInterfaceOfIMesh.AddEditDataUserDelegate call_AddEditDataUserDelegate;

		// Token: 0x04000284 RID: 644
		public static ScriptingInterfaceOfIMesh.AddFaceDelegate call_AddFaceDelegate;

		// Token: 0x04000285 RID: 645
		public static ScriptingInterfaceOfIMesh.AddFaceCornerDelegate call_AddFaceCornerDelegate;

		// Token: 0x04000286 RID: 646
		public static ScriptingInterfaceOfIMesh.AddMeshToMeshDelegate call_AddMeshToMeshDelegate;

		// Token: 0x04000287 RID: 647
		public static ScriptingInterfaceOfIMesh.AddTriangleDelegate call_AddTriangleDelegate;

		// Token: 0x04000288 RID: 648
		public static ScriptingInterfaceOfIMesh.AddTriangleWithVertexColorsDelegate call_AddTriangleWithVertexColorsDelegate;

		// Token: 0x04000289 RID: 649
		public static ScriptingInterfaceOfIMesh.ClearMeshDelegate call_ClearMeshDelegate;

		// Token: 0x0400028A RID: 650
		public static ScriptingInterfaceOfIMesh.ComputeNormalsDelegate call_ComputeNormalsDelegate;

		// Token: 0x0400028B RID: 651
		public static ScriptingInterfaceOfIMesh.ComputeTangentsDelegate call_ComputeTangentsDelegate;

		// Token: 0x0400028C RID: 652
		public static ScriptingInterfaceOfIMesh.CreateMeshDelegate call_CreateMeshDelegate;

		// Token: 0x0400028D RID: 653
		public static ScriptingInterfaceOfIMesh.CreateMeshCopyDelegate call_CreateMeshCopyDelegate;

		// Token: 0x0400028E RID: 654
		public static ScriptingInterfaceOfIMesh.CreateMeshWithMaterialDelegate call_CreateMeshWithMaterialDelegate;

		// Token: 0x0400028F RID: 655
		public static ScriptingInterfaceOfIMesh.DisableContourDelegate call_DisableContourDelegate;

		// Token: 0x04000290 RID: 656
		public static ScriptingInterfaceOfIMesh.GetBaseMeshDelegate call_GetBaseMeshDelegate;

		// Token: 0x04000291 RID: 657
		public static ScriptingInterfaceOfIMesh.GetBillboardDelegate call_GetBillboardDelegate;

		// Token: 0x04000292 RID: 658
		public static ScriptingInterfaceOfIMesh.GetBoundingBoxHeightDelegate call_GetBoundingBoxHeightDelegate;

		// Token: 0x04000293 RID: 659
		public static ScriptingInterfaceOfIMesh.GetBoundingBoxMaxDelegate call_GetBoundingBoxMaxDelegate;

		// Token: 0x04000294 RID: 660
		public static ScriptingInterfaceOfIMesh.GetBoundingBoxMinDelegate call_GetBoundingBoxMinDelegate;

		// Token: 0x04000295 RID: 661
		public static ScriptingInterfaceOfIMesh.GetBoundingBoxWidthDelegate call_GetBoundingBoxWidthDelegate;

		// Token: 0x04000296 RID: 662
		public static ScriptingInterfaceOfIMesh.GetClothLinearVelocityMultiplierDelegate call_GetClothLinearVelocityMultiplierDelegate;

		// Token: 0x04000297 RID: 663
		public static ScriptingInterfaceOfIMesh.GetColorDelegate call_GetColorDelegate;

		// Token: 0x04000298 RID: 664
		public static ScriptingInterfaceOfIMesh.GetColor2Delegate call_GetColor2Delegate;

		// Token: 0x04000299 RID: 665
		public static ScriptingInterfaceOfIMesh.GetEditDataFaceCornerCountDelegate call_GetEditDataFaceCornerCountDelegate;

		// Token: 0x0400029A RID: 666
		public static ScriptingInterfaceOfIMesh.GetEditDataFaceCornerVertexColorDelegate call_GetEditDataFaceCornerVertexColorDelegate;

		// Token: 0x0400029B RID: 667
		public static ScriptingInterfaceOfIMesh.GetFaceCornerCountDelegate call_GetFaceCornerCountDelegate;

		// Token: 0x0400029C RID: 668
		public static ScriptingInterfaceOfIMesh.GetFaceCountDelegate call_GetFaceCountDelegate;

		// Token: 0x0400029D RID: 669
		public static ScriptingInterfaceOfIMesh.GetLocalFrameDelegate call_GetLocalFrameDelegate;

		// Token: 0x0400029E RID: 670
		public static ScriptingInterfaceOfIMesh.GetMaterialDelegate call_GetMaterialDelegate;

		// Token: 0x0400029F RID: 671
		public static ScriptingInterfaceOfIMesh.GetMeshFromResourceDelegate call_GetMeshFromResourceDelegate;

		// Token: 0x040002A0 RID: 672
		public static ScriptingInterfaceOfIMesh.GetNameDelegate call_GetNameDelegate;

		// Token: 0x040002A1 RID: 673
		public static ScriptingInterfaceOfIMesh.GetRandomMeshWithVdeclDelegate call_GetRandomMeshWithVdeclDelegate;

		// Token: 0x040002A2 RID: 674
		public static ScriptingInterfaceOfIMesh.GetSecondMaterialDelegate call_GetSecondMaterialDelegate;

		// Token: 0x040002A3 RID: 675
		public static ScriptingInterfaceOfIMesh.GetVectorArgumentDelegate call_GetVectorArgumentDelegate;

		// Token: 0x040002A4 RID: 676
		public static ScriptingInterfaceOfIMesh.GetVectorArgument2Delegate call_GetVectorArgument2Delegate;

		// Token: 0x040002A5 RID: 677
		public static ScriptingInterfaceOfIMesh.GetVisibilityMaskDelegate call_GetVisibilityMaskDelegate;

		// Token: 0x040002A6 RID: 678
		public static ScriptingInterfaceOfIMesh.HasClothDelegate call_HasClothDelegate;

		// Token: 0x040002A7 RID: 679
		public static ScriptingInterfaceOfIMesh.HasTagDelegate call_HasTagDelegate;

		// Token: 0x040002A8 RID: 680
		public static ScriptingInterfaceOfIMesh.HintIndicesDynamicDelegate call_HintIndicesDynamicDelegate;

		// Token: 0x040002A9 RID: 681
		public static ScriptingInterfaceOfIMesh.HintVerticesDynamicDelegate call_HintVerticesDynamicDelegate;

		// Token: 0x040002AA RID: 682
		public static ScriptingInterfaceOfIMesh.LockEditDataWriteDelegate call_LockEditDataWriteDelegate;

		// Token: 0x040002AB RID: 683
		public static ScriptingInterfaceOfIMesh.PreloadForRenderingDelegate call_PreloadForRenderingDelegate;

		// Token: 0x040002AC RID: 684
		public static ScriptingInterfaceOfIMesh.RecomputeBoundingBoxDelegate call_RecomputeBoundingBoxDelegate;

		// Token: 0x040002AD RID: 685
		public static ScriptingInterfaceOfIMesh.ReleaseEditDataUserDelegate call_ReleaseEditDataUserDelegate;

		// Token: 0x040002AE RID: 686
		public static ScriptingInterfaceOfIMesh.ReleaseResourcesDelegate call_ReleaseResourcesDelegate;

		// Token: 0x040002AF RID: 687
		public static ScriptingInterfaceOfIMesh.SetAdditionalBoneFrameDelegate call_SetAdditionalBoneFrameDelegate;

		// Token: 0x040002B0 RID: 688
		public static ScriptingInterfaceOfIMesh.SetAsNotEffectedBySeasonDelegate call_SetAsNotEffectedBySeasonDelegate;

		// Token: 0x040002B1 RID: 689
		public static ScriptingInterfaceOfIMesh.SetBillboardDelegate call_SetBillboardDelegate;

		// Token: 0x040002B2 RID: 690
		public static ScriptingInterfaceOfIMesh.SetColorDelegate call_SetColorDelegate;

		// Token: 0x040002B3 RID: 691
		public static ScriptingInterfaceOfIMesh.SetColor2Delegate call_SetColor2Delegate;

		// Token: 0x040002B4 RID: 692
		public static ScriptingInterfaceOfIMesh.SetColorAlphaDelegate call_SetColorAlphaDelegate;

		// Token: 0x040002B5 RID: 693
		public static ScriptingInterfaceOfIMesh.SetColorAndStrokeDelegate call_SetColorAndStrokeDelegate;

		// Token: 0x040002B6 RID: 694
		public static ScriptingInterfaceOfIMesh.SetContourColorDelegate call_SetContourColorDelegate;

		// Token: 0x040002B7 RID: 695
		public static ScriptingInterfaceOfIMesh.SetCullingModeDelegate call_SetCullingModeDelegate;

		// Token: 0x040002B8 RID: 696
		public static ScriptingInterfaceOfIMesh.SetCustomClipPlaneDelegate call_SetCustomClipPlaneDelegate;

		// Token: 0x040002B9 RID: 697
		public static ScriptingInterfaceOfIMesh.SetEditDataFaceCornerVertexColorDelegate call_SetEditDataFaceCornerVertexColorDelegate;

		// Token: 0x040002BA RID: 698
		public static ScriptingInterfaceOfIMesh.SetEditDataPolicyDelegate call_SetEditDataPolicyDelegate;

		// Token: 0x040002BB RID: 699
		public static ScriptingInterfaceOfIMesh.SetExternalBoundingBoxDelegate call_SetExternalBoundingBoxDelegate;

		// Token: 0x040002BC RID: 700
		public static ScriptingInterfaceOfIMesh.SetLocalFrameDelegate call_SetLocalFrameDelegate;

		// Token: 0x040002BD RID: 701
		public static ScriptingInterfaceOfIMesh.SetMaterialDelegate call_SetMaterialDelegate;

		// Token: 0x040002BE RID: 702
		public static ScriptingInterfaceOfIMesh.SetMaterialByNameDelegate call_SetMaterialByNameDelegate;

		// Token: 0x040002BF RID: 703
		public static ScriptingInterfaceOfIMesh.SetMeshRenderOrderDelegate call_SetMeshRenderOrderDelegate;

		// Token: 0x040002C0 RID: 704
		public static ScriptingInterfaceOfIMesh.SetMorphTimeDelegate call_SetMorphTimeDelegate;

		// Token: 0x040002C1 RID: 705
		public static ScriptingInterfaceOfIMesh.SetNameDelegate call_SetNameDelegate;

		// Token: 0x040002C2 RID: 706
		public static ScriptingInterfaceOfIMesh.SetupAdditionalBoneBufferDelegate call_SetupAdditionalBoneBufferDelegate;

		// Token: 0x040002C3 RID: 707
		public static ScriptingInterfaceOfIMesh.SetVectorArgumentDelegate call_SetVectorArgumentDelegate;

		// Token: 0x040002C4 RID: 708
		public static ScriptingInterfaceOfIMesh.SetVectorArgument2Delegate call_SetVectorArgument2Delegate;

		// Token: 0x040002C5 RID: 709
		public static ScriptingInterfaceOfIMesh.SetVisibilityMaskDelegate call_SetVisibilityMaskDelegate;

		// Token: 0x040002C6 RID: 710
		public static ScriptingInterfaceOfIMesh.UnlockEditDataWriteDelegate call_UnlockEditDataWriteDelegate;

		// Token: 0x040002C7 RID: 711
		public static ScriptingInterfaceOfIMesh.UpdateBoundingBoxDelegate call_UpdateBoundingBoxDelegate;

		// Token: 0x020002F8 RID: 760
		// (Invoke) Token: 0x060011F3 RID: 4595
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddEditDataUserDelegate(UIntPtr meshPointer);

		// Token: 0x020002F9 RID: 761
		// (Invoke) Token: 0x060011F7 RID: 4599
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int AddFaceDelegate(UIntPtr meshPointer, int faceCorner0, int faceCorner1, int faceCorner2, UIntPtr lockHandle);

		// Token: 0x020002FA RID: 762
		// (Invoke) Token: 0x060011FB RID: 4603
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int AddFaceCornerDelegate(UIntPtr meshPointer, Vec3 vertexPosition, Vec3 vertexNormal, Vec2 vertexUVCoordinates, uint vertexColor, UIntPtr lockHandle);

		// Token: 0x020002FB RID: 763
		// (Invoke) Token: 0x060011FF RID: 4607
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshToMeshDelegate(UIntPtr meshPointer, UIntPtr newMeshPointer, ref MatrixFrame meshFrame);

		// Token: 0x020002FC RID: 764
		// (Invoke) Token: 0x06001203 RID: 4611
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddTriangleDelegate(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint color, UIntPtr lockHandle);

		// Token: 0x020002FD RID: 765
		// (Invoke) Token: 0x06001207 RID: 4615
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddTriangleWithVertexColorsDelegate(UIntPtr meshPointer, Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint c1, uint c2, uint c3, UIntPtr lockHandle);

		// Token: 0x020002FE RID: 766
		// (Invoke) Token: 0x0600120B RID: 4619
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearMeshDelegate(UIntPtr meshPointer);

		// Token: 0x020002FF RID: 767
		// (Invoke) Token: 0x0600120F RID: 4623
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ComputeNormalsDelegate(UIntPtr meshPointer);

		// Token: 0x02000300 RID: 768
		// (Invoke) Token: 0x06001213 RID: 4627
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ComputeTangentsDelegate(UIntPtr meshPointer);

		// Token: 0x02000301 RID: 769
		// (Invoke) Token: 0x06001217 RID: 4631
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateMeshDelegate([MarshalAs(UnmanagedType.U1)] bool editable);

		// Token: 0x02000302 RID: 770
		// (Invoke) Token: 0x0600121B RID: 4635
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateMeshCopyDelegate(UIntPtr meshPointer);

		// Token: 0x02000303 RID: 771
		// (Invoke) Token: 0x0600121F RID: 4639
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateMeshWithMaterialDelegate(UIntPtr ptr);

		// Token: 0x02000304 RID: 772
		// (Invoke) Token: 0x06001223 RID: 4643
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DisableContourDelegate(UIntPtr meshPointer);

		// Token: 0x02000305 RID: 773
		// (Invoke) Token: 0x06001227 RID: 4647
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetBaseMeshDelegate(UIntPtr ptr);

		// Token: 0x02000306 RID: 774
		// (Invoke) Token: 0x0600122B RID: 4651
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate BillboardType GetBillboardDelegate(UIntPtr meshPointer);

		// Token: 0x02000307 RID: 775
		// (Invoke) Token: 0x0600122F RID: 4655
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetBoundingBoxHeightDelegate(UIntPtr meshPointer);

		// Token: 0x02000308 RID: 776
		// (Invoke) Token: 0x06001233 RID: 4659
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetBoundingBoxMaxDelegate(UIntPtr meshPointer);

		// Token: 0x02000309 RID: 777
		// (Invoke) Token: 0x06001237 RID: 4663
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetBoundingBoxMinDelegate(UIntPtr meshPointer);

		// Token: 0x0200030A RID: 778
		// (Invoke) Token: 0x0600123B RID: 4667
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetBoundingBoxWidthDelegate(UIntPtr meshPointer);

		// Token: 0x0200030B RID: 779
		// (Invoke) Token: 0x0600123F RID: 4671
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetClothLinearVelocityMultiplierDelegate(UIntPtr meshPointer);

		// Token: 0x0200030C RID: 780
		// (Invoke) Token: 0x06001243 RID: 4675
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetColorDelegate(UIntPtr meshPointer);

		// Token: 0x0200030D RID: 781
		// (Invoke) Token: 0x06001247 RID: 4679
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetColor2Delegate(UIntPtr meshPointer);

		// Token: 0x0200030E RID: 782
		// (Invoke) Token: 0x0600124B RID: 4683
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetEditDataFaceCornerCountDelegate(UIntPtr meshPointer);

		// Token: 0x0200030F RID: 783
		// (Invoke) Token: 0x0600124F RID: 4687
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetEditDataFaceCornerVertexColorDelegate(UIntPtr meshPointer, int index);

		// Token: 0x02000310 RID: 784
		// (Invoke) Token: 0x06001253 RID: 4691
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetFaceCornerCountDelegate(UIntPtr meshPointer);

		// Token: 0x02000311 RID: 785
		// (Invoke) Token: 0x06001257 RID: 4695
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetFaceCountDelegate(UIntPtr meshPointer);

		// Token: 0x02000312 RID: 786
		// (Invoke) Token: 0x0600125B RID: 4699
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetLocalFrameDelegate(UIntPtr meshPointer, ref MatrixFrame outFrame);

		// Token: 0x02000313 RID: 787
		// (Invoke) Token: 0x0600125F RID: 4703
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetMaterialDelegate(UIntPtr meshPointer);

		// Token: 0x02000314 RID: 788
		// (Invoke) Token: 0x06001263 RID: 4707
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetMeshFromResourceDelegate(byte[] materialName);

		// Token: 0x02000315 RID: 789
		// (Invoke) Token: 0x06001267 RID: 4711
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNameDelegate(UIntPtr meshPointer);

		// Token: 0x02000316 RID: 790
		// (Invoke) Token: 0x0600126B RID: 4715
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetRandomMeshWithVdeclDelegate(int vdecl);

		// Token: 0x02000317 RID: 791
		// (Invoke) Token: 0x0600126F RID: 4719
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetSecondMaterialDelegate(UIntPtr meshPointer);

		// Token: 0x02000318 RID: 792
		// (Invoke) Token: 0x06001273 RID: 4723
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetVectorArgumentDelegate(UIntPtr meshPointer);

		// Token: 0x02000319 RID: 793
		// (Invoke) Token: 0x06001277 RID: 4727
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetVectorArgument2Delegate(UIntPtr meshPointer);

		// Token: 0x0200031A RID: 794
		// (Invoke) Token: 0x0600127B RID: 4731
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate VisibilityMaskFlags GetVisibilityMaskDelegate(UIntPtr meshPointer);

		// Token: 0x0200031B RID: 795
		// (Invoke) Token: 0x0600127F RID: 4735
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasClothDelegate(UIntPtr meshPointer);

		// Token: 0x0200031C RID: 796
		// (Invoke) Token: 0x06001283 RID: 4739
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasTagDelegate(UIntPtr meshPointer, byte[] tag);

		// Token: 0x0200031D RID: 797
		// (Invoke) Token: 0x06001287 RID: 4743
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void HintIndicesDynamicDelegate(UIntPtr meshPointer);

		// Token: 0x0200031E RID: 798
		// (Invoke) Token: 0x0600128B RID: 4747
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void HintVerticesDynamicDelegate(UIntPtr meshPointer);

		// Token: 0x0200031F RID: 799
		// (Invoke) Token: 0x0600128F RID: 4751
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate UIntPtr LockEditDataWriteDelegate(UIntPtr meshPointer);

		// Token: 0x02000320 RID: 800
		// (Invoke) Token: 0x06001293 RID: 4755
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PreloadForRenderingDelegate(UIntPtr meshPointer);

		// Token: 0x02000321 RID: 801
		// (Invoke) Token: 0x06001297 RID: 4759
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RecomputeBoundingBoxDelegate(UIntPtr meshPointer);

		// Token: 0x02000322 RID: 802
		// (Invoke) Token: 0x0600129B RID: 4763
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseEditDataUserDelegate(UIntPtr meshPointer);

		// Token: 0x02000323 RID: 803
		// (Invoke) Token: 0x0600129F RID: 4767
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseResourcesDelegate(UIntPtr meshPointer);

		// Token: 0x02000324 RID: 804
		// (Invoke) Token: 0x060012A3 RID: 4771
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAdditionalBoneFrameDelegate(UIntPtr meshPointer, int boneIndex, in MatrixFrame frame);

		// Token: 0x02000325 RID: 805
		// (Invoke) Token: 0x060012A7 RID: 4775
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetAsNotEffectedBySeasonDelegate(UIntPtr meshPointer);

		// Token: 0x02000326 RID: 806
		// (Invoke) Token: 0x060012AB RID: 4779
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBillboardDelegate(UIntPtr meshPointer, BillboardType value);

		// Token: 0x02000327 RID: 807
		// (Invoke) Token: 0x060012AF RID: 4783
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetColorDelegate(UIntPtr meshPointer, uint newColor);

		// Token: 0x02000328 RID: 808
		// (Invoke) Token: 0x060012B3 RID: 4787
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetColor2Delegate(UIntPtr meshPointer, uint newColor2);

		// Token: 0x02000329 RID: 809
		// (Invoke) Token: 0x060012B7 RID: 4791
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetColorAlphaDelegate(UIntPtr meshPointer, uint newColorAlpha);

		// Token: 0x0200032A RID: 810
		// (Invoke) Token: 0x060012BB RID: 4795
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetColorAndStrokeDelegate(UIntPtr meshPointer, [MarshalAs(UnmanagedType.U1)] bool drawStroke);

		// Token: 0x0200032B RID: 811
		// (Invoke) Token: 0x060012BF RID: 4799
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetContourColorDelegate(UIntPtr meshPointer, Vec3 color, [MarshalAs(UnmanagedType.U1)] bool alwaysVisible, [MarshalAs(UnmanagedType.U1)] bool maskMesh);

		// Token: 0x0200032C RID: 812
		// (Invoke) Token: 0x060012C3 RID: 4803
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCullingModeDelegate(UIntPtr meshPointer, uint newCullingMode);

		// Token: 0x0200032D RID: 813
		// (Invoke) Token: 0x060012C7 RID: 4807
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCustomClipPlaneDelegate(UIntPtr meshPointer, Vec3 clipPlanePosition, Vec3 clipPlaneNormal, int planeIndex);

		// Token: 0x0200032E RID: 814
		// (Invoke) Token: 0x060012CB RID: 4811
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEditDataFaceCornerVertexColorDelegate(UIntPtr meshPointer, int index, uint color);

		// Token: 0x0200032F RID: 815
		// (Invoke) Token: 0x060012CF RID: 4815
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEditDataPolicyDelegate(UIntPtr meshPointer, EditDataPolicy policy);

		// Token: 0x02000330 RID: 816
		// (Invoke) Token: 0x060012D3 RID: 4819
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetExternalBoundingBoxDelegate(UIntPtr meshPointer, ref BoundingBox bbox);

		// Token: 0x02000331 RID: 817
		// (Invoke) Token: 0x060012D7 RID: 4823
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLocalFrameDelegate(UIntPtr meshPointer, ref MatrixFrame meshFrame);

		// Token: 0x02000332 RID: 818
		// (Invoke) Token: 0x060012DB RID: 4827
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMaterialDelegate(UIntPtr meshPointer, UIntPtr materialpointer);

		// Token: 0x02000333 RID: 819
		// (Invoke) Token: 0x060012DF RID: 4831
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMaterialByNameDelegate(UIntPtr meshPointer, byte[] materialName);

		// Token: 0x02000334 RID: 820
		// (Invoke) Token: 0x060012E3 RID: 4835
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMeshRenderOrderDelegate(UIntPtr meshPointer, int renderorder);

		// Token: 0x02000335 RID: 821
		// (Invoke) Token: 0x060012E7 RID: 4839
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMorphTimeDelegate(UIntPtr meshPointer, float newTime);

		// Token: 0x02000336 RID: 822
		// (Invoke) Token: 0x060012EB RID: 4843
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetNameDelegate(UIntPtr meshPointer, byte[] name);

		// Token: 0x02000337 RID: 823
		// (Invoke) Token: 0x060012EF RID: 4847
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetupAdditionalBoneBufferDelegate(UIntPtr meshPointer, int numBones);

		// Token: 0x02000338 RID: 824
		// (Invoke) Token: 0x060012F3 RID: 4851
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVectorArgumentDelegate(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x02000339 RID: 825
		// (Invoke) Token: 0x060012F7 RID: 4855
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVectorArgument2Delegate(UIntPtr meshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x0200033A RID: 826
		// (Invoke) Token: 0x060012FB RID: 4859
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVisibilityMaskDelegate(UIntPtr meshPointer, VisibilityMaskFlags value);

		// Token: 0x0200033B RID: 827
		// (Invoke) Token: 0x060012FF RID: 4863
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UnlockEditDataWriteDelegate(UIntPtr meshPointer, UIntPtr handle);

		// Token: 0x0200033C RID: 828
		// (Invoke) Token: 0x06001303 RID: 4867
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateBoundingBoxDelegate(UIntPtr meshPointer);
	}
}
