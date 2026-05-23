using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ManagedCallbacks
{
	// Token: 0x02000017 RID: 23
	internal class ScriptingInterfaceOfIManagedMeshEditOperations : IManagedMeshEditOperations
	{
		// Token: 0x060002B5 RID: 693 RVA: 0x00012FD9 File Offset: 0x000111D9
		public int AddFace(UIntPtr Pointer, int patchNode0, int patchNode1, int patchNode2)
		{
			return ScriptingInterfaceOfIManagedMeshEditOperations.call_AddFaceDelegate(Pointer, patchNode0, patchNode1, patchNode2);
		}

		// Token: 0x060002B6 RID: 694 RVA: 0x00012FEA File Offset: 0x000111EA
		public int AddFaceCorner1(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec3 color, ref Vec3 normal)
		{
			return ScriptingInterfaceOfIManagedMeshEditOperations.call_AddFaceCorner1Delegate(Pointer, vertexIndex, ref uv0, ref color, ref normal);
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x00012FFD File Offset: 0x000111FD
		public int AddFaceCorner2(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec2 uv1, ref Vec3 color, ref Vec3 normal)
		{
			return ScriptingInterfaceOfIManagedMeshEditOperations.call_AddFaceCorner2Delegate(Pointer, vertexIndex, ref uv0, ref uv1, ref color, ref normal);
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x00013012 File Offset: 0x00011212
		public void AddLine(UIntPtr Pointer, ref Vec3 start, ref Vec3 end, ref Vec3 color, float lineWidth)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddLineDelegate(Pointer, ref start, ref end, ref color, lineWidth);
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x00013025 File Offset: 0x00011225
		public void AddMesh(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshDelegate(Pointer, meshPointer, ref frame);
		}

		// Token: 0x060002BA RID: 698 RVA: 0x00013034 File Offset: 0x00011234
		public void AddMeshAux(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex, ref Vec3 color, bool transformNormal, bool heightGradient, bool addSkinData, bool useDoublePrecision)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshAuxDelegate(Pointer, meshPointer, ref frame, boneIndex, ref color, transformNormal, heightGradient, addSkinData, useDoublePrecision);
		}

		// Token: 0x060002BB RID: 699 RVA: 0x0001305A File Offset: 0x0001125A
		public void AddMeshToBone(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshToBoneDelegate(Pointer, meshPointer, ref frame, boneIndex);
		}

		// Token: 0x060002BC RID: 700 RVA: 0x0001306B File Offset: 0x0001126B
		public void AddMeshWithColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, bool useDoublePrecision)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshWithColorDelegate(Pointer, meshPointer, ref frame, ref vertexColor, useDoublePrecision);
		}

		// Token: 0x060002BD RID: 701 RVA: 0x0001307E File Offset: 0x0001127E
		public void AddMeshWithFixedNormals(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshWithFixedNormalsDelegate(Pointer, meshPointer, ref frame);
		}

		// Token: 0x060002BE RID: 702 RVA: 0x0001308D File Offset: 0x0001128D
		public void AddMeshWithFixedNormalsWithHeightGradientColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshWithFixedNormalsWithHeightGradientColorDelegate(Pointer, meshPointer, ref frame);
		}

		// Token: 0x060002BF RID: 703 RVA: 0x0001309C File Offset: 0x0001129C
		public void AddMeshWithSkinData(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddMeshWithSkinDataDelegate(Pointer, meshPointer, ref frame, boneIndex);
		}

		// Token: 0x060002C0 RID: 704 RVA: 0x000130AD File Offset: 0x000112AD
		public void AddRect(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddRectDelegate(Pointer, ref originBegin, ref originEnd, ref uvBegin, ref uvEnd, ref color);
		}

		// Token: 0x060002C1 RID: 705 RVA: 0x000130C2 File Offset: 0x000112C2
		public void AddRectangle3(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddRectangle3Delegate(Pointer, ref o, ref size, ref uv_origin, ref uvSize, ref color);
		}

		// Token: 0x060002C2 RID: 706 RVA: 0x000130D7 File Offset: 0x000112D7
		public void AddRectangleWithInverseUV(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddRectangleWithInverseUVDelegate(Pointer, ref o, ref size, ref uv_origin, ref uvSize, ref color);
		}

		// Token: 0x060002C3 RID: 707 RVA: 0x000130EC File Offset: 0x000112EC
		public void AddRectWithZUp(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddRectWithZUpDelegate(Pointer, ref originBegin, ref originEnd, ref uvBegin, ref uvEnd, ref color);
		}

		// Token: 0x060002C4 RID: 708 RVA: 0x00013101 File Offset: 0x00011301
		public void AddSkinnedMeshWithColor(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, bool useDoublePrecision)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddSkinnedMeshWithColorDelegate(Pointer, meshPointer, ref frame, ref vertexColor, useDoublePrecision);
		}

		// Token: 0x060002C5 RID: 709 RVA: 0x00013114 File Offset: 0x00011314
		public void AddTriangle1(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 color)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddTriangle1Delegate(Pointer, ref p1, ref p2, ref p3, ref uv1, ref uv2, ref uv3, ref color);
		}

		// Token: 0x060002C6 RID: 710 RVA: 0x00013138 File Offset: 0x00011338
		public void AddTriangle2(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec3 n1, ref Vec3 n2, ref Vec3 n3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 c1, ref Vec3 c2, ref Vec3 c3)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_AddTriangle2Delegate(Pointer, ref p1, ref p2, ref p3, ref n1, ref n2, ref n3, ref uv1, ref uv2, ref uv3, ref c1, ref c2, ref c3);
		}

		// Token: 0x060002C7 RID: 711 RVA: 0x00013166 File Offset: 0x00011366
		public int AddVertex(UIntPtr Pointer, ref Vec3 vertexPos)
		{
			return ScriptingInterfaceOfIManagedMeshEditOperations.call_AddVertexDelegate(Pointer, ref vertexPos);
		}

		// Token: 0x060002C8 RID: 712 RVA: 0x00013174 File Offset: 0x00011374
		public void ApplyCPUSkinning(UIntPtr Pointer, UIntPtr skeletonPointer)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_ApplyCPUSkinningDelegate(Pointer, skeletonPointer);
		}

		// Token: 0x060002C9 RID: 713 RVA: 0x00013182 File Offset: 0x00011382
		public void ClearAll(UIntPtr Pointer)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_ClearAllDelegate(Pointer);
		}

		// Token: 0x060002CA RID: 714 RVA: 0x0001318F File Offset: 0x0001138F
		public void ComputeCornerNormals(UIntPtr Pointer, bool checkFixedNormals, bool smoothCornerNormals)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_ComputeCornerNormalsDelegate(Pointer, checkFixedNormals, smoothCornerNormals);
		}

		// Token: 0x060002CB RID: 715 RVA: 0x0001319E File Offset: 0x0001139E
		public void ComputeCornerNormalsWithSmoothingData(UIntPtr Pointer)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_ComputeCornerNormalsWithSmoothingDataDelegate(Pointer);
		}

		// Token: 0x060002CC RID: 716 RVA: 0x000131AB File Offset: 0x000113AB
		public int ComputeTangents(UIntPtr Pointer, bool checkFixedNormals)
		{
			return ScriptingInterfaceOfIManagedMeshEditOperations.call_ComputeTangentsDelegate(Pointer, checkFixedNormals);
		}

		// Token: 0x060002CD RID: 717 RVA: 0x000131BC File Offset: 0x000113BC
		public ManagedMeshEditOperations Create(UIntPtr meshPointer)
		{
			NativeObjectPointer nativeObjectPointer = ScriptingInterfaceOfIManagedMeshEditOperations.call_CreateDelegate(meshPointer);
			ManagedMeshEditOperations result = null;
			if (nativeObjectPointer.Pointer != UIntPtr.Zero)
			{
				result = new ManagedMeshEditOperations(nativeObjectPointer.Pointer);
				LibraryApplicationInterface.IManaged.DecreaseReferenceCount(nativeObjectPointer.Pointer);
			}
			return result;
		}

		// Token: 0x060002CE RID: 718 RVA: 0x00013206 File Offset: 0x00011406
		public void EnsureTransformedVertices(UIntPtr Pointer)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_EnsureTransformedVerticesDelegate(Pointer);
		}

		// Token: 0x060002CF RID: 719 RVA: 0x00013213 File Offset: 0x00011413
		public void FinalizeEditing(UIntPtr Pointer)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_FinalizeEditingDelegate(Pointer);
		}

		// Token: 0x060002D0 RID: 720 RVA: 0x00013220 File Offset: 0x00011420
		public void GenerateGrid(UIntPtr Pointer, ref Vec2i numEdges, ref Vec2 edgeScale)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_GenerateGridDelegate(Pointer, ref numEdges, ref edgeScale);
		}

		// Token: 0x060002D1 RID: 721 RVA: 0x0001322F File Offset: 0x0001142F
		public Vec3 GetPositionOfVertex(UIntPtr Pointer, int vertexIndex)
		{
			return ScriptingInterfaceOfIManagedMeshEditOperations.call_GetPositionOfVertexDelegate(Pointer, vertexIndex);
		}

		// Token: 0x060002D2 RID: 722 RVA: 0x0001323D File Offset: 0x0001143D
		public Vec3 GetVertexColor(UIntPtr Pointer, int faceCornerIndex)
		{
			return ScriptingInterfaceOfIManagedMeshEditOperations.call_GetVertexColorDelegate(Pointer, faceCornerIndex);
		}

		// Token: 0x060002D3 RID: 723 RVA: 0x0001324B File Offset: 0x0001144B
		public float GetVertexColorAlpha(UIntPtr Pointer)
		{
			return ScriptingInterfaceOfIManagedMeshEditOperations.call_GetVertexColorAlphaDelegate(Pointer);
		}

		// Token: 0x060002D4 RID: 724 RVA: 0x00013258 File Offset: 0x00011458
		public void InvertFacesWindingOrder(UIntPtr Pointer)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_InvertFacesWindingOrderDelegate(Pointer);
		}

		// Token: 0x060002D5 RID: 725 RVA: 0x00013265 File Offset: 0x00011465
		public void MoveVerticesAlongNormal(UIntPtr Pointer, float moveAmount)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_MoveVerticesAlongNormalDelegate(Pointer, moveAmount);
		}

		// Token: 0x060002D6 RID: 726 RVA: 0x00013273 File Offset: 0x00011473
		public int RemoveDuplicatedCorners(UIntPtr Pointer)
		{
			return ScriptingInterfaceOfIManagedMeshEditOperations.call_RemoveDuplicatedCornersDelegate(Pointer);
		}

		// Token: 0x060002D7 RID: 727 RVA: 0x00013280 File Offset: 0x00011480
		public void RemoveFace(UIntPtr Pointer, int faceIndex)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_RemoveFaceDelegate(Pointer, faceIndex);
		}

		// Token: 0x060002D8 RID: 728 RVA: 0x0001328E File Offset: 0x0001148E
		public void RescaleMesh2d(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax);
		}

		// Token: 0x060002D9 RID: 729 RVA: 0x0001329D File Offset: 0x0001149D
		public void RescaleMesh2dRepeatX(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dRepeatXDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide);
		}

		// Token: 0x060002DA RID: 730 RVA: 0x000132B0 File Offset: 0x000114B0
		public void RescaleMesh2dRepeatXWithTiling(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide, float xyRatio)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dRepeatXWithTilingDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide, xyRatio);
		}

		// Token: 0x060002DB RID: 731 RVA: 0x000132C5 File Offset: 0x000114C5
		public void RescaleMesh2dRepeatY(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dRepeatYDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide);
		}

		// Token: 0x060002DC RID: 732 RVA: 0x000132D8 File Offset: 0x000114D8
		public void RescaleMesh2dRepeatYWithTiling(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide, float xyRatio)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dRepeatYWithTilingDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax, frameThickness, frameSide, xyRatio);
		}

		// Token: 0x060002DD RID: 733 RVA: 0x000132ED File Offset: 0x000114ED
		public void RescaleMesh2dWithoutChangingUV(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float remaining)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_RescaleMesh2dWithoutChangingUVDelegate(Pointer, ref scaleSizeMin, ref scaleSizeMax, remaining);
		}

		// Token: 0x060002DE RID: 734 RVA: 0x000132FE File Offset: 0x000114FE
		public void ReserveFaceCorners(UIntPtr Pointer, int count)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_ReserveFaceCornersDelegate(Pointer, count);
		}

		// Token: 0x060002DF RID: 735 RVA: 0x0001330C File Offset: 0x0001150C
		public void ReserveFaces(UIntPtr Pointer, int count)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_ReserveFacesDelegate(Pointer, count);
		}

		// Token: 0x060002E0 RID: 736 RVA: 0x0001331A File Offset: 0x0001151A
		public void ReserveVertices(UIntPtr Pointer, int count)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_ReserveVerticesDelegate(Pointer, count);
		}

		// Token: 0x060002E1 RID: 737 RVA: 0x00013328 File Offset: 0x00011528
		public void ScaleVertices1(UIntPtr Pointer, float newScale)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_ScaleVertices1Delegate(Pointer, newScale);
		}

		// Token: 0x060002E2 RID: 738 RVA: 0x00013336 File Offset: 0x00011536
		public void ScaleVertices2(UIntPtr Pointer, ref Vec3 newScale, bool keepUvX, float maxUvSize)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_ScaleVertices2Delegate(Pointer, ref newScale, keepUvX, maxUvSize);
		}

		// Token: 0x060002E3 RID: 739 RVA: 0x00013347 File Offset: 0x00011547
		public void SetCornerUV(UIntPtr Pointer, int cornerNo, ref Vec2 newUV, int uvNumber)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_SetCornerUVDelegate(Pointer, cornerNo, ref newUV, uvNumber);
		}

		// Token: 0x060002E4 RID: 740 RVA: 0x00013358 File Offset: 0x00011558
		public void SetCornerVertexColor(UIntPtr Pointer, int cornerNo, ref Vec3 vertexColor)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_SetCornerVertexColorDelegate(Pointer, cornerNo, ref vertexColor);
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x00013367 File Offset: 0x00011567
		public void SetPositionOfVertex(UIntPtr Pointer, int vertexIndex, ref Vec3 position)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_SetPositionOfVertexDelegate(Pointer, vertexIndex, ref position);
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x00013376 File Offset: 0x00011576
		public void SetTangentsOfFaceCorner(UIntPtr Pointer, int faceCornerIndex, ref Vec3 tangent, ref Vec3 binormal)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_SetTangentsOfFaceCornerDelegate(Pointer, faceCornerIndex, ref tangent, ref binormal);
		}

		// Token: 0x060002E7 RID: 743 RVA: 0x00013387 File Offset: 0x00011587
		public void SetVertexColor(UIntPtr Pointer, ref Vec3 color)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_SetVertexColorDelegate(Pointer, ref color);
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x00013395 File Offset: 0x00011595
		public void SetVertexColorAlpha(UIntPtr Pointer, float newAlpha)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_SetVertexColorAlphaDelegate(Pointer, newAlpha);
		}

		// Token: 0x060002E9 RID: 745 RVA: 0x000133A3 File Offset: 0x000115A3
		public void TransformVerticesToLocal(UIntPtr Pointer, ref MatrixFrame frame)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_TransformVerticesToLocalDelegate(Pointer, ref frame);
		}

		// Token: 0x060002EA RID: 746 RVA: 0x000133B1 File Offset: 0x000115B1
		public void TransformVerticesToParent(UIntPtr Pointer, ref MatrixFrame frame)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_TransformVerticesToParentDelegate(Pointer, ref frame);
		}

		// Token: 0x060002EB RID: 747 RVA: 0x000133BF File Offset: 0x000115BF
		public void TranslateVertices(UIntPtr Pointer, ref Vec3 newOrigin)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_TranslateVerticesDelegate(Pointer, ref newOrigin);
		}

		// Token: 0x060002EC RID: 748 RVA: 0x000133CD File Offset: 0x000115CD
		public void UpdateOverlappedVertexNormals(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame attachFrame, float mergeRadiusSQ)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_UpdateOverlappedVertexNormalsDelegate(Pointer, meshPointer, ref attachFrame, mergeRadiusSQ);
		}

		// Token: 0x060002ED RID: 749 RVA: 0x000133DE File Offset: 0x000115DE
		public void Weld(UIntPtr Pointer)
		{
			ScriptingInterfaceOfIManagedMeshEditOperations.call_WeldDelegate(Pointer);
		}

		// Token: 0x0400022D RID: 557
		private static readonly Encoding _utf8 = Encoding.UTF8;

		// Token: 0x0400022E RID: 558
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddFaceDelegate call_AddFaceDelegate;

		// Token: 0x0400022F RID: 559
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddFaceCorner1Delegate call_AddFaceCorner1Delegate;

		// Token: 0x04000230 RID: 560
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddFaceCorner2Delegate call_AddFaceCorner2Delegate;

		// Token: 0x04000231 RID: 561
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddLineDelegate call_AddLineDelegate;

		// Token: 0x04000232 RID: 562
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshDelegate call_AddMeshDelegate;

		// Token: 0x04000233 RID: 563
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshAuxDelegate call_AddMeshAuxDelegate;

		// Token: 0x04000234 RID: 564
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshToBoneDelegate call_AddMeshToBoneDelegate;

		// Token: 0x04000235 RID: 565
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithColorDelegate call_AddMeshWithColorDelegate;

		// Token: 0x04000236 RID: 566
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithFixedNormalsDelegate call_AddMeshWithFixedNormalsDelegate;

		// Token: 0x04000237 RID: 567
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithFixedNormalsWithHeightGradientColorDelegate call_AddMeshWithFixedNormalsWithHeightGradientColorDelegate;

		// Token: 0x04000238 RID: 568
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddMeshWithSkinDataDelegate call_AddMeshWithSkinDataDelegate;

		// Token: 0x04000239 RID: 569
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddRectDelegate call_AddRectDelegate;

		// Token: 0x0400023A RID: 570
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddRectangle3Delegate call_AddRectangle3Delegate;

		// Token: 0x0400023B RID: 571
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddRectangleWithInverseUVDelegate call_AddRectangleWithInverseUVDelegate;

		// Token: 0x0400023C RID: 572
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddRectWithZUpDelegate call_AddRectWithZUpDelegate;

		// Token: 0x0400023D RID: 573
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddSkinnedMeshWithColorDelegate call_AddSkinnedMeshWithColorDelegate;

		// Token: 0x0400023E RID: 574
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddTriangle1Delegate call_AddTriangle1Delegate;

		// Token: 0x0400023F RID: 575
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddTriangle2Delegate call_AddTriangle2Delegate;

		// Token: 0x04000240 RID: 576
		public static ScriptingInterfaceOfIManagedMeshEditOperations.AddVertexDelegate call_AddVertexDelegate;

		// Token: 0x04000241 RID: 577
		public static ScriptingInterfaceOfIManagedMeshEditOperations.ApplyCPUSkinningDelegate call_ApplyCPUSkinningDelegate;

		// Token: 0x04000242 RID: 578
		public static ScriptingInterfaceOfIManagedMeshEditOperations.ClearAllDelegate call_ClearAllDelegate;

		// Token: 0x04000243 RID: 579
		public static ScriptingInterfaceOfIManagedMeshEditOperations.ComputeCornerNormalsDelegate call_ComputeCornerNormalsDelegate;

		// Token: 0x04000244 RID: 580
		public static ScriptingInterfaceOfIManagedMeshEditOperations.ComputeCornerNormalsWithSmoothingDataDelegate call_ComputeCornerNormalsWithSmoothingDataDelegate;

		// Token: 0x04000245 RID: 581
		public static ScriptingInterfaceOfIManagedMeshEditOperations.ComputeTangentsDelegate call_ComputeTangentsDelegate;

		// Token: 0x04000246 RID: 582
		public static ScriptingInterfaceOfIManagedMeshEditOperations.CreateDelegate call_CreateDelegate;

		// Token: 0x04000247 RID: 583
		public static ScriptingInterfaceOfIManagedMeshEditOperations.EnsureTransformedVerticesDelegate call_EnsureTransformedVerticesDelegate;

		// Token: 0x04000248 RID: 584
		public static ScriptingInterfaceOfIManagedMeshEditOperations.FinalizeEditingDelegate call_FinalizeEditingDelegate;

		// Token: 0x04000249 RID: 585
		public static ScriptingInterfaceOfIManagedMeshEditOperations.GenerateGridDelegate call_GenerateGridDelegate;

		// Token: 0x0400024A RID: 586
		public static ScriptingInterfaceOfIManagedMeshEditOperations.GetPositionOfVertexDelegate call_GetPositionOfVertexDelegate;

		// Token: 0x0400024B RID: 587
		public static ScriptingInterfaceOfIManagedMeshEditOperations.GetVertexColorDelegate call_GetVertexColorDelegate;

		// Token: 0x0400024C RID: 588
		public static ScriptingInterfaceOfIManagedMeshEditOperations.GetVertexColorAlphaDelegate call_GetVertexColorAlphaDelegate;

		// Token: 0x0400024D RID: 589
		public static ScriptingInterfaceOfIManagedMeshEditOperations.InvertFacesWindingOrderDelegate call_InvertFacesWindingOrderDelegate;

		// Token: 0x0400024E RID: 590
		public static ScriptingInterfaceOfIManagedMeshEditOperations.MoveVerticesAlongNormalDelegate call_MoveVerticesAlongNormalDelegate;

		// Token: 0x0400024F RID: 591
		public static ScriptingInterfaceOfIManagedMeshEditOperations.RemoveDuplicatedCornersDelegate call_RemoveDuplicatedCornersDelegate;

		// Token: 0x04000250 RID: 592
		public static ScriptingInterfaceOfIManagedMeshEditOperations.RemoveFaceDelegate call_RemoveFaceDelegate;

		// Token: 0x04000251 RID: 593
		public static ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dDelegate call_RescaleMesh2dDelegate;

		// Token: 0x04000252 RID: 594
		public static ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatXDelegate call_RescaleMesh2dRepeatXDelegate;

		// Token: 0x04000253 RID: 595
		public static ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatXWithTilingDelegate call_RescaleMesh2dRepeatXWithTilingDelegate;

		// Token: 0x04000254 RID: 596
		public static ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatYDelegate call_RescaleMesh2dRepeatYDelegate;

		// Token: 0x04000255 RID: 597
		public static ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dRepeatYWithTilingDelegate call_RescaleMesh2dRepeatYWithTilingDelegate;

		// Token: 0x04000256 RID: 598
		public static ScriptingInterfaceOfIManagedMeshEditOperations.RescaleMesh2dWithoutChangingUVDelegate call_RescaleMesh2dWithoutChangingUVDelegate;

		// Token: 0x04000257 RID: 599
		public static ScriptingInterfaceOfIManagedMeshEditOperations.ReserveFaceCornersDelegate call_ReserveFaceCornersDelegate;

		// Token: 0x04000258 RID: 600
		public static ScriptingInterfaceOfIManagedMeshEditOperations.ReserveFacesDelegate call_ReserveFacesDelegate;

		// Token: 0x04000259 RID: 601
		public static ScriptingInterfaceOfIManagedMeshEditOperations.ReserveVerticesDelegate call_ReserveVerticesDelegate;

		// Token: 0x0400025A RID: 602
		public static ScriptingInterfaceOfIManagedMeshEditOperations.ScaleVertices1Delegate call_ScaleVertices1Delegate;

		// Token: 0x0400025B RID: 603
		public static ScriptingInterfaceOfIManagedMeshEditOperations.ScaleVertices2Delegate call_ScaleVertices2Delegate;

		// Token: 0x0400025C RID: 604
		public static ScriptingInterfaceOfIManagedMeshEditOperations.SetCornerUVDelegate call_SetCornerUVDelegate;

		// Token: 0x0400025D RID: 605
		public static ScriptingInterfaceOfIManagedMeshEditOperations.SetCornerVertexColorDelegate call_SetCornerVertexColorDelegate;

		// Token: 0x0400025E RID: 606
		public static ScriptingInterfaceOfIManagedMeshEditOperations.SetPositionOfVertexDelegate call_SetPositionOfVertexDelegate;

		// Token: 0x0400025F RID: 607
		public static ScriptingInterfaceOfIManagedMeshEditOperations.SetTangentsOfFaceCornerDelegate call_SetTangentsOfFaceCornerDelegate;

		// Token: 0x04000260 RID: 608
		public static ScriptingInterfaceOfIManagedMeshEditOperations.SetVertexColorDelegate call_SetVertexColorDelegate;

		// Token: 0x04000261 RID: 609
		public static ScriptingInterfaceOfIManagedMeshEditOperations.SetVertexColorAlphaDelegate call_SetVertexColorAlphaDelegate;

		// Token: 0x04000262 RID: 610
		public static ScriptingInterfaceOfIManagedMeshEditOperations.TransformVerticesToLocalDelegate call_TransformVerticesToLocalDelegate;

		// Token: 0x04000263 RID: 611
		public static ScriptingInterfaceOfIManagedMeshEditOperations.TransformVerticesToParentDelegate call_TransformVerticesToParentDelegate;

		// Token: 0x04000264 RID: 612
		public static ScriptingInterfaceOfIManagedMeshEditOperations.TranslateVerticesDelegate call_TranslateVerticesDelegate;

		// Token: 0x04000265 RID: 613
		public static ScriptingInterfaceOfIManagedMeshEditOperations.UpdateOverlappedVertexNormalsDelegate call_UpdateOverlappedVertexNormalsDelegate;

		// Token: 0x04000266 RID: 614
		public static ScriptingInterfaceOfIManagedMeshEditOperations.WeldDelegate call_WeldDelegate;

		// Token: 0x020002A5 RID: 677
		// (Invoke) Token: 0x060010A7 RID: 4263
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int AddFaceDelegate(UIntPtr Pointer, int patchNode0, int patchNode1, int patchNode2);

		// Token: 0x020002A6 RID: 678
		// (Invoke) Token: 0x060010AB RID: 4267
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int AddFaceCorner1Delegate(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec3 color, ref Vec3 normal);

		// Token: 0x020002A7 RID: 679
		// (Invoke) Token: 0x060010AF RID: 4271
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int AddFaceCorner2Delegate(UIntPtr Pointer, int vertexIndex, ref Vec2 uv0, ref Vec2 uv1, ref Vec3 color, ref Vec3 normal);

		// Token: 0x020002A8 RID: 680
		// (Invoke) Token: 0x060010B3 RID: 4275
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddLineDelegate(UIntPtr Pointer, ref Vec3 start, ref Vec3 end, ref Vec3 color, float lineWidth);

		// Token: 0x020002A9 RID: 681
		// (Invoke) Token: 0x060010B7 RID: 4279
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

		// Token: 0x020002AA RID: 682
		// (Invoke) Token: 0x060010BB RID: 4283
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshAuxDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex, ref Vec3 color, [MarshalAs(UnmanagedType.U1)] bool transformNormal, [MarshalAs(UnmanagedType.U1)] bool heightGradient, [MarshalAs(UnmanagedType.U1)] bool addSkinData, [MarshalAs(UnmanagedType.U1)] bool useDoublePrecision);

		// Token: 0x020002AB RID: 683
		// (Invoke) Token: 0x060010BF RID: 4287
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshToBoneDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex);

		// Token: 0x020002AC RID: 684
		// (Invoke) Token: 0x060010C3 RID: 4291
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshWithColorDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, [MarshalAs(UnmanagedType.U1)] bool useDoublePrecision);

		// Token: 0x020002AD RID: 685
		// (Invoke) Token: 0x060010C7 RID: 4295
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshWithFixedNormalsDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

		// Token: 0x020002AE RID: 686
		// (Invoke) Token: 0x060010CB RID: 4299
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshWithFixedNormalsWithHeightGradientColorDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame);

		// Token: 0x020002AF RID: 687
		// (Invoke) Token: 0x060010CF RID: 4303
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddMeshWithSkinDataDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, sbyte boneIndex);

		// Token: 0x020002B0 RID: 688
		// (Invoke) Token: 0x060010D3 RID: 4307
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddRectDelegate(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color);

		// Token: 0x020002B1 RID: 689
		// (Invoke) Token: 0x060010D7 RID: 4311
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddRectangle3Delegate(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color);

		// Token: 0x020002B2 RID: 690
		// (Invoke) Token: 0x060010DB RID: 4315
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddRectangleWithInverseUVDelegate(UIntPtr Pointer, ref Vec3 o, ref Vec2 size, ref Vec2 uv_origin, ref Vec2 uvSize, ref Vec3 color);

		// Token: 0x020002B3 RID: 691
		// (Invoke) Token: 0x060010DF RID: 4319
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddRectWithZUpDelegate(UIntPtr Pointer, ref Vec3 originBegin, ref Vec3 originEnd, ref Vec2 uvBegin, ref Vec2 uvEnd, ref Vec3 color);

		// Token: 0x020002B4 RID: 692
		// (Invoke) Token: 0x060010E3 RID: 4323
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddSkinnedMeshWithColorDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame frame, ref Vec3 vertexColor, [MarshalAs(UnmanagedType.U1)] bool useDoublePrecision);

		// Token: 0x020002B5 RID: 693
		// (Invoke) Token: 0x060010E7 RID: 4327
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddTriangle1Delegate(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 color);

		// Token: 0x020002B6 RID: 694
		// (Invoke) Token: 0x060010EB RID: 4331
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void AddTriangle2Delegate(UIntPtr Pointer, ref Vec3 p1, ref Vec3 p2, ref Vec3 p3, ref Vec3 n1, ref Vec3 n2, ref Vec3 n3, ref Vec2 uv1, ref Vec2 uv2, ref Vec2 uv3, ref Vec3 c1, ref Vec3 c2, ref Vec3 c3);

		// Token: 0x020002B7 RID: 695
		// (Invoke) Token: 0x060010EF RID: 4335
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int AddVertexDelegate(UIntPtr Pointer, ref Vec3 vertexPos);

		// Token: 0x020002B8 RID: 696
		// (Invoke) Token: 0x060010F3 RID: 4339
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ApplyCPUSkinningDelegate(UIntPtr Pointer, UIntPtr skeletonPointer);

		// Token: 0x020002B9 RID: 697
		// (Invoke) Token: 0x060010F7 RID: 4343
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ClearAllDelegate(UIntPtr Pointer);

		// Token: 0x020002BA RID: 698
		// (Invoke) Token: 0x060010FB RID: 4347
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ComputeCornerNormalsDelegate(UIntPtr Pointer, [MarshalAs(UnmanagedType.U1)] bool checkFixedNormals, [MarshalAs(UnmanagedType.U1)] bool smoothCornerNormals);

		// Token: 0x020002BB RID: 699
		// (Invoke) Token: 0x060010FF RID: 4351
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ComputeCornerNormalsWithSmoothingDataDelegate(UIntPtr Pointer);

		// Token: 0x020002BC RID: 700
		// (Invoke) Token: 0x06001103 RID: 4355
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int ComputeTangentsDelegate(UIntPtr Pointer, [MarshalAs(UnmanagedType.U1)] bool checkFixedNormals);

		// Token: 0x020002BD RID: 701
		// (Invoke) Token: 0x06001107 RID: 4359
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate NativeObjectPointer CreateDelegate(UIntPtr meshPointer);

		// Token: 0x020002BE RID: 702
		// (Invoke) Token: 0x0600110B RID: 4363
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void EnsureTransformedVerticesDelegate(UIntPtr Pointer);

		// Token: 0x020002BF RID: 703
		// (Invoke) Token: 0x0600110F RID: 4367
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void FinalizeEditingDelegate(UIntPtr Pointer);

		// Token: 0x020002C0 RID: 704
		// (Invoke) Token: 0x06001113 RID: 4371
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void GenerateGridDelegate(UIntPtr Pointer, ref Vec2i numEdges, ref Vec2 edgeScale);

		// Token: 0x020002C1 RID: 705
		// (Invoke) Token: 0x06001117 RID: 4375
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetPositionOfVertexDelegate(UIntPtr Pointer, int vertexIndex);

		// Token: 0x020002C2 RID: 706
		// (Invoke) Token: 0x0600111B RID: 4379
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate Vec3 GetVertexColorDelegate(UIntPtr Pointer, int faceCornerIndex);

		// Token: 0x020002C3 RID: 707
		// (Invoke) Token: 0x0600111F RID: 4383
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate float GetVertexColorAlphaDelegate(UIntPtr Pointer);

		// Token: 0x020002C4 RID: 708
		// (Invoke) Token: 0x06001123 RID: 4387
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void InvertFacesWindingOrderDelegate(UIntPtr Pointer);

		// Token: 0x020002C5 RID: 709
		// (Invoke) Token: 0x06001127 RID: 4391
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void MoveVerticesAlongNormalDelegate(UIntPtr Pointer, float moveAmount);

		// Token: 0x020002C6 RID: 710
		// (Invoke) Token: 0x0600112B RID: 4395
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate int RemoveDuplicatedCornersDelegate(UIntPtr Pointer);

		// Token: 0x020002C7 RID: 711
		// (Invoke) Token: 0x0600112F RID: 4399
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RemoveFaceDelegate(UIntPtr Pointer, int faceIndex);

		// Token: 0x020002C8 RID: 712
		// (Invoke) Token: 0x06001133 RID: 4403
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RescaleMesh2dDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax);

		// Token: 0x020002C9 RID: 713
		// (Invoke) Token: 0x06001137 RID: 4407
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RescaleMesh2dRepeatXDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide);

		// Token: 0x020002CA RID: 714
		// (Invoke) Token: 0x0600113B RID: 4411
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RescaleMesh2dRepeatXWithTilingDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide, float xyRatio);

		// Token: 0x020002CB RID: 715
		// (Invoke) Token: 0x0600113F RID: 4415
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RescaleMesh2dRepeatYDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide);

		// Token: 0x020002CC RID: 716
		// (Invoke) Token: 0x06001143 RID: 4419
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RescaleMesh2dRepeatYWithTilingDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float frameThickness, int frameSide, float xyRatio);

		// Token: 0x020002CD RID: 717
		// (Invoke) Token: 0x06001147 RID: 4423
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void RescaleMesh2dWithoutChangingUVDelegate(UIntPtr Pointer, ref Vec2 scaleSizeMin, ref Vec2 scaleSizeMax, float remaining);

		// Token: 0x020002CE RID: 718
		// (Invoke) Token: 0x0600114B RID: 4427
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReserveFaceCornersDelegate(UIntPtr Pointer, int count);

		// Token: 0x020002CF RID: 719
		// (Invoke) Token: 0x0600114F RID: 4431
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReserveFacesDelegate(UIntPtr Pointer, int count);

		// Token: 0x020002D0 RID: 720
		// (Invoke) Token: 0x06001153 RID: 4435
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ReserveVerticesDelegate(UIntPtr Pointer, int count);

		// Token: 0x020002D1 RID: 721
		// (Invoke) Token: 0x06001157 RID: 4439
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ScaleVertices1Delegate(UIntPtr Pointer, float newScale);

		// Token: 0x020002D2 RID: 722
		// (Invoke) Token: 0x0600115B RID: 4443
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void ScaleVertices2Delegate(UIntPtr Pointer, ref Vec3 newScale, [MarshalAs(UnmanagedType.U1)] bool keepUvX, float maxUvSize);

		// Token: 0x020002D3 RID: 723
		// (Invoke) Token: 0x0600115F RID: 4447
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCornerUVDelegate(UIntPtr Pointer, int cornerNo, ref Vec2 newUV, int uvNumber);

		// Token: 0x020002D4 RID: 724
		// (Invoke) Token: 0x06001163 RID: 4451
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetCornerVertexColorDelegate(UIntPtr Pointer, int cornerNo, ref Vec3 vertexColor);

		// Token: 0x020002D5 RID: 725
		// (Invoke) Token: 0x06001167 RID: 4455
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetPositionOfVertexDelegate(UIntPtr Pointer, int vertexIndex, ref Vec3 position);

		// Token: 0x020002D6 RID: 726
		// (Invoke) Token: 0x0600116B RID: 4459
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetTangentsOfFaceCornerDelegate(UIntPtr Pointer, int faceCornerIndex, ref Vec3 tangent, ref Vec3 binormal);

		// Token: 0x020002D7 RID: 727
		// (Invoke) Token: 0x0600116F RID: 4463
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVertexColorDelegate(UIntPtr Pointer, ref Vec3 color);

		// Token: 0x020002D8 RID: 728
		// (Invoke) Token: 0x06001173 RID: 4467
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void SetVertexColorAlphaDelegate(UIntPtr Pointer, float newAlpha);

		// Token: 0x020002D9 RID: 729
		// (Invoke) Token: 0x06001177 RID: 4471
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TransformVerticesToLocalDelegate(UIntPtr Pointer, ref MatrixFrame frame);

		// Token: 0x020002DA RID: 730
		// (Invoke) Token: 0x0600117B RID: 4475
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TransformVerticesToParentDelegate(UIntPtr Pointer, ref MatrixFrame frame);

		// Token: 0x020002DB RID: 731
		// (Invoke) Token: 0x0600117F RID: 4479
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void TranslateVerticesDelegate(UIntPtr Pointer, ref Vec3 newOrigin);

		// Token: 0x020002DC RID: 732
		// (Invoke) Token: 0x06001183 RID: 4483
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void UpdateOverlappedVertexNormalsDelegate(UIntPtr Pointer, UIntPtr meshPointer, ref MatrixFrame attachFrame, float mergeRadiusSQ);

		// Token: 0x020002DD RID: 733
		// (Invoke) Token: 0x06001187 RID: 4487
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		[SuppressUnmanagedCodeSecurity]
		[MonoNativeFunctionWrapper]
		public delegate void WeldDelegate(UIntPtr Pointer);
	}
}
