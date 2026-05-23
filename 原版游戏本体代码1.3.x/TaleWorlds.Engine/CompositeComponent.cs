using System;
using TaleWorlds.DotNet;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000011 RID: 17
	[EngineClass("rglComposite_component")]
	public sealed class CompositeComponent : GameEntityComponent
	{
		// Token: 0x0600007C RID: 124 RVA: 0x000033F6 File Offset: 0x000015F6
		internal CompositeComponent(UIntPtr pointer)
			: base(pointer)
		{
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600007D RID: 125 RVA: 0x000033FF File Offset: 0x000015FF
		public bool IsValid
		{
			get
			{
				return base.Pointer != UIntPtr.Zero;
			}
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00003411 File Offset: 0x00001611
		public static bool IsNull(CompositeComponent component)
		{
			return component == null || component.Pointer == UIntPtr.Zero;
		}

		// Token: 0x0600007F RID: 127 RVA: 0x0000342E File Offset: 0x0000162E
		public static CompositeComponent CreateCompositeComponent()
		{
			return EngineApplicationInterface.ICompositeComponent.CreateCompositeComponent();
		}

		// Token: 0x06000080 RID: 128 RVA: 0x0000343A File Offset: 0x0000163A
		public CompositeComponent CreateCopy()
		{
			return EngineApplicationInterface.ICompositeComponent.CreateCopy(base.Pointer);
		}

		// Token: 0x06000081 RID: 129 RVA: 0x0000344C File Offset: 0x0000164C
		public void AddComponent(GameEntityComponent component)
		{
			EngineApplicationInterface.ICompositeComponent.AddComponent(base.Pointer, component.Pointer);
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00003464 File Offset: 0x00001664
		public void AddPrefabEntity(string prefabName, Scene scene)
		{
			EngineApplicationInterface.ICompositeComponent.AddPrefabEntity(base.Pointer, scene.Pointer, prefabName);
		}

		// Token: 0x06000083 RID: 131 RVA: 0x0000347D File Offset: 0x0000167D
		public void Dispose()
		{
			if (this.IsValid)
			{
				this.Release();
				GC.SuppressFinalize(this);
			}
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00003493 File Offset: 0x00001693
		private void Release()
		{
			EngineApplicationInterface.ICompositeComponent.Release(base.Pointer);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000034A8 File Offset: 0x000016A8
		~CompositeComponent()
		{
			this.Dispose();
		}

		// Token: 0x06000086 RID: 134 RVA: 0x000034D4 File Offset: 0x000016D4
		public uint GetFactor1()
		{
			return EngineApplicationInterface.ICompositeComponent.GetFactor1(base.Pointer);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x000034E6 File Offset: 0x000016E6
		public uint GetFactor2()
		{
			return EngineApplicationInterface.ICompositeComponent.GetFactor2(base.Pointer);
		}

		// Token: 0x06000088 RID: 136 RVA: 0x000034F8 File Offset: 0x000016F8
		public void SetFactor1(uint factorColor1)
		{
			EngineApplicationInterface.ICompositeComponent.SetFactor1(base.Pointer, factorColor1);
		}

		// Token: 0x06000089 RID: 137 RVA: 0x0000350B File Offset: 0x0000170B
		public void SetFactor2(uint factorColor2)
		{
			EngineApplicationInterface.ICompositeComponent.SetFactor2(base.Pointer, factorColor2);
		}

		// Token: 0x0600008A RID: 138 RVA: 0x0000351E File Offset: 0x0000171E
		public void SetVectorArgument(float vectorArgument0, float vectorArgument1, float vectorArgument2, float vectorArgument3)
		{
			EngineApplicationInterface.ICompositeComponent.SetVectorArgument(base.Pointer, vectorArgument0, vectorArgument1, vectorArgument2, vectorArgument3);
		}

		// Token: 0x0600008B RID: 139 RVA: 0x00003535 File Offset: 0x00001735
		public void SetMaterial(Material material)
		{
			EngineApplicationInterface.ICompositeComponent.SetMaterial(base.Pointer, material.Pointer);
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600008C RID: 140 RVA: 0x00003550 File Offset: 0x00001750
		// (set) Token: 0x0600008D RID: 141 RVA: 0x00003578 File Offset: 0x00001778
		public MatrixFrame Frame
		{
			get
			{
				MatrixFrame result = default(MatrixFrame);
				EngineApplicationInterface.ICompositeComponent.GetFrame(base.Pointer, ref result);
				return result;
			}
			set
			{
				EngineApplicationInterface.ICompositeComponent.SetFrame(base.Pointer, ref value);
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x0600008E RID: 142 RVA: 0x0000358C File Offset: 0x0000178C
		// (set) Token: 0x0600008F RID: 143 RVA: 0x0000359E File Offset: 0x0000179E
		public Vec3 VectorUserData
		{
			get
			{
				return EngineApplicationInterface.ICompositeComponent.GetVectorUserData(base.Pointer);
			}
			set
			{
				EngineApplicationInterface.ICompositeComponent.SetVectorUserData(base.Pointer, ref value);
			}
		}

		// Token: 0x06000090 RID: 144 RVA: 0x000035B2 File Offset: 0x000017B2
		public void SetVisibilityMask(VisibilityMaskFlags visibilityMask)
		{
			EngineApplicationInterface.ICompositeComponent.SetVisibilityMask(base.Pointer, visibilityMask);
		}

		// Token: 0x06000091 RID: 145 RVA: 0x000035C5 File Offset: 0x000017C5
		public override MetaMesh GetFirstMetaMesh()
		{
			return EngineApplicationInterface.ICompositeComponent.GetFirstMetaMesh(base.Pointer);
		}

		// Token: 0x06000092 RID: 146 RVA: 0x000035D7 File Offset: 0x000017D7
		public void AddMultiMesh(string MultiMeshName)
		{
			EngineApplicationInterface.ICompositeComponent.AddMultiMesh(base.Pointer, MultiMeshName);
		}

		// Token: 0x06000093 RID: 147 RVA: 0x000035EA File Offset: 0x000017EA
		public void SetVisible(bool visible)
		{
			EngineApplicationInterface.ICompositeComponent.SetVisible(base.Pointer, visible);
		}

		// Token: 0x06000094 RID: 148 RVA: 0x000035FD File Offset: 0x000017FD
		public bool GetVisible()
		{
			return EngineApplicationInterface.ICompositeComponent.IsVisible(base.Pointer);
		}
	}
}
