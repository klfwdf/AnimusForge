using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000068 RID: 104
	[EngineClass("rglMesh")]
	public sealed class Mesh : Resource
	{
		// Token: 0x06000991 RID: 2449 RVA: 0x0000941A File Offset: 0x0000761A
		internal Mesh(UIntPtr meshPointer)
			: base(meshPointer)
		{
		}

		// Token: 0x06000992 RID: 2450 RVA: 0x00009423 File Offset: 0x00007623
		public static Mesh CreateMeshWithMaterial(Material material)
		{
			return EngineApplicationInterface.IMesh.CreateMeshWithMaterial(material.Pointer);
		}

		// Token: 0x06000993 RID: 2451 RVA: 0x00009435 File Offset: 0x00007635
		public static Mesh CreateMesh(bool editable = true)
		{
			return EngineApplicationInterface.IMesh.CreateMesh(editable);
		}

		// Token: 0x06000994 RID: 2452 RVA: 0x00009442 File Offset: 0x00007642
		public Mesh GetBaseMesh()
		{
			return EngineApplicationInterface.IMesh.GetBaseMesh(base.Pointer);
		}

		// Token: 0x06000995 RID: 2453 RVA: 0x00009454 File Offset: 0x00007654
		public static Mesh GetFromResource(string meshName)
		{
			return EngineApplicationInterface.IMesh.GetMeshFromResource(meshName);
		}

		// Token: 0x06000996 RID: 2454 RVA: 0x00009461 File Offset: 0x00007661
		public static Mesh GetRandomMeshWithVdecl(int inputLayout)
		{
			return EngineApplicationInterface.IMesh.GetRandomMeshWithVdecl(inputLayout);
		}

		// Token: 0x06000997 RID: 2455 RVA: 0x0000946E File Offset: 0x0000766E
		public void SetColorAndStroke(uint color, uint strokeColor, bool drawStroke)
		{
			this.Color = color;
			this.Color2 = strokeColor;
			EngineApplicationInterface.IMesh.SetColorAndStroke(base.Pointer, drawStroke);
		}

		// Token: 0x06000998 RID: 2456 RVA: 0x0000948F File Offset: 0x0000768F
		public void SetMeshRenderOrder(int renderOrder)
		{
			EngineApplicationInterface.IMesh.SetMeshRenderOrder(base.Pointer, renderOrder);
		}

		// Token: 0x06000999 RID: 2457 RVA: 0x000094A2 File Offset: 0x000076A2
		public bool HasTag(string str)
		{
			return EngineApplicationInterface.IMesh.HasTag(base.Pointer, str);
		}

		// Token: 0x0600099A RID: 2458 RVA: 0x000094B5 File Offset: 0x000076B5
		public Mesh CreateCopy()
		{
			return EngineApplicationInterface.IMesh.CreateMeshCopy(base.Pointer);
		}

		// Token: 0x0600099B RID: 2459 RVA: 0x000094C7 File Offset: 0x000076C7
		public void SetMaterial(string newMaterialName)
		{
			EngineApplicationInterface.IMesh.SetMaterialByName(base.Pointer, newMaterialName);
		}

		// Token: 0x0600099C RID: 2460 RVA: 0x000094DA File Offset: 0x000076DA
		public void SetVectorArgument(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			EngineApplicationInterface.IMesh.SetVectorArgument(base.Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x0600099D RID: 2461 RVA: 0x000094F1 File Offset: 0x000076F1
		public void SetVectorArgument2(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			EngineApplicationInterface.IMesh.SetVectorArgument2(base.Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x0600099E RID: 2462 RVA: 0x00009508 File Offset: 0x00007708
		public Vec3 GetVectorArgument()
		{
			return EngineApplicationInterface.IMesh.GetVectorArgument(base.Pointer);
		}

		// Token: 0x0600099F RID: 2463 RVA: 0x0000951A File Offset: 0x0000771A
		public Vec3 GetVectorArgument2()
		{
			return EngineApplicationInterface.IMesh.GetVectorArgument2(base.Pointer);
		}

		// Token: 0x060009A0 RID: 2464 RVA: 0x0000952C File Offset: 0x0000772C
		public void SetupAdditionalBoneBuffer(int numBones)
		{
			EngineApplicationInterface.IMesh.SetupAdditionalBoneBuffer(base.Pointer, numBones);
		}

		// Token: 0x060009A1 RID: 2465 RVA: 0x0000953F File Offset: 0x0000773F
		public void SetAdditionalBoneFrame(int boneIndex, in MatrixFrame frame)
		{
			EngineApplicationInterface.IMesh.SetAdditionalBoneFrame(base.Pointer, boneIndex, frame);
		}

		// Token: 0x060009A2 RID: 2466 RVA: 0x00009553 File Offset: 0x00007753
		public void SetMaterial(Material material)
		{
			EngineApplicationInterface.IMesh.SetMaterial(base.Pointer, material.Pointer);
		}

		// Token: 0x060009A3 RID: 2467 RVA: 0x0000956B File Offset: 0x0000776B
		public Material GetMaterial()
		{
			return EngineApplicationInterface.IMesh.GetMaterial(base.Pointer);
		}

		// Token: 0x060009A4 RID: 2468 RVA: 0x0000957D File Offset: 0x0000777D
		public Material GetSecondMaterial()
		{
			return EngineApplicationInterface.IMesh.GetSecondMaterial(base.Pointer);
		}

		// Token: 0x060009A5 RID: 2469 RVA: 0x0000958F File Offset: 0x0000778F
		public int AddFaceCorner(Vec3 position, Vec3 normal, Vec2 uvCoord, uint color, UIntPtr lockHandle)
		{
			if (base.IsValid)
			{
				return EngineApplicationInterface.IMesh.AddFaceCorner(base.Pointer, position, normal, uvCoord, color, lockHandle);
			}
			return -1;
		}

		// Token: 0x060009A6 RID: 2470 RVA: 0x000095B2 File Offset: 0x000077B2
		public int AddFace(int patchNode0, int patchNode1, int patchNode2, UIntPtr lockHandle)
		{
			if (base.IsValid)
			{
				return EngineApplicationInterface.IMesh.AddFace(base.Pointer, patchNode0, patchNode1, patchNode2, lockHandle);
			}
			return -1;
		}

		// Token: 0x060009A7 RID: 2471 RVA: 0x000095D3 File Offset: 0x000077D3
		public void ClearMesh()
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.ClearMesh(base.Pointer);
			}
		}

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x060009A8 RID: 2472 RVA: 0x000095ED File Offset: 0x000077ED
		// (set) Token: 0x060009A9 RID: 2473 RVA: 0x0000960D File Offset: 0x0000780D
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

		// Token: 0x17000056 RID: 86
		// (set) Token: 0x060009AA RID: 2474 RVA: 0x00009620 File Offset: 0x00007820
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

		// Token: 0x17000057 RID: 87
		// (set) Token: 0x060009AB RID: 2475 RVA: 0x0000963B File Offset: 0x0000783B
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

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060009AD RID: 2477 RVA: 0x0000968B File Offset: 0x0000788B
		// (set) Token: 0x060009AC RID: 2476 RVA: 0x00009656 File Offset: 0x00007856
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
					return;
				}
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\Mesh.cs", "Color", 331);
			}
		}

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x060009AF RID: 2479 RVA: 0x000096D2 File Offset: 0x000078D2
		// (set) Token: 0x060009AE RID: 2478 RVA: 0x0000969D File Offset: 0x0000789D
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
					return;
				}
				Debug.FailedAssert("false", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine\\Mesh.cs", "Color2", 354);
			}
		}

		// Token: 0x060009B0 RID: 2480 RVA: 0x000096E4 File Offset: 0x000078E4
		public void SetColorAlpha(uint newAlpha)
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.SetColorAlpha(base.Pointer, newAlpha);
			}
		}

		// Token: 0x060009B1 RID: 2481 RVA: 0x000096FF File Offset: 0x000078FF
		public uint GetFaceCount()
		{
			if (!base.IsValid)
			{
				return 0U;
			}
			return EngineApplicationInterface.IMesh.GetFaceCount(base.Pointer);
		}

		// Token: 0x060009B2 RID: 2482 RVA: 0x0000971B File Offset: 0x0000791B
		public uint GetFaceCornerCount()
		{
			if (!base.IsValid)
			{
				return 0U;
			}
			return EngineApplicationInterface.IMesh.GetFaceCornerCount(base.Pointer);
		}

		// Token: 0x060009B3 RID: 2483 RVA: 0x00009737 File Offset: 0x00007937
		public void ComputeNormals()
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.ComputeNormals(base.Pointer);
			}
		}

		// Token: 0x060009B4 RID: 2484 RVA: 0x00009751 File Offset: 0x00007951
		public void ComputeTangents()
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.ComputeTangents(base.Pointer);
			}
		}

		// Token: 0x060009B5 RID: 2485 RVA: 0x0000976C File Offset: 0x0000796C
		public void AddMesh(string meshResourceName, MatrixFrame meshFrame)
		{
			if (base.IsValid)
			{
				Mesh fromResource = Mesh.GetFromResource(meshResourceName);
				EngineApplicationInterface.IMesh.AddMeshToMesh(base.Pointer, fromResource.Pointer, ref meshFrame);
			}
		}

		// Token: 0x060009B6 RID: 2486 RVA: 0x000097A0 File Offset: 0x000079A0
		public void AddMesh(Mesh mesh, MatrixFrame meshFrame)
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.AddMeshToMesh(base.Pointer, mesh.Pointer, ref meshFrame);
			}
		}

		// Token: 0x060009B7 RID: 2487 RVA: 0x000097C4 File Offset: 0x000079C4
		public MatrixFrame GetLocalFrame()
		{
			if (base.IsValid)
			{
				MatrixFrame result = default(MatrixFrame);
				EngineApplicationInterface.IMesh.GetLocalFrame(base.Pointer, ref result);
				return result;
			}
			return default(MatrixFrame);
		}

		// Token: 0x060009B8 RID: 2488 RVA: 0x000097FE File Offset: 0x000079FE
		public void SetLocalFrame(MatrixFrame meshFrame)
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.SetLocalFrame(base.Pointer, ref meshFrame);
			}
		}

		// Token: 0x060009B9 RID: 2489 RVA: 0x0000981A File Offset: 0x00007A1A
		public void SetVisibilityMask(VisibilityMaskFlags visibilityMask)
		{
			EngineApplicationInterface.IMesh.SetVisibilityMask(base.Pointer, visibilityMask);
		}

		// Token: 0x060009BA RID: 2490 RVA: 0x0000982D File Offset: 0x00007A2D
		public void UpdateBoundingBox()
		{
			if (base.IsValid)
			{
				EngineApplicationInterface.IMesh.UpdateBoundingBox(base.Pointer);
			}
		}

		// Token: 0x060009BB RID: 2491 RVA: 0x00009847 File Offset: 0x00007A47
		public void SetAsNotEffectedBySeason()
		{
			EngineApplicationInterface.IMesh.SetAsNotEffectedBySeason(base.Pointer);
		}

		// Token: 0x060009BC RID: 2492 RVA: 0x00009859 File Offset: 0x00007A59
		public float GetBoundingBoxWidth()
		{
			if (!base.IsValid)
			{
				return 0f;
			}
			return EngineApplicationInterface.IMesh.GetBoundingBoxWidth(base.Pointer);
		}

		// Token: 0x060009BD RID: 2493 RVA: 0x00009879 File Offset: 0x00007A79
		public float GetBoundingBoxHeight()
		{
			if (!base.IsValid)
			{
				return 0f;
			}
			return EngineApplicationInterface.IMesh.GetBoundingBoxHeight(base.Pointer);
		}

		// Token: 0x060009BE RID: 2494 RVA: 0x00009899 File Offset: 0x00007A99
		public Vec3 GetBoundingBoxMin()
		{
			return EngineApplicationInterface.IMesh.GetBoundingBoxMin(base.Pointer);
		}

		// Token: 0x060009BF RID: 2495 RVA: 0x000098AB File Offset: 0x00007AAB
		public Vec3 GetBoundingBoxMax()
		{
			return EngineApplicationInterface.IMesh.GetBoundingBoxMax(base.Pointer);
		}

		// Token: 0x060009C0 RID: 2496 RVA: 0x000098C0 File Offset: 0x00007AC0
		public void AddTriangle(Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint color, UIntPtr lockHandle)
		{
			EngineApplicationInterface.IMesh.AddTriangle(base.Pointer, p1, p2, p3, uv1, uv2, uv3, color, lockHandle);
		}

		// Token: 0x060009C1 RID: 2497 RVA: 0x000098EC File Offset: 0x00007AEC
		public void AddTriangleWithVertexColors(Vec3 p1, Vec3 p2, Vec3 p3, Vec2 uv1, Vec2 uv2, Vec2 uv3, uint c1, uint c2, uint c3, UIntPtr lockHandle)
		{
			EngineApplicationInterface.IMesh.AddTriangleWithVertexColors(base.Pointer, p1, p2, p3, uv1, uv2, uv3, c1, c2, c3, lockHandle);
		}

		// Token: 0x060009C2 RID: 2498 RVA: 0x0000991A File Offset: 0x00007B1A
		public void HintIndicesDynamic()
		{
			EngineApplicationInterface.IMesh.HintIndicesDynamic(base.Pointer);
		}

		// Token: 0x060009C3 RID: 2499 RVA: 0x0000992C File Offset: 0x00007B2C
		public void HintVerticesDynamic()
		{
			EngineApplicationInterface.IMesh.HintVerticesDynamic(base.Pointer);
		}

		// Token: 0x060009C4 RID: 2500 RVA: 0x0000993E File Offset: 0x00007B3E
		public void RecomputeBoundingBox()
		{
			EngineApplicationInterface.IMesh.RecomputeBoundingBox(base.Pointer);
		}

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x060009C5 RID: 2501 RVA: 0x00009950 File Offset: 0x00007B50
		// (set) Token: 0x060009C6 RID: 2502 RVA: 0x00009962 File Offset: 0x00007B62
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

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x060009C7 RID: 2503 RVA: 0x00009975 File Offset: 0x00007B75
		// (set) Token: 0x060009C8 RID: 2504 RVA: 0x00009987 File Offset: 0x00007B87
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

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x060009C9 RID: 2505 RVA: 0x0000999A File Offset: 0x00007B9A
		public int EditDataFaceCornerCount
		{
			get
			{
				return EngineApplicationInterface.IMesh.GetEditDataFaceCornerCount(base.Pointer);
			}
		}

		// Token: 0x060009CA RID: 2506 RVA: 0x000099AC File Offset: 0x00007BAC
		public void SetEditDataFaceCornerVertexColor(int index, uint color)
		{
			EngineApplicationInterface.IMesh.SetEditDataFaceCornerVertexColor(base.Pointer, index, color);
		}

		// Token: 0x060009CB RID: 2507 RVA: 0x000099C0 File Offset: 0x00007BC0
		public uint GetEditDataFaceCornerVertexColor(int index)
		{
			return EngineApplicationInterface.IMesh.GetEditDataFaceCornerVertexColor(base.Pointer, index);
		}

		// Token: 0x060009CC RID: 2508 RVA: 0x000099D3 File Offset: 0x00007BD3
		public void PreloadForRendering()
		{
			EngineApplicationInterface.IMesh.PreloadForRendering(base.Pointer);
		}

		// Token: 0x060009CD RID: 2509 RVA: 0x000099E5 File Offset: 0x00007BE5
		public void SetContourColor(Vec3 color, bool alwaysVisible, bool maskMesh)
		{
			EngineApplicationInterface.IMesh.SetContourColor(base.Pointer, color, alwaysVisible, maskMesh);
		}

		// Token: 0x060009CE RID: 2510 RVA: 0x000099FA File Offset: 0x00007BFA
		public void DisableContour()
		{
			EngineApplicationInterface.IMesh.DisableContour(base.Pointer);
		}

		// Token: 0x060009CF RID: 2511 RVA: 0x00009A0C File Offset: 0x00007C0C
		public void SetExternalBoundingBox(BoundingBox bbox)
		{
			EngineApplicationInterface.IMesh.SetExternalBoundingBox(base.Pointer, ref bbox);
		}

		// Token: 0x060009D0 RID: 2512 RVA: 0x00009A20 File Offset: 0x00007C20
		public void AddEditDataUser()
		{
			EngineApplicationInterface.IMesh.AddEditDataUser(base.Pointer);
		}

		// Token: 0x060009D1 RID: 2513 RVA: 0x00009A32 File Offset: 0x00007C32
		public void ReleaseEditDataUser()
		{
			EngineApplicationInterface.IMesh.ReleaseEditDataUser(base.Pointer);
		}

		// Token: 0x060009D2 RID: 2514 RVA: 0x00009A44 File Offset: 0x00007C44
		public void SetEditDataPolicy(EditDataPolicy policy)
		{
			EngineApplicationInterface.IMesh.SetEditDataPolicy(base.Pointer, policy);
		}

		// Token: 0x060009D3 RID: 2515 RVA: 0x00009A57 File Offset: 0x00007C57
		public UIntPtr LockEditDataWrite()
		{
			return EngineApplicationInterface.IMesh.LockEditDataWrite(base.Pointer);
		}

		// Token: 0x060009D4 RID: 2516 RVA: 0x00009A69 File Offset: 0x00007C69
		public void UnlockEditDataWrite(UIntPtr handle)
		{
			EngineApplicationInterface.IMesh.UnlockEditDataWrite(base.Pointer, handle);
		}

		// Token: 0x060009D5 RID: 2517 RVA: 0x00009A7C File Offset: 0x00007C7C
		public void SetCustomClipPlane(Vec3 clipPlanePosition, Vec3 clipPlaneNormal, int planeIndex)
		{
			EngineApplicationInterface.IMesh.SetCustomClipPlane(base.Pointer, clipPlanePosition, clipPlaneNormal, planeIndex);
		}

		// Token: 0x060009D6 RID: 2518 RVA: 0x00009A91 File Offset: 0x00007C91
		public float GetClothLinearVelocityMultiplier()
		{
			return EngineApplicationInterface.IMesh.GetClothLinearVelocityMultiplier(base.Pointer);
		}

		// Token: 0x060009D7 RID: 2519 RVA: 0x00009AA3 File Offset: 0x00007CA3
		public bool HasCloth()
		{
			return EngineApplicationInterface.IMesh.HasCloth(base.Pointer);
		}
	}
}
