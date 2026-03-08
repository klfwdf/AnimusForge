using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x0200001B RID: 27
	internal class ScriptingInterfaceOfIMetaMesh : IMetaMesh
	{
		// Token: 0x06000359 RID: 857 RVA: 0x00014143 File Offset: 0x00012343
		public void AddEditDataUser(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_AddEditDataUserDelegate(meshPointer);
		}

		// Token: 0x0600035A RID: 858 RVA: 0x00014150 File Offset: 0x00012350
		public void AddMesh(UIntPtr multiMeshPointer, UIntPtr meshPointer, uint lodLevel)
		{
			ScriptingInterfaceOfIMetaMesh.call_AddMeshDelegate(multiMeshPointer, meshPointer, lodLevel);
		}

		// Token: 0x0600035B RID: 859 RVA: 0x0001415F File Offset: 0x0001235F
		public void AddMetaMesh(UIntPtr metaMeshPtr, UIntPtr otherMetaMeshPointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_AddMetaMeshDelegate(metaMeshPtr, otherMetaMeshPointer);
		}

		// Token: 0x0600035C RID: 860 RVA: 0x0001416D File Offset: 0x0001236D
		public void AssignClothBodyFrom(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_AssignClothBodyFromDelegate(multiMeshPointer, multiMeshToMergePointer);
		}

		// Token: 0x0600035D RID: 861 RVA: 0x0001417B File Offset: 0x0001237B
		public void BatchMultiMeshes(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_BatchMultiMeshesDelegate(multiMeshPointer, multiMeshToMergePointer);
		}

		// Token: 0x0600035E RID: 862 RVA: 0x0001418C File Offset: 0x0001238C
		public void BatchMultiMeshesMultiple(UIntPtr multiMeshPointer, UIntPtr[] multiMeshToMergePointers, int metaMeshCount)
		{
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(multiMeshToMergePointers, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			ScriptingInterfaceOfIMetaMesh.call_BatchMultiMeshesMultipleDelegate(multiMeshPointer, pointer, metaMeshCount);
			pinnedArrayData.Dispose();
		}

		// Token: 0x0600035F RID: 863 RVA: 0x000141C0 File Offset: 0x000123C0
		public void CheckMetaMeshExistence(string multiMeshPrefixName, int lod_count_check)
		{
			byte[] array = null;
			if (multiMeshPrefixName != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(multiMeshPrefixName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(multiMeshPrefixName, 0, multiMeshPrefixName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIMetaMesh.call_CheckMetaMeshExistenceDelegate(array, lod_count_check);
		}

		// Token: 0x06000360 RID: 864 RVA: 0x0001421B File Offset: 0x0001241B
		public int CheckResources(UIntPtr meshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_CheckResourcesDelegate(meshPointer);
		}

		// Token: 0x06000361 RID: 865 RVA: 0x00014228 File Offset: 0x00012428
		public void ClearEditData(UIntPtr multiMeshPointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_ClearEditDataDelegate(multiMeshPointer);
		}

		// Token: 0x06000362 RID: 866 RVA: 0x00014235 File Offset: 0x00012435
		public void ClearMeshes(UIntPtr multiMeshPointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_ClearMeshesDelegate(multiMeshPointer);
		}

		// Token: 0x06000363 RID: 867 RVA: 0x00014242 File Offset: 0x00012442
		public void ClearMeshesForLod(UIntPtr multiMeshPointer, int lodToClear)
		{
			ScriptingInterfaceOfIMetaMesh.call_ClearMeshesForLodDelegate(multiMeshPointer, lodToClear);
		}

		// Token: 0x06000364 RID: 868 RVA: 0x00014250 File Offset: 0x00012450
		public void ClearMeshesForLowerLods(UIntPtr multiMeshPointer, int lod)
		{
			ScriptingInterfaceOfIMetaMesh.call_ClearMeshesForLowerLodsDelegate(multiMeshPointer, lod);
		}

		// Token: 0x06000365 RID: 869 RVA: 0x0001425E File Offset: 0x0001245E
		public void ClearMeshesForOtherLods(UIntPtr multiMeshPointer, int lodToKeep)
		{
			ScriptingInterfaceOfIMetaMesh.call_ClearMeshesForOtherLodsDelegate(multiMeshPointer, lodToKeep);
		}

		// Token: 0x06000366 RID: 870 RVA: 0x0001426C File Offset: 0x0001246C
		public void CopyTo(UIntPtr metaMesh, UIntPtr targetMesh, bool copyMeshes)
		{
			ScriptingInterfaceOfIMetaMesh.call_CopyToDelegate(metaMesh, targetMesh, copyMeshes);
		}

		// Token: 0x06000367 RID: 871 RVA: 0x0001427C File Offset: 0x0001247C
		public MetaMesh CreateCopy(UIntPtr ptr)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMetaMesh.call_CreateCopyDelegate(ptr);
			MetaMesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new MetaMesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000368 RID: 872 RVA: 0x000142C8 File Offset: 0x000124C8
		public MetaMesh CreateCopyFromName(string multiMeshPrefixName, bool showErrors, bool mayReturnNull)
		{
			byte[] array = null;
			if (multiMeshPrefixName != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(multiMeshPrefixName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(multiMeshPrefixName, 0, multiMeshPrefixName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMetaMesh.call_CreateCopyFromNameDelegate(array, showErrors, mayReturnNull);
			MetaMesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new MetaMesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000369 RID: 873 RVA: 0x00014358 File Offset: 0x00012558
		public MetaMesh CreateMetaMesh(string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMetaMesh.call_CreateMetaMeshDelegate(array);
			MetaMesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new MetaMesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x0600036A RID: 874 RVA: 0x000143E4 File Offset: 0x000125E4
		public void DrawTextWithDefaultFont(UIntPtr multiMeshPointer, string text, Vec2 textPositionMin, Vec2 textPositionMax, Vec2 size, uint color, TextFlags flags)
		{
			byte[] array = null;
			if (text != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(text);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(text, 0, text.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIMetaMesh.call_DrawTextWithDefaultFontDelegate(multiMeshPointer, array, textPositionMin, textPositionMax, size, color, flags);
		}

		// Token: 0x0600036B RID: 875 RVA: 0x00014448 File Offset: 0x00012648
		public int GetAllMultiMeshes(UIntPtr[] gameEntitiesTemp)
		{
			PinnedArrayData<UIntPtr> pinnedArrayData = new PinnedArrayData<UIntPtr>(gameEntitiesTemp, false);
			IntPtr pointer = pinnedArrayData.Pointer;
			int result = ScriptingInterfaceOfIMetaMesh.call_GetAllMultiMeshesDelegate(pointer);
			pinnedArrayData.Dispose();
			return result;
		}

		// Token: 0x0600036C RID: 876 RVA: 0x00014478 File Offset: 0x00012678
		public void GetBoundingBox(UIntPtr multiMeshPointer, ref BoundingBox outBoundingBox)
		{
			ScriptingInterfaceOfIMetaMesh.call_GetBoundingBoxDelegate(multiMeshPointer, ref outBoundingBox);
		}

		// Token: 0x0600036D RID: 877 RVA: 0x00014486 File Offset: 0x00012686
		public uint GetFactor1(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_GetFactor1Delegate(multiMeshPointer);
		}

		// Token: 0x0600036E RID: 878 RVA: 0x00014493 File Offset: 0x00012693
		public uint GetFactor2(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_GetFactor2Delegate(multiMeshPointer);
		}

		// Token: 0x0600036F RID: 879 RVA: 0x000144A0 File Offset: 0x000126A0
		public void GetFrame(UIntPtr multiMeshPointer, ref MatrixFrame outFrame)
		{
			ScriptingInterfaceOfIMetaMesh.call_GetFrameDelegate(multiMeshPointer, ref outFrame);
		}

		// Token: 0x06000370 RID: 880 RVA: 0x000144AE File Offset: 0x000126AE
		public int GetLodMaskForMeshAtIndex(UIntPtr multiMeshPointer, int meshIndex)
		{
			return ScriptingInterfaceOfIMetaMesh.call_GetLodMaskForMeshAtIndexDelegate(multiMeshPointer, meshIndex);
		}

		// Token: 0x06000371 RID: 881 RVA: 0x000144BC File Offset: 0x000126BC
		public Mesh GetMeshAtIndex(UIntPtr multiMeshPointer, int meshIndex)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMetaMesh.call_GetMeshAtIndexDelegate(multiMeshPointer, meshIndex);
			Mesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new Mesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000372 RID: 882 RVA: 0x00014507 File Offset: 0x00012707
		public int GetMeshCount(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_GetMeshCountDelegate(multiMeshPointer);
		}

		// Token: 0x06000373 RID: 883 RVA: 0x00014514 File Offset: 0x00012714
		public int GetMeshCountWithTag(UIntPtr multiMeshPointer, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIMetaMesh.call_GetMeshCountWithTagDelegate(multiMeshPointer, array);
		}

		// Token: 0x06000374 RID: 884 RVA: 0x00014570 File Offset: 0x00012770
		public MetaMesh GetMorphedCopy(string multiMeshName, float morphTarget, bool showErrors)
		{
			byte[] array = null;
			if (multiMeshName != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(multiMeshName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(multiMeshName, 0, multiMeshName.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMetaMesh.call_GetMorphedCopyDelegate(array, morphTarget, showErrors);
			MetaMesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new MetaMesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000375 RID: 885 RVA: 0x00014600 File Offset: 0x00012800
		public MetaMesh GetMultiMesh(string name)
		{
			byte[] array = null;
			if (name != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(name);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(name, 0, name.Length, array, 0);
				array[byteCount] = 0;
			}
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIMetaMesh.call_GetMultiMeshDelegate(array);
			MetaMesh result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new MetaMesh(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x06000376 RID: 886 RVA: 0x0001468C File Offset: 0x0001288C
		public int GetMultiMeshCount()
		{
			return ScriptingInterfaceOfIMetaMesh.call_GetMultiMeshCountDelegate();
		}

		// Token: 0x06000377 RID: 887 RVA: 0x00014698 File Offset: 0x00012898
		public string GetName(UIntPtr multiMeshPointer)
		{
			if (ScriptingInterfaceOfIMetaMesh.call_GetNameDelegate(multiMeshPointer) != 1)
			{
				return null;
			}
			return Managed.ReturnValueFromEngine;
		}

		// Token: 0x06000378 RID: 888 RVA: 0x000146AF File Offset: 0x000128AF
		public int GetTotalGpuSize(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_GetTotalGpuSizeDelegate(multiMeshPointer);
		}

		// Token: 0x06000379 RID: 889 RVA: 0x000146BC File Offset: 0x000128BC
		public Vec3 GetVectorArgument2(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_GetVectorArgument2Delegate(multiMeshPointer);
		}

		// Token: 0x0600037A RID: 890 RVA: 0x000146C9 File Offset: 0x000128C9
		public Vec3 GetVectorUserData(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_GetVectorUserDataDelegate(multiMeshPointer);
		}

		// Token: 0x0600037B RID: 891 RVA: 0x000146D6 File Offset: 0x000128D6
		public VisibilityMaskFlags GetVisibilityMask(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_GetVisibilityMaskDelegate(multiMeshPointer);
		}

		// Token: 0x0600037C RID: 892 RVA: 0x000146E3 File Offset: 0x000128E3
		public bool HasAnyGeneratedLods(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_HasAnyGeneratedLodsDelegate(multiMeshPointer);
		}

		// Token: 0x0600037D RID: 893 RVA: 0x000146F0 File Offset: 0x000128F0
		public bool HasAnyLods(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_HasAnyLodsDelegate(multiMeshPointer);
		}

		// Token: 0x0600037E RID: 894 RVA: 0x000146FD File Offset: 0x000128FD
		public bool HasClothData(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_HasClothDataDelegate(multiMeshPointer);
		}

		// Token: 0x0600037F RID: 895 RVA: 0x0001470A File Offset: 0x0001290A
		public bool HasVertexBufferOrEditDataOrPackageItem(UIntPtr multiMeshPointer)
		{
			return ScriptingInterfaceOfIMetaMesh.call_HasVertexBufferOrEditDataOrPackageItemDelegate(multiMeshPointer);
		}

		// Token: 0x06000380 RID: 896 RVA: 0x00014717 File Offset: 0x00012917
		public void MergeMultiMeshes(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_MergeMultiMeshesDelegate(multiMeshPointer, multiMeshToMergePointer);
		}

		// Token: 0x06000381 RID: 897 RVA: 0x00014725 File Offset: 0x00012925
		public void PreloadForRendering(UIntPtr multiMeshPointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_PreloadForRenderingDelegate(multiMeshPointer);
		}

		// Token: 0x06000382 RID: 898 RVA: 0x00014732 File Offset: 0x00012932
		public void PreloadShaders(UIntPtr multiMeshPointer, bool useTableau, bool useTeamColor)
		{
			ScriptingInterfaceOfIMetaMesh.call_PreloadShadersDelegate(multiMeshPointer, useTableau, useTeamColor);
		}

		// Token: 0x06000383 RID: 899 RVA: 0x00014741 File Offset: 0x00012941
		public void RecomputeBoundingBox(UIntPtr multiMeshPointer, bool recomputeMeshes)
		{
			ScriptingInterfaceOfIMetaMesh.call_RecomputeBoundingBoxDelegate(multiMeshPointer, recomputeMeshes);
		}

		// Token: 0x06000384 RID: 900 RVA: 0x0001474F File Offset: 0x0001294F
		public void Release(UIntPtr multiMeshPointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_ReleaseDelegate(multiMeshPointer);
		}

		// Token: 0x06000385 RID: 901 RVA: 0x0001475C File Offset: 0x0001295C
		public void ReleaseEditDataUser(UIntPtr meshPointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_ReleaseEditDataUserDelegate(meshPointer);
		}

		// Token: 0x06000386 RID: 902 RVA: 0x0001476C File Offset: 0x0001296C
		public int RemoveMeshesWithoutTag(UIntPtr multiMeshPointer, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIMetaMesh.call_RemoveMeshesWithoutTagDelegate(multiMeshPointer, array);
		}

		// Token: 0x06000387 RID: 903 RVA: 0x000147C8 File Offset: 0x000129C8
		public int RemoveMeshesWithTag(UIntPtr multiMeshPointer, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			return ScriptingInterfaceOfIMetaMesh.call_RemoveMeshesWithTagDelegate(multiMeshPointer, array);
		}

		// Token: 0x06000388 RID: 904 RVA: 0x00014823 File Offset: 0x00012A23
		public void SetBillboarding(UIntPtr multiMeshPointer, BillboardType billboard)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetBillboardingDelegate(multiMeshPointer, billboard);
		}

		// Token: 0x06000389 RID: 905 RVA: 0x00014831 File Offset: 0x00012A31
		public void SetContourColor(UIntPtr meshPointer, uint color)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetContourColorDelegate(meshPointer, color);
		}

		// Token: 0x0600038A RID: 906 RVA: 0x0001483F File Offset: 0x00012A3F
		public void SetContourState(UIntPtr meshPointer, bool alwaysVisible)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetContourStateDelegate(meshPointer, alwaysVisible);
		}

		// Token: 0x0600038B RID: 907 RVA: 0x0001484D File Offset: 0x00012A4D
		public void SetCullMode(UIntPtr metaMeshPtr, MBMeshCullingMode cullMode)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetCullModeDelegate(metaMeshPtr, cullMode);
		}

		// Token: 0x0600038C RID: 908 RVA: 0x0001485B File Offset: 0x00012A5B
		public void SetEditDataPolicy(UIntPtr meshPointer, EditDataPolicy policy)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetEditDataPolicyDelegate(meshPointer, policy);
		}

		// Token: 0x0600038D RID: 909 RVA: 0x00014869 File Offset: 0x00012A69
		public void SetFactor1(UIntPtr multiMeshPointer, uint factorColor1)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetFactor1Delegate(multiMeshPointer, factorColor1);
		}

		// Token: 0x0600038E RID: 910 RVA: 0x00014877 File Offset: 0x00012A77
		public void SetFactor1Linear(UIntPtr multiMeshPointer, uint linearFactorColor1)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetFactor1LinearDelegate(multiMeshPointer, linearFactorColor1);
		}

		// Token: 0x0600038F RID: 911 RVA: 0x00014885 File Offset: 0x00012A85
		public void SetFactor2(UIntPtr multiMeshPointer, uint factorColor2)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetFactor2Delegate(multiMeshPointer, factorColor2);
		}

		// Token: 0x06000390 RID: 912 RVA: 0x00014893 File Offset: 0x00012A93
		public void SetFactor2Linear(UIntPtr multiMeshPointer, uint linearFactorColor2)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetFactor2LinearDelegate(multiMeshPointer, linearFactorColor2);
		}

		// Token: 0x06000391 RID: 913 RVA: 0x000148A4 File Offset: 0x00012AA4
		public void SetFactorColorToSubMeshesWithTag(UIntPtr meshPointer, uint color, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIMetaMesh.call_SetFactorColorToSubMeshesWithTagDelegate(meshPointer, color, array);
		}

		// Token: 0x06000392 RID: 914 RVA: 0x00014900 File Offset: 0x00012B00
		public void SetFrame(UIntPtr multiMeshPointer, ref MatrixFrame meshFrame)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetFrameDelegate(multiMeshPointer, ref meshFrame);
		}

		// Token: 0x06000393 RID: 915 RVA: 0x0001490E File Offset: 0x00012B0E
		public void SetGlossMultiplier(UIntPtr multiMeshPointer, float value)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetGlossMultiplierDelegate(multiMeshPointer, value);
		}

		// Token: 0x06000394 RID: 916 RVA: 0x0001491C File Offset: 0x00012B1C
		public void SetLodBias(UIntPtr multiMeshPointer, int lod_bias)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetLodBiasDelegate(multiMeshPointer, lod_bias);
		}

		// Token: 0x06000395 RID: 917 RVA: 0x0001492A File Offset: 0x00012B2A
		public void SetMaterial(UIntPtr multiMeshPointer, UIntPtr materialPointer)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetMaterialDelegate(multiMeshPointer, materialPointer);
		}

		// Token: 0x06000396 RID: 918 RVA: 0x00014938 File Offset: 0x00012B38
		public void SetMaterialToSubMeshesWithTag(UIntPtr meshPointer, UIntPtr materialPointer, string tag)
		{
			byte[] array = null;
			if (tag != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(tag);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(tag, 0, tag.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIMetaMesh.call_SetMaterialToSubMeshesWithTagDelegate(meshPointer, materialPointer, array);
		}

		// Token: 0x06000397 RID: 919 RVA: 0x00014994 File Offset: 0x00012B94
		public void SetNumLods(UIntPtr multiMeshPointer, int num_lod)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetNumLodsDelegate(multiMeshPointer, num_lod);
		}

		// Token: 0x06000398 RID: 920 RVA: 0x000149A4 File Offset: 0x00012BA4
		public void SetShaderToMaterial(UIntPtr multiMeshPointer, string shaderName)
		{
			byte[] array = null;
			if (shaderName != null)
			{
				int byteCount = ScriptingInterfaceOfIMetaMesh._utf8.GetByteCount(shaderName);
				array = ((byteCount < 1024) ? CallbackStringBufferManager.StringBuffer0 : new byte[byteCount + 1]);
				ScriptingInterfaceOfIMetaMesh._utf8.GetBytes(shaderName, 0, shaderName.Length, array, 0);
				array[byteCount] = 0;
			}
			ScriptingInterfaceOfIMetaMesh.call_SetShaderToMaterialDelegate(multiMeshPointer, array);
		}

		// Token: 0x06000399 RID: 921 RVA: 0x000149FF File Offset: 0x00012BFF
		public void SetVectorArgument(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetVectorArgumentDelegate(multiMeshPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x0600039A RID: 922 RVA: 0x00014A12 File Offset: 0x00012C12
		public void SetVectorArgument2(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetVectorArgument2Delegate(multiMeshPointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x0600039B RID: 923 RVA: 0x00014A25 File Offset: 0x00012C25
		public void SetVectorUserData(UIntPtr multiMeshPointer, ref Vec3 vectorArg)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetVectorUserDataDelegate(multiMeshPointer, ref vectorArg);
		}

		// Token: 0x0600039C RID: 924 RVA: 0x00014A33 File Offset: 0x00012C33
		public void SetVisibilityMask(UIntPtr multiMeshPointer, VisibilityMaskFlags visibilityMask)
		{
			ScriptingInterfaceOfIMetaMesh.call_SetVisibilityMaskDelegate(multiMeshPointer, visibilityMask);
		}

		// Token: 0x0600039D RID: 925 RVA: 0x00014A41 File Offset: 0x00012C41
		public void UseHeadBoneFaceGenScaling(UIntPtr multiMeshPointer, UIntPtr skeleton, sbyte headLookDirectionBoneIndex, ref MatrixFrame frame)
		{
			ScriptingInterfaceOfIMetaMesh.call_UseHeadBoneFaceGenScalingDelegate(multiMeshPointer, skeleton, headLookDirectionBoneIndex, ref frame);
		}

		// Token: 0x040002CC RID: 716
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x040002CD RID: 717
		public static ScriptingInterfaceOfIMetaMesh.AddEditDataUserDelegate call_AddEditDataUserDelegate;

		// Token: 0x040002CE RID: 718
		public static ScriptingInterfaceOfIMetaMesh.AddMeshDelegate call_AddMeshDelegate;

		// Token: 0x040002CF RID: 719
		public static ScriptingInterfaceOfIMetaMesh.AddMetaMeshDelegate call_AddMetaMeshDelegate;

		// Token: 0x040002D0 RID: 720
		public static ScriptingInterfaceOfIMetaMesh.AssignClothBodyFromDelegate call_AssignClothBodyFromDelegate;

		// Token: 0x040002D1 RID: 721
		public static ScriptingInterfaceOfIMetaMesh.BatchMultiMeshesDelegate call_BatchMultiMeshesDelegate;

		// Token: 0x040002D2 RID: 722
		public static ScriptingInterfaceOfIMetaMesh.BatchMultiMeshesMultipleDelegate call_BatchMultiMeshesMultipleDelegate;

		// Token: 0x040002D3 RID: 723
		public static ScriptingInterfaceOfIMetaMesh.CheckMetaMeshExistenceDelegate call_CheckMetaMeshExistenceDelegate;

		// Token: 0x040002D4 RID: 724
		public static ScriptingInterfaceOfIMetaMesh.CheckResourcesDelegate call_CheckResourcesDelegate;

		// Token: 0x040002D5 RID: 725
		public static ScriptingInterfaceOfIMetaMesh.ClearEditDataDelegate call_ClearEditDataDelegate;

		// Token: 0x040002D6 RID: 726
		public static ScriptingInterfaceOfIMetaMesh.ClearMeshesDelegate call_ClearMeshesDelegate;

		// Token: 0x040002D7 RID: 727
		public static ScriptingInterfaceOfIMetaMesh.ClearMeshesForLodDelegate call_ClearMeshesForLodDelegate;

		// Token: 0x040002D8 RID: 728
		public static ScriptingInterfaceOfIMetaMesh.ClearMeshesForLowerLodsDelegate call_ClearMeshesForLowerLodsDelegate;

		// Token: 0x040002D9 RID: 729
		public static ScriptingInterfaceOfIMetaMesh.ClearMeshesForOtherLodsDelegate call_ClearMeshesForOtherLodsDelegate;

		// Token: 0x040002DA RID: 730
		public static ScriptingInterfaceOfIMetaMesh.CopyToDelegate call_CopyToDelegate;

		// Token: 0x040002DB RID: 731
		public static ScriptingInterfaceOfIMetaMesh.CreateCopyDelegate call_CreateCopyDelegate;

		// Token: 0x040002DC RID: 732
		public static ScriptingInterfaceOfIMetaMesh.CreateCopyFromNameDelegate call_CreateCopyFromNameDelegate;

		// Token: 0x040002DD RID: 733
		public static ScriptingInterfaceOfIMetaMesh.CreateMetaMeshDelegate call_CreateMetaMeshDelegate;

		// Token: 0x040002DE RID: 734
		public static ScriptingInterfaceOfIMetaMesh.DrawTextWithDefaultFontDelegate call_DrawTextWithDefaultFontDelegate;

		// Token: 0x040002DF RID: 735
		public static ScriptingInterfaceOfIMetaMesh.GetAllMultiMeshesDelegate call_GetAllMultiMeshesDelegate;

		// Token: 0x040002E0 RID: 736
		public static ScriptingInterfaceOfIMetaMesh.GetBoundingBoxDelegate call_GetBoundingBoxDelegate;

		// Token: 0x040002E1 RID: 737
		public static ScriptingInterfaceOfIMetaMesh.GetFactor1Delegate call_GetFactor1Delegate;

		// Token: 0x040002E2 RID: 738
		public static ScriptingInterfaceOfIMetaMesh.GetFactor2Delegate call_GetFactor2Delegate;

		// Token: 0x040002E3 RID: 739
		public static ScriptingInterfaceOfIMetaMesh.GetFrameDelegate call_GetFrameDelegate;

		// Token: 0x040002E4 RID: 740
		public static ScriptingInterfaceOfIMetaMesh.GetLodMaskForMeshAtIndexDelegate call_GetLodMaskForMeshAtIndexDelegate;

		// Token: 0x040002E5 RID: 741
		public static ScriptingInterfaceOfIMetaMesh.GetMeshAtIndexDelegate call_GetMeshAtIndexDelegate;

		// Token: 0x040002E6 RID: 742
		public static ScriptingInterfaceOfIMetaMesh.GetMeshCountDelegate call_GetMeshCountDelegate;

		// Token: 0x040002E7 RID: 743
		public static ScriptingInterfaceOfIMetaMesh.GetMeshCountWithTagDelegate call_GetMeshCountWithTagDelegate;

		// Token: 0x040002E8 RID: 744
		public static ScriptingInterfaceOfIMetaMesh.GetMorphedCopyDelegate call_GetMorphedCopyDelegate;

		// Token: 0x040002E9 RID: 745
		public static ScriptingInterfaceOfIMetaMesh.GetMultiMeshDelegate call_GetMultiMeshDelegate;

		// Token: 0x040002EA RID: 746
		public static ScriptingInterfaceOfIMetaMesh.GetMultiMeshCountDelegate call_GetMultiMeshCountDelegate;

		// Token: 0x040002EB RID: 747
		public static ScriptingInterfaceOfIMetaMesh.GetNameDelegate call_GetNameDelegate;

		// Token: 0x040002EC RID: 748
		public static ScriptingInterfaceOfIMetaMesh.GetTotalGpuSizeDelegate call_GetTotalGpuSizeDelegate;

		// Token: 0x040002ED RID: 749
		public static ScriptingInterfaceOfIMetaMesh.GetVectorArgument2Delegate call_GetVectorArgument2Delegate;

		// Token: 0x040002EE RID: 750
		public static ScriptingInterfaceOfIMetaMesh.GetVectorUserDataDelegate call_GetVectorUserDataDelegate;

		// Token: 0x040002EF RID: 751
		public static ScriptingInterfaceOfIMetaMesh.GetVisibilityMaskDelegate call_GetVisibilityMaskDelegate;

		// Token: 0x040002F0 RID: 752
		public static ScriptingInterfaceOfIMetaMesh.HasAnyGeneratedLodsDelegate call_HasAnyGeneratedLodsDelegate;

		// Token: 0x040002F1 RID: 753
		public static ScriptingInterfaceOfIMetaMesh.HasAnyLodsDelegate call_HasAnyLodsDelegate;

		// Token: 0x040002F2 RID: 754
		public static ScriptingInterfaceOfIMetaMesh.HasClothDataDelegate call_HasClothDataDelegate;

		// Token: 0x040002F3 RID: 755
		public static ScriptingInterfaceOfIMetaMesh.HasVertexBufferOrEditDataOrPackageItemDelegate call_HasVertexBufferOrEditDataOrPackageItemDelegate;

		// Token: 0x040002F4 RID: 756
		public static ScriptingInterfaceOfIMetaMesh.MergeMultiMeshesDelegate call_MergeMultiMeshesDelegate;

		// Token: 0x040002F5 RID: 757
		public static ScriptingInterfaceOfIMetaMesh.PreloadForRenderingDelegate call_PreloadForRenderingDelegate;

		// Token: 0x040002F6 RID: 758
		public static ScriptingInterfaceOfIMetaMesh.PreloadShadersDelegate call_PreloadShadersDelegate;

		// Token: 0x040002F7 RID: 759
		public static ScriptingInterfaceOfIMetaMesh.RecomputeBoundingBoxDelegate call_RecomputeBoundingBoxDelegate;

		// Token: 0x040002F8 RID: 760
		public static ScriptingInterfaceOfIMetaMesh.ReleaseDelegate call_ReleaseDelegate;

		// Token: 0x040002F9 RID: 761
		public static ScriptingInterfaceOfIMetaMesh.ReleaseEditDataUserDelegate call_ReleaseEditDataUserDelegate;

		// Token: 0x040002FA RID: 762
		public static ScriptingInterfaceOfIMetaMesh.RemoveMeshesWithoutTagDelegate call_RemoveMeshesWithoutTagDelegate;

		// Token: 0x040002FB RID: 763
		public static ScriptingInterfaceOfIMetaMesh.RemoveMeshesWithTagDelegate call_RemoveMeshesWithTagDelegate;

		// Token: 0x040002FC RID: 764
		public static ScriptingInterfaceOfIMetaMesh.SetBillboardingDelegate call_SetBillboardingDelegate;

		// Token: 0x040002FD RID: 765
		public static ScriptingInterfaceOfIMetaMesh.SetContourColorDelegate call_SetContourColorDelegate;

		// Token: 0x040002FE RID: 766
		public static ScriptingInterfaceOfIMetaMesh.SetContourStateDelegate call_SetContourStateDelegate;

		// Token: 0x040002FF RID: 767
		public static ScriptingInterfaceOfIMetaMesh.SetCullModeDelegate call_SetCullModeDelegate;

		// Token: 0x04000300 RID: 768
		public static ScriptingInterfaceOfIMetaMesh.SetEditDataPolicyDelegate call_SetEditDataPolicyDelegate;

		// Token: 0x04000301 RID: 769
		public static ScriptingInterfaceOfIMetaMesh.SetFactor1Delegate call_SetFactor1Delegate;

		// Token: 0x04000302 RID: 770
		public static ScriptingInterfaceOfIMetaMesh.SetFactor1LinearDelegate call_SetFactor1LinearDelegate;

		// Token: 0x04000303 RID: 771
		public static ScriptingInterfaceOfIMetaMesh.SetFactor2Delegate call_SetFactor2Delegate;

		// Token: 0x04000304 RID: 772
		public static ScriptingInterfaceOfIMetaMesh.SetFactor2LinearDelegate call_SetFactor2LinearDelegate;

		// Token: 0x04000305 RID: 773
		public static ScriptingInterfaceOfIMetaMesh.SetFactorColorToSubMeshesWithTagDelegate call_SetFactorColorToSubMeshesWithTagDelegate;

		// Token: 0x04000306 RID: 774
		public static ScriptingInterfaceOfIMetaMesh.SetFrameDelegate call_SetFrameDelegate;

		// Token: 0x04000307 RID: 775
		public static ScriptingInterfaceOfIMetaMesh.SetGlossMultiplierDelegate call_SetGlossMultiplierDelegate;

		// Token: 0x04000308 RID: 776
		public static ScriptingInterfaceOfIMetaMesh.SetLodBiasDelegate call_SetLodBiasDelegate;

		// Token: 0x04000309 RID: 777
		public static ScriptingInterfaceOfIMetaMesh.SetMaterialDelegate call_SetMaterialDelegate;

		// Token: 0x0400030A RID: 778
		public static ScriptingInterfaceOfIMetaMesh.SetMaterialToSubMeshesWithTagDelegate call_SetMaterialToSubMeshesWithTagDelegate;

		// Token: 0x0400030B RID: 779
		public static ScriptingInterfaceOfIMetaMesh.SetNumLodsDelegate call_SetNumLodsDelegate;

		// Token: 0x0400030C RID: 780
		public static ScriptingInterfaceOfIMetaMesh.SetShaderToMaterialDelegate call_SetShaderToMaterialDelegate;

		// Token: 0x0400030D RID: 781
		public static ScriptingInterfaceOfIMetaMesh.SetVectorArgumentDelegate call_SetVectorArgumentDelegate;

		// Token: 0x0400030E RID: 782
		public static ScriptingInterfaceOfIMetaMesh.SetVectorArgument2Delegate call_SetVectorArgument2Delegate;

		// Token: 0x0400030F RID: 783
		public static ScriptingInterfaceOfIMetaMesh.SetVectorUserDataDelegate call_SetVectorUserDataDelegate;

		// Token: 0x04000310 RID: 784
		public static ScriptingInterfaceOfIMetaMesh.SetVisibilityMaskDelegate call_SetVisibilityMaskDelegate;

		// Token: 0x04000311 RID: 785
		public static ScriptingInterfaceOfIMetaMesh.UseHeadBoneFaceGenScalingDelegate call_UseHeadBoneFaceGenScalingDelegate;

		// Token: 0x02000340 RID: 832
		// (Invoke) Token: 0x06001313 RID: 4883
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddEditDataUserDelegate(UIntPtr meshPointer);

		// Token: 0x02000341 RID: 833
		// (Invoke) Token: 0x06001317 RID: 4887
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshDelegate(UIntPtr multiMeshPointer, UIntPtr meshPointer, uint lodLevel);

		// Token: 0x02000342 RID: 834
		// (Invoke) Token: 0x0600131B RID: 4891
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMetaMeshDelegate(UIntPtr metaMeshPtr, UIntPtr otherMetaMeshPointer);

		// Token: 0x02000343 RID: 835
		// (Invoke) Token: 0x0600131F RID: 4895
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AssignClothBodyFromDelegate(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

		// Token: 0x02000344 RID: 836
		// (Invoke) Token: 0x06001323 RID: 4899
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void BatchMultiMeshesDelegate(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

		// Token: 0x02000345 RID: 837
		// (Invoke) Token: 0x06001327 RID: 4903
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void BatchMultiMeshesMultipleDelegate(UIntPtr multiMeshPointer, IntPtr multiMeshToMergePointers, int metaMeshCount);

		// Token: 0x02000346 RID: 838
		// (Invoke) Token: 0x0600132B RID: 4907
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CheckMetaMeshExistenceDelegate(byte[] multiMeshPrefixName, int lod_count_check);

		// Token: 0x02000347 RID: 839
		// (Invoke) Token: 0x0600132F RID: 4911
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int CheckResourcesDelegate(UIntPtr meshPointer);

		// Token: 0x02000348 RID: 840
		// (Invoke) Token: 0x06001333 RID: 4915
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearEditDataDelegate(UIntPtr multiMeshPointer);

		// Token: 0x02000349 RID: 841
		// (Invoke) Token: 0x06001337 RID: 4919
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearMeshesDelegate(UIntPtr multiMeshPointer);

		// Token: 0x0200034A RID: 842
		// (Invoke) Token: 0x0600133B RID: 4923
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearMeshesForLodDelegate(UIntPtr multiMeshPointer, int lodToClear);

		// Token: 0x0200034B RID: 843
		// (Invoke) Token: 0x0600133F RID: 4927
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearMeshesForLowerLodsDelegate(UIntPtr multiMeshPointer, int lod);

		// Token: 0x0200034C RID: 844
		// (Invoke) Token: 0x06001343 RID: 4931
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearMeshesForOtherLodsDelegate(UIntPtr multiMeshPointer, int lodToKeep);

		// Token: 0x0200034D RID: 845
		// (Invoke) Token: 0x06001347 RID: 4935
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void CopyToDelegate(UIntPtr metaMesh, UIntPtr targetMesh, [MarshalAs(UnmanagedType.U1)] bool copyMeshes);

		// Token: 0x0200034E RID: 846
		// (Invoke) Token: 0x0600134B RID: 4939
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateCopyDelegate(UIntPtr ptr);

		// Token: 0x0200034F RID: 847
		// (Invoke) Token: 0x0600134F RID: 4943
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateCopyFromNameDelegate(byte[] multiMeshPrefixName, [MarshalAs(UnmanagedType.U1)] bool showErrors, [MarshalAs(UnmanagedType.U1)] bool mayReturnNull);

		// Token: 0x02000350 RID: 848
		// (Invoke) Token: 0x06001353 RID: 4947
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateMetaMeshDelegate(byte[] name);

		// Token: 0x02000351 RID: 849
		// (Invoke) Token: 0x06001357 RID: 4951
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void DrawTextWithDefaultFontDelegate(UIntPtr multiMeshPointer, byte[] text, Vec2 textPositionMin, Vec2 textPositionMax, Vec2 size, uint color, TextFlags flags);

		// Token: 0x02000352 RID: 850
		// (Invoke) Token: 0x0600135B RID: 4955
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetAllMultiMeshesDelegate(IntPtr gameEntitiesTemp);

		// Token: 0x02000353 RID: 851
		// (Invoke) Token: 0x0600135F RID: 4959
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetBoundingBoxDelegate(UIntPtr multiMeshPointer, ref BoundingBox outBoundingBox);

		// Token: 0x02000354 RID: 852
		// (Invoke) Token: 0x06001363 RID: 4963
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetFactor1Delegate(UIntPtr multiMeshPointer);

		// Token: 0x02000355 RID: 853
		// (Invoke) Token: 0x06001367 RID: 4967
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate uint GetFactor2Delegate(UIntPtr multiMeshPointer);

		// Token: 0x02000356 RID: 854
		// (Invoke) Token: 0x0600136B RID: 4971
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GetFrameDelegate(UIntPtr multiMeshPointer, ref MatrixFrame outFrame);

		// Token: 0x02000357 RID: 855
		// (Invoke) Token: 0x0600136F RID: 4975
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetLodMaskForMeshAtIndexDelegate(UIntPtr multiMeshPointer, int meshIndex);

		// Token: 0x02000358 RID: 856
		// (Invoke) Token: 0x06001373 RID: 4979
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetMeshAtIndexDelegate(UIntPtr multiMeshPointer, int meshIndex);

		// Token: 0x02000359 RID: 857
		// (Invoke) Token: 0x06001377 RID: 4983
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetMeshCountDelegate(UIntPtr multiMeshPointer);

		// Token: 0x0200035A RID: 858
		// (Invoke) Token: 0x0600137B RID: 4987
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetMeshCountWithTagDelegate(UIntPtr multiMeshPointer, byte[] tag);

		// Token: 0x0200035B RID: 859
		// (Invoke) Token: 0x0600137F RID: 4991
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetMorphedCopyDelegate(byte[] multiMeshName, float morphTarget, [MarshalAs(UnmanagedType.U1)] bool showErrors);

		// Token: 0x0200035C RID: 860
		// (Invoke) Token: 0x06001383 RID: 4995
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer GetMultiMeshDelegate(byte[] name);

		// Token: 0x0200035D RID: 861
		// (Invoke) Token: 0x06001387 RID: 4999
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetMultiMeshCountDelegate();

		// Token: 0x0200035E RID: 862
		// (Invoke) Token: 0x0600138B RID: 5003
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetNameDelegate(UIntPtr multiMeshPointer);

		// Token: 0x0200035F RID: 863
		// (Invoke) Token: 0x0600138F RID: 5007
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int GetTotalGpuSizeDelegate(UIntPtr multiMeshPointer);

		// Token: 0x02000360 RID: 864
		// (Invoke) Token: 0x06001393 RID: 5011
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetVectorArgument2Delegate(UIntPtr multiMeshPointer);

		// Token: 0x02000361 RID: 865
		// (Invoke) Token: 0x06001397 RID: 5015
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetVectorUserDataDelegate(UIntPtr multiMeshPointer);

		// Token: 0x02000362 RID: 866
		// (Invoke) Token: 0x0600139B RID: 5019
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate VisibilityMaskFlags GetVisibilityMaskDelegate(UIntPtr multiMeshPointer);

		// Token: 0x02000363 RID: 867
		// (Invoke) Token: 0x0600139F RID: 5023
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasAnyGeneratedLodsDelegate(UIntPtr multiMeshPointer);

		// Token: 0x02000364 RID: 868
		// (Invoke) Token: 0x060013A3 RID: 5027
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasAnyLodsDelegate(UIntPtr multiMeshPointer);

		// Token: 0x02000365 RID: 869
		// (Invoke) Token: 0x060013A7 RID: 5031
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasClothDataDelegate(UIntPtr multiMeshPointer);

		// Token: 0x02000366 RID: 870
		// (Invoke) Token: 0x060013AB RID: 5035
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool HasVertexBufferOrEditDataOrPackageItemDelegate(UIntPtr multiMeshPointer);

		// Token: 0x02000367 RID: 871
		// (Invoke) Token: 0x060013AF RID: 5039
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void MergeMultiMeshesDelegate(UIntPtr multiMeshPointer, UIntPtr multiMeshToMergePointer);

		// Token: 0x02000368 RID: 872
		// (Invoke) Token: 0x060013B3 RID: 5043
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PreloadForRenderingDelegate(UIntPtr multiMeshPointer);

		// Token: 0x02000369 RID: 873
		// (Invoke) Token: 0x060013B7 RID: 5047
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void PreloadShadersDelegate(UIntPtr multiMeshPointer, [MarshalAs(UnmanagedType.U1)] bool useTableau, [MarshalAs(UnmanagedType.U1)] bool useTeamColor);

		// Token: 0x0200036A RID: 874
		// (Invoke) Token: 0x060013BB RID: 5051
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RecomputeBoundingBoxDelegate(UIntPtr multiMeshPointer, [MarshalAs(UnmanagedType.U1)] bool recomputeMeshes);

		// Token: 0x0200036B RID: 875
		// (Invoke) Token: 0x060013BF RID: 5055
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseDelegate(UIntPtr multiMeshPointer);

		// Token: 0x0200036C RID: 876
		// (Invoke) Token: 0x060013C3 RID: 5059
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReleaseEditDataUserDelegate(UIntPtr meshPointer);

		// Token: 0x0200036D RID: 877
		// (Invoke) Token: 0x060013C7 RID: 5063
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int RemoveMeshesWithoutTagDelegate(UIntPtr multiMeshPointer, byte[] tag);

		// Token: 0x0200036E RID: 878
		// (Invoke) Token: 0x060013CB RID: 5067
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int RemoveMeshesWithTagDelegate(UIntPtr multiMeshPointer, byte[] tag);

		// Token: 0x0200036F RID: 879
		// (Invoke) Token: 0x060013CF RID: 5071
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetBillboardingDelegate(UIntPtr multiMeshPointer, BillboardType billboard);

		// Token: 0x02000370 RID: 880
		// (Invoke) Token: 0x060013D3 RID: 5075
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetContourColorDelegate(UIntPtr meshPointer, uint color);

		// Token: 0x02000371 RID: 881
		// (Invoke) Token: 0x060013D7 RID: 5079
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetContourStateDelegate(UIntPtr meshPointer, [MarshalAs(UnmanagedType.U1)] bool alwaysVisible);

		// Token: 0x02000372 RID: 882
		// (Invoke) Token: 0x060013DB RID: 5083
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCullModeDelegate(UIntPtr metaMeshPtr, MBMeshCullingMode cullMode);

		// Token: 0x02000373 RID: 883
		// (Invoke) Token: 0x060013DF RID: 5087
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetEditDataPolicyDelegate(UIntPtr meshPointer, EditDataPolicy policy);

		// Token: 0x02000374 RID: 884
		// (Invoke) Token: 0x060013E3 RID: 5091
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFactor1Delegate(UIntPtr multiMeshPointer, uint factorColor1);

		// Token: 0x02000375 RID: 885
		// (Invoke) Token: 0x060013E7 RID: 5095
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFactor1LinearDelegate(UIntPtr multiMeshPointer, uint linearFactorColor1);

		// Token: 0x02000376 RID: 886
		// (Invoke) Token: 0x060013EB RID: 5099
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFactor2Delegate(UIntPtr multiMeshPointer, uint factorColor2);

		// Token: 0x02000377 RID: 887
		// (Invoke) Token: 0x060013EF RID: 5103
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFactor2LinearDelegate(UIntPtr multiMeshPointer, uint linearFactorColor2);

		// Token: 0x02000378 RID: 888
		// (Invoke) Token: 0x060013F3 RID: 5107
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFactorColorToSubMeshesWithTagDelegate(UIntPtr meshPointer, uint color, byte[] tag);

		// Token: 0x02000379 RID: 889
		// (Invoke) Token: 0x060013F7 RID: 5111
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetFrameDelegate(UIntPtr multiMeshPointer, ref MatrixFrame meshFrame);

		// Token: 0x0200037A RID: 890
		// (Invoke) Token: 0x060013FB RID: 5115
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetGlossMultiplierDelegate(UIntPtr multiMeshPointer, float value);

		// Token: 0x0200037B RID: 891
		// (Invoke) Token: 0x060013FF RID: 5119
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetLodBiasDelegate(UIntPtr multiMeshPointer, int lod_bias);

		// Token: 0x0200037C RID: 892
		// (Invoke) Token: 0x06001403 RID: 5123
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMaterialDelegate(UIntPtr multiMeshPointer, UIntPtr materialPointer);

		// Token: 0x0200037D RID: 893
		// (Invoke) Token: 0x06001407 RID: 5127
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetMaterialToSubMeshesWithTagDelegate(UIntPtr meshPointer, UIntPtr materialPointer, byte[] tag);

		// Token: 0x0200037E RID: 894
		// (Invoke) Token: 0x0600140B RID: 5131
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetNumLodsDelegate(UIntPtr multiMeshPointer, int num_lod);

		// Token: 0x0200037F RID: 895
		// (Invoke) Token: 0x0600140F RID: 5135
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetShaderToMaterialDelegate(UIntPtr multiMeshPointer, byte[] shaderName);

		// Token: 0x02000380 RID: 896
		// (Invoke) Token: 0x06001413 RID: 5139
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVectorArgumentDelegate(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x02000381 RID: 897
		// (Invoke) Token: 0x06001417 RID: 5143
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVectorArgument2Delegate(UIntPtr multiMeshPointer, float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3);

		// Token: 0x02000382 RID: 898
		// (Invoke) Token: 0x0600141B RID: 5147
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVectorUserDataDelegate(UIntPtr multiMeshPointer, ref Vec3 vectorArg);

		// Token: 0x02000383 RID: 899
		// (Invoke) Token: 0x0600141F RID: 5151
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVisibilityMaskDelegate(UIntPtr multiMeshPointer, VisibilityMaskFlags visibilityMask);

		// Token: 0x02000384 RID: 900
		// (Invoke) Token: 0x06001423 RID: 5155
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UseHeadBoneFaceGenScalingDelegate(UIntPtr multiMeshPointer, UIntPtr skeleton, sbyte headLookDirectionBoneIndex, ref MatrixFrame frame);
	}
}
